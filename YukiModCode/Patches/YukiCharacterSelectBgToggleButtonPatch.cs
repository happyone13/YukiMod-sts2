using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch]
public static class YukiCharacterSelectBgToggleButtonPatch
{
	private const string BgContainerNodeName = "AnimatedBg";
	private const string CharSelectButtonsNodeName = "CharSelectButtons";
	private const string ToggleButtonNodeName = "YukiBgToggleButton";
	private const string YukiCharacterId = YukiModInfo.CharacterId;
	private const string LoopAnimName = "animation";

	[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
	[HarmonyPostfix]
	public static void ReadyPostfix(NCharacterSelectScreen __instance)
	{
		Control? charSelectButtons = __instance.GetNodeOrNull<Control>(CharSelectButtonsNodeName);
		if (charSelectButtons == null)
		{
			Log.Warn($"[{YukiModInfo.ModId}] CharacterSelect missing node: {CharSelectButtonsNodeName}");
			return;
		}

		if (charSelectButtons.GetNodeOrNull<Button>(ToggleButtonNodeName) != null)
		{
			return;
		}

		Button btn = new Button
		{
			Name = ToggleButtonNodeName,
			Text = "切换背景",
			Visible = false,
		};

		charSelectButtons.AddChildSafely(btn);

		btn.TopLevel = true;
		btn.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		btn.Size = new Vector2(100, 28);

		btn.Pressed += () =>
		{
			try
			{
				YukiCharacterSelectBgToggleState.UseAltBg = !YukiCharacterSelectBgToggleState.UseAltBg;
				ReloadYukiBackground(__instance);
			}
			catch (Exception ex)
			{
				Log.Warn($"[{YukiModInfo.ModId}] Toggle Yuki character select bg failed: {ex.Message}");
			}
		};
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
	[HarmonyPostfix]
	public static void SelectCharacterPostfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
	{
		Control? charSelectButtons = __instance.GetNodeOrNull<Control>(CharSelectButtonsNodeName);
		if (charSelectButtons == null)
		{
			return;
		}

		Button? btn = charSelectButtons.GetNodeOrNull<Button>(ToggleButtonNodeName);
		if (btn == null)
		{
			return;
		}

		btn.Visible = !charSelectButton.IsLocked && IsYukiCharacter(characterModel);
		if (btn.Visible)
		{
			UpdateToggleButtonPosition(btn, charSelectButton);
		}
	}

	private static bool IsYukiCharacter(CharacterModel? characterModel)
	{
		if (characterModel == null)
		{
			return false;
		}

		try
		{
			return string.Equals(characterModel.Id.Entry, YukiCharacterId, StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static void UpdateToggleButtonPosition(Button btn, Control selectedButton)
	{
		Rect2 rect = selectedButton.GetGlobalRect();

		float width = Mathf.Max(60, rect.Size.X);
		float height = 28;
		Vector2 size = new Vector2(width, height);
		btn.Size = size;

		float x = rect.Position.X;
		float y = rect.Position.Y - size.Y + 6;
		btn.GlobalPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
	}

	private static void ReloadYukiBackground(NCharacterSelectScreen screen)
	{
		Control? bgContainer = screen.GetNodeOrNull<Control>(BgContainerNodeName);
		if (bgContainer == null)
		{
			return;
		}

		foreach (Node child in bgContainer.GetChildren())
		{
			bgContainer.RemoveChildSafely(child);
			child.QueueFreeSafely();
		}

		string bgPath = "res://YukiMod/ArtWorks/scenes/screens/char_select/char_select_bg_chaos_yuki" +
			(YukiCharacterSelectBgToggleState.UseAltBg ? "_2" : "") +
			".tscn";

		PackedScene scene = PreloadManager.Cache.GetScene(bgPath);
		Control bg = scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		bg.Name = "chaos_yuki_character_bg";
		bgContainer.AddChildSafely(bg);

		if (!YukiCharacterSelectBgToggleState.UseAltBg)
		{
			ForceLoopAnimationOnAllSpineSprites(bg);
		}
	}

	private static void ForceLoopAnimationOnAllSpineSprites(Node root)
	{
		var stack = new System.Collections.Generic.Stack<Node>();
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
				if (sprite.HasAnimation(LoopAnimName))
				{
					sprite.GetAnimationState().SetAnimation(LoopAnimName, loop: true);
				}
			}
			catch (Exception ex)
			{
				Log.Warn($"[{YukiModInfo.ModId}] Force loop bg anim failed: {ex.Message}");
			}
		}
	}
}

