using System;
using System.Collections.Generic;
using YukiMod.YukiModCode.Cards;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Mechanics.Animation;

public static class ChaosTeleportAttackProfiles
{
	private const string YukiCardIdPrefix = "YUKIMOD-";

	public static readonly ChaosTeleportAttackProfile Default = new ChaosTeleportAttackProfile(
		Id: "default_melee_teleport",
		TeleportDistance: 250f,
		UniformScaleMultiplier: 1.2f,
		VanillaTeleportAtRatio: 0.35f,
		ReadyAnim: "attack_ready",
		PlayAnim: "attack_play",
		AltPlayAnim: "attack_play2",
		EndAnim: "attack_end",
		Vfx: new ChaosTeleportAttackVfxSet(
			StepPlayerMoveB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_move_b.tscn",
			StepPlayerMoveF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_move_f.tscn",
			StepTargetArriveB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_arrive_b.tscn",
			StepTargetArriveF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_arrive_f.tscn",
			StepPlayerArriveB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_arrive_b.tscn",
			StepPlayerArriveF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_arrive_f.tscn",
			StepTargetMove: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_move.tscn",
			AttackPlayB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play1_b.tscn",
			AttackPlayF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play1_f.tscn",
			StepAnimBack: "eff_b",
			StepAnimFront: "eff_f",
			AttackPlayAnim: "animation"
		),
		AltAttackPlayB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play2_b.tscn",
		AltAttackPlayF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_attack_play2_f.tscn",
		ReturnVfxTiming: ChaosTeleportReturnVfxTiming.TargetMoveAtPlayEnd,
		ForceTargetsCenter: false,
		ForceLeftmostTarget: false,
		ReadyTeleportEvent: "",
		ReadyTeleportOffset: default,
		StepPlayerMoveEvent: "",
		DamageEvent: "",
		RandomizePlayAnim: true
	);

	public static readonly ChaosTeleportAttackProfile U3Attack = new ChaosTeleportAttackProfile(
		Id: "yuki_u3_attack",
		TeleportDistance: 250f,
		UniformScaleMultiplier: 1.2f,
		VanillaTeleportAtRatio: 0.35f,
		ReadyAnim: "u3_attack_ready",
		PlayAnim: "u3_attack_play",
		AltPlayAnim: "",
		EndAnim: "u3_attack_end",
		Vfx: new ChaosTeleportAttackVfxSet(
			StepPlayerMoveB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_move_b.tscn",
			StepPlayerMoveF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_move_f.tscn",
			StepTargetArriveB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_arrive_b.tscn",
			StepTargetArriveF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_arrive_f.tscn",
			StepPlayerArriveB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_arrive_b.tscn",
			StepPlayerArriveF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_player_arrive_f.tscn",
			StepTargetMove: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/step_target_move.tscn",
			AttackPlayB: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u3_attack_play_b.tscn",
			AttackPlayF: "res://YukiMod/ArtWorks/modspine/tscn_point/effect_scenes/yuki/yuki_u3_attack_play_f.tscn",
			StepAnimBack: "eff_b",
			StepAnimFront: "eff_f",
			AttackPlayAnim: "animation"
		),
		AltAttackPlayB: "",
		AltAttackPlayF: "",
		ReturnVfxTiming: ChaosTeleportReturnVfxTiming.TargetMoveAtPlayEnd,
		ForceTargetsCenter: false,
		ForceLeftmostTarget: true,
		ReadyTeleportEvent: "jump",
		ReadyTeleportOffset: new Vector2(0f, -500f),
		StepPlayerMoveEvent: "jump",
		DamageEvent: "",
		RandomizePlayAnim: false
	);

	public static readonly ChaosTeleportAttackProfile U2Attack = new ChaosTeleportAttackProfile(
		Id: "u2_attack",
		TeleportDistance: 0f,
		UniformScaleMultiplier: 1.2f,
		VanillaTeleportAtRatio: 0.35f,
		ReadyAnim: "u2_attack_ready",
		PlayAnim: "u2_attack_play",
		AltPlayAnim: "",
		EndAnim: "u2_attack_end",
		Vfx: Default.Vfx,
		AltAttackPlayB: "",
		AltAttackPlayF: "",
		ReturnVfxTiming: ChaosTeleportReturnVfxTiming.TargetMoveAtPlayEnd,
		ForceTargetsCenter: false,
		ForceLeftmostTarget: false,
		ReadyTeleportEvent: "",
		ReadyTeleportOffset: default,
		StepPlayerMoveEvent: "",
		DamageEvent: "",
		RandomizePlayAnim: false
	);

	private static readonly IReadOnlyDictionary<string, ChaosTeleportAttackProfile> ByCardId = new Dictionary<string, ChaosTeleportAttackProfile>(StringComparer.Ordinal)
	{
	};

	public static ChaosTeleportAttackProfile? Resolve(CardModel card)
	{
		if (card == null)
		{
			return null;
		}

		if (card is IChaosTeleportAttackProfileOverride o && !string.IsNullOrWhiteSpace(o.TeleportAttackProfileId))
		{
			if (TryGetByProfileId(o.TeleportAttackProfileId, out ChaosTeleportAttackProfile byId))
			{
				return byId;
			}
		}

		string cardId = "";
		try
		{
			cardId = card.Id.Entry;
		}
		catch
		{
		}

		if (!string.IsNullOrWhiteSpace(cardId) && ByCardId.TryGetValue(cardId, out ChaosTeleportAttackProfile profile))
		{
			return profile;
		}

		if (card.Type == CardType.Attack
		    && !string.IsNullOrWhiteSpace(cardId)
		    && cardId.StartsWith(YukiCardIdPrefix, StringComparison.Ordinal))
		{
			return Default;
		}

		if (card is IYukiMeleeAttackCard)
		{
			return Default;
		}

		return null;
	}

	public static bool TryGetByProfileId(string profileId, out ChaosTeleportAttackProfile profile)
	{
		if (string.Equals(profileId, Default.Id, StringComparison.Ordinal))
		{
			profile = Default;
			return true;
		}

		if (string.Equals(profileId, U3Attack.Id, StringComparison.Ordinal))
		{
			profile = U3Attack;
			return true;
		}

		if (string.Equals(profileId, U2Attack.Id, StringComparison.Ordinal))
		{
			profile = U2Attack;
			return true;
		}

		profile = default;
		return false;
	}
}

public interface IChaosTeleportAttackProfileOverride
{
	string TeleportAttackProfileId { get; }
}

public enum ChaosTeleportReturnVfxTiming
{
	PlayerArriveAtPlayEnd = 0,
	TargetMoveAtPlayEnd = 1
}

public readonly record struct ChaosTeleportAttackVfxSet(
	string StepPlayerMoveB,
	string StepPlayerMoveF,
	string StepTargetArriveB,
	string StepTargetArriveF,
	string StepPlayerArriveB,
	string StepPlayerArriveF,
	string StepTargetMove,
	string AttackPlayB,
	string AttackPlayF,
	string StepAnimBack,
	string StepAnimFront,
	string AttackPlayAnim
);

public readonly record struct ChaosTeleportAttackProfile(
	string Id,
	float TeleportDistance,
	float UniformScaleMultiplier,
	float VanillaTeleportAtRatio,
	string ReadyAnim,
	string PlayAnim,
	string AltPlayAnim,
	string EndAnim,
	ChaosTeleportAttackVfxSet Vfx,
	string AltAttackPlayB,
	string AltAttackPlayF,
	ChaosTeleportReturnVfxTiming ReturnVfxTiming,
	bool ForceTargetsCenter,
	bool ForceLeftmostTarget,
	string ReadyTeleportEvent,
	Vector2 ReadyTeleportOffset,
	string StepPlayerMoveEvent,
	string DamageEvent,
	bool RandomizePlayAnim
);
