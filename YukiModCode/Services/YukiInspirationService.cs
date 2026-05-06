using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Services;

public interface IInspiredTriggeredListener
{
    Task OnInspiredTriggered(PlayerChoiceContext choiceContext, Player player, CardModel sourceCard);
}

public static class YukiInspirationService
{
    public static bool IsInspirationSchoolCard(CardModel card)
    {
        return card switch
        {
            YukiModCard yukiCard => yukiCard.School == YukiCardSchool.Inspiration,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.School == YukiCardSchool.Inspiration,
            _ => false
        };
    }

    public static bool HasOwnInspirationEffect(CardModel card)
    {
        return card switch
        {
            YukiModCard yukiCard => yukiCard.HasOwnInspirationEffect,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.HasOwnInspirationEffect,
            _ => false
        };
    }

    public static bool CanReceiveInspiration(CardModel card)
    {
        if (HasOwnInspirationEffect(card))
        {
            return true;
        }

        if (YukiSnowMoonFlowerService.ShouldGrantInspiration(card))
        {
            return true;
        }

        return card.Owner != null
            && card.Tags.Contains(CardTag.Strike)
            && card.Owner.Creature.Powers.OfType<ShuangJiangPower>().Any();
    }

    public static bool IsInspired(CardModel card)
    {
        return card switch
        {
            YukiModCard yukiCard => yukiCard.IsInspired,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.IsInspired,
            _ => false
        };
    }

    public static bool SetInspired(CardModel card, bool inspired)
    {
        switch (card)
        {
            case YukiModCard yukiCard:
                yukiCard.IsInspired = inspired;
                return true;
            case YukiModTokenCard yukiTokenCard:
                yukiTokenCard.IsInspired = inspired;
                return true;
            default:
                return false;
        }
    }

    public static bool ActivateInspiration(CardModel card)
    {
        if (!CanReceiveInspiration(card) || IsInspired(card))
        {
            return false;
        }

        return SetInspired(card, true);
    }

    public static bool WillTriggerOnPlay(CardModel card)
    {
        return card switch
        {
            YukiModCard { IsInspirationTriggeredForCurrentPlay: true } => true,
            YukiModTokenCard { IsInspirationTriggeredForCurrentPlay: true } => true,
            _ => CanReceiveInspiration(card) && IsInspired(card)
        };
    }

    public static IEnumerable<CardModel> GetInspirationSchoolCards(Player owner, params PileType[] piles)
    {
        return piles
            .SelectMany(pileType => pileType.GetPile(owner).Cards)
            .Where(IsInspirationSchoolCard);
    }

    public static IEnumerable<CardModel> GetInspirableCards(Player owner, params PileType[] piles)
    {
        return piles
            .SelectMany(pileType => pileType.GetPile(owner).Cards)
            .Where(CanReceiveInspiration);
    }

    public static IEnumerable<CardModel> GetInspiredCards(Player owner, params PileType[] piles)
    {
        return GetInspirableCards(owner, piles)
            .Where(IsInspired);
    }

    public static async Task<CardModel?> DrawPrioritizedInspirationCard(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel? source = null,
        bool fromHandDraw = false)
    {
        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var drawPile = PileType.Draw.GetPile(player);
        var prioritizedCard = drawPile.Cards.FirstOrDefault(IsInspirationSchoolCard);
        if (prioritizedCard != null && drawPile.Cards.FirstOrDefault() != prioritizedCard)
        {
            await CardPileCmd.Add(prioritizedCard, PileType.Draw, CardPilePosition.Top, source, skipVisuals: true);
        }

        return (await CardPileCmd.Draw(choiceContext, 1m, player, fromHandDraw)).FirstOrDefault();
    }

    public static async Task NotifyInspiredTriggered(PlayerChoiceContext choiceContext, Player owner, CardModel sourceCard)
    {
        var listeners = owner.Creature.Powers
            .OfType<IInspiredTriggeredListener>()
            .Concat(owner.Relics.OfType<IInspiredTriggeredListener>())
            .ToList();
        foreach (var listener in listeners)
        {
            await listener.OnInspiredTriggered(choiceContext, owner, sourceCard);
        }
    }
}
