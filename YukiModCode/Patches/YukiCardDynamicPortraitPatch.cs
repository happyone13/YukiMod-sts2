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

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class YukiCardDynamicPortraitUpdateVisualsPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void UpdateVisualsPrefix(NCard __instance)
    {
        YukiCardDynamicPortraitPatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void UpdateVisualsPostfix(NCard __instance)
    {
        YukiCardDynamicPortraitPatch.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
public static class YukiCardDynamicPortraitReloadPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void ReloadPrefix(NCard __instance)
    {
        YukiCardDynamicPortraitPatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void ReloadPostfix(NCard __instance)
    {
        YukiCardDynamicPortraitPatch.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "_EnterTree")]
public static class YukiCardDynamicPortraitEnterTreePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void EnterTreePostfix(NCard __instance)
    {
        YukiCardDynamicPortraitPatch.Apply(__instance);
    }
}

internal static class YukiCardDynamicPortraitPatch
{
    public const string OverlayNodeName = "YukiDynamicPortraitOverlay";

    private static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Dictionary<string, PackedScene> SceneCache = new();

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetDynamicPortraitScenePath(cardNode, out string? scenePath))
        {
            RemoveOverlay(cardNode);
            return;
        }

        if (cardNode?.IsInsideTree() != true)
            return;

        if (HasActiveOverlay(cardNode))
            return;

        AttachOverlay(cardNode, scenePath!);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (TryGetDynamicPortraitScenePath(cardNode, out _))
            return;

        RemoveOverlay(cardNode);
    }

    private static void AttachOverlay(NCard cardNode, string scenePath)
    {
        if (cardNode.Model is not YukiModCard cardModel)
            return;

        TextureRect? portrait = GetTargetPortrait(cardNode, cardModel.CustomSpinePortraitSlot);
        if (portrait == null)
            return;

        RemoveOverlay(cardNode);

        Node? sceneInstance = GetOrCreateScene(scenePath);
        if (sceneInstance == null)
            return;

        var viewportContainer = FindNodeByType<SubViewportContainer>(sceneInstance);
        var subViewport = FindNodeByType<SubViewport>(sceneInstance);
        if (subViewport == null)
        {
            sceneInstance.QueueFree();
            return;
        }

        if (viewportContainer != null)
            viewportContainer.GetParent()?.RemoveChild(viewportContainer);

        PrepareViewport(subViewport);

        var overlay = new Control
        {
            Name = OverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = 0f,
            OffsetRight = 0f,
            OffsetBottom = 0f,
            ClipContents = true
        };

        var attachedContainer = viewportContainer ?? CreateViewportContainer(subViewport);
        overlay.AddChild(attachedContainer);
        portrait.AddChild(overlay);
        portrait.Texture = null;
        sceneInstance.QueueFree();
    }

    private static SubViewportContainer CreateViewportContainer(SubViewport subViewport)
    {
        var viewportContainer = new SubViewportContainer
        {
            Name = "ViewportContainer",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = 0f,
            OffsetRight = 0f,
            OffsetBottom = 0f,
            Stretch = true,
            ClipContents = true
        };

        viewportContainer.AddChild(subViewport);
        return viewportContainer;
    }

    private static void PrepareViewport(SubViewport subViewport)
    {
        subViewport.Set("transparent_bg", true);
        subViewport.TransparentBg = true;
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        foreach (Node child in subViewport.GetChildren())
        {
            if (child is ColorRect colorRect)
                colorRect.Visible = false;
        }
    }

    private static TextureRect? GetTargetPortrait(NCard cardNode, SpinePortraitSlot slot)
    {
        return slot == SpinePortraitSlot.Ancient
            ? AncientPortraitField?.GetValue(cardNode) as TextureRect
            : PortraitField?.GetValue(cardNode) as TextureRect;
    }

    private static bool TryGetDynamicPortraitScenePath(NCard? cardNode, out string? scenePath)
    {
        scenePath = null;

        if (cardNode?.Model is not IYukiCardVisualProfile cardModel)
            return false;

        if (!cardModel.UseDynamicPortrait || !YukiModConfig.UseYukiDynamicPortraits)
            return false;

        scenePath = cardModel.CustomSpinePortraitScenePath;
        return !string.IsNullOrWhiteSpace(scenePath) && ResourceLoader.Exists(scenePath);
    }

    private static void RemoveOverlay(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
            return;

        RemoveOverlayNode(cardNode);

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait)
        {
            RemoveOverlayNode(portrait);
            RestorePortraitTexture(cardNode, portrait);
        }

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait)
        {
            RemoveOverlayNode(ancientPortrait);
            RestorePortraitTexture(cardNode, ancientPortrait);
        }
    }

    private static void RemoveOverlayNode(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child.Name == OverlayNodeName && GodotObject.IsInstanceValid(child))
                child.QueueFree();
        }
    }

    private static void RestorePortraitTexture(NCard cardNode, TextureRect portrait)
    {
        if (portrait.Texture == null)
            portrait.Texture = cardNode.Model?.Portrait;
    }

    private static bool HasActiveOverlay(NCard? cardNode)
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

    private static Node? GetOrCreateScene(string scenePath)
    {
        if (!SceneCache.TryGetValue(scenePath, out PackedScene? scene))
        {
            scene = GD.Load<PackedScene>(scenePath);
            if (scene == null)
                return null;

            SceneCache[scenePath] = scene;
        }

        return scene.Instantiate<Node>();
    }

    private static T? FindNodeByType<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T typedChild)
                return typedChild;

            T? nested = FindNodeByType<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
