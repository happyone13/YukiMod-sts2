using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.HoverTips;

public static class YukiHoverTipFactory
{
    private const string ForeseeTitleKey = "YUKIMOD-FORESEE.title";
    private const string ForeseeDescriptionKey = "YUKIMOD-FORESEE.description";

    public static IHoverTip FromForesee()
    {
        return new HoverTip(
            new LocString("cards", ForeseeTitleKey),
            new LocString("cards", ForeseeDescriptionKey));
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
