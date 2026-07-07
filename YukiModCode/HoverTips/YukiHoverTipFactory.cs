using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using YukiMod.YukiModCode.Cards;
namespace YukiMod.YukiModCode.HoverTips;

public static class YukiHoverTipFactory
{
    private const string ForeseeTitleKey = "YUKIMOD_FORESEE.title";
    private const string ForeseeDescriptionKey = "YUKIMOD_FORESEE.description";
    private const string InspirationTitleKey = "YUKIMOD_INSPIRATION.title";
    private const string InspirationDescriptionKey = "YUKIMOD_INSPIRATION.description";
    private const string NingJuTitleKey = "YUKIMOD_NING_JU.title";
    private const string NingJuDescriptionKey = "YUKIMOD_NING_JU.description";
    private const string BlackCloudTitleKey = "YUKIMOD_BLACK_CLOUD.title";
    private const string BlackCloudDescriptionKey = "YUKIMOD_BLACK_CLOUD.description";
    private const string NoMingTitleKey = "YUKIMOD_NO_MING.title";
    private const string NoMingDescriptionKey = "YUKIMOD_NO_MING.description";
    private const string CountsAsMoonshadowTitleKey = "YUKIMOD_COUNTS_AS_MOONSHADOW.title";
    private const string CountsAsMoonshadowDescriptionKey = "YUKIMOD_COUNTS_AS_MOONSHADOW.description";
    private const string NextAttackPlayCountTitleKey = "YUKIMOD_NEXT_ATTACK_PLAY_COUNT.title";
    private const string NextAttackPlayCountDescriptionKey = "YUKIMOD_NEXT_ATTACK_PLAY_COUNT.description";
    private const string JuHeTitleKey = "YUKIMOD_JU_HE_KEYWORD.title";
    private const string JuHeDescriptionKey = "YUKIMOD_JU_HE_KEYWORD.description";

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

    public static IHoverTip FromNoMing()
    {
        return new HoverTip(
            new LocString("cards", NoMingTitleKey),
            new LocString("cards", NoMingDescriptionKey));
    }

    public static IHoverTip FromCountsAsMoonshadow()
    {
        return new HoverTip(
            new LocString("cards", CountsAsMoonshadowTitleKey),
            new LocString("cards", CountsAsMoonshadowDescriptionKey));
    }

    public static IHoverTip FromNextAttackPlayCount()
    {
        return new HoverTip(
            new LocString("cards", NextAttackPlayCountTitleKey),
            new LocString("cards", NextAttackPlayCountDescriptionKey));
    }

    public static IEnumerable<IHoverTip> FromIai()
    {
        return [HoverTipFactory.FromCard<JuHe>(), FromJuHeKeyword()];
    }

    public static IHoverTip FromJuHeKeyword()
    {
        return new HoverTip(
            new LocString("cards", JuHeTitleKey),
            new LocString("cards", JuHeDescriptionKey));
    }

    public static IEnumerable<IHoverTip> FromSheathe()
    {
        return [HoverTipFactory.FromCard<NaDao>()];
    }
}
