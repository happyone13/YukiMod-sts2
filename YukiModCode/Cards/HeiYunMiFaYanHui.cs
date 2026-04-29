using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunMiFaYanHui() : YukiModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud()];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiBlackCloudService.Resolve(
            choiceContext,
            this,
            () => Task.CompletedTask,
            BlackCloudKeepMode.ThisCard);

        var cards = YukiBlackCloudService.GetBlackCloudCards(Owner, PileType.Draw, PileType.Discard).ToList();
        if (cards.Count == 0)
        {
            return;
        }

        var selectedCard = Owner.RunState.Rng.CombatCardSelection.NextItem(cards);
        if (selectedCard != null)
        {
            await CardPileCmd.Add(selectedCard, PileType.Hand, source: this);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
