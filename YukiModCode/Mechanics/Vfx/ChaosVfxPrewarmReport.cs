namespace YukiMod.YukiModCode.Mechanics.Vfx;

public readonly record struct ChaosVfxPrewarmReport(int Requested, int Loaded)
{
	public int Failed => Math.Max(0, Requested - Loaded);

	public static ChaosVfxPrewarmReport Empty => new(0, 0);

	public static ChaosVfxPrewarmReport operator +(
		ChaosVfxPrewarmReport left,
		ChaosVfxPrewarmReport right) =>
		new(left.Requested + right.Requested, left.Loaded + right.Loaded);
}
