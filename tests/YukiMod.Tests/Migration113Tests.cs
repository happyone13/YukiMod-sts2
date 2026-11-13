using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Godot;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Encounters;
using YukiMod.YukiModCode.Monsters.Gloomy;
using YukiMod.YukiModCode.Monsters.Gloomy.Powers;
using YukiMod.YukiModCode.Patches;
using YukiCharacter = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.Tests;

public sealed class Migration113Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<YukiCharacter>().AddEnemy<BigDummy>().WithSeed("yukimod-113-migration");
    }

    [Fact]
    public async Task Reflection_targets_and_both_character_selection_postfixes_are_applied()
    {
        await Play(await AddToHand<DefendYuki>());
        foreach (var type in typeof(MainFile).Assembly.GetTypes()
                     .Where(type => !type.IsAbstract && typeof(IPatchMethod).IsAssignableFrom(type)))
        {
            var targets = (ModPatchTarget[])type.GetMethod("GetTargets")!.Invoke(null, null)!;
            foreach (var target in targets)
                Assert.NotNull(PatchTargetMethodResolver.ResolveRequired(target));
        }

        var selection = AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter));
        var patches = Harmony.GetPatchInfo(selection);
        Assert.NotNull(patches);
        Assert.Contains(patches.Postfixes, patch => patch.PatchMethod.DeclaringType == typeof(YukiCharacterSelectAnimLoopPatch));
        Assert.Contains(patches.Postfixes, patch => patch.PatchMethod.DeclaringType == typeof(YukiCharacterSelectBgToggleButtonPatch));
    }

    [Fact]
    public async Task Character_selection_loads_both_backgrounds_and_shows_the_toggle()
    {
        await Play(await AddToHand<DefendYuki>());
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/screens/character_select_screen.tscn");
        Assert.NotNull(scene);
        var screen = scene.Instantiate<NCharacterSelectScreen>();
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(screen);
        try
        {
            screen.InitializeSingleplayer();
            var button = Assert.Single(screen.GetNode("CharSelectButtons/ButtonContainer")
                .GetChildren().OfType<NCharacterSelectButton>()
                .Where(candidate => candidate.Character is YukiCharacter));
            Assert.False(button.IsLocked);
            screen.SelectCharacter(button);
            await tree.ToSignal(tree.CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
            var toggle = screen.GetNode<Button>("YukiBgToggleButton");
            Assert.True(toggle.Visible);
            Assert.NotNull(screen.GetNode("AnimatedBg/YukiCharSelectBg1"));
            Assert.NotNull(screen.GetNode("AnimatedBg/YukiCharSelectBg2"));
            if (DisplayServer.GetName() != "headless")
            {
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = tree.Root.GetTexture().GetImage();
                Assert.Equal(Error.Ok, image.SavePng(
                    Path.Combine(RepositoryRoot(), "tmp", "migration113", "character-selection.png")));
            }
        }
        finally
        {
            screen.OnSubmenuClosed();
            screen.Free();
        }
    }

    [Fact]
    public async Task ZhaoJia_preview_and_conversion_include_previously_played_cards()
    {
        await Play(await AddToHand<DefendYuki>());
        var card = await AddToHand<ZhaoJia>();
        card.DynamicVars.Block.UpdateCardPreview(card, CardPreviewMode.Normal, null, true);
        Assert.Equal(6m, card.DynamicVars.Block.PreviewValue);
        Assert.Equal(6m, Convert.ToDecimal(card.DynamicVars.Block));
        var blockBefore = Player.Creature.Block;
        await Play(card);
        Assert.Equal(blockBefore + 6m, Player.Creature.Block);
    }

    [Fact]
    public async Task Game_over_die_cue_plays_the_existing_death_animation()
    {
        await Play(await AddToHand<DefendYuki>());
        var visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            ((YukiCharacter)Player.Character).CustomVisualsPath!);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(visuals);
        try
        {
            visuals.Position = tree.Root.GetVisibleRect().Size * new Vector2(0.5f, 0.8f);
            Assert.NotNull(visuals.SpineBody);
            Assert.True(visuals.SpineBody.HasAnimation("death"));
            Assert.True(ModCreatureVisualPlayback.TryPlayCue(visuals, Player.Character, "die"));
            await tree.ToSignal(tree.CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Assert.Equal("death", visuals.SpineBody.GetAnimationState().GetCurrent(0)?.GetAnimationName());
            if (DisplayServer.GetName() != "headless")
            {
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = tree.Root.GetTexture().GetImage();
                Assert.Equal(Error.Ok, image.SavePng(
                    Path.Combine(RepositoryRoot(), "tmp", "migration113", "death-animation.png")));
            }
        }
        finally
        {
            visuals.Free();
        }
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", ".."));
    }
}

public sealed class Migration113GloomyTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<YukiCharacter>().Encounter<GloomyPackEncounter>().WithSeed("yukimod-113-gloomy-death");
    }

    [Fact]
    public async Task Beast_death_queues_one_replacement_on_the_next_enemy_turn()
    {
        var prime = Enemy<GloomyPrimeMonster>();
        var beast = Combat.Enemies.First(enemy => enemy.Monster is GloomyBeastMonster);
        var summon = Assert.IsType<GloomySummonPower>(
            await ApplyPower<GloomySummonPower>(prime, 2, prime));
        var source = await AddToHand<StrikeYuki>();
        await CreatureCmd.Kill(beast, source);
        await WaitForIdle();
        Assert.Equal(2, Combat.Enemies.Count(enemy => enemy.Monster is GloomyBeastMonster));
        await EndTurn();
        Assert.Equal(3, Combat.Enemies.Count(enemy => enemy.Monster is GloomyBeastMonster));
        Assert.Equal(1, summon.Amount);
    }
}
