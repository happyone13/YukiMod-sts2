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
        get
        {
#if STS2_103
            return CardPile.maxCardsInHand;
#else
            return CardPile.MaxCardsInHand;
#endif
        }
    }

    public static Task AddGeneratedCardsToCombat(IEnumerable<CardModel> cards, PileType pileType, Player owner)
    {
#if STS2_103
        return CardPileCmd.AddGeneratedCardsToCombat(cards, pileType, addedByPlayer: true);
#else
        return CardPileCmd.AddGeneratedCardsToCombat(cards, pileType, owner);
#endif
    }
}
