using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class BlackCloudEnterNextTurnPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, YukiCombatState combatState)
    {
        if (player != Owner.Player || AmountOnTurnStart <= 0)
        {
            return;
        }

        Flash();
        await YukiBlackCloudService.Enter(choiceContext, player, this);
        await PowerCmd.Remove(this);
    }
}
