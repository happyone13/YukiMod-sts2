using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YukiMod.YukiModCode.Powers;

public class TianJiZhanJiPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool IsInstanced => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var handCards = PileType.Hand.GetPile(player).Cards.ToArray();
        if (handCards.Length == 0)
        {
            return;
        }

        Flash();
        await CardPileCmd.Add(handCards, PileType.Draw, CardPilePosition.Top, this);
        await CardPileCmd.Draw(choiceContext, handCards.Length + 2, player);
    }
}
