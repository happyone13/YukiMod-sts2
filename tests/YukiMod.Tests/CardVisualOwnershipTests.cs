using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TestTheSpire;
using Xunit;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Mechanics.Settings;
using Character = YukiMod.YukiModCode.Character.YukiMod;
using Frame = YukiMod.YukiModCode.Patches.YukiCardCustomFramePatch;
using Portrait = YukiMod.YukiModCode.Patches.YukiCardSpinePortraitPatch;

namespace YukiMod.Tests;

public sealed class CardVisualOwnershipTests : CombatTestSuite
{
    private const string Prefix = "YukiChaos";

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Character>().AddEnemy<BigDummy>().WithSeed("yuki-card-ownership");

    [Fact]
    public async Task Real_card_refresh_reuses_nodes_and_hands_foreign_UI_back_before_redraw()
    {
        await Play(await AddToHand<DefendYuki>());
        var owned = await AddToHand<DefendYuki>();
        var vanilla = await AddToHand<StrikeIronclad>();
        var card = CreateCard();
        var reference = CreateCard();
        try
        {
            reference.Model = vanilla;
            card.Model = vanilla;
            Refresh(reference);
            Refresh(card);
            AssertForeignUntouched(card, portraitFirst: true);
            card.Model = owned;
            Refresh(card);
            var cost = card.Body.GetNode<Control>(Prefix + "CostLine");
            var type = card.Body.GetNode<Label>(Prefix + "CategoryText");
            var upgrade = card.Body.GetNode<Control>(Prefix + "UpgradeIcon");
            ulong costId = cost.GetInstanceId(), typeId = type.GetInstanceId();
            Assert.False(upgrade.Visible);
            owned.UpgradeInternal();
            owned.EnergyCost.SetThisTurn(0);
            Refresh(card);
            Assert.True(upgrade.Visible);
            Assert.Equal("0", card.GetNode<Label>("%EnergyLabel").Text);
            var energy = card.GetNode<Label>("%EnergyLabel");
            energy.AddThemeColorOverride("font_color", Colors.Lime);
            Frame.Apply(card);
            var digit = card.Body.GetNode<Control>(Prefix + "CostText/CostDigit0");
            ulong digitId = digit.GetInstanceId();
            Frame.Apply(card);
            Assert.Equal(digitId, card.Body.GetNode<Control>(Prefix + "CostText/CostDigit0").GetInstanceId());
            Assert.Equal(costId, card.Body.GetNode<Control>(Prefix + "CostLine").GetInstanceId());
            Assert.Equal(typeId, card.Body.GetNode<Label>(Prefix + "CategoryText").GetInstanceId());

            card.Model = vanilla;
            Refresh(card);
            Assert.False(GodotObject.IsInstanceValid(cost));
            Assert.Equal(reference.GetNode<TextureRect>("%Portrait").Texture, card.GetNode<TextureRect>("%Portrait").Texture);
            foreach (string path in new[] { "%TitleLabel", "%EnergyIcon", "%TypeLabel", "%Frame" })
            {
                var expected = reference.GetNode<Control>(path);
                var actual = card.GetNode<Control>(path);
                Assert.Equal(expected.Position, actual.Position);
                Assert.Equal(expected.Visible, actual.Visible);
                Assert.Equal(expected.GetIndex(), actual.GetIndex());
            }
            AssertForeignUntouched(card, portraitFirst: false);
        }
        finally
        {
            card.Free();
            reference.Free();
        }
    }

    [Fact]
    public async Task Dynamic_portrait_reuses_only_its_own_model_and_native_pool_clear_wins()
    {
        await Play(await AddToHand<DefendYuki>());
        var first = await AddToHand<BaDao>();
        var second = await AddToHand<BaDao>();
        var vanilla = await AddToHand<StrikeIronclad>();
        var card = CreateCard();
        bool previous = YukiModSharedSettings.DynamicCardPortraitsEnabled;
        try
        {
            YukiModSharedSettings.SetDynamicCardPortraitsEnabled(true, persist: false);
            var ancient = card.GetNode<TextureRect>("%AncientPortrait");
            var foreign = new Control { Name = "OtherSkinOverlay" };
            ancient.AddChild(foreign);
            card.Model = first;
            Refresh(card);
            var overlay = ancient.GetNode<Control>(Portrait.SpineOverlayNodeName);
            ulong oldId = overlay.GetInstanceId();
            Refresh(card);
            Assert.Equal(oldId, ancient.GetNode<Control>(Portrait.SpineOverlayNodeName).GetInstanceId());
            card.Model = second;
            Refresh(card);
            Assert.False(GodotObject.IsInstanceValid(overlay));
            Assert.NotEqual(oldId, ancient.GetNode<Control>(Portrait.SpineOverlayNodeName).GetInstanceId());
            card.OnFreedToPool();
            Assert.Null(card.GetNode<TextureRect>("%Portrait").Texture);
            Assert.Null(ancient.GetNodeOrNull<Control>(Portrait.SpineOverlayNodeName));
            Assert.Same(ancient, foreign.GetParent());
            card.Model = vanilla;
            Refresh(card);
            AssertForeignUntouched(card, portraitFirst: true);
        }
        finally
        {
            card.Free();
            YukiModSharedSettings.SetDynamicCardPortraitsEnabled(previous, persist: false);
        }
    }

    private static void AssertForeignUntouched(NCard card, bool portraitFirst)
    {
        var normal = card.GetNode<TextureRect>("%Portrait");
        var ancient = card.GetNode<TextureRect>("%AncientPortrait");
        var foreign = new Control { Name = Prefix + "CostLine", Visible = false };
        var oldTexture = normal.Texture;
        bool oldVisibility = normal.Visible, oldClip = ancient.ClipContents;
        card.Body.AddChild(foreign);
        try
        {
            normal.Texture = null;
            normal.Visible = false;
            ancient.ClipContents = false;
            if (portraitFirst) Portrait.RemoveSpineOverlay(card);
            Frame.PrepareForBaseVisuals(card);
            Frame.Apply(card);
            Frame.CleanupPooledCard(card);
            Portrait.Apply(card);
            if (!portraitFirst) Portrait.RemoveSpineOverlay(card);
            Assert.Null(normal.Texture);
            Assert.False(normal.Visible);
            Assert.False(ancient.ClipContents);
            Assert.Same(card.Body, foreign.GetParent());
            Assert.False(foreign.Visible);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(foreign)) foreign.Free();
            normal.Texture = oldTexture;
            normal.Visible = oldVisibility;
            ancient.ClipContents = oldClip;
        }
    }

    private static NCard CreateCard()
    {
        var result = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
        result.Scale = Vector2.One * 1.2f;
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(result);
        return result;
    }

    private static void Refresh(NCard card) => card.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
}
