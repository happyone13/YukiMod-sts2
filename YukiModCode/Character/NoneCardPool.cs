using BaseLib.Abstracts;
using Godot;

namespace YukiMod.YukiModCode.Character;

// Cards in this pool stay available to code but are excluded from the character card pool.
public class NoneCardPool : CustomCardPoolModel
{
    public override string Title => "YUKIMOD_NONE_POOL";
    public override string EnergyColorName => "none";
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => new("FFFFFF");
    public override bool IsColorless => true;
}
