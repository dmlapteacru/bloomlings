using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>A short modal message before play: a demo or an unlock (FR-031, FR-071), with optional variant tiles.</summary>
    public sealed record DemoCard(string Id, IReadOnlyList<string> Lines, IReadOnlyList<VariantId> Variants, bool ShowIgnore);

    /// <summary>
    /// The gameplay screen of frames 7–9, with the pod, slot and booster states of frames 12–14 (spec 002 US1).
    /// <list type="bullet">
    /// <item><description>The rules resolve every tap at once in the core.</description></item>
    /// <item><description><see cref="LevelAnimator"/> plays the result: Bloomlings walk, tiles shrink away, counts drop.</description></item>
    /// <item><description>Boosters, their targeting, demos, the jam sheet, the win and milestone cards work as in the
    /// first playtest, in the board's look.</description></item>
    /// </list>
    /// It holds no rules: it draws <see cref="LevelView"/> and calls <see cref="PlaytestMeta"/>. Engine-free.
    /// </summary>
    public sealed class LevelScreen
    {
        public static readonly (BoosterKind Kind, Recovery Recovery, string Id)[] Boosters =
        {
            (BoosterKind.ExtraSlot, Recovery.ExtraSlot, "extra_slot"),
            (BoosterKind.Shuffle, Recovery.Shuffle, "shuffle"),
            (BoosterKind.Return, Recovery.Return, "return"),
            (BoosterKind.BloomBurst, Recovery.BloomBurst, "bloom_burst"),
        };

        private readonly DesignApp _app;
        private readonly List<GuideStep> _guide = new List<GuideStep>();
        private string? _toast;
        private float _toastUntil;
        private float _lastClearSound = -1f;

        public LevelScreen(DesignApp app, int levelNumber)
        {
            _app = app;
            Level = Math.Max(1, levelNumber);
            ContentSet content = app.Content;
            LevelDefinition definition = content.GetLevel(app.Resolve(Level));
            Session = LevelSession.Load(definition, content.GetPicture(definition.Picture), new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget));
            RefreshSpeed();

            // The level's clearing style (spec 005 FR-038): the free pair by level, or the chosen bought one.
            Animator.Style = app.Meta.Clearing.StyleFor(Level);
            Animator.Arrived += OnArrived;
            Animator.Shown += OnShown;
            Animator.Reset(Session.View);
            ShowIntro();
        }

        public int Level { get; }

        public LevelSession Session { get; }

        public LevelAnimator Animator { get; } = new LevelAnimator();

        public PlaytestMeta Meta => _app.Meta;

        /// <summary>The booster whose target the player is choosing (Return: a slot; Bloom Burst: a tile).</summary>
        public Recovery? Targeting { get; private set; }

        /// <summary>The jam sheet is hidden while a recovery started from it is choosing its target.</summary>
        public bool JamHidden { get; private set; }

        /// <summary>The ad rescue was used in this attempt (once per attempt, FR-048).</summary>
        public bool RescueUsed { get; private set; }

        public WinPayout? Payout { get; private set; }

        /// <summary>The last booster used and when (animation time), for its effect (fx.shuffle_swirl, fx.burst).</summary>
        public (BoosterKind Kind, float At)? LastBooster { get; private set; }

        public DemoCard? Demo { get; set; }

        public float DemoOpenedAt { get; private set; }

        /// <summary>
        /// The guided spotlight's current step (spec 005 FR-035, <see cref="GuideTour"/>): Level 1's entry and first tap,
        /// the blocked entry, a booster's forced and free first use. Null when none shows.
        /// </summary>
        public GuideStep? Guide => _guide.Count > 0 ? _guide[0] : null;

        /// <summary>When the current guided step showed (for its fade, ring and hand).</summary>
        public float GuideOpenedAt { get; private set; }

        /// <summary>The pod the guided first tap points at.</summary>
        public string? GuidePod { get; private set; }

        /// <summary>
        /// The booster uses of this attempt that were a guided demo's free use (spec 001 FR-042 as amended on 2026-10-05):
        /// they take no charge and keep the clean-clear bonus.
        /// </summary>
        public int DemoBoosterUses { get; private set; }

        /// <summary>Where each unlocked booster's tile was drawn last frame (the guided spotlight lights it).</summary>
        public Dictionary<BoosterKind, Box> BoosterBoxes { get; } = new Dictionary<BoosterKind, Box>();

        /// <summary>The win card is closed and the milestone card shows (frame 16).</summary>
        public bool ShowingMilestone { get; private set; }

        /// <summary>When the end card (win or jam) first showed, for its rise and pop.</summary>
        public float EndShownAt { get; set; } = -1f;

        /// <summary>
        /// Where each tray pod's sticker tile was drawn last frame, in its column of the tray's grid (flights to a slot start
        /// there, and a returned pod's flight lands there).
        /// </summary>
        public Dictionary<string, Box> PodBoxes { get; } = new Dictionary<string, Box>(StringComparer.Ordinal);

        /// <summary>The tray's pods sliding between the rows of their columns (presentation only, <see cref="Design.TrayMotion"/>).</summary>
        public TrayMotion TrayMotion { get; } = new TrayMotion();

        /// <summary>Where each committed pod's tile was when it was tapped: its flight to the slot grows from that size.</summary>
        public Dictionary<string, Box> LaunchBoxes { get; } = new Dictionary<string, Box>(StringComparer.Ordinal);

        /// <summary>Where each slot was drawn last frame.</summary>
        public Box?[] SlotBoxes { get; } = new Box?[WaitingSlots.Capacity];

        /// <summary>The board's cell size and origin, for walkers and flights.</summary>
        public (float Ox, float Oy, float Cell, int Height) Board { get; set; }

        /// <summary>This frame's clearing-style items (<see cref="ClearPainter"/>), in cell units.</summary>
        public FxList Fx { get; } = new FxList();

        /// <summary>The cells whose tile the clearing style holds this frame (drawn as ground under it).</summary>
        public HashSet<CellPos> Held { get; } = new HashSet<CellPos>();

        /// <summary>This frame's just-cleared tiles, for the style's restore and Blossom's swaying neighbours.</summary>
        public List<ClearFade> ClearFades { get; } = new List<ClearFade>();

        /// <summary>This frame's slot plates, where the clearing style's tiles go.</summary>
        public IReadOnlyList<Box> FxSlots { get; private set; } = Array.Empty<Box>();

        public bool Won => Session.Status == LevelStatus.Won;

        public bool Blocked => Session.Status == LevelStatus.Jammed || Session.Status == LevelStatus.Stuck;

        /// <summary>How long the jam sheet's rise and settle animate (seconds after it shows).</summary>
        public const float EndCardSeconds = 2.3f;

        /// <summary>
        /// How long the win and milestone cards' celebration animates after the card shows (rays turning, petals falling,
        /// Next breathing). Then the screen holds still until the next input, so a card left open does not keep redrawing
        /// the whole level.
        /// </summary>
        public const float CelebrationSeconds = 8f;

        /// <summary>
        /// Whether the win or milestone card's last frame showed the level's animated hero (spec 005 FR-028,
        /// <see cref="EndCards"/>): its idle loops for as long as the card shows, so the screen keeps redrawing.
        /// </summary>
        public bool HeroMoving { get; set; }

        /// <summary>
        /// Whether the screen still moves: animations, the tray's pods sliding, a toast, a demo opening, booster targeting,
        /// and the end cards (the jam's rise for <see cref="EndCardSeconds"/>; the win and milestone cards' celebration for
        /// <see cref="CelebrationSeconds"/>, and for as long as they show an animated hero).
        /// </summary>
        public bool NeedsFrames => !Animator.Idle || TrayMotion.Moving(Animator.Now) || (LastBooster.HasValue && Animator.Now - LastBooster.Value.At < 0.7f) || (_toast != null && _app.Now < _toastUntil) || (EndShownAt >= 0f && (_app.Now - EndShownAt < (Won ? CelebrationSeconds : EndCardSeconds) || (Won && HeroMoving))) || Targeting.HasValue || Guide != null || (Demo != null && _app.Now - DemoOpenedAt < 0.3f);

        /// <summary>
        /// Whether only the win's own motion moves the screen (the win or milestone card is open and every other animation
        /// is done): a host may then redraw at a lower frame rate to save battery.
        /// </summary>
        public bool OnlyCelebrating => Won && EndShownAt >= 0f && _app.Now - EndShownAt >= EndCardSeconds && Animator.Idle && Targeting == null && Demo == null && Guide == null && (_toast == null || _app.Now >= _toastUntil);

        public void Advance(float dt) => Animator.Advance(dt, Session.View);

        public string? ToastText => _toast != null && _app.Now < _toastUntil ? _toast : null;

        public void Toast(string message)
        {
            _toast = message;
            _toastUntil = _app.Now + 1.6f;
        }

        // ---- Before play ----

        /// <summary>
        /// At most one demo before play (FR-031, FR-042, FR-059, FR-071): the guided spotlights first (Level 1's entry and
        /// first tap, a booster's forced free use, the blocked entry; spec 005 FR-035), else a mechanic's or variant's card.
        /// </summary>
        private void ShowIntro()
        {
            IReadOnlyList<GuideStep> guide = GuideTour.AtStart(Level, false, Meta.HasSeenDemo, Meta.Economy.IsUnlocked, Session);
            if (guide.Count > 0)
            {
                StartGuide(guide);
                return;
            }

            foreach (string unlockId in LevelMechanics.UnlocksUsed(Session.Definition, Session.Picture))
            {
                if (!unlockId.StartsWith("mechanic.", StringComparison.Ordinal) || Meta.HasSeenDemo(unlockId) || !Meta.Progression.IsUnlocked(unlockId))
                {
                    continue;
                }

                IReadOnlyList<string> lines = Lines("demo." + unlockId.Substring("mechanic.".Length));
                if (lines.Count > 0)
                {
                    OpenDemo(new DemoCard(unlockId, lines, Array.Empty<VariantId>(), false));
                    return;
                }
            }

            var variants = new List<VariantId>();
            foreach (PodDef pod in Session.Definition.Pods)
            {
                if (!variants.Contains(pod.Variant))
                {
                    variants.Add(pod.Variant);
                }
            }

            foreach (VariantId variant in variants)
            {
                VariantInfo info = VariantCatalog.Default.Get(variant);
                if (info.Status == VariantStatus.Expansion && !Meta.HasSeenDemo("demo.variant." + variant.Key))
                {
                    OpenDemo(new DemoCard("demo.variant." + variant.Key, new[] { PlaytestText.T("demo.new_variant") }, new[] { variant }, false));
                    return;
                }
            }

            if (!Meta.HasSeenDemo("demo.siblings"))
            {
                var byFamily = new Dictionary<Family, VariantId>();
                foreach (VariantId variant in variants)
                {
                    Family family = VariantCatalog.Default.Get(variant).Family;
                    if (byFamily.TryGetValue(family, out VariantId other))
                    {
                        OpenDemo(new DemoCard("demo.siblings", new[] { PlaytestText.T("demo.exact_symbol") }, new[] { other, variant }, true));
                        return;
                    }

                    byFamily[family] = variant;
                }
            }
        }

        private void StartGuide(IReadOnlyList<GuideStep> steps)
        {
            _guide.Clear();
            _guide.AddRange(steps);
            GuidePod = GuideTour.FirstTapPod(Session);
            GuideOpenedAt = _app.Now;
        }

        /// <summary>
        /// The guided spotlight goes on to its next step (a tap anywhere on a step that is not forced, or the forced step's
        /// action done); a demo is marked seen when its last step ends.
        /// </summary>
        public void NextGuideStep()
        {
            if (_guide.Count == 0)
            {
                return;
            }

            GuideStep done = _guide[0];
            _guide.RemoveAt(0);
            if (_guide.Count == 0 || _guide[0].DemoId != done.DemoId)
            {
                Meta.MarkDemoSeen(done.DemoId);
            }

            GuideOpenedAt = _app.Now;
            if (!done.Forced)
            {
                _app.Sound.Play(SoundCue.Click);
            }
        }

        /// <summary>Ends the guided spotlight at once (tests and previews): its demos count as seen.</summary>
        public void SkipGuide()
        {
            while (_guide.Count > 0)
            {
                Meta.MarkDemoSeen(_guide[0].DemoId);
                _guide.RemoveAt(0);
            }
        }

        /// <summary>
        /// Return's guided demo waits until a pod waits in a slot after Return's unlock (Level 6: after the first tap),
        /// once the board has settled (<see cref="GuideTour.WhenSettled"/>).
        /// </summary>
        private void GuideWhenSettled()
        {
            if (Guide != null || Demo != null || Targeting != null || !Animator.Idle)
            {
                return;
            }

            IReadOnlyList<GuideStep> steps = GuideTour.WhenSettled(false, Meta.HasSeenDemo, Meta.Economy.IsUnlocked, Session);
            if (steps.Count > 0)
            {
                StartGuide(steps);
            }
        }

        /// <summary>The recovery a booster is (its targeting, its jam choice).</summary>
        public static Recovery RecoveryOf(BoosterKind kind)
        {
            foreach ((BoosterKind k, Recovery recovery, string _) in Boosters)
            {
                if (k == kind)
                {
                    return recovery;
                }
            }

            return Recovery.ExtraSlot;
        }

        private void OpenDemo(DemoCard demo)
        {
            Demo = demo;
            DemoOpenedAt = _app.Now;
        }

        /// <summary>A demo's lines: <c>key</c>, or <c>key.1</c> and <c>key.2</c>.</summary>
        private static IReadOnlyList<string> Lines(string key)
        {
            if (PlaytestText.Has(key))
            {
                return new[] { PlaytestText.T(key) };
            }

            var lines = new List<string>();
            for (int i = 1; PlaytestText.Has(key + "." + i); i++)
            {
                lines.Add(PlaytestText.T(key + "." + i));
            }

            return lines;
        }

        public void CloseDemo()
        {
            if (Demo != null)
            {
                Meta.MarkDemoSeen(Demo.Id);
                Demo = null;
                _app.Sound.Play(SoundCue.Click);
            }
        }

        // ---- Commands ----

        public void Tap(string podId)
        {
            if (Targeting != null || Demo != null || (Guide != null && !(Guide.Kind == GuideKind.FirstTap && podId == GuidePod)))
            {
                return;
            }

            var tap = new TapPod(podId);
            CommandCheck check = Session.Check(tap);
            if (!check.IsAllowed)
            {
                _app.Sound.Play(SoundCue.Refused);
                Toast(RefusalText(check.Reason));
                return;
            }

            // A pod goes in only to a slot that shows no pod (the owner, 2026-10-05; spec 001 FR-070 as amended): the rules
            // free a finished pod's slot at once, but the player waits until it is empty on screen, so quick taps cannot
            // stack pods behind the ones still working. The rules never see a refused tap.
            int group = Session.View.ConnectedGroup(podId).Count;
            if (Animator.FreeOnScreen(Session.View) < group)
            {
                _app.Sound.Play(SoundCue.Refused);
                Toast(RefusalText(group > 1 ? RejectReason.NotEnoughSlotsForGroup : RejectReason.NoFreeSlot));
                return;
            }

            var before = new Dictionary<string, (int Count, VariantId? Variant, float X, float Y)>(StringComparer.Ordinal);
            var members = new List<string>(Session.View.ConnectedGroup(podId)) { podId };
            foreach (string member in members)
            {
                PodInfo info = Session.View.Pod(member);
                Box? box = PodBoxes.TryGetValue(member, out Box b) ? b : (Box?)null;
                before[member] = (info.Remaining, info.Variant, box?.CenterX ?? float.NaN, box?.CenterY ?? float.NaN);
                if (box.HasValue)
                {
                    LaunchBoxes[member] = box.Value;
                }
            }

            CommandResult result = Session.Apply(tap);
            _app.Sound.Play(SoundCue.Tap);
            if (Guide != null && Guide.Kind == GuideKind.FirstTap)
            {
                NextGuideStep();
            }

            Animator.Tapped(result, Session.View, before);
            AfterCommand();
        }

        /// <param name="confirmed">The purchase confirmation's Buy already answered (spec 005 FR-040): a use without charges
        /// buys one for Petals only then; it asks first otherwise.</param>
        public void UseBooster(BoosterKind kind, Command command, bool free = false, bool confirmed = false)
        {
            // A guided demo's forced use is free: the unlock's free charge stays (spec 001 FR-042 as amended on 2026-10-05).
            bool demo = Guide != null && Guide.Booster == kind && (Guide.Kind == GuideKind.Booster || Guide.Kind == GuideKind.BoosterTarget);
            if (Guide != null && !demo)
            {
                return;
            }

            free |= demo;
            Targeting = null;
            CommandCheck check = Session.Check(command);
            if (!check.IsAllowed)
            {
                Toast(PlaytestText.T("gameplay.booster_useless"));
                return;
            }

            if (!free && !confirmed && Meta.Economy.Charges(kind) == 0 && Meta.Economy.CanAfford(kind))
            {
                // No charge left: the use buys one for Petals, so the purchase confirmation asks first and the booster is
                // bought and used only on its Buy (spec 005 FR-040); a cancel buys nothing and brings the jam card back.
                string id = EndCards.IdOf(kind);
                _app.ConfirmPurchase(
                    PurchaseOffer.ForPetals(EndCards.BoosterName(kind), Meta.Economy.Price(kind)),
                    (q, well) => Kit.BoosterIcon(q, id, well.Inset(well.Width * 0.1f)),
                    () => UseBooster(kind, command, confirmed: true),
                    () => JamHidden = false);
                return;
            }

            if (!free && !Meta.Economy.TryTakeCharge(kind))
            {
                Toast(PlaytestText.T("gameplay.not_enough_petals"));
                return;
            }

            Animator.Flush(Session.View);

            // Return: the pod flies back from the slot it shows in (maybe not its slot in the rules, LevelAnimator.Place).
            int from = command is UseReturn aimed && Session.View.PodInSlot(aimed.SlotIndex) is string aimedPod ? Animator.PlaceOf(aimedPod) : -1;
            CommandResult result = Session.Apply(command);
            _app.Sound.Play(SoundCue.Booster);
            Animator.Boosted(result, Session.View);
            if (command is UseReturn back)
            {
                // Return: the pod flies back from its slot and lands on top of its column, which slides down for it.
                foreach (GameEvent e in result.Events)
                {
                    if (e is PodReturned returned)
                    {
                        Animator.Flights.Add(new Flight(float.NaN, float.NaN, from >= 0 ? from : back.SlotIndex, PodPainter.Shown(Session.View.Pod(returned.PodId)), Animator.Now, true, returned.PodId));
                    }
                }
            }

            LastBooster = (kind, Animator.Now);
            EndShownAt = -1f;
            if (demo)
            {
                // Seen at once, so leaving the level now never gives the free use again.
                DemoBoosterUses++;
                Meta.MarkDemoSeen(Guide!.DemoId);
                NextGuideStep();
            }

            AfterCommand();
        }

        public void PressBooster(BoosterKind kind, Recovery recovery)
        {
            bool guided = Guide != null && Guide.Kind == GuideKind.Booster && Guide.Booster == kind;
            if (Demo != null || (Guide != null && !guided))
            {
                return;
            }

            if (Targeting == recovery)
            {
                Targeting = null;
                if (Blocked)
                {
                    JamHidden = false;
                }

                return;
            }

            JamHidden = false;
            switch (recovery)
            {
                case Recovery.ExtraSlot:
                    UseBooster(kind, new UseExtraSlot());
                    break;
                case Recovery.Shuffle:
                    UseBooster(kind, new UseShuffle());
                    break;
                default:
                    if (!Meta.Economy.CanAfford(kind))
                    {
                        Toast(PlaytestText.T("gameplay.not_enough_petals"));
                        return;
                    }

                    Targeting = recovery;
                    JamHidden = true;
                    if (guided)
                    {
                        // The guided demo lights the targets and says what to tap.
                        NextGuideStep();
                    }
                    else
                    {
                        Toast(PlaytestText.T(recovery == Recovery.Return ? "gameplay.hint_return" : "gameplay.hint_burst"));
                    }

                    break;
            }
        }

        /// <summary>
        /// The jam rescue: one free use of a booster that resolves the jam, once per attempt (FR-027, FR-048). The
        /// playtest has no ads, so it is granted directly and labelled so.
        /// </summary>
        public (BoosterKind Kind, Command Command)? RescueOffer()
        {
            if (RescueUsed || !Blocked)
            {
                return null;
            }

            bool Usable(BoosterKind kind, Command command) => Meta.Economy.IsUnlocked(kind) && Session.Check(command).IsAllowed;
            if (Usable(BoosterKind.ExtraSlot, new UseExtraSlot()))
            {
                return (BoosterKind.ExtraSlot, new UseExtraSlot());
            }

            if (Usable(BoosterKind.Shuffle, new UseShuffle()))
            {
                return (BoosterKind.Shuffle, new UseShuffle());
            }

            for (int slot = 0; slot < WaitingSlots.Capacity; slot++)
            {
                if (Usable(BoosterKind.Return, new UseReturn(slot)))
                {
                    return (BoosterKind.Return, new UseReturn(slot));
                }
            }

            return null;
        }

        public void UseRescue()
        {
            (BoosterKind Kind, Command Command)? rescue = RescueOffer();
            if (rescue.HasValue)
            {
                RescueUsed = true;
                UseBooster(rescue.Value.Kind, rescue.Value.Command, free: true);
            }
        }

        public void Restart()
        {
            Session.Apply(new Restart());
            RefreshSpeed();
            Animator.Reset(Session.View);
            TrayMotion.Clear();
            Targeting = null;
            JamHidden = false;
            RescueUsed = false;
            DemoBoosterUses = 0;
            EndShownAt = -1f;
            _app.Sound.Play(SoundCue.Click);
        }

        /// <summary>A won level is recorded and paid at once, so closing the app during the animation keeps it (R15).</summary>
        private void AfterCommand()
        {
            RefreshSpeed();
            if (Won && Payout == null)
            {
                Payout = Meta.CompleteLevel(Level, Session.Definition.Difficulty.Class, Session.BoostersUsed - DemoBoosterUses, Session.Definition) ?? new WinPayout(null, null);
            }
        }

        /// <summary>NEXT on the win card: the milestone card when this level had one, else the next level through the lotus iris.</summary>
        public void Next()
        {
            if (!ShowingMilestone && Payout?.Milestone != null)
            {
                ShowingMilestone = true;
                EndShownAt = _app.Now;
                _app.Sound.Play(SoundCue.Click);
                return;
            }

            _app.NextLevel();
        }

        private void OnArrived(TileCleared clear)
        {
            if (Animator.Now - _lastClearSound > 0.045f)
            {
                _lastClearSound = Animator.Now;
                _app.Sound.Play(SoundCue.Clear);
            }
        }

        private void OnShown(GameEvent e)
        {
            switch (e)
            {
                case PodCompleted:
                    _app.Sound.Play(SoundCue.PodDone);
                    break;
                case KeyCollected:
                    _app.Sound.Play(SoundCue.Key);
                    break;
                case SpecialTriggered:
                    _app.Sound.Play(SoundCue.Special);
                    break;
                case LevelWon:
                    _app.Sound.Play(SoundCue.Win);
                    break;
                case LevelJammed:
                case LevelStuck:
                    _app.Sound.Play(SoundCue.Jam);
                    break;
            }
        }

        public void ToggleSpeed()
        {
            Meta.Save.Settings.Speed2x = !Meta.Save.Settings.Speed2x;
            Meta.Persist();
            RefreshSpeed();
        }

        /// <summary>
        /// The animation's speed after every command: fast forward (<see cref="PlaySpeed.Fast"/>, 3×) when the player chose
        /// it (FR-069 as amended on 2026-10-06), and also while no pod can be tapped (the owner, 2026-10-04: every pod
        /// picked, only the animation left), so the rest plays fast; the speed pill is lit then (it shows
        /// <see cref="LevelAnimator.Speed"/>). Animation only: never an outcome. The saved choice stays as the player set it.
        /// </summary>
        public void RefreshSpeed() => Animator.Speed = PlaySpeed.Of(Meta.Save.Settings.Speed2x || !CanTapAny());

        /// <summary>Whether the rules allow a tap on any exposed pod (the top of a Source stack).</summary>
        private bool CanTapAny()
        {
            LevelView view = Session.View;
            for (int i = 0; i < view.StackCount; i++)
            {
                IReadOnlyList<string> stack = view.Stack(i);
                if (stack.Count > 0 && Session.Check(new TapPod(stack[0])).IsAllowed)
                {
                    return true;
                }
            }

            return false;
        }

        public static string RefusalText(RejectReason? reason) => reason switch
        {
            RejectReason.NoFreeSlot => PlaytestText.T("refusal.no_free_slot"),
            RejectReason.NotExposed => PlaytestText.T("refusal.not_exposed"),
            RejectReason.Locked => PlaytestText.T("refusal.locked"),
            RejectReason.NotEnoughSlotsForGroup => PlaytestText.T("refusal.group"),
            RejectReason.BoosterNotApplicable => PlaytestText.T("gameplay.booster_useless"),
            _ => PlaytestText.T("refusal.no_free_slot"),
        };

        public static int UnlockLevel(BoosterKind kind)
        {
            foreach ((string id, BoosterKind k) in Client.Services.Economy.EconomyService.BoosterUnlocks)
            {
                if (k == kind)
                {
                    return UnlockRoadmap.Default.LevelOf(id) ?? 0;
                }
            }

            return 0;
        }

        // ---- Drawing ----

        /// <summary>
        /// This frame's reference gameplay regions (spec 005 FR-020, contracts/look.md §6.1): one column of pods per Source
        /// stack (the Garden Entries take no room: they have no arch), the Waiting Slots shown (the
        /// sixth one too once Extra Slot opened it), the booster row once a booster is unlocked, and the Hard or Super
        /// Hard badge under the sign.
        /// </summary>
        public ReferenceGameplayRegions Regions(IPainter p, bool hasBoosters, bool hasBadge)
        {
            LevelView view = Session.View;
            int slots = 0;
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                if (view.SlotStateOf(i) != SlotState.Absent)
                {
                    slots++;
                }
            }

            return ScreenLayout.ReferenceGameplay(p.Width, p.Height, p.Insets, view.StackCount, slots, hasBoosters, hasBadge);
        }

        /// <summary>
        /// The tray (spec 005 FR-012, FR-020, contracts/look.md §6.1), as in the reference: one parchment tray across the
        /// whole screen from the entry strip to past the bottom of the screen, its top corners rounded by
        /// <see cref="ReferenceGameplayRegions.TrayRadius"/>, casting a soft shadow up onto the lawn. Its rows (the slots,
        /// the boosters, the columns of pods) lie on bands of warm parchment parted by the grooves at the separators, so the cream
        /// plates, boxes and wooden pods stand out on it.
        /// </summary>
        public static void TrayPanel(IPainter p, ReferenceGameplayRegions r)
        {
            var cuts = new List<float>();
            foreach (Box line in new[] { r.SeparatorTop, r.SeparatorBottom })
            {
                if (!line.IsEmpty)
                {
                    cuts.Add(line.CenterY);
                }
            }

            TrayPanel(p, r.Tray, r.TrayRadius, r.W, cuts);
        }

        /// <summary>
        /// A tray filling <paramref name="tray"/> (its bottom corners fall below it, off the screen): the frame and one band
        /// per row, parted by a groove at each of <paramref name="cuts"/> (screen y). <paramref name="w"/> is the safe
        /// width its sizes scale by.
        /// </summary>
        public static void TrayPanel(IPainter p, Box tray, float radius, float w, IReadOnlyList<float> cuts)
        {
            var frame = new Box(tray.Left, tray.Top, tray.Right, tray.Bottom + radius);
            TrayFrame(p, frame, radius, w);
            float margin = w * TrayMargin;
            float groove = Math.Max(2f, w * TrayGroove);
            float top = frame.Top + margin;
            for (int i = 0; i <= cuts.Count; i++)
            {
                float bottom = i < cuts.Count ? cuts[i] - (groove / 2f) : frame.Bottom;
                float corner = i == 0 ? radius - margin : w * TrayBandRadius;
                TrayBand(p, new Box(frame.Left + margin, top, frame.Right - margin, bottom), corner, w);
                top = i < cuts.Count ? cuts[i] + (groove / 2f) : top;
            }
        }

        /// <summary>The tray's frame showing round its bands, as a share of the safe width.</summary>
        public const float TrayMargin = 0.012f;

        /// <summary>The groove between two bands of the tray, as a share of the safe width.</summary>
        public const float TrayGroove = 0.009f;

        /// <summary>The corner radius of the tray's inner bands, as a share of the safe width.</summary>
        public const float TrayBandRadius = 0.03f;

        /// <summary>
        /// The tray's frame (<c>mat.parchment</c>): the deep parchment seen in the grooves between its bands
        /// (<c>parchment.edge</c> deepened toward <c>parchment.line</c>), a dark outline and a soft shadow rising onto
        /// the lawn above it.
        /// </summary>
        public static void TrayFrame(IPainter p, Box box, float radius, float w)
        {
            p.Mark("mat.parchment");
            float r = Math.Min(radius, box.Height / 2f);
            float grow = w * 0.01f;
            for (int k = 3; k >= 1; k--)
            {
                float g = grow * k;
                p.FillRound(new Box(box.Left - g, box.Top - g, box.Right + g, box.Bottom), r + g, C.GardenShadow.WithAlpha(0.07f));
            }

            float line = Math.Max(2f, w * 0.004f);
            p.FillRound(box, r, C.ParchmentEdge.Mix(C.ParchmentLine, 0.55f));
            p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, C.ParchmentLine.Darken(0.25f));
        }

        /// <summary>
        /// One band of the tray's parchment (<c>mat.parchment</c>): a board a little deeper than a card's parchment, so the
        /// cream plates and boxes stand out on it as in the reference (<c>parchment.well</c> with light wood at the top,
        /// with <c>parchment.edge</c> at the bottom), its edges aged darker in fine steps, a light bevel along its top
        /// (the light line under each groove) and a thin <c>parchment.line</c> outline.
        /// </summary>
        public static void TrayBand(IPainter p, Box box, float radius, float w)
        {
            p.Mark("mat.parchment");
            float r = Math.Max(0f, Math.Min(radius, box.Height / 2f));
            p.FillRoundGradient(box, r, C.ParchmentWell.Mix(C.WoodLight, 0.55f), C.ParchmentEdge.Mix(C.ParchmentWell, 0.4f));
            float step = w * 0.0024f;
            for (int k = 0; k < 10; k++)
            {
                float inset = step * k;
                p.StrokeRound(box.Inset(inset), Math.Max(0f, r - inset), step * 1.6f, C.ParchmentLine.WithAlpha(0.16f * (1f - (k / 10f))));
            }

            // The light bevel along the top edge, fading out round the upper corners (no hook down the sides).
            float bevel = w * 0.003f;
            float[] reach = { r * 0.5f, r, r + (w * 0.006f) };
            float[] alphas = { 0.8f, 0.5f, 0.25f };
            float top = box.Top;
            for (int k = 0; k < reach.Length; k++)
            {
                // Each band's clip starts where the previous one stopped, so the bevel steps down in strength.
                p.PushClip(new Box(box.Left, top, box.Right, box.Top + reach[k]));
                p.StrokeRound(box.Inset(bevel), Math.Max(0f, r - bevel), bevel, C.ParchmentTop.WithAlpha(alphas[k]));
                p.PopClip();
                top = box.Top + reach[k];
            }

            float edge = Math.Max(1f, w * 0.002f);
            p.StrokeRound(box.Inset(edge / 2f), Math.Max(0f, r - (edge / 2f)), edge, C.ParchmentLine.WithAlpha(0.55f));
        }

        /// <summary>
        /// A groove line across a component sheet (frames 12–14), as between the tray's bands: a line of the frame's deep
        /// parchment with a light <c>parchment.top</c> line under it. Nothing for an empty box.
        /// </summary>
        public static void Separator(IPainter p, Box line)
        {
            if (line.IsEmpty)
            {
                return;
            }

            float h = line.Height;
            p.FillRound(line.Offset(0f, h * 0.9f), h / 2f, C.ParchmentTop.WithAlpha(0.95f));
            p.FillRound(line, h / 2f, C.ParchmentEdge.Mix(C.ParchmentLine, 0.55f));
        }

        public void Draw(IPainter p)
        {
            // A level that ends under a guided step (its last "kept" step) leaves the end cards alone.
            if (Guide != null && (Won || Blocked))
            {
                SkipGuide();
            }

            if (DrawEndScreen(p))
            {
                // The full-screen win or milestone replaced the gameplay (spec 005 FR-023).
                return;
            }

            LevelView view = Session.View;
            bool hasBoosters = false;
            foreach ((BoosterKind kind, Recovery _, string _) in Boosters)
            {
                hasBoosters |= Meta.Economy.IsUnlocked(kind);
            }

            (string Text, Rgba Color, string Slot)? badge = Visuals.DifficultyBadge(Session.Definition.Difficulty.Class);
            if (badge.HasValue && !Meta.Progression.IsUnlocked(Session.Definition.Difficulty.Class == DifficultyClass.Hard ? "profile.hard" : "profile.super_hard"))
            {
                badge = null;
            }

            // The reference layout (spec 005 FR-020, contracts/look.md §6.1): the top bar, the board wide in its stone
            // border on the lawn, the thin entry strip of lawn under it, and one parchment tray to the bottom of the
            // screen with the slots, the boosters and the columns of pods.
            ReferenceGameplayRegions r = Regions(p, hasBoosters, badge.HasValue);
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, Level);

            // Top bar (spec 005 §3.3, §6.1): the cream Pause squircle, the level on a wide wooden sign with ivy, and the
            // cream speed pill. It stays while the win and milestone cards show, under their scrim and celebration, and
            // Pause and the speed pill keep their taps (see below; spec 005 FR-002).
            Action openPause = () => _app.OpenOverlay(Overlay.Pause);
            Box pause = r.Pause;
            Kit.RoundButton(p, pause.CenterX, pause.CenterY, pause.Height, "ui.pause", openPause, squircle: true);
            p.Mark("ui.pause");
            Kit.LevelPill(p, r.Sign, PlaytestText.F("common.level", NumberText.Group(Level)), Session.Definition.Difficulty.Class == DifficultyClass.SuperHard && badge.HasValue);
            Box speed = r.Speed;
            Kit.SpeedPill(p, speed, Animator.Speed > 1f, ToggleSpeed);
            if (badge.HasValue)
            {
                // HARD or SUPER HARD under the sign.
                Kit.Badge(p, r.Badge, badge.Value.Text, badge.Value.Color, badge.Value.Slot);
            }

            BoardLayout board = r.FitBoard(view.Width, view.Height);
            FxSlots = r.Slots;
            BoardPainter.Draw(p, board, this);
            TrayPanel(p, r);
            SlotPainter.DrawRow(p, r.Slots, this);
            if (hasBoosters)
            {
                BoosterBarPainter.Draw(p, r.Boosters, this);
            }

            PodPainter.DrawColumns(p, r, this);
            PodPainter.DrawFlights(p, this);

            // The clearing style's flights to the slots (bubbles, fireflies, tossed tiles, sparkles) and the slots' rings.
            ClearPainter.Draw(p, this, FxLayer.Over);

            Box over = board.Outer;
            string? toast = ToastText;
            if (toast != null)
            {
                Kit.Toast(p, over, toast);
            }

            // The guided spotlight over the gameplay (spec 005 FR-035): Return's demo starts once a pod waits.
            GuideWhenSettled();
            if (Guide != null)
            {
                GuidePainter.Draw(p, this, board, r);
            }

            if (Animator.Settled && (Won || (Blocked && !JamHidden)))
            {
                if (EndShownAt < 0f)
                {
                    EndShownAt = _app.Now;
                }

                float since = _app.Now - EndShownAt;
                if (Won && ShowingMilestone)
                {
                    EndCards.Milestone(p, this, since);
                }
                else if (Won)
                {
                    EndCards.Win(p, this, since);
                }
                else
                {
                    EndCards.Jam(p, this, since);
                }

                if (Won)
                {
                    // The win and milestone cards' scrim takes every tap off the card; Pause and the speed pill take theirs
                    // through it, as in the Unity client (FR-002), so Home, Restart and Settings stay reachable from the
                    // win. (The jam sheet's light scrim takes no taps, so the top bar works above it anyway.)
                    p.Hit(Kit.Touch(p, pause), openPause);
                    p.Hit(Kit.Touch(p, speed), ToggleSpeed);
                }
            }

            if (Demo != null)
            {
                EndCards.Demo(p, this, _app.Now - DemoOpenedAt);
            }
        }

        /// <summary>
        /// The full-screen win and milestone (spec 005 FR-023, <see cref="EndCards"/>): once the win has faded in over the
        /// gameplay (<see cref="EndCards.WinFadeSeconds"/>, drawn by <see cref="Draw"/>'s end cards), the celebration
        /// replaces the whole gameplay screen, its top bar included; the milestone that follows it is full screen at once.
        /// Returns whether it drew the screen.
        /// </summary>
        private bool DrawEndScreen(IPainter p)
        {
            if (!Won || !Animator.Settled || EndShownAt < 0f || (!ShowingMilestone && _app.Now - EndShownAt < EndCards.WinFadeSeconds))
            {
                return false;
            }

            float since = _app.Now - EndShownAt;
            if (ShowingMilestone)
            {
                EndCards.Milestone(p, this, since);
            }
            else
            {
                EndCards.Win(p, this, since);
            }

            if (Demo != null)
            {
                EndCards.Demo(p, this, _app.Now - DemoOpenedAt);
            }

            return true;
        }

        internal DesignApp App => _app;
    }
}
