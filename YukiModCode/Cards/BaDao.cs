using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class BaDao() : YukiModCard(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy), IChaosTeleportAttackProfileOverride
{
    public string TeleportAttackProfileId => ChaosTeleportAttackProfiles.U2Attack.Id;

    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/ba_dao_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;
    public override bool HasOwnBlackCloudEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        YukiAudioService.TryPlayCustomAttackCardClip("ba_dao", Owner);

        var hitCount = 1;
        var shouldEnterBlackCloud = !YukiBlackCloudService.IsActive(Owner);
        await YukiBlackCloudService.Resolve(
            choiceContext,
            this,
            () =>
            {
                hitCount++;
                return Task.CompletedTask;
            });

        if (shouldEnterBlackCloud)
        {
            await YukiBlackCloudService.Enter(choiceContext, Owner, this);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
