# 阴郁兽群遭遇兼容约定

YukiMod 自带阴郁首领、阴郁野兽、遭遇、能力、本地化、Spine、VFX、音频、场景资源和逃跑牌，不依赖 Fei 或 MeiLinMod 的程序集与 PCK，可以独立工作。

三个提供者通过 AppDomain 共享状态动态选举，优先级固定为 `Fei > YukiMod > MeiLinMod`。各 Mod 注册并持有自己的模型、公开 ID、遭遇和逃跑牌，只在各自 `GloomyPackEncounter.IsValidForAct` 中过滤非当选提供者。加载顺序不会改变结果，也不会跨程序集复用类型或公开 ID。

- 共享设置文件：`user://chaosmod/gloomy_encounter_settings.json`
- JSON：`{ "Enabled": true }`
- 开关键：`CHAOSMOD_GLOOMY_ENCOUNTER_ENABLED`
- 提供者键：`CHAOSMOD_GLOOMY_PROVIDER_<ModId>`
- 默认：开启

YukiMod 的 RitsuLib 设置页在“游戏内容”区提供“一位旧识”开关。关闭后，所有遵守协议的提供者都不会把该遭遇加入房间池；重新开启后恢复优先级最高的已加载提供者。

## 逃跑牌

YukiMod 的遭遇在开局抽牌前给每位玩家一张本地 `YUKIMOD_GLOOMY_ESCAPE`：0 费、无色技能、保留、消耗。该牌不触发友纪专属动画、灵感或卡框逻辑，因此其他角色和多人玩家也可以安全使用。

打出后只对当前 YukiMod `GloomyPackEncounter` 生效：记录玩家选择逃跑，调用原版 `CreatureCmd.Escape` 依次移除敌人，不移除任何玩家生物，并禁止奖励和金币。逃跑状态通过遭遇自定义存档字段 `player_escaped` 保存。

共同加载时，各提供者的发牌补丁只识别自己拥有的具体遭遇类型。由于同一时间只有当选提供者的遭遇可以进入房间池，因此不会重复发牌；卸载其他 Mod 后，YukiMod 的卡牌、遭遇和资源仍完整可用。
