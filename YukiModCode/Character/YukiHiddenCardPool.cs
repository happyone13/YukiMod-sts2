using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace YukiMod.YukiModCode.Character;

// Cards in this pool stay available to code but are excluded from the character card pool.
public class YukiHiddenCardPool : TypeListCardPoolModel
{
    public override string Title => "YUKIMOD_NONE_POOL";
    public override string EnergyColorName => "none";
    public override Color DeckEntryCardColor => new("FFFFFF");
    public override bool IsColorless => true;
}
