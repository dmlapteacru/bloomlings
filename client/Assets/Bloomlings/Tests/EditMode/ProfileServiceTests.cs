using System;
using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>The profile page's data (spec 005 FR-037): avatars, name, ID, joining day, the edit card and the save.</summary>
    public class ProfileServiceTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

        private static string AvatarFolder => Path.Combine(Application.dataPath, "Bloomlings", "Art", "Avatars", "Resources", "Avatars");

        private static string CatalogPath => Path.Combine(Application.dataPath, "Bloomlings", "Meta", "Wardrobe", "Resources", "CosmeticCatalog.json");

        [Test]
        public void TheCatalog_HasFourFreeDefaults_AndTenForPetalsAtTheOwnersPrices()
        {
            Assert.That(AvatarCatalog.All.Count, Is.EqualTo(14));
            Assert.That(AvatarCatalog.All.Select(a => a.Id).Distinct().Count(), Is.EqualTo(14));
            Assert.That(AvatarCatalog.All.Where(a => a.IsFree).Select(a => a.Family), Is.EquivalentTo(new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig }));
            Assert.That(AvatarCatalog.All.Count(a => a.Tier == AvatarTier.Common), Is.EqualTo(4));
            Assert.That(AvatarCatalog.All.Count(a => a.Tier == AvatarTier.Rare), Is.EqualTo(4));
            Assert.That(AvatarCatalog.All.Count(a => a.Tier == AvatarTier.Special), Is.EqualTo(2));
            Assert.That(AvatarCatalog.Default.IsFree, Is.True);
            Assert.That(AvatarCatalog.All.Select(a => (int)a.Tier), Is.Ordered, "free first, then by price");

            ProfileService profile = Service(out _, out _);
            Assert.That(AvatarCatalog.All.Where(a => !a.IsFree).Select(profile.Price).Distinct(), Is.EquivalentTo(new[] { 300, 600, 1200 }));
            foreach (AvatarItem avatar in AvatarCatalog.All)
            {
                Assert.That(avatar.Id, Is.EqualTo(AvatarCatalog.Prefix + avatar.Picture));
                Assert.That(avatar.Picture, Does.StartWith(WardrobeService.FamilyKey(avatar.Family) + "_"), "the picture portrays its family");
            }
        }

        [Test]
        public void EveryAvatar_HasItsPicture_Square_AndSmall()
        {
            foreach (AvatarItem avatar in AvatarCatalog.All)
            {
                string path = Path.Combine(AvatarFolder, avatar.Picture + ".jpg");
                Assert.That(File.Exists(path), Is.True, path);
                byte[] bytes = File.ReadAllBytes(path);
                Assert.That(bytes.Length, Is.LessThan(80 * 1024), avatar.Picture + ": kept small (384 px JPEG)");
                (int w, int h) = JpegSize(bytes);
                Assert.That((w, h), Is.EqualTo((384, 384)), avatar.Picture);
            }

            Assert.That(Directory.GetFiles(AvatarFolder, "*.jpg").Length, Is.EqualTo(AvatarCatalog.All.Count), "no picture without an avatar");
        }

        [Test]
        public void FreeAvatars_AreOwned_AndTheDefaultIsShown()
        {
            ProfileService profile = Service(out PlayerSave save, out _);
            Assert.That(profile.Avatar.Id, Is.EqualTo(AvatarCatalog.DefaultId));
            Assert.That(AvatarCatalog.All.Where(a => a.IsFree).All(profile.Owns), Is.True);
            Assert.That(profile.Choose("avatar.drop_default"), Is.True);
            Assert.That(profile.Avatar.Id, Is.EqualTo("avatar.drop_default"));
            Assert.That(save.Cosmetics.Equipped["profile.avatar"], Is.EqualTo("avatar.drop_default"));
            Assert.That(profile.Choose("avatar.drop_sailor_sticker"), Is.False, "not bought");
            Assert.That(profile.Choose("avatar.unknown"), Is.False);

            // An unknown or unowned choice in the save falls back to the default.
            save.Cosmetics.Equipped["profile.avatar"] = "avatar.bloom_pearl_tiara_3d";
            Assert.That(profile.Avatar.Id, Is.EqualTo(AvatarCatalog.DefaultId));
        }

        [Test]
        public void Buying_SpendsPetalsOnce_AndShowsTheAvatar()
        {
            ProfileService profile = Service(out PlayerSave save, out EconomyService economy);
            int bought = 0;
            profile.Bought += _ => bought++;
            economy.Grant(299, null);
            Assert.That(profile.TryBuy("avatar.drop_sailor_sticker"), Is.False, "Petals short");
            Assert.That(save.Cosmetics.Owned, Is.Empty);
            Assert.That(save.Cosmetics.Equipped.ContainsKey("profile.avatar"), Is.False, "the failed buy left nothing behind");

            economy.Grant(1, null);
            Assert.That(profile.TryBuy("avatar.drop_sailor_sticker"), Is.True);
            Assert.That(economy.Petals, Is.EqualTo(0));
            Assert.That(save.Cosmetics.Owned, Does.Contain("avatar.drop_sailor_sticker"));
            Assert.That(profile.Avatar.Id, Is.EqualTo("avatar.drop_sailor_sticker"));
            Assert.That(bought, Is.EqualTo(1));

            economy.Grant(1000, null);
            Assert.That(profile.TryBuy("avatar.drop_sailor_sticker"), Is.False, "bought once");
            Assert.That(profile.TryBuy("avatar.bloom_default"), Is.False, "free avatars are not sold");
            Assert.That(economy.Petals, Is.EqualTo(1000));
        }

        [Test]
        public void Names_AreCleaned_AndTheDefaultNumberIsStable()
        {
            Assert.That(ProfileService.CleanName("  Rose   Petal  "), Is.EqualTo("Rose Petal"));
            Assert.That(ProfileService.CleanName("Ёжик_42"), Is.EqualTo("Ёжик_42"));
            Assert.That(ProfileService.CleanName("a<b>c\n\td😀"), Is.EqualTo("abc d"));
            Assert.That(ProfileService.CleanName("   "), Is.Null);
            Assert.That(ProfileService.CleanName("!!!"), Is.Null);
            Assert.That(ProfileService.CleanName(new string('x', 30))!.Length, Is.EqualTo(ProfileService.MaxNameLength));
            Assert.That(ProfileService.CleanName("abcdefghijklmno pq"), Is.EqualTo("abcdefghijklmno"), "no space left at the cut");

            ProfileService profile = Service(out PlayerSave save, out _);
            Assert.That(profile.Name, Is.Null);
            Assert.That(profile.DefaultNumber, Is.InRange(1000, 9999));
            Assert.That(Service(out _, out _).DefaultNumber, Is.EqualTo(profile.DefaultNumber), "the same player ID, the same number");
            Assert.That(profile.Rename("  "), Is.False);
            Assert.That(profile.Rename(" Rosie "), Is.True);
            Assert.That(save.Profile.Name, Is.EqualTo("Rosie"));
            Assert.That(profile.ShortId, Is.EqualTo("0A1B2C3D"));
        }

        [Test]
        public void TheJoiningDay_IsSetOnce()
        {
            ProfileService profile = Service(out PlayerSave save, out _);
            Assert.That(save.Profile.JoinedAt, Is.EqualTo("2026-10-05"));
            Assert.That(profile.JoinedMonth, Is.EqualTo("10/2026"));
            var later = new ProfileService(save, new BundledRemoteConfigService(), new Clock(Today.AddDays(40)), () => { });
            Assert.That(later.JoinedAt, Is.EqualTo(Today), "kept");
        }

        [Test]
        public void TheProfile_RoundTripsThroughTheSave_AndMerges()
        {
            ProfileService profile = Service(out PlayerSave save, out EconomyService economy);
            economy.Grant(600, null);
            Assert.That(profile.TryBuy("avatar.twig_autumn_wreath_3d"), Is.True);
            profile.Rename("Moss");

            PlayerSave read = SaveSerializer.Read(SaveSerializer.Write(save));
            Assert.That(read.Profile.Name, Is.EqualTo("Moss"));
            Assert.That(read.Profile.JoinedAt, Is.EqualTo("2026-10-05"));
            Assert.That(read.Cosmetics.Equipped["profile.avatar"], Is.EqualTo("avatar.twig_autumn_wreath_3d"));
            Assert.That(read.Cosmetics.Owned, Does.Contain("avatar.twig_autumn_wreath_3d"));

            PlayerSave other = PlayerSave.CreateNew("p2", Today);
            other.Profile.JoinedAt = "2026-09-30";
            PlayerSave merged = SaveMerge.Merge(read, other);
            Assert.That(merged.Profile.Name, Is.EqualTo("Moss"));
            Assert.That(merged.Profile.JoinedAt, Is.EqualTo("2026-09-30"), "the earlier day");

            PlayerSave fresh = PlayerSave.CreateNew("p3", Today);
            Assert.That(SaveSerializer.ToJObject(fresh)["profile"], Is.Null, "no profile until something is set");
        }

        [Test]
        public void TheEditor_BuysThenSaves_AndKeepsFramesForTheWardrobe()
        {
            ProfileService profile = Service(out PlayerSave save, out EconomyService economy);
            var wardrobe = new WardrobeService(save, CosmeticCatalog.Parse(File.ReadAllText(CatalogPath)), new BundledRemoteConfigService(), () => { }, economy);
            save.Cosmetics.Owned.Add("frame.daisy");

            var editor = new ProfileEditor(profile, wardrobe);
            Assert.That(editor.ProfileItemsOpen, Is.False, "frames wait for the Wardrobe");
            Assert.That(editor.Owned(CosmeticKind.Frame), Is.Empty);

            editor.PickAvatar("avatar.bloom_ribbon_plush");
            Assert.That(editor.Action, Is.EqualTo(ProfileAction.Buy));
            Assert.That(editor.Price, Is.EqualTo(300));
            Assert.That(editor.Confirm(), Is.EqualTo(ProfileOutcome.NotEnoughPetals));
            economy.Grant(300, null);
            Assert.That(editor.Confirm(), Is.EqualTo(ProfileOutcome.Bought));
            Assert.That(editor.Action, Is.EqualTo(ProfileAction.Save));

            editor.PickAvatar("avatar.sprig_default");
            Assert.That(editor.EnterName("***"), Is.False);
            Assert.That(editor.EnterName("Sprout"), Is.True);
            Assert.That(profile.Name, Is.Null, "kept only on Save");
            Assert.That(editor.Confirm(), Is.EqualTo(ProfileOutcome.Saved));
            Assert.That(profile.Avatar.Id, Is.EqualTo("avatar.sprig_default"));
            Assert.That(profile.Name, Is.EqualTo("Sprout"));

            save.Unlocks.Flags[WardrobeService.UnlockId] = true;
            save.Cosmetics.Owned.Add("frame.ivy");
            editor = new ProfileEditor(profile, wardrobe, ProfileTab.Frame);
            Assert.That(editor.Owned(CosmeticKind.Frame).Select(i => i.Id), Is.EqualTo(new[] { "frame.daisy", "frame.ivy" }));
            editor.PickFrame("frame.daisy");
            editor.PickFrame("frame.aurora");
            Assert.That(editor.FrameId, Is.EqualTo("frame.daisy"), "not owned: the pick stays");
            Assert.That(editor.Confirm(), Is.EqualTo(ProfileOutcome.Saved));
            Assert.That(wardrobe.Profile.Frame!.Id, Is.EqualTo("frame.daisy"));
        }

        private static ProfileService Service(out PlayerSave save, out EconomyService economy)
        {
            save = PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", Today);
            economy = new EconomyService(save, EconomyConfig.Bundled, () => { });
            return new ProfileService(save, new BundledRemoteConfigService(), new Clock(Today), () => { }, economy);
        }

        /// <summary>The width and height in a JPEG's first start-of-frame marker.</summary>
        private static (int Width, int Height) JpegSize(byte[] bytes)
        {
            int i = 2;
            while (i + 9 < bytes.Length)
            {
                if (bytes[i] != 0xFF)
                {
                    i++;
                    continue;
                }

                byte marker = bytes[i + 1];
                int length = (bytes[i + 2] << 8) | bytes[i + 3];
                if (marker >= 0xC0 && marker <= 0xC2)
                {
                    return ((bytes[i + 7] << 8) | bytes[i + 8], (bytes[i + 5] << 8) | bytes[i + 6]);
                }

                i += 2 + length;
            }

            return (0, 0);
        }

        private sealed class Clock : IClock
        {
            public Clock(DateTime now) => UtcNow = now;

            public DateTime UtcNow { get; }

            public DateTime UtcToday => UtcNow.Date;
        }
    }
}
