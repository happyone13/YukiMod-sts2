using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Services;

public static class YukiForeseeService
{
    public static async Task<IReadOnlyList<CardModel>> ForeseeTopCards(
        PlayerChoiceContext choiceContext,
        Player player,
        int count,
        LocString prompt,
        AbstractModel source)
    {
        if (count <= 0)
        {
            return Array.Empty<CardModel>();
        }

        var drawPile = PileType.Draw.GetPile(player);
        var candidates = drawPile.Cards.Take(Math.Min(count, drawPile.Cards.Count)).ToList();
        if (candidates.Count == 0)
        {
            return Array.Empty<CardModel>();
        }

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                player,
                new CardSelectorPrefs(prompt, 0, candidates.Count)))
            .ToList();

        foreach (var selectedCard in selectedCards)
        {
            await CardPileCmd.Add(selectedCard, PileType.Discard, source: source);
        }

        return selectedCards;
    }
}
