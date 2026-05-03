using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using YukiCharacterModel = YukiMod.YukiModCode.Character.YukiMod;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch]
public static class YukiAncientRelicPatch
{
    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
    [HarmonyPrefix]
    public static bool ArchaicToothSetupForPlayerPrefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        var starter = GetStarterSuppressionPrep(player);
        if (starter == null)
        {
            return true;
        }

        var transformed = player.RunState.CreateCard<ShenYaZhiZhunBei>(player);
        CopyStarterUpgradesAndEnchantments(starter, transformed);

        __instance.SetupForTests(starter.ToSerializable(), transformed.ToSerializable());
        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
    [HarmonyPrefix]
    public static bool ArchaicToothAfterObtainedPrefix(ArchaicTooth __instance, ref Task __result)
    {
        var owner = __instance.Owner;
        if (owner == null)
        {
            return true;
        }

        var starter = GetStarterSuppressionPrep(owner);
        if (starter == null)
        {
            return true;
        }

        __result = HandleArchaicToothTransform(owner, starter);
        return false;
    }

    [HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
    [HarmonyPrefix]
    public static bool DustyTomeSetupForPlayerPrefix(DustyTome __instance, Player player)
    {
        if (!IsYukiPlayer(player))
        {
            return true;
        }

        var candidates = GetCardPoolCards(player)
            .Where(IsDustyTomeCandidate)
            .ToList();
        if (candidates.Count == 0)
        {
            return true;
        }

        var selected = player.PlayerRng.Rewards.NextItem(candidates);
        if (selected == null)
        {
            return true;
        }

        __instance.AncientCard = selected.Id;
        return false;
    }

    [HarmonyPatch(typeof(DustyTome), nameof(DustyTome.AfterObtained))]
    [HarmonyPrefix]
    public static void DustyTomeAfterObtainedPrefix(DustyTome __instance)
    {
        var owner = __instance.Owner;
        if (__instance.AncientCard != null || owner?.Character == null)
        {
            return;
        }

        var setupMethod = AccessTools.Method(typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
        setupMethod?.Invoke(__instance, [owner]);
    }

    private static async Task HandleArchaicToothTransform(Player owner, CardModel starter)
    {
        var transformed = owner.RunState.CreateCard<ShenYaZhiZhunBei>(owner);
        CopyStarterUpgradesAndEnchantments(starter, transformed);
        await CardCmd.Transform(starter, transformed);
    }

    private static void CopyStarterUpgradesAndEnchantments(CardModel starter, CardModel transformed)
    {
        if (starter.IsUpgraded)
        {
            CardCmd.Upgrade(transformed);
        }

        if (starter.Enchantment != null)
        {
            var enchantment = (EnchantmentModel)starter.Enchantment.MutableClone();
            CardCmd.Enchant(enchantment, transformed, enchantment.Amount);
        }
    }

    private static CardModel? GetStarterSuppressionPrep(Player player)
    {
        return player.Deck.Cards.FirstOrDefault(card =>
            card is YaZhiZhunBei || card.Id.Entry == "YUKIMOD-YA_ZHI_ZHUN_BEI");
    }

    private static bool IsYukiPlayer(Player? player)
    {
        if (player == null)
        {
            return false;
        }

        if (player.Character is YukiCharacterModel)
        {
            return true;
        }

        return GetStarterSuppressionPrep(player) != null;
    }

    private static IEnumerable<CardModel> GetCardPoolCards(Player player)
    {
        var cardPool = player.Character.CardPool;
        var allCardsProperty = AccessTools.Property(cardPool.GetType(), "AllCards");
        if (allCardsProperty?.GetValue(cardPool) is IEnumerable<CardModel> allCards)
        {
            return allCards;
        }

        return cardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }

    private static bool IsDustyTomeCandidate(CardModel card)
    {
        if (card.Rarity != CardRarity.Ancient)
        {
            return false;
        }

        if (ArchaicTooth.TranscendenceCards.Contains(card))
        {
            return false;
        }

        return card is not ShenYaZhiZhunBei && card.Id.Entry != "YUKIMOD-SHEN_YA_ZHI_ZHUN_BEI";
    }
}
