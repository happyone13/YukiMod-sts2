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
public class HuiTian() : YukiModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromNextAttackPlayCount()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Repeat", 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return YukiMod.YukiModCode.Services.YukiPowerService.Apply<NextAttackPlayCountPower>(choiceContext, Owner.Creature, DynamicVars["Repeat"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
