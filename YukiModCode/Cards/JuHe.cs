using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Mechanics.Vfx;
using YukiMod.YukiModCode.Mechanics.Settings;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(TokenCardPool))]
public class JuHe() : YukiModTokenCard(0, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromJuHeKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(33m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (YukiModSharedSettings.CombatEffectsEnabled && YukiModSharedSettings.UltimateCinematicsEnabled)
        {
            YukiAudioService.SuppressNextDefaultAttackSfx(Owner);
            YukiAudioService.TryPlayUgAttackVoice(Owner);
            YukiAudioService.TryPlayUgAttackSound(Owner);
        }

        var combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        await YukiUgPresentation.PlayAsync(Owner.Creature, combatState.HittableEnemies.ToList(), async cinematic =>
        {
            foreach (var enemy in combatState.HittableEnemies.ToList())
            {
                var damage = DynamicVars.Damage.BaseValue + GetTenPercent(enemy.CurrentHp);
                var attack = DamageCmd.Attack(damage)
                    .FromCard(this, cardPlay)
                    .Targeting(enemy);
                if (cinematic)
                    attack.WithNoAttackerAnim();
                else
                    attack.WithHitFx("vfx/vfx_attack_slash");
                await attack.Execute(choiceContext);
            }

            await YukiMod.YukiModCode.Services.YukiPowerService.Apply<JuHeEndTurnDamagePower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Damage.BaseValue,
                Owner.Creature,
                this);
        });
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(17m);
    }

    public static async Task<CardModel?> CreateInHand(Player owner, ICombatState combatState, bool upgraded = false)
    {
        return (await CreateInHand(owner, 1, combatState, upgraded)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState, bool upgraded = false)
    {
        if (count <= 0 || CombatManager.Instance.IsOverOrEnding)
        {
            return Array.Empty<CardModel>();
        }

        var cards = new List<CardModel>(count);
        for (var i = 0; i < count; i++)
        {
            var card = combatState.CreateCard<JuHe>(owner);
            if (upgraded)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.None);
            }

            cards.Add(card);
        }

        await YukiCardPileService.AddGeneratedCardsToCombat(cards, PileType.Hand, owner);
        return cards;
    }

    private static decimal GetTenPercent(int hp)
    {
        return Math.Ceiling(hp * 0.1m);
    }
}
