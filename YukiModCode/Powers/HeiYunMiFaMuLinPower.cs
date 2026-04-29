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

public class HeiYunMiFaMuLinPower : YukiModPower, IBlackCloudEnteredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<HeiYunMiFaMuLinTemporaryStrengthPower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}
