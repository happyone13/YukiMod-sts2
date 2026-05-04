using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ShenYaZhiZhunBei() : YukiModCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    private static bool IsInspiredAttack(CardModel card) =>
        card.Type == CardType.Attack &&
        YukiInspirationService.IsInspirationSchoolCard(card);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var drawLimit = CardPile.MaxCardsInHand - hand.Cards.Count;
        if (drawLimit <= 0)
        {
            return;
        }

        for (var i = 0; i < drawLimit; i++)
        {
            var candidates = PileType.Draw.GetPile(Owner).Cards
                .Concat(PileType.Discard.GetPile(Owner).Cards)
                .Where(IsEligibleAttackForCurrentState)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var selectedAttack = YukiInspirationService.WillTriggerOnPlay(this)
                ? candidates.FirstOrDefault(IsInspiredAttack) ?? candidates.First()
                : candidates.First();
            if (selectedAttack == null)
            {
                return;
            }

            var drawPile = PileType.Draw.GetPile(Owner);
            if (selectedAttack.Pile?.Type != PileType.Draw || drawPile.Cards.FirstOrDefault() != selectedAttack)
            {
                await CardPileCmd.Add(selectedAttack, PileType.Draw, CardPilePosition.Top, this, skipVisuals: true);
            }

            var drawnCards = await CardPileCmd.Draw(choiceContext, 1m, Owner);
            if (!drawnCards.Any())
            {
                return;
            }
        }
    }

    protected override void OnUpgrade() { }

    private bool IsEligibleAttackForCurrentState(CardModel card) =>
        card.Type == CardType.Attack &&
        (!IsUpgraded || !card.Tags.Contains(CardTag.Strike));
}
