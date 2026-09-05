using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YukiMod.YukiModCode.Mechanics.Animation;
using YukiMod.YukiModCode.Mechanics.Settings;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class YukiBattleVfxPrewarmer
{
    private const float WarmAlpha = 0.001f;
    private static readonly string[] CandidateAnimations = ["animation", "eff_b", "eff_f"];
    private static int _generation;

    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance)
    {
        if (!YukiModSharedSettings.CombatEffectsEnabled ||
            __instance == null ||
            !GodotObject.IsInstanceValid(__instance))
        {
            return;
        }

        int generation = Interlocked.Increment(ref _generation);
        string[] scenePaths = YukiMeleeTeleportAttackPatch.GetPreloadScenePaths()
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        RunAsync(__instance, generation, scenePaths);
    }

    private static async void RunAsync(NCombatRoom room, int generation, IReadOnlyList<string> scenePaths)
    {
        int warmed = 0;
        int failed = 0;
        try
        {
            await NextFrame(room);
            await NextFrame(room);

            foreach (string scenePath in scenePaths)
            {
                if (!IsCurrent(room, generation))
                    return;

                if (!ChaosSpineVfxInstance.TryCreate(scenePath, out ChaosSpineVfxInstance? instance) || instance == null)
                {
                    failed++;
                    await NextFrame(room);
                    continue;
                }

                room.CombatVfxContainer.AddChild(instance.Node);
                instance.Node.GlobalPosition = Vector2.Zero;
                instance.Node.Modulate = new Color(1f, 1f, 1f, WarmAlpha);

                bool playing = CandidateAnimations.Any(animation => instance.TryPlay(animation, loop: false));
                if (playing)
                    warmed++;
                else
                    failed++;

                await NextFrame(room);
                await NextFrame(room);
                instance.QueueFree();
                await NextFrame(room);
            }

            if (IsCurrent(room, generation))
            {
                MainFile.Logger.Info(
                    $"[{YukiModInfo.ModId}] Battle deep VFX prewarm complete. warmed={warmed}/{scenePaths.Count}, failed={failed}.");
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
