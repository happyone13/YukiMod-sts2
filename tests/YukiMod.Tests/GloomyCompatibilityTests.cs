using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Encounters;
using YukiMod.YukiModCode.Mechanics.Settings;
using Xunit;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class GloomyCompatibilityTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-gloomy-compatibility");
    }

    [Fact]
    public async Task Only_the_selected_gloomy_encounter_requests_its_playable_vfx()
    {
        var encounter = ModelDb.Encounter<GloomyPackEncounter>().ToMutable();
        encounter.GenerateMonstersWithSlots(Combat.RunState);
        var paths = encounter.GetAssetPaths(Combat.RunState)
            .Where(path => path.StartsWith("res://YukiMod/scenes/vfx/gloomy_", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(10, paths.Count);
        var ordinary = ModelDb.Encounter<BattlewornDummyEventV1Encounter>().ToMutable();
        ordinary.GenerateMonstersWithSlots(Combat.RunState);
        Assert.Empty(ordinary.GetAssetPaths(Combat.RunState).Intersect(paths));
        Assert.Empty(encounter.MapNodeAssetPaths.Intersect(paths));

        foreach (string path in paths)
        {
            var scene = ResourceLoader.Load<PackedScene>(path);
            Assert.NotNull(scene);
            var instance = scene.Instantiate<Node2D>();
            Assert.True(instance.GetChildCount() > 0, path);
            instance.Free();
        }
        await Play(await AddToHand<DefendYuki>());
    }

    [Fact]
    public async Task Provider_election_is_dynamic_and_order_independent()
    {
        var defend = await AddToHand<DefendYuki>();
        await Play(defend);

        const string prefix = "CHAOSMOD_GLOOMY_PROVIDER_";
        string[] providers = ["Fei", "YukiMod", "MeiLinMod"];
        Dictionary<string, object?> original = providers.ToDictionary(
            provider => provider,
            provider => AppDomain.CurrentDomain.GetData(prefix + provider));
        bool originalEnabled = GloomyEncounterSharedSettings.Enabled;

        try
        {
            foreach (string provider in providers)
                AppDomain.CurrentDomain.SetData(prefix + provider, null);

            GloomyEncounterSharedSettings.RegisterProvider("MeiLinMod");
            Assert.True(GloomyEncounterSharedSettings.IsActiveProvider("MeiLinMod"));

            GloomyEncounterSharedSettings.RegisterProvider("YukiMod");
            Assert.True(GloomyEncounterSharedSettings.IsActiveProvider("YukiMod"));
            Assert.False(GloomyEncounterSharedSettings.IsActiveProvider("MeiLinMod"));

            GloomyEncounterSharedSettings.RegisterProvider("Fei");
            Assert.True(GloomyEncounterSharedSettings.IsActiveProvider("Fei"));
            Assert.False(GloomyEncounterSharedSettings.IsActiveProvider("YukiMod"));
            Assert.False(GloomyEncounterSharedSettings.IsActiveProvider("MeiLinMod"));

            GloomyEncounterSharedSettings.SetEnabled(false, persist: false);
            Assert.False(GloomyEncounterSharedSettings.Enabled);
            GloomyEncounterSharedSettings.SetEnabled(true, persist: false);
            Assert.True(GloomyEncounterSharedSettings.Enabled);
        }
        finally
        {
            GloomyEncounterSharedSettings.SetEnabled(originalEnabled, persist: false);
            foreach ((string provider, object? value) in original)
                AppDomain.CurrentDomain.SetData(prefix + provider, value);
        }
    }
}
