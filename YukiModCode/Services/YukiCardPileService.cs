using System.Collections.Generic;
using System.Linq;
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

    public static async Task AddGeneratedCardsToCombat(IEnumerable<CardModel> cards, PileType pileType, Player owner)
    {
        var cardList = cards.ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(cardList, pileType, addedByPlayer: owner != null);

        if (pileType != PileType.Hand)
        {
            return;
        }

        foreach (var card in cardList)
        {
            YukiInspirationService.ActivateInspiration(card);
        }
    }
}
