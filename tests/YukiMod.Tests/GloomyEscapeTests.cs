using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Encounters;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class GloomyEscapeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<YukiCharacter>().Encounter<GloomyPackEncounter>().WithSeed("yukimod-gloomy-escape");
    }

    [Fact]
    public async Task Encounter_deals_one_neutral_escape_token_before_opening_hand()
    {
        var card = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<GloomyEscape>());
        var encounter = Assert.IsType<GloomyPackEncounter>(Combat.Encounter);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Token, card.Rarity);
        Assert.Equal(TargetType.Self, card.TargetType);
        Assert.IsType<ColorlessCardPool>(card.Pool);
        Assert.Contains(CardKeyword.Retain, card.Keywords);
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        Assert.False(card.UseCustomFrame);
        Assert.True(encounter.EscapeCardsDealt);

        await Play(await AddToHand<DefendYuki>());
    }

    [Fact]
    public async Task Playing_escape_suppresses_rewards_and_escapes_only_enemies()
    {
        var card = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<GloomyEscape>());
        var encounter = Assert.IsType<GloomyPackEncounter>(Combat.Encounter);
        var enemyCount = Combat.Enemies.Count;

        await Play(card);

        Assert.True(encounter.WasPlayerEscape);
        Assert.False(encounter.ShouldGiveRewards);
        Assert.Equal(0f, encounter.CalculateGoldProportion(Combat));
        Assert.Empty(Combat.Enemies);
        Assert.Equal(enemyCount, Combat.EscapedCreatures.Count);
        Assert.DoesNotContain(Player.Creature, Combat.EscapedCreatures);
    }

    [Fact]
    public async Task Escape_state_round_trips_through_custom_state()
    {
        var encounter = Assert.IsType<GloomyPackEncounter>(Combat.Encounter);
        encounter.MarkPlayerEscaped();

        var restored = (GloomyPackEncounter)ModelDb.Encounter<GloomyPackEncounter>().ToMutable();
        restored.LoadCustomState(encounter.SaveCustomState());

        Assert.True(restored.WasPlayerEscape);
        Assert.False(restored.ShouldGiveRewards);

        await Play(await AddToHand<DefendYuki>());
    }

    [Fact]
    public async Task Injection_patch_is_noncritical_and_targets_before_combat_start()
    {
        Assert.False(GloomyEscapeCardBeforeCombatStartPatch.IsCritical);
        Assert.Equal("YukiMod.GloomyEscapeCard.BeforeCombatStart", GloomyEscapeCardBeforeCombatStartPatch.PatchId);
        Assert.NotEmpty(GloomyEscapeCardBeforeCombatStartPatch.GetTargets());

        await Play(await AddToHand<DefendYuki>());
    }
}

public sealed class GloomyEscapeMultiplayerTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddRemotePlayer<YukiCharacter>(2)
            .Encounter<GloomyPackEncounter>()
            .WithSeed("yukimod-gloomy-escape-multiplayer");
    }

    [Fact]
    public async Task Encounter_deals_exactly_one_escape_token_to_each_player()
    {
        Assert.Equal(2, Players.Count);
        foreach (var player in Players)
            Assert.Single(PileType.Hand.GetPile(player).Cards.OfType<GloomyEscape>());

        await Play(await AddToHand<DefendYuki>());
    }
}
