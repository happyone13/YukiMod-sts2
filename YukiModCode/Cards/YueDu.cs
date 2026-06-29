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
public class YueDu() : YukiModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/yue_du_dynamic.tscn";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MoonshadowDamage", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<YueDuPower>(choiceContext, Owner.Creature, DynamicVars["MoonshadowDamage"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MoonshadowDamage"].UpgradeValueBy(1m);
    }
}
