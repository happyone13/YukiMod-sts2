using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

namespace YukiMod.YukiModCode.Patches;

public sealed class YukiDeathAnimationCuePatch : IPatchMethod
{
    public static string PatchId => "YukiMod.Animation.DeathCue";
    public static bool IsCritical => false;
    public static string Description => "Map Yuki game-over death cue to its Spine animation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(ModCreatureVisualPlayback), nameof(ModCreatureVisualPlayback.TryPlayCue),
            typeof(NCreatureVisuals), typeof(CharacterModel), typeof(string), typeof(ReadOnlySpan<string>), typeof(bool))
    ];

    public static void Prefix(CharacterModel? character, ref string primaryCue)
    {
        if (YukiTarget.IsTarget(character) &&
            string.Equals(primaryCue, "die", StringComparison.OrdinalIgnoreCase))
        {
            primaryCue = "death";
        }
    }
}
