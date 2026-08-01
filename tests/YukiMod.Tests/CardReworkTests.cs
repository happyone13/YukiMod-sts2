using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using Godot;
using System.Reflection;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Patches;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class CardReworkTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-card-reworks");
    }

    [Fact]
    public async Task Static_card_shapes_match_the_new_design()
    {
        Assert.Equal(CardRarity.Uncommon, ModelDb.Card<ChenSi>().Rarity);
        Assert.Equal(CardRarity.Common, ModelDb.Card<BiAnHua>().Rarity);
        Assert.Equal(8m, ModelDb.Card<HuiNian>().DynamicVars.Block.BaseValue);
        var upgradedRecollection = (HuiNian)ModelDb.Card<HuiNian>().ToMutable();
        CardCmd.Upgrade(upgradedRecollection);
        Assert.Equal(11m, upgradedRecollection.DynamicVars.Block.BaseValue);
        Assert.Equal(2, ModelDb.Card<KuaiSuZhan>().DynamicVars.Repeat.IntValue);
        Assert.Equal(1m, ModelDb.Card<ManYue>().DynamicVars["MoonshadowDamage"].BaseValue);

        var upgradedFullMoon = (ManYue)ModelDb.Card<ManYue>().ToMutable();
        CardCmd.Upgrade(upgradedFullMoon);
        Assert.Contains(CardKeyword.Innate, upgradedFullMoon.Keywords);
        Assert.Equal(1m, upgradedFullMoon.DynamicVars["MoonshadowDamage"].BaseValue);

        Assert.True(ModelDb.Card<ZhanPoMingYun>().EnergyCost.CostsX);
        Assert.Equal(CardRarity.Uncommon, ModelDb.Card<ZhanPoMingYun>().Rarity);
        Assert.True(ModelDb.Card<MingDing>().EnergyCost.CostsX);
        Assert.Equal(CardType.Attack, ModelDb.Card<MingDing>().Type);
        Assert.Equal(CardRarity.Rare, ModelDb.Card<MingDing>().Rarity);
        Assert.Equal(TargetType.AllEnemies, ModelDb.Card<MingDing>().TargetType);
        Assert.IsType<YukiMod.YukiModCode.Character.YukiModCardPool>(ModelDb.Card<YiShan>().Pool);
        Assert.Equal(CardMultiplayerConstraint.MultiplayerOnly, ModelDb.Card<YiShan>().MultiplayerConstraint);

        var normalRegionsField = typeof(YukiCardCustomFramePatch).GetField(
            "NormalDigitRegions",
            BindingFlags.NonPublic | BindingFlags.Static);
        var normalRegions = Assert.IsType<Dictionary<char, Rect2>>(normalRegionsField?.GetValue(null));
        Assert.Equal(new Rect2(395f, 96f, 74f, 83f), normalRegions['X']);

        var atlasTextMethod = typeof(YukiCardCustomFramePatch).GetMethod(
            "IsAtlasCostText",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(Assert.IsType<bool>(atlasTextMethod?.Invoke(null, ["X"])));

        var fallbackTextMethod = typeof(YukiCardCustomFramePatch).GetMethod(
            "GetFallbackCostText",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Equal("x", Assert.IsType<string>(fallbackTextMethod?.Invoke(null, ["X"])));
        Assert.Equal("2", Assert.IsType<string>(fallbackTextMethod?.Invoke(null, ["2"])));

        await Play(await AddToHand<DefendYuki>());
    }

    [Fact]
    public async Task Dao_shang_wu_shares_its_sequence_across_all_copies_this_combat()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;

        await Play(await AddToHand<DaoShangWu>(), enemy);
        await Play(await AddToHand<DaoShangWu>(), enemy);
        await Play(await AddToHand<DaoShangWu>(), enemy);

        Assert.Equal(hpBefore - 36, enemy.CurrentHp);
        var sequencePower = Player.Creature.GetPower<DaoShangWuPower>();
        Assert.NotNull(sequencePower);
        Assert.Equal(3, sequencePower.Amount);
    }

    [Fact]
    public async Task Tian_yan_reduces_only_the_card_instance_that_was_played()
    {
        var first = await AddToHand<TianYan>();
        var second = await AddToHand<TianYan>();

        Assert.Equal(2, first.CurrentDraw);
        Assert.Equal(2, second.CurrentDraw);

        await Play(first);
        Assert.Equal(1, first.CurrentDraw);
        Assert.Equal(2, second.CurrentDraw);

        await CardPileCmd.Add(first, PileType.Hand, CardPilePosition.Top);
        await Play(first);
        Assert.Equal(0, first.CurrentDraw);
        Assert.Equal(2, second.CurrentDraw);
    }

    [Fact]
    public async Task Freezing_edge_hits_every_enemy_when_inspiration_triggers()
    {
        var source = await AddToHand<BingDianZhiRen>();
        var before = Combat.Enemies.Select(static enemy => enemy.CurrentHp).ToArray();
        var power = await ApplyPower<BingDianZhiRenPower>(Player.Creature, 4, Player.Creature, source);
        Assert.NotNull(power);

        await power.OnInspiredTriggered(new ThrowingPlayerChoiceContext(), Player, source);

        Assert.Equal(before[0] - 4, EnemyAt(0).CurrentHp);
        Assert.Equal(before[1] - 4, EnemyAt(1).CurrentHp);

        await Play(await AddToHand<DefendYuki>());
    }

    [Fact]
    public async Task Sever_fate_uses_x_for_hits_and_autoplays_drawn_attacks()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var strike = Combat.CreateCard<StrikeYuki>(Player);
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Draw, Player, CardPilePosition.Top);
        await PlayerCmd.SetEnergy(1m, Player);

        await Play(await AddToHand<ZhanPoMingYun>(), enemy);

        Assert.Equal(hpBefore - 11, enemy.CurrentHp);
    }

    [Fact]
    public async Task Fate_sealed_uses_x_for_damage_and_hit_count_against_all_enemies()
    {
        var before = Combat.Enemies.Select(static enemy => enemy.CurrentHp).ToArray();
        var card = await AddToHand<MingDing>();
        card.IsInspired = false;
        await PlayerCmd.SetEnergy(2m, Player);

        await Play(card);

        Assert.Equal(before[0] - 4, EnemyAt(0).CurrentHp);
        Assert.Equal(before[1] - 4, EnemyAt(1).CurrentHp);
    }
}
