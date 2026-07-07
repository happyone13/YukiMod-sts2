using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunMiFaYanHui() : YukiModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;
    public override bool HasOwnBlackCloudEffect => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud(), HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!YukiBlackCloudService.IsActive(Owner))
        {
            await YukiMod.YukiModCode.Services.YukiPowerService.Apply<NextBlackCloudCardReplayPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            return;
        }

        var previousBlackCloud = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry =>
                entry.CardPlay.Card.Owner == Owner
                && entry.CardPlay.IsFirstInSeries
                && entry.CardPlay.Card is not HeiYunMiFaYanHui
                && YukiBlackCloudService.IsBlackCloudCard(entry.CardPlay.Card));
        if (previousBlackCloud == null)
        {
            return;
        }

        await YukiCardReplayService.AutoPlayClone(choiceContext, previousBlackCloud.CardPlay);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
