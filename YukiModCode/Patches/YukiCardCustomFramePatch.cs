using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class YukiCardCustomFrameUpdateVisualsPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void UpdateVisualsPrefix(NCard __instance)
    {
        YukiCardCustomFramePatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void UpdateVisualsPostfix(NCard __instance)
    {
        YukiCardCustomFramePatch.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
public static class YukiCardCustomFrameReloadPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void ReloadPrefix(NCard __instance)
    {
        YukiCardCustomFramePatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void ReloadPostfix(NCard __instance)
    {
        YukiCardCustomFramePatch.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "_EnterTree")]
public static class YukiCardCustomFrameEnterTreePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void EnterTreePostfix(NCard __instance)
    {
        YukiCardCustomFramePatch.Apply(__instance);
    }
}

internal static class YukiCardCustomFramePatch
{
    private static readonly FieldInfo? FrameField =
        typeof(NCard).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? PortraitBorderField =
        typeof(NCard).GetField("_portraitBorder", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? BannerField =
        typeof(NCard).GetField("_banner", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientBorderField =
        typeof(NCard).GetField("_ancientBorder", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientBannerField =
        typeof(NCard).GetField("_ancientBanner", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientTextBgField =
        typeof(NCard).GetField("_ancientTextBg", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientHighlightField =
        typeof(NCard).GetField("_ancientHighlight", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? TitleLabelField =
        typeof(NCard).GetField("_titleLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? EnergyIconField =
        typeof(NCard).GetField("_energyIcon", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? EnergyLabelField =
        typeof(NCard).GetField("_energyLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TypeLabelField =
        typeof(NCard).GetField("_typeLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TypePlaqueField =
        typeof(NCard).GetField("_typePlaque", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? DescriptionLabelField =
        typeof(NCard).GetField("_descriptionLabel", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Dictionary<string, Texture2D?> TextureCache = new();
    private static readonly ConditionalWeakTable<NCard, OriginalCardVisualState> OriginalStates = new();

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetYukiCard(cardNode, out CardModel cardModel))
        {
            RestoreOriginalState(cardNode);
            return;
        }

        if (cardNode?.IsInsideTree() != true)
            return;

        CaptureOriginalState(cardNode);
        ApplyYukiFrame(cardNode, cardModel);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        RestoreOriginalState(cardNode);
    }

    private static bool TryGetYukiCard(NCard? cardNode, out CardModel cardModel)
    {
        cardModel = null!;
        if (cardNode?.Model is not CardModel model || cardNode.Model is not IYukiCardVisualProfile)
            return false;

        cardModel = model;
        return true;
    }

    private static void ApplyYukiFrame(NCard cardNode, CardModel cardModel)
    {
        var frame = Get<TextureRect>(FrameField, cardNode);
        var portrait = Get<TextureRect>(PortraitField, cardNode);
        var ancientPortrait = Get<TextureRect>(AncientPortraitField, cardNode);
        var portraitBorder = Get<TextureRect>(PortraitBorderField, cardNode);
        var banner = Get<TextureRect>(BannerField, cardNode);
        var ancientBorder = Get<TextureRect>(AncientBorderField, cardNode);
        var ancientBanner = Get<Control>(AncientBannerField, cardNode);
        var ancientTextBg = Get<TextureRect>(AncientTextBgField, cardNode);
        var ancientHighlight = Get<TextureRect>(AncientHighlightField, cardNode);

        frame?.Hide();
        portrait?.Hide();
        banner?.Hide();

        if (portraitBorder != null)
        {
            SetTexture(portraitBorder, YukiCardFramePaths.GetPortraitBorderTexturePath());
            portraitBorder.Show();
        }

        if (ancientPortrait != null)
        {
            ancientPortrait.Texture = cardModel.Portrait;
            ancientPortrait.Show();
        }

        SetTexture(ancientBorder, YukiCardFramePaths.GetAncientBorderTexturePathForTypeAndRarity(cardModel.Type, cardModel.Rarity));
        SetTexture(ancientTextBg, YukiCardFramePaths.GetAncientTextBgPathForType(cardModel.Type));
        SetTexture(ancientHighlight, YukiCardFramePaths.GetAncientHighlightTexturePath());

        if (ancientBanner != null)
        {
            ancientBanner.Show();
            if (cardModel.BannerMaterial != null)
                ancientBanner.Material = cardModel.BannerMaterial;

            if (FindTextureRectInNode(ancientBanner) is TextureRect bannerTexture)
                SetTexture(bannerTexture, YukiCardFramePaths.GetAncientBannerTexturePathForType(cardModel.Type));
        }

        EnsureControlVisible(TitleLabelField, cardNode);
        EnsureControlVisible(EnergyIconField, cardNode);
        EnsureControlVisible(EnergyLabelField, cardNode);
        EnsureControlVisible(TypeLabelField, cardNode);
        EnsureControlVisible(TypePlaqueField, cardNode);
        EnsureControlVisible(DescriptionLabelField, cardNode);
    }

    private static void CaptureOriginalState(NCard cardNode)
    {
        var state = OriginalStates.GetOrCreateValue(cardNode);
        if (state.HasSnapshot && ReferenceEquals(state.CapturedModel, cardNode.Model))
            return;

        state.CapturedModel = cardNode.Model;
        state.Frame = CaptureControlSnapshot(Get<Control>(FrameField, cardNode));
        state.Portrait = CaptureControlSnapshot(Get<Control>(PortraitField, cardNode));
        state.AncientPortrait = CaptureControlSnapshot(Get<Control>(AncientPortraitField, cardNode));
        state.PortraitBorder = CaptureControlSnapshot(Get<Control>(PortraitBorderField, cardNode));
        state.Banner = CaptureControlSnapshot(Get<Control>(BannerField, cardNode));
        state.AncientBorder = CaptureControlSnapshot(Get<Control>(AncientBorderField, cardNode));
        state.AncientBanner = CaptureControlSnapshot(Get<Control>(AncientBannerField, cardNode));
        state.AncientTextBg = CaptureControlSnapshot(Get<Control>(AncientTextBgField, cardNode));
        state.AncientHighlight = CaptureControlSnapshot(Get<Control>(AncientHighlightField, cardNode));
        state.TitleLabel = CaptureControlSnapshot(Get<Control>(TitleLabelField, cardNode));
        state.EnergyIcon = CaptureControlSnapshot(Get<Control>(EnergyIconField, cardNode));
        state.EnergyLabel = CaptureControlSnapshot(Get<Control>(EnergyLabelField, cardNode));
        state.TypeLabel = CaptureControlSnapshot(Get<Control>(TypeLabelField, cardNode));
        state.TypePlaque = CaptureControlSnapshot(Get<Control>(TypePlaqueField, cardNode));
        state.DescriptionLabel = CaptureControlSnapshot(Get<Control>(DescriptionLabelField, cardNode));
        state.HasSnapshot = true;
    }

    private static void RestoreOriginalState(NCard? cardNode)
    {
        if (cardNode == null || !OriginalStates.TryGetValue(cardNode, out OriginalCardVisualState? state) || !state.HasSnapshot)
            return;

        RestoreControlSnapshot(Get<Control>(FrameField, cardNode), state.Frame, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(PortraitField, cardNode), state.Portrait, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(AncientPortraitField, cardNode), state.AncientPortrait, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(PortraitBorderField, cardNode), state.PortraitBorder, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(BannerField, cardNode), state.Banner, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(AncientBorderField, cardNode), state.AncientBorder, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(AncientBannerField, cardNode), state.AncientBanner, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(AncientTextBgField, cardNode), state.AncientTextBg, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(AncientHighlightField, cardNode), state.AncientHighlight, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(TitleLabelField, cardNode), state.TitleLabel, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(EnergyIconField, cardNode), state.EnergyIcon, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(EnergyLabelField, cardNode), state.EnergyLabel, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(TypeLabelField, cardNode), state.TypeLabel, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(TypePlaqueField, cardNode), state.TypePlaque, restoreTexture: true);
        RestoreControlSnapshot(Get<Control>(DescriptionLabelField, cardNode), state.DescriptionLabel, restoreTexture: true);
        OriginalStates.Remove(cardNode);
    }

    private static ControlSnapshot? CaptureControlSnapshot(Control? control)
    {
        if (control == null)
            return null;

        return new ControlSnapshot
        {
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
            ClipContents = control.ClipContents,
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
        control.ClipContents = snapshot.ClipContents;
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

    private static void SetTexture(TextureRect? textureRect, string texturePath)
    {
        if (textureRect == null)
            return;

        var texture = LoadTexture(texturePath);
        if (texture != null)
            textureRect.Texture = texture;

        textureRect.Show();
    }

    private static Texture2D? LoadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (TextureCache.TryGetValue(path, out var cached))
            return cached;

        var texture = GD.Load<Texture2D>(path);
        if (texture != null)
        {
            TextureCache[path] = texture;
            return texture;
        }

        try
        {
            string filePath = ProjectSettings.GlobalizePath(path);
            if (Godot.FileAccess.FileExists(filePath))
            {
                var image = Image.LoadFromFile(filePath);
                if (image != null)
                {
                    var imageTexture = ImageTexture.CreateFromImage(image);
                    TextureCache[path] = imageTexture;
                    return imageTexture;
                }
            }
        }
        catch
        {
            // Keep the fallback silent; the caller will simply keep the existing node texture.
        }

        TextureCache[path] = null;
        return null;
    }

    private static T? Get<T>(FieldInfo? field, NCard cardNode) where T : GodotObject
    {
        return field?.GetValue(cardNode) as T;
    }

    private static TextureRect? FindTextureRectInNode(Node node)
    {
        if (node is TextureRect textureRect)
            return textureRect;

        foreach (Node child in node.GetChildren())
        {
            var result = FindTextureRectInNode(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void EnsureControlVisible(FieldInfo? field, NCard cardNode)
    {
        if (field?.GetValue(cardNode) is Control control)
            control.Show();
    }

    private sealed class OriginalCardVisualState
    {
        public bool HasSnapshot { get; set; }
        public CardModel? CapturedModel { get; set; }
        public ControlSnapshot? Frame { get; set; }
        public ControlSnapshot? Portrait { get; set; }
        public ControlSnapshot? AncientPortrait { get; set; }
        public ControlSnapshot? PortraitBorder { get; set; }
        public ControlSnapshot? Banner { get; set; }
        public ControlSnapshot? AncientBorder { get; set; }
        public ControlSnapshot? AncientBanner { get; set; }
        public ControlSnapshot? AncientTextBg { get; set; }
        public ControlSnapshot? AncientHighlight { get; set; }
        public ControlSnapshot? TitleLabel { get; set; }
        public ControlSnapshot? EnergyIcon { get; set; }
        public ControlSnapshot? EnergyLabel { get; set; }
        public ControlSnapshot? TypeLabel { get; set; }
        public ControlSnapshot? TypePlaque { get; set; }
        public ControlSnapshot? DescriptionLabel { get; set; }
    }

    private sealed class ControlSnapshot
    {
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
        public bool ClipContents { get; init; }
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
