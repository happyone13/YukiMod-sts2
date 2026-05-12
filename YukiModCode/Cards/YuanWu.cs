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
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class YuanWu() : YukiModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string MoonshadowDamageKey = "MoonshadowDamage";

    public override YukiCardSchool School => YukiCardSchool.Moonshadow;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(8m, ValueProp.Move), new DynamicVar(MoonshadowDamageKey, 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner, DynamicVars[MoonshadowDamageKey].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MoonshadowDamageKey].UpgradeValueBy(2m);
    }
}
