using BaseLib.Config;
using YukiMod.YukiModCode.Mechanics.CardHoldOverlay;
using YukiMod.YukiModCode.Mechanics.Settings;

namespace YukiMod.YukiModCode.Config;

internal class YukiModConfig : SimpleModConfig
{
    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseDynamicCardPortraits
    {
        get => YukiModSharedSettings.DynamicCardPortraitsEnabled;
        set => YukiModSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true);
    }

    [ConfigSection("CardVisuals")]
    public static bool UseBattleReadyOverlay
    {
        get => YukiModSharedSettings.BattleReadyOverlayEnabled;
        set
        {
            YukiModSharedSettings.SetBattleReadyOverlayEnabled(value, persist: true);
            if (!value)
            {
                YukiBattleReadyOverlay.NotifyCombatEnded();
            }
        }
    }

    [ConfigSection("CardVisuals")]
    public static bool UseCombatEffects
    {
        get => YukiModSharedSettings.CombatEffectsEnabled;
        set => YukiModSharedSettings.SetCombatEffectsEnabled(value, persist: true);
    }
}
