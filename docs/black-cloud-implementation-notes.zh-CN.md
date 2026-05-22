# 黑云实现笔记

日期：2026-04-29

这份文档只记录当前已经落地的黑云实现口径，避免后续补牌时重新猜时序。

## 1. 状态源

- `BlackCloudStancePower`
  - 是“当前是否处于黑云姿态”的唯一状态源。
- `BlackCloudPower`
  - 是“黑云层数”的唯一状态源。
  - 下次进入黑云姿态时转换为等量力量。
  - 退出黑云姿态时移除这些力量和自身。
- `DelayedBlackCloudPower`
  - 是“黑云姿态中获得的黑云”的暂存状态源。
  - 它不会影响当前黑云姿态已经提供的力量。
  - 退出黑云姿态后，转化为等量 `BlackCloudPower`，供下次进入黑云姿态使用。
- 不再允许每张黑云牌自己各写一套 `if/else` 来判断姿态。

## 2. 统一入口

- 所有写着“`[gold]黑云[/gold]：...`”的牌，统一走：
  - `YukiBlackCloudService.Resolve(...)`
- 固定规则：
  - 如果已在黑云姿态中，则执行黑云额外效果。
  - 否则，获得 1 层 `BlackCloudPower`。
  - `BlackCloudPower` 会在下次进入黑云姿态时转换为等量力量，并在退出黑云姿态时移除这些力量和自身。
  - 如果某张牌还写有“否则进入黑云姿态”，该进入姿态效果由单卡在 `Resolve(...)` 之后显式调用 `Enter(...)`，例如 `拔刀`。
  - 如果某张牌写有“否则消耗 N 层黑云”，只有当前 `BlackCloudPower.Amount >= N` 时才触发；层数不足时该否则分支不触发。

## 2.1 黑云层数辅助入口

- 获得黑云层：
  - `YukiBlackCloudService.GainBlackCloud(...)`
  - 如果当前不在黑云姿态，直接获得 `BlackCloudPower`。
  - 如果当前已在黑云姿态，改为获得 `DelayedBlackCloudPower`，本次姿态力量不变，退出姿态后再转为普通黑云。
- 消耗黑云层：
  - `YukiBlackCloudService.TryConsumeBlackCloud(...)`
  - 返回 `false` 时，调用方不要结算后续延迟收益。

## 3. 退出规则

- 全局规则仍然是：
  - 处于黑云姿态时，使用非攻击牌会退出黑云。
- 但“进入黑云的这张非攻击牌自己把姿态立刻退掉”是无效实现。
- 所以当前服务层固定补了一个保护：
  - 如果是一张非攻击牌让你进入黑云姿态，自动发放一次性的“本次不退出黑云”保护。

## 4. 两种“保留黑云”

这两个语义已经分开，后续不要混用。

- `BlackCloudKeepStanceOncePower`
  - 只保护当前这一次非攻击结算。
  - 用于：
    - `黑云奥义：残`
    - `黑云秘法：燕回`
    - `黑云秘法：霞阵`
    - 以及其它明确写着“黑云：保留黑云姿态”的单次牌
- `BlackCloudKeepStanceThisTurnPower`
  - 整回合内非攻击都不会退出黑云。
  - 用于：
    - `黑云心法`

## 5. 回合开始时序

- 当前“回合开始时进入黑云姿态 / 抽黑云牌”的实现统一挂在 `BeforeHandDraw`。
- 原因：
  - 它们属于起手抽牌阶段的一部分。
  - 这样可以尽量和 `fromHandDraw: true` 对齐，减少把回合开始抽牌误算成“本回合额外抽牌”的风险。
- 已按这个口径接入：
  - `黑雾降临`
  - `如影随形`
  - `黑云秘法：积雨`
  - `拨云见日`
  - `黑云秘法：虚像`

## 6. 延迟触发写法

- “下次进入黑云姿态时...”统一做成监听 Power：
  - 接口：`IBlackCloudEnteredListener`
- “下次退出黑云姿态时...”统一做成监听 Power：
  - 接口：`IBlackCloudExitedListener`

这样做的目的：

- 延迟效果不散落在单卡里。
- 叠层时更容易处理。
- 后续如果黑云时序要改，只改服务层和监听层。

## 7. 当前已实现的监听类

- 进入黑云时触发：
  - `HeiYunMiFaJiangLinPower`
  - `HeiYunMiFaMuLinPower`
  - `HeiYunMiFaHunYouPower`
  - `HeiYunMiFaYingFuPower`
  - `YinLeiTianYunPower`
- 退出黑云时触发：
  - `HeiYunMiFaChuQiaoPower`

## 8. 直接写“进入黑云姿态”的技能牌

这类牌不能只调用一次 `Enter(...)` 就结束。

原因：

- 如果本来已经在黑云姿态中，技能牌本身还是会触发“非攻击退出黑云”。

所以像 `黑云秘法：出鞘` 这种牌，需要额外处理：

- 如果当前已在黑云姿态，先补一次性保护。
- 再执行进入黑云和后续效果。

## 9. 当前验证状态

- `dotnet build YukiMod.csproj`
  - 已通过
- `YukiMod.pck`
  - 已重新导出到：
  - `D:\Steam\steamapps\common\Slay the Spire 2\mods\YukiMod\YukiMod.pck`
- 最新日志目录检查到的仍是旧运行日志：
  - `godot.log` 最后时间是 2026-04-29 12:54:16
  - 这说明本轮代码改完后还没有新的游戏内回归日志
