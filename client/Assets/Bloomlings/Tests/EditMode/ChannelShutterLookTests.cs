using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Drop water channel and the Twig wooden shutters as both builds show them (rules 17 §8, spec 006 FR-069, FR-070; the
    /// owner's looks A and B of 2026-11-05): the animator follows <c>channelReady</c>, <c>channelOpen</c> and
    /// <c>shutterOpen</c>, a channel rider's walk lies between its rides, the tag shows the rules' count, the exits' touch
    /// targets, and the pictures are deterministic.
    /// </summary>
    public class ChannelShutterLookTests
    {
        // The engine's channel toy (tools/rounds/test/channel-shutters.test.mjs channelToy): the spring at picture (4, 4) inside
        // the B cubes, exit 0 in the G pocket at (1, 1), exit 1 in the R pocket at (7, 1); rows top first, the gate below.
        private static readonly string[] ChannelRowsTop =
        {
            "#############",
            "#...........#",
            "#.GGGGGRRRR.#",
            "#.G.GGGRR.R.#",
            "#.GGGGGRRRR.#",
            "#.BBBBBBBBB.#",
            "#.BBBB.BBBB.#",
            "#.BBBBBBBBB.#",
            "#...........#",
            "######.######",
        };

        private static int At(string[] rowsTop, int x, int y) => ((rowsTop.Length - 1 - (y + 2)) * rowsTop[0].Length) + x + 2;

        private static RoundLevel ChannelToy()
        {
            string[] r = ChannelRowsTop;
            int[] Route(params (int X, int Y)[] points) => points.Select(p => At(r, p.X, p.Y)).ToArray();
            var channel = new RoundChannel(At(r, 4, 4), new[]
            {
                new RoundChannelExit(At(r, 1, 1), Route((4, 4), (4, 3), (4, 2), (3, 2), (2, 2), (1, 2), (1, 1)), 600_000),
                new RoundChannelExit(At(r, 7, 1), Route((4, 4), (5, 4), (6, 4), (7, 4), (7, 3), (7, 2), (7, 1)), 800_000),
            });
            return new RoundLevel(1, 13, 10, r.Reverse().ToArray(), new[] { new RoundPod('B', 26), new RoundPod('G', 14), new RoundPod('R', 11) },
                new IReadOnlyList<int>[] { new[] { 0 }, new[] { 1 }, new[] { 2 } }, new[] { 6 }, channel: channel);
        }

        // The engine's shutter toy (shutterToy): an A frame round 10 G cells under a shutter counting 5 units of B.
        private static readonly string[] ShutterRowsTop =
        {
            "###########",
            "#.........#",
            "#.AAAAAAA.#",
            "#.AGGGGGA.#",
            "#.AGGGGGA.#",
            "#.AAAAAAA.#",
            "#.BBBBBBB.#",
            "#.........#",
            "#####.#####",
        };

        private static RoundLevel ShutterToy()
        {
            string[] r = ShutterRowsTop;
            var cells = new List<int>();
            for (int y = 1; y <= 2; y++)
            {
                for (int x = 1; x <= 5; x++)
                {
                    cells.Add(At(r, x, y));
                }
            }

            cells.Sort();
            return new RoundLevel(1, 11, 9, r.Reverse().ToArray(), new[] { new RoundPod('A', 18), new RoundPod('B', 7), new RoundPod('G', 10) },
                new IReadOnlyList<int>[] { new[] { 0 }, new[] { 1 }, new[] { 2 } }, new[] { 5 }, shutters: new[] { new RoundShutter(cells, 'B', 5) });
        }

        [Test]
        public void TheChannel_CallsOnceTheSpringIsReached_TheChoiceIsShown_AndARiderRidesOutsideItsWalk()
        {
            RoundLevel level = ChannelToy();
            var play = new RoundPlay(level);
            RoundAnimator a = play.Animator;
            Assert.That(a.HasChannel && !a.ChannelReady && a.ChannelExit == -1, Is.True);
            Assert.That(ChannelLook.Pose(a).Calling, Is.False);
            play.Commit(RoundAction.Tap(0));
            for (int k = 0; k < 20_000 && !a.ChannelReady; k++)
            {
                play.Advance(1.0 / 60.0, 1);
            }

            Assert.That(a.ChannelReady, Is.True, "the walkers reach the spring");
            Assert.That(RoundRules.ChannelChoices(play.Timeline.State), Is.EqualTo(new[] { 0, 1 }));
            ChannelPose calling = ChannelLook.Pose(a);
            Assert.That(calling.Calling && calling.Chosen == -1 && calling.ExitAlpha(0) == 1f && calling.DotsAlpha(1) == 1f, Is.True);

            Assert.That(play.Commit(RoundAction.ChooseExit(0)).Accepted, Is.True);
            Assert.That(a.ChannelExit, Is.EqualTo(0), "the choice shows at its event");
            Assert.That(ChannelLook.Pose(a).Calling, Is.False);
            play.Commit(RoundAction.Tap(1));
            RoundWalk rider = a.Walkers.First(w => w.RideUs > 0);
            Assert.That(rider.RideUs, Is.EqualTo(600_000));
            Assert.That(rider.Route[0], Is.EqualTo(level.Channel!.Exits[0].Cell), "a rider walks from the chosen exit");
            RoundCourier courier = play.Timeline.Couriers.First(c => c.Id == rider.Id);
            Assert.That(rider.Start, Is.EqualTo(courier.Start + courier.RideUs + play.IdleUs), "its walk starts after its ride out");
            Assert.That(rider.Done, Is.EqualTo(courier.Done - courier.RideUs + play.IdleUs), "and is home before its ride back");

            RoundBoardLayout board = RoundBoardLayout.Fit(new Box(0f, 0f, 1000f, 1000f), level);
            ChannelLook look = ChannelLook.For(board, level.Channel)!;
            Assert.That(look.Rider(rider, rider.Start - (rider.RideUs / 2), out Box bubble), Is.True);
            (float sx, float sy) = look.SpringPoint;
            (float ex, float ey) = look.ExitPoint(0);
            Assert.That(bubble.CenterY > Math.Min(sy, ey) - board.Cell && bubble.CenterY < Math.Max(sy, ey) + board.Cell, Is.True, "half way along the route");
            Assert.That(look.Rider(rider, rider.Start + 1, out _), Is.False, "walking, not riding");
            Assert.That(look.Rider(rider, RoundAnimator.HomeUs(rider) + 1, out _), Is.True, "riding back once home");
            Assert.That(RoundAnimator.ShownUntilUs(rider), Is.EqualTo(RoundAnimator.HomeUs(rider) + rider.RideUs));
            play.Commit(RoundAction.Tap(2));

            for (int k = 0; k < 100_000 && (play.Timeline.Pending || a.Busy); k++)
            {
                play.Advance(1.0 / 30.0, 2);
            }

            Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(a.ShowsRulesState(play.Timeline, out string difference), Is.True, difference);
            ChannelPose done = ChannelLook.Pose(a);
            Assert.That(done.Chosen == 0 && done.Run == 1f && done.ExitAlpha(1) == 0f && done.DotsAlpha(1) == 0f, Is.True, "the other exit and its dots are gone");
        }

        [Test]
        public void AnExit_TakesATapOfAtLeastTheTouchMinimum_RoundItsWell()
        {
            RoundLevel level = ChannelToy();
            RoundBoardLayout board = RoundBoardLayout.Fit(new Box(0f, 0f, 400f, 400f), level);
            ChannelLook look = ChannelLook.For(board, level.Channel)!;
            const float min = 96f;
            for (int e = 0; e < 2; e++)
            {
                Box touch = look.Touch(e, min);
                Assert.That(touch.Width >= min && touch.Height >= min, Is.True);
                (float x, float y) = look.ExitPoint(e);
                Assert.That(look.ExitAt(x, y, min), Is.EqualTo(e));
            }

            Assert.That(look.ExitAt(-1000f, -1000f, min), Is.EqualTo(-1));
            Assert.That(ChannelLook.For(board, null), Is.Null);
        }

        [Test]
        public void TheTag_ShowsTheRulesCount_AndTheShutterOpensAtItsEvent()
        {
            var play = new RoundPlay(ShutterToy());
            RoundAnimator a = play.Animator;
            Assert.That(a.ShutterCount, Is.EqualTo(1));
            Assert.That(a.ShutterLeft(0), Is.EqualTo(5));
            Assert.That(a.ShutterClosed(0) && a.ShutterShown(0), Is.True);
            play.Commit(RoundAction.Tap(1));
            play.Commit(RoundAction.Tap(0));
            play.Commit(RoundAction.Tap(2));
            bool opened = false;
            for (int k = 0; k < 100_000 && (play.Timeline.Pending || a.Busy); k++)
            {
                foreach (RoundEvent e in play.Advance(1.0 / 60.0, 1))
                {
                    if (e.Type == RoundEventType.ShutterOpen)
                    {
                        opened = true;
                        Assert.That(a.ShutterClosed(0), Is.False, "open at its event");
                        Assert.That(a.ShutterOpenSince(0), Is.GreaterThanOrEqualTo(0f));
                    }
                }

                RoundState s = play.Timeline.State;
                if (s.Shut![0])
                {
                    Assert.That(a.ShutterLeft(0), Is.EqualTo(s.ShutterLeft(0)), "the tag shows the rules' count");
                }
            }

            Assert.That(opened, Is.True);
            Assert.That(play.Status, Is.EqualTo(RoundStatus.Won));
            Assert.That(a.ShowsRulesState(play.Timeline, out string difference), Is.True, difference);
        }

        [Test]
        public void TheLid_StandsOverItsRectangle_DrawnAfterItsBottomRow_AndItsSlatsPopOff()
        {
            RoundLevel level = ShutterToy();
            RoundBoardLayout board = RoundBoardLayout.Fit(new Box(0f, 0f, 1000f, 1000f), level);
            ShutterLook look = ShutterLook.For(board, level, 58f).Single();
            Assert.That(look.BottomRow, Is.EqualTo(level.Shutters[0].Cells.Min() / level.Width));
            Assert.That(look.Covers(level.Shutters[0].Cells[3]) && !look.Covers(At(ShutterRowsTop, 0, 0)), Is.True);
            Assert.That(look.Tag.Width, Is.GreaterThanOrEqualTo(58f * 0.95f - 0.01f));
            Assert.That(look.Slat(0, 0f).Alpha, Is.EqualTo(1f));
            Assert.That(look.Slat(0, 0.6f).Alpha, Is.LessThan(0.01f), "the top slat is off by 0.6 of the opening");
            Assert.That(look.Slat(0, 0.3f).Box.CenterY, Is.LessThan(look.Slat(0, 0f).Box.CenterY), "it lifts");
            Assert.That(ShutterLook.TagAlpha(0.5f), Is.EqualTo(0f));
            Assert.That(ShutterLook.TagScale(0.1f, 0f), Is.GreaterThan(1f), "it pops at a counted cube");
        }

        [Test]
        public void ThePictures_AreTheSameEveryTime_AndAStreamPieceIsPartOfTheWhole()
        {
            RoundLevel level = ChannelToy();
            RoundBoardLayout board = RoundBoardLayout.Fit(new Box(0f, 0f, 600f, 600f), level);
            ChannelLook look = ChannelLook.For(board, level.Channel)!;
            (Box box, string key, Func<int, int, byte[]> render) = look.Stream(0, 0, look.Bands(0) - 1);
            int w = (int)Math.Ceiling(box.Width);
            int h = (int)Math.Ceiling(box.Height);
            byte[] whole = render(w, h);
            Assert.That(render(w, h), Is.EqualTo(whole));
            Assert.That(whole.Where((b, i) => i % 4 == 3).Any(b => b > 0), Is.True, "the stream is drawn");
            (Box _, string pieceKey, Func<int, int, byte[]> piece) = look.Stream(0, 2, 2);
            Assert.That(pieceKey, Is.Not.EqualTo(key));
            Assert.That(piece(32, 48).Where((b, i) => i % 4 == 3).Any(b => b > 0), Is.True);
            Assert.That(UiRaster.ChannelSpring(64, 64, true, 3), Is.EqualTo(UiRaster.ChannelSpring(64, 64, true, 3)));
            Assert.That(UiRaster.ChannelSpring(64, 64, true, 3), Is.Not.EqualTo(UiRaster.ChannelSpring(64, 64, false, 0)));
            Assert.That(UiRaster.ShutterBase(80, 60, 4f, 2f, 0.44f).Length, Is.EqualTo(80 * 60 * 4));
            Assert.That(UiRaster.ShutterBatten(16, 64), Is.EqualTo(UiRaster.ShutterBatten(16, 64)));
            Assert.That(AssetSlots.Has(ChannelLook.Slot) && AssetSlots.Has(ShutterLook.Slot), Is.True);
        }
    }
}
