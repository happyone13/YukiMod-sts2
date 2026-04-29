using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class HeiYunMiFaChuQiaoPower : YukiModPower, IBlackCloudExitedListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnBlackCloudExited(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || CombatState == null)
        {
            return;
        }

        Flash();
        for (var i = 0; i < Amount; i++)
        {
            await NaDao.CreateInHand(player, CombatState, upgraded: true);
        }

        await PowerCmd.Remove(this);
    }
}
