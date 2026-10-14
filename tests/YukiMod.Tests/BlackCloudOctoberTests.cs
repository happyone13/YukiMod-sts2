using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class BlackCloudOctoberTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<YukiCharacter>().AddEnemy<BigDummy>().AddEnemy<BigDummy>()
            .WithSeed("yukimod-black-cloud-october");
    }

    [Fact]
    public async Task Descent_attacks_once_then_applies_vulnerable_on_next_stance_entry()
    {
        var card = await AddToHand<HeiYunMiFaJiangLin>();
        var enemy = EnemyAt(0);
        var hp = enemy.CurrentHp;
        Assert.Equal(CardType.Attack, card.Type);
        Assert.True(YukiBlackCloudService.IsBlackCloudCard(card));

        await Play(card, enemy);

        Assert.Equal(hp - 4, enemy.CurrentHp);
        Assert.NotNull(Player.Creature.GetPower<HeiYunMiFaJiangLinPower>());
        Assert.Null(enemy.GetPower<VulnerablePower>());
        await YukiBlackCloudService.Enter(new ThrowingPlayerChoiceContext(), Player);
        Assert.Equal(1m, EnemyAt(0).GetPower<VulnerablePower>()!.Amount);
        Assert.Equal(1m, EnemyAt(1).GetPower<VulnerablePower>()!.Amount);
        Assert.Null(Player.Creature.GetPower<HeiYunMiFaJiangLinPower>());
    }

    [Fact]
    public async Task Upgraded_descent_attacks_three_times_in_black_cloud_without_delayed_vulnerable()
    {
        var card = await AddToHand<HeiYunMiFaJiangLin>();
        CardCmd.Upgrade(card);
        await ApplyPower<BlackCloudStancePower>(Player.Creature, 1, Player.Creature, card);
        var enemy = EnemyAt(0);
        var hp = enemy.CurrentHp;

        await Play(card, enemy);

        Assert.Equal(hp - 18, enemy.CurrentHp);
        Assert.Null(Player.Creature.GetPower<HeiYunMiFaJiangLinPower>());
        Assert.True(YukiBlackCloudService.IsActive(Player));
    }

    [Fact]
    public async Task Gathering_rain_refunds_one_energy_only_in_black_cloud()
    {
        var card = await AddToHand<HeiYunMiFaJiYu>();
        CardCmd.Upgrade(card);
        await ApplyPower<BlackCloudStancePower>(Player.Creature, 1, Player.Creature, card);
        await PlayerCmd.SetEnergy(3m, Player);

        await Play(card);

        Assert.Equal(11, Player.Creature.Block);
        Assert.Equal(3, Player.PlayerCombatState!.Energy);
        Assert.Null(Player.Creature.GetPower<BlackCloudEnterNextTurnPower>());
    }

    [Fact]
    public async Task Gathering_rain_schedules_next_turn_stance_without_refunding_energy_outside_black_cloud()
    {
        var card = await AddToHand<HeiYunMiFaJiYu>();
        await PlayerCmd.SetEnergy(3m, Player);

        await Play(card);

        Assert.Equal(8, Player.Creature.Block);
        Assert.Equal(2, Player.PlayerCombatState!.Energy);
        Assert.NotNull(Player.Creature.GetPower<BlackCloudEnterNextTurnPower>());
    }

    [Fact]
    public async Task Upgraded_twilight_draws_three_cards_without_granting_ignorance_in_black_cloud()
    {
        var card = await AddToHand<HeiYunMiFaHuangHun>();
        CardCmd.Upgrade(card);
        await ApplyPower<BlackCloudStancePower>(Player.Creature, 1, Player.Creature, card);
        var drawn = new List<CardModel>();
        for (var i = 0; i < 3; i++)
        {
            var defend = Combat.CreateCard<DefendYuki>(Player);
            drawn.Add(defend);
            await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Draw, Player, CardPilePosition.Top);
        }

        await Play(card);

        Assert.Equal(10, Player.Creature.Block);
        Assert.All(drawn, c => Assert.Contains(c, PileType.Hand.GetPile(Player).Cards));
        Assert.Null(Player.Creature.GetPower<BlackCloudPower>());
        Assert.Null(Player.Creature.GetPower<DelayedBlackCloudPower>());
    }

    [Fact]
    public async Task Twilight_grants_ignorance_without_drawing_outside_black_cloud()
    {
        var card = await AddToHand<HeiYunMiFaHuangHun>();
        var defend = Combat.CreateCard<DefendYuki>(Player);
        await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Draw, Player, CardPilePosition.Top);

        await Play(card);

        Assert.Equal(8, Player.Creature.Block);
        Assert.Equal(2m, Player.Creature.GetPower<BlackCloudPower>()!.Amount);
        Assert.Contains(defend, PileType.Draw.GetPile(Player).Cards);
    }

    [Fact]
    public async Task Upgraded_extinguish_deals_ten_damage_and_applies_two_weak_outside_black_cloud()
    {
        var card = await AddToHand<HeiYunAoYiMie>();
        CardCmd.Upgrade(card);
        var enemy = EnemyAt(0);
        var hp = enemy.CurrentHp;

        await Play(card, enemy);

        Assert.Equal(hp - 10, enemy.CurrentHp);
        Assert.Equal(2m, enemy.GetPower<WeakPower>()!.Amount);
        Assert.Equal(0, ModelDb.Card<BoRiJianYun>().EnergyCost.GetWithModifiers(CostModifiers.All));
    }
}
