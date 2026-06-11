using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YukiMod.YukiModCode.Powers;

public class ZhenDaoPower : YukiModPower
{
    private sealed class Data
    {
        public int SpentEnergy;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount => Math.Max(1, Amount - GetInternalData<Data>().SpentEnergy);

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        var player = Owner.Player;
        if (player == null)
        {
            return;
        }

        var spentEnergy = cardPlay.Resources.EnergyValue;
        if (spentEnergy <= 0)
        {
            return;
        }

        var data = GetInternalData<Data>();
        data.SpentEnergy += spentEnergy;

        while (data.SpentEnergy >= Amount)
        {
            data.SpentEnergy -= Amount;
            Flash();
            await CardPileCmd.Draw(choiceContext, 1m, player);
        }

        InvokeDisplayAmountChanged();
    }
}
