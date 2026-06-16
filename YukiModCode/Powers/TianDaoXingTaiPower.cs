using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class TianDaoXingTaiPower : YukiModPower
{
    private sealed class Data
    {
        public CardPlay? FirstAttackThisTurn;
        public CardPlay? LastSkillThisTurn;
        public CardPlay? LastSkillPreviousTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        data.FirstAttackThisTurn = CombatManager.Instance.History.CardPlaysFinished
            .FirstOrDefault(entry =>
                entry.Actor == Owner
                && entry.CardPlay.IsFirstInSeries
                && entry.CardPlay.Card.Type == CardType.Attack
                && entry.HappenedThisTurn(CombatState))
            ?.CardPlay;
        data.LastSkillThisTurn = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry =>
                entry.Actor == Owner
                && entry.CardPlay.IsFirstInSeries
                && entry.CardPlay.Card.Type == CardType.Skill
                && entry.HappenedThisTurn(CombatState))
            ?.CardPlay;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !cardPlay.IsFirstInSeries)
        {
            return Task.CompletedTask;
        }

        var data = GetInternalData<Data>();
        if (cardPlay.Card.Type == CardType.Attack)
        {
            data.FirstAttackThisTurn ??= cardPlay;
        }
        else if (cardPlay.Card.Type == CardType.Skill)
        {
            data.LastSkillThisTurn = cardPlay;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var previousSkill = GetInternalData<Data>().LastSkillPreviousTurn;
        if (previousSkill == null)
        {
            return;
        }

        Flash();
        for (var i = 0; i < Amount; i++)
        {
            await YukiCardReplayService.AutoPlayClone(choiceContext, previousSkill);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }

        var data = GetInternalData<Data>();
        var firstAttackThisTurn = data.FirstAttackThisTurn;
        data.LastSkillPreviousTurn = data.LastSkillThisTurn;
        data.FirstAttackThisTurn = null;
        data.LastSkillThisTurn = null;

        if (firstAttackThisTurn == null)
        {
            return;
        }

        Flash();
        for (var i = 0; i < Amount; i++)
        {
            await YukiCardReplayService.AutoPlayClone(choiceContext, firstAttackThisTurn);
        }
    }
}
