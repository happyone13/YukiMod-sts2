using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class ChenSiPower : YukiModPower, IInspiredTriggeredListener
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
        return CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
