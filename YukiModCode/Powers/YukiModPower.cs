using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;
using YukiMod.YukiModCode.Extensions;

namespace YukiMod.YukiModCode.Powers;

public abstract class YukiModPower : ModPowerTemplate
{
    public override LocString Description => AddPowerDescriptionArgs(base.Description);

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePathOrDefault(),
        BigIconPath: $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePathOrDefault());

    protected LocString AddPowerDescriptionArgs(LocString description)
    {
        DynamicVars.AddTo(description);
        description.Add("Amount", Amount);
        description.Add("DisplayAmount", DisplayAmount);
        return description;
    }
}
