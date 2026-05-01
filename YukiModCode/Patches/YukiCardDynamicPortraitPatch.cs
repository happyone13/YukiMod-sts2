using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardDynamicPortraitPatch
{
    public const string OverlayNodeName = "YukiDynamicPortraitOverlay";
    private const string ViewportContainerNodeName = "ViewportContainer";
    private const float AncientInsetLeft = 7.0f;
    private const float AncientInsetTop = 7.0f;
    private const float AncientInsetRight = 7.0f;
    private const float AncientInsetBottom = 10.0f;

    public static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Dictionary<string, PackedScene> SceneCache = new();

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetDynamicPortraitScenePath(cardNode, out string? scenePath, out SpinePortraitSlot slot))
        {
            RemoveSpineOverlay(cardNode);
            return;
        }

        if (cardNode?.IsInsideTree() != true)
            return;

        TextureRect? targetPortrait = GetTargetPortrait(cardNode, slot);
        if (targetPortrait == null)
            return;

        if (HasActiveSpineOverlay(cardNode))
        {
            ForcePortraitSlot(cardNode, GetPortrait(cardNode), GetAncientPortrait(cardNode), slot);
            targetPortrait.Texture = null;
            UpdateOverlay(cardNode, targetPortrait);
            return;
        }

        ApplySpinePortrait(cardNode, scenePath!, slot);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (!TryGetDynamicPortraitScenePath(cardNode, out _, out _))
            RemoveSpineOverlay(cardNode);
    }

    public static bool HasActiveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
            return false;

        if (cardNode.GetNodeOrNull<Control>(OverlayNodeName) != null)
            return true;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait &&
            portrait.GetNodeOrNull<Control>(OverlayNodeName) != null)
        {
            return true;
        }

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait &&
            ancientPortrait.GetNodeOrNull<Control>(OverlayNodeName) != null)
        {
            return true;
        }

        return false;
    }

    public static void ForcePortraitSlot(NCard cardNode, TextureRect? portrait, TextureRect? ancientPortrait, SpinePortraitSlot slot)
    {
        if (portrait == null || ancientPortrait == null)
            return;

        portrait.Visible = slot != SpinePortraitSlot.Ancient;
        ancientPortrait.Visible = slot == SpinePortraitSlot.Ancient;

        if (slot == SpinePortraitSlot.Ancient)
            ancientPortrait.Texture = cardNode.Model?.Portrait;
    }

    public static void UpdateOverlay(NCard cardNode, TextureRect? portrait)
    {
        if (portrait == null)
            return;

        var container = portrait.GetNodeOrNull<Control>(OverlayNodeName);
        if (container == null)
            return;

        var subViewport = container.GetNodeOrNull<SubViewport>($"{ViewportContainerNodeName}/SubViewport");
        if (subViewport == null)
            return;

        SyncOverlayLayout(cardNode, portrait, container, subViewport);
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
    }

    public static bool ApplySpinePortrait(NCard cardNode, string scenePath, SpinePortraitSlot slot)
    {
        TextureRect? targetPortrait = GetTargetPortrait(cardNode, slot);
        if (targetPortrait == null)
            return false;

        RemoveSpineOverlay(cardNode);

        PackedScene? scene = GetOrCreateSpineScene(scenePath);
        if (scene == null)
            return false;

        if (scene.Instantiate<Node>() is not Node spineInstance)
            return false;

        SubViewportContainer? viewportContainer = GetViewportContainer(spineInstance);
        if (viewportContainer == null)
        {
            spineInstance.QueueFree();
            return false;
        }

        SubViewport? subViewport = viewportContainer.GetNodeOrNull<SubViewport>("SubViewport");
        if (subViewport == null)
        {
            spineInstance.QueueFree();
            return false;
        }

        ConfigureSubViewport(subViewport);
        PrepareViewportContainer(viewportContainer);

        if (viewportContainer.GetParent() != null)
            viewportContainer.GetParent()?.RemoveChild(viewportContainer);

        var overlay = new Control
        {
            Name = OverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = true,
            AnchorLeft = 0.0f,
            AnchorTop = 0.0f,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f
        };

        targetPortrait.ClipContents = true;
        overlay.AddChild(viewportContainer);
        targetPortrait.AddChild(overlay);
        spineInstance.QueueFree();

        targetPortrait.Texture = null;
        SyncOverlayLayout(cardNode, targetPortrait, overlay, subViewport);
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        var updater = new SpinePortraitUpdater();
        updater.Initialize(cardNode, overlay, subViewport);
        overlay.AddChild(updater);
        return true;
    }

    public static void RemoveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        RemoveAllSpineOverlays(cardNode);

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait)
            RemoveAllSpineOverlays(portrait);

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait)
            RemoveAllSpineOverlays(ancientPortrait);

        RestorePortraitTextures(cardNode);
    }

    private static bool TryGetDynamicPortraitScenePath(NCard? cardNode, out string? scenePath, out SpinePortraitSlot slot)
    {
        scenePath = null;
        slot = SpinePortraitSlot.Ancient;

        if (!YukiModConfig.UseYukiCardDynamicPortraits)
            return false;

        switch (cardNode?.Model)
        {
            case YukiModCard card when card.UseDynamicPortrait:
                scenePath = card.CustomSpinePortraitScenePath;
                slot = card.CustomSpinePortraitSlot;
                break;
            case YukiModTokenCard tokenCard when tokenCard.UseDynamicPortrait:
                scenePath = tokenCard.CustomSpinePortraitScenePath;
                slot = tokenCard.CustomSpinePortraitSlot;
                break;
            default:
                return false;
        }

        return !string.IsNullOrWhiteSpace(scenePath) && ResourceLoader.Exists(scenePath);
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

    private static TextureRect? GetTargetPortrait(NCard cardNode, SpinePortraitSlot slot)
    {
        var portrait = PortraitField?.GetValue(cardNode) as TextureRect;
        var ancientPortrait = AncientPortraitField?.GetValue(cardNode) as TextureRect;

        return slot == SpinePortraitSlot.Ancient
            ? ancientPortrait ?? portrait
            : portrait ?? ancientPortrait;
    }

    private static TextureRect? GetPortrait(NCard cardNode) => PortraitField?.GetValue(cardNode) as TextureRect;

    private static TextureRect? GetAncientPortrait(NCard cardNode) => AncientPortraitField?.GetValue(cardNode) as TextureRect;

    private static SubViewportContainer? GetViewportContainer(Node root)
    {
        if (root.GetNodeOrNull<SubViewportContainer>(ViewportContainerNodeName) is { } namedContainer)
            return namedContainer;

        if (root.GetNodeOrNull<SubViewportContainer>("SubViewportContainer") is { } altContainer)
            return altContainer;

        foreach (Node child in root.GetChildren())
        {
            if (child is SubViewportContainer container)
                return container;
        }

        return null;
    }

    private static void PrepareViewportContainer(SubViewportContainer viewportContainer)
    {
        viewportContainer.Name = ViewportContainerNodeName;
        viewportContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportContainer.Stretch = true;
        viewportContainer.ClipContents = true;
    }

    private static void ConfigureSubViewport(SubViewport subViewport)
    {
        subViewport.TransparentBg = true;
        if (subViewport.Size.X < 1 || subViewport.Size.Y < 1)
            subViewport.Size = new Vector2I(598, 844);
    }

    private static void SyncOverlayLayout(NCard cardNode, TextureRect portrait, Control container, SubViewport subViewport)
    {
        if (!GodotObject.IsInstanceValid(portrait) ||
            !GodotObject.IsInstanceValid(container) ||
            !GodotObject.IsInstanceValid(subViewport))
        {
            return;
        }

        bool isAncientPortrait = ReferenceEquals(AncientPortraitField?.GetValue(cardNode), portrait);
        Vector2 insetPosition = isAncientPortrait
            ? new Vector2(AncientInsetLeft, AncientInsetTop)
            : Vector2.Zero;
        Vector2 insetSize = isAncientPortrait
            ? new Vector2(
                Mathf.Max(0.0f, portrait.Size.X - AncientInsetLeft - AncientInsetRight),
                Mathf.Max(0.0f, portrait.Size.Y - AncientInsetTop - AncientInsetBottom))
            : portrait.Size;

        bool overlayParentIsPortrait = ReferenceEquals(container.GetParent(), portrait);
        container.Position = overlayParentIsPortrait ? insetPosition : portrait.Position + insetPosition;
        container.Size = insetSize;
        container.Scale = overlayParentIsPortrait ? Vector2.One : portrait.Scale;
        container.Rotation = overlayParentIsPortrait ? 0.0f : portrait.Rotation;
        container.PivotOffset = Vector2.Zero;

        if (container.GetNodeOrNull<SubViewportContainer>(ViewportContainerNodeName) is { } viewportContainer)
        {
            viewportContainer.Position = Vector2.Zero;
            viewportContainer.Size = container.Size;
        }
    }

    private static void RemoveAllSpineOverlays(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child.Name == OverlayNodeName && GodotObject.IsInstanceValid(child))
                child.Free();
        }
    }

    private static void RestorePortraitTextures(NCard cardNode)
    {
        Texture2D? portraitTexture = cardNode.Model?.Portrait;
        if (portraitTexture == null)
            return;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait && portrait.Texture == null)
            portrait.Texture = portraitTexture;

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait && ancientPortrait.Texture == null)
            ancientPortrait.Texture = portraitTexture;
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
}

public partial class SpinePortraitUpdater : Node
{
    private NCard _card = null!;
    private Control _container = null!;
    private SubViewport _subViewport = null!;

    public void Initialize(NCard card, Control container, SubViewport subViewport)
    {
        _card = card;
        _container = container;
        _subViewport = subViewport;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) ||
            !GodotObject.IsInstanceValid(_container) ||
            !GodotObject.IsInstanceValid(_subViewport))
        {
            QueueFree();
            return;
        }

        var portrait = _container.GetParent() as TextureRect;
        if (portrait != null)
            YukiCardDynamicPortraitPatch.UpdateOverlay(_card, portrait);
    }
}
