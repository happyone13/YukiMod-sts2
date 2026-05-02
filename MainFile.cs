using Godot;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using YukiMod.YukiModCode.Config;

namespace YukiMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YukiMod";

    public static void Initialize()
    {
        ModConfigRegistry.Register(ModId, new YukiModConfig());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
