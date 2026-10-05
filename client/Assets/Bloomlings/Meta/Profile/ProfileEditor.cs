using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.Meta.Profile
{
    /// <summary>The edit card's tabs (spec 005 FR-037), in their order.</summary>
    public enum ProfileTab
    {
        Avatar,
        Frame,
        Badge,
        Name,
    }

    /// <summary>What the edit card's main button does now.</summary>
    public enum ProfileAction
    {
        /// <summary>Keeps the choices and closes the card.</summary>
        Save,

        /// <summary>Buys the picked avatar for Petals (it is not owned yet).</summary>
        Buy,
    }

    /// <summary>What a press of the edit card's main button did.</summary>
    public enum ProfileOutcome
    {
        /// <summary>The choices were kept: the card closes.</summary>
        Saved,

        /// <summary>The picked avatar was bought and is shown: the card stays open.</summary>
        Bought,

        /// <summary>The Petals are short for the picked avatar.</summary>
        NotEnoughPetals,

        /// <summary>The typed name had nothing left once cleaned (<see cref="ProfileService.CleanName"/>).</summary>
        EmptyName,
    }

    /// <summary>
    /// The "Edit profile" card's state (spec 005 FR-037, both builds draw it): the tab shown and the choices picked but not
    /// kept yet (an avatar, a frame, a badge, a name). An avatar not owned yet turns the main button into "Buy" at its
    /// price; Save keeps every choice at once and Close forgets them. Frames and badges come from the Wardrobe (milestones
    /// and the Store), so their tabs list the owned ones and stay locked until the Wardrobe opens. Engine-free.
    /// </summary>
    public sealed class ProfileEditor
    {
        private readonly ProfileService _profile;
        private readonly WardrobeService? _wardrobe;

        public ProfileEditor(ProfileService profile, WardrobeService? wardrobe, ProfileTab tab = ProfileTab.Avatar)
        {
            _profile = profile;
            _wardrobe = wardrobe;
            Tab = tab;
            AvatarId = profile.Avatar.Id;
            FrameId = wardrobe?.Shown(CosmeticKind.Frame)?.Id;
            BadgeId = wardrobe?.Shown(CosmeticKind.Badge)?.Id;
            Name = profile.Name;
        }

        public static IReadOnlyList<ProfileTab> Tabs { get; } = new[] { ProfileTab.Avatar, ProfileTab.Frame, ProfileTab.Badge, ProfileTab.Name };

        public ProfileTab Tab { get; set; }

        /// <summary>The avatar picked (shown in the card's preview).</summary>
        public string AvatarId { get; private set; }

        /// <summary>The frame picked, or null for none owned.</summary>
        public string? FrameId { get; private set; }

        /// <summary>The badge picked, or null for none owned.</summary>
        public string? BadgeId { get; private set; }

        /// <summary>The name typed, or null for the default one.</summary>
        public string? Name { get; private set; }

        public AvatarItem Avatar => AvatarCatalog.Get(AvatarId) ?? AvatarCatalog.Default;

        public CosmeticItem? Frame => Item(FrameId);

        public CosmeticItem? Badge => Item(BadgeId);

        /// <summary>Whether frames and badges can be picked: the Wardrobe is open (they are its items).</summary>
        public bool ProfileItemsOpen => _wardrobe != null && _wardrobe.IsAvailable;

        /// <summary>The owned frames or badges a tab lists (empty while the Wardrobe is closed).</summary>
        public IReadOnlyList<CosmeticItem> Owned(CosmeticKind kind) =>
            ProfileItemsOpen ? _wardrobe!.OwnedOf(kind) : (IReadOnlyList<CosmeticItem>)Array.Empty<CosmeticItem>();

        public void PickAvatar(string avatarId)
        {
            if (AvatarCatalog.Get(avatarId) != null)
            {
                AvatarId = avatarId;
            }
        }

        public void PickFrame(string itemId) => FrameId = Pick(CosmeticKind.Frame, itemId) ?? FrameId;

        public void PickBadge(string itemId) => BadgeId = Pick(CosmeticKind.Badge, itemId) ?? BadgeId;

        /// <summary>Takes a typed name; false (and nothing changes) when nothing is left of it once cleaned.</summary>
        public bool EnterName(string text)
        {
            string? name = ProfileService.CleanName(text);
            if (name == null)
            {
                return false;
            }

            Name = name;
            return true;
        }

        /// <summary>Whether the picked avatar is still to be bought.</summary>
        public bool NeedsBuying => !_profile.Owns(Avatar);

        public ProfileAction Action => NeedsBuying ? ProfileAction.Buy : ProfileAction.Save;

        /// <summary>The picked avatar's price while it is to be bought, else 0.</summary>
        public int Price => NeedsBuying ? _profile.Price(Avatar) : 0;

        /// <summary>The main button: buys the picked avatar, or keeps every choice.</summary>
        public ProfileOutcome Confirm()
        {
            if (NeedsBuying)
            {
                return _profile.TryBuy(AvatarId) ? ProfileOutcome.Bought : ProfileOutcome.NotEnoughPetals;
            }

            if (Name != null && Name != _profile.Name && !_profile.Rename(Name))
            {
                return ProfileOutcome.EmptyName;
            }

            _profile.Choose(AvatarId);
            if (ProfileItemsOpen)
            {
                if (FrameId != null && FrameId != _wardrobe!.Shown(CosmeticKind.Frame)?.Id)
                {
                    _wardrobe.Show(FrameId);
                }

                if (BadgeId != null && BadgeId != _wardrobe!.Shown(CosmeticKind.Badge)?.Id)
                {
                    _wardrobe.Show(BadgeId);
                }
            }

            return ProfileOutcome.Saved;
        }

        private string? Pick(CosmeticKind kind, string itemId)
        {
            foreach (CosmeticItem item in Owned(kind))
            {
                if (item.Id == itemId)
                {
                    return itemId;
                }
            }

            return null;
        }

        private CosmeticItem? Item(string? id) =>
            id != null && _wardrobe != null && _wardrobe.Catalog.TryGet(id, out CosmeticItem? item) ? item : null;
    }
}
