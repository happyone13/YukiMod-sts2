using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace YukiMod.YukiModCode.Mechanics.Perf;

[HarmonyPatch(typeof(NodeUtil), nameof(NodeUtil.AwaitProcessFrame))]
public static class YukiAwaitProcessFrameGuardPatch
{
	[HarmonyPrefix]
	public static bool Prefix(Node node, CancellationToken ct, ref Task<float> __result)
	{
		if (ct.IsCancellationRequested)
		{
			__result = Task.FromCanceled<float>(ct);
			return false;
		}

		if (node == null || !GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion() || !node.IsInsideTree())
		{
			__result = Task.FromResult(1f);
			return false;
		}

		return true;
	}
}


