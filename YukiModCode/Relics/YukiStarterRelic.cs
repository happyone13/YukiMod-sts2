using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class YukiStarterRelic : YukiModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromNingJu(), HoverTipFactory.FromCard<YueYing>()];

    public override async Task BeforeCombatStart()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        await YukiMoonshadowService.NingJu(Owner, combatState, 1);
    }
}
