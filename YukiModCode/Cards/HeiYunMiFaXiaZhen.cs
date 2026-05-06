using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunMiFaXiaZhen() : YukiModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BlackCloud", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiBlackCloudService.GainBlackCloud(choiceContext, Owner, DynamicVars["BlackCloud"].BaseValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlackCloud"].UpgradeValueBy(1m);
    }
}
