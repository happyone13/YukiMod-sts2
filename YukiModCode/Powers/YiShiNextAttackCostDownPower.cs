using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Powers;

public class YiShiNextAttackCostDownPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!IsValidTarget(card, originalCost))
        {
            return false;
        }

        modifiedCost -= Amount;
        if (modifiedCost < 0m)
        {
            modifiedCost = 0m;
        }

        return modifiedCost != originalCost;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (IsValidTarget(cardPlay.Card, cardPlay.Card.EnergyCost.GetResolved()))
        {
            await PowerCmd.Decrement(this);
        }
    }

    private bool IsValidTarget(CardModel card, decimal originalCost)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Attack || originalCost <= 0m)
        {
            return false;
        }

        return card.Pile?.Type is PileType.Hand or PileType.Play;
    }
}
