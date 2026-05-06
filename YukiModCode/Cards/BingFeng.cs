using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class BingFeng() : YukiModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string InspiredWeakKey = "InspiredWeak";

    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move), new PowerVar<WeakPower>(1m), new DynamicVar(InspiredWeakKey, 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var weakAmount = DynamicVars.Weak.BaseValue;
        if (YukiInspirationService.WillTriggerOnPlay(this))
        {
            weakAmount += DynamicVars[InspiredWeakKey].BaseValue;
        }

        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<WeakPower>(choiceContext, cardPlay.Target, weakAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
