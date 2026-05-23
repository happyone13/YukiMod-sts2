using System;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YukiMod.YukiModCode.Mechanics.Animation;

public static class YukiVictoryAnimCoordinator
{
	private sealed class State
	{
		public bool TeleportActive;
		public Action? PendingVictory;
	}

	private static readonly ConditionalWeakTable<Creature, State> States = new();

	public static void MarkTeleportStart(Creature creature)
	{
		if (creature == null)
		{
			return;
		}
		try
		{
			State s = States.GetOrCreateValue(creature);
			s.TeleportActive = true;
		}
		catch
		{
		}
	}

	public static void MarkTeleportEnd(Creature creature)
	{
		if (creature == null)
		{
			return;
		}

		Action? pending = null;
		try
		{
			if (States.TryGetValue(creature, out State? s) && s != null)
			{
				s.TeleportActive = false;
				pending = s.PendingVictory;
				s.PendingVictory = null;
			}
		}
		catch
		{
		}

		if (pending != null)
		{
			try
			{
				Callable.From(pending).CallDeferred();
			}
			catch
			{
				try
				{
					pending();
				}
				catch
				{
				}
			}
		}
	}

	public static void PlayOrDeferVictory(Creature creature, Action playVictory)
	{
		if (creature == null || playVictory == null)
		{
			return;
		}

		try
		{
			State s = States.GetOrCreateValue(creature);
			if (s.TeleportActive)
			{
				s.PendingVictory = playVictory;
				return;
			}
		}
		catch
		{
		}

		playVictory();
	}
}


