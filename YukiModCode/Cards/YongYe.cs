using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public class YongYe() : YukiModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override YukiCardSchool School => YukiCardSchool.BlackCloud;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [YukiHoverTipFactory.FromNoMing(), HoverTipFactory.FromPower<BlackCloudEnterNextTurnPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BlackCloud", 4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        var allies = CombatState.GetTeammatesOf(Owner.Creature)
            .Append(Owner.Creature)
            .Where(creature => creature is { IsAlive: true, IsPlayer: true })
            .Distinct()
            .Select(creature => creature.Player)
            .OfType<Player>()
            .ToList();

        foreach (var player in allies)
        {
            await YukiBlackCloudService.GainBlackCloud(choiceContext, player, DynamicVars["BlackCloud"].BaseValue, this);
            await YukiPowerService.Apply<BlackCloudEnterNextTurnPower>(choiceContext, player.Creature, 1m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlackCloud"].UpgradeValueBy(2m);
    }
}
