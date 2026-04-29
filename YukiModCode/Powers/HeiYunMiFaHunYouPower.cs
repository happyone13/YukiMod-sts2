using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class HeiYunMiFaHunYouPower : YukiModPower, IBlackCloudEnteredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
        await PowerCmd.Remove(this);
    }
}
