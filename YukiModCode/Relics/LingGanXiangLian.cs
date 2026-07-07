using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class LingGanXiangLian : YukiModRelic, IInspiredTriggeredListener
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    public Task OnInspiredTriggered(PlayerChoiceContext choiceContext, Player player, CardModel sourceCard)
    {
        var combatState = Owner.Creature.CombatState;
        if (player != Owner || combatState == null)
        {
            return Task.CompletedTask;
        }

        Creature? target = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
        if (target == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        return CreatureCmd.Damage(choiceContext, target, 2m, ValueProp.Unpowered, sourceCard, null);
    }
}
