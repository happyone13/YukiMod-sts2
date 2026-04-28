using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Powers;

public class NuTaoZhanPower : YukiModPower
{
    private sealed class Data
    {
        public int AttacksPlayedThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => Math.Max(0, Amount - GetInternalData<Data>().AttacksPlayedThisTurn);

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().AttacksPlayedThisTurn =
            CombatManager.Instance.History.CardPlaysStarted.Count(
                entry => entry.CardPlay.Card.Type == CardType.Attack
                    && entry.CardPlay.Card.Owner.Creature == Owner
                    && entry.HappenedThisTurn(CombatState));
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        var data = GetInternalData<Data>();
        data.AttacksPlayedThisTurn++;
        InvokeDisplayAmountChanged();

        if (data.AttacksPlayedThisTurn != Amount)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            GetInternalData<Data>().AttacksPlayedThisTurn = 0;
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }
}
