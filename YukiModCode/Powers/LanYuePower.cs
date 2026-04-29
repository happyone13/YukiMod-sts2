using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class LanYuePower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public Task OnInspiredTriggered(CardModel sourceCard)
    {
        if (Owner.Player == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner.Player, Amount);
        return Task.CompletedTask;
    }
}
