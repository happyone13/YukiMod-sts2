using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class TianJiZhanJi() : YukiModCard(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    protected override string? CustomPowerCastClipKey => "tian_ji_zhan_ji";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawCards = PileType.Draw.GetPile(Owner).Cards.ToList();
        foreach (var card in drawCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        var discardCards = PileType.Discard.GetPile(Owner).Cards.ToList();
        foreach (var card in discardCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var drawCount = handCards.Count;
        if (drawCount == 0)
        {
            return;
        }

        await CardCmd.Discard(choiceContext, handCards);
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
