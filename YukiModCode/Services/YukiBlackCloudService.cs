using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Services;

public enum BlackCloudKeepMode
{
    None,
    ThisCard,
    ThisTurn
}

public interface IBlackCloudEnteredListener
{
    Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, Player player);
}

public interface IBlackCloudExitedListener
{
    Task OnBlackCloudExited(PlayerChoiceContext choiceContext, Player player);
}

public static class YukiBlackCloudService
{
    public static bool IsActive(Player? player)
    {
        return GetStancePower(player) != null;
    }

    public static BlackCloudStancePower? GetStancePower(Player? player)
    {
        return player?.Creature.Powers.OfType<BlackCloudStancePower>().FirstOrDefault();
    }

    public static async Task Resolve(
        PlayerChoiceContext choiceContext,
        CardModel source,
        Func<Task> onActive,
        BlackCloudKeepMode keepMode = BlackCloudKeepMode.None)
    {
        if (source.Owner == null)
        {
            return;
        }

        if (IsActive(source.Owner))
        {
            await ApplyKeepMode(choiceContext, source.Owner, source, keepMode);
            await onActive();
            return;
        }

        await PowerCmd.Apply<BlackCloudPower>(choiceContext, source.Owner.Creature, 1m, source.Owner.Creature, source);
    }

    public static Task GainBlackCloud(PlayerChoiceContext choiceContext, Player player, decimal amount, CardModel? source = null)
    {
        return PowerCmd.Apply<BlackCloudPower>(choiceContext, player.Creature, amount, player.Creature, source);
    }

    public static async Task<bool> TryConsumeBlackCloud(PlayerChoiceContext choiceContext, Player player, decimal amount, CardModel? source = null)
    {
        var blackCloudPower = player.Creature.Powers.OfType<BlackCloudPower>().FirstOrDefault();
        if (blackCloudPower == null || blackCloudPower.Amount < amount)
        {
            return false;
        }

        await PowerCmd.ModifyAmount(choiceContext, blackCloudPower, -amount, player.Creature, source);
        return true;
    }

    public static async Task Enter(PlayerChoiceContext choiceContext, Player player, AbstractModel? source = null)
    {
        if (IsActive(player))
        {
            return;
        }

        if (source is CardModel { Type: not CardType.Attack } cardSource)
        {
            await GrantKeepStanceOnce(choiceContext, player, cardSource);
        }

        await PowerCmd.Apply<BlackCloudStancePower>(choiceContext, player.Creature, 1m, player.Creature, source as CardModel);
        await NotifyEntered(choiceContext, player);
    }

    public static async Task Exit(PlayerChoiceContext choiceContext, Player player)
    {
        var stancePower = GetStancePower(player);
        if (stancePower == null)
        {
            return;
        }

        await PowerCmd.Remove(stancePower);
        await NotifyExited(choiceContext, player);
    }

    public static async Task<bool> TryPreventNonAttackExit(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature.Powers.OfType<BlackCloudKeepStanceThisTurnPower>().Any())
        {
            return true;
        }

        var keepOncePower = player.Creature.Powers.OfType<BlackCloudKeepStanceOncePower>().FirstOrDefault();
        if (keepOncePower == null)
        {
            return false;
        }

        if (keepOncePower.Amount > 1)
        {
            await PowerCmd.Decrement(keepOncePower);
        }
        else
        {
            await PowerCmd.Remove(keepOncePower);
        }

        return true;
    }

    public static IEnumerable<CardModel> GetBlackCloudCards(Player owner, params PileType[] piles)
    {
        return piles
            .SelectMany(pileType => pileType.GetPile(owner).Cards)
            .Where(IsBlackCloudCard);
    }

    public static bool IsBlackCloudCard(CardModel card)
    {
        return card switch
        {
            YukiModCard yukiCard => yukiCard.School == YukiCardSchool.BlackCloud,
            YukiModTokenCard yukiTokenCard => yukiTokenCard.School == YukiCardSchool.BlackCloud,
            _ => false
        };
    }

    public static async Task<CardModel?> DrawPrioritizedBlackCloudCard(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel? source = null,
        bool fromHandDraw = false)
    {
        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var drawPile = PileType.Draw.GetPile(player);
        var prioritizedCard = drawPile.Cards.FirstOrDefault(IsBlackCloudCard);
        if (prioritizedCard != null && drawPile.Cards.FirstOrDefault() != prioritizedCard)
        {
            await CardPileCmd.Add(prioritizedCard, PileType.Draw, CardPilePosition.Top, source, skipVisuals: true);
        }

        return (await CardPileCmd.Draw(choiceContext, 1m, player, fromHandDraw)).FirstOrDefault();
    }

    public static Task GrantKeepStanceOnce(PlayerChoiceContext choiceContext, Player player, CardModel source)
    {
        return PowerCmd.Apply<BlackCloudKeepStanceOncePower>(choiceContext, player.Creature, 1m, player.Creature, source, silent: true);
    }

    public static Task GrantKeepStanceThisTurn(PlayerChoiceContext choiceContext, Player player, CardModel source)
    {
        return PowerCmd.Apply<BlackCloudKeepStanceThisTurnPower>(choiceContext, player.Creature, 1m, player.Creature, source, silent: true);
    }

    private static Task ApplyKeepMode(PlayerChoiceContext choiceContext, Player player, CardModel source, BlackCloudKeepMode keepMode)
    {
        return keepMode switch
        {
            BlackCloudKeepMode.ThisCard => GrantKeepStanceOnce(choiceContext, player, source),
            BlackCloudKeepMode.ThisTurn => GrantKeepStanceThisTurn(choiceContext, player, source),
            _ => Task.CompletedTask
        };
    }

    private static async Task NotifyEntered(PlayerChoiceContext choiceContext, Player player)
    {
        var listeners = player.Creature.Powers
            .OfType<IBlackCloudEnteredListener>()
            .Concat(player.Relics.OfType<IBlackCloudEnteredListener>())
            .ToList();
        foreach (var listener in listeners)
        {
            await listener.OnBlackCloudEntered(choiceContext, player);
        }
    }

    private static async Task NotifyExited(PlayerChoiceContext choiceContext, Player player)
    {
        var listeners = player.Creature.Powers
            .OfType<IBlackCloudExitedListener>()
            .Concat(player.Relics.OfType<IBlackCloudExitedListener>())
            .ToList();
        foreach (var listener in listeners)
        {
            await listener.OnBlackCloudExited(choiceContext, player);
        }
    }
}
