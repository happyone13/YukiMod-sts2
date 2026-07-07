using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch(typeof(SpineAnimationAccess), nameof(SpineAnimationAccess.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
public static class YukiGameOverAnimationFallbackPatch
{
    private static readonly FieldInfo SpriteField = AccessTools.Field(typeof(SpineAnimationAccess), "_sprite");
    private static bool _redirectInProgress;

    [HarmonyPrefix]
    public static bool SetAnimationPrefix(SpineAnimationAccess __instance, string name, bool loop, int track)
    {
        if (_redirectInProgress)
            return true;

        if (!string.Equals(name, "die", StringComparison.Ordinal))
            return true;

        MegaSprite? sprite = SpriteField.GetValue(__instance) as MegaSprite;
        if (sprite == null || !IsYukiVisual(sprite))
            return true;

        try
        {
            _redirectInProgress = true;
            MegaAnimationState animationState = sprite.GetAnimationState();
            animationState.SetAnimation("death_ready", false, track);
            animationState.AddAnimation("death", 0f, loop: false, trackId: track);
            return false;
        }
        finally
        {
            _redirectInProgress = false;
        }
    }

    private static bool IsYukiVisual(MegaSprite sprite)
    {
        if (sprite.BoundObject is not Node node)
            return false;

        for (Node? current = node; current != null; current = current.GetParent())
        {
            string scenePath = current.SceneFilePath ?? string.Empty;
            if (scenePath.Contains("YukiMod/scenes/yuki_character", StringComparison.OrdinalIgnoreCase))
                return true;

            if (scenePath.Contains("YukiMod/scenes/merchant/characters/yukimod_merchant", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
