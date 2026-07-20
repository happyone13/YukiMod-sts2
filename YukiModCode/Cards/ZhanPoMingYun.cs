using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ZhanPoMingYun() : YukiModCard(-1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        var x = ResolveEnergyXValue();
        if (x <= 0)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(x)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var drawnCards = (await CardPileCmd.Draw(choiceContext, x, Owner)).ToArray();
        foreach (var drawnCard in drawnCards.Where(static card => card.Type == CardType.Attack))
            await CardCmd.AutoPlay(choiceContext, drawnCard, ResolveTarget(drawnCard, cardPlay.Target));
    }

    private Creature? ResolveTarget(CardModel drawnCard, Creature originalTarget)
    {
        return drawnCard.TargetType == TargetType.AnyEnemy
               && originalTarget.IsAlive
               && CombatState?.HittableEnemies.Contains(originalTarget) == true
            ? originalTarget
            : null;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
