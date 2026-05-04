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
public class BingXueShouZhuo : YukiModRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        var candidates = YukiInspirationService.GetInspirationSchoolCards(player, PileType.Hand)
            .Where(card => !YukiInspirationService.IsInspired(card))
            .ToList();
        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        var selectedCard = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (selectedCard == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        YukiInspirationService.ActivateInspiration(selectedCard);
        return Task.CompletedTask;
    }
}
