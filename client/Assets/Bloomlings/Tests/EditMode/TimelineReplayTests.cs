using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Gameplay.Slots;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Content.Golden;
using Bloomlings.Content.Json;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// Replays every golden case and showcase solution through the Unity client's engine-free presentation (the timeline's
    /// <see cref="TimelinePlayer"/> and the slots' <see cref="SlotPlaces"/>, driven the way <c>GameplayController</c> and
    /// <c>SlotRowView</c> drive them), taps settled one by one and taps in quick succession: with the waves of different
    /// taps side by side, every cell, special and pod still shows its changes in the rules' order, the outcome comes last,
    /// and the slots end on the rules' state (the playtest's <c>playtest/check</c> does the same for its animator).
    /// </summary>
    public class TimelineReplayTests
    {
        private const float LeaveSeconds = 0.22f;

        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void Replays_ShowEveryChangeInTheRulesOrder_AndEndOnTheRulesState()
        {
            List<Run> runs = Runs();
            Assert.That(runs.Count, Is.GreaterThan(40), "golden cases and showcase solutions found under " + Root);
            var problems = new List<string>();
            foreach (Run run in runs)
            {
                foreach (bool rapid in new[] { false, true })
                {
                    foreach (string problem in Replay(run, rapid).Take(3))
                    {
                        problems.Add($"{run.Name} rapid={rapid}: {problem}");
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems.Take(12)));
        }

        [Test]
        public void TwoQuickTaps_ShowOnTwoPlates_AndTheirWavesPlaySideBySide()
        {
            int working = 0;
            int together = 0;
            var problems = new List<string>();
            foreach (Run run in Runs())
            {
                List<TapPod> taps = run.Commands.OfType<TapPod>().Take(2).ToList();
                if (taps.Count < 2)
                {
                    continue;
                }

                var replay = new Presentation(LevelSession.Load(run.Level, run.Picture, new SessionOptions(1, run.Budget)));
                int withWork = 0;
                bool ok = true;
                foreach (TapPod tap in taps)
                {
                    if (!replay.Session.Check(tap).IsAllowed)
                    {
                        ok = false;
                        break;
                    }

                    withWork += replay.Apply(tap).Any(e => e is TileCleared) ? 1 : 0;
                    replay.Advance(0.03f);
                }

                if (!ok)
                {
                    continue;
                }

                // Both pods show at once, each on a plate of its own, even when the rules gave the second the first one's slot
                // because the first finished at once: no pod has left its plate yet, and the rules took both, so a plate was
                // free on screen for each.
                int first = replay.Places.PlaceOf(taps[0].PodId);
                int second = replay.Places.PlaceOf(taps[1].PodId);
                if (first < 0 || second < 0 || first == second)
                {
                    problems.Add($"{run.Name}: the pods show on plates {first} and {second}");
                }

                bool both = false;
                for (int i = 0; i < 3000 && !replay.Settled; i++)
                {
                    both |= replay.Player.Playing >= 2;
                    replay.Advance(1f / 60f);
                }

                if (withWork == 2)
                {
                    working++;
                    together += both ? 1 : 0;
                    if (!both)
                    {
                        problems.Add(run.Name + ": the two taps' waves never played at once");
                    }
                }
            }

            Assert.That(working, Is.GreaterThan(10), "levels whose first two taps both have work");
            Assert.That(problems, Is.Empty, string.Join("\n", problems.Take(12)));
            Assert.That(together, Is.EqualTo(working));
        }

        private static IEnumerable<string> Replay(Run run, bool rapid)
        {
            var replay = new Presentation(LevelSession.Load(run.Level, run.Picture, new SessionOptions(1, run.Budget)));
            int step = 0;
            foreach (Command command in run.Commands)
            {
                step++;
                if (!replay.Session.Check(command).IsAllowed)
                {
                    continue; // A refused tap changes nothing (golden refusal cases).
                }

                replay.Apply(command);
                if (rapid)
                {
                    replay.Advance(0.03f);
                }
                else
                {
                    replay.Settle();
                    foreach (string problem in replay.CheckSlots())
                    {
                        yield return $"step {step} {command}: {problem}";
                    }
                }
            }

            replay.Settle();
            foreach (string problem in replay.Problems.Concat(replay.CheckSlots()).Concat(replay.CheckOrders(complete: true)))
            {
                yield return "end: " + problem;
            }
        }

        private static List<Run> Runs()
        {
            var runs = new List<Run>();
            foreach (string file in Directory.GetFiles(Path.Combine(Root, "core", "tests", "golden"), "*.golden.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                GoldenCase golden = GoldenCase.Read(File.ReadAllText(file));
                runs.Add(new Run(golden.Name, golden.Definition, golden.Picture, golden.Commands, golden.ShuffleNodeBudget));
            }

            var pictures = new Dictionary<string, BasePicture>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(Path.Combine(Root, "content", "pictures", "lib"), "*.json"))
            {
                BasePicture picture = BasePictureJson.Read(File.ReadAllText(file));
                pictures[picture.Id] = picture with { Review = picture.Review with { Status = ReviewStatus.Approved } };
            }

            string showcase = Path.Combine(Root, "content", "showcase");
            foreach (string file in Directory.GetFiles(Path.Combine(showcase, "levels"), "*.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                string record = Path.Combine(showcase, "validation", Path.GetFileName(file));
                if (!File.Exists(record))
                {
                    continue;
                }

                LevelDefinition level = DefinitionJson.Read(File.ReadAllText(file));
                ValidationRecord validation = ValidationRecord.Read(File.ReadAllText(record));
                runs.Add(new Run("showcase/L" + level.LevelNumber, level, pictures[level.Picture.Id], validation.SolutionTrace, 20000));
                if (validation.JamWitness != null)
                {
                    runs.Add(new Run("showcase/L" + level.LevelNumber + "-jam", level, pictures[level.Picture.Id], validation.JamWitness, 20000));
                }
            }

            return runs;
        }

        private sealed class Run
        {
            public Run(string name, LevelDefinition level, BasePicture picture, IReadOnlyList<Command> commands, int budget)
            {
                Name = name;
                Level = level;
                Picture = picture;
                Commands = commands;
                Budget = budget;
            }

            public string Name { get; }

            public LevelDefinition Level { get; }

            public BasePicture Picture { get; }

            public IReadOnlyList<Command> Commands { get; }

            public int Budget { get; }
        }

        /// <summary>
        /// A level played through the presentation as <c>GameplayController</c> does: taps and boosters apply at once, the
        /// slots show commits at once, the timeline plays the rest; a finished pod leaves its plate 0.22 s after its wave,
        /// a key opens its slot lock as soon as its wave shows. It records what showed, and when, to compare with the rules.
        /// </summary>
        private sealed class Presentation : ITimelineSink
        {
            private readonly HashSet<int> _held = new HashSet<int>();
            private readonly List<(int Place, float At)> _leaves = new List<(int, float)>();

            // What the rules did, in order, and what showed, in order: per cell its clears, per special its progress.
            private readonly Dictionary<CellPos, List<TileCleared>> _rulesCells = new Dictionary<CellPos, List<TileCleared>>();
            private readonly Dictionary<CellPos, List<TileCleared>> _shownCells = new Dictionary<CellPos, List<TileCleared>>();
            private readonly Dictionary<string, List<string>> _rulesSpecials = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            private readonly Dictionary<string, List<string>> _shownSpecials = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _rulesWork = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _shownWork = new Dictionary<string, int>(StringComparer.Ordinal);
            private bool _flushing;

            public Presentation(LevelSession session)
            {
                Session = session;
                Player.Bind(this);
                Places.Rebuild(SlotPlaces.ToShow(session.View, null), Usable);
            }

            public LevelSession Session { get; }

            public TimelinePlayer Player { get; } = new TimelinePlayer();

            public SlotPlaces Places { get; } = new SlotPlaces();

            public List<string> Problems { get; } = new List<string>();

            public bool Settled => Player.IsIdle && _leaves.Count == 0;

            public IReadOnlyList<GameEvent> Apply(Command command)
            {
                LevelView view = Session.View;
                if (command is TapPod tap)
                {
                    var before = new Dictionary<string, (int Count, VariantId? Variant)>(StringComparer.Ordinal);
                    foreach (string member in view.ConnectedGroup(tap.PodId).Append(tap.PodId))
                    {
                        before[member] = (view.Pod(member).Remaining, view.Pod(member).Variant);
                    }

                    CommandResult tapped = Session.Apply(command);
                    Hold(tapped.Events);
                    foreach (GameEvent e in tapped.Events.TakeWhile(e => e.Round == 0))
                    {
                        if (e is PodCommitted committed)
                        {
                            (int count, VariantId? variant) = before[committed.PodId];
                            Places.Commit(committed.SlotIndex, committed.PodId, variant, count, Usable, out _);
                        }
                        else if (e is MysteryPodRevealed revealed)
                        {
                            Places.Reveal(revealed.PodId, revealed.Variant);
                        }
                    }

                    Queue(tapped.Events);
                    return tapped.Events;
                }

                if (command is Restart)
                {
                    Session.Apply(command);
                    Problems.AddRange(CheckOrders(complete: false));
                    Player.Clear();
                    _held.Clear();
                    _leaves.Clear();
                    _rulesCells.Clear();
                    _shownCells.Clear();
                    _rulesSpecials.Clear();
                    _shownSpecials.Clear();
                    _rulesWork.Clear();
                    _shownWork.Clear();
                    Places.Clear();
                    Places.Rebuild(SlotPlaces.ToShow(Session.View, null), Usable);
                    return Array.Empty<GameEvent>();
                }

                // A booster: everything pending shows at once, then its own changes, then the slots take the new state.
                _flushing = true;
                Player.Flush();
                _flushing = false;
                _leaves.Clear();
                CommandResult boosted = Session.Apply(command);
                Hold(boosted.Events);
                foreach (GameEvent e in boosted.Events)
                {
                    if (e.Round == 0 && !(e is VariantBurst || e is ExtraSlotAdded || e is PodReturned || e is TrayShuffled))
                    {
                        Rules(e);
                        OnEvent(e);
                    }
                }

                _leaves.Clear();
                Places.Rebuild(SlotPlaces.ToShow(Session.View, boosted.Events), Usable);
                Queue(boosted.Events);
                return boosted.Events;
            }

            public void Advance(float seconds)
            {
                Player.Advance(seconds);
                for (int i = _leaves.Count - 1; i >= 0; i--)
                {
                    if (Player.Now >= _leaves[i].At)
                    {
                        Places.FinishLeave(_leaves[i].Place);
                        _leaves.RemoveAt(i);
                    }
                }
            }

            public void Settle()
            {
                for (int i = 0; i < 20000 && !Settled; i++)
                {
                    Advance(1f / 30f);
                }

                if (!Settled)
                {
                    Problems.Add("the timeline never settled");
                }
            }

            /// <summary>Every pod the rules hold shows once, on a usable plate, with its count and variant; nothing else shows or waits.</summary>
            public IEnumerable<string> CheckSlots()
            {
                LevelView view = Session.View;
                var held = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < view.SlotCapacity; i++)
                {
                    string? pod = view.PodInSlot(i);
                    if (pod == null)
                    {
                        continue;
                    }

                    held.Add(pod);
                    int place = Places.PlaceOf(pod);
                    if (place < 0)
                    {
                        yield return $"pod {pod} of slot {i} does not show";
                        continue;
                    }

                    SlotPlace shown = Places[place];
                    if (shown.Count != view.Pod(pod).Remaining || !Equals(shown.Variant, view.Pod(pod).Variant))
                    {
                        yield return $"plate {place} shows {shown.Count}/{shown.Variant}, the rules have {view.Pod(pod).Remaining}/{view.Pod(pod).Variant}";
                    }

                    if (!Usable(place))
                    {
                        yield return $"pod {pod} shows on the unusable plate {place}";
                    }
                }

                foreach (SlotPlace place in Places.Places)
                {
                    if (place.PodId != null && !held.Contains(place.PodId))
                    {
                        yield return $"plate {place.Index} shows {place.PodId}, which the rules no longer hold";
                    }

                    if (place.Waiting > 0)
                    {
                        yield return $"plate {place.Index} still has pods waiting";
                    }
                }
            }

            /// <summary>Each cell's clears and each special's progress showed in the rules' order (all of them, when complete).</summary>
            public IEnumerable<string> CheckOrders(bool complete)
            {
                foreach (KeyValuePair<CellPos, List<TileCleared>> cell in _rulesCells)
                {
                    List<TileCleared> shown = _shownCells.TryGetValue(cell.Key, out List<TileCleared>? s) ? s : new List<TileCleared>();
                    bool prefix = shown.Count <= cell.Value.Count && shown.Select((c, i) => ReferenceEquals(c, cell.Value[i])).All(same => same);
                    if (!prefix || (complete && shown.Count != cell.Value.Count))
                    {
                        yield return $"cell {cell.Key.X},{cell.Key.Y} showed {Describe(shown)}, the rules did {Describe(cell.Value)}";
                    }
                }

                foreach (KeyValuePair<string, List<string>> special in _rulesSpecials)
                {
                    List<string> shown = _shownSpecials.TryGetValue(special.Key, out List<string>? s) ? s : new List<string>();
                    bool prefix = shown.Count <= special.Value.Count && shown.SequenceEqual(special.Value.Take(shown.Count));
                    if (!prefix || (complete && shown.Count != special.Value.Count))
                    {
                        yield return $"special {special.Key} showed {string.Join(" ", shown)}, the rules did {string.Join(" ", special.Value)}";
                    }
                }
            }

            public void OnWorkStarted(IReadOnlyList<WorkUnit> batch, float start, float travelSeconds)
            {
                if (_flushing)
                {
                    Problems.Add("a walker set off while flushing");
                }
            }

            public void OnWorkArrived(WorkUnit unit)
            {
                TileCleared clear = unit.Clear;
                (_shownCells.TryGetValue(clear.Cell, out List<TileCleared>? list) ? list : _shownCells[clear.Cell] = new List<TileCleared>()).Add(clear);
                _shownWork[clear.PodId] = (_shownWork.TryGetValue(clear.PodId, out int n) ? n : 0) + 1;
                Places.Decrement(clear.PodId);
            }

            public void OnEvent(GameEvent e)
            {
                switch (e)
                {
                    case PodCompleted done:
                        int rules = _rulesWork.TryGetValue(done.PodId, out int r) ? r : 0;
                        int shown = _shownWork.TryGetValue(done.PodId, out int s) ? s : 0;
                        if (shown != rules)
                        {
                            Problems.Add($"pod {done.PodId} left after {shown} of its {rules} Bloomlings");
                        }

                        int place = Places.Complete(done.PodId);
                        if (place >= 0)
                        {
                            _leaves.Add((place, Player.Now + LeaveSeconds));
                        }

                        break;
                    case SpecialProgressed progressed:
                        Shown(progressed.SpecialId, "p" + progressed.Progress);
                        break;
                    case SpecialTriggered triggered:
                        Shown(triggered.SpecialId, "t");
                        break;
                    case KeyCollected key:
                        foreach (LockDef lockDef in Session.View.Locks)
                        {
                            if (lockDef.KeyId == key.KeyId && lockDef.TargetKind == LockTargetKind.Slot && int.TryParse(lockDef.TargetId, out int slot) && _held.Remove(slot))
                            {
                                Places.TakeWaiting(slot);
                            }
                        }

                        break;
                    case LevelWon _:
                    case LevelJammed _:
                    case LevelStuck _:
                        if (!Player.IsIdle)
                        {
                            Problems.Add(e.GetType().Name + " showed before the last wave");
                        }

                        break;
                }
            }

            private static string Describe(List<TileCleared> clears) => string.Join(" ", clears.Select(c => c.PodId + "@" + c.Round));

            private bool Usable(int slot)
            {
                SlotState state = Session.View.SlotStateOf(slot);
                return state != SlotState.Locked && state != SlotState.Absent && !_held.Contains(slot);
            }

            /// <summary>Slot locks the rules opened with this command stay drawn locked until their key shows (GameplayController.HoldLocksOpenedBy).</summary>
            private void Hold(IReadOnlyList<GameEvent> events)
            {
                foreach (GameEvent e in events)
                {
                    if (e is KeyCollected key)
                    {
                        foreach (LockDef lockDef in Session.View.Locks)
                        {
                            if (lockDef.KeyId == key.KeyId && lockDef.TargetKind == LockTargetKind.Slot && int.TryParse(lockDef.TargetId, out int slot))
                            {
                                _held.Add(slot);
                            }
                        }
                    }
                }
            }

            private void Queue(IReadOnlyList<GameEvent> events)
            {
                foreach (GameEvent e in events)
                {
                    if (e.Round > 0)
                    {
                        Rules(e);
                    }
                }

                Player.Enqueue(events);
            }

            private void Rules(GameEvent e)
            {
                switch (e)
                {
                    case TileCleared clear:
                        (_rulesCells.TryGetValue(clear.Cell, out List<TileCleared>? list) ? list : _rulesCells[clear.Cell] = new List<TileCleared>()).Add(clear);
                        _rulesWork[clear.PodId] = (_rulesWork.TryGetValue(clear.PodId, out int n) ? n : 0) + 1;
                        break;
                    case SpecialProgressed progressed:
                        Expect(progressed.SpecialId, "p" + progressed.Progress);
                        break;
                    case SpecialTriggered triggered:
                        Expect(triggered.SpecialId, "t");
                        break;
                }
            }

            private void Expect(string special, string what) =>
                (_rulesSpecials.TryGetValue(special, out List<string>? list) ? list : _rulesSpecials[special] = new List<string>()).Add(what);

            private void Shown(string special, string what) =>
                (_shownSpecials.TryGetValue(special, out List<string>? list) ? list : _shownSpecials[special] = new List<string>()).Add(what);
        }
    }
}
