using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace YukiMod.YukiModCode.Powers;

public class JuHeEndTurnDamagePower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var combatState = CombatState;
        if (side != Owner.Side || combatState == null)
        {
            return;
        }

        foreach (var enemy in combatState.HittableEnemies.ToList())
        {
            var missingHp = Math.Max(0, enemy.MaxHp - enemy.CurrentHp);
            var damage = Amount + Math.Ceiling(missingHp * 0.1m);
            await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Unpowered | ValueProp.Move, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}
