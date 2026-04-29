using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(TokenCardPool))]
public class YueYing() : YukiModTokenCard(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    private const string CurrentDamageKey = "CurrentDamage";

    public override YukiCardSchool School => YukiCardSchool.Moonshadow;
    public override bool IsRealMoonshadow => true;
    public override bool CountsAsMoonshadow => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];

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
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() { }

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

        if (!command.Results.Any(result => result.TotalDamage > 0))
        {
            return Task.CompletedTask;
        }

        DynamicVars.Damage.BaseValue += 1m;
        return Task.CompletedTask;
    }

    public static async Task<CardModel?> CreateInHand(Player owner, ICombatState combatState)
    {
        return (await CreateInHand(owner, 1, combatState)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState)
    {
        if (count <= 0 || CombatManager.Instance.IsOverOrEnding)
        {
            return Array.Empty<CardModel>();
        }

        var cards = new List<CardModel>(count);
        for (var i = 0; i < count; i++)
        {
            cards.Add(combatState.CreateCard<YueYing>(owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, owner);
        return cards;
    }
}
