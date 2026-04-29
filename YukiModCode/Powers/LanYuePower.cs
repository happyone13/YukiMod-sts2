using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class LanYuePower : YukiModPower, IInspiredTriggeredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public Task OnInspiredTriggered(PlayerChoiceContext choiceContext, Player player, CardModel sourceCard)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        Flash();
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner.Player, Amount);
        return Task.CompletedTask;
    }
}
