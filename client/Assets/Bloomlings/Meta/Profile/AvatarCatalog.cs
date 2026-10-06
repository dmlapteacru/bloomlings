using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.Meta.Profile
{
    /// <summary>
    /// The price tier of an avatar picture (spec 005 FR-037): the four free defaults, then the bought ones at the owner's
    /// 300, 600 and 1200 Petals (2026-10-05; Remote Config <c>economy.price.avatarCommon</c>, <c>avatarRare</c>,
    /// <c>avatarSpecial</c>).
    /// </summary>
    public enum AvatarTier
    {
        Free,
        Common,
        Rare,
        Special,
    }

    /// <summary>
    /// One avatar picture the profile can show (spec 005 FR-037): its id (<c>avatar.drop_sailor_sticker</c>, stored in
    /// the save's <c>cosmetics.owned</c> once bought and in <c>cosmetics.equipped.profile.avatar</c> once chosen), the
    /// owner's picture it shows (<c>Art/Avatars/Resources/Avatars/{Picture}.jpg</c>), the family it portrays and its
    /// tier.
    /// </summary>
    public sealed record AvatarItem(string Id, string Picture, Family Family, AvatarTier Tier)
    {
        public bool IsFree => Tier == AvatarTier.Free;
    }

    /// <summary>
    /// The owner's avatar pictures (spec 005 FR-037, the owner's delivery of 2026-10-05): one free default per family,
    /// then ten for Petals in three tiers, in the order the edit card lists them (free first, then by price). Avatars are
    /// pictures for the profile only (no gameplay effect, constitution V). Engine-free.
    /// </summary>
    public static class AvatarCatalog
    {
        /// <summary>The prefix of every avatar id.</summary>
        public const string Prefix = "avatar.";

        /// <summary>The avatar a new profile shows: Bloom's default, the hero Home's avatar showed before (<c>CharacterArt.ProfileHero</c>).</summary>
        public const string DefaultId = Prefix + "bloom_default";

        /// <summary>Every avatar, free ones first, then by tier.</summary>
        public static IReadOnlyList<AvatarItem> All { get; } = new[]
        {
            Item("sprig_default", Family.Sprig, AvatarTier.Free),
            Item("bloom_default", Family.Bloom, AvatarTier.Free),
            Item("drop_default", Family.Drop, AvatarTier.Free),
            Item("twig_default", Family.Twig, AvatarTier.Free),
            Item("sprig_ladybug_watercolor", Family.Sprig, AvatarTier.Common),
            Item("bloom_ribbon_plush", Family.Bloom, AvatarTier.Common),
            Item("drop_sailor_sticker", Family.Drop, AvatarTier.Common),
            Item("twig_glasses_scarf_clay", Family.Twig, AvatarTier.Common),
            Item("sprig_dewdrop_papercut", Family.Sprig, AvatarTier.Rare),
            Item("sprig_flower_crown_3d", Family.Sprig, AvatarTier.Rare),
            Item("drop_bubble_shell_3d", Family.Drop, AvatarTier.Rare),
            Item("twig_autumn_wreath_3d", Family.Twig, AvatarTier.Rare),
            Item("bloom_pearl_tiara_3d", Family.Bloom, AvatarTier.Special),
            Item("bloom_jeweled_crown_magic", Family.Bloom, AvatarTier.Special),
        };

        /// <summary>The default avatar.</summary>
        public static AvatarItem Default => Get(DefaultId)!;

        /// <summary>The avatar of an id, or null for an unknown one (from newer content, or not an avatar).</summary>
        public static AvatarItem? Get(string? id)
        {
            if (id == null)
            {
                return null;
            }

            foreach (AvatarItem item in All)
            {
                if (string.Equals(item.Id, id, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>The Remote Config price of a bought tier; null for the free one.</summary>
        public static IntKey? PriceKey(AvatarTier tier) => tier switch
        {
            AvatarTier.Common => RemoteConfigKeys.PriceAvatarCommon,
            AvatarTier.Rare => RemoteConfigKeys.PriceAvatarRare,
            AvatarTier.Special => RemoteConfigKeys.PriceAvatarSpecial,
            _ => null,
        };

        private static AvatarItem Item(string picture, Family family, AvatarTier tier) => new AvatarItem(Prefix + picture, picture, family, tier);
    }
}
