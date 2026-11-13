# YukiMod 0.113 迁移记录

验证日期：2026-11-13。发布版本：YukiMod v0.1.8。

宿主为本机 STS2 v0.113.0（commit `16ac0fe3`），RitsuLib 0.6.11 使用其 0.113.0 runtime。这里记录的是实际验证组合，不代表 RitsuLib 的最新版本或最低兼容版本。

## 修复范围

- 默认构建目标切换至 113，增加 `Sts2Path113`、`STS2_113_PATH` 和 `STS2_113`，同步主清单与测试清单。嵌套 Godot 导出传递同一目标和输出目录。
- `Hook.AfterDeath` 精确反射目标和 Gloomy 的死亡回调均适配新增 `CardModel source` 参数，保留原有死亡过滤及替补数量逻辑。
- 两处 `SelectCharacter` Harmony postfix 从 `charSelectButton.Character` 取得角色，不再注入已删除的原方法参数。
- 遥测改用 `RunHistoryFiltered`，从 `TelemetryCaptureContext.SourceData` 读取 `RunEndedEvent`，保留原有 Yuki 筛选与同意配置。
- `ZhaoJia` 的自定义动态变量仅在旧目标保留 `GetBaseValueForIConvertible` override。0.113 已删除此成员，数值转换使用现有预览值；原卡牌效果及预览计算不变。
- 增加仅匹配 Yuki 的 RitsuLib 动画 cue 修正，将结束/放弃流程请求的 `die` 映射至 Spine 实际存在的 `death`，保留其他角色和普通战斗逻辑。
- 选择框场景删除失效的纹理 UID，保留有效资源路径。
- 测试引用本地重建的 0.113 TestTheSpire，并在测试安装包中明确复制该 DLL。辅助脚本为 `tools/build_testthespire_113.ps1`。

## 验证结果

| 项目 | 结果 |
| --- | --- |
| 主项目构建及全新 PCK 导出 | 0 warnings / 0 errors |
| 测试项目构建 | 0 warnings / 0 errors |
| 完整回归 | 37/37，通过；无跳过；进程退出码 0；stderr 为空 |
| D3D12 图形迁移测试 | 5/5，通过；进程退出码 0 |
| 安装 DLL 元数据检查 | 361 个游戏成员引用全部解析，963 个虚方法重写无失配，基类解析无缺失 |
| 可选内容/表现补丁 | 1/1、6/6 应用成功 |
| VFX 预载 | 48/48，失败 0 |

新增测试实际调用角色选择并检查两个背景、背景按钮及两个已注册 postfix；通过真实 RitsuLib 播放接口检查死亡动画轨道；验证 Gloomy 死亡后下个敌方回合生成一个替补；验证 `ZhaoJia` 在已出一张牌后，预览、数值转换与实际格挡均为 6。

图形测试的角色选择与死亡截图已人工查看。首次图形测试发现的选择框纹理 UID 警告已在最终重新导出前修复，最终完整回归错误日志为空。图形环境另有引擎不支持 PSO 缓存的提示。

测试日志与截图位于 `tmp/migration113/`：

- `all-tests-stdout.log`、`all-tests-stderr.log`
- `graphics-stdout.log`、`graphics-stderr.log`
- `character-selection.png`、`death-animation.png`
- `export.log`、`test-build.log`

## 部署与边界

全新 DLL、JSON、PCK 部署至 `D:\STEAM\steamapps\common\Slay the Spire 2\mods\YukiMod`。测试存档使用项目内隔离目录，测试完成后移出本次安装的 TestTheSpire、xunit 和 Yuki 测试 Mod，保留正式 Yuki、MeiLin、RitsuLib。

保留旧目标的条件编译路径，但本轮只验证 0.113。未完成整局人工通关或真实联网多人验证，不将模型层多人用例视为联网实测。没有因 MeiLin 的音效问题而给 Yuki 增加未经证实需要的音频补丁。
