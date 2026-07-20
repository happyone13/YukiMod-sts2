using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class BingDianZhiRenPower : YukiModPower, IInspiredTriggeredListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public Task OnInspiredTriggered(PlayerChoiceContext choiceContext, Player player, CardModel sourceCard)
    {
        if (player != Owner.Player || CombatState == null)
        {
            return Task.CompletedTask;
        }

        var targets = CombatState.HittableEnemies.ToList();
        if (targets.Count == 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        return DamageAll(choiceContext, targets, sourceCard);
    }

    private async Task DamageAll(
        PlayerChoiceContext choiceContext,
        IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> targets,
        CardModel sourceCard)
    {
        foreach (var target in targets)
            await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Move | ValueProp.Unpowered, sourceCard, null);
    }
}
