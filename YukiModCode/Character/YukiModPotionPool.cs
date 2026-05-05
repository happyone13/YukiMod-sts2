using BaseLib.Abstracts;
using Godot;

namespace YukiMod.YukiModCode.Character;

public class YukiModPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => "defect";
    public override Color LabOutlineColor => YukiMod.Color;
}
