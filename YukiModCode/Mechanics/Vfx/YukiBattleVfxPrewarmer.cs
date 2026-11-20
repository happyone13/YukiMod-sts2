using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Mechanics.Settings;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class YukiBattleVfxPrewarmer
{
    private const float WarmAlpha = 0.001f;
    private static int _generation;

    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance)
    {
        int generation = Interlocked.Increment(ref _generation);
        if (!YukiModSharedSettings.CombatEffectsEnabled ||
            __instance == null ||
            !GodotObject.IsInstanceValid(__instance))
        {
            return;
        }

        RunAsync(__instance, generation);
    }

    private static async void RunAsync(NCombatRoom room, int generation)
    {
        int warmed = 0;
        int failed = 0;
        try
        {
            await NextFrame(room);
            if (!IsCurrent(room, generation))
                return;
            await NextFrame(room);
            if (!IsCurrent(room, generation) ||
                !room.CreatureNodes.Any(node => node.Entity?.Player?.Character is YukiCharacter))
                return;

            YukiUxPresentation.PrefetchOpening(room);
            string[] scenePaths = YukiMeleeTeleportAttackPatch.GetPreloadScenePaths()
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (string scenePath in scenePaths)
            {
                if (!IsCurrent(room, generation))
                    return;

                bool back = scenePath.EndsWith("_b.tscn", StringComparison.Ordinal) ||
                            scenePath.EndsWith("/step_target_move.tscn", StringComparison.Ordinal);
                Node parent = back ? room.BackCombatVfxContainer : room.CombatVfxContainer;
                var instance = ChaosOneShotVfx.WarmInstance(scenePath, parent, WarmAlpha);
                if (instance == null)
                {
                    failed++;
                    await NextFrame(room);
                    continue;
                }

                int instanceGeneration = instance.Generation;
                try
                {
                    if (instance.Spines.Count > 0)
                        warmed++;
                    else
                        failed++;

                    await NextFrame(room);
                    if (!IsCurrent(room, generation))
                        return;
                    await NextFrame(room);
                    if (!IsCurrent(room, generation))
                        return;
                }
                finally
                {
                    instance.Return(instanceGeneration);
                }
                await NextFrame(room);
            }

            if (IsCurrent(room, generation))
            {
                MainFile.Logger.Info(
                    $"[{YukiModInfo.ModId}] Battle deep VFX prewarm complete. warmed={warmed}/{scenePaths.Length}, failed={failed}.");
            }
        }
        catch (Exception ex)
        {
            if (IsCurrent(room, generation))
                MainFile.Logger.Info($"[{YukiModInfo.ModId}] Battle deep VFX prewarm stopped. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool IsCurrent(NCombatRoom room, int generation)
    {
        return generation == Volatile.Read(ref _generation) &&
               YukiModSharedSettings.CombatEffectsEnabled &&
               GodotObject.IsInstanceValid(room) &&
               ReferenceEquals(NCombatRoom.Instance, room) &&
               room.CombatVfxContainer != null &&
               GodotObject.IsInstanceValid(room.CombatVfxContainer);
    }

    private static async Task NextFrame(Node node)
    {
        SceneTree? tree = node.GetTree();
        if (tree != null)
            await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
