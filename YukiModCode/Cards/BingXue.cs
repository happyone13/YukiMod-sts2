using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class BingXue() : YukiModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string RepeatKey = "Repeat";

    public override YukiCardSchool School => YukiCardSchool.Inspiration;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move), new DynamicVar(RepeatKey, 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars[RepeatKey].IntValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var hand = PileType.Hand.GetPile(Owner);
        var drawPile = PileType.Draw.GetPile(Owner);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            return;
        }

        var inspirationCard = drawPile.Cards.FirstOrDefault(YukiInspirationService.IsInspirationSchoolCard);
        if (inspirationCard == null)
        {
            return;
        }

        await CardPileCmd.Add(inspirationCard, PileType.Draw, CardPilePosition.Top, this, skipVisuals: true);
        await CardPileCmd.Draw(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[RepeatKey].UpgradeValueBy(1m);
    }
}
