using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Powers;

public class ZhaoJiaPower : YukiModPower
{
    private sealed class Data
    {
        public bool Triggered;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer.Side == Owner.Side)
        {
            return Task.CompletedTask;
        }

        if (!props.IsCardOrMonsterMove() || !result.WasFullyBlocked || result.BlockedDamage <= 0 || Owner.Block != 0)
        {
            return Task.CompletedTask;
        }

        var data = GetInternalData<Data>();
        if (!data.Triggered)
        {
            data.Triggered = true;
            Flash();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        if (GetInternalData<Data>().Triggered && CombatState != null)
        {
            Flash();
            await JuHe.CreateInHand(player, CombatState);
        }

        await PowerCmd.Remove(this);
    }
}
