using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class MiHuoYiJi() : YukiModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IChaosTeleportAttackProfileOverride
{
    public string TeleportAttackProfileId => ChaosTeleportAttackProfiles.U3Attack.Id;

    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/mi_huo_yi_ji_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.Inspiration;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        YukiAudioService.TryPlayCustomAttackCardClip("mi_huo_yi_ji", Owner);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var candidates = YukiInspirationService.GetInspirableCards(Owner, PileType.Hand)
            .Where(card => !YukiInspirationService.IsInspired(card))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel? selectedCard;
        if (IsUpgraded)
        {
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
            selectedCard = (await CardSelectCmd.FromHand(
                    choiceContext,
                    Owner,
                    prefs,
                    card => YukiInspirationService.CanReceiveInspiration(card) && !YukiInspirationService.IsInspired(card),
                    this))
                .FirstOrDefault();
        }
        else
        {
            selectedCard = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        }

        if (selectedCard != null)
        {
            YukiInspirationService.ActivateInspiration(selectedCard);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
