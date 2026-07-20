using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YukiMod.YukiModCode.Encounters;

namespace YukiMod.YukiModCode.Cards;

/// <summary>
/// A character-neutral, combat-only escape option supplied by YukiMod's Gloomy encounter.
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class GloomyEscape() : YukiModTokenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    public override bool UseCustomFrame => false;

    protected override bool ShouldGlowGoldInternal => false;

    public override Task BeforeCardPlayed(CardPlay cardPlay) => Task.CompletedTask;

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source) =>
        Task.CompletedTask;

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) =>
        Task.CompletedTask;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState?.Encounter is not GloomyPackEncounter encounter)
            return;

        encounter.MarkPlayerEscaped();

        // Escape mutates the enemy collection; never apply it to player creatures.
        foreach (var enemy in combatState.Enemies.ToList())
            await CreatureCmd.Escape(enemy, removeCreatureNode: true);
    }

    protected override void OnUpgrade()
    {
    }
}
