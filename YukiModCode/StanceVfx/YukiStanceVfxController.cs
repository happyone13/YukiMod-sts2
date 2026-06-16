using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YukiMod.YukiModCode.Mechanics.Settings;

namespace YukiMod.YukiModCode.StanceVfx;

public sealed class YukiStanceVfxController
{
    public const string BlackCloudAuraScenePath = "res://YukiMod/scenes/vfx/black_cloud_aura.tscn";

    private const string ContainerName = "YukiStanceVfxContainer";

    private Node2D? _currentAura;
    private string? _currentAuraScenePath;

    public YukiStanceVfxController()
    {
        YukiModSharedSettings.CombatEffectsEnabledChanged += OnCombatEffectsEnabledChanged;
    }

    public async Task SetAura(Creature owner, string? auraScenePath)
    {
        if (!YukiModSharedSettings.CombatEffectsEnabled)
        {
            await ClearAura();
            return;
        }

        if (string.IsNullOrWhiteSpace(auraScenePath))
        {
            await ClearAura();
            return;
        }

        var visuals = NCombatRoom.Instance?.GetCreatureNode(owner)?.Visuals;
        if (visuals == null)
            return;

        var container = visuals.GetNodeOrNull<Node2D>(ContainerName);
        if (container == null)
        {
            container = new Node2D
            {
                Name = ContainerName,
                Position = Vector2.Zero
            };
            visuals.AddChild(container);
        }

        if (_currentAura != null &&
            GodotObject.IsInstanceValid(_currentAura) &&
            _currentAura.GetParent() == container &&
            _currentAuraScenePath == auraScenePath)
        {
            return;
        }

        await ClearAura();

        try
        {
            var aura = CreateAuraNode(auraScenePath);
            if (aura == null)
                return;

            aura.Position = Vector2.Zero;
            aura.Scale = Vector2.One;
            container.AddChild(aura);
            _currentAura = aura;
            _currentAuraScenePath = auraScenePath;
        }
        catch (System.Exception ex)
        {
            GD.PushWarning($"[YukiStanceVfx] Failed to create aura {auraScenePath}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public Task ClearAura()
    {
        if (_currentAura == null || !GodotObject.IsInstanceValid(_currentAura))
        {
            _currentAura = null;
            _currentAuraScenePath = null;
            return Task.CompletedTask;
        }

        var aura = _currentAura;
        _currentAura = null;
        _currentAuraScenePath = null;

        foreach (var child in aura.GetChildren())
        {
            switch (child)
            {
                case YukiBlackCloudStreakSpawner streaks:
                    streaks.StopSpawning();
                    break;
                case YukiAuraBlobEmitter blob:
                    foreach (var cpu in blob.GetChildren().OfType<CpuParticles2D>())
                        cpu.Emitting = false;

                    var tree = blob.GetTree();
                    if (tree == null)
                    {
                        blob.QueueFree();
                    }
                    else
                    {
                        var timer = tree.CreateTimer(2.5f);
                        timer.Timeout += () =>
                        {
                            if (GodotObject.IsInstanceValid(blob))
                                blob.QueueFree();
                        };
                    }

                    break;
                case Node node when node.HasMethod("StopSpawning"):
                    node.Call("StopSpawning");
                    break;
            }
        }

        var auraTree = aura.GetTree();
        if (auraTree == null)
        {
            aura.QueueFree();
        }
        else
        {
            var cleanupTimer = auraTree.CreateTimer(2.6f);
            cleanupTimer.Timeout += () =>
            {
                if (GodotObject.IsInstanceValid(aura))
                    aura.QueueFree();
            };
        }

        return Task.CompletedTask;
    }

    private static Node2D? CreateAuraNode(string auraScenePath)
    {
        if (auraScenePath == BlackCloudAuraScenePath)
        {
            var auraRoot = new Node2D
            {
                Name = "BlackCloudAura"
            };

            var streaks = new YukiBlackCloudStreakSpawner
            {
                Name = "BlackCloudStreaks",
                Position = new Vector2(0, -100)
            };

            var blobs = new YukiAuraBlobEmitter
            {
                Name = "AuraBlobs",
                Position = new Vector2(0, -188),
                BlobColor = new Color(0.18f, 0.08f, 0.30f, 1f)
            };

            auraRoot.AddChild(streaks);
            auraRoot.AddChild(blobs);
            return auraRoot;
        }

        GD.PushWarning($"[YukiStanceVfx] Unsupported aura path requested: {auraScenePath}");
        return null;
    }

    private void OnCombatEffectsEnabledChanged(bool enabled)
    {
        if (!enabled)
        {
            _ = ClearAura();
        }
    }
}
