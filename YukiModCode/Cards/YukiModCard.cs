using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Extensions;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

[Pool(typeof(YukiModCardPool))]
public abstract class YukiModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public virtual YukiCardSchool School => YukiCardSchool.Other;
    public virtual bool IsRealMoonshadow => false;
    public virtual bool CountsAsMoonshadow => IsRealMoonshadow;

    public bool IsInspired { get; set; }

    public decimal MoonshadowBlackCloudDamageMultiplierBonus { get; set; }

    protected string IdPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePathOrDefault();
    protected string IdBigPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePathOrDefault();

    public override string CustomPortraitPath => IdBigPortraitPath;
    public override string PortraitPath => IdPortraitPath;
    public override string BetaPortraitPath => $"beta/{Id.Entry.ToLowerInvariant()}.png".CardImagePath();

    protected override bool ShouldGlowGoldInternal =>
        School == YukiCardSchool.Inspiration && IsInspired;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        DynamicVars.AddTo(description);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this)
        {
            return Task.CompletedTask;
        }

        if (Pile?.Type == PileType.Hand && oldPileType != PileType.Hand)
        {
            IsInspired = true;
        }
        else if (Pile?.Type != PileType.Hand)
        {
            IsInspired = false;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this)
        {
            IsInspired = !fromHandDraw;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
        {
            if (School == YukiCardSchool.Inspiration && IsInspired)
            {
                YukiInspirationService.NotifyInspiredTriggered(Owner, this);
            }

            IsInspired = false;
        }

        return Task.CompletedTask;
    }
}
