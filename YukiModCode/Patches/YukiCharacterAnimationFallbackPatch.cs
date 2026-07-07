using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch]
public static class YukiCharacterAnimationFallbackPatch
{
    private const string RestSiteScenePath = "res://YukiMod/scenes/yuki_character_camp.tscn";
    private const string MerchantScenePath = "res://YukiMod/scenes/merchant/characters/yukimod_merchant.tscn";

    private static readonly string[] MerchantFallbacks = ["relaxed_loop", "stop", "camping", "b_idle", "idle_loop", "idle"];
    private static readonly string[] RestFallbacks = ["overgrowth_loop", "hive_loop", "glory_loop", "camping", "b_idle", "idle_loop", "idle"];

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    public static bool MerchantReadyPrefix(NMerchantCharacter __instance)
    {
        if (!IsYukiScene(__instance))
            return true;

        return !TryPlayFirstAvailableOnFirstChild(__instance, MerchantFallbacks, loop: true);
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
    [HarmonyPostfix]
    public static void RestSiteReadyPostfix(NRestSiteCharacter __instance)
    {
        if (!IsYukiScene(__instance))
            return;

        YukiAudioService.TryPlayRestSiteVoice();

        foreach (Node child in __instance.GetChildren())
        {
            if (child is Node2D node2D)
                TryPlayFirstAvailable(node2D, RestFallbacks, loop: true);
        }
    }

    private static bool IsYukiScene(Node node)
    {
        string path = node.SceneFilePath ?? string.Empty;
        return string.Equals(path, RestSiteScenePath, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, MerchantScenePath, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryPlayFirstAvailableOnFirstChild(Node parent, string[] candidates, bool loop)
    {
        Node? child = parent.GetChildCount() > 0 ? parent.GetChild(0) : null;
        return child is Node2D node2D && TryPlayFirstAvailable(node2D, candidates, loop);
    }

    private static bool TryPlayFirstAvailable(Node2D node, string[] candidates, bool loop)
    {
        MegaSprite sprite;
        try
        {
            sprite = new MegaSprite(node);
        }
        catch
        {
            return false;
        }

        foreach (string anim in candidates)
        {
            bool hasAnimation;
            try
            {
                hasAnimation = sprite.HasAnimation(anim);
            }
            catch
            {
                return false;
            }

            if (!hasAnimation)
                continue;

            sprite.GetAnimationState().SetAnimation(anim, loop);
            return true;
        }

        return false;
    }
}
