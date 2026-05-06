using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;
using System.Collections.Generic;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch]
public static class YukiCharacterAnimationFallbackPatch
{
    private static readonly string[] MerchantFallbacks = ["stop", "camping", "b_idle", "idle"];
    private static readonly string[] RestFallbacks = ["camping", "b_idle", "idle"];
    private static readonly HashSet<string> RestSiteActLoops = new(System.StringComparer.Ordinal)
    {
        "overgrowth_loop",
        "hive_loop",
        "glory_loop"
    };
    private static readonly HashSet<ulong> RegisteredRestSiteAnimationStates = [];

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    public static bool MerchantReadyPrefix(NMerchantCharacter __instance)
    {
        if (!IsYukiScene(__instance))
            return true;

        return !TryPlayFirstAvailableOnFirstChild(__instance, MerchantFallbacks, loop: true);
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
    [HarmonyPrefix]
    public static void RestSiteReadyPrefix(NRestSiteCharacter __instance)
    {
        if (!IsYukiScene(__instance))
            return;

        RegisteredRestSiteAnimationStates.Clear();
        RegisterRestSiteAnimationStates(__instance);
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
    [HarmonyPostfix]
    public static void RestSiteReadyPostfix(NRestSiteCharacter __instance)
    {
        if (!IsYukiScene(__instance))
            return;

        foreach (Node child in __instance.GetChildren())
        {
            if (child is Node2D node2D)
                TryPlayFirstAvailable(node2D, RestFallbacks, loop: true);
        }
    }

#if !STS2_103
    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._ExitTree))]
    [HarmonyPrefix]
    public static void RestSiteExitTreePrefix(NRestSiteCharacter __instance)
    {
        if (!IsYukiScene(__instance))
            return;

        UnregisterRestSiteAnimationStates(__instance);
    }
#endif

    [HarmonyPatch(typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
    [HarmonyPrefix]
    public static void RestSiteSetAnimationPrefix(MegaAnimationState __instance, ref string __0)
    {
        if (!RestSiteActLoops.Contains(__0))
            return;

        ulong instanceId = __instance.BoundObject.GetInstanceId();
        if (RegisteredRestSiteAnimationStates.Contains(instanceId))
            __0 = "camping";
    }

    private static bool IsYukiScene(Node node)
    {
        string path = node.SceneFilePath ?? string.Empty;
        return path.Contains("YukiMod/scenes/", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void RegisterRestSiteAnimationStates(NRestSiteCharacter restSiteCharacter)
    {
        foreach (Node child in restSiteCharacter.GetChildren())
        {
            if (child is not Node2D node2D)
                continue;

            try
            {
                MegaAnimationState animationState = new MegaSprite(node2D).GetAnimationState();
                RegisteredRestSiteAnimationStates.Add(animationState.BoundObject.GetInstanceId());
            }
            catch
            {
                // Ignore non-Spine children.
            }
        }
    }

    private static void UnregisterRestSiteAnimationStates(NRestSiteCharacter restSiteCharacter)
    {
        foreach (Node child in restSiteCharacter.GetChildren())
        {
            if (child is not Node2D node2D)
                continue;

            try
            {
                MegaAnimationState animationState = new MegaSprite(node2D).GetAnimationState();
                RegisteredRestSiteAnimationStates.Remove(animationState.BoundObject.GetInstanceId());
            }
            catch
            {
                // Ignore non-Spine children.
            }
        }
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

            MegaTrackEntry? entry = sprite.GetAnimationState().SetAnimation(anim, loop);
            if (loop && entry != null)
                entry.SetTrackTime(entry.GetAnimationEnd() * Rng.Chaotic.NextFloat());

            return true;
        }

        return false;
    }
}
