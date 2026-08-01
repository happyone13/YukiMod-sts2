using System.IO;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class Migration110Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-110-migration");
    }

    [Fact]
    public async Task Card_dupe_preserves_owner_with_the_110_api()
    {
        var card = await AddToHand<DefendYuki>();
        Assert.NotNull(card.Owner);
        var owner = card.Owner!;

        var duplicate = card.CreateDupe(owner);

        Assert.Same(owner, duplicate.Owner);
        await Play(card);

        var replayServiceSource = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "YukiModCode", "Services", "YukiCardReplayService.cs"));
        Assert.Contains("CreateDupe(owner)", replayServiceSource);
    }

    private static string RepositoryRoot(
        [System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
