using MegaCrit.Sts2.Core.Entities.Powers;

namespace YukiMod.YukiModCode.Powers;

public class XuePower : YukiModPower
{
    private sealed class Data
    {
        public bool CreatedJuHe;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public bool HasCreatedJuHe => GetInternalData<Data>().CreatedJuHe;

    public void MarkJuHeCreated()
    {
        GetInternalData<Data>().CreatedJuHe = true;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }
}
