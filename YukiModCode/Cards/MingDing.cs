using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class MingDing() : YukiModCard(-1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override bool HasEnergyCostX => true;

    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;
    public override bool HasOwnBlackCloudEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), YukiHoverTipFactory.FromBlackCloud()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null)
            return;

        var x = ResolveEnergyXValue();
        var amount = x + (IsUpgraded ? 1 : 0);
        var hitCount = amount;

        if (YukiInspirationService.WillTriggerOnPlay(this))
            amount *= 2;
        if (YukiBlackCloudService.IsActive(Owner))
            hitCount *= 2;

        if (amount <= 0 || hitCount <= 0)
            return;

        await DamageCmd.Attack(amount)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
