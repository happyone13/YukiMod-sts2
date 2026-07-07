using System.IO;
using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using STS2RitsuLib.Telemetry;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class TelemetryScaffoldTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-telemetry-scaffold");
    }

    [Fact]
    public async Task Telemetry_is_registered_for_posthog_run_history()
    {
        await InitializeBattle();

        Assert.Contains(
            TelemetryRegistry.GetApplicants(),
            applicant => applicant.ApplicantId == "YukiMod" && applicant.OwnerModId == "YukiMod");

        var applicant = CreateApplicantForInspection();
        var requestedCategories = applicant.Requests.Select(request => request.Category).ToHashSet();
        var requestIds = applicant.Requests.Select(request => request.RequestId).ToHashSet(StringComparer.Ordinal);

        Assert.Equal("YukiMod", applicant.ApplicantId);
        Assert.Equal("YukiMod", applicant.OwnerModId);
        Assert.Equal("YukiMod", applicant.DisplayName);
        var postHogAdapter = Assert.IsType<PostHogTelemetryAdapter>(applicant.Adapter);
        Assert.Equal("https://us.i.posthog.com/", postHogAdapter.Host.ToString());
        Assert.Equal("posthog", postHogAdapter.AdapterId);
        Assert.Equal(4, applicant.Requests.Count);
        Assert.Contains(TelemetryDataCategory.BasicUsage, requestedCategories);
        Assert.Contains(TelemetryDataCategory.ModInventory, requestedCategories);
        Assert.Contains(TelemetryDataCategory.Diagnostics, requestedCategories);
        Assert.Contains(TelemetryDataCategory.RunHistory, requestedCategories);
        Assert.DoesNotContain(TelemetryDataCategory.Custom, requestedCategories);
        Assert.Contains("run_history", requestIds);
        Assert.DoesNotContain("yuki_balance", requestIds);
        Assert.DoesNotContain("meilin_balance", requestIds);

        var entrySource = File.ReadAllText(RepoFile("MainFile.cs"));
        var bootstrapSource = File.ReadAllText(RepoFile("YukiModCode", "Telemetry", "YukiTelemetryBootstrap.cs"));
        var configurationSource = File.ReadAllText(RepoFile("YukiModCode", "Telemetry", "YukiTelemetryConfiguration.cs"));

        Assert.Contains("YukiTelemetryBootstrap.Initialize();", entrySource);
        Assert.Contains("internal static class YukiTelemetryBootstrap", bootstrapSource);
        Assert.Contains("TelemetryRegistry.RegisterApplicant(CreateApplicant());", bootstrapSource);
        Assert.Contains("ApplicantId = YukiTelemetryConfiguration.ApplicantId", bootstrapSource);
        Assert.Contains("OwnerModId = MainFile.ModId", bootstrapSource);
        Assert.Contains("TelemetryRequest.BasicUsage", bootstrapSource);
        Assert.Contains("TelemetryRequest.ModInventory", bootstrapSource);
        Assert.Contains("TelemetryRequest.Diagnostics", bootstrapSource);
        Assert.Contains("TelemetryRequest.RunHistory", bootstrapSource);
        Assert.Contains("captureFilter: IsYukiRun", bootstrapSource);
        Assert.Contains("internal static bool IsYukiRun(RunEndedEvent evt)", bootstrapSource);
        Assert.DoesNotContain("TelemetryRequest.Custom", bootstrapSource);
        Assert.DoesNotContain("run_summary", bootstrapSource);
        Assert.DoesNotContain("meilin_balance", bootstrapSource);

        Assert.Contains("internal const string ApplicantId = MainFile.ModId;", configurationSource);
        Assert.Contains("internal const string DisplayName = \"YukiMod\";", configurationSource);
        Assert.Contains("internal const string BackendReference = \"PostHog\";", configurationSource);
        Assert.Contains("internal const string PostHogHost = \"https://us.i.posthog.com\";", configurationSource);
        Assert.Contains("new PostHogTelemetryAdapter(PostHogHost, PostHogProjectApiKey)", configurationSource);
        Assert.DoesNotContain("HttpJsonTelemetryAdapter", configurationSource);
        Assert.DoesNotContain("DisabledTelemetryAdapter", configurationSource);
    }

    private static string RepoFile(params string[] segments)
    {
        return Path.Combine(RepositoryRoot(), Path.Combine(segments));
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendYuki>();
        await Play(defend);
    }

    private static TelemetryApplicant CreateApplicantForInspection()
    {
        var bootstrapType = typeof(global::YukiMod.MainFile).Assembly.GetType(
                                "YukiMod.YukiModCode.Telemetry.YukiTelemetryBootstrap",
                                throwOnError: true)
                            ?? throw new InvalidOperationException("Telemetry bootstrap type was not found.");

        var createApplicant = bootstrapType.GetMethod(
                                  "CreateApplicant",
                                  BindingFlags.Static | BindingFlags.NonPublic)
                              ?? throw new MissingMethodException(bootstrapType.FullName, "CreateApplicant");

        return Assert.IsType<TelemetryApplicant>(createApplicant.Invoke(null, null));
    }
}
