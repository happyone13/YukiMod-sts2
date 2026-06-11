using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Powers;

public abstract class YukiModPower : CustomPowerModel
{
    public override LocString Description => AddPowerDescriptionArgs(base.Description);

    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => CustomPackedIconPath;

    protected LocString AddPowerDescriptionArgs(LocString description)
    {
        DynamicVars.AddTo(description);
        description.Add("Amount", Amount);
        description.Add("DisplayAmount", DisplayAmount);
        return description;
    }
}
