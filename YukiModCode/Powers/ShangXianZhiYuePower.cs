using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Powers;

public class ShangXianZhiYuePower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || Owner.Player == null || cardSource == null || !YukiMoonshadowService.CountsAsMoonshadow(cardSource) || result.TotalDamage < Amount || CombatState == null)
        {
            return;
        }

        Flash();
        await JuHe.CreateInHand(Owner.Player, CombatState);
    }
}
