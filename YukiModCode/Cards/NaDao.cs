using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(TokenCardPool))]
public class NaDao() : YukiModTokenCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [.. YukiHoverTipFactory.FromIai(), YukiHoverTipFactory.FromBlackCloud()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        var combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        var attacksPlayedThisTurn = CombatManager.Instance.History.CardPlaysStarted.Count(
            entry => entry.CardPlay.Card.Type == CardType.Attack
                && entry.CardPlay.Card.Owner == Owner
                && entry.HappenedThisTurn(combatState));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(attacksPlayedThisTurn + 1)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this)
        {
            return false;
        }

        if (Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }

        var hasOtherAttackInHand = PileType.Hand.GetPile(Owner).Cards.Any(c => c != this && c.Type == CardType.Attack);
        if (hasOtherAttackInHand)
        {
            return false;
        }

        modifiedCost = Math.Max(0m, originalCost - 1m);
        return true;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    public static async Task<CardModel?> CreateInHand(Player owner, ICombatState combatState, bool upgraded = false)
    {
        return (await CreateInHand(owner, 1, combatState, upgraded)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState, bool upgraded = false)
    {
        if (count <= 0 || CombatManager.Instance.IsOverOrEnding)
        {
            return Array.Empty<CardModel>();
        }

        var cards = new List<CardModel>(count);
        for (var i = 0; i < count; i++)
        {
            var card = combatState.CreateCard<NaDao>(owner);
            if (upgraded)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.None);
            }

            cards.Add(card);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, owner);
        return cards;
    }
}
