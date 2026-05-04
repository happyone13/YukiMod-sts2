using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardSpinePortraitPatch
{
    public const string SpineOverlayNodeName = "YukiSpinePortraitOverlay";
    internal const string SpineViewportContainerNodeName = "ViewportContainer";
    private const string OverlayScenePathMetaKey = "yuki_spine_scene_path";
    private const string OverlayModelIdentityMetaKey = "yuki_spine_model_identity";
    private const string OverlayTargetSlotMetaKey = "yuki_target_slot";
    private const string OverlayTargetSlotAncient = "ancient";
    private const string OverlayTargetSlotNormal = "normal";
    private const float AncientOverlayInsetLeft = 7.0f;
    private const float AncientOverlayInsetTop = 7.0f;
    private const float AncientOverlayInsetRight = 7.0f;
    private const float AncientOverlayInsetBottom = 10.0f;
    private const int WarmUpFrames = 3;

    public static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly HashSet<string> MissingResourceWarnings = new();
    private static readonly ConditionalWeakTable<NCard, PortraitVisibilityState> VisibilityStates = new();
    private static readonly FieldInfo? NCardHolderIsHoveredField =
        typeof(NCardHolder).GetField("_isHovered", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? NCardHolderIsFocusedField =
        typeof(NCardHolder).GetField("_isFocused", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? NCardHolderCurrentPressedActionField =
        typeof(NCardHolder).GetField("_currentPressedAction", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Apply(NCard? cardNode)
    {
        if (!YukiModConfig.UseDynamicCardPortraits)
        {
            RemoveSpineOverlay(cardNode);
            return;
        }

        if (!TryGetSpineScenePath(cardNode, out string? scenePath))
        {
            RemoveSpineOverlay(cardNode);
            return;
        }

        if (cardNode?.Model is not IYukiCardVisualProfile profile || cardNode.IsInsideTree() != true)
            return;

        ApplySpinePortrait(cardNode, scenePath!, profile.CustomSpinePortraitSlot);
    }

    public static bool ApplySpinePortrait(NCard cardNode, string scenePath, YukiSpinePortraitSlot slot)
    {
        if (!GodotObject.IsInstanceValid(cardNode) ||
            cardNode.Model is not IYukiCardVisualProfile profile ||
            string.IsNullOrWhiteSpace(profile.CustomSpinePortraitScenePath) ||
            !string.Equals(profile.CustomSpinePortraitScenePath, scenePath, System.StringComparison.Ordinal))
        {
            return false;
        }

        TextureRect? portrait = GetTargetPortrait(cardNode, slot);
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return false;

        string? activeScenePath = GetActiveSpineOverlayScenePath(portrait);
        int currentModelIdentity = GetModelIdentity(cardNode.Model);
        int? activeModelIdentity = GetActiveSpineOverlayModelIdentity(portrait);
        if (HasActiveSpineOverlay(cardNode) &&
            (!string.Equals(activeScenePath, scenePath, System.StringComparison.Ordinal) ||
             activeModelIdentity != currentModelIdentity))
        {
            RemoveSpineOverlay(cardNode);
        }

        if (HasActiveSpineOverlay(cardNode))
            return true;

        PackedScene? scene = LoadSpineScene(scenePath);
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

        ConfigureViewportContainer(viewportContainer);
        ConfigureSubViewport(subViewport);

        if (viewportContainer.GetParent() != null)
            viewportContainer.GetParent()?.RemoveChild(viewportContainer);

        RemoveOverlayFromPortrait(portrait);

        var overlay = new Control
        {
            Name = SpineOverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 0,
            ClipContents = true,
            AnchorLeft = 0.0f,
            AnchorTop = 0.0f,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            Modulate = Colors.White,
            SelfModulate = Colors.White
        };
        overlay.SetMeta(OverlayScenePathMetaKey, scenePath);
        overlay.SetMeta(OverlayModelIdentityMetaKey, currentModelIdentity.ToString());
        overlay.SetMeta(
            OverlayTargetSlotMetaKey,
            ReferenceEquals(AncientPortraitField?.GetValue(cardNode), portrait)
                ? OverlayTargetSlotAncient
                : OverlayTargetSlotNormal);

        portrait.ClipContents = true;
        overlay.AddChild(viewportContainer);
        portrait.AddChild(overlay);
        spineInstance.QueueFree();

        portrait.Texture = null;
        SyncOverlayLayout(cardNode, portrait, overlay, subViewport);
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        var updater = new SpinePortraitUpdater();
        updater.Initialize(cardNode, overlay, subViewport);
        overlay.AddChild(updater);
        return true;
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (!YukiModConfig.UseDynamicCardPortraits)
        {
            RemoveSpineOverlay(cardNode);
            return;
        }

        if (TryGetSpineScenePath(cardNode, out _))
            return;

        RemoveSpineOverlay(cardNode);
    }

    public static void RemoveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
            return;

        RemoveAllSpineOverlays(cardNode);

        if (PortraitField?.GetValue(cardNode) is TextureRect portraitRect)
            RemoveAllSpineOverlays(portraitRect);

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortraitRect)
            RemoveAllSpineOverlays(ancientPortraitRect);

        RestorePortraitTextures(cardNode);
        RestorePortraitVisibility(cardNode);
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

    public static bool ShouldDisplayCustomUi(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree())
            return false;

        bool hasHolderAncestor = false;
        bool isHolderActive = false;
        bool isInCardPlay = false;
        bool isPreviewHolder = false;
        bool isHoverTipCard = false;

        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard);

        if (isInCardPlay)
            return false;

        bool isEnlarged = ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;
        return hasHolderAncestor || isPreviewHolder || isHoverTipCard || isEnlarged;
    }

    public static bool ShouldDisplayDynamicOverlays(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree())
            return false;

        bool hasHolderAncestor = false;
        bool isHolderActive = false;
        bool isInCardPlay = false;
        bool isPreviewHolder = false;
        bool isHoverTipCard = false;

        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard);

        bool isEnlarged = ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;
        return isHolderActive || isPreviewHolder || isHoverTipCard || isInCardPlay || isEnlarged;
    }

    public static bool IsPreviewHolderContext(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree())
            return false;

        bool hasHolderAncestor = false;
        bool isHolderActive = false;
        bool isInCardPlay = false;
        bool isPreviewHolder = false;
        bool isHoverTipCard = false;
        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard);
        return isPreviewHolder;
    }

    public static bool IsHoverTipContext(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree())
            return false;

        bool hasHolderAncestor = false;
        bool isHolderActive = false;
        bool isInCardPlay = false;
        bool isPreviewHolder = false;
        bool isHoverTipCard = false;
        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard);
        return isHoverTipCard;
    }

    public static void ForcePortraitSlot(
        NCard cardNode,
        TextureRect? portrait,
        TextureRect? ancientPortrait,
        YukiSpinePortraitSlot slot)
    {
        if (portrait == null || ancientPortrait == null)
            return;

        if (slot == YukiSpinePortraitSlot.Ancient && !HasActiveSpineOverlay(cardNode))
        {
            portrait.Visible = true;
            ancientPortrait.Visible = false;
            return;
        }

        var state = VisibilityStates.GetOrCreateValue(cardNode);
        if (!state.HasSnapshot)
        {
            state.PortraitVisible = portrait.Visible;
            state.AncientPortraitVisible = ancientPortrait.Visible;
            state.HasSnapshot = true;
        }

        portrait.Visible = slot != YukiSpinePortraitSlot.Ancient;
        ancientPortrait.Visible = slot == YukiSpinePortraitSlot.Ancient;
    }

    public static void UpdateSpineAnimationState(
        NCard cardNode,
        Control container,
        SubViewport subViewport,
        int framesSinceCreated)
    {
        if (!GodotObject.IsInstanceValid(cardNode) ||
            !GodotObject.IsInstanceValid(container) ||
            !GodotObject.IsInstanceValid(subViewport))
        {
            return;
        }

        var parentPortrait = ResolveOverlayTargetPortrait(cardNode, container);
        if (parentPortrait != null)
            SyncOverlayLayout(cardNode, parentPortrait, container, subViewport);

        if (framesSinceCreated < WarmUpFrames)
        {
            if (subViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
                subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            return;
        }

        if (subViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
            subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
    }

    public static void UpdateOverlay(NCard cardNode, TextureRect? portrait)
    {
        if (portrait == null)
            return;

        var container = cardNode.GetNodeOrNull<Control>(SpineOverlayNodeName)
                        ?? portrait.GetNodeOrNull<Control>(SpineOverlayNodeName);
        var subViewport = container?.GetNodeOrNull<SubViewport>($"{SpineViewportContainerNodeName}/SubViewport");
        if (container != null && subViewport != null)
            UpdateSpineAnimationState(cardNode, container, subViewport, int.MaxValue);
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

    private static void RemoveOverlayFromPortrait(TextureRect? portrait)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return;

        var overlay = portrait.GetNodeOrNull<Control>(SpineOverlayNodeName);
        if (overlay != null)
        {
            overlay.GetParent()?.RemoveChild(overlay);
            overlay.QueueFree();
        }
    }

    private static string? GetActiveSpineOverlayScenePath(TextureRect? portrait)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return null;

        var overlay = portrait.GetNodeOrNull<Control>(SpineOverlayNodeName);
        if (overlay == null || !overlay.HasMeta(OverlayScenePathMetaKey))
            return null;

        return overlay.GetMeta(OverlayScenePathMetaKey).AsString();
    }

    private static int? GetActiveSpineOverlayModelIdentity(TextureRect? portrait)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return null;

        var overlay = portrait.GetNodeOrNull<Control>(SpineOverlayNodeName);
        if (overlay == null || !overlay.HasMeta(OverlayModelIdentityMetaKey))
            return null;

        string meta = overlay.GetMeta(OverlayModelIdentityMetaKey).AsString();
        return int.TryParse(meta, out int value) ? value : null;
    }

    private static PackedScene? LoadSpineScene(string scenePath)
    {
        return ResourceLoader.Load<PackedScene>(scenePath, "", ResourceLoader.CacheMode.ReplaceDeep);
    }

    private static SubViewportContainer? GetViewportContainer(Node root)
    {
        if (root.GetNodeOrNull<SubViewportContainer>(SpineViewportContainerNodeName) is { } namedContainer)
            return namedContainer;

        if (root.GetNodeOrNull<SubViewportContainer>("SubViewportContainer") is { } altNamedContainer)
            return altNamedContainer;

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
        viewportContainer.AnchorLeft = 0.0f;
        viewportContainer.AnchorTop = 0.0f;
        viewportContainer.AnchorRight = 1.0f;
        viewportContainer.AnchorBottom = 1.0f;
        viewportContainer.OffsetLeft = 0.0f;
        viewportContainer.OffsetTop = 0.0f;
        viewportContainer.OffsetRight = 0.0f;
        viewportContainer.OffsetBottom = 0.0f;
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
            ? new Vector2(AncientOverlayInsetLeft, AncientOverlayInsetTop)
            : Vector2.Zero;
        Vector2 insetSize = isAncientPortrait
            ? new Vector2(
                Mathf.Max(0.0f, portrait.Size.X - AncientOverlayInsetLeft - AncientOverlayInsetRight),
                Mathf.Max(0.0f, portrait.Size.Y - AncientOverlayInsetTop - AncientOverlayInsetBottom))
            : portrait.Size;

        bool overlayParentIsPortrait = ReferenceEquals(container.GetParent(), portrait);
        container.Position = overlayParentIsPortrait ? insetPosition : portrait.Position + insetPosition;
        container.Size = insetSize;
        container.Scale = overlayParentIsPortrait ? Vector2.One : portrait.Scale;
        container.Rotation = overlayParentIsPortrait ? 0.0f : portrait.Rotation;
        container.PivotOffset = Vector2.Zero;

        if (container.GetNodeOrNull<SubViewportContainer>(SpineViewportContainerNodeName) is { } viewportContainer)
        {
            viewportContainer.Position = Vector2.Zero;
            viewportContainer.Size = container.Size;
        }
    }

    private static void RemoveAllSpineOverlays(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child.Name == SpineOverlayNodeName && GodotObject.IsInstanceValid(child))
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

    private static void RestorePortraitVisibility(NCard cardNode)
    {
        if (!VisibilityStates.TryGetValue(cardNode, out PortraitVisibilityState? state) || !state.HasSnapshot)
            return;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait)
            portrait.Visible = state.PortraitVisible;

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait)
            ancientPortrait.Visible = state.AncientPortraitVisible;

        VisibilityStates.Remove(cardNode);
    }

    private static void CollectPresentationState(
        NCard cardNode,
        ref bool hasHolderAncestor,
        ref bool isHolderActive,
        ref bool isInCardPlay,
        ref bool isPreviewHolder,
        ref bool isHoverTipCard)
    {
        Node? current = cardNode.GetParent();
        while (current != null)
        {
            if (current is NCardHolder holder)
            {
                hasHolderAncestor = true;
                if (holder is NPreviewCardHolder)
                    isPreviewHolder = true;

                bool isHovered = (bool?)NCardHolderIsHoveredField?.GetValue(holder) ?? false;
                bool isFocused = (bool?)NCardHolderIsFocusedField?.GetValue(holder) ?? false;
                if (isHovered || isFocused)
                    isHolderActive = true;

                if (NCardHolderCurrentPressedActionField?.GetValue(holder) != null)
                    isInCardPlay = true;

                if (holder.GetParent() is NPlayerHand playerHand)
                {
                    foreach (Node child in playerHand.GetChildren())
                    {
                        if (child is NCardPlay cardPlay && cardPlay.Holder == holder)
                        {
                            isInCardPlay = true;
                            break;
                        }
                    }
                }
            }
            else if (current is NMerchantSlot)
            {
                hasHolderAncestor = true;
            }
            else if (current is NHoverTipCardContainer or NHoverTipSet)
            {
                isHoverTipCard = true;
            }

            current = current.GetParent();
        }
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

    private static TextureRect? ResolveOverlayTargetPortrait(NCard cardNode, Control container)
    {
        string slot = container.GetMeta(OverlayTargetSlotMetaKey, OverlayTargetSlotNormal).AsString();
        return slot == OverlayTargetSlotAncient
            ? AncientPortraitField?.GetValue(cardNode) as TextureRect
            : PortraitField?.GetValue(cardNode) as TextureRect;
    }

    private static int GetModelIdentity(CardModel? model)
    {
        return model == null ? 0 : RuntimeHelpers.GetHashCode(model);
    }

    private sealed class PortraitVisibilityState
    {
        public bool HasSnapshot { get; set; }
        public bool PortraitVisible { get; set; }
        public bool AncientPortraitVisible { get; set; }
    }
}

public partial class SpinePortraitUpdater : Node
{
    private NCard _card = null!;
    private Control _container = null!;
    private SubViewport _subViewport = null!;
    private int _framesSinceCreated;

    public void Initialize(NCard card, Control container, SubViewport subViewport)
    {
        _card = card;
        _container = container;
        _subViewport = subViewport;
        _framesSinceCreated = 0;
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

        YukiCardSpinePortraitPatch.UpdateSpineAnimationState(_card, _container, _subViewport, _framesSinceCreated);
        _framesSinceCreated++;
    }
}
