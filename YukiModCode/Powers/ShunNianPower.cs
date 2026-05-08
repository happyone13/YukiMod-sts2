using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class ShunNianPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, YukiCombatState combatState)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var candidates = player.Character.CardPool.AllCards
            .Where(YukiInspirationService.IsInspirationCard)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        Flash();
        var rng = player.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < Amount; i++)
        {
            var canonicalCard = rng.NextItem(candidates);
            if (canonicalCard == null)
            {
                return;
            }

            var card = combatState.CreateCard(canonicalCard, player);
            await YukiCardPileService.AddGeneratedCardsToCombat([card], PileType.Hand, player);
        }
    }
}
