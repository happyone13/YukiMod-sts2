using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Infrastructure;
using YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

namespace YukiMod.YukiModCode.Mechanics.Animation;

internal static class YukiCardFlowFallback
{
    private static readonly System.Reflection.MethodInfo? GetResultPileTypeForCardPlayMethod =
        AccessTools.Method(typeof(CardModel), "GetResultPileTypeForCardPlay");

    internal static Task WrapCardFlowFallback(Task original, CardModel? card, PlayerChoiceContext? choiceContext, bool skipCardPileVisuals)
    {
        if (card == null || choiceContext == null || !YukiTarget.IsMineTargetCard(card))
        {
            return original ?? Task.CompletedTask;
        }

        return WrapCardFlowCore(original ?? Task.CompletedTask, card, choiceContext, skipCardPileVisuals);
    }

    private static async Task WrapCardFlowCore(Task original, CardModel card, PlayerChoiceContext choiceContext, bool skipCardPileVisuals)
    {
        Exception? error = null;
        try
        {
            await original.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
            TryLogCardFlowException(card, ex);
        }
        finally
        {
            try
            {
                await TryFinalizeCardFlow(card, choiceContext, skipCardPileVisuals, forceCleanup: error != null).ConfigureAwait(false);
            }
            catch (Exception finalizeEx)
            {
                try
                {
                    Log.Warn($"[{YukiModInfo.ModId}] CardFlow cleanup failed: card={GetCardId(card)} ex={finalizeEx.GetType().Name}: {finalizeEx.Message}");
                }
                catch
                {
                }
            }
        }
    }

    private static async Task TryFinalizeCardFlow(CardModel card, PlayerChoiceContext choiceContext, bool skipCardPileVisuals, bool forceCleanup)
    {
        CardPile? pile = null;
        try
        {
            pile = card.Pile;
        }
        catch
        {
        }

        if (pile == null || pile.Type != PileType.Play)
        {
            return;
        }

        bool shouldCleanup = forceCleanup;
        if (!shouldCleanup)
        {
            try
            {
                shouldCleanup = CombatManager.Instance == null || CombatManager.Instance.IsOverOrEnding || !CombatManager.Instance.IsInProgress;
            }
            catch
            {
                shouldCleanup = true;
            }
        }

        if (!shouldCleanup)
        {
            return;
        }

        PileType resultPileType = ResolveResultPileTypeForFallback(card);
        switch (resultPileType)
        {
            case PileType.None:
                await CardPileCmd.RemoveFromCombat(card, skipCardPileVisuals).ConfigureAwait(false);
                break;
            case PileType.Exhaust:
                await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false, skipCardPileVisuals).ConfigureAwait(false);
                break;
            default:
                await CardPileCmd.Add(card, resultPileType, CardPilePosition.Bottom, clonedBy: null, skipCardPileVisuals).ConfigureAwait(false);
                break;
        }

        if (forceCleanup)
        {
            try
            {
                Log.Info($"[{YukiModInfo.ModId}] CardFlow cleanup applied: card={GetCardId(card)} resultPile={resultPileType}");
            }
            catch
            {
            }
        }
    }

    private static PileType ResolveResultPileTypeForFallback(CardModel card)
    {
        try
        {
            if (GetResultPileTypeForCardPlayMethod != null && GetResultPileTypeForCardPlayMethod.Invoke(card, null) is PileType pileType)
            {
                return pileType;
            }
        }
        catch
        {
        }

        try
        {
            if (card.IsDupe || card.Type == CardType.Power)
            {
                return PileType.None;
            }

            if (card.ExhaustOnNextPlay || card.Keywords.Contains(CardKeyword.Exhaust))
            {
                return PileType.Exhaust;
            }
        }
        catch
        {
        }

        return PileType.Discard;
    }

    private static void TryLogCardFlowException(CardModel card, Exception ex)
    {
        try
        {
            string stack = ex.StackTrace?.Replace("\r", " ").Replace("\n", " | ") ?? "<no stack>";
            Log.Warn($"[{YukiModInfo.ModId}] CardFlow swallowed exception to finish cleanup: card={GetCardId(card)} exType={ex.GetType().FullName} message={ex.Message} stack={stack}");
        }
        catch
        {
        }
    }

    private static string GetCardId(CardModel? card)
    {
        try
        {
            return card?.Id.Entry ?? "";
        }
        catch
        {
            return "";
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class YukiCardFlowFallbackPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext, bool skipCardPileVisuals, ref Task __result)
    {
        __result = YukiCardFlowFallback.WrapCardFlowFallback(__result ?? Task.CompletedTask, __instance, choiceContext, skipCardPileVisuals);
    }
}

