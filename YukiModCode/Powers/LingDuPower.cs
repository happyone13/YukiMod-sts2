using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class LingDuPower : YukiModPower, IInspiredTriggeredListener
{
    private sealed class Data
    {
        public int TriggerCount;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount
    {
        get
        {
            var threshold = Math.Max(1, (int)Amount);
            var progress = GetInternalData<Data>().TriggerCount % threshold;
            return progress == 0 ? threshold : threshold - progress;
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public async Task OnInspiredTriggered(PlayerChoiceContext choiceContext, Player player, CardModel sourceCard)
    {
        if (player != Owner.Player || CombatState == null)
        {
            return;
        }

        var threshold = Math.Max(1, (int)Amount);
        var data = GetInternalData<Data>();
        data.TriggerCount++;

        var createdAny = false;
        while (data.TriggerCount >= threshold)
        {
            data.TriggerCount -= threshold;
            await JuHe.CreateInHand(player, CombatState);
            createdAny = true;
        }

        if (createdAny)
        {
            Flash();
        }

        InvokeDisplayAmountChanged();
    }
}
