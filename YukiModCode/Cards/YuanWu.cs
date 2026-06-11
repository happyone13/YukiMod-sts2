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
public class YuanWu() : YukiModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Moonshadow;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiMod.YukiModCode.HoverTips.YukiHoverTipFactory.FromNingJu(), HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(11m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (CombatState != null)
        {
            await YukiMoonshadowService.NingJu(Owner, CombatState, 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
