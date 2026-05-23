using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class YukiCharacterSelectAnimLoopPatch
{
    private const string BgContainerNodeName = "AnimatedBg";
    private const string LoopAnimName = "animation";
    private const string YukiCharacterId = YukiModInfo.CharacterId;

    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
    {
        if (charSelectButton.IsLocked || !IsYukiCharacter(characterModel))
        {
            return;
        }

        Control? bgContainer = __instance.GetNodeOrNull<Control>(BgContainerNodeName);
        if (bgContainer == null)
        {
            Log.Warn($"[{YukiModInfo.ModId}] CharacterSelect bg container missing: {BgContainerNodeName}");
            return;
        }

        int found = 0;
        int started = 0;
        int missingAnim = 0;
        int failed = 0;

        foreach (Node child in bgContainer.GetChildren())
        {
            ForceLoopAnimationOnAllSpineSprites(child, ref found, ref started, ref missingAnim, ref failed);
        }

        Log.Info($"[{YukiModInfo.ModId}] CharacterSelect bg anim loop: found={found} started={started} missing={missingAnim} failed={failed}");
    }

    private static bool IsYukiCharacter(CharacterModel? characterModel)
    {
        if (characterModel == null)
            return false;

        try
        {
            return string.Equals(characterModel.Id.Entry, YukiCharacterId, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static void ForceLoopAnimationOnAllSpineSprites(Node root, ref int found, ref int started, ref int missingAnim, ref int failed)
    {
        Stack<Node> stack = new Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Node node = stack.Pop();
            foreach (Node child in node.GetChildren())
            {
                stack.Push(child);
            }

            if (!string.Equals(node.GetClass(), "SpineSprite", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                MegaSprite sprite = new MegaSprite(node);
                found++;
                if (sprite.HasAnimation(LoopAnimName))
                {
                    sprite.GetAnimationState().SetAnimation(LoopAnimName, loop: true);
                    started++;
                }
                else
                {
                    missingAnim++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                Log.Warn($"[{YukiModInfo.ModId}] CharacterSelect bg anim loop failed: {ex.Message}");
            }
        }
    }
}


