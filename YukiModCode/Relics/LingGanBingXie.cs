using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class LingGanBingXie : YukiModRelic
{
    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    public override Task BeforeCombatStart()
    {
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
#if STS2_104
        var combatState = Owner.Creature.CombatState;
#else
        var combatState = CombatState;
#endif
        if (player != Owner || combatState?.RoundNumber != 1 || _triggeredThisCombat)
        {
            return Task.CompletedTask;
        }

        var handCards = YukiInspirationService.GetInspirationSchoolCards(player, PileType.Hand)
            .Where(card => !YukiInspirationService.IsInspired(card))
            .ToList();

        _triggeredThisCombat = true;
        if (handCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        foreach (var card in handCards)
        {
            YukiInspirationService.ActivateInspiration(card);
        }

        return Task.CompletedTask;
    }
}
