using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public abstract class YukiModRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePathOrDefault();

    protected override string PackedIconOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicOutlineImagePathOrDefault();

    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePathOrDefault();
}
