using System;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

public static class YukiBattleDeadOverlay
{
	private const string Anim = "animation";
	private const string ScenePath = "res://YukiMod/ArtWorks/modspine/tscn_point/chaos_yuki_dead_point.tscn";
	private static PackedScene? _cachedScene;
	private static bool _sceneLoadAttempted;
	private static bool _sceneMissingWarned;
	private static bool _missingAnimWarned;
	private static bool _missingSkeletonWarned;
	private static Resource? _cachedSkeleton;
	private static long _createDisabledUntil;
	private static int _createErrorLogged;
	private const int CreateDisableMs = 30000;

	public static void Preload()
	{
		_ = GetScene();
		_ = GetSkeleton();
	}

	public static void Play()
	{
		long now = System.Environment.TickCount64;
		if (now < _createDisabledUntil)
		{
			return;
		}

		try
		{
			NCombatRoom? room = NCombatRoom.Instance;
			if (room == null)
			{
				return;
			}

			PackedScene? scene = GetScene();
			if (scene == null)
			{
				if (!_sceneMissingWarned)
				{
					_sceneMissingWarned = true;
					Log.Warn($"[{YukiModInfo.ModId}] YukiBattleDeadOverlay: missing scene {ScenePath}");
				}
				return;
			}

			Node instance = scene.Instantiate();
			ApplySkeletonIfPresent(instance);

			MegaSprite sprite;
			try
			{
				sprite = new MegaSprite(instance);
			}
			catch (Exception ex)
			{
				Log.Warn($"[{YukiModInfo.ModId}] YukiBattleDeadOverlay: MegaSprite init failed: {ex.Message}");
				instance.QueueFreeSafely();
				return;
			}

			if (!sprite.HasAnimation(Anim))
			{
				if (!_missingAnimWarned)
				{
					_missingAnimWarned = true;
					Log.Warn($"[{YukiModInfo.ModId}] YukiBattleDeadOverlay: missing animation {Anim}");
				}
				instance.QueueFreeSafely();
				return;
			}

			CanvasLayer layer = new CanvasLayer
			{
				Layer = 1000
			};
			(layer as Node).Name = "YukiBattleDeadOverlayLayer";

			Node layerParent = room.Ui as Node ?? (Node)room;
			layerParent.AddChildSafely(layer);
			layer.AddChildSafely(instance);

			sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, __, ___) =>
			{
				if (GodotObject.IsInstanceValid(layer))
				{
					layer.QueueFreeSafely();
				}
			}));

			if (instance is CanvasItem canvasItem)
			{
				canvasItem.TopLevel = true;
				canvasItem.ZAsRelative = false;
				canvasItem.ZIndex = 1000;
			}

			sprite.GetAnimationState().SetAnimation(Anim, loop: false);
		}
		catch (Exception ex)
		{
			_createDisabledUntil = System.Environment.TickCount64 + CreateDisableMs;
			if (System.Threading.Interlocked.Exchange(ref _createErrorLogged, 1) == 0)
			{
				Log.Warn($"[{YukiModInfo.ModId}] YukiBattleDeadOverlay create failed: {ex}");
			}
		}
	}

	private static PackedScene? GetScene()
	{
		if (_cachedScene != null)
		{
			return _cachedScene;
		}

		if (_sceneLoadAttempted)
		{
			return null;
		}

		_sceneLoadAttempted = true;
		try
		{
			_cachedScene = ResourceLoader.Load<PackedScene>(ScenePath);
		}
		catch
		{
			_cachedScene = null;
		}
		return _cachedScene;
	}

	private static void ApplySkeletonIfPresent(Node instance)
	{
		Resource? skeleton = GetSkeleton();
		if (skeleton == null)
		{
			if (!_missingSkeletonWarned)
			{
				_missingSkeletonWarned = true;
				Log.Warn($"[{YukiModInfo.ModId}] YukiBattleDeadOverlay: missing skeleton data");
			}
			return;
		}

		instance.Set("skeleton_data_res", skeleton);
	}

	private static Resource? GetSkeleton()
	{
		if (_cachedSkeleton != null)
		{
			return _cachedSkeleton;
		}

		string key = YukiModInfo.PlaceholderId;
		string dataPath = $"res://YukiMod/ArtWorks/modspine/deadcg/deadcg_{key}/deadcg_{key}_skel_data.tres";
		string datePath = $"res://YukiMod/ArtWorks/modspine/deadcg/deadcg_{key}/deadcg_{key}_skel_date.tres";

		string? selected = null;
		if (ResourceLoader.Exists(dataPath))
		{
			selected = dataPath;
		}
		else if (ResourceLoader.Exists(datePath))
		{
			selected = datePath;
		}

		if (selected == null)
		{
			return null;
		}

		try
		{
			_cachedSkeleton = ResourceLoader.Load<Resource>(selected);
		}
		catch
		{
			_cachedSkeleton = null;
		}
		return _cachedSkeleton;
	}
}

