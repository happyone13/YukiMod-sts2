using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using YukiMod.YukiModCode.Infrastructure;
using YukiMod.YukiModCode.Migration;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Mechanics.Vfx;
using YukiMod.YukiModCode.Telemetry;

namespace YukiMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YukiMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = typeof(MainFile).Assembly;
        YukiRitsuMigration.Initialize();
        YukiTelemetryBootstrap.Initialize();
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        ChaosOneShotVfx.Prewarm(YukiMeleeTeleportAttackPatch.GetPreloadScenePaths());
        ChaosSpineVfxInstance.Prewarm(YukiMeleeTeleportAttackPatch.GetPreloadScenePaths());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
        ModelIdDeduplicator.DeduplicateForMod("YUKIMOD_");
    }
}
