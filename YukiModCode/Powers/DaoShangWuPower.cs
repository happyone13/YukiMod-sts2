using MegaCrit.Sts2.Core.Entities.Powers;

namespace YukiMod.YukiModCode.Powers;

/// <summary>Combat-local shared play count for all Dao Shang Wu copies owned by one player.</summary>
public class DaoShangWuPower : YukiModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
