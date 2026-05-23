using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

public static class YukiTarget
{
	public const string CharacterId = YukiModInfo.CharacterId;

	public static bool IsTarget(Player? player)
	{
		return IsTarget(player?.Character);
	}

	public static bool IsTarget(CharacterModel? character)
	{
		return character != null && string.Equals(character.Id.Entry, CharacterId, StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsMineTargetCard(CardModel? card)
	{
		return card != null && IsTarget(card.Owner?.Character);
	}
}

