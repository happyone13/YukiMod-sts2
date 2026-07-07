using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using YukiMod.YukiModCode.Relics;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.SetupForPlayer))]
public static class TouchOfOrobasYukiPatch
{
    [HarmonyPrefix]
    public static bool SetupForPlayerPrefix(TouchOfOrobas __instance, Player player, ref bool __result)
    {
        var starterRelic = player.Relics.FirstOrDefault(relic =>
            relic is YukiStarterRelic || relic.Id.Entry == "YUKIMOD_YUKI_STARTER_RELIC");
        if (starterRelic == null)
        {
            return true;
        }

        __instance.SetupForTests(starterRelic.Id, ModelDb.Relic<YukiStarterRelicPlus>().Id);
        __result = true;
        return false;
    }
}
