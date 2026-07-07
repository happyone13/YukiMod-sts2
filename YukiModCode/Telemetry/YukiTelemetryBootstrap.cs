using STS2RitsuLib;
using STS2RitsuLib.Telemetry;

namespace YukiMod.YukiModCode.Telemetry;

internal static class YukiTelemetryBootstrap
{
    private static bool _initialized;

    internal static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        TelemetryRegistry.RegisterApplicant(CreateApplicant());
    }

    internal static TelemetryApplicant CreateApplicant()
    {
        return new TelemetryApplicant
        {
            ApplicantId = YukiTelemetryConfiguration.ApplicantId,
            OwnerModId = MainFile.ModId,
            DisplayName = YukiTelemetryConfiguration.DisplayName,
            Adapter = YukiTelemetryConfiguration.CreateAdapter(),
            Requests =
            [
                TelemetryRequest.BasicUsage("Session start, framework/game versions, platform, language, and anonymous install id."),
                TelemetryRequest.ModInventory("Installed mod list, versions, and load states for compatibility analysis."),
                TelemetryRequest.Diagnostics("Exception reports and runtime diagnostics."),
                TelemetryRequest.RunHistory(
                    "Complete Yuki run history after each run ends, including final deck, outcome, floor reached, ascension, and run duration for balance analysis.",
                    captureFilter: IsYukiRun)
            ]
        };
    }

    internal static bool IsYukiRun(RunEndedEvent evt)
    {
        return evt.Run.Players.Any(player => IsYukiCharacterId(player.CharacterId?.ToString()));
    }

    private static bool IsYukiCharacterId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains("YUKI", StringComparison.OrdinalIgnoreCase);
    }
}
