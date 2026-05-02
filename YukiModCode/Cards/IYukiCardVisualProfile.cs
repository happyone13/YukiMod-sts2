namespace YukiMod.YukiModCode.Cards;

public enum YukiSpinePortraitSlot
{
    Normal,
    Ancient
}

public interface IYukiCardVisualProfile
{
    string? CustomSpinePortraitScenePath { get; }

    YukiSpinePortraitSlot CustomSpinePortraitSlot { get; }
}
