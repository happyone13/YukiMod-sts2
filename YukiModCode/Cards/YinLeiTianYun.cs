using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class YinLeiTianYun() : YukiModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(3m)];

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<BlackCloudStancePower>(), HoverTipFactory.FromPower<StrengthPower>()];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return YukiMod.YukiModCode.Services.YukiPowerService.Apply<YinLeiTianYunPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Strength.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
    }
}
