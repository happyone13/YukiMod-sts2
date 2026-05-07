using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class YueDuPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
        {
            return Task.CompletedTask;
        }

        if (Owner.Player == null || command.ModelSource is not CardModel sourceCard)
        {
            return Task.CompletedTask;
        }

        if (sourceCard.Owner != Owner.Player || sourceCard.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }

        var hitCount = command.Results.Count(result => result.TotalDamage > 0);
        if (hitCount <= 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner.Player, Amount * hitCount);
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
