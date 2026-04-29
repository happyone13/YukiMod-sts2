using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Services;

public static class YukiInspirationService
{
    public static void NotifyInspiredTriggered(Player owner, CardModel sourceCard)
    {
        foreach (var power in owner.Creature.Powers)
        {
            if (power is LanYuePower lanYuePower)
            {
                _ = lanYuePower.OnInspiredTriggered(sourceCard);
            }
        }
    }
}
