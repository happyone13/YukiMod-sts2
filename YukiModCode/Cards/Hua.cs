using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class Hua() : YukiModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override YukiCardSchool School => YukiCardSchool.BlackCloud;
    public override bool HasOwnBlackCloudEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud(), HoverTipFactory.FromPower<HuaPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var hitCount = 1;
        if (YukiSnowMoonFlowerService.ShouldGrantExtraAttack(this))
        {
            hitCount++;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await YukiSnowMoonFlowerService.ApplyHua(choiceContext, Owner, CombatState, this);
        if (YukiInspirationService.WillTriggerOnPlay(this))
        {
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
        }
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this
            || originalCost <= 0m
            || Pile?.Type is not (PileType.Hand or PileType.Play)
            || !YukiBlackCloudService.IsActive(Owner))
        {
            return false;
        }

        modifiedCost = originalCost - 1m;
        if (modifiedCost < 0m)
        {
            modifiedCost = 0m;
        }

        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
