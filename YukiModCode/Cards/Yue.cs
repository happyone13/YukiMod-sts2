using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class Yue() : YukiModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool UseDynamicPortrait => true;
    public override string? CustomSpinePortraitScenePath => "res://YukiMod/scenes/cards/yue_dynamic.tscn";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move), new DynamicVar("MoonshadowDamage", 2m)];

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

        await YukiSnowMoonFlowerService.ApplyYue(choiceContext, Owner, CombatState, this);
        YukiMoonshadowService.GainMoonshadowDamageInHand(Owner, DynamicVars["MoonshadowDamage"].BaseValue);

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
