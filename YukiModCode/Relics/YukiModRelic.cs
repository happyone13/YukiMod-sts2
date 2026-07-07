using BaseLib.Utils;
using STS2RitsuLib.Scaffolding.Content;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public abstract class YukiModRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePathOrDefault(),
        IconOutlinePath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicOutlineImagePathOrDefault(),
        BigIconPath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePathOrDefault());
}
