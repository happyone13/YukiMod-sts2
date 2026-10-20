using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.YukiModCode.Services;

public static class YukiReplayVisuals
{
    private static readonly AsyncLocal<CardModel?> ActiveCard = new();

    internal static CardModel? CurrentCard
    {
        get => ActiveCard.Value;
        set => ActiveCard.Value = value;
    }

    public static bool ShouldSkip(Creature? caster = null)
    {
        var card = CurrentCard;
        return card is { IsMutable: true, CurrentPlayIndex: > 0 }
            && card.Owner?.Character is YukiCharacter
            && (caster == null || card.Owner.Creature == caster);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class YukiReplayVisualContextPatch
{
    public static void Prefix(CardModel __instance, out CardModel? __state)
    {
        __state = YukiReplayVisuals.CurrentCard;
        YukiReplayVisuals.CurrentCard = __instance;
    }

    // The async body keeps the captured card; its caller/nested plays retain their own context.
    public static void Postfix(CardModel? __state) => YukiReplayVisuals.CurrentCard = __state;
    public static void Finalizer(CardModel? __state) => YukiReplayVisuals.CurrentCard = __state;
}

[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
public static class YukiReplayAttackAnimationPatch
{
    public static void Prefix(AttackCommand __instance)
    {
        if (__instance.CardPlay is { IsFirstInSeries: false } play
            && play.Card.Owner?.Character is YukiCharacter)
            __instance.WithNoAttackerAnim();
    }
}
