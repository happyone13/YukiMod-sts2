using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class YinLeiTianYunPower : YukiModPower, IBlackCloudEnteredListener, IBlackCloudExitedListener
{
    private int _grantedStrength;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
#if STS2_104
        PlayerChoiceContext choiceContext,
#endif
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || Owner.Player == null || !YukiBlackCloudService.IsActive(Owner.Player))
        {
            return;
        }

        _grantedStrength += (int)amount;
#if STS2_103
        var choiceContext = new ThrowingPlayerChoiceContext();
#endif
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<StrengthPower>(choiceContext, Owner, amount, Owner, cardSource, silent: true);
    }

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player || _grantedStrength != 0)
        {
            return;
        }

        Flash();
        _grantedStrength = Amount;
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null, silent: true);
    }

    public async Task OnBlackCloudExited(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player || _grantedStrength == 0)
        {
            return;
        }

        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<StrengthPower>(choiceContext, Owner, -_grantedStrength, Owner, null, silent: true);
        _grantedStrength = 0;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_grantedStrength == 0)
        {
            return;
        }

        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), oldOwner, -_grantedStrength, oldOwner, null, silent: true);
        _grantedStrength = 0;
    }
}
