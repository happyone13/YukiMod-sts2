using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
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
public class XueYing() : YukiModCard(1, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [.. YukiHoverTipFactory.FromIai()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Threshold", 33m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        var allies = CombatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature is { IsAlive: true, IsPlayer: true })
            .ToList();
        if (allies.Count == 0)
        {
            allies.Add(Owner.Creature);
        }

        await YukiPowerService.Apply<XueYingPower>(
            choiceContext,
            allies,
            DynamicVars["Threshold"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
