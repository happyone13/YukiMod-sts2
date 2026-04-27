using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using YukiMod.YukiModCode.Character;

namespace YukiMod.YukiModCode.Relics;

[Pool(typeof(YukiModRelicPool))]
public class YukiStarterRelic : YukiModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
}
