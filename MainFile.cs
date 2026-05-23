using Godot;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using YukiMod.YukiModCode.Config;
using YukiMod.YukiModCode.Mechanics.Vfx;
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
        ChaosOneShotVfx.Prewarm([
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_move_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_move_f.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_arrive_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_arrive_f.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_arrive_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_arrive_f.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_move.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play1_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play1_f.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play2_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play2_f.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u1_buff_play_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u1_buff_play_f.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u3_attack_play_b.tscn",
            "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u3_attack_play_f.tscn"
        ]);

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
