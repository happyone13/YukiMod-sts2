using MegaCrit.Sts2.Core.Entities.Powers;

namespace YukiMod.YukiModCode.Powers;

public class YuePower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
