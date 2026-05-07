using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Services;

public static class YukiPowerService
{
    public static async Task<T> Apply<T>(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel, new()
    {
        var power = await PowerCmd.Apply<T>(target, amount, applier!, cardSource!, silent);
        return power!;
    }

    public static Task<IReadOnlyList<T>> Apply<T>(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel, new()
    {
        return PowerCmd.Apply<T>(targets, amount, applier!, cardSource!, silent);
    }

    public static Task Apply(
        PowerModel power,
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply(power, target, amount, applier!, cardSource!, silent);
    }

    public static Task<int> ModifyAmount(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal offset,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.ModifyAmount(power, offset, applier!, cardSource!, silent);
    }
}
