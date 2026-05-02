using BaseLib.Config;

namespace YukiMod.YukiModCode.Config;

internal class YukiModConfig : SimpleModConfig
{
    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseDynamicCardPortraits { get; set; } = true;
}
