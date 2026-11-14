using Godot;
using MegaCrit.Sts2.Core.Logging;
using YukiMod.YukiModCode.Mechanics.Settings;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

public static class ChaosOneShotVfx
{
    private static readonly HashSet<string> Warned = new(StringComparer.Ordinal);
    private static readonly object SceneCacheLock = new();
    private static readonly Dictionary<string, PackedScene?> SceneCache = new(StringComparer.Ordinal);

    public static ChaosVfxPrewarmReport Prewarm(IEnumerable<string> scenePaths)
    {
        if (!YukiModSharedSettings.CombatEffectsEnabled || scenePaths == null)
            return ChaosVfxPrewarmReport.Empty;
        string[] paths = scenePaths.Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal).ToArray();
        int loaded = paths.Count(path => GetOrLoadScene(path) != null);
        return new ChaosVfxPrewarmReport(paths.Length, loaded);
    }

    private static PackedScene? GetOrLoadScene(string scenePath)
    {
        lock (SceneCacheLock)
        {
            if (SceneCache.TryGetValue(scenePath, out var cached))
                return cached;
        }
        try
        {
            var scene = ResourceLoader.Load<PackedScene>(scenePath, "", ResourceLoader.CacheMode.Reuse);
            if (scene != null)
            {
                lock (SceneCacheLock)
                    SceneCache[scenePath] = scene;
                return scene;
            }
            WarnOnce($"miss:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx missing: {scenePath}");
        }
        catch (Exception ex)
        {
            WarnOnce($"load:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx load failed: {scenePath}: {ex.Message}");
        }
        return null;
    }

    public static void PlaySpineOneShot(string scenePath, string anim, Node parent, Vector2 globalPos,
        int? zIndex = null, float? uniformScale = null) =>
        PlaySpineOneShot(scenePath, anim, parent, globalPos, 0f, zIndex, uniformScale);

    public static void PlaySpineOneShot(string scenePath, string anim, Node parent, Vector2 globalPos,
        float rotationDelta, int? zIndex = null, float? uniformScale = null)
    {
        if (!YukiModSharedSettings.CombatEffectsEnabled ||
            parent == null || !GodotObject.IsInstanceValid(parent) || parent.IsQueuedForDeletion() ||
            string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(anim))
            return;
        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
            return;

        OneShotVfxPool.Lease? lease = null;
        int generation = 0;
        try
        {
            lease = OneShotVfxPool.Rent(parent, scenePath, scene);
            generation = lease.Generation;
            lease.Root.GlobalPosition = globalPos;
            lease.Root.Rotation += rotationDelta;
            if (zIndex.HasValue)
                lease.Root.ZIndex = zIndex.Value;
            if (uniformScale.HasValue)
                lease.Root.Scale *= uniformScale.Value;
            var sprites = lease.Spines.Where(pair => pair.Sprite.HasAnimation(anim)).ToArray();
            if (sprites.Length == 0)
            {
                WarnOnce($"anim:{scenePath}:{anim}", $"[{YukiModInfo.ModId}] OneShotVfx missing anim: {anim} in {scenePath}");
                lease.Return(generation);
                return;
            }

            int remaining = sprites.Length;
            foreach (var pair in sprites)
            {
                bool completed = false;
                void Complete()
                {
                    if (completed || !lease.IsCurrent(generation))
                        return;
                    completed = true;
                    if (--remaining == 0)
                        lease.Return(generation);
                }
                lease.Connect(pair.Sprite,
                    Callable.From<GodotObject, GodotObject, GodotObject>((_, _, _) => Complete()));
                try
                {
                    pair.Sprite.GetAnimationState().SetAnimation(anim, loop: false);
                }
                catch
                {
                    Complete();
                }
            }
        }
        catch (Exception ex)
        {
            lease?.Return(generation);
            WarnOnce($"play:{scenePath}", $"[{YukiModInfo.ModId}] OneShotVfx play failed: {scenePath}: {ex.Message}");
        }
    }

    internal static OneShotVfxPool.Lease? WarmInstance(string scenePath, Node parent, float warmAlpha)
    {
        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
            return null;
        string[] candidates = ["animation", "eff_b", "eff_f"];
        var lease = OneShotVfxPool.Rent(parent, scenePath, scene);
        int generation = lease.Generation;
        try
        {
            lease.Root.Modulate = new Color(1f, 1f, 1f, warmAlpha);
            int started = 0;
            foreach (var pair in lease.Spines)
            {
                string? animation = candidates.FirstOrDefault(name => pair.Sprite.HasAnimation(name));
                if (animation != null)
                {
                    pair.Sprite.GetAnimationState().SetAnimation(animation, loop: false);
                    started++;
                }
            }
            if (started == 0)
            {
                lease.Return(generation);
                return null;
            }
            return lease;
        }
        catch
        {
            lease.Return(generation);
            throw;
        }
    }

    private static void WarnOnce(string key, string message)
    {
        lock (Warned)
        {
            if (!Warned.Add(key))
                return;
        }
        Log.Warn(message);
    }
}
