# YukiMod 运行时 Hook 与排错笔记

## 1. 用途

这份文档记录已经踩过并确认过的运行时时序问题，重点解决两类高频错误：

- 选错 Hook，导致“回合开始”“抽牌后”“进手牌后”等效果落在错误时机
- 侧边预览或描述展开递归，导致卡牌生成、渲染或悬浮说明卡死

默认原则：

1. 先看 `sts104` 原版调用链，再决定覆写哪个 Hook
2. 先确认效果要作用于“当前手牌”“将要抽到的牌”还是“刚抽到的牌”
3. 如果文案里提到具体卡牌预览，先检查是否会递归展开对方的 Hover Tip

## 2. 回合开始与抽牌的实际顺序

当前版本 `104` 的玩家回合开始流程，核心顺序在 `sts104/src/Core/Combat/CombatManager.cs` 已确认：

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
- 如果效果要求处理“回合开始后已经在手里的整手牌”，应优先考虑 `AfterPlayerTurnStart`

## 3. 案例：天际斩击

### 3.1 现象

`天际斩击` 的“每回合开始时，将手牌洗入抽牌堆，然后抽取等量的牌”在实战里看起来没有触发。

### 3.2 根因

最初实现挂在 `BeforeHandDraw`。这会导致效果发生在系统正常抽牌之前：

- 此时手牌里只有上回合保留下来的牌
- 本回合开始的正常抽牌还没有进入手牌
- 如果设计目标是重洗“本回合开始后的整手牌”，这个 Hook 就偏早了

### 3.3 修复

将实现从 `BeforeHandDraw` 改为 `AfterPlayerTurnStart`，文件在：

- `YukiModCode/Powers/TianJiZhanJiPower.cs`

这样效果会在系统完成本回合开始抽牌后触发，能够正确处理当前整手牌。

### 3.4 选 Hook 的判断规则

遇到“回合开始”类效果时，先问三个问题：

1. 它要作用于抽牌前、抽牌数量、还是抽牌后的手牌？
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

## 5. 案例：荣耀卡死

`荣耀` 的卡牌生成卡死，不是退出流程问题，而是 Hover Tip 递归：

- `荣耀` 预览 `居合`
- `居合` 预览 `纳刀`
- `纳刀` 又预览 `居合`

如果三者都使用 `HoverTipFactory.FromCardWithCardHoverTips<T>()`，展开说明时会继续递归带出对方的 Hover Tip，最终卡死。

修复策略：

- 对会互相引用的卡，不要双方都用递归版预览
- 至少一侧改成 `HoverTipFactory.FromCard<T>()`

## 6. 开发时的最小检查表

写或改一个效果前，至少做这几步：

1. 在 `sts104/src/Core/Combat/CombatManager.cs` 找到主调用链
2. 在 `sts104/src/Core/Hooks/Hook.cs` 确认目标 Hook 会分发到哪些模型
3. 明确效果是“改流程”还是“读结果”
4. 如果描述里引用其他卡牌，检查 Hover Tip 是否可能互相递归
5. 完成后至少做一次 `dotnet build`
6. 环境允许时确认最新 `YukiMod.pck` 已导出

## 7. 后续规则

以后凡是涉及以下主题，先读这份文档再动手：

- 回合开始
- 抽牌数量修改
- 回合开始时抽牌与额外抽牌的区分
- 洗回牌堆后重抽
- 带具体卡牌预览的 Hover Tip
