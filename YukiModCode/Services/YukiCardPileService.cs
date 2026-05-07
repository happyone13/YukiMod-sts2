using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Services;

public static class YukiCardPileService
{
    public static int MaxCardsInHand
    {
        get { return CardPile.maxCardsInHand; }
    }

    public static Task AddGeneratedCardsToCombat(IEnumerable<CardModel> cards, PileType pileType, Player owner)
    {
        return CardPileCmd.AddGeneratedCardsToCombat(cards, pileType, addedByPlayer: owner != null);
    }
}
