using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(NoneCardPool))]
public class HeiYunMiJiHuiZhuan() : YukiModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;
    public override bool HasOwnBlackCloudEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), YukiHoverTipFactory.FromBlackCloud()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2), new DynamicVar("BlackCloud", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiBlackCloudService.DrawPrioritizedBlackCloudCard(choiceContext, Owner, this);

        if (YukiInspirationService.WillTriggerOnPlay(this))
        {
            await YukiBlackCloudService.GainBlackCloud(choiceContext, Owner, DynamicVars["BlackCloud"].BaseValue, this);
        }

        if (YukiBlackCloudService.IsActive(Owner))
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
        else
        {
            await YukiBlackCloudService.Enter(choiceContext, Owner, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlackCloud"].UpgradeValueBy(1m);
    }
}
