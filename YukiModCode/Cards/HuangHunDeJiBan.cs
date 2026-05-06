using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Powers;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class HuangHunDeJiBan() : YukiModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/huang_hun_de_ji_ban_dynamic.tscn";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<HuangHunDeJiBanPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
