using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class BaDaoZhan() : YukiModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override YukiCardSchool School => YukiCardSchool.Inspiration;
    public override bool HasOwnInspirationEffect => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromInspiration()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawnCard = (await CardPileCmd.Draw(choiceContext, 1m, Owner)).FirstOrDefault();
        if (drawnCard == null)
        {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, drawnCard, null);
    }

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || !YukiInspirationService.WillTriggerOnPlay(this) || originalCost <= 0m)
        {
            return false;
        }

        modifiedCost = originalCost - 1m;
        if (modifiedCost < 0m)
        {
            modifiedCost = 0m;
        }

        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
