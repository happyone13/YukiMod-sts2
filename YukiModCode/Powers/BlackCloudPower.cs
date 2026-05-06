using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class BlackCloudPower : YukiModPower, IBlackCloudEnteredListener, IBlackCloudExitedListener
{
    private decimal _grantedStrength;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || _grantedStrength == 0)
        {
            return;
        }

        _grantedStrength += amount;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, amount, applier, cardSource, silent: true);
    }

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player || _grantedStrength != 0)
        {
            return;
        }

        Flash();
        _grantedStrength = Amount;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null, silent: true);
    }

    public async Task OnBlackCloudExited(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player || _grantedStrength == 0)
        {
            return;
        }

        var strengthToRemove = _grantedStrength;
        _grantedStrength = 0;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -strengthToRemove, Owner, null, silent: true);
        await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_grantedStrength == 0)
        {
            return;
        }

        var strengthToRemove = _grantedStrength;
        _grantedStrength = 0;
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), oldOwner, -strengthToRemove, oldOwner, null, silent: true);
    }
}
