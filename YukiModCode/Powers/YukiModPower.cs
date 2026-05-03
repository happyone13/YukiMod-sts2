using BaseLib.Abstracts;
using BaseLib.Extensions;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Powers;

public abstract class YukiModPower : CustomPowerModel
{
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => CustomPackedIconPath;
}
