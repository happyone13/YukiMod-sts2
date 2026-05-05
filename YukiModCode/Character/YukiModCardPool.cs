using System.Collections.Generic;
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
    private static readonly Dictionary<string, Texture2D?> CustomFrameTextures = new();

    public override string Title => YukiMod.CharacterId;
    public override string EnergyColorName => "defect";
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => YukiMod.Color;

    public override bool IsColorless => false;

    public override Texture2D? CustomFrame(CustomCardModel card)
    {
        if (card is YukiModCard { UseCustomFrame: true } or YukiModTokenCard { UseCustomFrame: true })
        {
            string texturePath = YukiCardFramePaths.GetCustomFrameTexturePath(card.Rarity);
            if (!CustomFrameTextures.TryGetValue(texturePath, out Texture2D? texture))
            {
                texture = GD.Load<Texture2D>(texturePath);
                CustomFrameTextures[texturePath] = texture;
            }

            return texture;
        }

        return null;
    }
}
