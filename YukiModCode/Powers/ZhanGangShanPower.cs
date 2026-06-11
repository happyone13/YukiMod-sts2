using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.HoverTips;

namespace YukiMod.YukiModCode.Powers;

public class ZhanGangShanPower : YukiModPower
{
    private sealed class Data
    {
        public bool HasRegisteredPlay;
        public int LastPlayedRound;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        YukiHoverTipFactory.FromIai();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public bool RegisterPlay(int currentRound)
    {
        var data = GetInternalData<Data>();
        var nextAmount = data.HasRegisteredPlay && data.LastPlayedRound == currentRound - 1
            ? Amount + 1
            : 1;

        data.HasRegisteredPlay = true;
        data.LastPlayedRound = currentRound;
        SetAmount(nextAmount);
        Flash();
        return nextAmount >= 3;
    }
}
