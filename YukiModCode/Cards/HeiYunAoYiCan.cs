using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunAoYiCan() : YukiModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/hei_yun_ao_yi_can_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
        await YukiBlackCloudService.Enter(choiceContext, Owner, this);

        if (PileType.Hand.GetPile(Owner).Cards.Count == 0)
        {
            return;
        }

        var selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                null,
                this))
            .FirstOrDefault();
        if (selected != null)
        {
            await CardCmd.Exhaust(choiceContext, selected);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
