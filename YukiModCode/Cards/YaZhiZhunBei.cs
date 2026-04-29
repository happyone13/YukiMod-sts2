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
public class YaZhiZhunBei() : YukiModCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var drawPile = PileType.Draw.GetPile(Owner);
        var drawLimit = CardPile.MaxCardsInHand - hand.Cards.Count;
        if (drawLimit <= 0 || drawPile.Cards.Count == 0)
        {
            return;
        }

        var selectedAttack = drawPile.Cards
            .Where(IsEligibleAttack)
            .OrderByDescending(IsInspiredAttack)
            .FirstOrDefault();
        if (selectedAttack == null)
        {
            return;
        }

        await CardPileCmd.Add(selectedAttack, PileType.Draw, CardPilePosition.Top, this, skipVisuals: true);
        await CardPileCmd.Draw(choiceContext, Owner);
    }

    protected override void OnUpgrade() { }

    private static bool IsEligibleAttack(CardModel card) =>
        card.Type == CardType.Attack && !card.Tags.Contains(CardTag.Strike);

    private static bool IsInspiredAttack(CardModel card) =>
        card is YukiModCard yukiCard && yukiCard.School == YukiCardSchool.Inspiration;
}
