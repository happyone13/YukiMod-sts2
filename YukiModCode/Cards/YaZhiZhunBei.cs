using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class YaZhiZhunBei() : YukiModCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://YukiMod/scenes/cards/ya_zhi_zhun_bei_dynamic.tscn";

    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), HoverTipFactory.FromPower<VigorPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VigorPower>(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        YukiAudioService.TryPlayCustomCastCardClip("ya_zhi_zhun_bei", Owner);

        var hand = PileType.Hand.GetPile(Owner);
        var drawLimit = YukiCardPileService.MaxCardsInHand - hand.Cards.Count;
        if (drawLimit > 0)
        {
            if (YukiInspirationService.WillTriggerOnPlay(this))
            {
                await YukiInspirationService.DrawPrioritizedInspirationCard(choiceContext, Owner, this);
            }
            else
            {
                await CardPileCmd.Draw(choiceContext, Owner);
            }
        }

        await YukiPowerService.Apply<VigorPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["VigorPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VigorPower"].UpgradeValueBy(3m);
    }
}
