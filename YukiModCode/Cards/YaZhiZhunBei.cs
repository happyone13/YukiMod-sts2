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
public class YaZhiZhunBei() : YukiModCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/ya_zhi_zhun_bei_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        YukiAudioService.TryPlayCustomCastCardClip("ya_zhi_zhun_bei", Owner);

        var hand = PileType.Hand.GetPile(Owner);
        var drawLimit = YukiCardPileService.MaxCardsInHand - hand.Cards.Count;
        if (drawLimit <= 0)
        {
            return;
        }

        var eligibleAttacks = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(IsEligibleAttackForCurrentState)
            .ToList();
        if (eligibleAttacks.Count == 0)
        {
            return;
        }

        var selectedAttack = YukiInspirationService.WillTriggerOnPlay(this)
            ? eligibleAttacks.FirstOrDefault(IsInspiredAttack) ?? eligibleAttacks.First()
            : eligibleAttacks.First();
        if (selectedAttack == null)
        {
            return;
        }

        var drawPile = PileType.Draw.GetPile(Owner);
        if (selectedAttack.Pile?.Type != PileType.Draw || drawPile.Cards.FirstOrDefault() != selectedAttack)
        {
            await CardPileCmd.Add(selectedAttack, PileType.Draw, CardPilePosition.Top, this, skipVisuals: true);
        }

        await CardPileCmd.Draw(choiceContext, Owner);
    }

    protected override void OnUpgrade() { }

    private bool IsEligibleAttackForCurrentState(CardModel card) =>
        card.Type == CardType.Attack &&
        (!IsUpgraded || !card.Tags.Contains(CardTag.Strike));

    private static bool IsInspiredAttack(CardModel card) =>
        YukiInspirationService.IsInspirationSchoolCard(card);
}
