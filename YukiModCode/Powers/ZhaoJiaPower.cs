using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.HoverTips;

namespace YukiMod.YukiModCode.Powers;

public class ZhaoJiaPower : YukiModPower
{
    private sealed class Data
    {
        public int ExtraBlockGained;
        public CardModel? SourceCard;
        public bool IgnoredSourcePlay;
        public bool Triggered;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        YukiHoverTipFactory.FromIai();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => GetInternalData<Data>().ExtraBlockGained;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().SourceCard = cardSource;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
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

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        var data = GetInternalData<Data>();
        if (!data.IgnoredSourcePlay && cardPlay.Card == data.SourceCard)
        {
            data.IgnoredSourcePlay = true;
            return;
        }

        data.ExtraBlockGained++;
        Flash();
        InvokeDisplayAmountChanged();
        await CreatureCmd.GainBlock(Owner, 1m, ValueProp.Move, cardPlay);
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
