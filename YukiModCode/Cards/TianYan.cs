using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class TianYan() : YukiModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private int _currentDraw = 2;

    public override YukiCardSchool School => YukiCardSchool.Inspiration;

    [SavedProperty]
    public int CurrentDraw
    {
        get => _currentDraw;
        set
        {
            AssertMutable();
            _currentDraw = Math.Max(0, value);
            DynamicVars.Cards.BaseValue = _currentDraw;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(CurrentDraw)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawCount = CurrentDraw;
        if (drawCount > 0)
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);

        CurrentDraw = drawCount - 1;
    }

    protected override void OnUpgrade()
    {
        CurrentDraw++;
    }
}
