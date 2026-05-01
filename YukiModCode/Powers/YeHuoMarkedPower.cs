using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class YeHuoMarkedPower : YukiModPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool IsInstanced => true;

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || dealer?.Player == null || result.TotalDamage <= 0 || cardSource is not CardModel sourceCard)
        {
            return Task.CompletedTask;
        }

        if (sourceCard.Owner != dealer.Player || sourceCard.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }

        Flash();
        YukiMoonshadowService.GainMoonshadowDamageInHand(dealer.Player, Amount);
        return Task.CompletedTask;
    }
}
