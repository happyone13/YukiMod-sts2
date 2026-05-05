using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Services;
using YukiMod.YukiModCode.StanceVfx;

namespace YukiMod.YukiModCode.Powers;

public class BlackCloudStancePower : YukiModPower
{
    private readonly YukiStanceVfxController _stanceVfx = new();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        return _stanceVfx.SetAura(Owner, YukiStanceVfxController.BlackCloudAuraScenePath);
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        return _stanceVfx.ClearAura();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Player == null || cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type == CardType.Attack)
        {
            return;
        }

        if (await YukiBlackCloudService.TryPreventNonAttackExit(choiceContext, Owner.Player))
        {
            return;
        }

        await YukiBlackCloudService.Exit(choiceContext, Owner.Player);
    }
}
