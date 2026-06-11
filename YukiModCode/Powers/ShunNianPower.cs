using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class ShunNianPower : YukiModPower
{
    private sealed class Data
    {
        public bool CreateUpgradedInspirationCard;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override LocString Description =>
        AddPowerDescriptionArgs(new LocString("powers", GetInternalData<Data>().CreateUpgradedInspirationCard
            ? $"{Id.Entry}.descriptionUpgraded"
            : $"{Id.Entry}.description"));

    protected override string SmartDescriptionLocKey =>
        GetInternalData<Data>().CreateUpgradedInspirationCard
            ? $"{Id.Entry}.smartDescriptionUpgraded"
            : base.SmartDescriptionLocKey;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().CreateUpgradedInspirationCard = cardSource?.IsUpgraded == true;
        return Task.CompletedTask;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, YukiCombatState combatState)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var candidates = player.Character.CardPool.AllCards
            .Where(YukiInspirationService.IsInspirationCard)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        Flash();
        var rng = player.RunState.Rng.CombatCardSelection;
        var createUpgradedInspirationCard = GetInternalData<Data>().CreateUpgradedInspirationCard;
        for (var i = 0; i < Amount; i++)
        {
            var canonicalCard = rng.NextItem(candidates);
            if (canonicalCard == null)
            {
                return;
            }

            var card = combatState.CreateCard(canonicalCard, player);
            if (createUpgradedInspirationCard)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.None);
            }

            await YukiCardPileService.AddGeneratedCardsToCombat([card], PileType.Hand, player);
        }
    }
}
