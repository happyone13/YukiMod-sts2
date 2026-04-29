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

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class LingDu() : YukiModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), HoverTipFactory.FromCard<JuHe>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Threshold", 33m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return PowerCmd.Apply<LingDuPower>(choiceContext, Owner.Creature, DynamicVars["Threshold"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Threshold"].UpgradeValueBy(-11m);
    }
}
