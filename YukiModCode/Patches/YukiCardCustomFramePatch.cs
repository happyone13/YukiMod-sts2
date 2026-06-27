using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardCustomFramePatch
{
    private const string ChaosEffectsBasePath = "res://YukiMod/images/cards/card_effects/";
    private const string ChaosEffectsTemplatePath = "res://YukiMod/scenes/cards/chaos_card_effects_frame_template.tscn";
    private const string TemplateCardContainerPath = "CardContainer";
    private const string RarityBaseNodeName = "YukiChaosRarityBase";
    private const string RaritySubNodeName = "YukiChaosRaritySub";
    private const string EgoBadgeNodeName = "YukiChaosEgoBadge";
    private const string EgoBadge2NodeName = "YukiChaosEgoBadge2";
    private const string FrameSparkNodeName = "YukiChaosFrameSpark";
    private const string CostLineNodeName = "YukiChaosCostLine";
    private const string CategoryIconNodeName = "YukiChaosCategoryIcon";
    private const string CategoryTextNodeName = "YukiChaosCategoryText";
    private const string CostTextNodeName = "YukiChaosCostText";
    private const string CostTextFallbackNodeName = "YukiChaosCostTextFallback";
    private const string UpgradeIconNodeName = "YukiChaosUpgradeIcon";
    private const string DescriptionMaskNodeName = "YukiChaosDescriptionMask";
    private static readonly NodeLayout TitleRibbonLayout = new(-146.0f, -214.0f, 292.0f, 82.0f);
    private static readonly NodeLayout CardTitleLayout = new(-151.0f, -209.0f, 201.0f, 58.0f);
    private static readonly NodeLayout CostLineLayout = new(-145.0f, -200.0f, 68.0f, 115.0f);
    private static readonly NodeLayout CostTextLayout = new(-138.0f, -235.0f, 55.0f, 90.0f);
    private static readonly NodeLayout CategoryIconLayout = new(-87.0f, -177.0f, 28.0f, 44.0f);
    private static readonly NodeLayout CategoryTextLayout = new(-57.0f, -178.0f, 198.0f, 42.0f);
    private static readonly NodeLayout DescriptionTextLayout = new(-142.0f, 40.0f, 278.0f, 161.0f);
    private static readonly NodeLayout DescriptionMaskLayout = new(-153.0f, -63.0f, 298.0f, 271.0f);
    private static readonly NodeLayout EgoBadgeLayout = new(-196.0f, -218.0f, 97.0f, 431.4479f);
    private static readonly NodeLayout EgoBadge2Layout = new(96.0f, -215.0f, 96.0f, 427.0f, Visible: false);
    private static readonly NodeLayout RarityBaseLayout = new(-172.0f, -194.0f, 35.0f, 78.0f);
    private static readonly NodeLayout RaritySubLayout = new(122.0f, -199.0f, 56.0f, 90.0f);
    private static readonly NodeLayout FrameSparkLayout = new(-91.0f, -83.0f, 157.0f, 218.0f);
    private static readonly NodeLayout UpgradeIconLayout = new(-131.0f, -138.0f, 32.0f, 32.0f, Visible: false);
    private static readonly Dictionary<char, Rect2> NormalDigitRegions = new()
    {
        ['0'] = new Rect2(79.0f, 4.0f, 78.0f, 87.0f),
        ['1'] = new Rect2(158.0f, 4.0f, 78.0f, 87.0f),
        ['2'] = new Rect2(237.0f, 4.0f, 78.0f, 87.0f),
        ['3'] = new Rect2(316.0f, 4.0f, 78.0f, 87.0f),
        ['4'] = new Rect2(395.0f, 4.0f, 78.0f, 87.0f),
        ['5'] = new Rect2(0.0f, 96.0f, 78.0f, 87.0f),
        ['6'] = new Rect2(79.0f, 96.0f, 78.0f, 87.0f),
        ['7'] = new Rect2(158.0f, 96.0f, 78.0f, 87.0f),
        ['8'] = new Rect2(237.0f, 96.0f, 78.0f, 87.0f),
        ['9'] = new Rect2(316.0f, 96.0f, 78.0f, 87.0f)
    };
    private static readonly Dictionary<char, Rect2> GreenDigitRegions = new()
    {
        ['0'] = new Rect2(0.0f, 4.0f, 78.0f, 87.0f),
        ['1'] = new Rect2(79.0f, 4.0f, 78.0f, 87.0f),
        ['2'] = new Rect2(158.0f, 4.0f, 78.0f, 87.0f),
        ['3'] = new Rect2(237.0f, 4.0f, 78.0f, 87.0f),
        ['4'] = new Rect2(316.0f, 4.0f, 78.0f, 87.0f),
        ['5'] = new Rect2(395.0f, 4.0f, 78.0f, 87.0f),
        ['6'] = new Rect2(0.0f, 96.0f, 78.0f, 87.0f),
        ['7'] = new Rect2(79.0f, 96.0f, 78.0f, 87.0f),
        ['8'] = new Rect2(158.0f, 96.0f, 78.0f, 87.0f),
        ['9'] = new Rect2(237.0f, 96.0f, 78.0f, 87.0f)
    };
    private static readonly Dictionary<char, Rect2> RedDigitRegions = new()
    {
        ['0'] = new Rect2(0.0f, 4.0f, 78.0f, 87.0f),
        ['1'] = new Rect2(79.0f, 4.0f, 78.0f, 87.0f),
        ['2'] = new Rect2(158.0f, 4.0f, 78.0f, 87.0f),
        ['3'] = new Rect2(237.0f, 4.0f, 78.0f, 87.0f),
        ['4'] = new Rect2(316.0f, 4.0f, 78.0f, 87.0f),
        ['5'] = new Rect2(395.0f, 4.0f, 78.0f, 87.0f),
        ['6'] = new Rect2(0.0f, 96.0f, 78.0f, 87.0f),
        ['7'] = new Rect2(79.0f, 96.0f, 78.0f, 87.0f),
        ['8'] = new Rect2(158.0f, 96.0f, 78.0f, 87.0f),
        ['9'] = new Rect2(237.0f, 96.0f, 78.0f, 87.0f)
    };

    private static readonly FieldInfo? FrameField =
        typeof(NCard).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? PortraitBorderField =
        typeof(NCard).GetField("_portraitBorder", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? BannerField =
        typeof(NCard).GetField("_banner", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientBorderField =
        typeof(NCard).GetField("_ancientBorder", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientTextBgField =
        typeof(NCard).GetField("_ancientTextBg", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientBannerField =
        typeof(NCard).GetField("_ancientBanner", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientHighlightField =
        typeof(NCard).GetField("_ancientHighlight", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? PortraitCanvasGroupField =
        typeof(NCard).GetField("_portraitCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? EnergyIconField =
        typeof(NCard).GetField("_energyIcon", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? EnergyLabelField =
        typeof(NCard).GetField("_energyLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TitleLabelField =
        typeof(NCard).GetField("_titleLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TypeLabelField =
        typeof(NCard).GetField("_typeLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TypePlaqueField =
        typeof(NCard).GetField("_typePlaque", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? DescriptionLabelField =
        typeof(NCard).GetField("_descriptionLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CardFlyVfxCardField =
        typeof(NCardFlyVfx).GetField("_card", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Dictionary<string, Resource?> ResourceCache = new();
    private static readonly HashSet<string> MissingResourceWarnings = new();
    private static readonly Dictionary<CostAtlasVariant, Texture2D?> CostAtlasTextures = new();
    private static readonly ConditionalWeakTable<NCard, OriginalCardVisualState> OriginalStates = new();
    private static Control? _templateRoot;
    private static Control? _templateCardContainer;

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsNodeReady())
            return;

        if (!TryGetCustomFrameCard(cardNode, out _) && !HasYukiVisualState(cardNode))
            return;

        if (YukiCardSpinePortraitPatch.HasActiveSpineOverlay(cardNode) ||
            cardNode.Model is IYukiCardVisualProfile)
        {
            YukiCardSpinePortraitPatch.PrepareForBaseVisuals(cardNode);
        }

        RemoveChaosEffects(cardNode, restoreOriginalState: true);
    }

    public static void Apply(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsNodeReady())
            return;

        if (!TryGetCustomFrameCard(cardNode, out CardModel? cardModel))
        {
            if (HasYukiVisualState(cardNode))
            {
                RemoveChaosEffects(cardNode, restoreOriginalState: true);
                YukiCardSpinePortraitPatch.RemoveSpineOverlay(cardNode);
            }

            return;
        }

        var frame = Get<TextureRect>(FrameField, cardNode!);
        var portrait = Get<TextureRect>(PortraitField, cardNode!);
        var ancientPortrait = Get<TextureRect>(AncientPortraitField, cardNode!);
        var portraitBorder = Get<TextureRect>(PortraitBorderField, cardNode!);
        var banner = Get<TextureRect>(BannerField, cardNode!);
        var ancientBorder = Get<TextureRect>(AncientBorderField, cardNode!);
        var ancientTextBg = Get<TextureRect>(AncientTextBgField, cardNode!);
        var ancientBanner = Get<Control>(AncientBannerField, cardNode!);
        var ancientHighlight = Get<TextureRect>(AncientHighlightField, cardNode!);
        var portraitCanvasGroup = Get<CanvasGroup>(PortraitCanvasGroupField, cardNode!);
        Material? frameMaterial = LoadResource<Material>(YukiCardFramePaths.FrameMaterialPath);
        Material? bannerMaterial = LoadResource<Material>(YukiCardFramePaths.BannerMaterialPath);
        bool hasDynamicSpineScene = YukiModConfig.UseDynamicCardPortraits &&
                                    YukiCardSpinePortraitPatch.TryGetSpineScenePath(cardNode, out _);
        if (hasDynamicSpineScene)
            YukiCardSpinePortraitPatch.Apply(cardNode);

        bool shouldDisplayCustomUi = YukiCardSpinePortraitPatch.ShouldDisplayCustomUi(cardNode);

        if (hasDynamicSpineScene && !YukiCardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
        {
            RemoveChaosEffects(cardNode, restoreOriginalState: true);
            frame?.Show();
            portrait?.Show();
            portraitBorder?.Show();
            banner?.Show();
            ancientPortrait?.Hide();
            ancientBorder?.Hide();
            ancientTextBg?.Hide();
            ancientBanner?.Hide();
            ancientHighlight?.Hide();
            portraitCanvasGroup?.Show();
            return;
        }

        if (!shouldDisplayCustomUi)
        {
            ApplyTransitionDynamicPortraitState(
                cardNode!,
                cardModel!,
                frame,
                portrait,
                ancientPortrait,
                portraitBorder,
                banner,
                ancientBorder,
                ancientTextBg,
                ancientBanner,
                ancientHighlight,
                frameMaterial,
                bannerMaterial);
            return;
        }

        CaptureOriginalState(cardNode!);

        frame?.Hide();
        portrait?.Hide();
        portraitBorder?.Hide();
        banner?.Hide();

        if (ancientPortrait != null)
        {
            ancientPortrait.Show();
            SetPortraitTextureForOverlayState(ancientPortrait, cardNode!.Model?.Portrait);
        }
        portraitCanvasGroup?.Show();

        ApplyTextureRect(ancientBorder, YukiCardFramePaths.AncientBorderTexturePath, frameMaterial, show: true);
        ancientBorder?.Hide();
        ApplyTextureRect(ancientTextBg, YukiCardFramePaths.GetAncientTextBgTexturePath(cardModel!.Type), frameMaterial, show: true);
        ancientHighlight?.Hide();

        if (ancientBanner != null)
        {
            ancientBanner.Show();
            ancientBanner.Material = bannerMaterial;
            if (ResolveTextureRect(ancientBanner) is { } bannerTextureRect)
                ApplyTextureRect(bannerTextureRect, YukiCardFramePaths.AncientBannerTexturePath, bannerMaterial, show: true);
        }

        ApplyChaosEffects(cardNode!, cardModel);

        if (hasDynamicSpineScene && cardNode!.Model is IYukiCardVisualProfile profile)
        {
            YukiCardSpinePortraitPatch.ForcePortraitSlot(cardNode, portrait, ancientPortrait, profile.CustomSpinePortraitSlot);
            YukiCardSpinePortraitPatch.UpdateOverlay(
                cardNode,
                profile.CustomSpinePortraitSlot == YukiSpinePortraitSlot.Ancient ? ancientPortrait : portrait);
        }
    }

    private static void ApplyTransitionDynamicPortraitState(
        NCard cardNode,
        CardModel cardModel,
        TextureRect? frame,
        TextureRect? portrait,
        TextureRect? ancientPortrait,
        TextureRect? portraitBorder,
        TextureRect? banner,
        TextureRect? ancientBorder,
        TextureRect? ancientTextBg,
        Control? ancientBanner,
        TextureRect? ancientHighlight,
        Material? frameMaterial,
        Material? bannerMaterial)
    {
        CaptureOriginalState(cardNode);
        RemoveChaosEffects(cardNode, restoreOriginalState: false);
        RestoreOriginalState(cardNode, removeState: false);

        frame?.Hide();
        portrait?.Hide();
        if (ancientPortrait != null)
        {
            ancientPortrait.Show();
            SetPortraitTextureForOverlayState(ancientPortrait, cardNode.Model?.Portrait);
        }
        Get<CanvasGroup>(PortraitCanvasGroupField, cardNode)?.Show();
        portraitBorder?.Hide();
        if (banner != null)
            banner.Material = null;
        ApplyTextureRect(banner, GetRarityTitlePath(cardModel.Rarity), material: null, show: true);
        ApplyTextureRect(ancientBorder, YukiCardFramePaths.AncientBorderTexturePath, frameMaterial, show: true);
        ancientBorder?.Hide();
        ancientTextBg?.Hide();
        ancientBanner?.Hide();
        ancientHighlight?.Hide();

        var energyLabel = Get<Control>(EnergyLabelField, cardNode);
        if (Get<TextureRect>(EnergyIconField, cardNode) is { } energyIcon)
        {
            ApplyTextureRect(
                energyIcon,
                GetEnergyLinePath(GetCostAtlasVariant(energyLabel)),
                material: null,
                show: true);
        }

        Get<Control>(TitleLabelField, cardNode)?.Show();
        Get<Control>(DescriptionLabelField, cardNode)?.Show();

        var typeLabel = Get<Control>(TypeLabelField, cardNode);
        var typePlaque = Get<Control>(TypePlaqueField, cardNode);
        if (energyLabel != null)
            energyLabel.Show();
        if (typeLabel != null)
            typeLabel.Show();
        if (typePlaque != null)
            typePlaque.Show();
        RemoveNode(cardNode, CostLineNodeName);
        RemoveNode(cardNode, CostTextNodeName);
        RemoveNode(cardNode, CostTextFallbackNodeName);
        RemoveNode(cardNode, CategoryTextNodeName);
        RemoveNode(cardNode, CategoryIconNodeName);

        if (cardNode.Model is not IYukiCardVisualProfile profile)
        {
            if (portrait != null)
                portrait.Hide();
            if (ancientPortrait != null)
                ancientPortrait.Show();
            return;
        }

        if (!YukiCardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
        {
            if (portrait != null)
                portrait.Hide();
            if (ancientPortrait != null)
                ancientPortrait.Show();
            return;
        }

        YukiCardSpinePortraitPatch.ForcePortraitSlot(cardNode, portrait, ancientPortrait, profile.CustomSpinePortraitSlot);
        if (portrait != null && profile.CustomSpinePortraitSlot == YukiSpinePortraitSlot.Ancient)
            portrait.Hide();
        if (ancientPortrait != null && profile.CustomSpinePortraitSlot == YukiSpinePortraitSlot.Ancient)
            ancientPortrait.Show();

        YukiCardSpinePortraitPatch.UpdateOverlay(
            cardNode,
            profile.CustomSpinePortraitSlot == YukiSpinePortraitSlot.Ancient ? ancientPortrait : portrait);
    }

    private static bool TryGetCustomFrameCard(NCard? cardNode, out CardModel? cardModel)
    {
        cardModel = null;

        switch (cardNode?.Model)
        {
            case YukiModCard card when card.UseCustomFrame:
                cardModel = card;
                return true;
            case YukiModTokenCard tokenCard when tokenCard.UseCustomFrame:
                cardModel = tokenCard;
                return true;
            default:
                return false;
        }
    }

    private static void ApplyChaosEffects(NCard cardNode, CardModel cardModel)
    {
        var banner = Get<TextureRect>(BannerField, cardNode);
        var titleLabel = Get<Control>(TitleLabelField, cardNode);
        var energyIcon = Get<TextureRect>(EnergyIconField, cardNode);
        var descriptionLabel = Get<Control>(DescriptionLabelField, cardNode);
        var energyLabel = Get<Control>(EnergyLabelField, cardNode);
        var typeLabel = Get<Control>(TypeLabelField, cardNode);
        var typePlaque = Get<Control>(TypePlaqueField, cardNode);

        ApplyTemplateLayout(banner, "TitleRibbon", TitleRibbonLayout);
        ApplyTemplateLayout(titleLabel, "CardTitle", CardTitleLayout);
        ApplyTemplateLayout(descriptionLabel, "DescriptionText", DescriptionTextLayout);
        ApplyTemplateLayout(typeLabel, "CategoryText", CategoryTextLayout);

        EnsureControlVisible(banner);
        EnsureControlVisible(titleLabel);
        EnsureControlVisible(descriptionLabel);

        if (banner != null)
            banner.Material = null;
        ApplyTextureRect(banner, GetRarityTitlePath(cardModel.Rarity), material: null, show: true);
        ConfigureCostOverlay(cardNode, energyIcon, energyLabel);

        if (Get<Control>(AncientBannerField, cardNode) is { } activeAncientBanner)
            activeAncientBanner.Hide();

        if (typePlaque != null)
            typePlaque.Visible = false;

        string typeText = GetDisplayTypeText(cardModel);
        if (typeLabel != null)
            typeLabel.Hide();

        EnsureTemplateOverlay(cardNode, CategoryTextNodeName, "CategoryText", () => CreateLabelOverlay(CategoryTextLayout), control =>
        {
            ApplyTemplateLayout(control, "CategoryText", CategoryTextLayout);
            SetOverlayText(control, typeText, !string.IsNullOrWhiteSpace(typeText), typeLabel);
            BringToFront(control);
        });

        if (banner != null)
            BringToFront(banner);
        if (titleLabel != null)
            BringToFront(titleLabel);
        if (descriptionLabel != null)
            BringToFront(descriptionLabel);
        BringCostOverlayToFront(cardNode);

        RemoveNode(cardNode, DescriptionMaskNodeName);

        EnsureTemplateOverlay(cardNode, EgoBadgeNodeName, "EgoBadge", () => CreateTextureOverlay(EgoBadgeLayout), control =>
        {
            ApplyTemplateLayout(control, "EgoBadge", EgoBadgeLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, YukiCardFramePaths.GetEgoBadgeTexturePath(cardModel.Rarity), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, EgoBadge2NodeName, "EgoBadge2", () => CreateTextureOverlay(EgoBadge2Layout), control =>
        {
            ApplyTemplateLayout(control, "EgoBadge2", EgoBadge2Layout);
            control.Visible = UsesAllFrameBadge(cardModel.Rarity);
        });

        EnsureTemplateOverlay(cardNode, RarityBaseNodeName, "RarityBase", () => CreateTextureOverlay(RarityBaseLayout), control =>
        {
            ApplyTemplateLayout(control, "RarityBase", RarityBaseLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetRarityBasePath(cardModel.Rarity), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, RaritySubNodeName, "RaritySub", () => CreateTextureOverlay(RaritySubLayout), control =>
        {
            ApplyTemplateLayout(control, "RaritySub", RaritySubLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetRaritySubPath(cardModel.Rarity), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, FrameSparkNodeName, "FrameSpark", () => CreateTextureOverlay(FrameSparkLayout), control =>
        {
            ApplyTemplateLayout(control, "FrameSpark", FrameSparkLayout);
            BringToFront(control);
        });

        EnsureTemplateOverlay(cardNode, CategoryIconNodeName, "CategoryIcon", () => CreateTextureOverlay(CategoryIconLayout), control =>
        {
            ApplyTemplateLayout(control, "CategoryIcon", CategoryIconLayout);
            SetOverlayVisibility(control, !string.IsNullOrWhiteSpace(typeText), typeLabel);
            EnsureDrawBefore(control, typeLabel);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetCategoryIconPath(cardModel.Type), material: null, show: true);
            BringToFront(control);
        });

        EnsureTemplateOverlay(cardNode, UpgradeIconNodeName, "UpgradeIcon", () => CreateTextureOverlay(UpgradeIconLayout), control =>
        {
            ApplyTemplateLayout(control, "UpgradeIcon", UpgradeIconLayout with { Visible = cardModel.IsUpgraded });
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, $"{ChaosEffectsBasePath}icon_card_battle_expand_default.png", material: null, show: cardModel.IsUpgraded);
        });
        BringCostOverlayToFront(cardNode);
    }

    private static void ConfigureCostOverlay(NCard cardNode, TextureRect? energyIcon, Control? energyLabelControl)
    {
        CostAtlasVariant costVariant = GetCostAtlasVariant(energyLabelControl);

        EnsureTemplateOverlay(cardNode, CostLineNodeName, "CostLine", () => CreateTextureOverlay(CostLineLayout), control =>
        {
            ApplyTemplateLayout(control, "CostLine", CostLineLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetEnergyLinePath(costVariant), material: null, show: true);
            BringToFront(control);
        });

        if (GetOverlayNode(cardNode, CostTextNodeName) is Label)
        {
            RemoveNode(cardNode, CostTextNodeName);
        }

        EnsureTemplateOverlay(cardNode, CostTextNodeName, "CostTextAtlasPreview", () => new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        }, control =>
        {
            ApplyTemplateLayout(control, "CostTextAtlasPreview", CostTextLayout);
            BringToFront(control);
        });

        if (GetOverlayNode(cardNode, CostTextFallbackNodeName) is { } existingFallback && existingFallback is not Label)
        {
            RemoveNode(cardNode, CostTextFallbackNodeName);
        }

        EnsureTemplateOverlay(cardNode, CostTextFallbackNodeName, "CostText", () => CreateLabelOverlay(CostTextLayout), control =>
        {
            ApplyTemplateLayout(control, "CostText", CostTextLayout);
            BringToFront(control);
        });

        if (energyIcon != null)
            energyIcon.Hide();
        if (energyLabelControl != null)
            energyLabelControl.Hide();

        string displayText = GetControlText(energyLabelControl);
        var preview = GetOverlayNode(cardNode, CostTextNodeName);
        var fallbackLabel = GetOverlayNode(cardNode, CostTextFallbackNodeName) as Label;
        if (preview == null || fallbackLabel == null)
            return;

        if (!string.IsNullOrWhiteSpace(displayText) && IsDigitsOnly(displayText))
        {
            RenderCostDigits(preview, displayText, costVariant);
            preview.Show();
            fallbackLabel.Hide();
            return;
        }

        ClearCostDigits(preview);
        preview.Hide();

        if (string.IsNullOrWhiteSpace(displayText))
        {
            fallbackLabel.Hide();
            return;
        }

        fallbackLabel.Text = displayText;
        SyncFallbackCostTheme(fallbackLabel, energyLabelControl as Label);
        fallbackLabel.Show();
    }

    private static string GetControlText(Control? control)
    {
        return control switch
        {
            Label label => label.Text ?? string.Empty,
            RichTextLabel richText => richText.Text ?? string.Empty,
            _ => string.Empty
        };
    }

    private static bool IsDigitsOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (char c in text)
        {
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    private static void SyncFallbackCostTheme(Label target, Label? source)
    {
        if (source == null)
            return;

        target.AddThemeColorOverride("font_color", GetThemeColor(source, "font_color"));
        target.AddThemeColorOverride("font_outline_color", GetThemeColor(source, "font_outline_color"));
        target.AddThemeConstantOverride("outline_size", GetThemeConstant(source, "outline_size"));
    }

    private static Color GetThemeColor(Control control, string name)
    {
        return control.GetThemeColor(name);
    }

    private static int GetThemeConstant(Control control, string name)
    {
        return control.GetThemeConstant(name);
    }

    private static CostAtlasVariant GetCostAtlasVariant(Control? energyLabelControl)
    {
        if (energyLabelControl is not Label label)
            return CostAtlasVariant.Normal;

        Color fontColor = GetThemeColor(label, "font_color");
        Color outlineColor = GetThemeColor(label, "font_outline_color");
        if (LooksLikeGreen(fontColor) || LooksLikeGreen(outlineColor))
            return CostAtlasVariant.Green;
        if (LooksLikeRed(fontColor) || LooksLikeRed(outlineColor))
            return CostAtlasVariant.Red;

        return CostAtlasVariant.Normal;
    }

    private static bool LooksLikeGreen(Color color)
    {
        return color.G >= 0.6f && color.G >= color.R + 0.08f && color.G >= color.B + 0.08f;
    }

    private static bool LooksLikeRed(Color color)
    {
        return color.R >= 0.6f && color.R >= color.G + 0.15f && color.R >= color.B + 0.15f;
    }

    private static void RenderCostDigits(Control preview, string text, CostAtlasVariant variant)
    {
        ClearCostDigits(preview);

        Dictionary<char, Rect2> digitRegions = GetDigitRegions(variant);
        Texture2D? texture = LoadCostAtlasTexture(variant);
        if (texture == null)
        {
            preview.Hide();
            return;
        }

        var visibleDigits = new List<char>(text.Length);
        float totalSourceWidth = 0.0f;
        float maxSourceHeight = 0.0f;

        foreach (char c in text)
        {
            if (!digitRegions.TryGetValue(c, out Rect2 region))
                continue;

            visibleDigits.Add(c);
            totalSourceWidth += region.Size.X;
            maxSourceHeight = MathF.Max(maxSourceHeight, region.Size.Y);
        }

        if (visibleDigits.Count == 0 || totalSourceWidth <= 0.0f || maxSourceHeight <= 0.0f)
        {
            preview.Hide();
            return;
        }

        float scale = MathF.Min(preview.Size.Y / maxSourceHeight, preview.Size.X / totalSourceWidth);
        if (scale <= 0.0f || float.IsNaN(scale) || float.IsInfinity(scale))
        {
            preview.Hide();
            return;
        }

        float startX = (preview.Size.X - totalSourceWidth * scale) * 0.5f;
        float startY = (preview.Size.Y - maxSourceHeight * scale) * 0.5f;
        float cursorX = startX;

        for (int i = 0; i < visibleDigits.Count; i++)
        {
            Rect2 region = digitRegions[visibleDigits[i]];
            var rect = new TextureRect
            {
                Name = $"CostDigit{i}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Texture = new AtlasTexture
                {
                    Atlas = texture,
                    Region = region
                },
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Position = new Vector2(cursorX, startY),
                Size = region.Size * scale
            };
            preview.AddChild(rect);
            cursorX += region.Size.X * scale;
        }

        preview.Show();
    }

    private static void ClearCostDigits(Control preview)
    {
        foreach (Node child in preview.GetChildren())
        {
            if (child.Name.ToString().StartsWith("CostDigit", StringComparison.Ordinal))
                DestroyNodeImmediately(child);
        }
    }

    private static Texture2D? LoadCostAtlasTexture(CostAtlasVariant variant)
    {
        if (CostAtlasTextures.TryGetValue(variant, out Texture2D? cached) && cached != null && GodotObject.IsInstanceValid(cached))
            return cached;

        string path = variant switch
        {
            CostAtlasVariant.Green => $"{ChaosEffectsBasePath}card_green_0.png",
            CostAtlasVariant.Red => $"{ChaosEffectsBasePath}card_red_0.png",
            _ => $"{ChaosEffectsBasePath}card_normal_0.png"
        };

        Texture2D? texture = LoadResource<Texture2D>(path);
        CostAtlasTextures[variant] = texture;
        return texture;
    }

    private static Dictionary<char, Rect2> GetDigitRegions(CostAtlasVariant variant)
    {
        return variant switch
        {
            CostAtlasVariant.Green => GreenDigitRegions,
            CostAtlasVariant.Red => RedDigitRegions,
            _ => NormalDigitRegions
        };
    }

    private static void RemoveChaosEffects(NCard? cardNode, bool restoreOriginalState)
    {
        if (cardNode == null)
            return;

        RemoveNode(cardNode, RarityBaseNodeName);
        RemoveNode(cardNode, RaritySubNodeName);
        RemoveNode(cardNode, EgoBadgeNodeName);
        RemoveNode(cardNode, EgoBadge2NodeName);
        RemoveNode(cardNode, FrameSparkNodeName);
        RemoveNode(cardNode, CostLineNodeName);
        RemoveNode(cardNode, CategoryIconNodeName);
        RemoveNode(cardNode, CategoryTextNodeName);
        RemoveNode(cardNode, CostTextNodeName);
        RemoveNode(cardNode, CostTextFallbackNodeName);
        RemoveNode(cardNode, UpgradeIconNodeName);
        RemoveNode(cardNode, DescriptionMaskNodeName);

        if (restoreOriginalState)
            RestoreOriginalState(cardNode);
    }

    private static void EnsureTemplateOverlay(
        NCard cardNode,
        string runtimeNodeName,
        string templateNodeName,
        Func<Control?> fallbackCreate,
        Action<Control>? configure = null)
    {
        Node overlayParent = GetOverlayParent(cardNode);
        Control? control = GetOverlayNode(cardNode, runtimeNodeName);
        if (control == null)
        {
            control = DuplicateTemplateNode(templateNodeName) ?? fallbackCreate();
            if (control == null)
                return;

            control.Name = runtimeNodeName;
            overlayParent.AddChild(control);
        }
        else if (control.GetParent() != overlayParent)
        {
            control.GetParent()?.RemoveChild(control);
            overlayParent.AddChild(control);
        }

        configure?.Invoke(control);
    }

    private static void ApplyTemplateLayout(Control? target, string templateNodeName, NodeLayout fallbackLayout)
    {
        if (target == null)
            return;

        if (GetTemplateNode<Control>(templateNodeName) is { } template)
        {
            ApplyLayout(target, template);
            return;
        }

        ApplyLayout(target, fallbackLayout);
    }

    private static void ApplyLayout(Control? target, Control template)
    {
        if (target == null)
            return;

        target.Position = template.Position;
        target.Size = template.Size;
        target.AnchorLeft = template.AnchorLeft;
        target.AnchorTop = template.AnchorTop;
        target.AnchorRight = template.AnchorRight;
        target.AnchorBottom = template.AnchorBottom;
        target.OffsetLeft = template.OffsetLeft;
        target.OffsetTop = template.OffsetTop;
        target.OffsetRight = template.OffsetRight;
        target.OffsetBottom = template.OffsetBottom;
        target.PivotOffset = template.PivotOffset;
        target.Rotation = template.Rotation;
        target.Scale = template.Scale;
        target.CustomMinimumSize = template.CustomMinimumSize;
        target.Visible = template.Visible;

        if (target is TextureRect targetTextureRect && template is TextureRect templateTextureRect)
        {
            targetTextureRect.StretchMode = templateTextureRect.StretchMode;
            targetTextureRect.ExpandMode = templateTextureRect.ExpandMode;
            targetTextureRect.Modulate = templateTextureRect.Modulate;
        }

        if (target is Label targetLabel && template is Label templateLabel)
        {
            targetLabel.HorizontalAlignment = templateLabel.HorizontalAlignment;
            targetLabel.VerticalAlignment = templateLabel.VerticalAlignment;
            targetLabel.AutowrapMode = templateLabel.AutowrapMode;
            targetLabel.ClipText = templateLabel.ClipText;
            targetLabel.Uppercase = templateLabel.Uppercase;
        }

        if (target is RichTextLabel targetRichText && template is RichTextLabel templateRichText)
        {
            targetRichText.ScrollActive = templateRichText.ScrollActive;
            targetRichText.FitContent = templateRichText.FitContent;
            targetRichText.AutowrapMode = templateRichText.AutowrapMode;
        }
    }

    private static void ApplyLayout(Control? target, NodeLayout layout)
    {
        if (target == null)
            return;

        target.Position = layout.Position;
        target.Size = layout.Size;
        target.Visible = layout.Visible;
    }

    private static Control? DuplicateTemplateNode(string templateNodeName)
    {
        return GetTemplateNode<Control>(templateNodeName)?.Duplicate() as Control;
    }

    private static T? GetTemplateNode<T>(string nodePath) where T : Node
    {
        return GetTemplateCardContainer()?.GetNodeOrNull<T>(nodePath);
    }

    private static Control? GetTemplateCardContainer()
    {
        if (_templateRoot != null &&
            GodotObject.IsInstanceValid(_templateRoot) &&
            _templateCardContainer != null &&
            GodotObject.IsInstanceValid(_templateCardContainer))
        {
            return _templateCardContainer;
        }

        PackedScene? scene = LoadResource<PackedScene>(ChaosEffectsTemplatePath);
        if (scene == null)
            return null;

        if (scene.Instantiate<Control>() is not { } root)
            return null;

        _templateRoot = root;
        _templateCardContainer = root.GetNodeOrNull<Control>(TemplateCardContainerPath);
        if (_templateCardContainer == null)
        {
            root.QueueFree();
            _templateRoot = null;
        }

        return _templateCardContainer;
    }

    private static void EnsureDrawBefore(Control node, Control? reference)
    {
        if (reference?.GetParent() != node.GetParent() || node.GetParent() == null)
            return;

        int referenceIndex = reference.GetIndex();
        if (node.GetIndex() > referenceIndex)
            node.GetParent().MoveChild(node, referenceIndex);
    }

    private static void RemoveNode(Node parent, string nodeName)
    {
        DestroyNodeImmediately(parent.GetNodeOrNull<Node>(nodeName));

        if (parent is NCard cardNode)
        {
            Node overlayParent = GetOverlayParent(cardNode);
            if (overlayParent != parent)
                DestroyNodeImmediately(overlayParent.GetNodeOrNull<Node>(nodeName));
        }
    }

    private static Control? GetOverlayNode(Node parent, string nodeName)
    {
        Control? control = null;
        if (parent is NCard cardNode)
        {
            Node overlayParent = GetOverlayParent(cardNode);
            control = overlayParent.GetNodeOrNull<Control>(nodeName);
            if (control == null && overlayParent != parent)
                control = parent.GetNodeOrNull<Control>(nodeName);
        }
        else
        {
            control = parent.GetNodeOrNull<Control>(nodeName);
        }

        if (control == null)
            return null;

        if (!GodotObject.IsInstanceValid(control) || control.IsQueuedForDeletion())
        {
            DestroyNodeImmediately(control);
            return null;
        }

        return control;
    }

    private static Node GetOverlayParent(NCard cardNode)
    {
        Control? body = cardNode.Body;
        if (body != null && GodotObject.IsInstanceValid(body) && !body.IsQueuedForDeletion())
            return body;

        return cardNode;
    }

    private static void DestroyNodeImmediately(Node? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;

        node.GetParent()?.RemoveChild(node);
        node.Free();
    }

    private static void CaptureOriginalState(NCard cardNode)
    {
        var state = OriginalStates.GetOrCreateValue(cardNode);
        if (state.HasSnapshot && ReferenceEquals(state.CapturedModel, cardNode.Model))
            return;

        state.CapturedModel = cardNode.Model;
        state.Banner = CaptureControlSnapshot(Get<Control>(BannerField, cardNode));
        state.Frame = CaptureControlSnapshot(Get<Control>(FrameField, cardNode));
        state.Portrait = CaptureControlSnapshot(Get<Control>(PortraitField, cardNode));
        state.AncientPortrait = CaptureControlSnapshot(Get<Control>(AncientPortraitField, cardNode));
        state.PortraitBorder = CaptureControlSnapshot(Get<Control>(PortraitBorderField, cardNode));
        state.AncientBorder = CaptureControlSnapshot(Get<Control>(AncientBorderField, cardNode));
        state.AncientBanner = CaptureControlSnapshot(Get<Control>(AncientBannerField, cardNode));
        state.AncientTextBg = CaptureControlSnapshot(Get<Control>(AncientTextBgField, cardNode));
        state.AncientHighlight = CaptureControlSnapshot(Get<Control>(AncientHighlightField, cardNode));
        state.TitleLabel = CaptureControlSnapshot(Get<Control>(TitleLabelField, cardNode));
        state.EnergyIcon = CaptureControlSnapshot(Get<Control>(EnergyIconField, cardNode));
        state.DescriptionLabel = CaptureControlSnapshot(Get<Control>(DescriptionLabelField, cardNode));
        state.EnergyLabel = CaptureControlSnapshot(Get<Control>(EnergyLabelField, cardNode));
        state.TypeLabel = CaptureControlSnapshot(Get<Control>(TypeLabelField, cardNode));
        state.TypePlaque = CaptureControlSnapshot(Get<Control>(TypePlaqueField, cardNode));
        state.HasSnapshot = true;
    }

    private static void RestoreOriginalState(NCard cardNode, bool removeState = true)
    {
        if (!OriginalStates.TryGetValue(cardNode, out OriginalCardVisualState? state) || !state.HasSnapshot)
            return;

        bool restoreTextures = ReferenceEquals(state.CapturedModel, cardNode.Model);
        RestoreControlSnapshot(Get<Control>(BannerField, cardNode), state.Banner, restoreTextures);
        RestoreControlSnapshot(Get<Control>(FrameField, cardNode), state.Frame, restoreTextures);
        RestoreControlSnapshot(Get<Control>(PortraitField, cardNode), state.Portrait, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientPortraitField, cardNode), state.AncientPortrait, restoreTextures);
        RestoreControlSnapshot(Get<Control>(PortraitBorderField, cardNode), state.PortraitBorder, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientBorderField, cardNode), state.AncientBorder, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientBannerField, cardNode), state.AncientBanner, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientTextBgField, cardNode), state.AncientTextBg, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientHighlightField, cardNode), state.AncientHighlight, restoreTextures);
        RestoreControlSnapshot(Get<Control>(TitleLabelField, cardNode), state.TitleLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(EnergyIconField, cardNode), state.EnergyIcon, restoreTextures);
        RestoreControlSnapshot(Get<Control>(DescriptionLabelField, cardNode), state.DescriptionLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(EnergyLabelField, cardNode), state.EnergyLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(TypeLabelField, cardNode), state.TypeLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(TypePlaqueField, cardNode), state.TypePlaque, restoreTextures);
        RestoreControlSiblingOrder(new List<(Control? Control, ControlSnapshot? Snapshot)>
        {
            (Get<Control>(BannerField, cardNode), state.Banner),
            (Get<Control>(FrameField, cardNode), state.Frame),
            (Get<Control>(PortraitField, cardNode), state.Portrait),
            (Get<Control>(AncientPortraitField, cardNode), state.AncientPortrait),
            (Get<Control>(PortraitBorderField, cardNode), state.PortraitBorder),
            (Get<Control>(AncientBorderField, cardNode), state.AncientBorder),
            (Get<Control>(AncientBannerField, cardNode), state.AncientBanner),
            (Get<Control>(AncientTextBgField, cardNode), state.AncientTextBg),
            (Get<Control>(AncientHighlightField, cardNode), state.AncientHighlight),
            (Get<Control>(TitleLabelField, cardNode), state.TitleLabel),
            (Get<Control>(EnergyIconField, cardNode), state.EnergyIcon),
            (Get<Control>(DescriptionLabelField, cardNode), state.DescriptionLabel),
            (Get<Control>(EnergyLabelField, cardNode), state.EnergyLabel),
            (Get<Control>(TypeLabelField, cardNode), state.TypeLabel),
            (Get<Control>(TypePlaqueField, cardNode), state.TypePlaque)
        });
        if (removeState)
            OriginalStates.Remove(cardNode);
    }

    private static ControlSnapshot? CaptureControlSnapshot(Control? control)
    {
        if (control == null)
            return null;

        return new ControlSnapshot
        {
            Parent = control.GetParent(),
            SiblingIndex = control.GetIndex(),
            Position = control.Position,
            Size = control.Size,
            AnchorLeft = control.AnchorLeft,
            AnchorTop = control.AnchorTop,
            AnchorRight = control.AnchorRight,
            AnchorBottom = control.AnchorBottom,
            OffsetLeft = control.OffsetLeft,
            OffsetTop = control.OffsetTop,
            OffsetRight = control.OffsetRight,
            OffsetBottom = control.OffsetBottom,
            PivotOffset = control.PivotOffset,
            Rotation = control.Rotation,
            Scale = control.Scale,
            CustomMinimumSize = control.CustomMinimumSize,
            Visible = control.Visible,
            ZIndex = control.ZIndex,
            Modulate = control.Modulate,
            SelfModulate = control.SelfModulate,
            Texture = (control as TextureRect)?.Texture,
            Material = control.Material,
            TextureExpandMode = (control as TextureRect)?.ExpandMode,
            TextureStretchMode = (control as TextureRect)?.StretchMode,
            LabelHorizontalAlignment = (control as Label)?.HorizontalAlignment,
            LabelVerticalAlignment = (control as Label)?.VerticalAlignment,
            LabelAutowrapMode = (control as Label)?.AutowrapMode,
            LabelClipText = (control as Label)?.ClipText,
            LabelUppercase = (control as Label)?.Uppercase,
            RichTextScrollActive = (control as RichTextLabel)?.ScrollActive,
            RichTextFitContent = (control as RichTextLabel)?.FitContent,
            RichTextAutowrapMode = (control as RichTextLabel)?.AutowrapMode
        };
    }

    private static void RestoreControlSiblingOrder(List<(Control? Control, ControlSnapshot? Snapshot)> snapshots)
    {
        snapshots.Sort((left, right) =>
            (left.Snapshot?.SiblingIndex ?? int.MaxValue).CompareTo(right.Snapshot?.SiblingIndex ?? int.MaxValue));

        foreach ((Control? control, ControlSnapshot? snapshot) in snapshots)
            RestoreControlSiblingIndex(control, snapshot);
    }

    private static void RestoreControlSiblingIndex(Control? control, ControlSnapshot? snapshot)
    {
        if (control == null || snapshot?.Parent == null)
            return;

        if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(snapshot.Parent))
            return;

        Node? parent = control.GetParent();
        if (!ReferenceEquals(parent, snapshot.Parent))
            return;

        int childCount = parent.GetChildCount();
        if (childCount <= 0)
            return;

        int targetIndex = Math.Clamp(snapshot.SiblingIndex, 0, childCount - 1);
        if (control.GetIndex() != targetIndex)
            parent.MoveChild(control, targetIndex);
    }

    private static void RestoreControlSnapshot(Control? control, ControlSnapshot? snapshot, bool restoreTexture)
    {
        if (control == null || snapshot == null)
            return;

        control.Position = snapshot.Position;
        control.Size = snapshot.Size;
        control.AnchorLeft = snapshot.AnchorLeft;
        control.AnchorTop = snapshot.AnchorTop;
        control.AnchorRight = snapshot.AnchorRight;
        control.AnchorBottom = snapshot.AnchorBottom;
        control.OffsetLeft = snapshot.OffsetLeft;
        control.OffsetTop = snapshot.OffsetTop;
        control.OffsetRight = snapshot.OffsetRight;
        control.OffsetBottom = snapshot.OffsetBottom;
        control.PivotOffset = snapshot.PivotOffset;
        control.Rotation = snapshot.Rotation;
        control.Scale = snapshot.Scale;
        control.CustomMinimumSize = snapshot.CustomMinimumSize;
        control.Visible = snapshot.Visible;
        control.ZIndex = snapshot.ZIndex;
        control.Modulate = snapshot.Modulate;
        control.SelfModulate = snapshot.SelfModulate;
        control.Material = snapshot.Material;

        if (control is TextureRect textureRect)
        {
            if (restoreTexture)
                textureRect.Texture = snapshot.Texture;
            if (snapshot.TextureExpandMode.HasValue)
                textureRect.ExpandMode = snapshot.TextureExpandMode.Value;
            if (snapshot.TextureStretchMode.HasValue)
                textureRect.StretchMode = snapshot.TextureStretchMode.Value;
        }

        if (control is Label label)
        {
            if (snapshot.LabelHorizontalAlignment.HasValue)
                label.HorizontalAlignment = snapshot.LabelHorizontalAlignment.Value;
            if (snapshot.LabelVerticalAlignment.HasValue)
                label.VerticalAlignment = snapshot.LabelVerticalAlignment.Value;
            if (snapshot.LabelAutowrapMode.HasValue)
                label.AutowrapMode = snapshot.LabelAutowrapMode.Value;
            if (snapshot.LabelClipText.HasValue)
                label.ClipText = snapshot.LabelClipText.Value;
            if (snapshot.LabelUppercase.HasValue)
                label.Uppercase = snapshot.LabelUppercase.Value;
        }

        if (control is RichTextLabel richTextLabel)
        {
            if (snapshot.RichTextScrollActive.HasValue)
                richTextLabel.ScrollActive = snapshot.RichTextScrollActive.Value;
            if (snapshot.RichTextFitContent.HasValue)
                richTextLabel.FitContent = snapshot.RichTextFitContent.Value;
            if (snapshot.RichTextAutowrapMode.HasValue)
                richTextLabel.AutowrapMode = snapshot.RichTextAutowrapMode.Value;
        }
    }

    private static TextureRect? ResolveTextureRect(Control? control)
    {
        if (control is TextureRect textureRect)
            return textureRect;

        if (control == null)
            return null;

        foreach (Node child in control.GetChildren())
        {
            if (child is TextureRect childTextureRect)
                return childTextureRect;

            if (child is Control childControl)
            {
                var found = ResolveTextureRect(childControl);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static void BringToFront(Node child)
    {
        if (child.GetParent() == null)
            return;

        child.GetParent().MoveChild(child, child.GetParent().GetChildCount() - 1);
    }

    private static void BringCostOverlayToFront(NCard cardNode)
    {
        if (GetOverlayNode(cardNode, CostLineNodeName) is { } costLine)
            BringToFront(costLine);
        if (GetOverlayNode(cardNode, CostTextNodeName) is { } costText)
            BringToFront(costText);
        if (GetOverlayNode(cardNode, CostTextFallbackNodeName) is { } fallbackText)
            BringToFront(fallbackText);
    }

    private static Control? CreateTextureOverlay(NodeLayout layout)
    {
        var textureRect = new TextureRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            StretchMode = TextureRect.StretchModeEnum.Scale
        };
        ApplyLayout(textureRect, layout);
        return textureRect;
    }

    private static Control CreateLabelOverlay(NodeLayout layout)
    {
        var label = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        ApplyLayout(label, layout);
        return label;
    }

    private static string GetDisplayTypeText(CardModel cardModel)
    {
        return cardModel.Type.ToLocString().GetFormattedText();
    }

    private static void SetOverlayText(Control control, string text, bool sourceVisible, Control? source = null)
    {
        SetOverlayVisibility(control, sourceVisible, source);
        bool visible = sourceVisible && !string.IsNullOrWhiteSpace(text);
        if (control is Label label)
            label.Text = text;

        control.Visible = visible;
    }

    private static void SetOverlayVisibility(Control control, bool sourceVisible, Control? source = null)
    {
        control.Visible = sourceVisible;
        if (source == null)
            return;

        control.ZIndex = source.ZIndex;
    }

    private static void EnsureControlVisible(Control? control)
    {
        if (control != null)
            control.Visible = true;
    }

    private static void ApplyTextureRect(TextureRect? textureRect, string texturePath, Material? material, bool show)
    {
        if (textureRect == null)
            return;

        Texture2D? texture = LoadResource<Texture2D>(texturePath);
        if (texture != null)
            textureRect.Texture = texture;

        if (material != null)
            textureRect.Material = material;

        if (show)
            textureRect.Show();
    }

    private static string GetCategoryIconPath(CardType type)
    {
        string file = type switch
        {
            CardType.Attack => "icon_category_card_atk.png",
            CardType.Skill => "icon_category_card_skill.png",
            CardType.Power => "icon_category_card_power.png",
            CardType.Status => "icon_category_card_abnorm.png",
            CardType.Curse => "icon_category_card_curse.png",
            _ => "icon_category_card_potion.png"
        };

        return $"{ChaosEffectsBasePath}{file}";
    }

    private static string GetRarityBasePath(CardRarity rarity)
    {
        string suffix = rarity switch
        {
            CardRarity.Uncommon => "rare",
            CardRarity.Rare => "legend",
            CardRarity.Ancient => "unique",
            _ => "common"
        };

        return $"{ChaosEffectsBasePath}card_rarity_{suffix}.png";
    }

    private static string GetRaritySubPath(CardRarity rarity)
    {
        string suffix = rarity switch
        {
            CardRarity.Uncommon => "rare",
            CardRarity.Rare => "legend",
            CardRarity.Ancient => "unique",
            _ => "common"
        };

        return $"{ChaosEffectsBasePath}card_rarity_{suffix}_sub.png";
    }

    private static string GetRarityTitlePath(CardRarity rarity)
    {
        string suffix = rarity switch
        {
            CardRarity.Uncommon => "rare",
            CardRarity.Rare => "legend",
            CardRarity.Ancient => "unique",
            _ => "common"
        };

        return $"{ChaosEffectsBasePath}card_title_rarity_{suffix}.png";
    }

    private static string GetEnergyLinePath(CostAtlasVariant variant)
    {
        string file = variant switch
        {
            CostAtlasVariant.Red => "energy_line_up.png",
            CostAtlasVariant.Green => "energy_line_down.png",
            _ => "energy_line_default.png"
        };

        return $"{ChaosEffectsBasePath}{file}";
    }

    private static bool UsesAllFrameBadge(CardRarity rarity)
    {
        return rarity == CardRarity.Ancient;
    }

    private static T? LoadResource<T>(string? path) where T : Resource
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (ResourceCache.TryGetValue(path, out Resource? cached))
            return cached as T;

        if (!ResourceLoader.Exists(path))
        {
            if (MissingResourceWarnings.Add(path))
                GD.PushWarning($"[YukiCardCustomFrame] Missing resource: {path}");

            return null;
        }

        T? resource = ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Reuse);
        ResourceCache[path] = resource;
        return resource;
    }

    private static T? Get<T>(FieldInfo? field, NCard cardNode) where T : GodotObject
    {
        return field?.GetValue(cardNode) as T;
    }

    private static bool IsCustomFrameCard(NCard? cardNode)
    {
        return TryGetCustomFrameCard(cardNode, out _);
    }

    private static bool HasYukiVisualState(NCard cardNode)
    {
        if (OriginalStates.TryGetValue(cardNode, out OriginalCardVisualState? state) && state.HasSnapshot)
            return true;

        if (YukiCardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
            return true;

        return GetOverlayNode(cardNode, RarityBaseNodeName) != null ||
               GetOverlayNode(cardNode, RaritySubNodeName) != null ||
               GetOverlayNode(cardNode, EgoBadgeNodeName) != null ||
               GetOverlayNode(cardNode, EgoBadge2NodeName) != null ||
               GetOverlayNode(cardNode, FrameSparkNodeName) != null ||
               GetOverlayNode(cardNode, CostLineNodeName) != null ||
               GetOverlayNode(cardNode, CategoryIconNodeName) != null ||
               GetOverlayNode(cardNode, CategoryTextNodeName) != null ||
               GetOverlayNode(cardNode, CostTextNodeName) != null ||
               GetOverlayNode(cardNode, CostTextFallbackNodeName) != null ||
               GetOverlayNode(cardNode, UpgradeIconNodeName) != null ||
               GetOverlayNode(cardNode, DescriptionMaskNodeName) != null;
    }

    private static void SetPortraitTextureForOverlayState(TextureRect portrait, Texture2D? fallbackTexture)
    {
        portrait.Texture = fallbackTexture;
    }

    private static void ApplyDeferredIfValid(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree() || !cardNode.IsNodeReady())
            return;

        Apply(cardNode);
    }

    [HarmonyPatch(typeof(NCard), "Reload")]
    public static class ReloadPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(NCard __instance)
        {
            PrepareForBaseVisuals(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCard __instance)
        {
            Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    public static class UpdateVisualsPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(NCard __instance)
        {
            PrepareForBaseVisuals(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCard __instance)
        {
            Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(NCard), "_Ready")]
    public static class ReadyPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCard __instance)
        {
            Callable.From(() => ApplyDeferredIfValid(__instance)).CallDeferred();
        }
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    public static class OnFreedToPoolPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCard __instance)
        {
            RemoveChaosEffects(__instance, restoreOriginalState: true);
            YukiCardSpinePortraitPatch.RemoveSpineOverlay(__instance);
            OriginalStates.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.CancelPlayCard))]
    public static class CancelPlayCardPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCardPlay __instance)
        {
            NCard? cardNode = __instance.Holder?.CardNode;
            if (cardNode == null)
                return;

            Callable.From(() => ApplyDeferredIfValid(cardNode)).CallDeferred();
        }
    }

    [HarmonyPatch(typeof(NCardFlyVfx), "_Ready")]
    public static class CardFlyVfxReadyPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCardFlyVfx __instance)
        {
            if (CardFlyVfxCardField?.GetValue(__instance) is not NCard cardNode)
                return;

            Callable.From(() => ApplyDeferredIfValid(cardNode)).CallDeferred();
        }
    }

    [HarmonyPatch(typeof(NCard), "UpdateTypePlaqueSizeAndPosition")]
    public static class UpdateTypePlaqueSizeAndPositionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(NCard __instance)
        {
            return !IsCustomFrameCard(__instance);
        }
    }

    private readonly record struct NodeLayout(float Left, float Top, float Width, float Height, bool Visible = true)
    {
        public Vector2 Position => new(Left, Top);
        public Vector2 Size => new(Width, Height);
    }

    private enum CostAtlasVariant
    {
        Normal,
        Green,
        Red
    }

    private sealed class OriginalCardVisualState
    {
        public bool HasSnapshot { get; set; }
        public CardModel? CapturedModel { get; set; }
        public ControlSnapshot? Banner { get; set; }
        public ControlSnapshot? Frame { get; set; }
        public ControlSnapshot? Portrait { get; set; }
        public ControlSnapshot? AncientPortrait { get; set; }
        public ControlSnapshot? PortraitBorder { get; set; }
        public ControlSnapshot? AncientBorder { get; set; }
        public ControlSnapshot? AncientBanner { get; set; }
        public ControlSnapshot? AncientTextBg { get; set; }
        public ControlSnapshot? AncientHighlight { get; set; }
        public ControlSnapshot? TitleLabel { get; set; }
        public ControlSnapshot? EnergyIcon { get; set; }
        public ControlSnapshot? DescriptionLabel { get; set; }
        public ControlSnapshot? EnergyLabel { get; set; }
        public ControlSnapshot? TypeLabel { get; set; }
        public ControlSnapshot? TypePlaque { get; set; }
    }

    private sealed class ControlSnapshot
    {
        public Node? Parent { get; init; }
        public int SiblingIndex { get; init; }
        public Vector2 Position { get; init; }
        public Vector2 Size { get; init; }
        public float AnchorLeft { get; init; }
        public float AnchorTop { get; init; }
        public float AnchorRight { get; init; }
        public float AnchorBottom { get; init; }
        public float OffsetLeft { get; init; }
        public float OffsetTop { get; init; }
        public float OffsetRight { get; init; }
        public float OffsetBottom { get; init; }
        public Vector2 PivotOffset { get; init; }
        public float Rotation { get; init; }
        public Vector2 Scale { get; init; }
        public Vector2 CustomMinimumSize { get; init; }
        public bool Visible { get; init; }
        public int ZIndex { get; init; }
        public Color Modulate { get; init; }
        public Color SelfModulate { get; init; }
        public Texture2D? Texture { get; init; }
        public Material? Material { get; init; }
        public TextureRect.ExpandModeEnum? TextureExpandMode { get; init; }
        public TextureRect.StretchModeEnum? TextureStretchMode { get; init; }
        public HorizontalAlignment? LabelHorizontalAlignment { get; init; }
        public VerticalAlignment? LabelVerticalAlignment { get; init; }
        public TextServer.AutowrapMode? LabelAutowrapMode { get; init; }
        public bool? LabelClipText { get; init; }
        public bool? LabelUppercase { get; init; }
        public bool? RichTextScrollActive { get; init; }
        public bool? RichTextFitContent { get; init; }
        public TextServer.AutowrapMode? RichTextAutowrapMode { get; init; }
    }
}
