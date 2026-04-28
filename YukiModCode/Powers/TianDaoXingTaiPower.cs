using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Powers;

public class TianDaoXingTaiPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Attack)
        {
            return playCount;
        }

        var attacksPlayedThisTurn = CombatManager.Instance.History.CardPlaysStarted.Count(
            entry => entry.Actor == Owner
                && entry.CardPlay.IsFirstInSeries
                && entry.CardPlay.Card.Type == CardType.Attack
                && entry.HappenedThisTurn(CombatState));

        if (attacksPlayedThisTurn >= 1)
        {
            return playCount;
        }

        return playCount + Amount;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        Flash();
        return Task.CompletedTask;
    }
}
