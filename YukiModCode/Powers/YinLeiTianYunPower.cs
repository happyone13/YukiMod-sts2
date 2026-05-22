using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class YinLeiTianYunPower : YukiModPower, IBlackCloudEnteredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<VigorPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
