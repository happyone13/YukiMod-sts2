using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Relics;

namespace YukiMod.YukiModCode.Character;

public class YukiMod : PlaceholderCharacterModel
{
    public const string CharacterId = "YukiMod";

    public static readonly Color Color = new("9DD9D2");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 72;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeYuki>(),
        ModelDb.Card<StrikeYuki>(),
        ModelDb.Card<StrikeYuki>(),
        ModelDb.Card<StrikeYuki>(),
        ModelDb.Card<DefendYuki>(),
        ModelDb.Card<DefendYuki>(),
        ModelDb.Card<DefendYuki>(),
        ModelDb.Card<DefendYuki>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<YukiStarterRelic>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<YukiModCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<YukiModRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<YukiModPotionPool>();

    public override string CustomIconTexturePath => "res://YukiMod/images/charui/character_icon_meilin_name.png";
    public override string CustomCharacterSelectIconPath => "res://YukiMod/images/charui/char_select_char_meilin.png";
    public override string CustomCharacterSelectLockedIconPath => "res://YukiMod/images/charui/char_select_char_name_locked.png";
    public override string CustomMapMarkerPath => "res://YukiMod/images/charui/map_marker_meilin_name.png";
    public override Color EnergyLabelOutlineColor => Color.Color8(120, 190, 185);

    public override string CustomIconPath => "res://YukiMod/scenes/yuki_icon.tscn";
    public override string CustomVisualPath => "res://YukiMod/scenes/yuki_character.tscn";
    public override string CustomRestSiteAnimPath => "res://YukiMod/scenes/yuki_character_camp.tscn";
    public override string CustomMerchantAnimPath => "res://YukiMod/scenes/merchant/characters/yukimod_merchant.tscn";
    public override string CustomCharacterSelectBg => "res://YukiMod/scenes/yuki_bg.tscn";

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) =>
        SetupAnimationState(
            controller,
            idleName: "idle",
            deadName: "death",
            deadLoop: false,
            hitName: "hit",
            hitLoop: false,
            attackName: "attack_play1",
            attackLoop: false,
            castName: "buff_play",
            castLoop: false,
            relaxedName: "camping",
            relaxedLoop: true);
}
