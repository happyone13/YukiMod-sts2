using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.Tests;

public sealed class BasicCombatTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<YukiCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("yukimod-basic-smoke");
    }

    [Fact]
    public async Task StrikeYuki_deals_six_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var strike = await AddToHand<StrikeYuki>();

        await Play(strike, enemy);

        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }
}
