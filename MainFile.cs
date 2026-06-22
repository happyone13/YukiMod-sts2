using Godot;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using YukiMod.YukiModCode.Config;
using YukiMod.YukiModCode.Infrastructure;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Mechanics.Vfx;

namespace YukiMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YukiMod";

    public static void Initialize()
    {
        ModConfigRegistry.Register(ModId, new YukiModConfig());
        ChaosOneShotVfx.Prewarm(YukiMeleeTeleportAttackPatch.GetPreloadScenePaths());
        ChaosSpineVfxInstance.Prewarm(YukiMeleeTeleportAttackPatch.GetPreloadScenePaths());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
        ModelIdDeduplicator.DeduplicateForMod("YUKIMOD-");
    }
}
