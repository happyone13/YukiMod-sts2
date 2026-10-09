using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Powers;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class CardBalanceSeptemberTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-card-balance-september");
    }

    [Fact]
    public async Task Updated_cards_have_requested_base_and_upgrade_values()
    {
        AssertDamage<DaoShangWu>(5m, 7m);
        AssertDamage<BiAnHua>(8m, 10m);
        AssertDamage<PoBingZhan>(9m, 12m);
        AssertDamage<MiHuoYiJi>(10m, 12m);

        var boRiJianYun = (BoRiJianYun)ModelDb.Card<BoRiJianYun>().ToMutable();
        Assert.Equal(4m, boRiJianYun.DynamicVars["BlackCloud"].BaseValue);
        CardCmd.Upgrade(boRiJianYun);
        Assert.Equal(6m, boRiJianYun.DynamicVars["BlackCloud"].BaseValue);

        var yiShi = (YiShi)ModelDb.Card<YiShi>().ToMutable();
        Assert.Equal(9m, yiShi.DynamicVars.Damage.BaseValue);
        CardCmd.Upgrade(yiShi);
        Assert.Equal(9m, yiShi.DynamicVars.Damage.BaseValue);

        await Play(await AddToHand<DefendYuki>());
    }

    [Fact]
    public async Task Upgraded_still_water_reduces_only_the_next_attack_by_two()
    {
        var yiShi = await AddToHand<YiShi>();
        CardCmd.Upgrade(yiShi);
        await PlayerCmd.SetEnergy(3m, Player);

        await Play(yiShi, EnemyAt(0));

        var costDownPower = Player.Creature.GetPower<YiShiNextAttackCostDownPower>();
        Assert.NotNull(costDownPower);
        Assert.Equal(2m, costDownPower.Amount);

        var nextAttack = await AddToHand<TouXiZhan>();
        Assert.Equal(0m, nextAttack.EnergyCost.GetWithModifiers(CostModifiers.All));
        await Play(nextAttack);

        Assert.Null(Player.Creature.GetPower<YiShiNextAttackCostDownPower>());
    }

    private static void AssertDamage<TCard>(decimal baseDamage, decimal upgradedDamage)
        where TCard : CardModel
    {
        var card = (TCard)ModelDb.Card<TCard>().ToMutable();
        Assert.Equal(baseDamage, card.DynamicVars.Damage.BaseValue);
        CardCmd.Upgrade(card);
        Assert.Equal(upgradedDamage, card.DynamicVars.Damage.BaseValue);
    }
}
