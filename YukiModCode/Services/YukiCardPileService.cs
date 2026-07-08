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
        get { return CardPile.MaxCardsInHand; }
    }

    public static async Task AddGeneratedCardsToCombat(IEnumerable<CardModel> cards, PileType pileType, Player owner)
    {
        var cardList = cards.ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(cardList, pileType, owner, CardPilePosition.Top);

        if (pileType != PileType.Hand)
        {
            return;
        }

        foreach (var card in cardList)
        {
            YukiInspirationService.ActivateInspiration(card);
        }
    }

    public static CardModel CloneForPlayer(CardModel card, Player owner)
    {
        var clone = card.CreateClone();
        clone.Owner = owner;
        return clone;
    }
}
