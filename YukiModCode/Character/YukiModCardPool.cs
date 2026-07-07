using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace YukiMod.YukiModCode.Character;

public class YukiModCardPool : TypeListCardPoolModel
{
    public override string Title => YukiMod.CharacterId;
    public override string EnergyColorName => "defect";
    public override Color DeckEntryCardColor => YukiMod.Color;

    public override bool IsColorless => false;
}
