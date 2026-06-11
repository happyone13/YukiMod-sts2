using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class HeiYunMiFaChuQiaoPower : YukiModPower, IBlackCloudExitedListener
{
    private sealed class Data
    {
        public bool CreateUpgradedNaDao;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override LocString Description =>
        AddPowerDescriptionArgs(new LocString("powers", GetInternalData<Data>().CreateUpgradedNaDao
            ? $"{Id.Entry}.descriptionUpgraded"
            : $"{Id.Entry}.description"));

    protected override string SmartDescriptionLocKey =>
        GetInternalData<Data>().CreateUpgradedNaDao
            ? $"{Id.Entry}.smartDescriptionUpgraded"
            : base.SmartDescriptionLocKey;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().CreateUpgradedNaDao = cardSource?.IsUpgraded == true;
        return Task.CompletedTask;
    }

    public async Task OnBlackCloudExited(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || CombatState == null)
        {
            return;
        }

        Flash();
        var createUpgradedNaDao = GetInternalData<Data>().CreateUpgradedNaDao;
        for (var i = 0; i < Amount; i++)
        {
            await NaDao.CreateInHand(player, CombatState, upgraded: createUpgradedNaDao);
        }

        await PowerCmd.Remove(this);
    }
}
