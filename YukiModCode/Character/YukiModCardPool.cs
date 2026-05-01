using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;
using YukiMod.YukiModCode.Patches;

namespace YukiMod.YukiModCode.Character;

public class YukiModCardPool : CustomCardPoolModel
{
    private static Texture2D? _customFrameTexture;

    public override string Title => YukiMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => YukiMod.Color;

    public override bool IsColorless => false;

    public override Texture2D? CustomFrame(CustomCardModel card)
    {
        if (!YukiModConfig.UseYukiCardDynamicPortraits)
            return null;

        if (card is YukiModCard { UseCustomFrame: true } or YukiModTokenCard { UseCustomFrame: true })
            return _customFrameTexture ??= GD.Load<Texture2D>(YukiCardFramePaths.CustomFrameTexturePath);

        return null;
    }
}
