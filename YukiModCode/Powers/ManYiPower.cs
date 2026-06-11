using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Powers;

public class ManYiPower : YukiModPower
{
    private sealed class Data
    {
        public bool TriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        var player = Owner.Player;
        if (card.Owner.Creature != Owner || player?.PlayerCombatState?.Hand == null)
        {
            return;
        }

        var data = GetInternalData<Data>();
        if (data.TriggeredThisTurn)
        {
            return;
        }

        if (player.PlayerCombatState.Hand.Cards.Count < YukiMod.YukiModCode.Services.YukiCardPileService.MaxCardsInHand)
        {
            return;
        }

        data.TriggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(2m, player);
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            GetInternalData<Data>().TriggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }
}
