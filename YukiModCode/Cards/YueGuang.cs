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
public class YueGuang() : YukiModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string MoonshadowDamageKey = "MoonshadowDamage";

    public override YukiCardSchool School => YukiCardSchool.Moonshadow;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromNingJu(), HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(MoonshadowDamageKey, 5m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null)
        {
            return Task.CompletedTask;
        }

        return ResolveMoonlight(combatState);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MoonshadowDamageKey].UpgradeValueBy(3m);
    }

    private async Task ResolveMoonlight(YukiCombatState combatState)
    {
        await YukiMoonshadowService.NingJu(Owner, combatState, 1);
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner, DynamicVars[MoonshadowDamageKey].BaseValue);
    }
}
