using System;
using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Services;

public static class YukiCardReplayService
{
    public static Task AutoPlayDupe(PlayerChoiceContext choiceContext, CardPlay previousPlay)
    {
        var owner = previousPlay.Card.Owner
                    ?? throw new InvalidOperationException("Cannot duplicate a replay card without an owner.");
        var replayCard = previousPlay.Card.CreateDupe(owner);
        return CardCmd.AutoPlay(choiceContext, replayCard, GetReplayTarget(previousPlay, replayCard));
    }

    private static Creature? GetReplayTarget(CardPlay previousPlay, CardModel replayCard)
    {
        var target = previousPlay.Target;
        if (target == null || !target.IsAlive)
        {
            return null;
        }

        return replayCard.TargetType == TargetType.AnyEnemy
               && previousPlay.Card.CombatState?.HittableEnemies.Contains(target) != true
            ? null
            : target;
    }
}
