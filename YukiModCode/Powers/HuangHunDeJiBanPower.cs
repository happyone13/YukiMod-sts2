using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Powers;

public class HuangHunDeJiBanPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool IsInstanced => true;

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player != Owner.Player)
        {
            return amount;
        }

        return 0m;
    }

    public override Task AfterModifyingEnergyGain()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        var handCards = PileType.Hand.GetPile(player)
            .Cards
            .Where(card => card.CostsEnergyOrStars(includeGlobalModifiers: false))
            .ToList();
        if (handCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rng = player.RunState.Rng.CombatCardSelection;
        var selectedCards = new List<CardModel>(2);
        for (var i = 0; i < 2 && handCards.Count > 0; i++)
        {
            var card = rng.NextItem(handCards);
            if (card == null)
            {
                break;
            }

            selectedCards.Add(card);
            handCards.Remove(card);
        }

        if (selectedCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        foreach (var card in selectedCards)
        {
            card.EnergyCost.AddUntilPlayed(-1, reduceOnly: true);
        }

        return Task.CompletedTask;
    }
}
