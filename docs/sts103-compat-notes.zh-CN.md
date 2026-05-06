# STS2 103.2 适配记录

项目保留 `103.2` 源码兼容分支，但当前本地 Steam 游戏版本为 `0.104` 形态，`YukiMod.csproj` 默认 `Sts2TargetVersion` 为 `104`。

注意：`0.103` 和 `0.104` 不能共用同一个已编译 DLL。需要按目标版本分别构建，并确保 `Sts2Path103` / `Sts2Path104` 指向对应版本的游戏目录。

与 104 侧 API 的主要差异：

1. `ICombatState` 不存在，代码侧使用 `CombatState`。
2. `CardPileCmd.AddGeneratedCardsToCombat(...)` 在 103.2 使用 `addedByPlayer` 参数，不传 `Player`。
3. `PowerCmd.Apply(...)` 与 `PowerCmd.ModifyAmount(...)` 在 103.2 不接收 `PlayerChoiceContext`。
4. `AfterPowerAmountChanged` 签名为 `(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)`。
5. `AfterAttack` 签名为 `(AttackCommand command)`。
6. `AfterSideTurnStart` 签名使用 `CombatState`。

本项目用以下兼容封装隔离版本差异：

- `YukiPowerService`
- `YukiCardPileService`
