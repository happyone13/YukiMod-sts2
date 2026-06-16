using System;
using System.Threading.Tasks;
using YukiMod.YukiModCode.Mechanics.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YukiMod.YukiModCode.Mechanics.Animation;

public static class YukiU1BuffAnim
{
	private const string ReadyAnim = "u1_buff_ready";
	private const string PlayAnim = "u1_buff_play";
	private const string IdleLoop = "idle_loop";

	private const string VfxPlayB = "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u1_buff_play_b.tscn";
	private const string VfxPlayF = "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u1_buff_play_f.tscn";
	private const string VfxAnim = "animation";

	public static async Task PlayAsync(Creature owner, Func<Task> onPlay)
	{
		if (onPlay == null)
		{
			return;
		}

		if (!TryGetNode(owner, out NCombatRoom room, out NCreature node))
		{
			await onPlay();
			return;
		}

		if (!HasAnimation(node, ReadyAnim) || !HasAnimation(node, PlayAnim))
		{
			await onPlay();
			return;
		}

		try
		{
			node.SpineAnimation.SetAnimation(ReadyAnim, loop: false);
		}
		catch
		{
			await onPlay();
			return;
		}

		float readyLen = 0.1f;
		try
		{
			readyLen = Mathf.Max(node.GetCurrentAnimationLength(), 0.01f);
		}
		catch
		{
			readyLen = 0.1f;
		}

		await Cmd.CustomScaledWait(readyLen * 0.5f, readyLen);

		try
		{
			node.SpineAnimation.SetAnimation(PlayAnim, loop: false);
		}
		catch
		{
			await onPlay();
			return;
		}

		Vector2 pos = GetFootPos(node);
		float uniformScale = GetUniformScale(node);
		PlayVfx(room, pos, uniformScale);
		TryAddIdleLoop(node);

		await onPlay();
	}

	private static void PlayVfx(NCombatRoom room, Vector2 globalPos, float uniformScale)
	{
		Node? back = room.BackCombatVfxContainer;
		if (back != null)
		{
			ChaosOneShotVfx.PlaySpineOneShot(VfxPlayB, VfxAnim, back, globalPos, zIndex: null, uniformScale: uniformScale);
		}

		Node? front = room.CombatVfxContainer;
		if (front != null)
		{
			ChaosOneShotVfx.PlaySpineOneShot(VfxPlayF, VfxAnim, front, globalPos, zIndex: -1, uniformScale: uniformScale);
		}
	}

	private static float GetUniformScale(NCreature creatureNode)
	{
		float s = 1f;
		try
		{
			Node2D? visualsRoot = creatureNode.Visuals;
			if (visualsRoot != null && GodotObject.IsInstanceValid(visualsRoot))
			{
				Marker2D? marker = visualsRoot.GetNodeOrNull<Marker2D>("Visuals/ChaosEff");
				if (marker != null && GodotObject.IsInstanceValid(marker))
				{
					s = Mathf.Abs(marker.GlobalScale.X);
				}
				else
				{
					s = Mathf.Abs(visualsRoot.GlobalScale.X);
				}
			}
		}
		catch
		{
			s = 1f;
		}

		return Mathf.Max(s, 0.01f);
	}

	private static bool TryGetNode(Creature owner, out NCombatRoom room, out NCreature node)
	{
		room = NCombatRoom.Instance!;
		node = null!;

		if (owner == null)
		{
			return false;
		}

		if (room == null)
		{
			return false;
		}

		NCreature? n;
		try
		{
			n = room.GetCreatureNode(owner);
		}
		catch
		{
			return false;
		}

		if (n == null || !GodotObject.IsInstanceValid(n) || !n.HasSpineAnimation)
		{
			return false;
		}

		node = n;
		return true;
	}

	private static bool HasAnimation(NCreature creatureNode, string name)
	{
		try
		{
			MegaSprite? body = creatureNode.Visuals?.SpineBody;
			return body != null && body.HasAnimation(name);
		}
		catch
		{
			return false;
		}
	}

	private static Vector2 GetFootPos(NCreature creatureNode)
	{
		try
		{
			Node2D? visualsRoot = creatureNode.Visuals;
			if (visualsRoot != null && GodotObject.IsInstanceValid(visualsRoot))
			{
				Marker2D? marker = visualsRoot.GetNodeOrNull<Marker2D>("Visuals/ChaosEff");
				if (marker != null && GodotObject.IsInstanceValid(marker))
				{
					return marker.GlobalPosition;
				}
			}
		}
		catch
		{
		}

		try
		{
			return creatureNode.Visuals?.GlobalPosition ?? creatureNode.GlobalPosition;
		}
		catch
		{
			return creatureNode.GlobalPosition;
		}
	}

	private static void TryAddIdleLoop(NCreature creatureNode)
	{
		try
		{
			MegaSprite? body = creatureNode.Visuals?.SpineBody;
			if (body == null)
			{
				return;
			}
			MegaAnimationState st = body.GetAnimationState();
			if (body.HasAnimation(IdleLoop))
			{
				st.AddAnimation(IdleLoop, 0f, loop: true);
			}
		}
		catch
		{
		}
	}
}
