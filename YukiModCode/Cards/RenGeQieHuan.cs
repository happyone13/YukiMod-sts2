using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class RenGeQieHuan() : YukiModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string PutBackKey = "PutBack";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2), new DynamicVar(PutBackKey, 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var putBackCount = Math.Min(DynamicVars[PutBackKey].IntValue, hand.Cards.Count);
        if (putBackCount > 0)
        {
            var selected = await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, putBackCount),
                null,
                this);
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[PutBackKey].UpgradeValueBy(-1m);
    }
}
