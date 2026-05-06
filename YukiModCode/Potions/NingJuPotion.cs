using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Potions;

[Pool(typeof(YukiModPotionPool))]
public class NingJuPotion : YukiModPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [YukiHoverTipFactory.FromNingJu()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
#if STS2_104
        var combatState = Owner.Creature.CombatState;
#else
        var combatState = CombatState;
#endif
        if (combatState == null)
        {
            return;
        }

        await YukiMoonshadowService.NingJu(Owner, combatState, (int)DynamicVars["Amount"].BaseValue);
    }
}
