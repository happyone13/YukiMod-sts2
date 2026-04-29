using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class ShuangJiangPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool IsInstanced => true;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner || originalCost <= 0m || !YukiInspirationService.WillTriggerOnPlay(card))
        {
            return false;
        }

        if (!card.Tags.Contains(CardTag.Strike) || card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }

        modifiedCost = originalCost - 1m;
        if (modifiedCost < 0m)
        {
            modifiedCost = 0m;
        }

        return modifiedCost != originalCost;
    }
}
