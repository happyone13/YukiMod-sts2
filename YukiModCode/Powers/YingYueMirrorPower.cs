using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class YingYueMirrorPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || !YukiMoonshadowService.CountsAsMoonshadow(cardPlay.Card))
        {
            return;
        }

        Flash();
        for (var i = 0; i < Amount; i++)
        {
            await YukiMoonshadowService.CloneToHand(cardPlay.Card);
        }

        await PowerCmd.Remove(this);
    }
}
