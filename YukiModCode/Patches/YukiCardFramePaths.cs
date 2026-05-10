using MegaCrit.Sts2.Core.Entities.Cards;

namespace YukiMod.YukiModCode.Patches;

public static class YukiCardFramePaths
{
    public const string BasicFrameTexturePath = "res://YukiMod/images/cards/card_effects/card_ego_basic.png";
    public const string UncommonFrameTexturePath = "res://YukiMod/images/cards/card_effects/card_ego_narcissism.png";
    public const string RareFrameTexturePath = "res://YukiMod/images/cards/card_effects/card_ego_instinct.png";
    public const string TokenFrameTexturePath = "res://YukiMod/images/cards/card_effects/card_ego_creed.png";
    public const string AncientCustomFrameTexturePath = "res://YukiMod/images/cards/card_effects/card_ego_all.png";
    public const string AncientEgoBadgeTexturePath = "res://YukiMod/images/cards/card_effects/card_ego_all.png";
    public const string FrameMaterialPath = "res://YukiMod/materials/cards/frames/card_frame_chaos_mat.tres";
    public const string BannerMaterialPath = "res://YukiMod/materials/cards/banners/card_banner_chaos_mat.tres";
    public const string AncientBorderTexturePath = "res://YukiMod/images/cards/chaos_frame/ancient_card_border.tres";
    public const string AncientBannerTexturePath = "res://YukiMod/images/cards/chaos_frame/ancient_banner.tres";
    public const string AncientHighlightTexturePath = "res://YukiMod/images/cards/chaos_frame/card_highlight_ancient.tres";

    public static string GetCustomFrameTexturePath(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => BasicFrameTexturePath,
            CardRarity.Common => BasicFrameTexturePath,
            CardRarity.Uncommon => UncommonFrameTexturePath,
            CardRarity.Rare => RareFrameTexturePath,
            CardRarity.Ancient => AncientCustomFrameTexturePath,
            CardRarity.Token => TokenFrameTexturePath,
            _ => BasicFrameTexturePath
        };
    }

    public static string GetEgoBadgeTexturePath(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => BasicFrameTexturePath,
            CardRarity.Common => BasicFrameTexturePath,
            CardRarity.Uncommon => UncommonFrameTexturePath,
            CardRarity.Rare => RareFrameTexturePath,
            CardRarity.Ancient => AncientEgoBadgeTexturePath,
            CardRarity.Token => TokenFrameTexturePath,
            _ => BasicFrameTexturePath
        };
    }

    public static string GetAncientTextBgTexturePath(CardType type)
    {
        string suffix = type switch
        {
            CardType.Power => "power",
            CardType.Attack => "attack",
            _ => "skill"
        };

        return $"res://YukiMod/images/cards/chaos_frame/ancient_card_text_bg_{suffix}.tres";
    }
}
