# YukiMod 运行时 Hook 与排错笔记

## 1. 用途

这份文档记录已经踩过并确认过的运行时问题，重点解决两类高频错误：

- 选错 Hook，导致“回合开始”“抽牌后”“进手牌后”等效果落在错误时机
- 自定义卡牌显示逻辑污染公共 `NCard` 节点，导致原版卡也出现错位、缺字或费用消失

默认原则：

1. 可先看 `sts104` 原版调用链，再用目标 107 的 `sts2.dll` 校验 Hook 签名
2. 先确认效果要作用于“当前手牌”“将要抽到的牌”还是“刚抽到的牌”
3. 如果文案里提到具体卡牌预览，先检查是否会递归展开对方的 Hover Tip
4. 如果改动了 `NCard` 原始控件，默认把它当成“必须可恢复的临时状态”

## 2. 回合开始与抽牌的实际顺序

当前可参考的玩家回合开始流程，核心顺序在
`sts104/src/Core/Combat/CombatManager.cs` 中已确认；用于 107 正式版时还需要以目标版本 API 校验签名：

1. `Hook.BeforeHandDraw(...)`
2. `Hook.ModifyHandDraw(...)`
3. `CardPileCmd.Draw(..., fromHandDraw: true)`
4. `Hook.AfterPlayerTurnStart(...)`

对应源码位置：

- `E:\DATA\GODOT\MyMod\sts104\src\Core\Combat\CombatManager.cs:466`
- `E:\DATA\GODOT\MyMod\sts104\src\Core\Combat\CombatManager.cs:468`
- `E:\DATA\GODOT\MyMod\sts104\src\Core\Combat\CombatManager.cs:487`
- `E:\DATA\GODOT\MyMod\sts104\src\Core\Combat\CombatManager.cs:489`

结论不要搞反：

- `BeforeHandDraw` 看到的是“本次正常抽牌发生前”的状态
- 这个时点可以改抽牌数量、阻止抽牌、或处理保留在手里的旧牌
- 这个时点看不到本回合刚抽进手牌的那些牌
- 如果效果要求处理“回合开始后已经在手里的整手牌”，优先考虑 `AfterPlayerTurnStart`

## 3. 案例：天际斩击

### 3.1 现象

`天际斩击` 的“每回合开始时，将手牌洗入抽牌堆，然后抽取等量的牌”在实战里看起来没有正确触发。

### 3.2 根因

最初实现挂在 `BeforeHandDraw`。这会导致效果发生在系统正常抽牌之前：

- 此时手牌里只有上回合保留下来的牌
- 本回合开始的正常抽牌还没进入手牌
- 如果设计目标是重洗“本回合开始后的整手牌”，这个 Hook 就偏早了

### 3.3 修复

将实现从 `BeforeHandDraw` 改为 `AfterPlayerTurnStart`，文件在：

- `YukiModCode/Powers/TianJiZhanJiPower.cs`

这样效果会在系统完成本回合开始抽牌后触发，能够正确处理当前整手牌。

### 3.4 选 Hook 的判断规则

遇到“回合开始”类效果时，先问三个问题：

1. 它要作用于抽牌前、抽牌数量，还是抽牌后的手牌？
2. 它要读到的是保留牌，还是本回合刚抽到的牌？
3. 它是修改抽牌流程，还是在抽牌完成后再做二次处理？

建议映射：

- 改抽牌次数、阻止正常抽牌、在抽牌前处理状态：`BeforeHandDraw`
- 依赖本回合刚抽到的手牌内容：`AfterPlayerTurnStart`
- 统计“是否为回合开始时抽的牌”：看 `CardDrawnEntry.FromHandDraw`

## 4. 案例：剑舞

`剑舞` 需要“和灵感一样，不计算回合开始时抽的牌”。这类判断不要靠猜测当前阶段，而要直接读历史记录：

- 统计 `CombatManager.Instance.History.Entries.OfType<CardDrawnEntry>()`
- 过滤 `entry.HappenedThisTurn(combatState)`
- 过滤 `!entry.FromHandDraw`

这样能稳定区分：

- 回合开始时系统发起的正常抽牌
- 本回合中途通过卡牌、能力、遗物触发的额外抽牌

## 5. 案例：Hover Tip 递归卡死

这类卡牌生成或悬浮说明卡死，常见根因不是退出流程，而是 Hover Tip 递归：

- `荣耀` 预览 `居合`
- `居合` 预览 `纳刀`
- `纳刀` 又预览 `居合`

如果几张牌都使用 `HoverTipFactory.FromCardWithCardHoverTips<T>()`，
展开说明时会继续递归带出对方的 Hover Tip，最终卡死。

修复策略：

- 对会互相引用的卡，不要双方都用递归版预览
- 至少一侧改成 `HoverTipFactory.FromCard<T>()`

## 6. 案例：自定义卡框污染原版卡 UI

这类问题的核心结论不是“原版卡主动调用了自定义 UI”，而是：

- `NCard` 是公共卡牌节点
- `NCard` 会被对象池复用
- 自定义卡框补丁通常全局挂在 `NCard.UpdateVisuals`、`NCard.Reload`、`NCard._EnterTree`
- 补丁不是只新增一层显示，而是会直接修改 `NCard` 自身的原始控件

只要自定义分支修改过原始控件，但在“切回原版卡”或“回收到对象池”时没有完整恢复，就会把脏状态传给下一张卡。

高风险的原始控件包括：

- `_energyLabel`
- `_typeLabel`
- `_typePlaque`
- `_titleLabel`
- `_descriptionLabel`
- `_energyIcon`
- `_frame`
- `_banner`
- `_portrait`

### 6.1 典型现象

- 原版卡费用偶发消失
- 原版卡类型文字或类型牌匾消失
- 原版卡标题、描述、费用位置错位
- 原版卡沿用了自定义卡框残留的贴图、显隐或透明度

### 6.2 为什么会这样

核心原因是“公共节点状态污染”：

1. 一张自定义卡进入 `NCard` 后，补丁修改了原始控件位置、贴图、显隐或透明度
2. 这张卡离开后，节点没有完整恢复
3. 同一个 `NCard` 被对象池分配给下一张原版卡
4. 原版卡继承了上一张自定义卡留下的 UI 状态

所以看到的问题虽然发生在“原版卡”身上，但真正的错误往往在自定义卡框补丁的清理和回退分支里。

### 6.3 修复原则

#### 原则 A：回收到对象池前，必须恢复原始控件状态

如果补丁使用了类似 `OriginalStates` 的快照恢复机制，那么在 `NCard.OnFreedToPool` 时不能只删除自定义 overlay，必须先恢复原始控件，再清理附加节点和缓存状态。

建议顺序：

1. `RemoveChaosEffects(..., restoreOriginalState: true)`
2. 移除动态立绘 overlay
3. `OriginalStates.Remove(__instance)`

只删除自定义节点、不恢复原始控件，会把错位和隐藏状态继续带给下一张卡。

#### 原则 B：凡是“回退到原版 UI”的分支，不要再隐藏原版控件

如果某个分支的语义是“当前不显示自定义 UI，改回原版 UI”，那这个分支里：

- 原版 `_energyLabel` 应为 `Show()`
- 原版 `_typeLabel` 应为 `Show()`
- 原版 `_typePlaque` 应为 `Show()`

可以删除自定义费用/类型 overlay，但不能一边删 overlay，一边把原版 label 也 `Hide()` 掉。

这是“原版费用消失”最容易出现的直接逻辑错误。

#### 原则 C：自定义 overlay 尽量少继承原版控件的运行时状态

自定义费用文字、类型文字、额外图层如果需要同步原版控件：

- 可以同步层级，如 `ZIndex`
- 不要默认继承 `Modulate` / `SelfModulate`

否则原版控件的透明度、淡入淡出状态也可能把自定义 overlay 一起隐藏。

#### 原则 D：恢复原版控件时必须恢复 sibling 绘制顺序

Godot 同父节点、同 `ZIndex` 的 `Control` 绘制顺序会受 child index 影响。如果自定义卡框对原版 `_banner`、`_titleLabel`、`_descriptionLabel`、`_energyIcon` 等控件调用过 `MoveChild()` / `BringToFront()`，快照不能只恢复 `ZIndex`，还必须恢复原父节点下的原始 sibling index。

典型表现是：打开友纪卡牌大图后再查看其他卡，标题背景偶发盖住费用图标。根因不是费用图标丢失，而是标题背景节点残留在费用图标之后绘制。

### 6.4 适用于 MeiLinMod / YukiMod 的检查项

如果 `MeiLinMod` 或 `YukiMod` 继续出现“原版卡费用消失”“原版卡错位”之类的问题，优先检查：

1. 是否给 `NCard.OnFreedToPool` 补了恢复原始控件的清理逻辑
2. 所有 fallback / transition 分支里，是否错误隐藏了原版 `_energyLabel`、`_typeLabel`、`_typePlaque`
3. 是否有自定义 overlay 继承了原版 label 的透明度或颜色状态
4. 是否只移除了 overlay，却没有恢复 `NCard` 原始控件的 `Visible / Position / Size / Texture / Material`
5. 是否调用过 `MoveChild()` / `BringToFront()`，但快照恢复没有还原原始 child index

### 6.5 排查结论模板

以后遇到同类问题，先按下面这句判断方向：

“不是原版卡调用了自定义 UI，而是自定义卡框补丁污染了公共 `NCard` 节点，原版卡复用了这个脏节点。”

## 7. 案例：自定义 Spine 资源污染原版角色模型

### 7.1 典型现象

原版角色路径仍然正确，例如日志里显示创建的是 `res://scenes/rest_site/characters/ironclad_rest_site.tscn` 或 `res://scenes/merchant/characters/ironclad_merchant.tscn`，但实际显示成 Mod 角色模型。

### 7.2 根因

从原版资源复制 `.tres` 或 `.tscn` 后，如果保留了原版资源的 `uid="uid://..."`，Godot 可能按 UID 把原版场景里的内部资源解析到 Mod 资源。Yuki 旧皮肤阶段遗留过这个问题：

- `rest_site_chaos_yuki_skel_data.tres` 曾复用铁甲火堆 `SpineSkeletonDataResource` 的 UID
- `chaos_yuki_merchant_skel_data.tres` 曾复用铁甲商店 `SpineSkeletonDataResource` 的 UID

结果不是铁甲路径被 C# 改掉，而是铁甲场景内部按 UID 取到了友纪的骨骼数据。

### 7.3 修复原则

复制原版 Godot 资源作为 Mod 资源时，不要保留原版 UID。对 Yuki 专用 Spine 资源，优先删除复制来的 `uid="uid://..."`，让资源按 `res://YukiMod/...` 路径解析；若确实需要 UID，则必须生成新的、不与原版冲突的 UID。

## 8. 开发时的最小检查表

写或改一个效果前，至少做这几步：

1. 在 `sts104/src/Core/Combat/CombatManager.cs` 找到主调用链
2. 在 `sts104/src/Core/Hooks/Hook.cs` 确认目标 Hook 会分发到哪些模型
3. 明确效果是“改流程”还是“读结果”
4. 如果描述里引用其他卡牌，检查 Hover Tip 是否可能互相递归
5. 如果改动了 `NCard` 原始控件，确认存在成对的恢复路径
6. 完成后至少做一次 `dotnet build`
7. 环境允许时确认最新 `YukiMod.pck` 已导出

## 9. 后续规则

以后凡是涉及以下主题，先读这份文档再动手：

- 回合开始
- 抽牌数量修改
- 回合开始时抽牌与额外抽牌的区分
- 洗回牌堆后重抽
- 带具体卡牌预览的 Hover Tip
- 自定义卡框、动态立绘、原版 `NCard` UI 污染
