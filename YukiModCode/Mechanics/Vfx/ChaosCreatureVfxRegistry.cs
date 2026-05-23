using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

internal sealed class ChaosVfxHandle
{
	public string Key { get; }
	public ChaosVfxSpec Spec { get; private set; }
	public ChaosSpineVfxInstance Instance { get; }
	public Node2D? Anchor { get; set; }
	public Tween? Tween { get; set; }
	public bool Stopping { get; set; }

	public ChaosVfxHandle(string key, ChaosVfxSpec spec, ChaosSpineVfxInstance instance)
	{
		Key = key;
		Spec = spec;
		Instance = instance;
	}

	public void UpdateSpec(ChaosVfxSpec spec)
	{
		Spec = spec;
	}
}

internal static class ChaosCreatureVfxRegistry
{
	private static readonly ConditionalWeakTable<Creature, Dictionary<string, ChaosVfxHandle>> Table = new();

	public static bool TryGet(Creature creature, string key, out ChaosVfxHandle? handle)
	{
		handle = null;
		if (creature == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}

		if (!Table.TryGetValue(creature, out Dictionary<string, ChaosVfxHandle>? map) || map == null)
		{
			return false;
		}

		return map.TryGetValue(key, out handle);
	}

	public static void Set(Creature creature, ChaosVfxHandle handle)
	{
		if (creature == null || handle == null)
		{
			return;
		}

		Dictionary<string, ChaosVfxHandle> map = Table.GetOrCreateValue(creature);
		map[handle.Key] = handle;
	}

	public static bool Remove(Creature creature, string key, out ChaosVfxHandle? removed)
	{
		removed = null;
		if (creature == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}

		if (!Table.TryGetValue(creature, out Dictionary<string, ChaosVfxHandle>? map) || map == null)
		{
			return false;
		}

		if (!map.TryGetValue(key, out removed))
		{
			return false;
		}

		map.Remove(key);
		if (map.Count == 0)
		{
			try
			{
				Table.Remove(creature);
			}
			catch
			{
			}
		}
		return true;
	}
}

