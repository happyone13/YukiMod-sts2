# YukiMod PCK 打包与导出说明

本文说明 `YukiMod.pck` 是怎么生成的、生成到哪里、什么时候需要手动导出，以及常见问题怎么排查。

## 产物关系

YukiMod 在游戏里不是单文件 Mod，而是两个主要产物一起工作：

- `YukiMod.dll`：C# 逻辑，包括卡牌、能力、遗物、补丁、服务等代码。
- `YukiMod.pck`：Godot 资源包，包括图片、场景、本地化、Spine 资源、音频等。
- `YukiMod.json`：Mod 清单，声明版本、依赖、是否有 `.dll` / `.pck`。

游戏实际加载目录是：

```text
D:\steam\steamapps\common\Slay the Spire 2\mods\YukiMod\
```

正常导出后，这个目录至少应有：

```text
YukiMod.dll
YukiMod.json
YukiMod.pck
```

## 一条命令完成构建和导出

仓库根目录执行：

```powershell
dotnet build YukiMod.csproj -v:minimal
```

这条命令会做三件事：

1. 编译 C#，生成 `YukiMod.dll`。
2. 把 `YukiMod.dll` 和根目录的 `YukiMod.json` 复制到游戏的 `mods/YukiMod/`。
3. 调用 Godot 4.5.1 headless export，把资源导出为 `mods/YukiMod/YukiMod.pck`。

因此，平时修改代码、图片、本地化、场景后，优先跑这一条命令，不需要手动复制 `.pck`。

## 自动导出的实现位置

自动流程写在 `YukiMod.csproj`。

### 路径检查

项目使用：

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1" InitialTargets="CheckDependencyPaths">
```

`CheckDependencyPaths` 会在构建前检查：

- `Sts2TargetVersion` 必须是 `107` 或 `108`，默认是 `108`。
- 当前目标版本对应的 `Sts2Path107` 或 `Sts2Path108` 必须能解析到游戏安装目录。
- `Sts2DataDir` 必须存在，即游戏数据目录。
- `GodotPath` 必须存在。

当前 YukiMod 默认支持 `108`，并保留 `107` 作为显式传参兼容构建目标；不要按旧模板传 `103`。

### 复制 dll 和 manifest

`CopyToModsFolderOnBuild` 会在构建后复制：

```text
$(TargetPath) -> $(ModsPath)YukiMod/
YukiMod.json -> $(ModsPath)YukiMod/
```

也就是把 `YukiMod.dll` 和 `YukiMod.json` 放进游戏 Mod 目录。

### 导出 pck

`GodotPackOnBuild` 会在 `Build` 后执行：

```powershell
"$(GodotPath)" --headless --export-pack "BasicExport" "$(ModsPath)YukiMod/YukiMod.pck"
```

同时设置环境变量：

```text
IsInnerGodotExport=true
MSBUILDDISABLENODEREUSE=1
STS2_107_PATH=$(Sts2Path107)
STS2_108_PATH=$(Sts2Path108)
```

`IsInnerGodotExport=true` 用来避免 Godot 导出过程中再次触发外层导出，造成递归构建。

## 关键路径如何解析

Windows 下，`YukiMod.csproj` 目前按这个顺序找路径。

### GodotPath

优先使用：

```text
H:/Trae_home/ChaosMod/Godot_v4.5.1/Godot_v4.5.1-stable_mono_win64.exe
```

如果不存在，则使用：

```text
E:\SOFT\godot\Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64.exe
```

注意：必须是 Godot `4.5.1` Mono 版。游戏使用的 Godot 版本和导出版本不一致时，`.pck` 可能无法被游戏正确加载。

### Sts2Path108 / Sts2Path107

优先级：

1. MSBuild 属性 `-p:Sts2Path108=...` 或 `-p:Sts2Path107=...`
2. 环境变量 `STS2_108_PATH` 或 `STS2_107_PATH`
3. `H:/SteamLibrary/steamapps/common/Slay the Spire 2`
4. Steam 注册表推断出的默认库
5. `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2`

本机当前常见目标是：

```text
D:\steam\steamapps\common\Slay the Spire 2
```

如果你的游戏不在默认位置，推荐显式传参：

```powershell
dotnet build YukiMod.csproj -v:minimal -p:Sts2Path108="D:\steam\steamapps\common\Slay the Spire 2"
```

## 手动导出兜底

通常不需要手动导出。如果 `dotnet build` 编译成功但没有更新 `.pck`，可以在仓库根目录手动跑：

```powershell
& "E:\SOFT\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --headless --export-pack "BasicExport" "D:/steam/steamapps/common/Slay the Spire 2/mods/YukiMod/YukiMod.pck"
```

如果你的 Godot 或游戏路径不同，替换命令中的两个路径。

如果遇到 Godot / MSBuild 文件锁，先执行：

```powershell
dotnet build-server shutdown
```

然后重新跑 `dotnet build` 或手动导出。

## export_presets.cfg 控制哪些资源进 pck

Godot 导出使用 `export_presets.cfg` 中的 `BasicExport`。

当前配置：

```text
export_filter="all_resources"
include_filter="YukiMod/images/**/*.png,YukiMod/scenes/**/*.tscn,YukiMod/sound/**/*.mp3,YukiMod/spine/**/*.atlas,YukiMod/spine/**/*.skel,YukiMod/spine/**/*.json,YukiMod/ArtWorks/**/*.tscn,YukiMod/ArtWorks/**/*.tres,YukiMod/ArtWorks/**/*.atlas,YukiMod/ArtWorks/**/*.skel,YukiMod/ArtWorks/**/*.json,YukiMod/ArtWorks/**/*.png,YukiMod/ArtWorks/**/*.mp3,YukiMod/ArtWorks/**/*.wav,YukiMod/ArtWorks/**/*.ogg,YukiMod/ArtWorks/**/*.ogg.import"
exclude_filter="ModTemplate.json,*.cs,*.cs.uid,*.csproj,*.sln,AGENTS.md,README.md,docs/*,tmp/*,tmpmods/*"
```

实际含义：

- `YukiMod/images/**/*.png` 会把卡图、power 图、遗物图等图片导入 pck。
- `YukiMod/localization/**/*.json` 属于 Godot 资源扫描下的项目资源，也会随项目资源一起进入 pck。
- `docs/*`、`tmp/*`、`tmpmods/*` 不会进入 pck。
- `.cs`、`.csproj`、`.sln` 不会进入 pck；代码逻辑走 `YukiMod.dll`。

如果新增了新的资源类型或新目录，例如 `.wav` 以外的新音频格式、新的资源根目录，需要检查 `include_filter` 是否覆盖。

## 资源导入文件

新增图片后，第一次导出时 Godot 会生成对应的 `.import` 文件和 `.godot/imported/...` 缓存。

例如新增：

```text
YukiMod/images/powers/xue_ying_power.png
```

导出后通常会出现：

```text
YukiMod/images/powers/xue_ying_power.png.import
```

协同提交资源时，建议把新增的 `.png` 和对应 `.png.import` 一起提交。否则其他机器第一次导出时也会重新生成 `.import`，容易产生额外变更。

## Power 图片命名规则

能力图标由 `YukiModCode/Powers/YukiModPower.cs` 自动推导：

```csharp
public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePathOrDefault();
```

也就是说，power 的本地化 / 注册 ID 去掉 `YUKIMOD_` 前缀后，转小写并加 `.png`，就是期望的图片名。

示例：

| Power 类 | 期望资源名 |
| --- | --- |
| `XueYingPower` | `xue_ying_power.png` |
| `NuTaoZhanPower` | `nu_tao_zhan_power.png` |
| `HongChenPower` | `hong_chen_power.png` |
| `NextBlackCloudCardReplayPower` | `next_black_cloud_card_replay_power.png` |

如果文件名写错，代码不会报错，而是回退到：

```text
YukiMod/images/powers/power.png
```

所以新增 power 图片时要特别检查拼写。

## 版本号修改

版本号只改根目录：

```text
YukiMod.json
```

例如：

```json
"version": "v0.0.9"
```

不要改 `tmpmods/YukiMod/YukiMod.json`。那个目录是临时或旧副本，不是当前构建的权威来源。

构建后可以检查游戏目录里的清单：

```powershell
Get-Content "D:\steam\steamapps\common\Slay the Spire 2\mods\YukiMod\YukiMod.json"
```

确认里面的 `"version"` 已经更新。

## 如何确认导出成功

构建结束后检查：

```powershell
Get-Item "D:\steam\steamapps\common\Slay the Spire 2\mods\YukiMod\YukiMod.pck" |
  Select-Object FullName,Length,LastWriteTime
```

重点看：

- `LastWriteTime` 是否是刚刚构建的时间。
- `Length` 是否非 0。
- `YukiMod.json` 是否已复制到同一目录，并且版本号正确。
- `YukiMod.dll` 是否已复制到同一目录。

## 常见问题

### 构建找不到游戏数据目录

报错类似：

```text
Slay the Spire 2 108 data not found at path ...
```

处理方式：

```powershell
dotnet build YukiMod.csproj -v:minimal -p:Sts2Path108="你的 Slay the Spire 2 安装目录"
```

目录里必须能找到：

```text
data_sts2_windows_x86_64/sts2.dll
```

### 构建找不到 Godot

报错类似：

```text
Godot not found at path ...
```

处理方式：

- 安装或复制 Godot `4.5.1 stable mono`。
- 修改 `YukiMod.csproj` 中的 `GodotPath`。
- 或临时传参：

```powershell
dotnet build YukiMod.csproj -v:minimal -p:GodotPath="E:\SOFT\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
```

### 编译成功但 pck 没更新

先看 build 输出里有没有：

```text
Exporting Godot .pck to mods folder on Build
```

如果没有，通常是 `GodotPath` 未配置或不存在。

如果有但失败，尝试：

```powershell
dotnet build-server shutdown
dotnet build YukiMod.csproj -v:minimal
```

仍失败时，用上面的手动导出命令兜底。

### 新图片进不去游戏

检查四件事：

1. 图片是否在 `YukiMod/images/**` 或 `YukiMod/ArtWorks/**` 这类被 `include_filter` 覆盖的目录。
2. 是否跑过 `dotnet build` 或 Godot 手动导出。
3. 是否生成了 `.png.import`。
4. 对 power 图片，文件名是否等于 power ID 推导出的名字。

### 不要把 pck 当源码提交

`YukiMod.pck` 是构建产物，应该通过 `dotnet build` 在本机重新生成。协作时提交源码、资源、`.import`、本地化和清单即可，除非发布流程明确要求附带构建产物。

## 推荐交付检查清单

每次准备给别人试用前，至少执行：

```powershell
dotnet build YukiMod.csproj -v:minimal
Get-Item "D:\steam\steamapps\common\Slay the Spire 2\mods\YukiMod\YukiMod.pck" |
  Select-Object FullName,Length,LastWriteTime
Get-Content "D:\steam\steamapps\common\Slay the Spire 2\mods\YukiMod\YukiMod.json"
```

确认：

- build 没有错误。
- `YukiMod.pck` 时间戳已更新。
- `YukiMod.json` 版本号正确。
- 新增资源对应的 `.import` 文件已生成并准备提交。
