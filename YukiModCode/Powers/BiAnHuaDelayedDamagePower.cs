using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace YukiMod.YukiModCode.Powers;

public class BiAnHuaDelayedDamagePower : YukiModPower
{
    private sealed class Data
    {
        public int TriggersRemaining = 2;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Applier?.Player || Owner == null || !Owner.IsAlive)
        {
            return;
        }

        var data = GetInternalData<Data>();
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Move, null, null);

        data.TriggersRemaining--;
        if (data.TriggersRemaining <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
