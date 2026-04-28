using MegaCrit.Sts2.Core.Entities.Powers;

namespace YukiMod.YukiModCode.Powers;

public class ZhanGangShanPower : YukiModPower
{
    private sealed class Data
    {
        public int LastPlayedRound;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public bool RegisterPlay(int currentRound)
    {
        var data = GetInternalData<Data>();
        var nextAmount = data.LastPlayedRound == currentRound - 1 ? Amount + 1 : 1;
        data.LastPlayedRound = currentRound;
        SetAmount(nextAmount);
        Flash();
        return nextAmount >= 3;
    }
}
