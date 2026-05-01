namespace YukiMod.YukiModCode.Cards;

public interface IYukiCardVisualProfile
{
    bool UseCustomFrame { get; }
    bool UseDynamicPortrait { get; }
    string? CustomSpinePortraitScenePath { get; }
    SpinePortraitSlot CustomSpinePortraitSlot { get; }
}
