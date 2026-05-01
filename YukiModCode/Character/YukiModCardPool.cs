using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Config;

namespace YukiMod.YukiModCode.Character;

public class YukiModCardPool : CustomCardPoolModel
{
    private const string YukiCardFramePath = "res://YukiMod/images/cards/chaos_frame/card_frame_chaos_s.tres";

    public override string Title => YukiMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => YukiMod.Color;
    public override Texture2D? CustomFrame(CustomCardModel card)
    {
        if (!YukiModConfig.UseYukiCustomCardFrame)
            return null;

        if (card is not YukiModCard model || !model.UseCustomFrame)
            return null;

        return PreloadManager.Cache.GetAsset<Texture2D>(YukiCardFramePath);
    }

    public override bool IsColorless => false;
}
