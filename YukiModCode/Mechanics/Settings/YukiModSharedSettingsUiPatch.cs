using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using BaseLib.Config;
using BaseLib.Config.UI;
using YukiMod.YukiModCode.Config;
using YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

namespace YukiMod.YukiModCode.Mechanics.Settings;

[HarmonyPatch(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready))]
public static class YukiModSharedSettingsUiPatch
{
	private const string ClipperPath = "ScrollContainer/Mask/Clipper";
	private const string SoundSettingsVBoxPath = ClipperPath + "/SoundSettings/VBoxContainer";
	private const string SettingsTabManagerPath = "SettingsTabManager";
	private const string ModTabScenePath = "res://scenes/screens/settings_tab.tscn";
	private const string SettingsSliderScenePath = "res://scenes/screens/settings_slider.tscn";
	private const string TemplateLabelPath = "SfxVolume/Label";
	private static readonly string TemplateLabelFullPath = SoundSettingsVBoxPath + "/" + TemplateLabelPath;

	private const string ModTabName = "XCskin_ModSettingsTab";
	private const string ModPanelName = "XCskin_ModSettingsPanel";

	private const string VoiceSectionName = "ChaosModVoiceVolume";
	private const string VoiceSliderName = "ChaosModVoiceSlider";
	private const string VoiceLineName = "Line_ChaosModVoice";
	private const string ScaleSectionName = "ChaosModBattleReadyScale";
	private const string ScaleSliderName = "ChaosModBattleReadyScaleSlider";
	private const string ScaleLineName = "Line_ChaosModBattleReadyScale";
	private const string OffsetYSectionName = "ChaosModBattleReadyOffsetY";
	private const string OffsetYSliderName = "ChaosModBattleReadyOffsetYSlider";
	private const string OffsetYLineName = "Line_ChaosModBattleReadyOffsetY";
	private const string OffsetXSectionName = "ChaosModBattleReadyOffsetX";
	private const string OffsetXSliderName = "ChaosModBattleReadyOffsetXSlider";
	private const string OffsetXLineName = "Line_ChaosModBattleReadyOffsetX";
	private const string ResetSectionName = "ChaosModBattleReadyReset";
	private const string ResetButtonName = "ChaosModBattleReadyResetButton";
	private const string ResetLineName = "Line_ChaosModBattleReadyReset";

	private const string CardVisualsLineName = "Line_YukiModCardVisuals";
	private const string DynamicPortraitsSectionName = "YukiModCardVisualsDynamicPortraits";
	private const string DynamicPortraitsRowName = "YukiModCardVisualsDynamicPortraitsRow";
	private const string DynamicPortraitsTickboxName = "YukiModCardVisualsDynamicPortraitsTickbox";

	private static int _injectLogOnce;
	private static int _ensureLogOnce;
	private static int _wireExistingOnce;

	[HarmonyPostfix]
	public static void Postfix(NSettingsScreen __instance)
	{
		TryInject(__instance, "_Ready");
	}

	private static void TryInject(NSettingsScreen screen, string source)
	{
		if (System.Threading.Interlocked.Exchange(ref _injectLogOnce, 1) == 0)
		{
			try
			{
				Log.Info("[YukiMod] Settings inject entered. source=" + source + " screen=" + screen.GetPath());
			}
			catch
			{
				Log.Info("[YukiMod] Settings inject entered. source=" + source);
			}
		}

		try
		{
			TryInjectInner(screen, source);
		}
		catch (Exception ex)
		{
			Log.Warn("[YukiMod] Settings inject failed (" + source + "): " + ex);
		}
	}

	private static void TryInjectInner(NSettingsScreen screen, string source)
	{
		if (!EnsureModSettingsTabAndPanel(screen, out NSettingsPanel? panel, source))
		{
			return;
		}

		VBoxContainer? vbox = panel!.GetNodeOrNull<VBoxContainer>("VBoxContainer");
		if (vbox == null)
		{
			Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): panel missing VBoxContainer");
			return;
		}

		bool hasVoice = vbox.GetNodeOrNull(VoiceSectionName) != null;
		bool hasScale = vbox.GetNodeOrNull(ScaleSectionName) != null;
		bool hasOffsetY = vbox.GetNodeOrNull(OffsetYSectionName) != null;
		bool hasOffsetX = vbox.GetNodeOrNull(OffsetXSectionName) != null;
		bool hasReset = vbox.GetNodeOrNull(ResetSectionName) != null;
		bool hasDynamicPortraits = vbox.GetNodeOrNull(DynamicPortraitsSectionName) != null;

		bool hasShared = hasVoice && hasScale && hasOffsetY && hasOffsetX && hasReset;
		if (hasShared)
		{
			TryWireExistingTransformHooksOnce(vbox);
			if (hasDynamicPortraits)
			{
				return;
			}
		}

		RichTextLabel? templateLabel = screen.GetNodeOrNull<RichTextLabel>(TemplateLabelFullPath);

		PackedScene? settingsSliderScene = ResourceLoader.Load<PackedScene>(SettingsSliderScenePath);
		if (settingsSliderScene == null)
		{
			Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): missing scene " + SettingsSliderScenePath);
			return;
		}

		Control? voiceSliderRoot = null;
		Control? scaleSliderRoot = null;
		Control? offsetYSliderRoot = null;
		Control? offsetXSliderRoot = null;
		Control? resetButtonRoot = null;
		Control? dynamicPortraitsRoot = null;
		ColorRect? voiceLine = null;
		ColorRect? scaleLine = null;
		ColorRect? offsetYLine = null;
		ColorRect? offsetXLine = null;
		ColorRect? resetLine = null;
		ColorRect? cardVisualsLine = null;
		VBoxContainer? voiceSection = null;
		VBoxContainer? scaleSection = null;
		VBoxContainer? offsetYSection = null;
		VBoxContainer? offsetXSection = null;
		VBoxContainer? resetSection = null;
		VBoxContainer? dynamicPortraitsSection = null;

		if (!hasVoice)
		{
			voiceLine = CreateLine(VoiceLineName);
			voiceSection = CreateSection(VoiceSectionName);
			RichTextLabel label = CreateLabel(templateLabel, "卡厄思角色语音音量");
			voiceSliderRoot = settingsSliderScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			voiceSliderRoot.Name = VoiceSliderName;
			voiceSliderRoot.Set("layout_mode", 2);
			voiceSliderRoot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			voiceSliderRoot.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
			voiceSliderRoot.CustomMinimumSize = new Vector2(0, 64);
			voiceSection.AddChild(label);
			voiceSection.AddChild(voiceSliderRoot);
		}

		if (!hasScale)
		{
			scaleLine = CreateLine(ScaleLineName);
			scaleSection = CreateSection(ScaleSectionName);
			RichTextLabel label = CreateLabel(templateLabel, "卡厄思角色立绘缩放");
			scaleSliderRoot = settingsSliderScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			scaleSliderRoot.Name = ScaleSliderName;
			scaleSliderRoot.Set("layout_mode", 2);
			scaleSliderRoot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			scaleSliderRoot.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
			scaleSliderRoot.CustomMinimumSize = new Vector2(0, 64);
			scaleSection.AddChild(label);
			scaleSection.AddChild(scaleSliderRoot);
		}

		if (!hasOffsetY)
		{
			offsetYLine = CreateLine(OffsetYLineName);
			offsetYSection = CreateSection(OffsetYSectionName);
			RichTextLabel label = CreateLabel(templateLabel, "卡厄思角色位置Y");
			offsetYSliderRoot = settingsSliderScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			offsetYSliderRoot.Name = OffsetYSliderName;
			offsetYSliderRoot.Set("layout_mode", 2);
			offsetYSliderRoot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			offsetYSliderRoot.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
			offsetYSliderRoot.CustomMinimumSize = new Vector2(0, 64);
			offsetYSection.AddChild(label);
			offsetYSection.AddChild(offsetYSliderRoot);
		}

		if (!hasOffsetX)
		{
			offsetXLine = CreateLine(OffsetXLineName);
			offsetXSection = CreateSection(OffsetXSectionName);
			RichTextLabel label = CreateLabel(templateLabel, "卡厄思角色位置X");
			offsetXSliderRoot = settingsSliderScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			offsetXSliderRoot.Name = OffsetXSliderName;
			offsetXSliderRoot.Set("layout_mode", 2);
			offsetXSliderRoot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			offsetXSliderRoot.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
			offsetXSliderRoot.CustomMinimumSize = new Vector2(0, 64);
			offsetXSection.AddChild(label);
			offsetXSection.AddChild(offsetXSliderRoot);
		}

		if (!hasReset)
		{
			resetLine = CreateLine(ResetLineName);
			resetSection = CreateSection(ResetSectionName);
			resetButtonRoot = CreateResetButton(templateLabel);
			resetButtonRoot.Name = ResetButtonName;
			resetButtonRoot.Set("layout_mode", 2);
			resetButtonRoot.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			resetButtonRoot.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
			resetButtonRoot.CustomMinimumSize = new Vector2(0, 48);
			resetSection.AddChild(resetButtonRoot);
		}

		if (!hasDynamicPortraits)
		{
			cardVisualsLine = CreateLine(CardVisualsLineName);
			dynamicPortraitsSection = CreateSection(DynamicPortraitsSectionName);

			string sectionTitle = LocString.GetIfExists("settings_ui", "chaos_yuki_card_visuals.title")?.GetFormattedText()
				?? "卡牌视觉";
			RichTextLabel header = CreateLabel(templateLabel, sectionTitle);
			dynamicPortraitsSection.AddChild(header);

			HBoxContainer row = new HBoxContainer
			{
				Name = DynamicPortraitsRowName,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};

			string labelText = LocString.GetIfExists("settings_ui", "chaos_yuki_use_dynamic_card_portraits.title")?.GetFormattedText()
				?? "启用动态卡图";
			RichTextLabel rowLabel = CreateRowLabel(templateLabel, labelText);

			NConfigTickbox tickbox = new NConfigTickbox
			{
				Name = DynamicPortraitsTickboxName
			};

			ModConfig? config = ModConfigRegistry.Get(YukiModInfo.ModId);
			PropertyInfo? prop = typeof(YukiModConfig).GetProperty(nameof(YukiModConfig.UseDynamicCardPortraits), BindingFlags.Public | BindingFlags.Static);
			if (config != null && prop != null)
			{
				tickbox.Initialize(config, prop);
			}

			row.AddChild(rowLabel);
			row.AddChild(tickbox);
			dynamicPortraitsRoot = row;
			dynamicPortraitsSection.AddChild(row);
		}

		if (voiceLine != null) vbox.AddChild(voiceLine);
		if (voiceSection != null) vbox.AddChild(voiceSection);
		if (scaleLine != null) vbox.AddChild(scaleLine);
		if (scaleSection != null) vbox.AddChild(scaleSection);
		if (offsetYLine != null) vbox.AddChild(offsetYLine);
		if (offsetYSection != null) vbox.AddChild(offsetYSection);
		if (offsetXLine != null) vbox.AddChild(offsetXLine);
		if (offsetXSection != null) vbox.AddChild(offsetXSection);
		if (resetLine != null) vbox.AddChild(resetLine);
		if (resetSection != null) vbox.AddChild(resetSection);
		if (cardVisualsLine != null) vbox.AddChild(cardVisualsLine);
		if (dynamicPortraitsSection != null) vbox.AddChild(dynamicPortraitsSection);

		if (voiceSliderRoot != null) WireVoiceSliderWhenReady(voiceSliderRoot, source, 0);
		if (scaleSliderRoot != null) WireScaleSliderWhenReady(scaleSliderRoot, source, 0);
		if (offsetYSliderRoot != null) WireOffsetYSliderWhenReady(offsetYSliderRoot, source, 0);
		if (offsetXSliderRoot != null) WireOffsetXSliderWhenReady(offsetXSliderRoot, source, 0);
		if (resetButtonRoot != null) WireResetButtonWhenReady(vbox, resetButtonRoot, source, 0);
	}

	private static void TryWireExistingTransformHooksOnce(VBoxContainer vbox)
	{
		if (System.Threading.Interlocked.Exchange(ref _wireExistingOnce, 1) != 0)
		{
			return;
		}

		Control? scaleRoot = vbox.GetNodeOrNull<Control>(ScaleSectionName + "/" + ScaleSliderName);
		if (scaleRoot != null)
		{
			TryConnectApplyTransform(scaleRoot);
		}

		Control? offsetXRoot = vbox.GetNodeOrNull<Control>(OffsetXSectionName + "/" + OffsetXSliderName);
		if (offsetXRoot != null)
		{
			TryConnectApplyTransform(offsetXRoot);
		}

		Control? offsetYRoot = vbox.GetNodeOrNull<Control>(OffsetYSectionName + "/" + OffsetYSliderName);
		if (offsetYRoot != null)
		{
			TryConnectApplyTransform(offsetYRoot);
		}

		Control? resetRoot = vbox.GetNodeOrNull<Control>(ResetSectionName + "/" + ResetButtonName);
		if (resetRoot is NClickableControl clickable)
		{
			clickable.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => YukiBattleReadyOverlay.ApplyTransformFromSettings()));
		}
	}

	private static void TryConnectApplyTransform(Control sliderRoot)
	{
		if (!sliderRoot.IsNodeReady())
		{
			Callable.From(() => TryConnectApplyTransform(sliderRoot)).CallDeferred();
			return;
		}
		NSlider? slider = sliderRoot.GetNodeOrNull<NSlider>("Slider");
		if (slider == null)
		{
			return;
		}
		slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(_ => YukiBattleReadyOverlay.ApplyTransformFromSettings()));
		slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
		{
			if (valueChanged)
			{
				YukiBattleReadyOverlay.ApplyTransformFromSettings();
			}
		}));
	}

	private static ColorRect CreateLine(string name)
	{
		return new ColorRect
		{
			Name = name,
			CustomMinimumSize = new Vector2(0, 4),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			Color = new Color(0.34f, 0.34f, 0.34f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	private static VBoxContainer CreateSection(string name)
	{
		return new VBoxContainer
		{
			Name = name,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	private static RichTextLabel CreateLabel(RichTextLabel? templateLabel, string text)
	{
		RichTextLabel label = templateLabel != null
			? (RichTextLabel)templateLabel.Duplicate()
			: new RichTextLabel();
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		label.Set("layout_mode", 2);
		if (label.CustomMinimumSize.Y < 32)
		{
			label.CustomMinimumSize = new Vector2(label.CustomMinimumSize.X, 32);
		}
		label.Text = text;
		return label;
	}

	private static RichTextLabel CreateRowLabel(RichTextLabel? templateLabel, string text)
	{
		RichTextLabel label = templateLabel != null
			? (RichTextLabel)templateLabel.Duplicate()
			: new RichTextLabel();
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.SizeFlagsVertical = Control.SizeFlags.Fill;
		label.CustomMinimumSize = new Vector2(label.CustomMinimumSize.X, 64);
		label.Set("layout_mode", 2);
		label.Text = text;
		return label;
	}

	private static Control CreateResetButton(RichTextLabel? templateLabel)
	{
		NButton button = new NButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
		};
		button.Set("layout_mode", 2);
		button.CustomMinimumSize = new Vector2(0, 48);
		button.MouseFilter = Control.MouseFilterEnum.Stop;

		StyleBoxFlat normalStyle = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.25f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			BorderColor = new Color(1, 1, 1, 0.15f),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 8,
			ContentMarginRight = 8,
			ContentMarginTop = 6,
			ContentMarginBottom = 6
		};

		StyleBoxFlat hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
		hoverStyle.BgColor = new Color(1, 1, 1, 0.10f);
		hoverStyle.BorderColor = new Color(1, 1, 1, 0.25f);

		StyleBoxFlat pressedStyle = (StyleBoxFlat)normalStyle.Duplicate();
		pressedStyle.BgColor = new Color(0, 0, 0, 0.35f);
		pressedStyle.BorderColor = new Color(1, 1, 1, 0.20f);

		Panel bg = new Panel
		{
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bg.AddThemeStyleboxOverride("panel", normalStyle);
		button.AddChild(bg);

		RichTextLabel label = templateLabel != null
			? (RichTextLabel)templateLabel.Duplicate()
			: new RichTextLabel();
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.Text = "重置立绘";
		button.AddChild(label);

		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(button) || !GodotObject.IsInstanceValid(label))
			{
				return;
			}

			float w1 = label.GetContentWidth();
			float w2 = label.GetMinimumSize().X;
			float w = Mathf.Max(w1, w2);

			if (w > 0)
			{
				button.CustomMinimumSize = new Vector2(w + 48, button.CustomMinimumSize.Y);
			}
		}).CallDeferred();

		Color normalColor = Colors.White;
		Color hoverColor = new Color(1.12f, 1.12f, 1.12f, 1);
		Color pressedColor = new Color(0.92f, 0.92f, 0.92f, 1);
		Vector2 normalScale = Vector2.One;
		Vector2 hoverScale = new Vector2(1.05f, 1.05f);
		Vector2 pressedScale = new Vector2(0.98f, 0.98f);

		bool isHover = false;
		bool isPressed = false;
		Tween? activeTween = null;

		void TweenTo(Vector2 scale, Color color, float duration)
		{
			activeTween?.Kill();
			activeTween = button.CreateTween();
			activeTween.SetParallel(true);
			activeTween.TweenProperty(button, "scale", scale, duration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
			activeTween.TweenProperty(button, "modulate", color, duration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		}

		void UpdatePivot()
		{
			button.PivotOffset = button.Size / 2f;
		}

		button.Resized += UpdatePivot;
		Callable.From(UpdatePivot).CallDeferred();

		button.MouseEntered += () =>
		{
			isHover = true;
			if (!isPressed)
			{
				bg.AddThemeStyleboxOverride("panel", hoverStyle);
				TweenTo(hoverScale, hoverColor, 0.08f);
			}
		};
		button.MouseExited += () =>
		{
			isHover = false;
			if (!isPressed)
			{
				bg.AddThemeStyleboxOverride("panel", normalStyle);
				TweenTo(normalScale, normalColor, 0.10f);
			}
		};

		button.GuiInput += input =>
		{
			if (input is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left)
			{
				return;
			}

			if (mb.Pressed)
			{
				isPressed = true;
				bg.AddThemeStyleboxOverride("panel", pressedStyle);
				TweenTo(pressedScale, pressedColor, 0.05f);
				return;
			}

			isPressed = false;
			bg.AddThemeStyleboxOverride("panel", isHover ? hoverStyle : normalStyle);
			TweenTo(isHover ? hoverScale : normalScale, isHover ? hoverColor : normalColor, 0.08f);
		};

		return button;
	}

	private static bool EnsureModSettingsTabAndPanel(NSettingsScreen screen, out NSettingsPanel? panel, string source)
	{
		panel = null;
		NSettingsTabManager? tabManager = screen.GetNodeOrNull<NSettingsTabManager>(SettingsTabManagerPath) ?? screen.GetNodeOrNull<NSettingsTabManager>("%SettingsTabManager");
		if (tabManager == null)
		{
			Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): missing SettingsTabManager");
			return false;
		}

		Control? clipper = screen.GetNodeOrNull<Control>(ClipperPath);
		if (clipper == null)
		{
			Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): missing Clipper");
			return false;
		}

		NSettingsTab? tab = tabManager.GetNodeOrNull<NSettingsTab>(ModTabName);
		NSettingsPanel? settingsPanel = clipper.GetNodeOrNull<NSettingsPanel>(ModPanelName);

		if (System.Threading.Interlocked.Exchange(ref _ensureLogOnce, 1) == 0)
		{
			try
			{
				Log.Info("[YukiMod] EnsureModTab: tabManager=" + tabManager.GetPath() + " clipper=" + clipper.GetPath() + " hasTab=" + (tab != null) + " hasPanel=" + (settingsPanel != null));
			}
			catch
			{
				Log.Info("[YukiMod] EnsureModTab: hasTab=" + (tab != null) + " hasPanel=" + (settingsPanel != null));
			}
		}

		if (settingsPanel == null)
		{
			NSettingsPanel? templatePanel = screen.GetNodeOrNull<NSettingsPanel>("%SoundSettings") ?? screen.GetNodeOrNull<NSettingsPanel>(ClipperPath + "/SoundSettings");
			if (templatePanel == null)
			{
				Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): missing template panel SoundSettings");
				return false;
			}

			try
			{
				settingsPanel = (NSettingsPanel)templatePanel.Duplicate();
			}
			catch (Exception ex)
			{
				Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): duplicate panel failed: " + ex.Message);
				return false;
			}

			settingsPanel.Name = ModPanelName;
			settingsPanel.UniqueNameInOwner = true;
			settingsPanel.Visible = false;

			VBoxContainer? vbox = settingsPanel.GetNodeOrNull<VBoxContainer>("VBoxContainer");
			if (vbox == null)
			{
				Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): duplicated panel missing VBoxContainer");
				return false;
			}
			Node? keep = null;
			foreach (Node child in vbox.GetChildren())
			{
				if (keep == null && child is Control)
				{
					keep = child;
				}
			}
			foreach (Node child in vbox.GetChildren())
			{
				if (child == keep)
				{
					continue;
				}
				vbox.RemoveChild(child);
				child.QueueFree();
			}

			clipper.AddChild(settingsPanel);
		}

		if (tab == null)
		{
			PackedScene? tabScene = ResourceLoader.Load<PackedScene>(ModTabScenePath);
			if (tabScene == null)
			{
				Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): missing tab scene " + ModTabScenePath);
				return false;
			}
			try
			{
				tab = tabScene.Instantiate<NSettingsTab>(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				Log.Warn("[YukiMod] Mod settings inject skipped (" + source + "): instantiate tab failed: " + ex.Message);
				return false;
			}
			tab.Name = ModTabName;
			tab.UniqueNameInOwner = true;
			int rightIconIndex = -1;
			Node? rightIcon = tabManager.GetNodeOrNull("RightTriggerIcon");
			if (rightIcon != null)
			{
				rightIconIndex = rightIcon.GetIndex();
			}
			tabManager.AddChild(tab);
			if (rightIconIndex >= 0)
			{
				tabManager.MoveChild(tab, rightIconIndex);
			}
			tab.Set("layout_mode", 2);
			Callable.From(() =>
			{
				if (!GodotObject.IsInstanceValid(tab) || !tab.IsNodeReady())
				{
					return;
				}
				tab.SetLabel("YukiMod");
			}).CallDeferred();
		}
		else
		{
			Node? rightIcon = tabManager.GetNodeOrNull("RightTriggerIcon");
			if (rightIcon != null)
			{
				int rightIconIndex = rightIcon.GetIndex();
				if (tab.GetIndex() > rightIconIndex)
				{
					tabManager.MoveChild(tab, rightIconIndex);
				}
			}
			tab.Set("layout_mode", 2);
		}

		panel = settingsPanel;
		EnsureTabBinding(tabManager, tab, settingsPanel);
		return true;
	}

	private static void EnsureTabBinding(NSettingsTabManager tabManager, NSettingsTab tab, NSettingsPanel panel)
	{
		try
		{
			var field = AccessTools.Field(typeof(NSettingsTabManager), "_tabs");
			if (field?.GetValue(tabManager) is not Dictionary<NSettingsTab, NSettingsPanel> dict)
			{
				return;
			}
			if (!dict.ContainsKey(tab))
			{
				dict.Add(tab, panel);
			}

			Callable callable = Callable.From<NButton>(_ => SwitchTabTo(tabManager, tab));
			if (!tab.IsConnected(NClickableControl.SignalName.Released, callable))
			{
				tab.Connect(NClickableControl.SignalName.Released, callable);
			}
		}
		catch
		{
		}
	}

	private static void SwitchTabTo(NSettingsTabManager tabManager, NSettingsTab tab)
	{
		try
		{
			var method = AccessTools.Method(typeof(NSettingsTabManager), "SwitchTabTo");
			method?.Invoke(tabManager, new object[] { tab });
		}
		catch
		{
		}
	}

	private static void WireVoiceSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
		{
			return;
		}
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
			{
				Callable.From(() => WireVoiceSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			}
			else
			{
				Log.Warn("[YukiMod] Voice slider not ready after retries (" + source + ")");
			}
			return;
		}
		WireVoiceSlider(sliderRoot);
	}

	private static void WireVoiceSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		MegaLabel valueLabel = sliderRoot.GetNode<MegaLabel>("SliderValue");

		slider.SetValueWithoutAnimation(YukiModSharedSettings.VoiceVolume * 100f);
		valueLabel.SetTextAutoSize($"{slider.Value}%");

		slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
		{
			valueLabel.SetTextAutoSize($"{value}%");
			YukiModSharedSettings.SetVoiceVolume((float)value * 0.01f, persist: false);
		}));
		slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
		{
			if (valueChanged)
			{
				YukiModSharedSettings.SetVoiceVolume(YukiModSharedSettings.VoiceVolume, persist: true);
			}
		}));

		sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
		{
			if (input.IsActionPressed(MegaInput.left))
			{
				slider.Value -= 5.0;
			}
			if (input.IsActionPressed(MegaInput.right))
			{
				slider.Value += 5.0;
			}
		}));
	}

	private static void WireScaleSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
		{
			return;
		}
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
			{
				Callable.From(() => WireScaleSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			}
			else
			{
				Log.Warn("[YukiMod] Scale slider not ready after retries (" + source + ")");
			}
			return;
		}
		WireScaleSlider(sliderRoot);
	}

	private static void WireScaleSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		MegaLabel valueLabel = sliderRoot.GetNode<MegaLabel>("SliderValue");

		slider.MinValue = 50.0;
		slider.MaxValue = 200.0;
		slider.Step = 5.0;
		slider.SetValueWithoutAnimation(Mathf.Clamp(YukiModSharedSettings.BattleReadyScale * 100f, 50f, 200f));
		valueLabel.SetTextAutoSize($"{slider.Value}%");

		slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
		{
			valueLabel.SetTextAutoSize($"{value}%");
			YukiModSharedSettings.SetBattleReadyScale((float)value * 0.01f, persist: false);
			YukiBattleReadyOverlay.ApplyTransformFromSettings();
		}));
		slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
		{
			if (valueChanged)
			{
				YukiModSharedSettings.SetBattleReadyScale(YukiModSharedSettings.BattleReadyScale, persist: true);
			}
		}));

		sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
		{
			if (input.IsActionPressed(MegaInput.left))
			{
				slider.Value -= 5.0;
			}
			if (input.IsActionPressed(MegaInput.right))
			{
				slider.Value += 5.0;
			}
		}));
	}

	private static void WireOffsetYSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
		{
			return;
		}
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
			{
				Callable.From(() => WireOffsetYSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			}
			else
			{
				Log.Warn("[YukiMod] Offset slider not ready after retries (" + source + ")");
			}
			return;
		}
		WireOffsetYSlider(sliderRoot);
	}

	private static void WireOffsetYSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		MegaLabel valueLabel = sliderRoot.GetNode<MegaLabel>("SliderValue");

		slider.MinValue = 0.0;
		slider.MaxValue = 800.0;
		slider.Step = 10.0;
		float initialOffset = Mathf.Clamp(YukiModSharedSettings.BattleReadyOffsetY, -400f, 400f);
		slider.SetValueWithoutAnimation(Mathf.Clamp(initialOffset + 400f, 0f, 800f));
		int initialDisplay = (int)Math.Round(slider.Value - 400.0);
		valueLabel.SetTextAutoSize($"{initialDisplay:+0;-0;0}px");

		slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
		{
			int display = (int)Math.Round(value - 400.0);
			valueLabel.SetTextAutoSize($"{display:+0;-0;0}px");
			YukiModSharedSettings.SetBattleReadyOffsetY((float)value - 400f, persist: false);
			YukiBattleReadyOverlay.ApplyTransformFromSettings();
		}));
		slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
		{
			if (valueChanged)
			{
				YukiModSharedSettings.SetBattleReadyOffsetY(YukiModSharedSettings.BattleReadyOffsetY, persist: true);
			}
		}));

		sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
		{
			if (input.IsActionPressed(MegaInput.left))
			{
				slider.Value -= 10.0;
			}
			if (input.IsActionPressed(MegaInput.right))
			{
				slider.Value += 10.0;
			}
		}));
	}

	private static void WireOffsetXSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
		{
			return;
		}
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
			{
				Callable.From(() => WireOffsetXSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			}
			else
			{
				Log.Warn("[YukiMod] OffsetX slider not ready after retries (" + source + ")");
			}
			return;
		}
		WireOffsetXSlider(sliderRoot);
	}

	private static void WireOffsetXSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		MegaLabel valueLabel = sliderRoot.GetNode<MegaLabel>("SliderValue");

		slider.MinValue = 0.0;
		slider.MaxValue = 800.0;
		slider.Step = 10.0;
		float initialOffset = Mathf.Clamp(YukiModSharedSettings.BattleReadyOffsetX, -400f, 400f);
		slider.SetValueWithoutAnimation(Mathf.Clamp(initialOffset + 400f, 0f, 800f));
		int initialDisplay = (int)Math.Round(slider.Value - 400.0);
		valueLabel.SetTextAutoSize($"{initialDisplay:+0;-0;0}px");

		slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
		{
			int display = (int)Math.Round(value - 400.0);
			valueLabel.SetTextAutoSize($"{display:+0;-0;0}px");
			YukiModSharedSettings.SetBattleReadyOffsetX((float)value - 400f, persist: false);
			YukiBattleReadyOverlay.ApplyTransformFromSettings();
		}));
		slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
		{
			if (valueChanged)
			{
				YukiModSharedSettings.SetBattleReadyOffsetX(YukiModSharedSettings.BattleReadyOffsetX, persist: true);
			}
		}));

		sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
		{
			if (input.IsActionPressed(MegaInput.left))
			{
				slider.Value -= 10.0;
			}
			if (input.IsActionPressed(MegaInput.right))
			{
				slider.Value += 10.0;
			}
		}));
	}

	private static void WireResetButtonWhenReady(VBoxContainer vbox, Control buttonRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(buttonRoot))
		{
			return;
		}
		if (!buttonRoot.IsNodeReady())
		{
			if (attempt < 8)
			{
				Callable.From(() => WireResetButtonWhenReady(vbox, buttonRoot, source, attempt + 1)).CallDeferred();
			}
			else
			{
				Log.Warn("[YukiMod] Reset button not ready after retries (" + source + ")");
			}
			return;
		}
		WireResetButton(vbox, buttonRoot);
	}

	private static void WireResetButton(VBoxContainer vbox, Control buttonRoot)
	{
		if (buttonRoot is not NClickableControl clickable)
		{
			return;
		}
		clickable.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => ResetBattleReadyPosition(vbox)));
	}

	private static void ResetBattleReadyPosition(VBoxContainer vbox)
	{
		YukiModSharedSettings.SetBattleReadyScale(1f, persist: false);
		YukiModSharedSettings.SetBattleReadyOffsetX(0f, persist: false);
		YukiModSharedSettings.SetBattleReadyOffsetY(0f, persist: true);
		YukiBattleReadyOverlay.ApplyTransformFromSettings();

		Control? scaleRoot = vbox.GetNodeOrNull<Control>(ScaleSectionName + "/" + ScaleSliderName);
		if (scaleRoot != null)
		{
			SetScaleSliderValue(scaleRoot, 1f);
		}

		Control? offsetXRoot = vbox.GetNodeOrNull<Control>(OffsetXSectionName + "/" + OffsetXSliderName);
		if (offsetXRoot != null)
		{
			SetOffsetSliderValue(offsetXRoot, 0f);
		}

		Control? offsetYRoot = vbox.GetNodeOrNull<Control>(OffsetYSectionName + "/" + OffsetYSliderName);
		if (offsetYRoot != null)
		{
			SetOffsetSliderValue(offsetYRoot, 0f);
		}
	}

	private static void SetScaleSliderValue(Control sliderRoot, float scale)
	{
		if (!sliderRoot.IsNodeReady())
		{
			return;
		}

		NSlider? slider = sliderRoot.GetNodeOrNull<NSlider>("Slider");
		MegaLabel? valueLabel = sliderRoot.GetNodeOrNull<MegaLabel>("SliderValue");
		if (slider == null || valueLabel == null)
		{
			return;
		}

		double v = Mathf.Clamp(scale * 100f, 50f, 200f);
		slider.SetValueWithoutAnimation(v);
		valueLabel.SetTextAutoSize($"{v}%");
	}

	private static void SetOffsetSliderValue(Control sliderRoot, float offset)
	{
		if (!sliderRoot.IsNodeReady())
		{
			return;
		}

		NSlider? slider = sliderRoot.GetNodeOrNull<NSlider>("Slider");
		MegaLabel? valueLabel = sliderRoot.GetNodeOrNull<MegaLabel>("SliderValue");
		if (slider == null || valueLabel == null)
		{
			return;
		}

		double v = Mathf.Clamp(offset + 400f, 0f, 800f);
		slider.SetValueWithoutAnimation(v);
		int display = (int)Math.Round(v - 400.0);
		valueLabel.SetTextAutoSize($"{display:+0;-0;0}px");
	}
}
