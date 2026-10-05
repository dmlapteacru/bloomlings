using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Services.Config;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity event timeline's schedule (<see cref="TimelinePlayer"/>, behind <see cref="EventTimeline"/>; the owner's
    /// report of 2026-10-03): the waves of one tap play one after another, the waves of different taps side by side, a
    /// wave waits only for the cells, specials and pods it depends on, and the clearing pace is halved (and halved again on
    /// 2026-10-05).
    /// </summary>
    public class EventTimelineTests
    {
        private const float Tolerance = 0.0005f;

        [Test]
        public void ClearingPace_IsHalvedTwice_AndTheSpeedUpStartsBeyondTwelveSeconds()
        {
            // The owner's requests of 2026-10-03 and 2026-10-05: 0.07 s a step, then 0.14 s, now 0.28 s.
            Assert.That(EventTimeline.StepSeconds, Is.EqualTo(0.28f));
            Assert.That(EventTimeline.MinWaveSeconds, Is.EqualTo(1.2f));
            Assert.That(EventTimeline.MaxWaveSeconds, Is.EqualTo(5.6f));
            Assert.That(EventTimeline.RestoreSeconds, Is.EqualTo(0.22f), "the restore keeps its time");
            Assert.That(RemoteConfigKeys.FxBacklogThresholdMs.Default, Is.EqualTo(12000));
            Assert.That(RemoteConfigKeys.FxBacklogThresholdMs.Min, Is.EqualTo(2000));
            Assert.That(RemoteConfigKeys.FxBacklogThresholdMs.Max, Is.EqualTo(20000));
            Assert.That(new TimelinePlayer().BacklogThresholdSeconds, Is.EqualTo(12f));
        }

        [Test]
        public void TwoTaps_TheirWavesPlaySideBySide()
        {
            var (player, sink) = Create();
            player.Enqueue(Tap(Clear(1, "a", Column(0, 9)), new PodCompleted(1, "a", 0), new SlotFreed(1, 0)));
            player.Advance(0.03f);
            player.Enqueue(Tap(Clear(1, "b", Column(5, 1)), new PodCompleted(1, "b", 1), new SlotFreed(1, 1)));

            int most = 0;
            for (int i = 0; i < 600 && !player.IsIdle; i++)
            {
                player.Advance(1f / 60f);
                most = System.Math.Max(most, player.Playing);
            }

            Assert.That(most, Is.EqualTo(2), "both taps' waves play at once");
            Assert.That(sink.Walkers.Select(w => w.Start), Is.EqualTo(new[] { 0f, 0.03f }).Within(Tolerance), "the second tap's Bloomlings set off at once");
            float aDone = sink.TimeOf("done a");
            float bDone = sink.TimeOf("done b");
            Assert.That(bDone, Is.LessThan(aDone), "the short wave of the second tap ends first");
            Assert.That(bDone, Is.EqualTo(0.03f + EventTimeline.MinWaveSeconds).Within(Tolerance));
            Assert.That(aDone, Is.EqualTo((10 * EventTimeline.StepSeconds) + EventTimeline.RestoreSeconds).Within(Tolerance));
        }

        [Test]
        public void OneTap_ItsRoundsPlayOneAfterAnother()
        {
            var (player, sink) = Create();
            player.Enqueue(Tap(
                Clear(1, "a", Column(0, 1)),
                Clear(2, "a", Column(1, 4)),
                new PodCompleted(2, "a", 0)));
            player.Advance(100f);

            float firstEnd = EventTimeline.MinWaveSeconds;
            Assert.That(sink.Walkers.Select(w => w.Start), Is.EqualTo(new[] { 0f, firstEnd }).Within(Tolerance), "round 2 starts as round 1 ends");
            Assert.That(sink.TimeOf("arrive a 1,4"), Is.EqualTo(firstEnd + (5 * EventTimeline.StepSeconds)).Within(Tolerance));
            Assert.That(sink.TimeOf("done a"), Is.EqualTo(firstEnd + (5 * EventTimeline.StepSeconds) + EventTimeline.RestoreSeconds).Within(Tolerance));
        }

        [Test]
        public void DependentWave_WaitsOnlyForTheCellItWalksOver()
        {
            var (player, sink) = Create();

            // The first tap clears (3,0) at the end of a 4-cell walk; the second tap's Bloomling walks over (3,0) first.
            player.Enqueue(Tap(Clear(1, "a", Row(0, 0, 3))));
            player.Enqueue(Tap(Clear(1, "b", new[] { new CellPos(3, 0), new CellPos(4, 0) })));
            player.Advance(100f);

            float cleared = sink.TimeOf("arrive a 3,0");
            Assert.That(cleared, Is.EqualTo(4 * EventTimeline.StepSeconds).Within(Tolerance));
            (float start, float travel, _) = sink.Walkers[1];
            float steppedOn = start + (travel / 2f);
            Assert.That(steppedOn, Is.EqualTo(cleared + TimelinePlayer.StepMargin).Within(Tolerance), "it steps on (3,0) just after that tile shows cleared");
            float firstWaveEnd = cleared + EventTimeline.RestoreSeconds;
            Assert.That(start, Is.LessThan(firstWaveEnd), "it does not wait for the whole first wave");
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
            Assert.That(sink.Log.Select(l => l.What), Is.EqualTo(new[] { "arrive a 3,0 layer", "arrive b 3,0 open", "special s 1", "special s 2" }));
        }

        [Test]
        public void Backlog_IsTheTimeUntilTheLastWaveEnds_AndOnlyALongOneSpeedsUp()
        {
            var (player, _) = Create();
            player.Enqueue(Tap(Clear(1, "a", Long(0))));
            player.Enqueue(Tap(Clear(1, "b", Long(5))));
            Assert.That(player.Backlog, Is.EqualTo(EventTimeline.MaxWaveSeconds).Within(Tolerance), "two taps side by side, not one after the other");
            player.Advance(0.01f);
            Assert.That(player.Rate, Is.EqualTo(1f), "under 12 s of backlog the pace stays");

            // One tap's three rounds follow each other (on cells of their own): 3 × 4.7 s.
            float round = (16 * EventTimeline.StepSeconds) + EventTimeline.RestoreSeconds;
            player.Enqueue(Tap(Clear(1, "c", Column(12, 15)), Clear(2, "c", Column(12, 15)), Clear(3, "c", Column(12, 15))));
            Assert.That(player.Backlog, Is.EqualTo(3 * round).Within(Tolerance));
            player.Advance(0.01f);
            Assert.That(player.Rate, Is.EqualTo(3 * round / player.BacklogThresholdSeconds).Within(0.01f), "beyond 12 s it plays faster");
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

        private static (TimelinePlayer Player, Recorder Sink) Create()
        {
            var player = new TimelinePlayer();
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
