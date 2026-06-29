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
	private const string ToggleButtonConnectedMeta = "YukiBgToggleButtonConnected";
	private static bool _forceLoopWarned;
	private static bool _toggleButtonCreatedLogged;
	private static bool _toggleButtonVisibleLogged;
	private static bool _bgContainerMissingWarned;
	private static bool _bgPairInjectedLogged;

	[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
	[HarmonyPostfix]
	public static void ReadyPostfix(NCharacterSelectScreen __instance)
	{
		EnsureToggleButton(__instance);
	}

	[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	public static void SelectCharacterPostfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
	{
		Button? btn = EnsureToggleButton(__instance);
		if (btn == null)
		{
			return;
		}

		btn.Visible = !charSelectButton.IsLocked && IsYukiCharacter(characterModel);
		if (btn.Visible)
		{
			btn.Disabled = false;
			btn.Modulate = Colors.White;
			btn.ZIndex = 4096;
			UpdateToggleButtonPosition(btn, charSelectButton);
			MoveToFront(btn);
			LogToggleButtonVisibleOnce(btn);
			EnsureBgPairInjected(__instance);
			ApplyBgVisibility(__instance);
			Callable.From(() => ApplyDeferredIfStillYuki(__instance, charSelectButton, characterModel)).CallDeferred();
		}
	}

	private static Button? EnsureToggleButton(NCharacterSelectScreen screen)
	{
		Button? btn = FindToggleButton(screen);
		if (btn == null)
		{
			btn = new Button
			{
				Name = ToggleButtonNodeName,
				Text = "切换背景",
				Visible = false,
				TopLevel = true,
				MouseFilter = Control.MouseFilterEnum.Stop,
				FocusMode = Control.FocusModeEnum.None,
				ZIndex = 4096,
			};

			screen.AddChildSafely(btn);
			LogToggleButtonCreatedOnce(screen);
		}
		else if (btn.GetParent() != screen)
		{
			Node? parent = btn.GetParent();
			parent?.RemoveChildSafely(btn);
			screen.AddChildSafely(btn);
		}

		btn.TopLevel = true;
		btn.MouseFilter = Control.MouseFilterEnum.Stop;
		btn.FocusMode = Control.FocusModeEnum.None;
		btn.ZIndex = 4096;
		btn.Size = new Vector2(100, 28);

		if (!btn.HasMeta(ToggleButtonConnectedMeta))
		{
			void OnPressed()
			{
				try
				{
					YukiCharacterSelectBgToggleState.UseAltBg = !YukiCharacterSelectBgToggleState.UseAltBg;
					EnsureBgPairInjected(screen);
					ApplyBgVisibility(screen);
				}
				catch (Exception ex)
				{
					Log.Warn($"[{YukiModInfo.ModId}] Toggle Yuki character select bg failed: {ex.Message}");
				}
			}

			btn.Connect(BaseButton.SignalName.Pressed, Callable.From(new Action(OnPressed)));
			btn.SetMeta(ToggleButtonConnectedMeta, true);
		}

		return btn;
	}

	private static Button? FindToggleButton(Node root)
	{
		var stack = new System.Collections.Generic.Stack<Node>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			Node node = stack.Pop();
			if (node is Button button && string.Equals(button.Name.ToString(), ToggleButtonNodeName, StringComparison.Ordinal))
			{
				return button;
			}

			foreach (Node child in node.GetChildren())
			{
				stack.Push(child);
			}
		}

		return null;
	}

	private static void ApplyDeferredIfStillYuki(NCharacterSelectScreen screen, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
	{
		if (!GodotObject.IsInstanceValid(screen) || !GodotObject.IsInstanceValid(charSelectButton))
		{
			return;
		}

		Button? btn = EnsureToggleButton(screen);
		if (btn == null)
		{
			return;
		}

		bool visible = !charSelectButton.IsLocked && IsYukiCharacter(characterModel);
		btn.Visible = visible;
		if (!visible)
		{
			return;
		}

		btn.Disabled = false;
		btn.ZIndex = 4096;
		UpdateToggleButtonPosition(btn, charSelectButton);
		MoveToFront(btn);
		LogToggleButtonVisibleOnce(btn);
		EnsureBgPairInjected(screen);
		ApplyBgVisibility(screen);
	}

	private static void LogToggleButtonCreatedOnce(NCharacterSelectScreen screen)
	{
		if (_toggleButtonCreatedLogged)
		{
			return;
		}

		_toggleButtonCreatedLogged = true;
		Log.Info($"[{YukiModInfo.ModId}] CharacterSelect bg toggle button created under {screen.GetPath()}.");
	}

	private static void LogToggleButtonVisibleOnce(Button btn)
	{
		if (_toggleButtonVisibleLogged)
		{
			return;
		}

		_toggleButtonVisibleLogged = true;
		Log.Info($"[{YukiModInfo.ModId}] CharacterSelect bg toggle visible at {btn.GlobalPosition} size={btn.Size}.");
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

		Rect2 viewportRect = selectedButton.GetViewportRect();
		float x = Mathf.Clamp(rect.Position.X, 8f, Mathf.Max(8f, viewportRect.Size.X - size.X - 8f));
		float y = rect.Position.Y - size.Y + 6;
		if (y < 8f)
		{
			y = rect.Position.Y + rect.Size.Y - size.Y - 6f;
		}

		y = Mathf.Clamp(y, 8f, Mathf.Max(8f, viewportRect.Size.Y - size.Y - 8f));
		btn.GlobalPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
	}

	private static void MoveToFront(Node node)
	{
		Node? parent = node.GetParent();
		if (parent == null)
		{
			return;
		}

		parent.MoveChild(node, parent.GetChildCount() - 1);
	}

	private static void EnsureBgPairInjected(NCharacterSelectScreen screen)
	{
		Control? bgContainer = screen.GetNodeOrNull<Control>(BgContainerNodeName);
		if (bgContainer == null)
		{
			LogBgContainerMissingOnce();
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

		LogBgPairInjectedOnce(bgContainer);

		StartForceLoopRetry(screen, bg1, maxFrames: 90);
	}

	private static void LogBgContainerMissingOnce()
	{
		if (_bgContainerMissingWarned)
		{
			return;
		}

		_bgContainerMissingWarned = true;
		Log.Warn($"[{YukiModInfo.ModId}] CharacterSelect bg container missing: {BgContainerNodeName}");
	}

	private static void LogBgPairInjectedOnce(Node bgContainer)
	{
		if (_bgPairInjectedLogged)
		{
			return;
		}

		_bgPairInjectedLogged = true;
		Log.Info($"[{YukiModInfo.ModId}] CharacterSelect bg pair injected into {bgContainer.GetPath()}.");
	}

	private static void ApplyBgVisibility(NCharacterSelectScreen screen)
	{
		Control? bgContainer = screen.GetNodeOrNull<Control>(BgContainerNodeName);
		if (bgContainer == null)
		{
			LogBgContainerMissingOnce();
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
