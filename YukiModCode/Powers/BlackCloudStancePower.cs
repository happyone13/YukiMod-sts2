using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class BlackCloudStancePower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

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
