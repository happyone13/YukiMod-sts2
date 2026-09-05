using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Settings;
using YukiMod.YukiModCode.Encounters;
using YukiMod.YukiModCode.Mechanics.CardHoldOverlay;
using YukiMod.YukiModCode.Mechanics.Settings;

namespace YukiMod.YukiModCode.Migration;

internal static class YukiRitsuMigration
{
    private const string SettingsPageId = "yuki-settings";

    public static void Initialize()
    {
        var assembly = typeof(MainFile).Assembly;
        MainFile.Logger.Info("[YukiRitsuMigration] Initializing RitsuLib integration.");
        ModTypeDiscoveryHub.RegisterModAssembly(MainFile.ModId, assembly);
        YukiRitsuContentRegistration.Register(assembly);
        RegisterSettingsPage();
        RegisterOptionalPatchers();
        MainFile.Logger.Info("[YukiRitsuMigration] RitsuLib integration initialized.");
    }

    private static void RegisterOptionalPatchers()
    {
        var contentPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-content", "optional content");
        contentPatcher.RegisterPatch<GloomyEscapeCardBeforeCombatStartPatch>();
        contentPatcher.PatchAll();

        var presentationPatcher = RitsuLibFramework.CreatePatcher(
            MainFile.ModId,
            "optional-presentation",
            "optional presentation hooks");
        presentationPatcher.RegisterPatch<YukiBattleReadyBeforeCombatStartPatch>();
        presentationPatcher.RegisterPatch<YukiBattleReadyAfterCombatVictoryPatch>();
        presentationPatcher.RegisterPatch<YukiBattleReadyAfterDeathPatch>();
        presentationPatcher.RegisterPatch<YukiBattleReadyBeforeCardPlayedPrefixPatch>();
        presentationPatcher.RegisterPatch<YukiBattleReadyBeforeCardPlayedPostfixPatch>();
        presentationPatcher.PatchAll();
        MainFile.Logger.Info("[YukiRitsuMigration] Optional content patches registered.");
    }

    private static void RegisterSettingsPage()
    {
        RitsuLibFramework.RegisterModSettings(
            MainFile.ModId,
            page =>
            {
                page
                    .WithTitle(SettingsText("YUKIMOD_RITSU_PAGE.title", "Yuki Settings"))
                    .WithModDisplayName(ModSettingsText.Literal("YukiMod"))
                    .WithDescription(SettingsText(
                        "YUKIMOD_RITSU_PAGE.description",
                        "YukiMod settings registered through RitsuLib."));

                page.AddSection("visuals", section =>
                {
                    section.WithTitle(SettingsText("YUKIMOD_RITSU_SECTION_VISUALS.title", "Visuals"));
                    section.AddToggle(
                            "battle_ready_overlay",
                            SettingsText("YUKIMOD_RITSU_BATTLE_READY_OVERLAY.title", "Back-Facing Portrait"),
                            BoolBinding(
                                "battle_ready_overlay",
                                () => YukiModSharedSettings.BattleReadyOverlayEnabled,
                                value =>
                                {
                                    YukiModSharedSettings.SetBattleReadyOverlayEnabled(value, persist: true);
                                    if (!value)
                                    {
                                        YukiBattleReadyOverlay.NotifyCombatEnded();
                                    }
                                }))
                        .AddToggle(
                            "combat_effects",
                            SettingsText("YUKIMOD_RITSU_COMBAT_EFFECTS.title", "Combat Effects"),
                            BoolBinding(
                                "combat_effects",
                                () => YukiModSharedSettings.CombatEffectsEnabled,
                                value => YukiModSharedSettings.SetCombatEffectsEnabled(value, persist: true)))
                        .AddToggle(
                            "ultimate_cinematics",
                            SettingsText("YUKIMOD_RITSU_ULTIMATE_CINEMATICS.title", "UG / UX Cinematics"),
                            BoolBinding(
                                "ultimate_cinematics",
                                () => YukiModSharedSettings.UltimateCinematicsEnabled,
                                value => YukiModSharedSettings.SetUltimateCinematicsEnabled(value, persist: true)))
                        .AddToggle(
                            "dynamic_card_portraits",
                            SettingsText("YUKIMOD_RITSU_DYNAMIC_CARD_PORTRAITS.title", "Dynamic Card Art"),
                            BoolBinding(
                                "dynamic_card_portraits",
                                () => YukiModSharedSettings.DynamicCardPortraitsEnabled,
                                value => YukiModSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true)));
                });

                page.AddSection("gameplay", section =>
                {
                    section.WithTitle(SettingsText("YUKIMOD_RITSU_SECTION_GAMEPLAY.title", "Gameplay"));
                    section.AddToggle(
                        "gloomy_encounter",
                        SettingsText("YUKIMOD_RITSU_GLOOMY_ENCOUNTER.title", "An Old Acquaintance"),
                        BoolBinding(
                            "gloomy_encounter",
                            () => GloomyEncounterSharedSettings.Enabled,
                            value => GloomyEncounterSharedSettings.SetEnabled(value, persist: true)),
                        SettingsText(
                            "YUKIMOD_RITSU_GLOOMY_ENCOUNTER.description",
                            "Enable this option and you may encounter an old acquaintance."));
                });
            },
            SettingsPageId);
    }

    private static ModSettingsText SettingsText(string key, string fallback)
    {
        return ModSettingsText.LocString("settings_ui", key, fallback);
    }

    private static IModSettingsValueBinding<bool> BoolBinding(
        string key,
        Func<bool> read,
        Action<bool> write)
    {
        return ModSettingsBindings.Callback(MainFile.ModId, key, read, write, SaveNoOp);
    }

    private static void SaveNoOp()
    {
        // The shared settings setters persist immediately to stay compatible with Yuki/Chaos.
    }
}
