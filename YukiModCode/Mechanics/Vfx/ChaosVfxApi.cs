using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YukiMod.YukiModCode.Mechanics.Settings;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

public static partial class ChaosVfxApi
{
	private static readonly HashSet<string> Preloaded = new HashSet<string>(StringComparer.Ordinal);

	private sealed partial class FollowAnchor : Node2D
	{
		public NCreatureVisuals? Visuals;
		public Vector2 LocalOffset;

		public override void _Process(double delta)
		{
			NCreatureVisuals? v = Visuals;
			if (v == null || !GodotObject.IsInstanceValid(v))
			{
				this.QueueFreeSafely();
				return;
			}
			GlobalPosition = v.VfxSpawnPosition.GlobalPosition + LocalOffset;
		}
	}

	public static void Preload(in ChaosVfxSpec spec)
	{
		if (!YukiModSharedSettings.CombatEffectsEnabled)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(spec.ScenePath))
		{
			return;
		}

		lock (Preloaded)
		{
			if (!Preloaded.Add(spec.ScenePath))
			{
				return;
			}
		}

		try
		{
			_ = ResourceLoader.Load<PackedScene>(spec.ScenePath);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{YukiModInfo.ModId}] Vfx preload failed: {spec.ScenePath}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	public static void PlayStatic(Creature creature, string effectKey, in ChaosVfxSpec spec)
	{
		if (!YukiModSharedSettings.CombatEffectsEnabled)
		{
			Stop(creature, effectKey);
			return;
		}

		if (creature == null || string.IsNullOrWhiteSpace(effectKey))
		{
			return;
		}

		if (!TryGetCreatureNode(creature, out NCreature node))
		{
			return;
		}

		Vector2 offset = spec.Offset ?? Vector2.Zero;
		Vector2 pos = node.Visuals.VfxSpawnPosition.GlobalPosition + offset;

		if (ChaosCreatureVfxRegistry.TryGet(creature, effectKey, out ChaosVfxHandle? existing) && existing != null)
		{
			existing.UpdateSpec(spec);
			existing.Stopping = false;
			TryAttachStatic(existing, node, pos);
			TryPlay(existing, spec);
			return;
		}

		if (!ChaosSpineVfxInstance.TryCreate(spec.ScenePath, out ChaosSpineVfxInstance? created) || created == null)
		{
			return;
		}

		ChaosVfxHandle handle = new ChaosVfxHandle(effectKey, spec, created);
		ChaosCreatureVfxRegistry.Set(creature, handle);
		TryAttachStatic(handle, node, pos);
		TryPlay(handle, spec);

		if (!string.IsNullOrWhiteSpace(spec.OutAnim))
		{
			_ = StartAutoOut(handle, spec.OutAnim!, spec.DurationSeconds);
		}
		else if (spec.DurationSeconds.HasValue && spec.DurationSeconds.Value > 0f)
		{
			_ = AutoFreeAfter(handle, spec.DurationSeconds.Value);
		}
	}

	public static void EnsureFollow(Creature creature, string effectKey, in ChaosVfxSpec spec)
	{
		if (!YukiModSharedSettings.CombatEffectsEnabled)
		{
			Stop(creature, effectKey);
			return;
		}

		if (creature == null || string.IsNullOrWhiteSpace(effectKey))
		{
			return;
		}

		if (!TryGetCreatureNode(creature, out NCreature node))
		{
			return;
		}

		if (ChaosCreatureVfxRegistry.TryGet(creature, effectKey, out ChaosVfxHandle? existing) && existing != null)
		{
			existing.UpdateSpec(spec);
			existing.Stopping = false;
			TryAttachFollow(existing, node);
			TryPlay(existing, spec);
			return;
		}

		if (!ChaosSpineVfxInstance.TryCreate(spec.ScenePath, out ChaosSpineVfxInstance? created) || created == null)
		{
			return;
		}

		ChaosVfxHandle handle = new ChaosVfxHandle(effectKey, spec, created);
		ChaosCreatureVfxRegistry.Set(creature, handle);
		TryAttachFollow(handle, node);
		TryPlay(handle, spec);
	}

	public static void Stop(Creature creature, string effectKey)
	{
		if (creature == null || string.IsNullOrWhiteSpace(effectKey))
		{
			return;
		}

		if (!ChaosCreatureVfxRegistry.Remove(creature, effectKey, out ChaosVfxHandle? removed) || removed == null)
		{
			return;
		}

		removed.Stopping = true;
		removed.Tween?.Kill();
		removed.Tween = null;

		string? outAnim = removed.Spec.OutAnim;
		if (!string.IsNullOrWhiteSpace(outAnim))
		{
			removed.Instance.TryPlayOutAndFree(outAnim!);
			removed.Anchor?.QueueFreeSafely();
			return;
		}

		removed.Instance.QueueFree();
		removed.Anchor?.QueueFreeSafely();
	}

	public static void FireProjectile(Creature from, Creature to, in ChaosVfxSpec spec)
	{
		if (!YukiModSharedSettings.CombatEffectsEnabled)
		{
			return;
		}

		if (from == null || to == null)
		{
			return;
		}

		if (!TryGetCreatureNode(from, out NCreature fromNode) || !TryGetCreatureNode(to, out NCreature toNode))
		{
			return;
		}

		if (!ChaosSpineVfxInstance.TryCreate(spec.ScenePath, out ChaosSpineVfxInstance? created) || created == null)
		{
			return;
		}

		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null)
		{
			created.QueueFree();
			return;
		}

		Node? container = GetBattleContainer(room, spec.Layer);
		if (container == null)
		{
			created.QueueFree();
			return;
		}

		Vector2 offset = spec.Offset ?? Vector2.Zero;
		Vector2 start = fromNode.Visuals.VfxSpawnPosition.GlobalPosition + offset;
		Vector2 end = toNode.Visuals.VfxSpawnPosition.GlobalPosition + offset;

		created.Node.GlobalPosition = start;
		ApplyZ(created.Node, spec);
		container.AddChildSafely(created.Node);
		TryPlayRaw(created, spec);

		if (spec.Mode == ChaosVfxMode.ProjectileInstantLine)
		{
			created.Node.GlobalPosition = end;
			if (spec.DurationSeconds.HasValue && spec.DurationSeconds.Value > 0f)
			{
				_ = FreeAfter(created, spec.DurationSeconds.Value);
			}
			return;
		}

		float dur = spec.DurationSeconds ?? 0.25f;
		dur = Mathf.Max(dur, 0.01f);
		string? outAnim = spec.OutAnim;
		Tween tween = created.Node.CreateTween();
		tween.TweenProperty(created.Node, "global_position", end, dur);
		tween.TweenCallback(Callable.From(() =>
		{
			if (!string.IsNullOrWhiteSpace(outAnim))
			{
				created.TryPlayOutAndFree(outAnim!);
				return;
			}
			created.QueueFree();
		}));
	}

	private static bool TryGetCreatureNode(Creature creature, out NCreature node)
	{
		node = null!;
		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null)
		{
			return false;
		}

		NCreature? n = room.GetCreatureNode(creature);
		if (n == null || !GodotObject.IsInstanceValid(n))
		{
			return false;
		}

		node = n;
		return true;
	}

	private static Node? GetBattleContainer(NCombatRoom room, ChaosVfxLayer layer)
	{
		return layer == ChaosVfxLayer.BattleBack ? room.BackCombatVfxContainer : room.CombatVfxContainer;
	}

	private static void TryAttachStatic(ChaosVfxHandle handle, NCreature creatureNode, Vector2 globalPos)
	{
		handle.Anchor?.QueueFreeSafely();
		handle.Anchor = null;

		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null)
		{
			return;
		}

		Node? container = handle.Spec.Layer switch
		{
			ChaosVfxLayer.BattleBack => room.BackCombatVfxContainer,
			ChaosVfxLayer.BattleFront => room.CombatVfxContainer,
			ChaosVfxLayer.CreatureBelow => room.BackCombatVfxContainer,
			ChaosVfxLayer.CreatureAbove => room.CombatVfxContainer,
			_ => room.CombatVfxContainer
		};

		if (container == null)
		{
			return;
		}

		Node2D vfxNode = handle.Instance.Node;
		if (vfxNode.GetParent() != container)
		{
			vfxNode.GetParent()?.RemoveChild(vfxNode);
			container.AddChildSafely(vfxNode);
		}

		vfxNode.GlobalPosition = globalPos;
		ApplyZ(vfxNode, handle.Spec);
	}

	private static void TryAttachFollow(ChaosVfxHandle handle, NCreature creatureNode)
	{
		handle.Anchor?.QueueFreeSafely();
		handle.Anchor = null;

		Vector2 offset = handle.Spec.Offset ?? Vector2.Zero;

		if (handle.Spec.Layer == ChaosVfxLayer.CreatureBelow || handle.Spec.Layer == ChaosVfxLayer.CreatureAbove)
		{
			Node2D parent = creatureNode.Visuals;
			Node2D vfxNode = handle.Instance.Node;
			if (vfxNode.GetParent() != parent)
			{
				vfxNode.GetParent()?.RemoveChild(vfxNode);
				parent.AddChildSafely(vfxNode);
			}

			vfxNode.Position = creatureNode.Visuals.VfxSpawnPosition.Position + offset;
			ApplyZ(vfxNode, handle.Spec);
			if (handle.Spec.Layer == ChaosVfxLayer.CreatureBelow && !handle.Spec.ZIndex.HasValue)
			{
				vfxNode.ZIndex = -1;
			}
			if (handle.Spec.Layer == ChaosVfxLayer.CreatureAbove && !handle.Spec.ZIndex.HasValue)
			{
				vfxNode.ZIndex = 1;
			}
			return;
		}

		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null)
		{
			return;
		}

		Node? container = GetBattleContainer(room, handle.Spec.Layer);
		if (container == null)
		{
			return;
		}

		FollowAnchor anchor = new FollowAnchor
		{
			Visuals = creatureNode.Visuals,
			LocalOffset = offset
		};
		ApplyZ(anchor, handle.Spec);
		container.AddChildSafely(anchor);

		Node2D vfxNode2 = handle.Instance.Node;
		vfxNode2.GetParent()?.RemoveChild(vfxNode2);
		anchor.AddChildSafely(vfxNode2);
		vfxNode2.Position = Vector2.Zero;

		handle.Anchor = anchor;
	}

	private static void TryPlay(ChaosVfxHandle handle, in ChaosVfxSpec spec)
	{
		TryPlayRaw(handle.Instance, spec);
	}

	private static void TryPlayRaw(ChaosSpineVfxInstance instance, in ChaosVfxSpec spec)
	{
		string anim = spec.PlayAnim ?? "idle_loop";
		bool loop = spec.PlayLoop || spec.Mode == ChaosVfxMode.FollowLoop;
		_ = instance.TryPlay(anim, loop);
	}

	private static void ApplyZ(CanvasItem item, in ChaosVfxSpec spec)
	{
		if (spec.ZIndex.HasValue)
		{
			item.ZIndex = spec.ZIndex.Value;
		}
	}

	private static async Task AutoFreeAfter(ChaosVfxHandle handle, float seconds)
	{
		await CmdWait(seconds);
		if (handle.Stopping)
		{
			return;
		}
		handle.Instance.QueueFree();
		handle.Anchor?.QueueFreeSafely();
	}

	private static async Task StartAutoOut(ChaosVfxHandle handle, string outAnim, float? seconds)
	{
		if (seconds.HasValue && seconds.Value > 0f)
		{
			await CmdWait(seconds.Value);
		}
		handle.Instance.TryPlayOutAndFree(outAnim);
		handle.Anchor?.QueueFreeSafely();
	}

	private static async Task FreeAfter(ChaosSpineVfxInstance instance, float seconds)
	{
		await CmdWait(seconds);
		instance.QueueFree();
	}

	private static Task CmdWait(float seconds)
	{
		try
		{
			SceneTree tree = (SceneTree)Engine.GetMainLoop();
			SceneTreeTimer t = tree.CreateTimer(seconds);
			TaskCompletionSource tcs = new TaskCompletionSource();
			t.Timeout += Done;
			return tcs.Task;
			void Done()
			{
				t.Timeout -= Done;
				tcs.TrySetResult();
			}
		}
		catch
		{
			return Task.CompletedTask;
		}
	}
}
