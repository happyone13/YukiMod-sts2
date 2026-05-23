using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

public sealed class ChaosSpineVfxInstance
{
	private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);

	public Node2D Node { get; }
	public MegaSprite Controller { get; }

	private ChaosSpineVfxInstance(Node2D node, MegaSprite controller)
	{
		Node = node;
		Controller = controller;
	}

	public static bool TryCreate(string scenePath, out ChaosSpineVfxInstance? instance)
	{
		instance = null;
		if (string.IsNullOrWhiteSpace(scenePath))
		{
			return false;
		}

		PackedScene? scene;
		try
		{
			scene = ResourceLoader.Load<PackedScene>(scenePath);
		}
		catch (Exception ex)
		{
			WarnOnce($"scene_load:{scenePath}", $"[{YukiModInfo.ModId}] Vfx scene load failed: {scenePath}: {ex.GetType().Name}: {ex.Message}");
			return false;
		}

		if (scene == null)
		{
			WarnOnce($"scene_null:{scenePath}", $"[{YukiModInfo.ModId}] Vfx scene missing: {scenePath}");
			return false;
		}

		Node2D? node;
		try
		{
			node = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		}
		catch (Exception ex)
		{
			WarnOnce($"scene_inst:{scenePath}", $"[{YukiModInfo.ModId}] Vfx scene instantiate failed: {scenePath}: {ex.GetType().Name}: {ex.Message}");
			return false;
		}

		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return false;
		}

		MegaSprite controller;
		try
		{
			controller = new MegaSprite(node);
		}
		catch (Exception ex)
		{
			WarnOnce($"controller:{scenePath}", $"[{YukiModInfo.ModId}] Vfx controller create failed: {scenePath}: {ex.GetType().Name}: {ex.Message}");
			node.QueueFreeSafely();
			return false;
		}

		instance = new ChaosSpineVfxInstance(node, controller);
		return true;
	}

	public bool HasAnimation(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}

		try
		{
			return Controller.HasAnimation(name);
		}
		catch
		{
			return false;
		}
	}

	public bool TryPlay(string name, bool loop)
	{
		if (!HasAnimation(name))
		{
			return false;
		}

		try
		{
			_ = Controller.GetAnimationState().SetAnimation(name, loop: loop);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool TryPlayOutAndFree(string outAnim)
	{
		if (!HasAnimation(outAnim))
		{
			QueueFree();
			return false;
		}

		try
		{
			bool done = false;
			Controller.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, __, ___) =>
			{
				if (done)
				{
					return;
				}
				done = true;
				QueueFree();
			}));

			_ = Controller.GetAnimationState().SetAnimation(outAnim, loop: false);
			return true;
		}
		catch
		{
			QueueFree();
			return false;
		}
	}

	public void QueueFree()
	{
		try
		{
			Node.QueueFreeSafely();
		}
		catch
		{
		}
	}

	private static void WarnOnce(string key, string message)
	{
		lock (Warned)
		{
			if (!Warned.Add(key))
			{
				return;
			}
		}
		Log.Warn(message);
	}
}

