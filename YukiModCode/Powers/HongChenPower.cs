using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class HongChenPower : YukiModPower, IInspiredTriggeredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public Task OnInspiredTriggered(PlayerChoiceContext choiceContext, Player player, CardModel sourceCard)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        var candidates = PileType.Hand.GetPile(player)
            .Cards
            .Where(card => card.CostsEnergyOrStars(includeGlobalModifiers: false)
                && !card.EnergyCost.CostsX
                && card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        var card = player.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (card == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        card.EnergyCost.AddUntilPlayed(-Amount, reduceOnly: true);
        return Task.CompletedTask;
    }
}
