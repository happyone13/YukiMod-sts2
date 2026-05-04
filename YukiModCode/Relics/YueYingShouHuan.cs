using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class YueYingShouHuan : YukiModRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<YueYing>()];

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side || !YukiMoonshadowService.GetMoonshadowCardsInHand(Owner).Any())
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 2m, ValueProp.Unpowered, null);
    }
}
