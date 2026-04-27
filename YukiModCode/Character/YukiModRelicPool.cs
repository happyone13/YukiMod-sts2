using BaseLib.Abstracts;
using Godot;

namespace YukiMod.YukiModCode.Character;

public class YukiModRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => YukiMod.Color;
}
