using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class YukiStarterRelicPlus : YukiModRelic
{
    private const string SharedIconFileName = "yuki_starter_relic.png";

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromNingJu(), HoverTipFactory.FromCard<YueYing>()];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: SharedIconFileName.RelicImagePathOrDefault(),
        IconOutlinePath: SharedIconFileName.RelicOutlineImagePathOrDefault(),
        BigIconPath: SharedIconFileName.BigRelicImagePathOrDefault());

    public override async Task BeforeCombatStart()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        await YukiMoonshadowService.NingJu(Owner, combatState, 2);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not YueYing || card.Owner != Owner)
        {
            return false;
        }

        modifiedCost -= 1m;
        return true;
    }
}
