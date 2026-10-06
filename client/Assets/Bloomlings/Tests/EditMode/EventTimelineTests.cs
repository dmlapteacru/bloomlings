using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity event timeline's schedule (<see cref="TimelinePlayer"/>, behind <see cref="EventTimeline"/>; the owner's
    /// reports of 2026-10-03 and 2026-10-04, and the calm pace of 2026-10-06): the waves of every tap and every round play
    /// side by side, a walker waits only for the cells it walks over and its line out of the arch, every trip takes the
    /// clearing styles' time, and the end events keep the rules' order.
    /// </summary>
    public class EventTimelineTests
    {
        private const float Tolerance = 0.0005f;

        private static float Trip(int cells) => ClearStyles.TripSeconds(cells);

        [Test]
        public void ClearingPace_IsTheStylesTripTime_AndTheSpeedUpWaitsForAMinute()
        {
            Assert.That(EventTimeline.MinWaveSeconds, Is.EqualTo(1.2f));
            Assert.That(EventTimeline.MaxWaveSeconds, Is.EqualTo(40f), "no trip is squeezed into its wave");
            Assert.That(EventTimeline.RestoreSeconds, Is.EqualTo(0.22f), "the restore keeps its time");
            Assert.That(RemoteConfigKeys.FxBacklogThresholdMs.Default, Is.EqualTo(60000));
            Assert.That(RemoteConfigKeys.FxBacklogThresholdMs.Min, Is.EqualTo(2000));
            Assert.That(RemoteConfigKeys.FxBacklogThresholdMs.Max, Is.EqualTo(120000));
            Assert.That(new TimelinePlayer().BacklogThresholdSeconds, Is.EqualTo(60f));
        }

        [Test]
        public void TwoTaps_TheirWavesPlaySideBySide()
        {
            var (player, sink) = Create();
            player.Enqueue(Tap(Clear(1, "a", Column(0, 9)), new PodCompleted(1, "a", 0), new SlotFreed(1, 0)));
            player.Advance(0.03f);
            player.Enqueue(Tap(Clear(1, "b", Column(5, 1)), new PodCompleted(1, "b", 1), new SlotFreed(1, 1)));

            int most = 0;
            for (int i = 0; i < 3000 && !player.IsIdle; i++)
            {
                player.Advance(1f / 60f);
                most = System.Math.Max(most, player.Playing);
            }

            Assert.That(most, Is.EqualTo(2), "both taps' waves play at once");
            Assert.That(sink.Walkers.Select(w => w.Start), Is.EqualTo(new[] { 0f, 0.03f }).Within(Tolerance), "the second tap's Bloomlings set off at once");
            float aDone = sink.TimeOf("done a");
            float bDone = sink.TimeOf("done b");
            Assert.That(bDone, Is.LessThan(aDone), "the short wave of the second tap ends first");
            Assert.That(bDone, Is.EqualTo(0.03f + Trip(2) + EventTimeline.RestoreSeconds).Within(Tolerance));
            Assert.That(aDone, Is.EqualTo(Trip(10) + EventTimeline.RestoreSeconds).Within(Tolerance));
        }

        [Test]
        public void OneTap_ItsRoundsDoNotWaitForEachOther_ButEndInTheRulesOrder()
        {
            var (player, sink) = Create();
            player.Enqueue(Tap(
                Clear(1, "a", Column(0, 3)),
                new SpecialProgressed(1, "s", 1, 2),
                Clear(2, "a", Column(5, 1)),
                new SpecialProgressed(2, "s", 2, 2),
                new PodCompleted(2, "a", 0)));
            player.Advance(100f);

            Assert.That(sink.Walkers.Select(w => w.Start), Is.EqualTo(new[] { 0f, 0f }).Within(Tolerance), "round 2 sets off at once: its way is clear");
            Assert.That(sink.TimeOf("arrive a 5,1"), Is.LessThan(sink.TimeOf("arrive a 0,3")), "its short trip arrives first");
            Assert.That(sink.TimeOf("special s 2"), Is.GreaterThan(sink.TimeOf("special s 1")), "the rounds end in the rules' order");
            Assert.That(sink.Log.Last().What, Is.EqualTo("done a"));
        }

        [Test]
        public void APodsBloomlings_LeaveTheArchInALine_WhilePodsWorkSideBySide()
        {
            var (player, sink) = Create();
            player.Enqueue(Tap(
                Clear(1, "a", new[] { new CellPos(2, 0), new CellPos(2, 1) }),
                Clear(1, "a", new[] { new CellPos(2, 0), new CellPos(1, 0) }),
                Clear(1, "a", new[] { new CellPos(2, 0), new CellPos(1, 1) }),
                Clear(2, "a", new[] { new CellPos(2, 0), new CellPos(1, 0), new CellPos(0, 0) })));
            player.Enqueue(Tap(Clear(1, "b", new[] { new CellPos(2, 0), new CellPos(3, 0), new CellPos(4, 0) })));
            player.Advance(0.01f);

            float gap = ClearStyles.LineGap;
            Assert.That(sink.Walkers.Take(3).Select(w => w.Start), Is.EqualTo(new[] { 0f, gap, 2 * gap }).Within(Tolerance), "one after another out of the arch");
            Assert.That(sink.Walkers.First(w => w.Batch[0].Clear.PodId == "b").Start, Is.EqualTo(0f).Within(Tolerance), "another pod works at the same time, in a line of its own");
            player.Advance(100f);
            Assert.That(sink.Walkers.First(w => w.Batch[0].Clear.Cell == new CellPos(0, 0)).Start, Is.GreaterThanOrEqualTo((3 * gap) - Tolerance), "the pod's next round joins its line");
        }

        [Test]
        public void DependentWave_WaitsOnlyForTheCellItWalksOver()
        {
            var (player, sink) = Create();

            // The first tap clears (3,0) at the end of a 4-cell trip; the second tap's Bloomling walks over (3,0) first.
            player.Enqueue(Tap(Clear(1, "a", Row(0, 0, 3))));
            player.Enqueue(Tap(Clear(1, "b", new[] { new CellPos(3, 0), new CellPos(4, 0) })));
            player.Advance(100f);

            float cleared = sink.TimeOf("arrive a 3,0");
            Assert.That(cleared, Is.EqualTo(Trip(4)).Within(Tolerance));
            (float start, _, _) = sink.Walkers[1];
            float steppedOn = start + (ClearStyles.LegsOf(ClearStyle.Blossom, 2).Out / 2f);
            Assert.That(steppedOn, Is.EqualTo(cleared + TimelinePlayer.StepMargin).Within(Tolerance), "it steps on (3,0) just after that tile shows cleared");
            Assert.That(start, Is.LessThan(cleared + EventTimeline.RestoreSeconds), "it does not wait for the whole first wave");
        }

        [Test]
        public void AWalker_CrossesACellOnceItsTileIsGone_BeforeItsClear()
        {
            // Munchers: the first Bloomling eats (3,0) and walks home with it; the next may cross (3,0) once it is eaten.
            var (player, sink) = Create(ClearStyle.Munchers);
            player.Enqueue(Tap(Clear(1, "a", new[] { new CellPos(3, 0) }), Clear(2, "a", new[] { new CellPos(3, 0), new CellPos(3, 1) })));
            player.Advance(100f);

            ClearLegs first = ClearStyles.LegsOf(ClearStyle.Munchers, 1);
            float eaten = first.Out + first.Act;
            float start = sink.Walkers[1].Start;
            float steppedOn = start + (ClearStyles.LegsOf(ClearStyle.Munchers, 2).Out / 2f);
            Assert.That(steppedOn, Is.EqualTo(eaten + TimelinePlayer.StepMargin).Within(Tolerance));
            Assert.That(steppedOn, Is.LessThan(sink.TimeOf("arrive a 3,0")), "before the first Bloomling is home");
        }

        [Test]
        public void ALayerUnderATile_KeepsItsCellUntilTheClear()
        {
            var (player, sink) = Create(ClearStyle.Munchers);
            var cell = new CellPos(3, 0);
            player.Enqueue(new GameEvent[] { Clear(1, "a", new[] { cell }), new LayerRevealed(1, cell, VariantId.Moss) });
            player.Enqueue(Tap(Clear(1, "b", new[] { cell })));
            player.Advance(100f);

            float revealed = sink.TimeOf("arrive a 3,0 layer");
            Assert.That(sink.Walkers[1].Start + ClearStyles.LegsOf(ClearStyle.Munchers, 1).Out, Is.EqualTo(revealed + TimelinePlayer.StepMargin).Within(Tolerance), "the new layer's Bloomling reaches it once it shows");
        }

        [Test]
        public void EachWalker_SetsOffWhenItsOwnRouteIsClear()
        {
            var (player, sink) = Create();

            // The owner's L1 (2026-10-04): the first tap clears (3,0) at once and (3,4) at the end of a long, winding walk;
            // the second tap's wave has a Bloomling for (3,1), just past (3,0), and one for (3,5), behind (3,4).
            player.Enqueue(Tap(Clear(1, "a", new[] { new CellPos(3, 0) }), Clear(1, "a", Column(0, 4).Concat(Row(4, 1, 3)).ToArray())));
            player.Enqueue(Tap(Clear(1, "b", new[] { new CellPos(3, 0), new CellPos(3, 1) }), Clear(1, "b", Column(3, 5))));
            player.Advance(100f);

            float near = sink.Walkers.First(w => w.Batch[0].Clear.Cell == new CellPos(3, 1)).Start;
            float far = sink.Walkers.First(w => w.Batch[0].Clear.Cell == new CellPos(3, 5)).Start;
            Assert.That(near, Is.LessThan(sink.TimeOf("arrive a 3,4")), "the near Bloomling does not wait for the far tile");
            Assert.That(far, Is.GreaterThan(near), "the far one sets off when its route is clear");
        }

        [Test]
        public void SpecialAndPodEvents_KeepTheRulesOrder()
        {
            var (player, sink) = Create();

            // The first tap's long wave moves the special to 1 of 3; the second tap's short wave moves it to 2 and
            // finishes pod "a", whose first Bloomling still walks in the first wave.
            player.Enqueue(Tap(Clear(1, "a", Column(0, 12)), new SpecialProgressed(1, "s", 1, 3)));
            player.Enqueue(Tap(Clear(1, "a", Column(4, 1)), new SpecialProgressed(1, "s", 2, 3), new PodCompleted(1, "a", 0)));
            player.Advance(100f);

            float first = sink.TimeOf("special s 1");
            float second = sink.TimeOf("special s 2");
            Assert.That(second, Is.GreaterThan(first), "the special's progress shows in the rules' order");
            Assert.That(sink.TimeOf("done a"), Is.GreaterThan(sink.TimeOf("arrive a 0,12")), "the pod leaves after its last Bloomling");
            Assert.That(sink.Walkers[1].Start, Is.LessThan(first), "the second wave still plays beside the first");
            Assert.That(sink.Log.Last().What, Is.EqualTo("done a"));
        }

        [Test]
        public void LevelOutcome_WaitsForEveryEarlierWave()
        {
            var (player, sink) = Create();
            player.Enqueue(Tap(Clear(1, "a", Column(0, 12)), new PodCompleted(1, "a", 0)));
            player.Enqueue(Tap(Clear(1, "b", Column(5, 1)), new PodCompleted(1, "b", 1), new LevelWon(2, 0)));
            player.Advance(100f);

            Assert.That(sink.Log.Last().What, Is.EqualTo("won"));
            Assert.That(sink.TimeOf("won"), Is.GreaterThan(sink.TimeOf("done a")));
        }

        [Test]
        public void Flush_PlaysEverythingAtOnce_InTheScheduleOrder_WithoutWalkers()
        {
            var (player, sink) = Create();

            // Two taps clear the same layered tile one after another: the first reveals a layer, the second opens it.
            var cell = new CellPos(3, 0);
            player.Enqueue(new GameEvent[]
            {
                Clear(1, "a", Row(0, 0, 3)), new LayerRevealed(1, cell, VariantId.Moss), new SpecialProgressed(1, "s", 1, 2),
            });
            player.Enqueue(new GameEvent[]
            {
                Clear(1, "b", new[] { cell }), new CellOpened(1, cell), new SpecialProgressed(1, "s", 2, 2),
            });
            float now = player.Now;
            player.Flush();

            Assert.That(player.IsIdle, Is.True);
            Assert.That(player.Now, Is.EqualTo(now), "the clock stays");
            Assert.That(sink.Walkers, Is.Empty, "no Bloomling sets off");
            // The second Bloomling reaches the new layer after it shows and acts there, so the first wave has ended by then.
            Assert.That(sink.Log.Select(l => l.What), Is.EqualTo(new[] { "arrive a 3,0 layer", "special s 1", "arrive b 3,0 open", "special s 2" }));
        }

        [Test]
        public void Backlog_IsTheTimeUntilTheLastWaveEnds_AndOnlyALongOneSpeedsUp()
        {
            var (player, _) = Create();
            player.Enqueue(Tap(Clear(1, "a", Long(0))));
            player.Enqueue(Tap(Clear(1, "b", Long(5))));
            float wave = Trip(20) + EventTimeline.RestoreSeconds;
            Assert.That(player.Backlog, Is.EqualTo(wave).Within(Tolerance), "two taps side by side, not one after the other");
            player.Advance(0.01f);
            Assert.That(player.Rate, Is.EqualTo(1f), "under a minute of backlog the pace stays");

            // With a lower threshold (Remote Config), a long backlog plays faster, up to the cap.
            player.BacklogThresholdSeconds = 12f;
            player.Advance(0.01f);
            Assert.That(player.Rate, Is.EqualTo((wave - 0.02f) / 12f).Within(0.01f));
            Assert.That(player.Rate, Is.LessThanOrEqualTo(EventTimeline.MaxRate));
        }

        [Test]
        public void Walkers_KnowTheirStart_AndArriveAsTheirTileChanges()
        {
            var (player, sink) = Create();
            player.Advance(1f);
            player.Enqueue(Tap(Clear(1, "a", Column(2, 2)), Clear(1, "a", Column(3, 5))));
            player.Advance(100f);

            Assert.That(sink.Walkers.Count, Is.EqualTo(2));
            foreach ((float start, float travel, IReadOnlyList<WorkUnit> batch) in sink.Walkers)
            {
                Assert.That(start, Is.EqualTo(1f), "it sets off as the tap is shown");
                WorkUnit unit = batch[batch.Count - 1];
                Assert.That(sink.TimeOf($"arrive a {unit.Clear.Cell.X},{unit.Clear.Cell.Y}"), Is.EqualTo(start + travel).Within(Tolerance));
            }
        }

        private static (TimelinePlayer Player, Recorder Sink) Create(ClearStyle style = ClearStyle.Blossom)
        {
            var player = new TimelinePlayer { Style = style };
            var sink = new Recorder(player);
            player.Bind(sink);
            return (player, sink);
        }

        private static GameEvent[] Tap(params GameEvent[] events) => events;

        private static TileCleared Clear(int round, string pod, IReadOnlyList<CellPos> route) =>
            new TileCleared(round, route[route.Count - 1], VariantId.Leaf, pod, route);

        /// <summary>A walk up column <paramref name="x"/> from the bottom row to row <paramref name="top"/>.</summary>
        private static CellPos[] Column(int x, int top) => Enumerable.Range(0, top + 1).Select(y => new CellPos(x, y)).ToArray();

        private static CellPos[] Row(int y, int from, int to) => Enumerable.Range(from, to - from + 1).Select(x => new CellPos(x, y)).ToArray();

        /// <summary>A 20-cell walk (up column <paramref name="x"/>, then right along the top row): the longest wave.</summary>
        private static CellPos[] Long(int x) => Column(x, 15).Concat(Row(15, x + 1, x + 4)).ToArray();

        /// <summary>Records every cue with the timeline clock when it came.</summary>
        private sealed class Recorder : ITimelineSink
        {
            private readonly TimelinePlayer _player;

            public Recorder(TimelinePlayer player)
            {
                _player = player;
            }

            public List<(float At, string What)> Log { get; } = new List<(float, string)>();

            public List<(float Start, float Travel, IReadOnlyList<WorkUnit> Batch)> Walkers { get; } = new List<(float, float, IReadOnlyList<WorkUnit>)>();

            public float TimeOf(string what)
            {
                foreach ((float at, string seen) in Log)
                {
                    if (seen == what)
                    {
                        return at;
                    }
                }

                Assert.Fail(what + " was not shown: " + string.Join(", ", Log.Select(l => l.What)));
                return 0f;
            }

            public void OnWorkStarted(IReadOnlyList<WorkUnit> batch, float start, float travelSeconds)
            {
                Assert.That(start, Is.GreaterThanOrEqualTo(_player.Now - Tolerance), "a walker sets off as its wave starts, or later when its route is not clear yet");
                Walkers.Add((start, travelSeconds, batch));
            }

            public void OnWorkArrived(WorkUnit unit)
            {
                string reveal = unit.Reveal switch
                {
                    CellOpened _ => " open",
                    LayerRevealed _ => " layer",
                    _ => string.Empty,
                };
                Log.Add((_player.Now, $"arrive {unit.Clear.PodId} {unit.Clear.Cell.X},{unit.Clear.Cell.Y}{reveal}"));
            }

            public void OnEvent(GameEvent e)
            {
                string what = e switch
                {
                    PodCompleted done => "done " + done.PodId,
                    SpecialProgressed progressed => $"special {progressed.SpecialId} {progressed.Progress}",
                    LevelWon _ => "won",
                    _ => e.GetType().Name,
                };
                Log.Add((_player.Now, what));
            }
        }
    }
}
