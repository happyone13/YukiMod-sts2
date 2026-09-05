using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Mechanics.Vfx;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class AnimationGenerationGuardTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-animation-generation-guards");
    }

    [Fact]
    public async Task Ready_watchers_are_bound_to_both_request_and_watch_generations()
    {
        var defend = await AddToHand<DefendYuki>();
        await Play(defend);

        ChaosVfxPrewarmReport prewarm =
            new ChaosVfxPrewarmReport(3, 2) + new ChaosVfxPrewarmReport(4, 4);
        Assert.Equal(7, prewarm.Requested);
        Assert.Equal(6, prewarm.Loaded);
        Assert.Equal(1, prewarm.Failed);

        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "YukiModCode",
            "Mechanics",
            "Animation",
            "YukiMeleeTeleportAttackPatch.cs"));

        Assert.Contains("StartReadyWatcher(attacker, requestId, watchId)", source);
        Assert.Contains(
            "StartReadyFallbackByTime(attacker, requestId, watchId, len)",
            source);
        Assert.True(
            CountOccurrences(source, "session.LastRequestId != requestId ||") >= 3,
            "Ready event, completion and timed fallback paths must all reject stale requests.");
        Assert.True(
            CountOccurrences(source, "RaiseAboveCombatants(room, attackerNode, session)") >= 3,
            "Every teleport path must raise Yuki above the combatant layer.");
        Assert.True(
            CountOccurrences(source, "RestoreLayer(attackerNode,") >= 4,
            "Every normal, completed, vanilla and chained return path must restore the layer snapshot.");
        Assert.Contains("session.OriginalSiblingIndex = attackerNode.GetIndex()", source);
        Assert.Contains("attackerNode.ZIndex = session.OriginalZIndex", source);

        string prewarmer = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "YukiModCode",
            "Mechanics",
            "Vfx",
            "YukiBattleVfxPrewarmer.cs"));
        Assert.Contains("[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]", prewarmer);
        Assert.Contains("await NextFrame(room);", prewarmer);
        Assert.Contains("ReferenceEquals(NCombatRoom.Instance, room)", prewarmer);
        Assert.Contains("generation == Volatile.Read(ref _generation)", prewarmer);
        Assert.Contains("YukiModSharedSettings.CombatEffectsEnabled", prewarmer);
        Assert.Contains("WarmAlpha = 0.001f", prewarmer);
    }

    private static int CountOccurrences(string value, string pattern)
    {
        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(pattern, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += pattern.Length;
        }

        return count;
    }

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        string testDirectory = Path.GetDirectoryName(sourcePath)
                               ?? throw new InvalidOperationException(
                                   "CallerFilePath did not provide a source directory.");
        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
