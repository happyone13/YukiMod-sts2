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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardSpinePortraitPatch
{
    public const string SpineOverlayNodeName = "YukiSpinePortraitOverlay";
    internal const string SpineViewportContainerNodeName = "ViewportContainer";
    private const string OverlayTargetSlotMetaKey = "yuki_target_slot";
    private const string OverlayTargetSlotAncient = "ancient";
    private const string OverlayTargetSlotNormal = "normal";
    private const float AncientOverlayInsetLeft = 0.0f;
    private const float AncientOverlayInsetTop = 0.0f;
    private const float AncientOverlayInsetRight = 0.0f;
    private const float AncientOverlayInsetBottom = 0.0f;
    private const int WarmUpFrames = 3;

    public static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly string[] DynamicPortraitScenePaths =
    {
        "res://YukiMod/scenes/cards/ba_dao_dynamic.tscn",
        "res://YukiMod/scenes/cards/bing_dian_zhi_ren_dynamic.tscn",
        "res://YukiMod/scenes/cards/hei_yun_ao_yi_can_dynamic.tscn",
        "res://YukiMod/scenes/cards/hei_yun_ao_yi_hei_wu_dynamic.tscn",
        "res://YukiMod/scenes/cards/hei_yun_ao_yi_mie_dynamic.tscn",
        "res://YukiMod/scenes/cards/hei_yun_xin_fa_dynamic.tscn",
        "res://YukiMod/scenes/cards/huang_hun_de_ji_ban_dynamic.tscn",
        "res://YukiMod/scenes/cards/mi_huo_yi_ji_dynamic.tscn",
        "res://YukiMod/scenes/cards/po_bing_zhan_dynamic.tscn",
        "res://YukiMod/scenes/cards/tou_xi_zhan_dynamic.tscn",
        "res://YukiMod/scenes/cards/ya_zhi_zhun_bei_dynamic.tscn",
        "res://YukiMod/scenes/cards/yao_guai_shou_lie_dynamic.tscn",
        "res://YukiMod/scenes/cards/ye_huo_dynamic.tscn",
        "res://YukiMod/scenes/cards/yue_du_dynamic.tscn",
        "res://YukiMod/scenes/cards/yue_ying_dynamic.tscn",
        "res://YukiMod/scenes/cards/zhao_jia_dynamic.tscn"
    };

    private static readonly Dictionary<string, PackedScene> SceneCache = new();
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

        if (VisibilityStates.TryGetValue(cardNode, out var owned))
        {
            if (owned.ScenePath == scenePath && owned.Target == portrait && HasActiveSpineOverlay(cardNode))
                return true;
            RemoveSpineOverlay(cardNode);
        }

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

        ConfigureViewportContainer(viewportContainer);
        ConfigureSubViewport(subViewport);

        if (viewportContainer.GetParent() != null)
            viewportContainer.GetParent()?.RemoveChild(viewportContainer);

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
        overlay.SetMeta(
            OverlayTargetSlotMetaKey,
            ReferenceEquals(AncientPortraitField?.GetValue(cardNode), portrait)
                ? OverlayTargetSlotAncient
                : OverlayTargetSlotNormal);

        YukiCardCustomFramePatch.CaptureBeforePortrait(cardNode);
        var state = new PortraitVisibilityState(cardNode.Model!, portrait, overlay, subViewport, scenePath);
        VisibilityStates.Add(cardNode, state);
        portrait.Texture = state.AppliedTexture;
        portrait.ClipContents = true;
        overlay.AddChild(viewportContainer);
        portrait.AddChild(overlay);
        portrait.MoveChild(overlay, portrait.GetChildCount() - 1);
        spineInstance.QueueFree();

        SyncOverlayLayout(cardNode, portrait, overlay, subViewport);
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        var updater = new SpinePortraitUpdater();
        updater.Initialize(cardNode, overlay, subViewport);
        overlay.AddChild(updater);
        return true;
    }

    public static void PreloadDynamicPortraitScenes()
    {
        if (!YukiModConfig.UseDynamicCardPortraits)
            return;

        foreach (string scenePath in DynamicPortraitScenePaths)
            GetOrCreateSpineScene(scenePath);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (cardNode == null || !VisibilityStates.TryGetValue(cardNode, out var state))
            return;
        if (!ReferenceEquals(state.Model, cardNode.Model) || !YukiModConfig.UseDynamicCardPortraits ||
            !TryGetSpineScenePath(cardNode, out _))
            RemoveSpineOverlay(cardNode, beforeModelDraw: true);
    }

    public static void RemoveSpineOverlay(NCard? cardNode, bool beforeModelDraw = false)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) ||
            !VisibilityStates.TryGetValue(cardNode, out var state))
            return;

        VisibilityStates.Remove(cardNode);
        if (GodotObject.IsInstanceValid(state.Overlay))
            state.Overlay.Free();
        if (!beforeModelDraw && !ReferenceEquals(state.Model, cardNode.Model))
            return;
        if (GodotObject.IsInstanceValid(state.Target))
        {
            if (state.Target.ClipContents)
                state.Target.ClipContents = state.OriginalClip;
            if (state.Target.Texture == state.AppliedTexture)
                state.Target.Texture = state.OriginalTexture;
        }
        if (state.ForcedAncient is not bool ancient)
            return;
        if (PortraitField?.GetValue(cardNode) is TextureRect normal && normal.Visible == !ancient)
            normal.Visible = state.PortraitVisible;
        if (AncientPortraitField?.GetValue(cardNode) is TextureRect special && special.Visible == ancient)
            special.Visible = state.AncientPortraitVisible;
    }

    public static bool HasActiveSpineOverlay(NCard? cardNode)
    {
        return cardNode != null && VisibilityStates.TryGetValue(cardNode, out var state) &&
               ReferenceEquals(state.Model, cardNode.Model) && GodotObject.IsInstanceValid(state.Overlay) &&
               state.Overlay.GetParent() == state.Target;
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
        bool isInspectScreenCard = false;
        bool isFlyAnimationCard = false;

        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard, ref isInspectScreenCard, ref isFlyAnimationCard);

        bool isTargetingSelection = NRun.Instance != null && NTargetManager.Instance.IsInSelection;
        if (isInCardPlay && !isTargetingSelection)
            return false;

        bool isEnlarged = ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;
        return hasHolderAncestor || isPreviewHolder || isHoverTipCard || isInspectScreenCard || isFlyAnimationCard || isEnlarged;
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
        bool isInspectScreenCard = false;
        bool isFlyAnimationCard = false;

        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard, ref isInspectScreenCard, ref isFlyAnimationCard);

        bool isEnlarged = ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;
        return isHolderActive || isPreviewHolder || isHoverTipCard || isInspectScreenCard || isFlyAnimationCard || isInCardPlay || isEnlarged;
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
        bool isInspectScreenCard = false;
        bool isFlyAnimationCard = false;
        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard, ref isInspectScreenCard, ref isFlyAnimationCard);
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
        bool isInspectScreenCard = false;
        bool isFlyAnimationCard = false;
        CollectPresentationState(cardNode, ref hasHolderAncestor, ref isHolderActive, ref isInCardPlay, ref isPreviewHolder, ref isHoverTipCard, ref isInspectScreenCard, ref isFlyAnimationCard);
        return isHoverTipCard;
    }

    public static void ForcePortraitSlot(
        NCard cardNode,
        TextureRect? portrait,
        TextureRect? ancientPortrait,
        YukiSpinePortraitSlot slot)
    {
        if (portrait == null || ancientPortrait == null ||
            !VisibilityStates.TryGetValue(cardNode, out var state) || !HasActiveSpineOverlay(cardNode))
            return;

        if (!state.ForcedAncient.HasValue)
        {
            state.PortraitVisible = portrait.Visible;
            state.AncientPortraitVisible = ancientPortrait.Visible;
        }
        state.ForcedAncient = slot == YukiSpinePortraitSlot.Ancient;
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

        bool active = DynamicPortraitActivity.ShouldRender(container);
        DynamicPortraitActivity.SetActive(subViewport, active);
        if (!active)
        {
            return;
        }

        var parentPortrait = ResolveOverlayTargetPortrait(cardNode, container);
        if (parentPortrait != null)
            SyncOverlayLayout(cardNode, parentPortrait, container, subViewport);
    }

    public static void UpdateOverlay(NCard cardNode, TextureRect? portrait)
    {
        if (portrait == null || !VisibilityStates.TryGetValue(cardNode, out var state) ||
            !ReferenceEquals(state.Model, cardNode.Model) || state.Target != portrait)
            return;

        UpdateSpineAnimationState(cardNode, state.Overlay, state.Viewport, int.MaxValue);
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

    private static PackedScene? GetOrCreateSpineScene(string scenePath)
    {
        if (SceneCache.TryGetValue(scenePath, out PackedScene? cachedScene))
            return cachedScene;

        if (!ResourceLoader.Exists(scenePath))
        {
            if (MissingResourceWarnings.Add(scenePath))
                GD.PushWarning($"[YukiCardSpinePortrait] Scene path missing or not found: {scenePath}");
            return null;
        }

        PackedScene? scene;
        try
        {
            scene = ResourceLoader.Load<PackedScene>(scenePath, "", ResourceLoader.CacheMode.Reuse);
        }
        catch (System.Exception ex)
        {
            if (MissingResourceWarnings.Add(scenePath))
                GD.PushWarning($"[YukiCardSpinePortrait] Failed to load {scenePath}: {ex.Message}");
            return null;
        }

        if (scene == null)
        {
            GD.PushWarning($"[YukiCardSpinePortrait] Failed to load PackedScene: {scenePath}");
            return null;
        }

        SceneCache[scenePath] = scene;
        return scene;
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

    private static void CollectPresentationState(
        NCard cardNode,
        ref bool hasHolderAncestor,
        ref bool isHolderActive,
        ref bool isInCardPlay,
        ref bool isPreviewHolder,
        ref bool isHoverTipCard,
        ref bool isInspectScreenCard,
        ref bool isFlyAnimationCard)
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
            else if (current is NInspectCardScreen)
            {
                isInspectScreenCard = true;
            }
            else if (current.GetParent() is NTopBar && string.Equals(current.Name.ToString(), "TrailContainer", System.StringComparison.Ordinal))
            {
                isFlyAnimationCard = true;
            }
            else if (current.Name.ToString().EndsWith("VfxContainer", System.StringComparison.Ordinal))
            {
                isFlyAnimationCard = true;
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

    private sealed class PortraitVisibilityState(CardModel model, TextureRect target, Control overlay, SubViewport viewport, string scenePath)
    {
        public CardModel Model { get; } = model;
        public TextureRect Target { get; } = target;
        public Control Overlay { get; } = overlay;
        public SubViewport Viewport { get; } = viewport;
        public string ScenePath { get; } = scenePath;
        public Texture2D? OriginalTexture { get; } = target.Texture;
        public Texture2D? AppliedTexture { get; } = model.Portrait;
        public bool OriginalClip { get; } = target.ClipContents;
        public bool? ForcedAncient { get; set; }
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
