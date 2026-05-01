using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardCustomFramePatch
{
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

    private static readonly Dictionary<string, Resource?> ResourceCache = new();
    private static readonly HashSet<string> MissingResourceWarnings = new();
    private static readonly ConditionalWeakTable<NCard, OriginalVisualState> OriginalStates = new();

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        RestoreOriginalState(cardNode);
    }

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetCustomFrameCard(cardNode))
            return;

        CaptureOriginalState(cardNode!);

        var frame = Get<TextureRect>(FrameField, cardNode!);
        var portrait = Get<TextureRect>(PortraitField, cardNode!);
        var portraitBorder = Get<TextureRect>(PortraitBorderField, cardNode!);
        var banner = Get<TextureRect>(BannerField, cardNode!);
        var ancientBorder = Get<TextureRect>(AncientBorderField, cardNode!);
        var ancientTextBg = Get<TextureRect>(AncientTextBgField, cardNode!);
        var ancientBanner = Get<Control>(AncientBannerField, cardNode!);
        var ancientHighlight = Get<TextureRect>(AncientHighlightField, cardNode!);
        var ancientPortrait = Get<TextureRect>(AncientPortraitField, cardNode!);

        Material? frameMaterial = LoadResource<Material>(YukiCardFramePaths.FrameMaterialPath);
        Material? bannerMaterial = LoadResource<Material>(YukiCardFramePaths.BannerMaterialPath);

        if (frame != null)
            frame.Hide();

        if (portraitBorder != null)
            portraitBorder.Hide();

        if (banner != null)
            banner.Hide();

        if (portrait != null)
            portrait.Hide();

        if (ancientPortrait != null)
        {
            ancientPortrait.Show();
            if (!YukiCardDynamicPortraitPatch.HasActiveSpineOverlay(cardNode))
                ancientPortrait.Texture = cardNode!.Model?.Portrait;
        }

        ApplyTextureRect(ancientBorder, YukiCardFramePaths.AncientBorderTexturePath, frameMaterial, show: true);
        ApplyTextureRect(ancientTextBg, YukiCardFramePaths.GetAncientTextBgTexturePath(cardNode!.Model!.Type), frameMaterial, show: true);
        ApplyTextureRect(ancientHighlight, YukiCardFramePaths.AncientHighlightTexturePath, material: null, show: true);

        if (ancientBanner != null)
        {
            ancientBanner.Show();
            ancientBanner.Material = bannerMaterial;
            if (ResolveTextureRect(ancientBanner) is { } bannerTextureRect)
                ApplyTextureRect(bannerTextureRect, YukiCardFramePaths.AncientBannerTexturePath, bannerMaterial, show: true);
        }
    }

    private static bool TryGetCustomFrameCard(NCard? cardNode)
    {
        if (!YukiModConfig.UseYukiCardDynamicPortraits)
            return false;

        if (cardNode?.Model is YukiModCard card)
            return card.UseCustomFrame;

        if (cardNode?.Model is YukiModTokenCard tokenCard)
            return tokenCard.UseCustomFrame;

        return false;
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

    private static void CaptureOriginalState(NCard cardNode)
    {
        var state = OriginalStates.GetOrCreateValue(cardNode);
        if (state.HasSnapshot)
            return;

        state.Frame = CaptureTextureState(Get<TextureRect>(FrameField, cardNode));
        state.Portrait = CaptureTextureState(Get<TextureRect>(PortraitField, cardNode));
        state.PortraitBorder = CaptureTextureState(Get<TextureRect>(PortraitBorderField, cardNode));
        state.Banner = CaptureTextureState(Get<TextureRect>(BannerField, cardNode));
        state.AncientBorder = CaptureTextureState(Get<TextureRect>(AncientBorderField, cardNode));
        state.AncientTextBg = CaptureTextureState(Get<TextureRect>(AncientTextBgField, cardNode));
        state.AncientBanner = CaptureTextureState(ResolveTextureRect(Get<Control>(AncientBannerField, cardNode)));
        state.AncientHighlight = CaptureTextureState(Get<TextureRect>(AncientHighlightField, cardNode));
        state.AncientPortrait = CaptureTextureState(Get<TextureRect>(AncientPortraitField, cardNode));
        state.HasSnapshot = true;
    }

    private static void RestoreOriginalState(NCard cardNode)
    {
        if (!OriginalStates.TryGetValue(cardNode, out OriginalVisualState? state) || !state.HasSnapshot)
            return;

        RestoreTextureState(Get<TextureRect>(FrameField, cardNode), state.Frame);
        RestoreTextureState(Get<TextureRect>(PortraitField, cardNode), state.Portrait);
        RestoreTextureState(Get<TextureRect>(PortraitBorderField, cardNode), state.PortraitBorder);
        RestoreTextureState(Get<TextureRect>(BannerField, cardNode), state.Banner);
        RestoreTextureState(Get<TextureRect>(AncientBorderField, cardNode), state.AncientBorder);
        RestoreTextureState(Get<TextureRect>(AncientTextBgField, cardNode), state.AncientTextBg);
        RestoreTextureState(ResolveTextureRect(Get<Control>(AncientBannerField, cardNode)), state.AncientBanner);
        RestoreTextureState(Get<TextureRect>(AncientHighlightField, cardNode), state.AncientHighlight);
        RestoreTextureState(Get<TextureRect>(AncientPortraitField, cardNode), state.AncientPortrait);
    }

    private static TextureSnapshot? CaptureTextureState(TextureRect? textureRect)
    {
        if (textureRect == null)
            return null;

        return new TextureSnapshot
        {
            Visible = textureRect.Visible,
            Texture = textureRect.Texture,
            Material = textureRect.Material
        };
    }

    private static void RestoreTextureState(TextureRect? textureRect, TextureSnapshot? snapshot)
    {
        if (textureRect == null || snapshot == null)
            return;

        textureRect.Visible = snapshot.Visible;
        textureRect.Texture = snapshot.Texture;
        textureRect.Material = snapshot.Material;
    }

    private static T? LoadResource<T>(string path) where T : Resource
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

        T? resource = GD.Load<T>(path);
        ResourceCache[path] = resource;
        return resource;
    }

    private static T? Get<T>(FieldInfo? field, NCard cardNode) where T : GodotObject
    {
        return field?.GetValue(cardNode) as T;
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

    [HarmonyPatch(typeof(NCard), "_EnterTree")]
    public static class EnterTreePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCard __instance)
        {
            Apply(__instance);
        }
    }

    private sealed class OriginalVisualState
    {
        public bool HasSnapshot { get; set; }
        public TextureSnapshot? Frame { get; set; }
        public TextureSnapshot? Portrait { get; set; }
        public TextureSnapshot? PortraitBorder { get; set; }
        public TextureSnapshot? Banner { get; set; }
        public TextureSnapshot? AncientBorder { get; set; }
        public TextureSnapshot? AncientTextBg { get; set; }
        public TextureSnapshot? AncientBanner { get; set; }
        public TextureSnapshot? AncientHighlight { get; set; }
        public TextureSnapshot? AncientPortrait { get; set; }
    }

    private sealed class TextureSnapshot
    {
        public bool Visible { get; set; }
        public Texture2D? Texture { get; set; }
        public Material? Material { get; set; }
    }
}
