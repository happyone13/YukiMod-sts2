using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

public sealed class YukiBattleReadyBeforeCombatStartPatch : IPatchMethod
{
    public static string PatchId => "YukiMod.BattleReadyOverlay.BeforeCombatStart";
    public static bool IsCritical => false;
    public static string Description => "Initialize Yuki combat presentation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.BeforeCombatStart), typeof(IRunState), typeof(ICombatState))
    ];

    public static void Postfix(IRunState runState, ICombatState? combatState) =>
        YukiBattleReadyOverlayPatches.AfterBeforeCombatStart(runState, combatState);
}

public sealed class YukiBattleReadyAfterCombatVictoryPatch : IPatchMethod
{
    public static string PatchId => "YukiMod.BattleReadyOverlay.AfterCombatVictory";
    public static bool IsCritical => false;
    public static string Description => "Finalize Yuki combat presentation after victory";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(
            typeof(Hook),
            nameof(Hook.AfterCombatVictory),
            typeof(IRunState),
            typeof(ICombatState),
            typeof(CombatRoom))
    ];

    public static void Postfix(IRunState runState, ICombatState? combatState) =>
        YukiBattleReadyOverlayPatches.AfterCombatVictory(runState, combatState);
}

public sealed class YukiBattleReadyAfterDeathPatch : IPatchMethod
{
    public static string PatchId => "YukiMod.BattleReadyOverlay.AfterDeath";
    public static bool IsCritical => false;
    public static string Description => "Finalize Yuki combat presentation after player death";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(
            typeof(Hook),
            nameof(Hook.AfterDeath),
            typeof(IRunState),
            typeof(ICombatState),
            typeof(Creature),
            typeof(bool),
            typeof(float))
    ];

    public static void Postfix(
        IRunState runState,
        ICombatState? combatState,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength) =>
        YukiBattleReadyOverlayPatches.AfterDeathPostfix(
            runState,
            combatState,
            creature,
            wasRemovalPrevented,
            deathAnimLength);
}

public sealed class YukiBattleReadyBeforeCardPlayedPrefixPatch : IPatchMethod
{
    public static string PatchId => "YukiMod.BattleReadyOverlay.BeforeCardPlayed.Prefix";
    public static bool IsCritical => false;
    public static string Description => "Prepare Yuki presentation before a card is played";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.BeforeCardPlayed), typeof(CombatState), typeof(CardPlay))
    ];

    public static void Prefix(CombatState combatState, CardPlay cardPlay) =>
        YukiBattleReadyOverlayPatches.BeforeCardPlayedPrefix(combatState, cardPlay);
}

public sealed class YukiBattleReadyBeforeCardPlayedPostfixPatch : IPatchMethod
{
    public static string PatchId => "YukiMod.BattleReadyOverlay.BeforeCardPlayed.Postfix";
    public static bool IsCritical => false;
    public static string Description => "Finalize Yuki presentation setup after a card starts playing";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.BeforeCardPlayed), typeof(CombatState), typeof(CardPlay))
    ];

    public static void Postfix(CombatState combatState, CardPlay cardPlay) =>
        YukiBattleReadyOverlayPatches.AfterBeforeCardPlayedPostfix(combatState, cardPlay);
}
