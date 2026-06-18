using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class Xue() : YukiModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), HoverTipFactory.FromPower<XuePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var hitCount = 1;
        if (YukiSnowMoonFlowerService.ShouldGrantBlackCloud(this))
        {
            await YukiBlackCloudService.Resolve(
                choiceContext,
                this,
                () =>
                {
                    hitCount++;
                    return Task.CompletedTask;
                });
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (YukiSnowMoonFlowerService.ShouldGrantMoonshadowDamage(this))
        {
            YukiMoonshadowService.GainMoonshadowDamageInHand(Owner, YukiSnowMoonFlowerService.SharedMoonshadowDamageBonus);
        }

        await YukiSnowMoonFlowerService.ApplyXue(choiceContext, Owner, CombatState, this);
        if (YukiInspirationService.WillTriggerOnPlay(this))
        {
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
