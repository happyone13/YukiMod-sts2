using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.HoverTips;

public static class YukiHoverTipFactory
{
    private const string ForeseeTitleKey = "YUKIMOD-FORESEE.title";
    private const string ForeseeDescriptionKey = "YUKIMOD-FORESEE.description";
    private const string InspirationTitleKey = "YUKIMOD-INSPIRATION.title";
    private const string InspirationDescriptionKey = "YUKIMOD-INSPIRATION.description";
    private const string NingJuTitleKey = "YUKIMOD-NING_JU.title";
    private const string NingJuDescriptionKey = "YUKIMOD-NING_JU.description";
    private const string BlackCloudTitleKey = "YUKIMOD-BLACK_CLOUD.title";
    private const string BlackCloudDescriptionKey = "YUKIMOD-BLACK_CLOUD.description";
    private const string CountsAsMoonshadowTitleKey = "YUKIMOD-COUNTS_AS_MOONSHADOW.title";
    private const string CountsAsMoonshadowDescriptionKey = "YUKIMOD-COUNTS_AS_MOONSHADOW.description";

    public static IHoverTip FromForesee()
    {
        return new HoverTip(
            new LocString("cards", ForeseeTitleKey),
            new LocString("cards", ForeseeDescriptionKey));
    }

    public static IHoverTip FromInspiration()
    {
        return new HoverTip(
            new LocString("cards", InspirationTitleKey),
            new LocString("cards", InspirationDescriptionKey));
    }

    public static IHoverTip FromNingJu()
    {
        return new HoverTip(
            new LocString("cards", NingJuTitleKey),
            new LocString("cards", NingJuDescriptionKey));
    }

    public static IHoverTip FromBlackCloud()
    {
        return new HoverTip(
            new LocString("cards", BlackCloudTitleKey),
            new LocString("cards", BlackCloudDescriptionKey));
    }

    public static IHoverTip FromCountsAsMoonshadow()
    {
        return new HoverTip(
            new LocString("cards", CountsAsMoonshadowTitleKey),
            new LocString("cards", CountsAsMoonshadowDescriptionKey));
    }

    public static IEnumerable<IHoverTip> FromIai()
    {
        return [HoverTipFactory.FromCard<JuHe>()];
    }

    public static IEnumerable<IHoverTip> FromSheathe()
    {
        return [HoverTipFactory.FromCard<NaDao>()];
    }
}
