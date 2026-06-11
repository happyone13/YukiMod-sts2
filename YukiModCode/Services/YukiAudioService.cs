using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using YukiMod.YukiModCode.Mechanics.Settings;
using YukiCharacterModel = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.YukiModCode.Services;

public static class YukiAudioService
{
    private const float VolumeScale = 0.3f;

    private static readonly string[] FmodPrefixes = ["event:/", "snapshot:/", "bus:/", "vca:/", "parameter:/"];

    private static readonly string[] AttackPool =
    [
        "res://YukiMod/sound/yuki_attack_01.mp3",
        "res://YukiMod/sound/yuki_attack_02.mp3",
        "res://YukiMod/sound/yuki_attack_03.mp3"
    ];

    private static readonly string[] CastPool =
    [
        "res://YukiMod/sound/yuki_cast_01.mp3",
        "res://YukiMod/sound/yuki_cast_02.mp3",
        "res://YukiMod/sound/yuki_cast_03.mp3",
        "res://YukiMod/sound/yuki_cast_04.mp3",
        "res://YukiMod/sound/yuki_cast_05.mp3",
        "res://YukiMod/sound/yuki_cast_06.mp3"
    ];

    private const string DiePath = "res://YukiMod/sound/yuki_die.mp3";
    private const string SelectPath = "res://YukiMod/sound/yuki_select.mp3";
    private const string CombatStartVoicePath = "res://YukiMod/ArtWorks/sound/chaos_yuki_v/vo_chaos_yuki_start_01.ogg";
    private const string VictoryVoicePath = "res://YukiMod/ArtWorks/sound/chaos_yuki_v/vo_chaos_yuki_victory_01.ogg";
    private const string RestSiteVoicePath = "res://YukiMod/ArtWorks/sound/chaos_yuki_v/vo_chaos_yuki_rest_01.ogg";

    private static readonly Dictionary<string, string> CustomCardClipMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bing_dian_zhi_ren"] = "res://YukiMod/sound/bing_dian_zhi_ren.mp3",
        ["mi_huo_yi_ji"] = "res://YukiMod/sound/mi_huo_yi_ji.mp3",
        ["po_bing_zhan"] = "res://YukiMod/sound/po_bing_zhan.mp3",
        ["tian_ji_zhan_ji"] = "res://YukiMod/sound/tian_ji_zhan_ji.mp3",
        ["tou_xi_zhan"] = "res://YukiMod/sound/tou_xi_zhan.mp3",
        ["ya_zhi_zhun_bei"] = "res://YukiMod/sound/ya_zhi_zhun_bei.mp3"
    };

    private static Node? _audioHost;
    private static long _playerCounter;
    private static int _suppressNextAttackSfxCount;
    private static int _suppressNextCastSfxCount;

    public static bool TryPlayFromSfxCmd(string sfx, float linearVolume)
    {
        if (string.IsNullOrWhiteSpace(sfx))
            return false;

        var key = sfx.Trim();
        var lower = key.ToLowerInvariant();

        foreach (var prefix in FmodPrefixes)
        {
            if (lower.StartsWith(prefix, StringComparison.Ordinal) && !lower.Contains("yuki"))
                return false;
        }

        if (!TryResolvePath(lower, out var path))
            return false;

        return TryPlay(path, linearVolume);
    }

    public static bool TryPlayDeath(Player? player, float linearVolume = 1f)
    {
        if (!IsYukiPlayer(player))
            return false;

        return TryPlay(DiePath, linearVolume);
    }

    public static bool TryPlayCombatStartVoice(Player? player, float linearVolume = 1f)
    {
        if (!IsYukiPlayer(player))
            return false;

        return TryPlay(CombatStartVoicePath, linearVolume);
    }

    public static bool TryPlayVictoryVoice(Player? player, float linearVolume = 1f)
    {
        if (!IsYukiPlayer(player))
            return false;

        return TryPlay(VictoryVoicePath, linearVolume);
    }

    public static bool TryPlayRestSiteVoice(float linearVolume = 1f)
    {
        return TryPlay(RestSiteVoicePath, linearVolume);
    }

    public static bool TryPlayCustomCardClip(string clipKey, Player? player = null, float linearVolume = 1f)
    {
        if (!IsYukiPlayer(player))
            return false;

        if (!CustomCardClipMap.TryGetValue(clipKey, out var path))
            return false;

        return TryPlay(path, linearVolume);
    }

    public static bool TryPlayCustomAttackCardClip(string clipKey, Player? player = null, float linearVolume = 1f)
    {
        if (!IsYukiPlayer(player) || !CustomCardClipMap.ContainsKey(clipKey))
            return false;

        SuppressNextDefaultAttackSfx(player);
        return TryPlayCustomCardClip(clipKey, player, linearVolume);
    }

    public static bool TryPlayCustomCastCardClip(string clipKey, Player? player = null, float linearVolume = 1f)
    {
        if (!IsYukiPlayer(player) || !CustomCardClipMap.ContainsKey(clipKey))
            return false;

        SuppressNextDefaultCastSfx(player);
        return TryPlayCustomCardClip(clipKey, player, linearVolume);
    }

    public static void SuppressNextDefaultAttackSfx(Player? player = null)
    {
        if (!IsYukiPlayer(player))
            return;

        _suppressNextAttackSfxCount++;
    }

    public static void SuppressNextDefaultCastSfx(Player? player = null)
    {
        if (!IsYukiPlayer(player))
            return;

        _suppressNextCastSfxCount++;
    }

    public static bool ShouldSuppressDefaultSfx(string sfx)
    {
        if (string.IsNullOrWhiteSpace(sfx))
            return false;

        var key = sfx.Trim().ToLowerInvariant();
        if (key.Contains("attack", StringComparison.Ordinal) && _suppressNextAttackSfxCount > 0)
        {
            _suppressNextAttackSfxCount--;
            return true;
        }

        if (key.Contains("cast", StringComparison.Ordinal) && _suppressNextCastSfxCount > 0)
        {
            _suppressNextCastSfxCount--;
            return true;
        }

        return false;
    }

    private static bool TryResolvePath(string key, out string path)
    {
        if (!key.Contains("yuki", StringComparison.Ordinal))
        {
            path = string.Empty;
            return false;
        }

        if (key.Contains("attack", StringComparison.Ordinal))
        {
            path = PickRandom(AttackPool);
            return true;
        }

        if (key.Contains("cast", StringComparison.Ordinal))
        {
            path = PickRandom(CastPool);
            return true;
        }

        if (key.Contains("die", StringComparison.Ordinal) || key.Contains("death", StringComparison.Ordinal))
        {
            path = DiePath;
            return true;
        }

        if (key.Contains("select", StringComparison.Ordinal) || key.Contains("pick", StringComparison.Ordinal))
        {
            path = SelectPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private static bool TryPlay(string resourcePath, float linearVolume)
    {
        var stream = ResourceLoader.Load<AudioStream>(resourcePath, cacheMode: ResourceLoader.CacheMode.Reuse);
        if (stream == null)
        {
            GD.PushWarning($"[YukiAudio] Failed to load stream: {resourcePath}");
            return false;
        }

        var host = EnsureHostNode();
        if (host == null)
            return false;

        var player = new AudioStreamPlayer
        {
            Name = $"YukiSfx_{++_playerCounter}",
            Stream = stream,
            VolumeDb = LinearToDb(linearVolume * VolumeScale * YukiModSharedSettings.VoiceVolume)
        };

        host.AddChild(player);
        player.Finished += () => player.QueueFree();
        player.Play();
        return true;
    }

    private static Node? EnsureHostNode()
    {
        if (_audioHost != null && GodotObject.IsInstanceValid(_audioHost) && _audioHost.IsInsideTree())
            return _audioHost;

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            return null;

        _audioHost = tree.Root.GetNodeOrNull<Node>("YukiAudioHost");
        if (_audioHost != null)
            return _audioHost;

        _audioHost = new Node
        {
            Name = "YukiAudioHost",
            ProcessMode = Node.ProcessModeEnum.Always
        };
        tree.Root.AddChild(_audioHost);
        return _audioHost;
    }

    private static string PickRandom(string[] pool)
    {
        var index = (int)GD.RandRange(0, pool.Length - 1);
        return pool[index];
    }

    private static float LinearToDb(float linearVolume)
    {
        if (linearVolume <= 0f)
            return -80f;

        return Mathf.LinearToDb(Mathf.Max(linearVolume, 0.0001f));
    }

    private static bool IsYukiPlayer(Player? player)
    {
        return player?.Character is YukiCharacterModel;
    }
}
