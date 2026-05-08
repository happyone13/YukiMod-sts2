# STS2 103.2 适配记录

项目当前只兼容 `103.2` 正式版，`YukiMod.csproj` 默认且唯一支持的 `Sts2TargetVersion` 为 `103`。

103 侧 API 约束：

1. `ICombatState` 不存在，代码侧使用 `CombatState`。
2. `CardPileCmd.AddGeneratedCardsToCombat(...)` 在 103.2 使用 `addedByPlayer` 参数，不传 `Player`。
3. `PowerCmd.Apply(...)` 与 `PowerCmd.ModifyAmount(...)` 在 103.2 不接收 `PlayerChoiceContext`。
4. `AfterPowerAmountChanged` 签名为 `(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)`。
5. `AfterAttack` 签名为 `(AttackCommand command)`。
6. `AfterSideTurnStart` 签名使用 `CombatState`。

本项目保留以下 103 侧辅助封装：

- `YukiPowerService`
- `YukiCardPileService`
- `YukiCombatStateAlias`
