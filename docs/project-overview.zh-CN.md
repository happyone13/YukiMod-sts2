# YukiMod 项目概览

## 1. 项目定位

`YukiMod` 是从 `MeiLinMod-sts2` 复制出来的新角色 Mod 项目，当前服务对象是角色 `友纪 / Yuki`。

现阶段它的定位不是“完整角色”，而是：

- 已保留可运行的角色模板骨架
- 已接上 Godot + C# + BaseLib + Harmony 的基础工程
- 尚未建立完整的角色机制、卡池、遗物池、药水池与最终视觉命名

后续开发应把“模板可运行”逐步推进到“角色可定义、机制可验证、资源可替换、文档可维护”。

## 2. 当前技术栈

- `Godot 4.5.1`
- `C# / .NET 9`
- `BaseLib`
- `Harmony`

构建产物仍沿用标准双件式结构：

- `YukiMod.dll`：游戏逻辑、角色注册、补丁和代码侧模型
- `YukiMod.pck`：Godot 资源包，包括场景、图片、本地化和动画资源

## 3. 当前目录重点

### 根目录

- `MainFile.cs`
  - Mod 初始化入口，负责脚本查找、配置注册和 Harmony 补丁加载
- `YukiMod.csproj`
  - 构建、依赖、拷贝到游戏目录、Godot 导出流程
- `YukiMod.json`
  - Mod 清单
- `AGENTS.md`
  - 后续 AI 协作的仓库级入口规则

### 代码目录

- `YukiModCode/Character`
  - 角色模型与牌池/遗物池/药水池定义
- `YukiModCode/Cards`
  - 当前为空
- `YukiModCode/Powers`
  - 当前为空
- `YukiModCode/Relics`
  - 当前为空
- `YukiModCode/Potions`
  - 当前为空
- 其他目录如 `Config`、`Patches`、`Services`、`StanceVfx`
  - 已预留模板结构，但内容仍需按友纪设计逐步落地

### 资源目录

- `YukiMod/scenes`
  - 已有友纪场景骨架和商店场景路径
- `YukiMod/images/cards/chaos_frame` 与 `YukiMod/images/cards/card_effects`
  - 已接入一套可复用的 Yuki 卡框资源，默认卡牌可直接走静态图 + 统一框体，动态肖像仅对个别卡按需开启
- `YukiMod/images/charui`
  - 仍保留多张 `meilin` 命名的模板图片
- `YukiMod/localization`
  - 英文文案可读，中文文案目前存在明显编码/占位问题
- `YukiMod/spine`
  - 已有占位动画资源目录

## 4. 代码层已确认现状

### 角色模型

当前 `YukiModCode/Character/YukiMod.cs` 已确认：

- 角色 ID：`YukiMod`
- 性别：`Feminine`
- 名字主色：`#9DD9D2`
- 初始生命：`72`
- 初始卡组：`4` 张 `StrikeYuki`、`4` 张 `DefendYuki`、`1` 张 `压制准备`、`1` 张 `拔刀`
- 初始遗物：`YukiStarterRelic`（月影吊坠），战斗开始时 `凝聚1`

这意味着项目已经能以“占位角色”的方式挂接进角色系统，但还没有真正可玩的内容闭环。

### 视觉与路径

当前角色模型已经绑定：

- 角色图标场景
- 角色立绘场景
- 篝火场景
- 商店角色场景
- 角色选择背景场景

但 `charui` 下仍存在 `character_icon_meilin_name.png`、`char_select_char_meilin.png`、`map_marker_meilin_name.png` 等模板遗留命名。它们目前是可用资源，不应在未规划迁移时被随手重命名。

### 文案状态

- 英文本地化仍是模板语义，例如 “Template character based on MeiLinMod.”
- 中文 `characters.json` 当前存在乱码，不能视为可信文案来源

## 5. 构建与环境约束

`YukiMod.csproj` 当前约束包括：

- Windows 下默认依赖本机 `Slay the Spire 2` 安装目录
- Windows 下 `GodotPath` 固定指向 `E:\SOFT\godot\Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64.exe`
- 当前默认且唯一支持的构建目标为 `107` 正式版，`BaseLib` 默认版本为 `3.2.0`
- 当前已知原游戏解包参考目录仍为 `E:\DATA\GODOT\MyMod\sts104`，只能作为旧版行为参考；涉及当前 API 时以本机 107 游戏目录中的 `data_sts2_windows_x86_64/sts2.dll` 为准
- 构建后会尝试把 `.dll`、`YukiMod.json`、`.pck` 复制或导出到游戏 `mods/YukiMod/`
- `export_presets.cfg` 的 `BasicExport` 使用 `binary_format/architecture="msil"`，避免 Godot/.NET 资源包导出被固定到 Windows x64；`spine_godot_extension.gdextension` 已将 Windows `msil` 导出映射到现有 x86_64 Spine DLL，避免导出阶段出现 `unknown_arch` 警告；macOS 实际运行仍需要对应平台的原生 GDExtension 依赖可用

因此后续凡是涉及“可构建”或“可导出”的任务，都要同时考虑：

- 本机游戏目录是否存在
- Godot 路径是否仍然有效
- 资源导出是否跟代码版本一致
- 如需核对原版能力、遗物、卡牌、动作或 Hook 行为，104 行为仍可先看 `E:\DATA\GODOT\MyMod\sts104`；107 API 签名需要以当前配置的 107 游戏目录 `data_sts2_windows_x86_64/sts2.dll` 为准

## 6. 当前已知空缺与风险

- 起始卡组与起始遗物已经有第一版闭环，`月影 / 黑云 / 灵感 / 雪月花` 的首轮机制实现已经接通
- `月影 Token -> 凝聚 -> 初始遗物 -> 月光` 这一条最小纵切片已经接通
- 月影主线的第一批承载牌与能力（如 `盈月`、`满月`、`朔月`、`影月`、`残月`）已经补到仓库，可进入游戏做集中回归
- `雪 / 月 / 花` 现已按卡表接入：不可叠加、两两联动、三件套转化 `居合`
- 当前保留的角色药水实现：`凝聚药水`
- 药水图片目前仍是占位资源，后续需要替换正式美术
- 中文角色文案不可直接使用
- 部分 UI 资源仍是梅玲模板命名
- 当前仓库只有 Windows Spine GDExtension 原生库目录；若要在 macOS 上运行 Spine 动画资源，需要补齐 `macos/libspine_godot.*.framework`
- README 仍停留在“模板说明”层

## 7. 当前推荐推进顺序

建议按以下顺序推进，而不是同时摊大：

1. 先稳定协作规则和项目文档
2. 再确认友纪的角色定位、核心机制与命名边界
3. 然后做一个最小可玩的纵切片
4. 最后逐步清理模板遗留资源并扩内容量

这份文档只描述当前已确认的工程事实，不替代未来的角色设定文档。
