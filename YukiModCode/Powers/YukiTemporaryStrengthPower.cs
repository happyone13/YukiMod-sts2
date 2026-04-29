using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YukiMod.YukiModCode.Powers;

public abstract class YukiTemporaryStrengthPower : YukiModPower
{
    private bool _shouldIgnoreNextInstance;

    public abstract AbstractModel OriginModel { get; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard((CardModel)OriginModel), HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _shouldIgnoreNextInstance = true;
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), target, amount, applier, cardSource, silent: true);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (!(amount == Amount) && power == this)
        {
            if (_shouldIgnoreNextInstance)
            {
                _shouldIgnoreNextInstance = false;
            }
            else
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, amount, applier, cardSource, silent: true);
            }
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            Flash();
            await PowerCmd.Remove(this);
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -Amount, Owner, null);
        }
    }
}
