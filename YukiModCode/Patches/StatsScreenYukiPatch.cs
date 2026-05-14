using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
public static class StatsScreenYukiPatch
{
    private const string YukiStatsNodeName = "YukiStats";

    private static readonly AccessTools.FieldRef<NGeneralStatsGrid, Control?> CharacterStatContainerRef =
        AccessTools.FieldRefAccess<NGeneralStatsGrid, Control?>("_characterStatContainer");

    [HarmonyPostfix]
    public static void LoadStatsPostfix(NGeneralStatsGrid __instance)
    {
        var characterStatContainer = CharacterStatContainerRef(__instance);
        if (characterStatContainer == null)
        {
            return;
        }

        if (characterStatContainer.HasNode(YukiStatsNodeName))
        {
            return;
        }

        var yukiStats = GetYukiStats();
        if (yukiStats == null)
        {
            return;
        }

        var statsNode = NCharacterStats.Create(yukiStats);
        statsNode.Name = YukiStatsNodeName;
        characterStatContainer.AddChild(statsNode);
    }

    private static CharacterStats? GetYukiStats()
    {
        var yukiId = ModelDb.Character<YukiCharacter>().Id;
        return SaveManager.Instance.Progress.GetStatsForCharacter(yukiId);
    }
}
