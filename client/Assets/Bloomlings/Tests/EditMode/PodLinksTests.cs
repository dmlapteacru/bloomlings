using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests.EditMode
{
    /// <summary>
    /// The connected pods' marks (spec 005 FR-043; the owner's Level 54 of 2026-10-08: a tap on the flower pod on top of the
    /// third column said "Take the top pod first", because its partner waited under the fifth column's "+2", and no mark
    /// showed which pod it was linked to).
    /// </summary>
    public sealed class PodLinksTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        private static LevelDefinition Level54 => DefinitionJson.Read(File.ReadAllText(Path.Combine(Root, "content", "catalog", "levels", "level-00054.json")));

        private static LevelSession Load(LevelDefinition definition)
        {
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(Root, "content", "pictures", "lib", definition.Picture.Id + ".json")));
            return LevelSession.Load(definition, picture, new SessionOptions(0, 20000));
        }

        /// <summary>
        /// Level 54 with its pair one pod down (both columns turned so the members start at depth 1, as a group must), then
        /// the third column's top pod tapped away: its member is on top, its partner one pod down in the fifth column.
        /// </summary>
        private static LevelSession MemberOnTop()
        {
            LevelDefinition level = Level54;
            var stacks = level.Tray.Stacks.Select(s => (IReadOnlyList<string>)s.ToList()).ToList();
            foreach ((int stack, string pod) in new[] { (2, "p33"), (4, "p35") })
            {
                List<string> ids = stacks[stack].ToList();
                int at = ids.IndexOf(pod) - 1;
                stacks[stack] = ids.Skip(at).Concat(ids.Take(at)).ToList();
            }

            LevelSession session = Load(level with { Tray = new TrayDef(stacks) });
            Assert.That(session.Apply(new TapPod(session.View.Stack(2)[0])).Accepted, Is.True);
            return session;
        }

        [Test]
        public void Level54_HasAPairApart_EachUnderItsColumnsPlusN()
        {
            LevelView view = Load(Level54).View;
            Assert.That(view.Pod("p33").ConnectedGroupId, Is.EqualTo("c1"));
            Assert.That(PodLinks.Place(view, "p33"), Is.EqualTo(((int, int)?)(2, 6)));
            Assert.That(PodLinks.Place(view, "p35"), Is.EqualTo(((int, int)?)(4, 6)));
            Assert.That(PodLinks.Buried(view, "p33"), Is.EqualTo(new[] { "p35" }));
            Assert.That(PodLinks.WaitsForPartner(view, "p33"), Is.False, "the pod itself is not on top yet");
            Assert.That(PodLinks.Buried(view, "p01"), Is.Empty, "not connected");

            // Both members wait deeper than three rows: each column's "+N" carries the small link badge.
            Assert.That(PodLinks.Hidden(view, 3), Is.EqualTo(new[] { null, null, "c1", null, "c1" }));
            Assert.That(PodLinks.Hidden(view, 7), Is.EqualTo(new string?[5]), "every member shown");
        }

        [Test]
        public void ATapOnTheMemberOnTop_WaitsForItsPartner_AndSaysSo()
        {
            LevelSession session = MemberOnTop();
            LevelView view = session.View;
            Assert.That(view.IsExposed("p33"), Is.True);
            Assert.That(PodLinks.Place(view, "p35"), Is.EqualTo(((int, int)?)(4, 1)));
            CommandCheck check = session.Check(new TapPod("p33"));
            Assert.That(check.IsAllowed, Is.False);
            Assert.That(check.Reason, Is.EqualTo(RejectReason.NotExposed), "the rule is unchanged (spec 001 FR-035)");
            Assert.That(PodLinks.WaitsForPartner(view, "p33"), Is.True);
            Assert.That(PodLinks.Buried(view, "p33"), Is.EqualTo(new[] { "p35" }));
            Assert.That(PodLinks.WaitsForPartner(view, "p04"), Is.False, "a pod that is not connected never waits");
            Assert.That(PodLinks.Hidden(view, 3), Is.EqualTo(new string?[5]), "both members shown");
            Assert.That(PodLinks.Hidden(view, 1)[4], Is.EqualTo("c1"), "with one row shown, the partner waits under the +N");
        }

        [Test]
        public void TheGroups_TakeThePaletteInTheirOrder()
        {
            LevelView view = Load(Level54).View;
            Assert.That(PodLinks.Groups(view), Is.EqualTo(new[] { "c1" }));
            Assert.That(PodLinks.ColorOf(view, "c1"), Is.EqualTo(DesignTokens.Colors.StateLink));
        }

        [Test]
        public void TheHint_PulsesTwice_ThenStops()
        {
            Assert.That(PodLinks.Hint(-0.1f), Is.Zero);
            Assert.That(PodLinks.Hint(0f), Is.Zero.Within(1e-4));
            float quarter = PodLinks.HintSeconds / (2f * PodLinks.HintPulses);
            Assert.That(PodLinks.Hint(quarter), Is.EqualTo(1f).Within(1e-4), "the top of the first pulse");
            Assert.That(PodLinks.Hint(quarter * 2f), Is.Zero.Within(1e-4), "between the pulses");
            Assert.That(PodLinks.Hint(quarter * 3f), Is.EqualTo(1f).Within(1e-4), "the top of the second pulse");
            Assert.That(PodLinks.Hint(PodLinks.HintSeconds), Is.Zero);
        }

        [Test]
        public void TheBadges_SitAtTheFramesTopRight_AndAtThePlusN()
        {
            PodChip chip = PodChip.In(new Box(0f, 0f, 200f, 100f));
            Box link = chip.Link;
            Assert.That(link.CenterX, Is.GreaterThan(chip.Frame.CenterX), "the right side: the +N takes the left");
            Assert.That(link.CenterY, Is.LessThan(chip.Frame.CenterY), "the top: the count takes the bottom right");
            Assert.That(link.Height, Is.EqualTo(chip.Frame.Height * PodLinks.PictureShare).Within(1e-3));
            Box hidden = PodLinks.HiddenMark(chip);
            Assert.That(hidden.Height, Is.LessThan(link.Height));
            Assert.That(hidden.Left, Is.GreaterThan(chip.Badge.CenterX + (chip.Badge.Height * 0.4f)), "clear of the +N's digits");
            Assert.That(hidden.Left, Is.LessThan(chip.Badge.CenterX + (chip.Badge.Height * 0.63f)), "over the badge's ring");
        }

        [Test]
        public void TheLinkBadge_IsADiscWithAWhiteChain_AndTheSameEveryTime()
        {
            const int size = 64;
            byte[] badge = UiRaster.LinkBadge(size, DesignTokens.Colors.StateLink);
            Assert.That(badge.Length, Is.EqualTo(size * size * 4));
            Assert.That(badge, Is.EqualTo(UiRaster.LinkBadge(size, DesignTokens.Colors.StateLink)), "deterministic");
            Assert.That(badge[3], Is.Zero, "the corners are clear");

            // A white chain over the face (besides the white ring), and the link color beside the chain.
            int Mid(int x, int y) => ((y * size) + x) * 4;
            int white = 0;
            for (int y = size / 4; y < size * 3 / 4; y++)
            {
                for (int x = size / 4; x < size * 3 / 4; x++)
                {
                    int i = Mid(x, y);
                    white += badge[i] > 230 && badge[i + 1] > 230 && badge[i + 2] > 230 && badge[i + 3] == 255 ? 1 : 0;
                }
            }

            Assert.That(white, Is.GreaterThan(size * size / 4 / 10), "the chain's links");
            int face = Mid((int)(size * 0.32f), (int)(size * 0.32f));
            Assert.That(badge[face + 3], Is.EqualTo(255));
            Assert.That(badge[face + 1], Is.GreaterThan(badge[face]), "teal: more green than red");
            Assert.That(UiRaster.LinkBadge(size, DesignTokens.Colors.StateLink3), Is.Not.EqualTo(badge), "each group its color");
        }
    }
}
