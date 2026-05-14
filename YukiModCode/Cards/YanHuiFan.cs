using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class YanHuiFan() : YukiModCard(1, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var previousAttack = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry =>
                entry.CardPlay.Card.Owner == Owner &&
                entry.CardPlay.Card.Type == CardType.Attack &&
                entry.CardPlay.Card.Id != Id);
        if (previousAttack == null)
        {
            return;
        }

        for (var i = 0; i < 2; i++)
        {
            var replayCard = previousAttack.CardPlay.Card.CreateClone();
            var target = GetReplayTarget(previousAttack.CardPlay, replayCard);
            await CardCmd.AutoPlay(choiceContext, replayCard, target);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    private Creature? GetReplayTarget(CardPlay previousPlay, CardModel replayCard)
    {
        var target = previousPlay.Target;
        if (target == null)
        {
            return null;
        }

        if (!target.IsAlive)
        {
            return null;
        }

        return replayCard.TargetType == TargetType.AnyEnemy && CombatState?.HittableEnemies.Contains(target) != true
            ? null
            : target;
    }
}
