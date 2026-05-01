using BaseLib.Config;

namespace YukiMod.YukiModCode.Config;

internal class YukiModConfig : SimpleModConfig
{
    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseYukiCustomCardFrame { get; set; } = true;

    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseYukiDynamicPortraits { get; set; } = true;
}
