using System;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace YukiMod.YukiModCode.Mechanics.Settings;

public static class YukiModSharedSettings
{
	private const string SharedSettingsDirName = "chaosmod";
	private const string SharedSettingsFileName = "xcskin_settings.json";
	private const string SharedDomainKeyPrefix = "CHAOSMOD_XCSKIN_";
	private static readonly string SharedVoiceVolumeKey = SharedDomainKeyPrefix + "VOICE_VOLUME";
	private static readonly string SharedBattleReadyScaleKey = SharedDomainKeyPrefix + "BATTLE_READY_SCALE";
	private static readonly string SharedBattleReadyOffsetXKey = SharedDomainKeyPrefix + "BATTLE_READY_OFFSET_X";
	private static readonly string SharedBattleReadyOffsetYKey = SharedDomainKeyPrefix + "BATTLE_READY_OFFSET_Y";
	private static readonly string SharedBattleReadyOverlayEnabledKey = SharedDomainKeyPrefix + "PORTRAITS_ENABLED";
	private static readonly string SharedCombatEffectsEnabledKey = SharedDomainKeyPrefix + "ACTION_VFX_ENABLED";
	private static readonly string SharedDynamicCardPortraitsEnabledKey = SharedDomainKeyPrefix + "DYNAMIC_CARD_PORTRAITS_ENABLED";

	private static int _settingsLoaded;
	private static float _voiceVolume = 0.8f;
	private static float _battleReadyScale = 1f;
	private static float _battleReadyOffsetX;
	private static float _battleReadyOffsetY;
	private static bool _battleReadyOverlayEnabled = true;
	private static bool _combatEffectsEnabled = true;
	private static bool _dynamicCardPortraitsEnabled = true;

	public static event Action<bool>? CombatEffectsEnabledChanged;

	public static float VoiceVolume
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedFloat(SharedVoiceVolumeKey, _voiceVolume);
		}
	}

	public static float BattleReadyScale
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedFloat(SharedBattleReadyScaleKey, _battleReadyScale);
		}
	}

	public static float BattleReadyOffsetX
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedFloat(SharedBattleReadyOffsetXKey, _battleReadyOffsetX);
		}
	}

	public static float BattleReadyOffsetY
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedFloat(SharedBattleReadyOffsetYKey, _battleReadyOffsetY);
		}
	}

	public static bool BattleReadyOverlayEnabled
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedBool(SharedBattleReadyOverlayEnabledKey, _battleReadyOverlayEnabled);
		}
	}

	public static bool CombatEffectsEnabled
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedBool(SharedCombatEffectsEnabledKey, _combatEffectsEnabled);
		}
	}

	public static bool DynamicCardPortraitsEnabled
	{
		get
		{
			EnsureSettingsLoaded();
			return GetSharedBool(SharedDynamicCardPortraitsEnabledKey, _dynamicCardPortraitsEnabled);
		}
	}

	public static void SetVoiceVolume(float value, bool persist)
	{
		EnsureSettingsLoaded();
		_voiceVolume = Mathf.Clamp(value, 0f, 1f);
		SetSharedFloat(SharedVoiceVolumeKey, _voiceVolume);
		if (persist)
		{
			Save();
		}
	}

	public static void SetBattleReadyScale(float value, bool persist)
	{
		EnsureSettingsLoaded();
		_battleReadyScale = Mathf.Clamp(value, 0.5f, 2.0f);
		SetSharedFloat(SharedBattleReadyScaleKey, _battleReadyScale);
		if (persist)
		{
			Save();
		}
	}

	public static void SetBattleReadyOffsetX(float value, bool persist)
	{
		EnsureSettingsLoaded();
		_battleReadyOffsetX = Mathf.Clamp(value, -400f, 400f);
		SetSharedFloat(SharedBattleReadyOffsetXKey, _battleReadyOffsetX);
		if (persist)
		{
			Save();
		}
	}

	public static void SetBattleReadyOffsetY(float value, bool persist)
	{
		EnsureSettingsLoaded();
		_battleReadyOffsetY = Mathf.Clamp(value, -400f, 400f);
		SetSharedFloat(SharedBattleReadyOffsetYKey, _battleReadyOffsetY);
		if (persist)
		{
			Save();
		}
	}

	public static void SetBattleReadyOverlayEnabled(bool value, bool persist)
	{
		EnsureSettingsLoaded();
		_battleReadyOverlayEnabled = value;
		SetSharedBool(SharedBattleReadyOverlayEnabledKey, _battleReadyOverlayEnabled);
		if (persist)
		{
			Save();
		}
	}

	public static void SetCombatEffectsEnabled(bool value, bool persist)
	{
		EnsureSettingsLoaded();
		bool changed = _combatEffectsEnabled != value;
		_combatEffectsEnabled = value;
		SetSharedBool(SharedCombatEffectsEnabledKey, _combatEffectsEnabled);
		if (persist)
		{
			Save();
		}
		if (changed)
		{
			try
			{
				CombatEffectsEnabledChanged?.Invoke(value);
			}
			catch
			{
			}
		}
	}

	public static void SetDynamicCardPortraitsEnabled(bool value, bool persist)
	{
		EnsureSettingsLoaded();
		_dynamicCardPortraitsEnabled = value;
		SetSharedBool(SharedDynamicCardPortraitsEnabledKey, _dynamicCardPortraitsEnabled);
		if (persist)
		{
			Save();
		}
	}

	public static void EnsureSettingsLoaded()
	{
		if (System.Threading.Interlocked.Exchange(ref _settingsLoaded, 1) != 0)
		{
			return;
		}

		try
		{
			string path = GetSettingsPath();
			if (!File.Exists(path))
			{
				_voiceVolume = 0.8f;
				_battleReadyScale = 1f;
				_battleReadyOffsetX = 0f;
				_battleReadyOffsetY = 0f;
				_battleReadyOverlayEnabled = true;
				_combatEffectsEnabled = true;
				_dynamicCardPortraitsEnabled = true;
			}
			else
			{
				string json = File.ReadAllText(path);
				LoadSettingsFromJson(json);
			}

			SetSharedFloat(SharedVoiceVolumeKey, _voiceVolume);
			SetSharedFloat(SharedBattleReadyScaleKey, _battleReadyScale);
			SetSharedFloat(SharedBattleReadyOffsetXKey, _battleReadyOffsetX);
			SetSharedFloat(SharedBattleReadyOffsetYKey, _battleReadyOffsetY);
			SetSharedBool(SharedBattleReadyOverlayEnabledKey, _battleReadyOverlayEnabled);
			SetSharedBool(SharedCombatEffectsEnabledKey, _combatEffectsEnabled);
			SetSharedBool(SharedDynamicCardPortraitsEnabledKey, _dynamicCardPortraitsEnabled);
		}
		catch (Exception ex)
		{
			_voiceVolume = 0.8f;
			_battleReadyScale = 1f;
			_battleReadyOffsetX = 0f;
			_battleReadyOffsetY = 0f;
			_battleReadyOverlayEnabled = true;
			_combatEffectsEnabled = true;
			_dynamicCardPortraitsEnabled = true;
			SetSharedFloat(SharedVoiceVolumeKey, _voiceVolume);
			SetSharedFloat(SharedBattleReadyScaleKey, _battleReadyScale);
			SetSharedFloat(SharedBattleReadyOffsetXKey, _battleReadyOffsetX);
			SetSharedFloat(SharedBattleReadyOffsetYKey, _battleReadyOffsetY);
			SetSharedBool(SharedBattleReadyOverlayEnabledKey, _battleReadyOverlayEnabled);
			SetSharedBool(SharedCombatEffectsEnabledKey, _combatEffectsEnabled);
			SetSharedBool(SharedDynamicCardPortraitsEnabledKey, _dynamicCardPortraitsEnabled);
			Log.Warn($"[{YukiModInfo.ModId}] Shared settings load failed: {ex.Message}");
		}
	}

	private static void Save()
	{
		try
		{
			string path = GetSettingsPath();
			string? dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(dir))
			{
				Directory.CreateDirectory(dir);
			}

			XCskinVoiceSettings settings = new XCskinVoiceSettings
			{
				Volume = GetSharedFloat(SharedVoiceVolumeKey, _voiceVolume),
				BattleReadyScale = GetSharedFloat(SharedBattleReadyScaleKey, _battleReadyScale),
				BattleReadyOffsetX = GetSharedFloat(SharedBattleReadyOffsetXKey, _battleReadyOffsetX),
				BattleReadyOffsetY = GetSharedFloat(SharedBattleReadyOffsetYKey, _battleReadyOffsetY),
				PortraitsEnabled = GetSharedBool(SharedBattleReadyOverlayEnabledKey, _battleReadyOverlayEnabled),
				ActionVfxEnabled = GetSharedBool(SharedCombatEffectsEnabledKey, _combatEffectsEnabled),
				DynamicCardPortraitsEnabled = GetSharedBool(SharedDynamicCardPortraitsEnabledKey, _dynamicCardPortraitsEnabled)
			};
			string json = JsonSerializer.Serialize(settings);
			File.WriteAllText(path, json);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{YukiModInfo.ModId}] Shared settings save failed: {ex.Message}");
		}
	}

	private static float GetSharedFloat(string key, float fallback)
	{
		try
		{
			object? obj = AppDomain.CurrentDomain.GetData(key);
			if (obj is float f)
			{
				return f;
			}
			if (obj is double d)
			{
				return (float)d;
			}
			if (obj is string s && float.TryParse(s, out float parsed))
			{
				return parsed;
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static bool GetSharedBool(string key, bool fallback)
	{
		try
		{
			object? obj = AppDomain.CurrentDomain.GetData(key);
			if (obj is bool b)
			{
				return b;
			}
			if (obj is string s && bool.TryParse(s, out bool parsed))
			{
				return parsed;
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static void LoadSettingsFromJson(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;

		_voiceVolume = Mathf.Clamp(ReadFloat(root, "Volume", 0.8f), 0f, 1f);
		_battleReadyScale = Mathf.Clamp(ReadFloat(root, "BattleReadyScale", 1f), 0.5f, 2.0f);
		_battleReadyOffsetX = Mathf.Clamp(ReadFloat(root, "BattleReadyOffsetX", 0f), -400f, 400f);
		_battleReadyOffsetY = Mathf.Clamp(ReadFloat(root, "BattleReadyOffsetY", 0f), -400f, 400f);
		_battleReadyOverlayEnabled = ReadBool(root, true, "PortraitsEnabled", "BattleReadyOverlayEnabled");
		_combatEffectsEnabled = ReadBool(root, true, "ActionVfxEnabled", "CombatEffectsEnabled");
		_dynamicCardPortraitsEnabled = ReadBool(root, true, "DynamicCardPortraitsEnabled", "UseDynamicCardPortraits");
	}

	private static float ReadFloat(JsonElement root, string propertyName, float fallback)
	{
		if (!root.TryGetProperty(propertyName, out JsonElement value))
		{
			return fallback;
		}

		if (value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float number))
		{
			return number;
		}

		if (value.ValueKind == JsonValueKind.String && float.TryParse(value.GetString(), out float parsed))
		{
			return parsed;
		}

		return fallback;
	}

	private static bool ReadBool(JsonElement root, bool fallback, params string[] propertyNames)
	{
		for (int i = 0; i < propertyNames.Length; i++)
		{
			if (!root.TryGetProperty(propertyNames[i], out JsonElement value))
			{
				continue;
			}

			if (value.ValueKind == JsonValueKind.True)
			{
				return true;
			}

			if (value.ValueKind == JsonValueKind.False)
			{
				return false;
			}

			if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed))
			{
				return parsed;
			}
		}

		return fallback;
	}

	private static void SetSharedFloat(string key, float value)
	{
		try
		{
			AppDomain.CurrentDomain.SetData(key, value);
		}
		catch
		{
		}
	}

	private static void SetSharedBool(string key, bool value)
	{
		try
		{
			AppDomain.CurrentDomain.SetData(key, value);
		}
		catch
		{
		}
	}

	private static string GetSettingsPath()
	{
		string baseDir = "";
		try
		{
			baseDir = OS.GetUserDataDir();
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(baseDir))
		{
			try
			{
				baseDir = ProjectSettings.GlobalizePath("user://");
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(baseDir))
		{
			try
			{
				baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			}
			catch
			{
			}
		}
		if (!string.IsNullOrWhiteSpace(baseDir) && baseDir.Contains(".app/Contents/", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				string fallback = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
				if (!string.IsNullOrWhiteSpace(fallback))
				{
					baseDir = fallback;
				}
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(baseDir))
		{
			baseDir = AppContext.BaseDirectory;
		}
		return Path.Combine(baseDir, SharedSettingsDirName, SharedSettingsFileName);
	}

	private sealed class XCskinVoiceSettings
	{
		public float Volume { get; set; } = 0.8f;
		public float BattleReadyScale { get; set; } = 1f;
		public float BattleReadyOffsetX { get; set; }
		public float BattleReadyOffsetY { get; set; }
		public bool PortraitsEnabled { get; set; } = true;
		public bool ActionVfxEnabled { get; set; } = true;
		public bool DynamicCardPortraitsEnabled { get; set; } = true;
	}
}
