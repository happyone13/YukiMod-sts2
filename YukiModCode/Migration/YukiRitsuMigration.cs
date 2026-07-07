using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
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
        MainFile.Logger.Info("[YukiRitsuMigration] RitsuLib integration initialized.");
    }

    private static void RegisterSettingsPage()
    {
        RitsuLibFramework.RegisterModSettings(
            MainFile.ModId,
            page =>
            {
                page
                    .WithTitle(ModSettingsText.Literal("Yuki Settings"))
                    .WithModDisplayName(ModSettingsText.Literal("YukiMod"))
                    .WithDescription(ModSettingsText.Literal("YukiMod settings registered through RitsuLib."));

                page.AddSection("visuals", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("Visuals"));
                    section.AddToggle(
                            "battle_ready_overlay",
                            ModSettingsText.Literal("Battle ready overlay"),
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
                            ModSettingsText.Literal("Combat effects"),
                            BoolBinding(
                                "combat_effects",
                                () => YukiModSharedSettings.CombatEffectsEnabled,
                                value => YukiModSharedSettings.SetCombatEffectsEnabled(value, persist: true)))
                        .AddToggle(
                            "dynamic_card_portraits",
                            ModSettingsText.Literal("Dynamic card portraits"),
                            BoolBinding(
                                "dynamic_card_portraits",
                                () => YukiModSharedSettings.DynamicCardPortraitsEnabled,
                                value => YukiModSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true)));
                });
            },
            SettingsPageId);
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
