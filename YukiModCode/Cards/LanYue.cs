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
public class LanYue() : YukiModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override bool UseDynamicPortrait => true;
    public override string? CustomSpinePortraitScenePath => "res://YukiMod/scenes/cards/lan_yue_dynamic.tscn";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MoonshadowDamage", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LanYuePower>(choiceContext, Owner.Creature, DynamicVars["MoonshadowDamage"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MoonshadowDamage"].UpgradeValueBy(1m);
    }
}
