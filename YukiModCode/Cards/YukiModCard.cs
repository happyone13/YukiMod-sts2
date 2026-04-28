using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public abstract class YukiModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public virtual YukiCardSchool School => YukiCardSchool.Other;

    protected string IdPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePathOrDefault();
    protected string IdBigPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePathOrDefault();

    public override string CustomPortraitPath => IdBigPortraitPath;
    public override string PortraitPath => IdPortraitPath;
    public override string BetaPortraitPath => $"beta/{Id.Entry.ToLowerInvariant()}.png".CardImagePath();

    protected override void AddExtraArgsToDescription(LocString description)
    {
        DynamicVars.AddTo(description);
    }
}
