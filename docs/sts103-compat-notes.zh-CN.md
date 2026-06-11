# STS2 103.2 适配记录

本文件是历史适配记录。当前项目默认目标已迁移到 `107` 正式版；以下内容仅用于回看 103.2 时代的 API 差异，不再代表当前构建目标。

103 侧 API 约束：

1. `ICombatState` 不存在，代码侧使用 `CombatState`。
2. `CardPileCmd.AddGeneratedCardsToCombat(...)` 在 103.2 使用 `addedByPlayer` 参数，不传 `Player`。
3. `PowerCmd.Apply(...)` 与 `PowerCmd.ModifyAmount(...)` 在 103.2 不接收 `PlayerChoiceContext`。
4. `AfterPowerAmountChanged` 签名为 `(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)`。
5. `AfterAttack` 签名为 `(AttackCommand command)`。
6. `AfterSideTurnStart` 签名使用 `CombatState`。

103 适配期曾保留以下辅助封装：

- `YukiPowerService`
- `YukiCardPileService`
- `YukiCombatStateAlias`
