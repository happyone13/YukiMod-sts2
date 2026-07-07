using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace YukiMod.YukiModCode.Character;

public class YukiModRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "defect";
    public override Color LabOutlineColor => YukiMod.Color;
}
