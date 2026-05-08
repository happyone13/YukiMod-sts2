# YukiMod AI 协作工作流

## 1. 目标

这份文档定义后续 AI 在本仓库内工作的基础流程，目的是让每次进入仓库时都能按同一套规则推进，而不是临时发挥。

## 2. 每次工作的最小流程

1. 先读 `AGENTS.md`
2. 再读 `docs/project-overview.zh-CN.md`
3. 根据任务类型补读对应文档
4. 再开始实现、修改或验证

推荐映射如下：

- 角色设定、文案、视觉语义：读 `docs/character-brief.zh-CN.md`
- 模板清理、命名迁移、发布前整理：读 `docs/template-cleanup-checklist.zh-CN.md`
- 运行时 Hook、回合开始/抽牌时序、递归 Hover Tip 排错：读 `docs/runtime-hook-notes.zh-CN.md`
- 纯工程或构建问题：以上两份可按需跳读，但项目概览仍应先读

## 3. 任务分类原则

后续任务默认分成三类，避免范围混乱：

- 模板清理
  - 把梅玲遗留、占位文本、乱码、无效命名逐步处理掉
- 新内容建设
  - 为友纪新增机制、卡牌、能力、遗物、药水、资源与本地化
- 工程稳定性
  - 处理构建、导出、补丁、注册、资源路径、编码与工具链问题

一个任务可以横跨多类，但在实现时必须说明主目标是什么，避免“顺手大改”。

## 4. 实施规则

### 4.1 修改前

- 先确认改动是模板迁移，还是友纪新内容，还是工程修复
- 先看已有代码和路径，不用记忆代替检查
- 涉及 `YukiMod.csproj`、构建目标版本、BaseLib 版本或导出流程时，先对照兄弟仓库 `E:\DATA\GODOT\MyMod\MeiLinMod-sts2` 中的 `MeiLinMod.csproj`，再决定 YukiMod 是否需要同步适配
- 涉及原版 `104` 行为时，优先查看解包目录 `E:\DATA\GODOT\MyMod\sts104`；涉及当前 103 兼容构建的 API 签名时，以配置的 103 游戏目录中的 `data_sts2_windows_x86_64/sts2.dll` 为准
- 涉及“回合开始”“抽牌前/后”“重抽整手牌”“是否计入起手抽牌”等效果时，先对照对应目标版本的 `CombatManager.cs` 实际调用顺序；只有 104 资料时可先参考 `sts104/src/Core/Combat/CombatManager.cs`，再用 103 API 校验
- 如果碰到 `meilin` 命名的资源，先判断它是暂时沿用还是准备迁移，不要自动重命名
- 运行时报错优先查看 `C:\Users\lozalia\AppData\Roaming\SlayTheSpire2\logs` 下最新的 `godot.log` 或时间戳日志，再决定修复方向
- 如果 Godot 编辑器里出现 `SpineAtlasResource` / `SpineSkeletonFileResource` 依赖损坏，先检查仓库根目录 `bin/spine_godot_extension.gdextension` 及对应 `bin/windows/libspine_godot...dll` 是否存在；缺这层时，编辑器会把 `.atlas/.skel` 误判成坏依赖
- 如果本地场景引用了原版工程里的 `res://src/...` 或 `res://scenes/...` 路径，而当前仓库没有这些文件，优先补最小本地桥接文件；对 C# 脚本优先使用继承原版类型的薄包装器，避免直接复制同名原版类导致与 `sts2.dll` 类型冲突

### 4.2 修改时

- 优先做闭环改动，不留“代码加了但资源/本地化/注册没跟上”的半成品
- 新规则要落文档，尤其是命名规则、机制规则、流程规则
- 卡牌描述里的通用关键词（如 `消耗`、`固有`、`保留`）默认依赖原版/框架自动追加；除非效果正文确实需要提到它们，否则不要在 `cards.json` 手写重复一遍
- 衍生牌 / Token 牌优先对照原版 `SovereignBlade` 实现：使用 `CardRarity.Token`，并挂到 `TokenCardPool`，不要继续放在角色主卡池里
- 自定义词条（如 `预见`）如果不是原版内置 `CardKeyword`，正文里要手动写成 `[gold]关键词[/gold]`，并在 `ExtraHoverTips` 里补对应侧边说明
- 如果卡牌或能力描述里直接提到了会生成、加入或关联的另一张具体卡牌，优先用 `HoverTipFactory.FromCardWithCardHoverTips<T>()` 补侧边卡牌预览，做法对照原版 `Forge -> SovereignBlade`
- 如果两张或多张卡会互相预览，至少一侧改用 `HoverTipFactory.FromCard<T>()`，避免 `FromCardWithCardHoverTips<T>()` 递归展开造成卡牌生成或悬浮说明卡死
- 需要选牌的卡，如果代码里使用 `SelectionScreenPrompt`，必须同时在 `cards.json` 中补上 `<CARD_ID>.selectionScreenPrompt`；缺这个键会在运行时抛 `No selection screen prompt for CARD...`，并表现为选牌界面卡住
- 动态数值描述按原版格式写占位符；会升级或会被动态变量修改的数值默认使用 `:diff()`，例如 `{Damage:diff()}`、`{Block:diff()}`、`{Repeat:diff()}`、`{Cards:diff()}`
- 中文 `cards.json` 中，数值与中文量词、标点之间默认不留多余空格，例如 `造成{Damage:diff()}点伤害。`
- 卡牌效果里的“消耗一张手牌/消耗若干牌”等结算，优先复用原版方法，如 `CardCmd.Exhaust(...)` 与原版选牌流程；不要手动移牌来模拟消耗
- 卡牌外观要拆开处理：默认卡牌优先走统一卡框资源，动态肖像只对单卡开启；不要把“有动态卡图”当成“必须有特殊卡框”的前提
- 从抽牌堆/弃牌堆/手牌选择牌时，优先找原版同类卡做参照；例如从抽牌堆选牌进手优先对照 `sts104` 中的 `SecretTechnique`
- 卡牌效果、费用、稀有度、种类以 `docs/yuki-card-table.xlsx` 为第一权威来源；若旧 md、旧代码、旧对话记录与表格冲突，以当前 xlsx 为准
- 当前默认占位资源约定为：卡牌缺图回退到 `YukiMod/images/card_portraits/card.png`，力量缺图回退到 `YukiMod/images/powers/power.png`，遗物缺图回退到 `YukiMod/images/relics/relic.png` 与 `relic_outline.png`；卡牌、力量、遗物的大图路径均复用小图路径
- 新文本文件默认使用 `UTF-8`
- 对 `Task`、`IEnumerable` 这类基础类型，尽量在源码里显式写 `using System.Threading.Tasks;`、`using System.Collections.Generic;`，不要只依赖 `ImplicitUsings` 或 `.godot/mono/temp/obj/...GlobalUsings.g.cs`，否则 IDE/编辑器索引异常时容易出现“缺少引用”的假报错

### 4.3 修改后

- 能编译就编译，能验证就验证
- 涉及代码或资源内容的任务，在环境允许时完成时必须确认最新 `YukiMod.pck` 已导出；如果 `dotnet build` 没有产出 `.pck`，则补跑一次 Godot 手动导出
- 当前仓库可用的手动导出兜底命令为：`Godot_v4.5.1-stable_mono_win64_console.exe --headless --export-pack "BasicExport" "D:/steam/steamapps/common/Slay the Spire 2/mods/YukiMod/YukiMod.pck"`；若导出阶段遇到 `msbuild_issues.csv` 文件锁，先执行 `dotnet build-server shutdown` 再重试
- 如果无法验证，明确写出阻塞条件
- 若任务改变了共识，应同步更新相关文档

## 5. 推荐的最小交付单位

对于角色内容开发，优先接受以下粒度：

- 一个明确的核心机制加其承载能力
- 一张卡牌及其完整注册链路
- 一个遗物及其触发闭环
- 一次编码/本地化/模板资源清理闭环

不推荐一开始直接铺大量空文件或大批量占位内容。

## 6. 文档更新触发条件

出现以下情况时，需要在同一轮工作内更新文档：

- 确认了友纪的正式设定或机制方向
- 新增了全局命名约定或目录约定
- 修正了构建路径、导出流程或环境前提
- 决定正式迁移一批模板资源
- 确认某些模板内容将被长期保留

## 7. 完成定义

一个任务默认在满足以下条件后才算真正完成：

- 相关代码或文档已经落地
- 影响范围内的注册/路径/文案没有明显断链
- 做过可行的验证，或明确说明为何无法验证
- 需要更新的说明文档已经同步更新

## 8. 当前阶段的优先级建议

现阶段建议遵守以下优先级：

1. 先把“规则入口”和“项目事实”写清楚
2. 再确认友纪是什么角色，不急着扩大量内容
3. 然后做第一条核心机制的纵切片
4. 最后再做大规模内容扩张和资源换皮
