using System.Collections.Generic;
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
public class HeiYunAoYiCan() : YukiModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;
    public override bool UseDynamicPortrait => true;
    public override string? CustomSpinePortraitScenePath => "res://YukiMod/scenes/cards/hei_yun_ao_yi_can_dynamic.tscn";

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

        await CardPileCmd.Draw(choiceContext, 1m, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
