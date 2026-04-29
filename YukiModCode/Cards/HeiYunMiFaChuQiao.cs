using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunMiFaChuQiao() : YukiModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<BlackCloudStancePower>(), HoverTipFactory.FromCard<NaDao>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (YukiBlackCloudService.IsActive(Owner))
        {
            await YukiBlackCloudService.GrantKeepStanceOnce(choiceContext, Owner, this);
        }

        await YukiBlackCloudService.Enter(choiceContext, Owner, this);
        await PowerCmd.Apply<HeiYunMiFaChuQiaoPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}
