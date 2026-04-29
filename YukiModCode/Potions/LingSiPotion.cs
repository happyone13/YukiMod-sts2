using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Potions;

public class LingSiPotion : YukiModPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 2m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [YukiHoverTipFactory.FromInspiration()];

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var candidates = YukiInspirationService.GetInspirableCards(Owner, PileType.Hand)
            .Where(card => !YukiInspirationService.IsInspired(card))
            .ToList();

        var rng = Owner.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < DynamicVars["Amount"].BaseValue && candidates.Count > 0; i++)
        {
            var selectedCard = rng.NextItem(candidates);
            if (selectedCard == null)
            {
                break;
            }

            YukiInspirationService.ActivateInspiration(selectedCard);
            candidates.Remove(selectedCard);
        }

        return Task.CompletedTask;
    }
}
