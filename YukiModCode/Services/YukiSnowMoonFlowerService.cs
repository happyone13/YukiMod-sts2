using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Services;

public static class YukiSnowMoonFlowerService
{
    public const decimal SharedMoonshadowDamageBonus = 2m;

    public static bool HasXue(Player? player) => GetXuePower(player) != null;
    public static bool HasYue(Player? player) => GetYuePower(player) != null;
    public static bool HasHua(Player? player) => GetHuaPower(player) != null;

    public static bool ShouldGrantInspiration(CardModel card)
    {
        return card.Owner != null
               && HasXue(card.Owner)
               && card is Yue or Hua;
    }

    public static bool ShouldGrantMoonshadowDamage(CardModel card)
    {
        return card.Owner != null
               && HasYue(card.Owner)
               && card is Xue or Hua;
    }

    public static bool ShouldGrantBlackCloud(CardModel card)
    {
        return card.Owner != null
               && HasHua(card.Owner)
               && card is Xue or Yue;
    }

    public static async Task ApplyXue(PlayerChoiceContext choiceContext, Player owner, ICombatState? combatState, CardModel source)
    {
        if (GetXuePower(owner) == null)
        {
            await PowerCmd.Apply<XuePower>(choiceContext, owner.Creature, 1m, owner.Creature, source);
        }

        await TryCreateJuHe(choiceContext, owner, combatState);
    }

    public static async Task ApplyYue(PlayerChoiceContext choiceContext, Player owner, ICombatState? combatState, CardModel source)
    {
        if (GetYuePower(owner) == null)
        {
            await PowerCmd.Apply<YuePower>(choiceContext, owner.Creature, 1m, owner.Creature, source);
        }

        await TryCreateJuHe(choiceContext, owner, combatState);
    }

    public static async Task ApplyHua(PlayerChoiceContext choiceContext, Player owner, ICombatState? combatState, CardModel source)
    {
        if (GetHuaPower(owner) == null)
        {
            await PowerCmd.Apply<HuaPower>(choiceContext, owner.Creature, 1m, owner.Creature, source);
        }

        await TryCreateJuHe(choiceContext, owner, combatState);
    }

    private static XuePower? GetXuePower(Player? player) =>
        player?.Creature.Powers.OfType<XuePower>().FirstOrDefault();

    private static YuePower? GetYuePower(Player? player) =>
        player?.Creature.Powers.OfType<YuePower>().FirstOrDefault();

    private static HuaPower? GetHuaPower(Player? player) =>
        player?.Creature.Powers.OfType<HuaPower>().FirstOrDefault();

    private static async Task TryCreateJuHe(PlayerChoiceContext choiceContext, Player owner, ICombatState? combatState)
    {
        var xuePower = GetXuePower(owner);
        var yuePower = GetYuePower(owner);
        var huaPower = GetHuaPower(owner);

        if (xuePower == null || yuePower == null || huaPower == null || combatState == null)
        {
            return;
        }

        await PowerCmd.Remove(xuePower);
        await PowerCmd.Remove(yuePower);
        await PowerCmd.Remove(huaPower);
        await JuHe.CreateInHand(owner, combatState);
    }
}
