using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunMiFaMuLin() : YukiModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1), new CardsVar(1), new DynamicVar("DelayedEnergy", 2m), new DynamicVar("DelayedCards", 2m)];

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud(), EnergyHoverTip];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (YukiBlackCloudService.IsActive(Owner))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            await YukiBlackCloudService.GrantKeepStanceOnce(choiceContext, Owner, this);
        }
        else if (await YukiBlackCloudService.TryConsumeBlackCloud(choiceContext, Owner, 2m, this))
        {
            await YukiMod.YukiModCode.Services.YukiPowerService.Apply<HeiYunMiFaHunYouPower>(choiceContext, Owner.Creature, DynamicVars["DelayedEnergy"].BaseValue, Owner.Creature, this);
            await YukiMod.YukiModCode.Services.YukiPowerService.Apply<HeiYunMiFaYingFuPower>(choiceContext, Owner.Creature, DynamicVars["DelayedCards"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["DelayedCards"].UpgradeValueBy(1m);
    }
}
