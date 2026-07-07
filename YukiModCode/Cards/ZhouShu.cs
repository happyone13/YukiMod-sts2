using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ZhouShu() : YukiModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string MoonshadowDamageKey = "MoonshadowDamage";

    public override YukiCardSchool School => YukiCardSchool.Moonshadow;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(MoonshadowDamageKey, 5m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        YukiMoonshadowService.UpgradeMoonshadowInHand(Owner);
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner, DynamicVars[MoonshadowDamageKey].BaseValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
