using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Entities.Players;
using YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

namespace YukiMod.YukiModCode.Mechanics.Visual;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class YukiCombatPositionPatch
{
	private const string AppliedMetaKey = "ChaosYuki_CombatOffsetApplied";
	private static int _enterLogOnce;

	[HarmonyPostfix]
	public static void Postfix(System.Collections.Generic.List<NCreature> creatureNodes, float scaling, bool fullyCenterPlayers)
	{
		float dx = YukiVisualProfile.CombatPlayerOffsetX;
		float dy = YukiVisualProfile.CombatPlayerOffsetY;
		if (System.Threading.Interlocked.Exchange(ref _enterLogOnce, 1) == 0)
		{
			string meCharId = "";
			string meVisualScene = "";
			string meSkeletonResPath = "";
			try
			{
				for (int i = 0; creatureNodes != null && i < creatureNodes.Count; i++)
				{
					NCreature n = creatureNodes[i];
					if (n == null || !n.Entity.IsPlayer || !LocalContext.IsMe(n.Entity))
					{
						continue;
					}

					meCharId = n.Entity.Player?.Character?.Id.Entry ?? "";
					try
					{
						meVisualScene = n.Visuals?.SceneFilePath ?? "";
					}
					catch
					{
						meVisualScene = "";
					}

					try
					{
						Node? body = n.Visuals?.GetNodeOrNull<Node>("%Visuals");
						Resource? res = body?.Get("skeleton_data_res").As<Resource?>();
						meSkeletonResPath = res?.ResourcePath ?? "";
					}
					catch
					{
						meSkeletonResPath = "";
					}
					break;
				}
			}
			catch
			{
				meCharId = "";
				meVisualScene = "";
				meSkeletonResPath = "";
			}

			Log.Info($"[YukiMod] CombatPositionPatch entered: dx={dx} dy={dy} nodes={(creatureNodes?.Count ?? 0)} scaling={scaling} fullyCenterPlayers={fullyCenterPlayers} meCharId={meCharId} meVisualScene={meVisualScene} meSkeletonResPath={meSkeletonResPath}");
		}
		if (dx == 0f && dy == 0f)
		{
			return;
		}
		if (creatureNodes == null || creatureNodes.Count == 0)
		{
			return;
		}

		NCreature? localPlayerNode = null;
		for (int i = 0; i < creatureNodes.Count; i++)
		{
			NCreature n = creatureNodes[i];
			if (n == null || !n.Entity.IsPlayer)
			{
				continue;
			}
			if (!LocalContext.IsMe(n.Entity))
			{
				continue;
			}
			if (!YukiTarget.IsTarget(n.Entity.Player))
			{
				continue;
			}
			localPlayerNode = n;
			break;
		}

		if (localPlayerNode == null)
		{
			return;
		}

		if (HasApplied(localPlayerNode))
		{
			return;
		}

		Vector2 delta = new Vector2(dx, dy);
		Vector2 before = localPlayerNode.Position;
		localPlayerNode.Position += delta;
		MarkApplied(localPlayerNode);
		Log.Info($"[YukiMod] CombatPositionPatch applied: before={before} after={localPlayerNode.Position} delta={delta}");

		Player? owner = localPlayerNode.Entity.Player;
		if (owner == null)
		{
			return;
		}

		for (int i = 0; i < creatureNodes.Count; i++)
		{
			NCreature n = creatureNodes[i];
			if (n == null || n.Entity.IsPlayer)
			{
				continue;
			}
			if (n.Entity.PetOwner == owner && !HasApplied(n))
			{
				n.Position += delta;
				MarkApplied(n);
			}
		}
	}

	private static bool HasApplied(Node node)
	{
		try
		{
			if (!node.HasMeta(AppliedMetaKey))
			{
				return false;
			}
			Variant v = node.GetMeta(AppliedMetaKey);
			return v.VariantType == Variant.Type.Bool && v.AsBool();
		}
		catch
		{
			return false;
		}
	}

	private static void MarkApplied(Node node)
	{
		try
		{
			node.SetMeta(AppliedMetaKey, true);
		}
		catch
		{
		}
	}
}

