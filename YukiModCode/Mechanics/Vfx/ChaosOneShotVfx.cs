using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

public static class ChaosOneShotVfx
{
	private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);
	private static readonly object SceneCacheLock = new object();
	private static readonly Dictionary<string, PackedScene?> SceneCache = new Dictionary<string, PackedScene?>(StringComparer.Ordinal);

	public static void Prewarm(IEnumerable<string> scenePaths)
	{
		if (scenePaths == null)
		{
			return;
		}

		foreach (string? scenePath in scenePaths)
		{
			if (string.IsNullOrWhiteSpace(scenePath))
			{
				continue;
			}
			_ = GetOrLoadScene(scenePath);
		}
	}

	private static PackedScene? GetOrLoadScene(string scenePath)
	{
		lock (SceneCacheLock)
		{
			if (SceneCache.TryGetValue(scenePath, out PackedScene? scene))
			{
				return scene;
			}
		}

		PackedScene? loaded;
		try
		{
			loaded = ResourceLoader.Load<PackedScene>(scenePath);
		}
		catch (Exception ex)
		{
			WarnOnce($"load:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx load failed: {scenePath}: {ex.GetType().Name}: {ex.Message}");
			return null;
		}

		if (loaded == null)
		{
			WarnOnce($"miss:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx missing: {scenePath}");
			return null;
		}

		lock (SceneCacheLock)
		{
			SceneCache[scenePath] = loaded;
		}
		return loaded;
	}

	public static void PlaySpineOneShot(string scenePath, string anim, Node parent, Vector2 globalPos, int? zIndex = null, float? uniformScale = null)
	{
		if (parent == null || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(anim))
		{
			return;
		}

		PackedScene? scene = GetOrLoadScene(scenePath);
		if (scene == null)
		{
			return;
		}

		Node2D node;
		try
		{
			node = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		}
		catch (Exception ex)
		{
			WarnOnce($"inst:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx instantiate failed: {scenePath}: {ex.GetType().Name}: {ex.Message}");
			return;
		}

		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}

		try
		{
			parent.AddChildSafely(node);
			node.GlobalPosition = globalPos;
			if (zIndex.HasValue && node is CanvasItem canvasItem)
			{
				canvasItem.ZIndex = zIndex.Value;
			}
			if (uniformScale.HasValue)
			{
				float s = uniformScale.Value;
				node.Scale *= new Vector2(s, s);
			}
		}
		catch
		{
			node.QueueFreeSafely();
			return;
		}

		try
		{
			List<Node> nodes = new List<Node>();
			CollectNodes(node, nodes);

			List<MegaSprite> sprites = new List<MegaSprite>();
			for (int i = 0; i < nodes.Count; i++)
			{
				MegaSprite sprite;
				try
				{
					sprite = new MegaSprite(nodes[i]);
				}
				catch
				{
					continue;
				}

				try
				{
					if (!sprite.HasAnimation(anim))
					{
						continue;
					}
				}
				catch
				{
					continue;
				}

				sprites.Add(sprite);
			}

			if (sprites.Count == 0)
			{
				WarnOnce($"anim:{scenePath}:{anim}", $"[{YukiModInfo.ModId}] OneShotVfx missing anim: {anim} in {scenePath}");
				node.QueueFreeSafely();
				return;
			}

			int remaining = sprites.Count;
			bool freed = false;

			for (int i = 0; i < sprites.Count; i++)
			{
				MegaSprite sprite = sprites[i];
				sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, __, ___) =>
				{
					if (freed)
					{
						return;
					}

					remaining--;
					if (remaining > 0)
					{
						return;
					}

					freed = true;
					node.QueueFreeSafely();
				}));

				try
				{
					sprite.GetAnimationState().SetAnimation(anim, loop: false);
				}
				catch
				{
					remaining--;
				}
			}

			if (!freed && remaining <= 0)
			{
				freed = true;
				node.QueueFreeSafely();
			}
		}
		catch
		{
			node.QueueFreeSafely();
		}
	}

	public static void PlaySpineOneShot(string scenePath, string anim, Node parent, Vector2 globalPos, float rotationDelta, int? zIndex = null, float? uniformScale = null)
	{
		if (parent == null || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(anim))
		{
			return;
		}

		PackedScene? scene = GetOrLoadScene(scenePath);
		if (scene == null)
		{
			return;
		}

		Node2D node;
		try
		{
			node = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		}
		catch (Exception ex)
		{
			WarnOnce($"inst:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx instantiate failed: {scenePath}: {ex.GetType().Name}: {ex.Message}");
			return;
		}

		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}

		try
		{
			parent.AddChildSafely(node);
			node.GlobalPosition = globalPos;
			node.Rotation += rotationDelta;
			if (zIndex.HasValue && node is CanvasItem canvasItem)
			{
				canvasItem.ZIndex = zIndex.Value;
			}
			if (uniformScale.HasValue)
			{
				float s = uniformScale.Value;
				node.Scale *= new Vector2(s, s);
			}
		}
		catch
		{
			node.QueueFreeSafely();
			return;
		}

		try
		{
			List<Node> nodes = new List<Node>();
			CollectNodes(node, nodes);

			List<MegaSprite> sprites = new List<MegaSprite>();
			for (int i = 0; i < nodes.Count; i++)
			{
				MegaSprite sprite;
				try
				{
					sprite = new MegaSprite(nodes[i]);
				}
				catch
				{
					continue;
				}

				try
				{
					if (!sprite.HasAnimation(anim))
					{
						continue;
					}
				}
				catch
				{
					continue;
				}

				sprites.Add(sprite);
			}

			if (sprites.Count == 0)
			{
				WarnOnce($"anim:{scenePath}:{anim}", $"[{YukiModInfo.ModId}] OneShotVfx missing anim: {anim} in {scenePath}");
				node.QueueFreeSafely();
				return;
			}

			int remaining = sprites.Count;
			bool freed = false;

			for (int i = 0; i < sprites.Count; i++)
			{
				MegaSprite sprite = sprites[i];
				sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, __, ___) =>
				{
					if (freed)
					{
						return;
					}

					remaining--;
					if (remaining > 0)
					{
						return;
					}

					freed = true;
					node.QueueFreeSafely();
				}));

				try
				{
					sprite.GetAnimationState().SetAnimation(anim, loop: false);
				}
				catch
				{
					remaining--;
				}
			}

			if (!freed && remaining <= 0)
			{
				freed = true;
				node.QueueFreeSafely();
			}
		}
		catch
		{
			node.QueueFreeSafely();
		}
	}

	private static void CollectNodes(Node node, List<Node> nodes)
	{
		nodes.Add(node);
		Godot.Collections.Array<Node> children = node.GetChildren();
		for (int i = 0; i < children.Count; i++)
		{
			CollectNodes(children[i], nodes);
		}
	}

	private static void WarnOnce(string key, string msg)
	{
		lock (Warned)
		{
			if (!Warned.Add(key))
			{
				return;
			}
		}
		Log.Warn(msg);
	}
}
