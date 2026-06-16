using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class ManTianPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player == null
            || cardPlay.Card.Owner != player
            || !YukiBlackCloudService.IsActive(player)
            || !YukiBlackCloudService.IsBlackCloudCard(cardPlay.Card))
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}
