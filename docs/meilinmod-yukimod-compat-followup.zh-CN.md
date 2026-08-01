# MeiLinMod 与 YukiMod 共载兼容修改说明

## 背景

玩家的 STS2 `0.109.0` 日志显示，在古代事件获得 `ArcaneScroll`、预览 Yuki 卡牌时，MeiLinMod 的全局卡牌立绘补丁进入了 Yuki 卡牌的显示和回收流程：

```text
MeiLinMod.MeiLinModCode.Patches.CardSpinePortraitPatch.RestorePortraitTextures
MeiLinMod.MeiLinModCode.Patches.CardSpinePortraitPatch.RemoveSpineOverlay
MeiLinMod.MeiLinModCode.Patches.CardSpinePortraitUpdateVisualsPatch.Postfix
CardModel.get_Portrait
res://YukiMod/images/card_portraits/shun_nian.png
```

这不是该次 `RelicCmd.Obtain` 空引用的首要根因，但说明 MeiLinMod 的立绘补丁会触碰其他模组卡牌，违反共载隔离要求，并会在对方资源缺失或对象池复用时扩大故障。

## 必须修改

目标文件主要是：

- `MeiLinModCode/Patches/CardSpinePortraitPatch.cs`
- 注册 `Reload`、`UpdateVisuals`、`OnFreedToPool` 等全局卡牌 Hook 的相关补丁文件

建议采用以下所有权规则：

1. `Apply`、`PrepareForBaseVisuals`、`RemoveSpineOverlay`、`RestorePortraitTextures` 入口必须先判断当前 `NCard.Model` 是否属于 MeiLinMod。
2. 所有权应使用 MeiLin 自己的模型接口或基类判断，例如 `IMeiLinCardVisualProfile`，不要根据节点类型、资源路径是否为空或“是否有 Portrait”判断。
3. `RemoveSpineOverlay` 可以清理残留的 `MeiLinSpinePortraitOverlay`，但如果当前模型不是 MeiLin：
   - 只删除 MeiLin 自己创建的节点和 metadata；
   - 不读取 `cardNode.Model.Portrait`；
   - 不修改普通立绘、远古立绘的 Texture；
   - 不恢复或覆盖其他模组的 Visible 状态。
4. 如果当前模型既不是 MeiLin，也没有 MeiLin 创建的残留 overlay，必须立即返回。
5. 对象池回收时，不能假定当前 `NCard.Model` 仍是创建 overlay 时的模型。建议 overlay metadata 保存模型身份，并在清理时区分“清理自己的残留节点”和“恢复 MeiLin 模型纹理”。
6. `RestorePortraitTextures` 只能对 MeiLin 模型运行；资源加载失败时安全返回，不应继续向 foreign card 写入空 Texture。
7. `MeiLinTriggerAnimPatch` 等全局攻击/动画补丁也应在入口先完成角色或卡牌所有权判断，再记录日志或访问动画资源。当前共载测试中，Yuki 攻击会输出：

```text
[MeiLinMod] [MeiLinTriggerAnimPatch] Player attack trigger. character=YUKIMOD_YUKI_MOD, isMeiLin=False
```

虽然该路径目前判断后没有播放 MeiLin 动画，但仍说明补丁会拦截所有模组角色攻击；建议把非 MeiLin 情况改为无日志的立即返回。

推荐结构：

```csharp
bool ownsModel = cardNode.Model is IMeiLinCardVisualProfile;
bool hasOwnedOverlay = HasActiveMeiLinOverlay(cardNode);
if (!ownsModel && !hasOwnedOverlay)
    return;

RemoveOnlyMeiLinOverlayNodes(cardNode);

if (ownsModel)
{
    RestoreMeiLinPortraitTextures(cardNode);
    RestoreMeiLinVisibility(cardNode);
}
else
{
    DropMeiLinSnapshotWithoutTouchingForeignVisuals(cardNode);
}
```

## 建议同时修改

日志还有以下兼容警告：

```text
[Acheron] Recommended to add a prefix such as "MeiLinMod_" to SavedProperty TurnsSeen for compatibility.
```

请将过于通用的 SavedProperty 名称 `TurnsSeen` 改为带稳定模组前缀的名称，例如 `MeiLinMod_TurnsSeen`，并按当前保存兼容策略保留旧字段迁移，避免与其他模组的序列化字段碰撞。

## 验证用例

至少覆盖：

1. 只启用 MeiLinMod，普通卡牌、动态立绘卡牌、远古卡牌显示和对象池回收正常。
2. 同时启用 MeiLinMod 与 YukiMod，分别预览两边的普通卡牌和动态立绘卡牌。
3. 使用 `ArcaneScroll` 等会连续创建、预览并回收卡牌节点的遗物。
4. Yuki 卡图故意缺失时，MeiLinMod 日志中不能出现 `CardSpinePortraitPatch` 尝试加载 `res://YukiMod/...`。
5. 卡牌节点从 MeiLin 模型复用为 Yuki/原版模型后，只清除 MeiLin overlay，不覆盖新模型的 Texture/Visible。

## 与本次事件卡死的边界

该玩家最终卡住的异常链为：

```text
AncientAffection 替换遗物
-> HextechRunes.DoubleVisionRune
-> RitsuLib HarmonyAsyncTaskBridge
-> RelicCmd.Obtain NullReferenceException
```

因此 MeiLinMod 的上述修改用于修复明确存在的跨模组污染，但不能替代 AncientAffection 与 HextechRunes 对遗物交易兼容性的修复。
