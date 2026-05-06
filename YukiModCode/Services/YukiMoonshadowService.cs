using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Services;

public static class YukiMoonshadowService
{
    public static async Task NingJu(Player owner, YukiCombatState combatState, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        for (var i = 0; i < amount; i++)
        {
            var moonshadowCards = GetMoonshadowCardsInHand(owner, realOnly: true).ToList();

            if (moonshadowCards.Count == 0)
            {
                await YueYing.CreateInHand(owner, combatState);
                continue;
            }

            ModifyMoonshadowCostInHand(owner, 1);
            GrantMoonshadowReplayInHand(owner, 1);
        }
    }

    public static void GainMoonshadowDamageInHand(Player owner, decimal amount)
    {
        if (amount == 0)
        {
            return;
        }

        foreach (var moonshadowCard in GetMoonshadowCardsInHand(owner))
        {
            if (moonshadowCard.DynamicVars.ContainsKey("Damage"))
            {
                moonshadowCard.DynamicVars.Damage.BaseValue += amount;
            }
        }
    }

    public static void ModifyMoonshadowCostInHand(Player owner, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        foreach (var moonshadowCard in GetMoonshadowCardsInHand(owner))
        {
            if (!moonshadowCard.EnergyCost.CostsX
                && moonshadowCard.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0)
            {
                moonshadowCard.EnergyCost.AddThisCombat(delta);
            }
        }
    }

    public static void GrantMoonshadowReplayInHand(Player owner, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        foreach (var moonshadowCard in GetMoonshadowCardsInHand(owner))
        {
            moonshadowCard.BaseReplayCount += amount;
        }
    }

    public static void GrantBlackCloudDamageBonusInHand(Player owner, decimal bonusMultiplier)
    {
        if (bonusMultiplier == 0)
        {
            return;
        }

        foreach (var moonshadowCard in GetMoonshadowCardsInHand(owner))
        {
            AddBlackCloudDamageBonus(moonshadowCard, bonusMultiplier);
        }
    }

    public static decimal GetCurrentAttackDamage(CardModel card)
    {
        var baseDamage = card.DynamicVars.ContainsKey("Damage")
            ? card.DynamicVars.Damage.BaseValue
            : 0m;

        if (!CountsAsMoonshadow(card) || card.Owner == null || !YukiBlackCloudService.IsActive(card.Owner))
        {
            return baseDamage;
        }

        var bonus = GetBlackCloudDamageBonus(card);
        if (bonus == 0)
        {
            return baseDamage;
        }

        return baseDamage * (1m + bonus);
    }

    public static async Task<CardModel?> CloneToHand(CardModel card)
    {
        if (card.Owner == null)
        {
            return null;
        }

        var clone = card.CreateClone();
        await YukiCardPileService.AddGeneratedCardsToCombat([clone], PileType.Hand, card.Owner);
        return clone;
    }

    public static bool IsRealMoonshadow(CardModel card) =>
        card switch
        {
            YukiModCard yukiCard => yukiCard.IsRealMoonshadow,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.IsRealMoonshadow,
            _ => false
        };

    public static bool CountsAsMoonshadow(CardModel card) =>
        card switch
        {
            YukiModCard yukiCard => yukiCard.CountsAsMoonshadow,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.CountsAsMoonshadow,
            _ => false
        };

    public static IEnumerable<CardModel> GetMoonshadowCardsInHand(Player owner, bool realOnly = false)
    {
        return PileType.Hand.GetPile(owner)
            .Cards
            .Where(card => realOnly ? IsRealMoonshadow(card) : CountsAsMoonshadow(card));
    }

    private static void AddBlackCloudDamageBonus(CardModel card, decimal bonusMultiplier)
    {
        switch (card)
        {
            case YukiModCard yukiCard:
                yukiCard.MoonshadowBlackCloudDamageMultiplierBonus += bonusMultiplier;
                break;
            case YukiModTokenCard yukiTokenCard:
                yukiTokenCard.MoonshadowBlackCloudDamageMultiplierBonus += bonusMultiplier;
                break;
        }
    }

    private static decimal GetBlackCloudDamageBonus(CardModel card) =>
        card switch
        {
            YukiModCard yukiCard => yukiCard.MoonshadowBlackCloudDamageMultiplierBonus,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.MoonshadowBlackCloudDamageMultiplierBonus,
            _ => 0m
        };
}
