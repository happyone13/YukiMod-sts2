using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

[HarmonyPatch]
public static class YukiBattleReadyOverlayPatches
{
	private const float CombatEventVoiceVolume = 3f;
	private static int _combatAnimToken;
	private static readonly string[] CombatStartCandidates = ["battle_start", "idle_to_b_idle", "b_in", "b_idle", "idle_loop", "idle"];
	private static readonly string[] CombatIdleCandidates = ["idle_loop", "b_idle", "idle"];
	private static readonly string[] VictoryStartCandidates = ["victory_ready", "b_idle", "idle_loop", "idle"];
	private static readonly string[] VictoryIdleCandidates = ["victory_loop", "b_idle", "idle_loop", "idle"];
	private static readonly string[] DefendCandidates = ["defend"];

	[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
	[HarmonyPostfix]
	public static void AfterBeforeCombatStart(IRunState runState, CombatState? combatState)
	{
		try
		{
			Player? me = LocalContext.GetMe(runState);
			if (!YukiTarget.IsTarget(me))
			{
				return;
			}

			YukiBattleReadyOverlay.Preload();
			YukiBattleDeadOverlay.Preload();
			YukiAudioService.TryPlayCombatStartVoice(me, CombatEventVoiceVolume);
			TryPlayCombatStartAnimation(me);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
	[HarmonyPostfix]
	public static void AfterCombatVictory(IRunState runState, CombatState? combatState)
	{
		try
		{
			Player? me = LocalContext.GetMe(runState);
			if (!YukiTarget.IsTarget(me))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyCombatEnded();
			YukiAudioService.TryPlayVictoryVoice(me, CombatEventVoiceVolume);
			Mechanics.Animation.YukiVictoryAnimCoordinator.PlayOrDeferVictory(me!.Creature, () => TryPlayVictoryAnimation(me));
		}
		catch
		{
		}
	}

	private static void TryPlayCombatStartAnimation(Player? player)
	{
		if (player == null)
		{
			return;
		}
		int token = ++_combatAnimToken;
		TryApplyPlayerAnimation(player, CombatStartCandidates, CombatIdleCandidates, token, retries: 8);
	}

	private static void TryPlayVictoryAnimation(Player? player)
	{
		if (player == null)
		{
			return;
		}
		int token = ++_combatAnimToken;
		TryApplyPlayerAnimation(player, VictoryStartCandidates, VictoryIdleCandidates, token, retries: 8);
	}

	private static void TryApplyPlayerAnimation(Player player, string[] firstCandidates, string[] loopCandidates, int token, int retries)
	{
		try
		{
			if (token != _combatAnimToken)
			{
				return;
			}

			NCombatRoom? room = NCombatRoom.Instance;
			if (room == null)
			{
				return;
			}

			NCreature? creatureNode = room.GetCreatureNode(player.Creature);
			if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode) || !creatureNode.HasSpineAnimation)
			{
				if (retries > 0)
				{
					Callable.From(() => TryApplyPlayerAnimation(player, firstCandidates, loopCandidates, token, retries - 1)).CallDeferred();
				}
				return;
			}

			MegaSprite sprite = new(creatureNode);
			MegaAnimationState state = sprite.GetAnimationState();
			if (state == null)
			{
				if (retries > 0)
				{
					Callable.From(() => TryApplyPlayerAnimation(player, firstCandidates, loopCandidates, token, retries - 1)).CallDeferred();
				}
				return;
			}

			string? firstAnim = FindFirstAvailable(sprite, firstCandidates);
			if (firstAnim == null)
			{
				return;
			}

			string? loopAnim = FindFirstAvailable(sprite, loopCandidates);
			if (loopAnim == null || string.Equals(firstAnim, loopAnim, StringComparison.Ordinal))
			{
				state.SetAnimation(firstAnim, loop: true);
				return;
			}

			state.SetAnimation(firstAnim, loop: false);
			state.AddAnimation(loopAnim, 0f, loop: true);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
	[HarmonyPostfix]
	public static void AfterDeathPostfix(IRunState runState, CombatState? combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		try
		{
			if (creature == null || !creature.IsPlayer)
			{
				return;
			}
			if (!LocalContext.IsMe(creature))
			{
				return;
			}
			if (!YukiTarget.IsTarget(creature.Player))
			{
				return;
			}
			if (wasRemovalPrevented)
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyCombatEnded();
			YukiBattleDeadOverlay.Play();
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NMouseCardPlay), nameof(NMouseCardPlay.Start))]
	[HarmonyPostfix]
	public static void AfterMouseCardPlayStart(NMouseCardPlay __instance)
	{
		try
		{
			CardModel? card = __instance.Holder?.CardModel;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyHovered(card!, hovered: true);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NHandCardHolder), "OnFocus")]
	[HarmonyPostfix]
	public static void AfterHandFocus(NHandCardHolder __instance)
	{
		try
		{
			CardModel? card = __instance.CardModel;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyUiFocused(card!, focused: true);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NHandCardHolder), "OnUnfocus")]
	[HarmonyPostfix]
	public static void AfterHandUnfocus(NHandCardHolder __instance)
	{
		try
		{
			CardModel? card = __instance.CardModel;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyUiFocused(card!, focused: false);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NHandCardHolder), "OnMousePressed")]
	[HarmonyPostfix]
	public static void AfterHandMousePressed(NHandCardHolder __instance, InputEvent inputEvent)
	{
		try
		{
			if (inputEvent is not InputEventMouseButton btn || btn.ButtonIndex != MouseButton.Left)
			{
				return;
			}

			CardModel? card = __instance.CardModel;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyHovered(card!, hovered: true);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NHandCardHolder), "DoCardHoverEffects")]
	[HarmonyPostfix]
	public static void AfterHandHoverEffects(NHandCardHolder __instance, bool isHovered)
	{
		try
		{
			CardModel? card = __instance.CardModel;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			if (isHovered)
			{
				YukiBattleReadyOverlay.NotifyHovered(card!, hovered: true);
				return;
			}

			if (__instance.HasFocus())
			{
				return;
			}
			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyHovered(card!, hovered: false);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
	[HarmonyPrefix]
	public static void BeforeCardPlayedPrefix(CombatState combatState, CardPlay cardPlay)
	{
		try
		{
			CardModel? card = cardPlay.Card;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyBeforeCardPlayed(cardPlay);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
	[HarmonyPostfix]
	public static void AfterBeforeCardPlayedPostfix(CombatState combatState, CardPlay cardPlay)
	{
		try
		{
			CardModel? maybeCard = cardPlay.Card;
			if (!YukiTarget.IsMineTargetCard(maybeCard))
			{
				return;
			}

			CardModel card = maybeCard!;
			string? id = card.Id.Entry;
			if (id == null || id.IndexOf("defend", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return;
			}

			TryPlayDefendAnimation(card.Owner);
		}
		catch
		{
		}
	}

	private static void TryPlayDefendAnimation(Player? player)
	{
		if (player == null)
		{
			return;
		}

		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null)
		{
			return;
		}

		NCreature? creatureNode = room.GetCreatureNode(player.Creature);
		if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode) || !creatureNode.HasSpineAnimation)
		{
			return;
		}

		try
		{
			MegaSprite sprite = new(creatureNode);
			string? defendAnim = FindFirstAvailable(sprite, DefendCandidates);
			if (defendAnim == null)
			{
				return;
			}

			MegaAnimationState state = sprite.GetAnimationState();
			state.SetAnimation(defendAnim, loop: false);
			string? loopAnim = FindFirstAvailable(sprite, CombatIdleCandidates);
			if (loopAnim != null)
			{
				state.AddAnimation(loopAnim, 0f, loop: true);
			}
		}
		catch
		{
			return;
		}
	}

	private static string? FindFirstAvailable(MegaSprite sprite, string[] candidates)
	{
		for (int i = 0; i < candidates.Length; i++)
		{
			string candidate = candidates[i];
			try
			{
				if (sprite.HasAnimation(candidate))
				{
					return candidate;
				}
			}
			catch
			{
				return null;
			}
		}

		return null;
	}

	[HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.CancelPlayCard))]
	[HarmonyPostfix]
	public static void AfterCancelPlayCard(NCardPlay __instance)
	{
		try
		{
			CardModel? card = __instance.Holder?.CardModel;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyCanceled(card!);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start))]
	[HarmonyPostfix]
	public static void AfterControllerPlayStart(NControllerCardPlay __instance)
	{
		try
		{
			CardModel? card = __instance.Holder?.CardNode?.Model;
			if (!YukiTarget.IsMineTargetCard(card))
			{
				return;
			}

			YukiBattleReadyOverlay.NotifyUiFocused(card!, focused: true);
		}
		catch
		{
		}
	}

	[HarmonyPatch(typeof(NControllerCardPlay), nameof(NControllerCardPlay._Input))]
	[HarmonyPostfix]
	public static void AfterControllerInput(NControllerCardPlay __instance, InputEvent inputEvent)
	{
		try
		{
			if (inputEvent is not InputEventAction { Pressed: true } action)
			{
				return;
			}

			if (action.Action == MegaInput.cancel)
			{
				CardModel? card = __instance.Holder?.CardModel;
				if (!YukiTarget.IsMineTargetCard(card))
				{
					return;
				}
				YukiBattleReadyOverlay.NotifyCanceled(card!);
			}
		}
		catch
		{
		}
	}
}
