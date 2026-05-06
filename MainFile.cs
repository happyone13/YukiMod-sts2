using Godot;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using YukiMod.YukiModCode.Config;
using YukiMod.YukiModCode.Patches;

namespace YukiMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YukiMod";

    public static void Initialize()
    {
        ModConfigRegistry.Register(ModId, new YukiModConfig());
        YukiCardSpinePortraitPatch.PreloadDynamicPortraitScenes();

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
