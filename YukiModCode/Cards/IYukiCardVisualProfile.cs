namespace YukiMod.YukiModCode.Cards;

public interface IYukiCardVisualProfile
{
    bool UseDynamicPortrait { get; }
    string? CustomSpinePortraitScenePath { get; }
    SpinePortraitSlot CustomSpinePortraitSlot { get; }
}
