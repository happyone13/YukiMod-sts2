using System.Collections.Generic;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardSpinePortraitPatch
{
    public const string SpineOverlayNodeName = "YukiSpinePortraitOverlay";
    private const string SpineViewportContainerNodeName = "ViewportContainer";
    private const string OverlayScenePathMetaKey = "yuki_spine_scene_path";

    public static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Dictionary<string, PackedScene> SceneCache = new();
    private static readonly HashSet<string> MissingResourceWarnings = new();

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetSpineScenePath(cardNode, out string? scenePath))
        {
            RemoveSpineOverlay(cardNode);
            return;
        }

        if (cardNode?.Model is not IYukiCardVisualProfile profile)
            return;

        TextureRect? portrait = GetTargetPortrait(cardNode, profile.CustomSpinePortraitSlot);
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return;

        if (!HasActiveSpineOverlay(cardNode) &&
            !AttachSpineOverlay(cardNode, portrait, scenePath!))
        {
            return;
        }

        SetPortraitTexture(portrait, null);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (TryGetSpineScenePath(cardNode, out _))
            return;

        RemoveSpineOverlay(cardNode);
    }

    public static void RemoveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
            return;

        RemoveOverlayFromPortrait(PortraitField?.GetValue(cardNode) as TextureRect);
        RemoveOverlayFromPortrait(AncientPortraitField?.GetValue(cardNode) as TextureRect);

        if (cardNode.Model?.Portrait is Texture2D portraitTexture)
        {
            if (PortraitField?.GetValue(cardNode) is TextureRect portrait && portrait.Texture == null)
                portrait.Texture = portraitTexture;

            if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait && ancientPortrait.Texture == null)
                ancientPortrait.Texture = portraitTexture;
        }
    }

    public static bool HasActiveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
            return false;

        if (cardNode.GetNodeOrNull<Control>(SpineOverlayNodeName) != null)
            return true;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait &&
            portrait.GetNodeOrNull<Control>(SpineOverlayNodeName) != null)
        {
            return true;
        }

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait &&
            ancientPortrait.GetNodeOrNull<Control>(SpineOverlayNodeName) != null)
        {
            return true;
        }

        return false;
    }

    internal static bool TryGetSpineScenePath(NCard? cardNode, out string? scenePath)
    {
        scenePath = null;

        if (cardNode?.Model is not IYukiCardVisualProfile profile)
            return false;

        scenePath = profile.CustomSpinePortraitScenePath;
        if (string.IsNullOrWhiteSpace(scenePath))
            return false;

        if (!ResourceLoader.Exists(scenePath))
        {
            if (MissingResourceWarnings.Add(scenePath))
                GD.PushWarning($"[YukiCardSpinePortrait] Scene path missing or not found: {scenePath}");

            return false;
        }

        return true;
    }

    private static bool AttachSpineOverlay(NCard cardNode, TextureRect portrait, string scenePath)
    {
        PackedScene? scene = GetOrCreateSpineScene(scenePath);
        if (scene == null)
            return false;

        if (scene.Instantiate<Node>() is not Node root)
            return false;

        SubViewportContainer? viewportContainer = GetViewportContainer(root);
        if (viewportContainer == null)
        {
            root.QueueFree();
            return false;
        }

        if (viewportContainer.GetNodeOrNull<SubViewport>(SubViewportNodeName()) is not SubViewport subViewport)
        {
            root.QueueFree();
            return false;
        }

        ConfigureViewportContainer(viewportContainer);
        ConfigureSubViewport(subViewport);

        if (viewportContainer.GetParent() != null)
            viewportContainer.GetParent()?.RemoveChild(viewportContainer);

        RemoveOverlayFromPortrait(portrait);

        var overlay = new Control
        {
            Name = SpineOverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = true
        };
        overlay.SetMeta(OverlayScenePathMetaKey, scenePath);
        portrait.AddChild(overlay);
        overlay.AddChild(viewportContainer);

        overlay.AnchorLeft = 0f;
        overlay.AnchorTop = 0f;
        overlay.AnchorRight = 1f;
        overlay.AnchorBottom = 1f;
        overlay.OffsetLeft = 0f;
        overlay.OffsetTop = 0f;
        overlay.OffsetRight = 0f;
        overlay.OffsetBottom = 0f;
        overlay.Position = Vector2.Zero;
        overlay.Size = portrait.Size;

        viewportContainer.AnchorLeft = 0f;
        viewportContainer.AnchorTop = 0f;
        viewportContainer.AnchorRight = 1f;
        viewportContainer.AnchorBottom = 1f;
        viewportContainer.OffsetLeft = 0f;
        viewportContainer.OffsetTop = 0f;
        viewportContainer.OffsetRight = 0f;
        viewportContainer.OffsetBottom = 0f;
        viewportContainer.Position = Vector2.Zero;
        viewportContainer.Size = overlay.Size;

        portrait.ClipContents = true;
        SetPortraitTexture(portrait, null);

        return true;
    }

    private static void RemoveOverlayFromPortrait(TextureRect? portrait)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return;

        var overlay = portrait.GetNodeOrNull<Control>(SpineOverlayNodeName);
        if (overlay != null)
            overlay.QueueFree();
    }

    private static void SetPortraitTexture(TextureRect? portrait, Texture2D? texture)
    {
        if (portrait != null)
            portrait.Texture = texture;
    }

    private static PackedScene? GetOrCreateSpineScene(string scenePath)
    {
        if (SceneCache.TryGetValue(scenePath, out PackedScene? scene))
            return scene;

        scene = GD.Load<PackedScene>(scenePath);
        if (scene == null)
            return null;

        SceneCache[scenePath] = scene;
        return scene;
    }

    private static SubViewportContainer? GetViewportContainer(Node root)
    {
        if (root.GetNodeOrNull<SubViewportContainer>(SpineViewportContainerNodeName) is { } viewportContainer)
            return viewportContainer;

        if (root.GetNodeOrNull<SubViewportContainer>("SubViewportContainer") is { } altViewportContainer)
            return altViewportContainer;

        foreach (Node child in root.GetChildren())
        {
            if (child is SubViewportContainer container)
                return container;
        }

        return null;
    }

    private static void ConfigureViewportContainer(SubViewportContainer viewportContainer)
    {
        viewportContainer.Name = SpineViewportContainerNodeName;
        viewportContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportContainer.Stretch = true;
        viewportContainer.ClipContents = true;
    }

    private static void ConfigureSubViewport(SubViewport subViewport)
    {
        subViewport.TransparentBg = true;
        subViewport.HandleInputLocally = false;
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        if (subViewport.Size.X < 1 || subViewport.Size.Y < 1)
            subViewport.Size = new Vector2I(598, 844);
    }

    private static TextureRect? GetTargetPortrait(NCard cardNode, YukiSpinePortraitSlot slot)
    {
        var portrait = PortraitField?.GetValue(cardNode) as TextureRect;
        var ancientPortrait = AncientPortraitField?.GetValue(cardNode) as TextureRect;

        return slot switch
        {
            YukiSpinePortraitSlot.Normal => portrait ?? ancientPortrait,
            _ => ancientPortrait ?? portrait
        };
    }

    private static string SubViewportNodeName() => "SubViewport";
}
