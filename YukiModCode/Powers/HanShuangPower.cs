using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class HanShuangPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool IsInstanced => true;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        var candidates = YukiInspirationService.GetInspirableCards(player, PileType.Hand)
            .Where(card => !YukiInspirationService.IsInspired(card))
            .ToList();
        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rng = player.RunState.Rng.CombatCardSelection;
        var triggered = 0;
        for (var i = 0; i < Amount && candidates.Count > 0; i++)
        {
            var selectedCard = rng.NextItem(candidates);
            if (selectedCard == null)
            {
                break;
            }

            if (YukiInspirationService.ActivateInspiration(selectedCard))
            {
                triggered++;
            }

            candidates.Remove(selectedCard);
        }

        if (triggered > 0)
        {
            Flash();
        }

        return Task.CompletedTask;
    }
}
