using System.Collections.Generic;
using System.Linq;
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
public class ShenYaZhiZhunBei() : YukiModCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration(), HoverTipFactory.FromPower<VigorPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VigorPower>(2m)];

    private static bool IsInspiredAttack(CardModel card) =>
        card.Type == CardType.Attack &&
        YukiInspirationService.IsInspirationCard(card);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        while (true)
        {
            var hand = PileType.Hand.GetPile(Owner);
            if (YukiCardPileService.MaxCardsInHand - hand.Cards.Count <= 0)
            {
                break;
            }

            var candidates = PileType.Draw.GetPile(Owner).Cards
                .Concat(PileType.Discard.GetPile(Owner).Cards)
                .Where(card => card.Type == CardType.Attack)
                .ToList();
            if (candidates.Count == 0)
            {
                break;
            }

            var selectedAttack = YukiInspirationService.WillTriggerOnPlay(this)
                ? candidates.FirstOrDefault(IsInspiredAttack) ?? candidates.First()
                : candidates.First();
            if (selectedAttack == null)
            {
                break;
            }

            await CardPileCmd.Add(selectedAttack, PileType.Hand, clonedBy: this);
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
