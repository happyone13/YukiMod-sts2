using System.Linq;
using System.Threading.Tasks;
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

public class HeiYunMiFaChuQiaoPower : YukiModPower, IBlackCloudEnteredListener
{
    private sealed class Data
    {
        public bool CreateUpgradedBlackCloudCard;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override LocString Description =>
        AddPowerDescriptionArgs(new LocString("powers", GetInternalData<Data>().CreateUpgradedBlackCloudCard
            ? $"{Id.Entry}.descriptionUpgraded"
            : $"{Id.Entry}.description"));

    protected override string SmartDescriptionLocKey =>
        GetInternalData<Data>().CreateUpgradedBlackCloudCard
            ? $"{Id.Entry}.smartDescriptionUpgraded"
            : base.SmartDescriptionLocKey;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().CreateUpgradedBlackCloudCard = cardSource?.IsUpgraded == true;
        return Task.CompletedTask;
    }

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || CombatState == null)
        {
            return;
        }

        var candidates = player.Character.CardPool.AllCards
            .Where(YukiBlackCloudService.IsBlackCloudCard)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        Flash();
        var createUpgradedBlackCloudCard = GetInternalData<Data>().CreateUpgradedBlackCloudCard;
        var rng = player.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < Amount; i++)
        {
            var canonicalCard = rng.NextItem(candidates);
            if (canonicalCard == null)
            {
                return;
            }

            var card = CombatState.CreateCard(canonicalCard, player);
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            if (createUpgradedBlackCloudCard && card.IsUpgradable && !card.IsUpgraded)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.None);
            }

            await YukiCardPileService.AddGeneratedCardsToCombat([card], PileType.Hand, player);
        }
    }
}
