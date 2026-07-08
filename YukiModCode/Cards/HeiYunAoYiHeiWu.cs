using System;
using System.Collections.Generic;
using System.Linq;
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
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HeiYunAoYiHeiWu() : YukiModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/hei_yun_ao_yi_hei_wu_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;
    public override bool HasOwnBlackCloudEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud(), HoverTipFactory.FromPower<BlackCloudPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move), new DynamicVar("NoMing", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var hitCount = 1;
        var isBlackCloudActive = YukiBlackCloudService.IsActive(Owner);
        if (isBlackCloudActive)
        {
            var hand = PileType.Hand.GetPile(Owner).Cards
                .Where(card => !YukiBlackCloudService.IsBlackCloudCard(card))
                .ToList();
            foreach (var card in hand)
            {
                await CardCmd.Exhaust(choiceContext, card);
                if (card.Pile?.Type == PileType.Exhaust)
                {
                    hitCount++;
                }
            }
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!isBlackCloudActive)
        {
            await YukiBlackCloudService.GainBlackCloud(choiceContext, Owner, DynamicVars["NoMing"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["NoMing"].UpgradeValueBy(1m);
    }
}
