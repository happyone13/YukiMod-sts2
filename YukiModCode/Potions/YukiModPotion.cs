using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Potions;

[Pool(typeof(YukiModPotionPool))]
public abstract class YukiModPotion : CustomPotionModel
{
    public override string CustomPackedImagePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePathOrDefault();
    public override string CustomPackedOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".PotionImagePathOrDefault();
}
