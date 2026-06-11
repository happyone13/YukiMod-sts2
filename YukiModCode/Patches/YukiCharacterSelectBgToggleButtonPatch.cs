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
	private const string Bg1NodeName = "YukiCharSelectBg1";
	private const string Bg2NodeName = "YukiCharSelectBg2";
	private const string Bg1Path = "res://YukiMod/ArtWorks/scenes/screens/char_select/char_select_bg_chaos_yuki.tscn";
	private const string Bg2Path = "res://YukiMod/ArtWorks/scenes/screens/char_select/char_select_bg_chaos_yuki_2.tscn";
	private const string LoopAnimName = "animation";
	private const string FallbackLoopAnimName = "idle";
	private static bool _forceLoopWarned;

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
				EnsureBgPairInjected(__instance);
				ApplyBgVisibility(__instance);
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
			EnsureBgPairInjected(__instance);
			ApplyBgVisibility(__instance);
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

	private static void EnsureBgPairInjected(NCharacterSelectScreen screen)
	{
		Control? bgContainer = screen.GetNodeOrNull<Control>(BgContainerNodeName);
		if (bgContainer == null)
		{
			return;
		}

		if (bgContainer.GetNodeOrNull<Control>(Bg1NodeName) != null &&
		    bgContainer.GetNodeOrNull<Control>(Bg2NodeName) != null)
		{
			return;
		}

		foreach (Node child in bgContainer.GetChildren())
		{
			bgContainer.RemoveChildSafely(child);
			child.QueueFreeSafely();
		}

		PackedScene bg1Scene = PreloadManager.Cache.GetScene(Bg1Path);
		Control bg1 = bg1Scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		bg1.Name = Bg1NodeName;
		bgContainer.AddChildSafely(bg1);

		PackedScene bg2Scene = PreloadManager.Cache.GetScene(Bg2Path);
		Control bg2 = bg2Scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		bg2.Name = Bg2NodeName;
		bgContainer.AddChildSafely(bg2);

		StartForceLoopRetry(screen, bg1, maxFrames: 90);
	}

	private static void ApplyBgVisibility(NCharacterSelectScreen screen)
	{
		Control? bgContainer = screen.GetNodeOrNull<Control>(BgContainerNodeName);
		if (bgContainer == null)
		{
			return;
		}

		Control? bg1 = bgContainer.GetNodeOrNull<Control>(Bg1NodeName);
		Control? bg2 = bgContainer.GetNodeOrNull<Control>(Bg2NodeName);
		if (bg1 == null || bg2 == null)
		{
			return;
		}

		bool useAlt = YukiCharacterSelectBgToggleState.UseAltBg;
		bg1.Visible = !useAlt;
		bg2.Visible = useAlt;

		if (!useAlt)
		{
			StartForceLoopRetry(screen, bg1, maxFrames: 90);
		}
	}

	private static int ForceLoopAnimationOnAllSpineSprites(Node root)
	{
		int success = 0;
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
					success++;
				}
				else if (sprite.HasAnimation(FallbackLoopAnimName))
				{
					sprite.GetAnimationState().SetAnimation(FallbackLoopAnimName, loop: true);
					success++;
				}
			}
			catch (Exception ex)
			{
				if (!_forceLoopWarned)
				{
					_forceLoopWarned = true;
					Log.Warn($"[{YukiModInfo.ModId}] Force loop bg anim failed: {ex.Message}");
				}
			}
		}

		return success;
	}

	private static async void StartForceLoopRetry(Node host, Node root, int maxFrames)
	{
		if (maxFrames <= 0)
		{
			return;
		}

		for (int i = 0; i < maxFrames; i++)
		{
			if (!GodotObject.IsInstanceValid(host) || !GodotObject.IsInstanceValid(root))
			{
				return;
			}

			SceneTree? tree = host.GetTree();
			if (tree == null)
			{
				return;
			}

			await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			if (!GodotObject.IsInstanceValid(root))
			{
				return;
			}

			if (ForceLoopAnimationOnAllSpineSprites(root) > 0)
			{
				return;
			}
		}
	}
}
