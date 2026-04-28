using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ShenYaZhiZhunBei() : YukiModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var drawCount = CardPile.MaxCardsInHand - hand.Cards.Count;
        if (drawCount > 0)
        {
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
