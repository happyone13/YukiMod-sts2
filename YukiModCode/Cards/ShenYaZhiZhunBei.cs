using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ShenYaZhiZhunBei() : YukiModCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private static bool IsInspiredAttack(CardModel card) =>
        card.Type == CardType.Attack &&
        card is YukiModCard yukiCard &&
        yukiCard.School == YukiCardSchool.Inspiration;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var drawPile = PileType.Draw.GetPile(Owner);
        var drawLimit = CardPile.MaxCardsInHand - hand.Cards.Count;
        if (drawLimit <= 0 || drawPile.Cards.Count == 0)
        {
            return;
        }

        // Keep the base draw flow so hand limit and draw hooks still come from vanilla logic.
        var orderedAttackCards = drawPile.Cards
            .Where(card => card.Type == CardType.Attack)
            .OrderByDescending(IsInspiredAttack)
            .Take(drawLimit)
            .ToList();

        if (orderedAttackCards.Count == 0)
        {
            return;
        }

        foreach (var card in Enumerable.Reverse(orderedAttackCards))
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, this, skipVisuals: true);
        }

        await CardPileCmd.Draw(choiceContext, orderedAttackCards.Count, Owner);
    }

    protected override void OnUpgrade() { }
}
