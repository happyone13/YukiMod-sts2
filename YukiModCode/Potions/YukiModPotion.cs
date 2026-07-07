using BaseLib.Utils;
using STS2RitsuLib.Scaffolding.Content;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Potions;

public abstract class YukiModPotion : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePathOrDefault(),
        OutlinePath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".PotionImagePathOrDefault());
}
