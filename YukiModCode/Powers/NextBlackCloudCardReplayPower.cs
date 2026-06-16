using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class NextBlackCloudCardReplayPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != Owner || !YukiBlackCloudService.IsBlackCloudCard(card))
        {
            return playCount;
        }

        return playCount + Amount;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !YukiBlackCloudService.IsBlackCloudCard(cardPlay.Card))
        {
            return;
        }

        if (Amount > 1)
        {
            await PowerCmd.Decrement(this);
        }
        else
        {
            await PowerCmd.Remove(this);
        }
    }
}
