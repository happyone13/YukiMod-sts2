# YukiMod 0.114 迁移记录

验证日期：2026-11-25。当前清单版本保持 `v0.1.10`。

宿主为本机 STS2 `v0.114.0`（commit `a7b9a9c0`），运行期 RitsuLib 为 `mods/STS2-RitsuLib.0.6.16.variant-pack`（日志确认 `Host=v0.114.0; runtime=0.114.0`）。

## 0.114 破坏点核对

- `CardCmd.Downgrade` / `CardCmd.RemoveAllUpgrades`：未调用 `CardCmd.Downgrade`，不受影响。
- `HoverTipAlignment.Center`：未使用，不受影响。
- 热键 API（新增 `Node owner` / 删除 `ClearHotkeys()`）：未使用，不受影响。
- `NFtue` 抽象类与 `SeenOnShowId`：未继承 `NFtue`，不受影响。
- `Hook` 新增 `BeforeActEntered`：不影响既有 Hook 签名与反射补丁目标。

## 修改范围

- `YukiMod.csproj`：默认 `Sts2TargetVersion` 升至 `114`，`RitsuLibVersion` 升至 `0.6.16`；新增 `Sts2Path114` / `STS2_114_PATH` / `STS2_114` 条件编译常量、依赖检查与嵌套 Godot 导出环境变量传递，同时保留 `107`–`113` 路由。
- `YukiMod.json`：`min_game_version` 升至 `v0.114.0`，`STS2-RitsuLib` `min_version` 升至 `0.6.16`。
- 扩展条件编译分支以纳入 `STS2_114`：
  - `YukiModCode/Cards/ZhaoJia.cs`（`#if !STS2_113 && !STS2_114` 保护旧版 `GetBaseValueForIConvertible` override）
  - `YukiModCode/Mechanics/CardHoldOverlay/YukiBattleReadyHookPatches.cs`（`Hook.AfterDeath` 含 `CardModel` 参数目标）
  - `YukiModCode/Monsters/Gloomy/GloomyPrimeMonster.cs`（`AfterDeath` 重写签名）
- 测试工程与工具：新增 `tools/build_testthespire_114.ps1`；更新 `tests/YukiMod.Tests/YukiMod.Tests.csproj` 及测试清单至 `v0.114.0` / `0.6.16`；当 `mods/` 已安装 `STS2-RitsuLib.0.6.16.variant-pack` 时跳过旧 Workshop RitsuLib 镜像复制。
- 文档同步更新：`docs/project-overview.zh-CN.md`、`docs/ai-workflow.zh-CN.md`。

## 验证结果

| 检查项 | 结果 |
| --- | --- |
| 主工程整体重编译 & Godot PCK 导出 | 0 warnings, 0 errors |
| `YukiMod.Tests` 与 `TestTheSpire` (0.114) 编译 | 0 warnings, 0 errors |
| Cecil 静态元数据扫描（vs 0.114 `sts2.dll`） | 363 个游戏成员引用 0 缺失；966 个虚方法重写 0 失配 |
| RitsuLib 框架补丁组（0.6.16 / 0.114.0） | 7 组共 692/692 应用成功，0 失败 |
| YukiMod 可选补丁组与自动注册 | 2 组共 7/7 应用成功，AutoRegister 5/5 成功，0 失败 |
| TestTheSpire 无界面回归 | `48 passed / 1 failed / 0 skipped`（total 49） |

非通过用例说明（与 0.114 无关）：

- `FAILED`：`AnimationGenerationGuardTests.Ready_watchers_are_bound_to_both_request_and_watch_generations`（既有源码字符串断言仍检查旧版 `session.OriginalSiblingIndex = attackerNode.GetIndex()`，早于本次 0.114 迁移的 0.1.9 表现重构已改动该实现）。

## 部署与边界

- 新版 `YukiMod.dll`、`YukiMod.json`、`YukiMod.pck` 已部署至 `D:\steam\steamapps\common\Slay the Spire 2\mods\YukiMod`。
- 测试完成后已从游戏 `mods/` 移出临时测试辅助 Mod，仅保留正式 Mod 与 RitsuLib 0.6.16。
- 本轮验证基于无界面 TestTheSpire 回归与静态元数据扫描，未执行图形模式人工整局通关或联机实测。
