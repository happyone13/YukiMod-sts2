using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(TokenCardPool))]
public class JuHe() : YukiModTokenCard(0, CardType.Attack, CardRarity.Token, TargetType.AllEnemies), IChaosTeleportAttackProfileOverride
{
    public string TeleportAttackProfileId => ChaosTeleportAttackProfiles.U2Attack.Id;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromJuHeKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(50m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (var enemy in combatState.HittableEnemies.ToList())
        {
            var damage = DynamicVars.Damage.BaseValue + GetTenPercent(enemy.CurrentHp);
            await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Move, Owner.Creature, this);
        }

        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<JuHeEndTurnDamagePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Damage.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(16m);
    }

    public static async Task<CardModel?> CreateInHand(Player owner, YukiCombatState combatState, bool upgraded = false)
    {
        return (await CreateInHand(owner, 1, combatState, upgraded)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, YukiCombatState combatState, bool upgraded = false)
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
