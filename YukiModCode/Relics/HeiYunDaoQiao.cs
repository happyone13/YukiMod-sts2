using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.HoverTips;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class HeiYunDaoQiao : YukiModRelic, IBlackCloudEnteredListener, IBlackCloudExitedListener
{
    private int _grantedStrength;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [YukiHoverTipFactory.FromBlackCloud(), HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<BlackCloudStancePower>()];

    public override Task BeforeCombatStart()
    {
        _grantedStrength = 0;
        return Task.CompletedTask;
    }

    public async Task OnBlackCloudEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || _grantedStrength != 0)
        {
            return;
        }

        Flash();
        _grantedStrength = 2;
        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<StrengthPower>(choiceContext, Owner.Creature, _grantedStrength, Owner.Creature, null, silent: true);
    }

    public async Task OnBlackCloudExited(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || _grantedStrength == 0)
        {
            return;
        }

        await YukiMod.YukiModCode.Services.YukiPowerService.Apply<StrengthPower>(choiceContext, Owner.Creature, -_grantedStrength, Owner.Creature, null, silent: true);
        _grantedStrength = 0;
    }
}
