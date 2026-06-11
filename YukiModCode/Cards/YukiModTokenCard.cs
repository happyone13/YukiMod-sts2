using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YukiMod.YukiModCode.Extensions;
using YukiMod.YukiModCode.Services;

namespace YukiMod.YukiModCode.Cards;

public abstract class YukiModTokenCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target),
    IYukiCardVisualProfile
{
    public virtual bool UseCustomFrame => true;

    public virtual YukiCardSchool School => YukiCardSchool.Other;
    public virtual string? CustomSpinePortraitScenePath => null;
    public virtual YukiSpinePortraitSlot CustomSpinePortraitSlot => YukiSpinePortraitSlot.Ancient;
    public virtual bool HasOwnInspirationEffect => false;
    public virtual bool HasOwnBlackCloudEffect => false;
    public virtual bool IsRealMoonshadow => false;
    public virtual bool CountsAsMoonshadow => IsRealMoonshadow;

    public bool IsInspired { get; set; }
    public bool IsInspirationTriggeredForCurrentPlay { get; private set; }

    public decimal MoonshadowBlackCloudDamageMultiplierBonus { get; set; }

    protected string IdPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePathOrDefault();

    public override string CustomPortraitPath => IdPortraitPath;
    public override string PortraitPath => IdPortraitPath;
    public override string BetaPortraitPath => PortraitPath;

    protected override bool ShouldGlowGoldInternal =>
        IsInspired && YukiInspirationService.CanReceiveInspiration(this);

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
            IsInspirationTriggeredForCurrentPlay = false;
            IsInspired = true;
        }
        else if (Pile?.Type == PileType.Play && oldPileType == PileType.Hand)
        {
            IsInspirationTriggeredForCurrentPlay = YukiInspirationService.CanReceiveInspiration(this) && IsInspired;
            IsInspired = false;
        }
        else if (Pile?.Type != PileType.Hand)
        {
            IsInspirationTriggeredForCurrentPlay = false;
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

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
        {
            if (YukiInspirationService.WillTriggerOnPlay(this))
            {
                await YukiInspirationService.NotifyInspiredTriggered(choiceContext, Owner, this);
            }

            IsInspired = false;
            IsInspirationTriggeredForCurrentPlay = false;
        }
    }
}
