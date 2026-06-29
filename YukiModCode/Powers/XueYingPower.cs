using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Powers;

public class XueYingPower : YukiModPower
{
    private sealed class Data
    {
        public int CardsPlayed;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount
    {
        get
        {
            var threshold = Math.Max(1, (int)Amount);
            var progress = GetInternalData<Data>().CardsPlayed % threshold;
            return progress == 0 ? threshold : threshold - progress;
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || CombatState == null)
        {
            return;
        }

        var threshold = Math.Max(1, (int)Amount);
        var data = GetInternalData<Data>();
        data.CardsPlayed++;

        var createdAny = false;
        while (data.CardsPlayed >= threshold)
        {
            data.CardsPlayed -= threshold;
            await JuHe.CreateInHand(Owner.Player, CombatState);
            createdAny = true;
        }

        if (createdAny)
        {
            Flash();
        }

        InvokeDisplayAmountChanged();
    }
}
