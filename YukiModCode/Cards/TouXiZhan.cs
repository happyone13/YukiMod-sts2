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
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class TouXiZhan() : YukiModCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/tou_xi_zhan_dynamic.tscn";

    private const string CostDownKey = "CostDown";

    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(15m, ValueProp.Move), new DynamicVar(CostDownKey, 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        YukiAudioService.SuppressNextDefaultAttackSfx(Owner);
        YukiAudioService.TryPlayCustomCardClip("tou_xi_zhan", Owner);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || !YukiInspirationService.WillTriggerOnPlay(this) || originalCost <= 0m)
        {
            return false;
        }

        modifiedCost = originalCost - DynamicVars[CostDownKey].BaseValue;
        if (modifiedCost < 0m)
        {
            modifiedCost = 0m;
        }

        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
        DynamicVars[CostDownKey].UpgradeValueBy(1m);
    }
}
