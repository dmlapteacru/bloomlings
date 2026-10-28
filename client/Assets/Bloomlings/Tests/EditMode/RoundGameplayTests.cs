using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Client.App;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Gameplay.Rounds;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity client's round gameplay, engine-free (spec 006 US5, T058, T059): the Gameplay scene's routing, the round
    /// level core the <c>RoundGameplayController</c> runs on (the kit's <see cref="RoundPlay"/>: the live timeline in whole µs
    /// × the display speed), and what its views read (places, walkers as tinted family heroes). Every published round level
    /// wins with its saved solution through the core, with the same events as the reference replay.
    /// </summary>
    public class RoundGameplayTests
    {
        private sealed class Host : IRoundLevelHost
        {
            public HashSet<string> Seen { get; } = new HashSet<string>(StringComparer.Ordinal);

            public HashSet<BoosterKind> Unlocked { get; } = new HashSet<BoosterKind>();

            public Dictionary<BoosterKind, int> ChargesLeft { get; } = new Dictionary<BoosterKind, int>();

            public bool Rich { get; set; } = true;

            public int Taken { get; private set; }

            public int Collects { get; private set; }

            public List<SoundCue> Sounds { get; } = new List<SoundCue>();

            public bool SpeedUp { get; set; }

            public bool Symbols { get; set; }

            public bool HasSeenDemo(string demoId) => Seen.Contains(demoId);

            public void MarkDemoSeen(string demoId) => Seen.Add(demoId);

            public bool IsUnlocked(BoosterKind kind) => Unlocked.Contains(kind);

            public bool CanAfford(BoosterKind kind) => IsUnlocked(kind) && (Charges(kind) > 0 || Rich);

            public int Charges(BoosterKind kind) => ChargesLeft.TryGetValue(kind, out int n) ? n : 0;

            public bool TryTakeCharge(BoosterKind kind)
            {
                if (Charges(kind) > 0)
                {
                    ChargesLeft[kind]--;
                }
                else if (!Rich)
                {
                    return false;
                }

                Taken++;
                return true;
            }

            public void Sound(SoundCue cue) => Sounds.Add(cue);

            public void Collect(ClearStyle style, int step) => Collects++;

            /// <summary>Every demo and introduction seen: the level opens straight on play.</summary>
            public Host SeenAll()
            {
                foreach (string id in GuideTour.DemoIds)
                {
                    Seen.Add(id);
                }

                foreach (string mechanic in RoundIntroCards.Mechanics)
                {
                    Seen.Add(RoundIntroCards.IdOf(mechanic, false));
                    Seen.Add(RoundIntroCards.IdOf(mechanic, true));
                }

                foreach (BoosterKind kind in RoundLevelCore.Boosters)
                {
                    Seen.Add(GuideTour.BoosterId(kind));
                }

                return this;
            }
        }

        private const float Frame = 1f / 60f;

        // 7 × 5 with the frame: the gate in the bottom wall's middle, an open ring, three G cubes in the middle row.
        private static RoundLevel Plain(RoundIntroduction? introduction = null) => new RoundLevel(1, 7, 5,
            new[] { "###.###", "#.....#", "#.GGG.#", "#.....#", "#######" },
            new[] { new RoundPod('G', 3) },
            new IReadOnlyList<int>[] { new[] { 0 } },
            introduction: introduction);

        // Two pods in one column: G on top, P under it.
        private static RoundLevel TwoInAColumn() => new RoundLevel(2, 7, 5,
            new[] { "###.###", "#.....#", "#.GGPP#", "#.....#", "#######" },
            new[] { new RoundPod('G', 2), new RoundPod('P', 2) },
            new IReadOnlyList<int>[] { new[] { 0, 1 } });

        // G linked with P, which is buried under B in the second column.
        private static RoundLevel BuriedPartner() => new RoundLevel(3, 8, 5,
            new[] { "####.###", "#......#", "#.GGPPB#", "#......#", "########" },
            new[] { new RoundPod('G', 2, group: "A"), new RoundPod('P', 2, group: "A"), new RoundPod('B', 1) },
            new IReadOnlyList<int>[] { new[] { 0 }, new[] { 2, 1 } });

        // Six B pods for six walled-in B cubes: five fill the places, the sixth waits, and the places jam.
        private static RoundLevel Jamming()
        {
            var pods = Enumerable.Range(0, 6).Select(_ => new RoundPod('B', 1)).ToArray();
            return new RoundLevel(4, 9, 7,
                new[] { "####.####", "#.......#", "#.#####.#", "#.#BBB#.#", "#.#BBB#.#", "#.#####.#", "#########" },
                pods,
                new IReadOnlyList<int>[] { Enumerable.Range(0, 6).ToArray() });
        }

        private static void Settle(RoundLevelCore core, int guard = 200_000)
        {
            for (int k = 0; k < guard && (core.Play.Timeline.Pending || core.Animator.Busy); k++)
            {
                core.Tick(Frame);
            }
        }

        private static RoundContentSet Published() => RoundContentLoader.ReadFolder(RoundContentLoader.RepositoryPublishedFolder);

        private static RoundProof ProofOf(int level) =>
            RoundJson.ReadProof(File.ReadAllText(Path.Combine(DevContent.RepositoryContentFolder, "game", "rounds", "proofs", "L" + level.ToString("0000") + ".json")));

        private static RoundInput Input(RoundCommand c) => c.Type == RoundCommandType.Tap ? RoundInput.Tap(c.Index, c.AtUs) : RoundInput.Bridge(c.Index, c.AtUs);

        [Test]
        public void ARoundAttempt_AlwaysPlaysOnTheRoundRules_NeverTheDevLevel()
        {
            var level = Plain();
            var picture = new RoundPicture("P0001", 1, "Test", 5, 3, new[] { ".....", ".GGG.", "....." }, "2026-10-28");
            var round = new RoundAttempt(1, level, picture, 1);
            Assert.That(GameplayRouting.For(round, null), Is.EqualTo(GameplayRoute.Round));
            Assert.That(GameplayRouting.For(null, null), Is.EqualTo(GameplayRoute.DevLevel));
        }

        [Test]
        public void ThePublishedRoundLevel_LoadsTheGameplayScene_ThroughTheOneEntryPoint()
        {
            var save = PlayerSave.CreateNew("p1", new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc));
            var progression = new ProgressionService(save, UnlockRoadmap.Default, () => { });
            progression.Initialize();
            var scenes = new List<string>();
            var legacy = new CatalogService(new ContentSet(0, LooseContentFolder.DevShuffleNodeBudget, Array.Empty<LevelDefinition>(), Array.Empty<BasePicture>()));
            var flow = new GameFlow(progression, legacy, new RoundCatalogService(Published()), scenes.Add);

            flow.Begin(firstLaunch: true);
            Assert.That(scenes, Is.EqualTo(new[] { GameFlow.GameplayScene }), "Level 1 plays at once in the Gameplay scene");
            Assert.That(flow.CurrentRoundAttempt, Is.Not.Null);
            Assert.That(flow.CurrentAttempt, Is.Null);
            Assert.That(GameplayRouting.For(flow.CurrentRoundAttempt, flow.CurrentAttempt), Is.EqualTo(GameplayRoute.Round));
        }

        [Test]
        public void EveryPublishedRoundLevel_WinsThroughTheUnityCore_WithTheReferenceEvents()
        {
            RoundContentSet set = Published();
            Assert.That(set.LevelCount, Is.GreaterThanOrEqualTo(20));
            for (int n = 1; n <= set.LevelCount; n++)
            {
                RoundLevel level = set.GetLevel(n);
                RoundProof proof = ProofOf(n);
                var host = new Host().SeenAll();
                var core = new RoundLevelCore(n, level, ClearStyle.Munchers, host);
                Assert.That(core.Intro, Is.Null, $"L{n}: every card seen");
                Assert.That(core.Guide, Is.Null, $"L{n}: every demo seen");

                foreach (RoundCommand c in proof.Solution.Commands)
                {
                    // The patient player: each command once the work before it has settled.
                    for (int k = 0; k < 200_000 && core.Play.Timeline.Pending; k++)
                    {
                        core.Tick(Frame);
                    }

                    Assert.That(core.Play.Timeline.Now, Is.EqualTo(c.AtUs), $"L{n}: {c} comes at its model time");
                    bool accepted = c.Type == RoundCommandType.Tap ? core.Tap(c.Index) == RoundTapOutcome.Accepted : core.BuildBridge(c.Index);
                    Assert.That(accepted, Is.True, $"L{n}: {c} accepted");
                    Assert.That(core.Animator.ShowsRulesState(core.Play.Timeline, out string after), Is.True, $"L{n} after {c}: {after}");
                }

                Settle(core);
                Assert.That(core.Won, Is.True, $"L{n}: won");
                Assert.That(core.Play.Settled, Is.True, $"L{n}: settled");
                Assert.That(core.TakeWin(), Is.True, $"L{n}: the win is recorded once");
                Assert.That(core.TakeWin(), Is.False);
                Assert.That(core.BoostersPaid, Is.Zero);

                var replay = new RoundReplay(new RoundTimeline(level));
                foreach (RoundCommand c in proof.Solution.Commands)
                {
                    replay.Execute(Input(c), patient: true);
                }

                replay.Finish();
                Assert.That(RoundEvents.Signature(core.Play.Timeline), Is.EqualTo(RoundEvents.Signature(replay.Timeline)), $"L{n}: the same final state as the reference");
                Assert.That(core.Animator.ShowsRulesState(core.Play.Timeline, out string end), Is.True, $"L{n}: {end}");
                Assert.That(core.Inputs.Select(i => i.ToString()), Is.EqualTo(proof.Solution.Commands.Select(c => Input(c).ToString())), $"L{n}: the recorded commands are the proof's");
                int cubes = level.RowsBottomFirst.Sum(row => row.Count(ch => RoundMaterial.IndexOf(ch) >= 0));
                Assert.That(host.Collects, Is.EqualTo(cubes), $"L{n}: one collect a cube");
                Assert.That(host.Sounds.Count(s => s == SoundCue.Win), Is.EqualTo(1), $"L{n}: the win sounds once");
            }
        }

        [Test]
        public void TheWinsCommandLogHash_IsTheSha256OfTheRecordedCommands()
        {
            var inputs = new[] { RoundInput.Tap(0, 0), RoundInput.Bridge(1, 2_340_000), RoundInput.ExtraSlot(5_000_000) };
            string expected = PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes("tap pod=0 at=0\nbridge choice=1 at=2340000\nextra-slot at=5000000\n"));
            Assert.That(LeaderboardClient.CommandLogHash(inputs), Is.EqualTo(expected));
        }

        [Test]
        public void TheSpeedPill_DoublesTheClock_AndAutoTakesTheRestAtDouble_WithTheHudGone()
        {
            var host = new Host().SeenAll();
            var core = new RoundLevelCore(2, TwoInAColumn(), ClearStyle.Munchers, host);
            Assert.That(core.Speed, Is.EqualTo(1));
            core.ToggleSpeed();
            Assert.That(host.SpeedUp, Is.True, "the speed pill is the save's setting");
            Assert.That(core.Speed, Is.EqualTo(RoundPlay.SpeedUp));
            core.ToggleSpeed();

            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Accepted));
            Assert.That(core.Auto, Is.False, "a pod still waits in the column");
            long before = core.Play.ClockUs;
            core.Tick(0.05f);
            Assert.That(core.Play.ClockUs - before, Is.EqualTo(50_000), "×1: real time");

            Assert.That(core.Tap(1), Is.EqualTo(RoundTapOutcome.Accepted));
            Assert.That(core.Auto, Is.True, "every column is empty: Auto");
            Assert.That(core.Speed, Is.EqualTo(RoundProfile.TailDisplaySpeed));
            Assert.That(core.SpeedShown, Is.True);
            before = core.Play.ClockUs;
            core.Tick(0.05f);
            Assert.That(core.Play.ClockUs - before, Is.EqualTo(50_000L * RoundProfile.TailDisplaySpeed), "Auto ×2");
            for (int k = 0; k < 30; k++)
            {
                core.Tick(Frame);
            }

            Assert.That(core.HudAlpha, Is.EqualTo(0f), "the whole HUD faded away (FR-044)");
            Assert.That(core.HudLive, Is.False);
            Settle(core);
            Assert.That(core.Won, Is.True);
        }

        [Test]
        public void UnderAuto_TapsAndBoostersAreIgnored()
        {
            var host = new Host().SeenAll();
            host.Unlocked.Add(BoosterKind.ExtraSlot);
            host.ChargesLeft[BoosterKind.ExtraSlot] = 3;
            var core = new RoundLevelCore(1, Plain(), ClearStyle.Munchers, host);
            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Accepted));
            Assert.That(core.Auto, Is.True);
            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Ignored));
            Assert.That(core.PressBooster(BoosterKind.ExtraSlot), Is.EqualTo(RoundBoosterOutcome.Ignored));
            Assert.That(core.Play.Inputs.Count, Is.EqualTo(1));
        }

        [Test]
        public void ATapWaitingForItsLinkPartner_KeepsTheRulesReason_AndPulsesThePartner()
        {
            var host = new Host().SeenAll();
            var core = new RoundLevelCore(3, BuriedPartner(), ClearStyle.Munchers, host);
            var toasts = new List<string>();
            core.Toasted += toasts.Add;

            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Refused));
            Assert.That(core.LastRefusal, Is.EqualTo(RoundRefusal.PartnerNotExposed));
            Assert.That(toasts, Is.EqualTo(new[] { "refusal.partner_buried" }));
            Assert.That(core.LinkHint.HasValue && core.LinkHint.Value.Pod == 0, Is.True);
            core.Tick(0.1f);
            Assert.That(core.LinkPulse, Is.GreaterThan(0f), "the partner pulses");
            Assert.That(host.Sounds, Does.Contain(SoundCue.Refused));
            Assert.That(core.Play.Inputs, Is.Empty, "a refusal records nothing");
        }

        [Test]
        public void TheIntroductionCard_HoldsTheClock_UntilGotIt_AndShowsOnce()
        {
            var host = new Host();
            var core = new RoundLevelCore(1, Plain(new RoundIntroduction(RoundIntroCards.Hidden, false)), ClearStyle.Munchers, host);
            Assert.That(core.Intro, Is.Not.Null);
            Assert.That(core.Intro!.Kind, Is.EqualTo(RoundIntroKind.Explanation));
            Assert.That(core.Paused, Is.True);
            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Ignored), "no tap under the card");
            core.CloseIntro();
            Assert.That(core.Intro, Is.Null);
            Assert.That(host.Seen, Does.Contain(RoundIntroCards.IdOf(RoundIntroCards.Hidden, false)));

            var again = new RoundLevelCore(1, Plain(new RoundIntroduction(RoundIntroCards.Hidden, false)), ClearStyle.Munchers, host);
            Assert.That(again.Intro, Is.Null, "each card shows once");
            var reminder = new RoundLevelCore(1, Plain(new RoundIntroduction(RoundIntroCards.Hidden, true)), ClearStyle.Munchers, host);
            Assert.That(reminder.Intro!.Kind, Is.EqualTo(RoundIntroKind.Reminder), "the second meeting shows the reminder");
        }

        [Test]
        public void AGuidedBoosterDemo_IsFree_AndKeepsTheCleanClear()
        {
            var host = new Host().SeenAll();
            host.Seen.Remove(GuideTour.BoosterId(BoosterKind.ExtraSlot));
            host.Unlocked.Add(BoosterKind.ExtraSlot);
            var core = new RoundLevelCore(3, TwoInAColumn(), ClearStyle.Munchers, host);
            Assert.That(core.Guide, Is.Not.Null);
            Assert.That(core.Guide!.Kind, Is.EqualTo(GuideKind.Booster));
            Assert.That(core.Guide.Booster, Is.EqualTo(BoosterKind.ExtraSlot));
            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Ignored), "only the lit booster takes the tap");

            Assert.That(core.PressBooster(BoosterKind.ExtraSlot), Is.EqualTo(RoundBoosterOutcome.Used));
            Assert.That(host.Taken, Is.Zero, "the demo's use takes no charge");
            Assert.That(core.Animator.PlaceCount, Is.EqualTo(6));
            Assert.That(core.DemoBoosterUses, Is.EqualTo(1));
            Assert.That(core.BoostersPaid, Is.Zero, "the clean clear stays");
            Assert.That(host.Seen, Does.Contain(GuideTour.BoosterId(BoosterKind.ExtraSlot)));
            while (core.Guide != null)
            {
                core.NextGuideStep();
            }

            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Accepted));
        }

        [Test]
        public void ABoosterWithoutCharges_AsksThePurchaseConfirmationFirst()
        {
            var host = new Host().SeenAll();
            host.Unlocked.Add(BoosterKind.ExtraSlot);
            var core = new RoundLevelCore(3, TwoInAColumn(), ClearStyle.Munchers, host);
            Assert.That(core.PressBooster(BoosterKind.ExtraSlot), Is.EqualTo(RoundBoosterOutcome.NeedsPurchase));
            Assert.That(core.Animator.PlaceCount, Is.EqualTo(5), "nothing used before the confirmation");
            Assert.That(core.UseBooster(BoosterKind.ExtraSlot, RoundInput.ExtraSlot(0), confirmed: true), Is.EqualTo(RoundBoosterOutcome.Used));
            Assert.That(host.Taken, Is.EqualTo(1));
            Assert.That(core.BoostersPaid, Is.EqualTo(1));
            Assert.That(core.Inputs.Last().Type, Is.EqualTo(RoundInputType.ExtraSlot));

            host.Rich = false;
            host.Unlocked.Add(BoosterKind.Return);
            Assert.That(core.PressBooster(BoosterKind.Return), Is.EqualTo(RoundBoosterOutcome.NotEnoughPetals));
        }

        [Test]
        public void TheJamCard_OffersOnlyTheBoostersTheRulesAcceptNow_AndTheRescueMakesRoom()
        {
            var host = new Host().SeenAll();
            foreach (BoosterKind kind in RoundLevelCore.Boosters)
            {
                host.Unlocked.Add(kind);
                host.ChargesLeft[kind] = 1;
            }

            var core = new RoundLevelCore(4, Jamming(), ClearStyle.Munchers, host);
            Assert.That(core.RescueBooster(), Is.Null, "no rescue while the level plays");
            for (int pod = 0; pod < 5; pod++)
            {
                Assert.That(core.Tap(pod), Is.EqualTo(RoundTapOutcome.Accepted));
            }

            Settle(core);
            Assert.That(core.Blocked, Is.True, "five places of pods without a cube in reach, one more waiting");
            List<BoosterKind> offered = core.JamBoosters();
            Assert.That(offered, Is.Not.Empty);
            Assert.That(offered.All(core.Applicable), Is.True);
            Assert.That(RoundLevelCore.Boosters.Where(core.Applicable), Is.EquivalentTo(offered));
            Assert.That(core.PlaceLookOf(0), Is.EqualTo(RoundPlaceLook.Stuck), "a pod with no cube in reach waits, greyed");
            Assert.That(core.Paused, Is.False);
            core.JamShown = true;
            Assert.That(core.Paused, Is.True, "the jam card holds the clock");

            (BoosterKind Kind, RoundInput Command)? rescue = core.RescueBooster();
            Assert.That(rescue.HasValue && rescue.Value.Kind == BoosterKind.ExtraSlot, Is.True, "Extra Slot first");
            Assert.That(core.UseBooster(rescue!.Value.Kind, rescue.Value.Command, free: true), Is.EqualTo(RoundBoosterOutcome.Used));
            Assert.That(host.Taken, Is.Zero, "the rescue is free");
            Assert.That(core.Blocked, Is.False);
            Assert.That(core.JamShown, Is.False);
            Assert.That(core.PlaceLookOf(5), Is.EqualTo(RoundPlaceLook.Danger), "the one free place left is in danger");
        }

        [Test]
        public void Restart_StartsTheSameLevelAgain_WithAFreshAttempt()
        {
            var host = new Host().SeenAll();
            var core = new RoundLevelCore(2, TwoInAColumn(), ClearStyle.Munchers, host);
            core.Tap(0);
            core.Tick(0.2f);
            long clock = core.Play.ClockUs;
            core.Restart();
            Assert.That(core.Inputs, Is.Empty);
            Assert.That(core.Play.Timeline.Now, Is.Zero);
            Assert.That(core.Play.ClockUs, Is.EqualTo(clock), "the display clock keeps running");
            Assert.That(core.Animator.ShowsRulesState(core.Play.Timeline, out string difference), Is.True, difference);
            Assert.That(core.Tap(0), Is.EqualTo(RoundTapOutcome.Accepted));
        }

        [Test]
        public void TheWalkers_AreTheMaterialsFamilyHero_AndTheHeldCubeShowsItsGround()
        {
            var host = new Host().SeenAll();
            RoundLevel level = Plain();
            var core = new RoundLevelCore(1, level, ClearStyle.Munchers, host);
            RoundBoardLayout layout = RoundBoardLayout.Fit(new Box(0f, 0f, 700f, 500f), level);
            var fx = new RoundFx();
            core.Tap(0);
            int g = RoundMaterial.IndexOf('G');
            bool walker = false;
            bool held = false;
            bool restore = false;
            for (int k = 0; k < 2000 && (core.Play.Timeline.Pending || core.Animator.Busy); k++)
            {
                core.Tick(Frame);
                fx.Build(core.Animator, layout, core.Style, core.Play.ClockUs, _ => new Box(3f, 4f, 4f, 5f));
                foreach (FxItem item in fx.List.Items)
                {
                    if (item.Kind == FxKind.Character)
                    {
                        Assert.That(item.Material, Is.EqualTo(g), "a round walker carries its material (FR-048)");
                        walker = true;
                    }
                }

                foreach (int cell in fx.HeldCells)
                {
                    Assert.That(fx.Holds(cell), Is.True);
                    Assert.That(layout.KindOf(cell), Is.EqualTo(RoundCellKind.Material));
                    held = true;
                }

                restore |= fx.List.Items.Any(i => i.Kind != FxKind.Character && i.Material == g);
            }

            Assert.That(core.Won, Is.True);
            Assert.That(walker, Is.True, "the walkers show");
            Assert.That(held, Is.True, "the style holds each cube while it eats it");
            Assert.That(restore || held, Is.True);
            Assert.That(RoundFx.FamilyVariant(g), Is.EqualTo(Core.Variants.VariantId.Leaf));
            (float x, float y) = RoundFx.InCells(layout, level.Entries[0]);
            Assert.That(y, Is.GreaterThan(layout.Height - 2 - 0.01f), "the gate's door lies on the border under the grid");
            Assert.That(x, Is.GreaterThan(2f).And.LessThan(3f));
        }
    }
}
