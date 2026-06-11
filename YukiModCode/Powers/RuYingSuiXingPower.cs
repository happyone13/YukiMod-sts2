using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class RuYingSuiXingPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner.Player)
        {
            return;
        }

        if (!YukiBlackCloudService.IsActive(player))
        {
            if (player.Creature.Powers.OfType<HeiWuJiangLinPower>().Any())
            {
                await YukiBlackCloudService.Enter(choiceContext, player, this);
            }
            else
            {
                return;
            }
        }

        Flash();
        await YukiBlackCloudService.DrawPrioritizedBlackCloudCard(choiceContext, player, this, fromHandDraw: true);
    }
}
