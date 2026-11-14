using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using YukiMod.YukiModCode.Infrastructure;
using YukiMod.YukiModCode.Migration;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Mechanics.Settings;
using YukiMod.YukiModCode.Mechanics.Vfx;
using YukiMod.YukiModCode.Monsters.Gloomy;
using YukiMod.YukiModCode.Telemetry;

namespace YukiMod;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "YukiMod";
    private static bool _characterEffectsPreloaded;

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = typeof(MainFile).Assembly;
        YukiRitsuMigration.Initialize();
        YukiTelemetryBootstrap.Initialize();
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        GloomyEncounterSharedSettings.RegisterProvider(ModId);
        GloomyMonsterVfx.Prewarm();

        Harmony harmony = new(ModId);
        harmony.PatchAll();
        ModelIdDeduplicator.DeduplicateForMod("YUKIMOD_");
    }

    internal static void PreloadCharacterEffects()
    {
        if (_characterEffectsPreloaded || !YukiModSharedSettings.CombatEffectsEnabled)
            return;
        YukiUgPresentation.Preload();
        YukiUxPresentation.Preload();
        string[] paths = YukiMeleeTeleportAttackPatch.GetPreloadScenePaths().Distinct(StringComparer.Ordinal).ToArray();
        ChaosVfxPrewarmReport prewarm = ChaosOneShotVfx.Prewarm(paths) + ChaosSpineVfxInstance.Prewarm(paths);
        Logger.Info($"[{ModId}] Character VFX prewarm summary. loaded={prewarm.Loaded}/{prewarm.Requested}, failed={prewarm.Failed}");
        _characterEffectsPreloaded = true;
    }
}
