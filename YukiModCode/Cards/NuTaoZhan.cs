using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class NuTaoZhan() : YukiModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const string ThresholdKey = "Threshold";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(ThresholdKey, 4m), new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<NuTaoZhanPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[ThresholdKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
