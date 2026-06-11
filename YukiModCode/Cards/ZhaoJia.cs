using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class ZhaoJia() : YukiModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/zhao_jia_dynamic.tscn";

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        YukiHoverTipFactory.FromIai();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new ZhaoJiaBlockVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var blockGain = GetCurrentBlockGain();
        await CreatureCmd.GainBlock(Owner.Creature, blockGain, ValueProp.Move, cardPlay);
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<ZhaoJiaPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, silent: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    private decimal GetCurrentBlockGain()
    {
        return DynamicVars.Block.BaseValue + GetFinishedCardsPlayedThisTurn();
    }

    private int GetFinishedCardsPlayedThisTurn()
    {
        var combatState = CombatState;
        if (combatState == null)
        {
            return 0;
        }

        return CombatManager.Instance.History.CardPlaysFinished.Count(
            entry => entry.Actor == Owner.Creature
                && entry.CardPlay.Card != this
                && entry.HappenedThisTurn(combatState));
    }

    private sealed class ZhaoJiaBlockVar(decimal block, ValueProp props) : BlockVar(block, props)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            var block = card is ZhaoJia zhaoJia
                ? zhaoJia.GetCurrentBlockGain()
                : BaseValue;

            var previewBlock = block;
            var enchantment = card.Enchantment;
            if (enchantment != null)
            {
                var enchantedBlock = BaseValue;
                enchantedBlock += enchantment.EnchantBlockAdditive(enchantedBlock);
                enchantedBlock *= enchantment.EnchantBlockMultiplicative(enchantedBlock);

                previewBlock += enchantment.EnchantBlockAdditive(previewBlock);
                previewBlock *= enchantment.EnchantBlockMultiplicative(previewBlock);
                if (!card.IsEnchantmentPreview)
                {
                    EnchantedValue = enchantedBlock;
                }
            }

            if (runGlobalHooks && card.CombatState != null)
            {
                previewBlock = Hook.ModifyBlock(card.CombatState, card.Owner.Creature, block, Props, card, null, out IEnumerable<AbstractModel> _);
            }
            else if (card.IsEnchantmentPreview)
            {
                previewBlock = block;
            }

            PreviewValue = previewBlock;
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return _owner is ZhaoJia zhaoJia
                ? zhaoJia.GetCurrentBlockGain()
                : base.GetBaseValueForIConvertible();
        }
    }
}
