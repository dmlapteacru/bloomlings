using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The profile page and its edit card fit every phone from 16:9 to 21:9 (spec 005 FR-037).</summary>
    public class ProfileLayoutTests
    {
        private static IEnumerable<(float W, float H, Insets Insets)> Phones()
        {
            foreach (float ratio in new[] { 16f / 9f, 2f, 19.5f / 9f, 20f / 9f, 21f / 9f })
            {
                yield return (1080f, 1080f * ratio, new Insets(ratio > 2f ? 110f : 60f, ratio > 2f ? 60f : 0f));
                yield return (720f, 720f * ratio, Insets.None);
            }
        }

        private static bool Inside(Box outer, Box inner) =>
            inner.Left >= outer.Left - 0.5f && inner.Right <= outer.Right + 0.5f && inner.Top >= outer.Top - 0.5f && inner.Bottom <= outer.Bottom + 0.5f;

        [Test]
        public void ThePage_StaysInsideTheSafeArea_WithItsPartsInOrder()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                string at = $"{w}x{h}";
                ReferenceProfileRegions r = ScreenLayout.ReferenceProfile(w, h, insets);
                Assert.That(r.Header, Is.EqualTo(ScreenLayout.ReferenceStore(w, h, insets, false, false).Header), at + ": the pages' header");
                foreach (Box box in new[] { r.Card, r.AchievementsNote }.Concat(r.Stats).Concat(r.Achievements))
                {
                    Assert.That(Inside(r.Safe, box), Is.True, at + ": inside the safe area");
                    Assert.That(Inside(r.Panel, box), Is.True, at + ": on the panel");
                }

                Assert.That(r.Card.Top, Is.GreaterThan(r.Header.Row.Bottom), at);
                foreach (Box box in new[] { r.Avatar, r.Name, r.Edit, r.Id, r.Joined, r.Plaque })
                {
                    Assert.That(Inside(r.Card, box), Is.True, at + ": on the card");
                }

                Assert.That(r.Avatar.Width, Is.EqualTo(r.Avatar.Height).Within(0.01f), at + ": round");
                Assert.That(r.Name.Right, Is.LessThan(r.Edit.Left), at + ": the pencil after the name");
                Assert.That(r.Name.Bottom, Is.LessThanOrEqualTo(r.Id.Top), at);
                Assert.That(r.Id.Bottom, Is.LessThanOrEqualTo(r.Joined.Top), at);
                Assert.That(r.Avatar.Bottom, Is.LessThan(r.Plaque.Top), at);
                Assert.That(r.Card.Bottom, Is.LessThan(r.Stats[0].Top), at);
                Assert.That(r.Stats[0].Bottom, Is.LessThan(r.AchievementsTitle.Top), at);
                Assert.That(r.AchievementsTitle.Bottom, Is.LessThan(r.Achievements[0].Top), at);
                Assert.That(r.Achievements[0].Bottom, Is.LessThanOrEqualTo(r.AchievementsNote.Top), at);
                Assert.That(r.AchievementsNote.Bottom, Is.LessThanOrEqualTo(r.Safe.Bottom - (0.04f * r.W) + 0.5f), at + ": clear of the safe bottom");
                for (int i = 1; i < r.Stats.Count; i++)
                {
                    Assert.That(r.Stats[i - 1].Right, Is.LessThan(r.Stats[i].Left), at + ": stat cells apart");
                }

                for (int i = 1; i < r.Achievements.Count; i++)
                {
                    Assert.That(r.Achievements[i - 1].Right, Is.LessThan(r.Achievements[i].Left), at + ": tiles apart");
                }
            }
        }

        [Test]
        public void TheEditCard_HoldsFourRowsOfFour_AboveItsButton()
        {
            foreach ((float w, float h, Insets insets) in Phones())
            {
                string at = $"{w}x{h}";
                ProfileEditRegions r = ScreenLayout.ProfileEdit(w, h, insets);
                Box body = r.Card.Body;
                float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(w, h);
                Assert.That(Inside(r.Card.Card, body), Is.True, at);
                Assert.That(r.Tabs.Bottom, Is.LessThan(r.Preview.Top), at);
                Assert.That(r.Preview.Bottom, Is.LessThan(r.Grid.Top), at);
                Assert.That(Inside(body, r.Preview) && Inside(body, r.PreviewName) && Inside(body, r.Button), Is.True, at);
                Assert.That(r.CellSize, Is.GreaterThanOrEqualTo(touch * 0.95f), at + ": a cell is a touch target");
                Box last = r.Cell((ProfileEditRegions.Rows * ProfileEditRegions.Columns) - 1);
                Assert.That(Inside(r.Grid, r.Cell(0)) && Inside(r.Grid, last), Is.True, at + ": the grid holds its cells");
                Assert.That(last.Bottom, Is.LessThan(r.Button.Top), at + ": above the button");
                Assert.That(r.Cell(0).Right, Is.LessThan(r.Cell(1).Left), at);
                Assert.That(r.Cell(0).Bottom, Is.LessThan(r.Cell(ProfileEditRegions.Columns).Top), at);
                Assert.That(Inside(r.Grid, r.NameField) && Inside(r.Grid, r.NameButton) && Inside(r.Grid, r.NameHint), Is.True, at + ": the Name tab");
                Assert.That(r.NameButton.Height, Is.GreaterThanOrEqualTo(touch * 0.95f), at);
                Box item = ProfileEditRegions.CellItemAvatar(r.Cell(0));
                Assert.That(Inside(r.Cell(0), AvatarLook.Frame(item)), Is.True, at + ": a drawn frame inside its cell");
            }
        }

        /// <summary>
        /// The owner's requests of 2026-10-06: the avatar's picture fills it, the avatar is a rounded square in the icon
        /// buttons' wood rim, and its border is one ("there is a default border, a thick one, and a chosen frame takes its
        /// place"): with no frame the picture fills the rim's inside, with one the picture grows to half the rim and the
        /// frame's band lies where the rim's middle was; no lip under the picture.
        /// </summary>
        [Test]
        public void TheAvatarsBorder_IsTheWoodRim_OrTheFrameInItsPlace_AndThePictureFillsTheRest()
        {
            var avatar = new Box(100f, 200f, 240f, 340f);
            Box rim = AvatarLook.Rim(avatar);
            Box disc = AvatarLook.Disc(avatar);
            Box framed = AvatarLook.Disc(avatar, framed: true);
            float ring = AvatarLook.Ring(disc, 2f);
            Box picture = AvatarLook.Picture(disc, ring);
            Assert.That(rim.Width, Is.EqualTo(140f).Within(0.01f), "the rim fills the avatar's square");
            Assert.That(disc.Left - rim.Left, Is.EqualTo(140f * GardenLook.IconRimShare).Within(0.01f), "inside a rim as thick as the icon buttons'");
            Assert.That(disc.Width, Is.EqualTo(disc.Height).Within(0.01f), "a square");
            Assert.That(disc.CenterY, Is.EqualTo(rim.CenterY).Within(0.01f), "centered: no lip under it");
            Assert.That(disc.Bottom, Is.EqualTo(rim.Bottom - (140f * GardenLook.IconRimShare)).Within(0.01f), "it fills the rim's inside to the bottom");
            Assert.That(AvatarLook.DiscRadius(avatar), Is.EqualTo(AvatarLook.RimRadius(avatar) - (disc.Left - rim.Left)).Within(0.01f), "its corners follow the rim's");
            Assert.That(AvatarLook.DiscRadiusOf(disc), Is.EqualTo(AvatarLook.DiscRadius(avatar)).Within(0.01f), "from its own box too");
            Assert.That(AvatarLook.DiscRadius(avatar), Is.LessThan(disc.Width / 2f), "a rounded square, not a circle");
            Assert.That(picture.Width, Is.EqualTo(disc.Width - (2f * ring)).Within(0.01f), "no gap: only the thin ring");
            Assert.That(ring / disc.Width, Is.LessThanOrEqualTo(0.03f), "a thin ring");

            // With a frame: the picture grows to half the rim, and the frame's band lies on its edge, the rim's middle.
            Assert.That(framed.Width, Is.GreaterThan(disc.Width), "a framed picture is larger");
            Assert.That(framed.Left - rim.Left, Is.EqualTo(140f * GardenLook.IconRimShare / 2f).Within(0.01f));
            Assert.That(AvatarLook.DiscRadiusOf(framed, framed: true), Is.EqualTo(AvatarLook.DiscRadius(avatar, framed: true)).Within(0.01f));
            Box frame = AvatarLook.Frame(avatar);
            Assert.That(frame.CenterX, Is.EqualTo(rim.CenterX).Within(0.01f));
            Assert.That(frame.CenterY, Is.EqualTo(rim.CenterY).Within(0.01f));
            Assert.That(AvatarLook.FrameEdge * frame.Width, Is.EqualTo(framed.Width / 2f).Within(0.01f), "the frames' bands lie on the framed picture's edge");
            Assert.That(AvatarLook.FrameCorner * frame.Width, Is.EqualTo(AvatarLook.DiscRadius(avatar, framed: true)).Within(0.01f), "with its corners");
            Assert.That(AvatarLook.ShapeFrame(avatar).Width, Is.EqualTo(frame.Width).Within(0.01f), "the plain frame in the same place");
        }
    }
}
