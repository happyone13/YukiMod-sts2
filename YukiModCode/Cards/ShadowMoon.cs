using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ShadowMoon() : YukiModCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string CurrentDamageKey = "CurrentDamage";

    public override bool IsRealMoonshadow => false;
    public override bool CountsAsMoonshadow => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromCountsAsMoonshadow(), HoverTipFactory.FromCard<YueYing>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add(CurrentDamageKey, DynamicVars.Damage.BaseValue);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(YukiMoonshadowService.GetCurrentAttackDamage(this))
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner.Creature)
        {
            return Task.CompletedTask;
        }

        if (command.ModelSource == this || command.ModelSource is not CardModel sourceCard)
        {
            return Task.CompletedTask;
        }

        if (sourceCard.Owner != Owner || sourceCard.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }

        var hitCount = command.Results.SelectMany(result => result).Count(result => result.TotalDamage > 0);
        if (hitCount > 0)
        {
            DynamicVars.Damage.BaseValue += hitCount;
        }

        return Task.CompletedTask;
    }
}
