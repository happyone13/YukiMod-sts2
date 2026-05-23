using Godot;

namespace YukiMod.YukiModCode.Mechanics.Vfx;

public enum ChaosVfxLayer
{
	BattleBack = 0,
	BattleFront = 1,
	CreatureBelow = 2,
	CreatureAbove = 3
}

public enum ChaosVfxMode
{
	StaticOnce = 0,
	FollowLoop = 1,
	FollowOnce = 2,
	ProjectileLinear = 3,
	ProjectileInstantLine = 4
}

public readonly record struct ChaosVfxSpec(
	string ScenePath,
	ChaosVfxLayer Layer,
	ChaosVfxMode Mode,
	string? PlayAnim = null,
	bool PlayLoop = false,
	string? OutAnim = null,
	Vector2? Offset = null,
	int? ZIndex = null,
	float? DurationSeconds = null);


