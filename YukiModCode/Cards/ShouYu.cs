using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ShouYu() : YukiModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targetPlayer = cardPlay.Target?.Player;
        if (targetPlayer == null || PileType.Hand.GetPile(Owner).Cards.Count == 0)
        {
            return;
        }

        var selectedCards = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue),
                null,
                this))
            .ToList();

        foreach (var selectedCard in selectedCards)
        {
            await GiveHandCardToPlayer(selectedCard, targetPlayer);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    private async Task GiveHandCardToPlayer(CardModel card, MegaCrit.Sts2.Core.Entities.Players.Player targetPlayer)
    {
        card.RemoveFromCurrentPile(false);
        card.GiveToAnotherPlayer(targetPlayer);
        await CardPileCmd.Add(
            [card],
            PileType.Hand.GetPile(targetPlayer),
            CardPilePosition.Random,
            this,
            skipVisuals: false,
            isChangingOwners: true);
    }
}
