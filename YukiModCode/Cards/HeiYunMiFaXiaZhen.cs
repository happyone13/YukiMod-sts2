using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunMiFaXiaZhen() : YukiModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiBlackCloudService.Resolve(
            choiceContext,
            this,
            () => Task.CompletedTask,
            BlackCloudKeepMode.ThisCard);

        var drawnCard = (await CardPileCmd.Draw(choiceContext, 1m, Owner)).FirstOrDefault();
        drawnCard?.EnergyCost.AddUntilPlayed(-1, reduceOnly: true);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
