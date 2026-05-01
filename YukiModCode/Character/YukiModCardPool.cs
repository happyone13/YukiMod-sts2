using BaseLib.Abstracts;
using Godot;
using YukiMod.YukiModCode.Cards;

namespace YukiMod.YukiModCode.Character;

public class YukiModCardPool : CustomCardPoolModel
{
    public override string Title => YukiMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => YukiMod.Color;

    public override bool IsColorless => false;
}
