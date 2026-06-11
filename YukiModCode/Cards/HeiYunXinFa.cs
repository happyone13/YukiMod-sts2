using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunXinFa() : YukiModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/hei_yun_xin_fa_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiBlackCloudService.GrantKeepStanceThisTurn(choiceContext, Owner, this);
    }
}
