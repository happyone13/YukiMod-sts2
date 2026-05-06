using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class HeiYunMiFaJiangLinPower : YukiModPower, IBlackCloudEnteredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        if (CombatState.HittableEnemies.Count > 0)
        {
            await YukiMod.YukiModCode.Services.YukiPowerService.Apply<VulnerablePower>(choiceContext, CombatState.HittableEnemies, Amount, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}
