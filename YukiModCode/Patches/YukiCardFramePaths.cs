using MegaCrit.Sts2.Core.Entities.Cards;

namespace YukiMod.YukiModCode.Patches;

internal static class YukiCardFramePaths
{
    private const string ChaosFrameBasePath = "res://YukiMod/images/cards/chaos_frame/";
    private const string ChaosEffectsBasePath = "res://YukiMod/images/cards/card_effects/";

    public static string GetAncientBorderTexturePathForTypeAndRarity(CardType type, CardRarity rarity)
    {
        return $"{ChaosFrameBasePath}card_frame_chaos_s.tres";
    }

    public static string GetAncientBannerTexturePathForType(CardType type)
    {
        return $"{ChaosFrameBasePath}ancient_banner.tres";
    }

    public static string GetAncientTextBgPathForType(CardType type)
    {
        return type switch
        {
            CardType.Skill => $"{ChaosFrameBasePath}ancient_card_text_bg_skill.tres",
            CardType.Power => $"{ChaosFrameBasePath}ancient_card_text_bg_power.tres",
            _ => $"{ChaosFrameBasePath}ancient_card_text_bg_attack.tres"
        };
    }

    public static string GetAncientHighlightTexturePath()
    {
        return $"{ChaosFrameBasePath}card_highlight_ancient.tres";
    }

    public static string GetPortraitBorderTexturePath()
    {
        return $"{ChaosEffectsBasePath}card_ego_narcissism.png";
    }
}
