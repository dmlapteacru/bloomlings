using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The level screen of a round level (spec 006 US1, US2; T026, T027, T047, T048): the round rules play on the live
    /// timeline (<see cref="RoundPlay"/>, the kit's, shared with the check and Unity) and the screen draws it in the game's
    /// look (<see cref="RoundPainter"/>).
    /// <list type="bullet">
    /// <item><description>The display clock runs at real time × the speed: ×1, the speed-up ×2 (the speed pill, saved in
    /// the settings), and Auto ×2 once every column is empty and no bridge is left to choose (FR-011), when the whole HUD
    /// fades away and only the board stays (FR-044). It stands still while a card, a guided step, Pause or a booster's
    /// targeting is open.</description></item>
    /// <item><description>Taps, bridge choices and boosters are committed at the current model time; the rules answer, and a
    /// refusal shows the rules' reason (the link partner's message pulses the partner, FR-008).</description></item>
    /// <item><description>Win and jam come from the rules' status once the board has settled; the win pays at once, the jam
    /// card offers the boosters the rules accept now and the free rescue (FR-012).</description></item>
    /// <item><description>The introduction card of the level's mechanic (first meeting) or its reminder (second) opens at the
    /// start (FR-045); the guided spotlights of spec 005 keep their steps (<see cref="GuideTour"/>'s round overloads).</description></item>
    /// </list>
    /// It holds no rules: every decision is the round core's. Engine-free.
    /// </summary>
    public sealed class RoundLevelScreen : ILevelEnd
    {
        /// <summary>How long the HUD takes to fade away when Auto starts (real seconds).</summary>
        public const float HudFadeSeconds = 0.35f;

        /// <summary>The longest frame step the clock takes (a stalled frame does not jump the walkers).</summary>
        public const float MaxStep = 0.1f;

        private readonly DesignApp _app;
        private readonly List<GuideStep> _guide = new List<GuideStep>();
        private readonly ClearLadder _ladder = new ClearLadder();
        private readonly string[] _podKeys;
        private readonly float[] _revealReal;
        private string? _toast;
        private float _toastUntil;
        private bool _endSounded;
        private float _autoSince = -1f;

        public RoundLevelScreen(DesignApp app, int levelNumber, RoundLevel level, RoundPicture? picture)
        {
            _app = app;
            Level = Math.Max(1, levelNumber);
            Round = level ?? throw new ArgumentNullException(nameof(level));
            Picture = picture;
            Play = new RoundPlay(level);
            Style = app.Meta.Clearing.StyleFor(Level);
            _podKeys = new string[level.Pods.Count];
            _revealReal = new float[level.Pods.Count];
            PodBoxes = new Box?[level.Pods.Count];
            LaunchBoxes = new Box?[level.Pods.Count];
            for (int p = 0; p < _podKeys.Length; p++)
            {
                _podKeys[p] = "p" + p.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _revealReal[p] = float.NegativeInfinity;
            }

            ShowIntro();
        }

        public int Level { get; }

        /// <summary>The round level's definition.</summary>
        public RoundLevel Round { get; }

        /// <summary>Its approved picture (the win's finished picture), when the source carries it.</summary>
        public RoundPicture? Picture { get; }

        /// <summary>The rules on the live timeline, the display clock and the animator.</summary>
        public RoundPlay Play { get; private set; }

        public RoundAnimator Animator => Play.Animator;

        /// <summary>The level's clearing style (spec 005 FR-038): Munchers, or the chosen bought one.</summary>
        public ClearStyle Style { get; }

        public PlaytestMeta Meta => _app.Meta;

        public DesignApp App => _app;

        /// <summary>The screen's real-time clock (cards, toasts, glows: never sped up).</summary>
        public float UiNow => _app.Now;

        /// <summary>The symbols mode (spec 006 FR-047, the save's setting).</summary>
        public bool Symbols => Meta.Save.Settings.Symbols;

        /// <summary>The difficulty class as the game's meta knows it (badge, reward).</summary>
        public DifficultyClass Difficulty => Round.Class switch
        {
            RoundClass.Hard => DifficultyClass.Hard,
            RoundClass.SuperHard => DifficultyClass.SuperHard,
            _ => DifficultyClass.Normal,
        };

        public bool Won => Play.Status == RoundStatus.Won;

        /// <summary>The places jammed or no pod can be tapped, nothing pending (the jam card's moment).</summary>
        public bool Blocked => Play.Status == RoundStatus.Jammed || Play.Status == RoundStatus.Stuck;

        /// <summary>Auto (FR-011): the rest plays at ×2 and the HUD is gone.</summary>
        public bool Auto => Play.Auto && !Won;

        /// <summary>The HUD's opacity: 1, fading to 0 over <see cref="HudFadeSeconds"/> once Auto starts (FR-044).</summary>
        public float HudAlpha => _autoSince < 0f ? 1f : 1f - Visuals.Clamp01((UiNow - _autoSince) / HudFadeSeconds);

        /// <summary>Whether the HUD shows at all this frame.</summary>
        public bool HudShown => HudAlpha > 0.001f;

        /// <summary>The display speed now (presentation only).</summary>
        public int Speed => Play.Speed(Meta.Save.Settings.Speed2x);

        public Recovery? Targeting { get; private set; }

        public bool JamHidden { get; private set; }

        public bool RescueUsed { get; private set; }

        public WinPayout? Payout { get; private set; }

        public RoundIntroCard? Intro { get; private set; }

        public float IntroOpenedAt { get; private set; }

        public GuideStep? Guide => _guide.Count > 0 ? _guide[0] : null;

        public float GuideOpenedAt { get; private set; }

        /// <summary>The pod the guided first tap points at, or -1.</summary>
        public int GuidePod { get; private set; } = -1;

        public int DemoBoosterUses { get; private set; }

        /// <summary>The last booster used and when (display µs), for its effect.</summary>
        public (BoosterKind Kind, long At)? LastBooster { get; private set; }

        /// <summary>The last tap that waited for its link partner (real time): the partner pulses (FR-008, spec 005 FR-043).</summary>
        public (int Pod, float At)? LinkHint { get; private set; }

        public bool ShowingMilestone { get; private set; }

        public float EndShownAt { get; set; } = -1f;

        public bool HeroMoving { get; set; }

        // ---- Where things were drawn last frame (flights, guides) ----

        /// <summary>Per pod, its tile's box in the columns last frame (null when not shown).</summary>
        public Box?[] PodBoxes { get; }

        /// <summary>Per pod, where its tile was when it was tapped: its flight to the place starts there.</summary>
        public Box?[] LaunchBoxes { get; }

        public Dictionary<BoosterKind, Box> BoosterBoxes { get; } = new Dictionary<BoosterKind, Box>();

        /// <summary>The places' plates last frame.</summary>
        public List<Box> PlaceBoxes { get; } = new List<Box>();

        /// <summary>The tray's pods sliding between rows (presentation only).</summary>
        public TrayMotion TrayMotion { get; } = new TrayMotion();

        /// <summary>This frame's clearing-style items, in cell units of the grid inside the frame.</summary>
        public FxList Fx { get; } = new FxList();

        /// <summary>The pod key a tray slide and a sound ladder know a pod by.</summary>
        public string KeyOf(int pod) => _podKeys[pod];

        /// <summary>The display clock in seconds (the clearing styles' clock).</summary>
        public float ShowSeconds => Play.ClockUs / 1_000_000f;

        /// <summary>The reveal glow of a pod now (FR-045), from its reveal in real time.</summary>
        public float RevealGlow(int pod) => RoundLook.RevealGlow(UiNow - _revealReal[pod]);

        /// <summary>Whether the live clock stands still: a card, a guide, Pause, a purchase or a booster's targeting is open.</summary>
        public bool Paused => Intro != null || Guide != null || Targeting.HasValue || _app.Overlays.Count > 0 || (EndShownAt >= 0f && !Won);

        public bool NeedsFrames =>
            (!Paused && (Play.Timeline.Pending || Animator.Busy))
            || TrayMotion.Moving(ShowSeconds)
            || (_toast != null && UiNow < _toastUntil)
            || (LinkHint.HasValue && UiNow - LinkHint.Value.At < PodLinks.HintSeconds)
            || (_autoSince >= 0f && UiNow - _autoSince < HudFadeSeconds)
            || AnyGlow()
            || (EndShownAt >= 0f && (UiNow - EndShownAt < (Won ? LevelScreen.CelebrationSeconds : LevelScreen.EndCardSeconds) || (Won && HeroMoving)))
            || Targeting.HasValue || Guide != null || (Intro != null && UiNow - IntroOpenedAt < 0.3f);

        public bool OnlyCelebrating => Won && EndShownAt >= 0f && UiNow - EndShownAt >= LevelScreen.EndCardSeconds && !Animator.Busy && Intro == null && Guide == null;

        public string? ToastText => _toast != null && UiNow < _toastUntil ? _toast : null;

        public void Toast(string message)
        {
            _toast = message;
            _toastUntil = UiNow + 1.6f;
        }

        /// <summary>A screen prepared ahead starts its cards' clocks when it comes into use.</summary>
        public void RestartIntroAnimation()
        {
            IntroOpenedAt = UiNow;
            GuideOpenedAt = UiNow;
        }

        // ---- The clock ----

        /// <summary>
        /// Moves the live clock on by <paramref name="dt"/> real seconds × the speed (unless paused), plays this step's
        /// sounds, records reveals for their glow, and notes Auto's start and the level's end.
        /// </summary>
        public void Advance(float dt)
        {
            if (!Paused)
            {
                IReadOnlyList<RoundEvent> events = Play.Advance(Math.Max(0f, Math.Min(MaxStep, dt)), Speed);
                Heard(events);
            }

            if (Play.Auto && _autoSince < 0f)
            {
                _autoSince = UiNow;
            }

            if (Guide == null && Intro == null && !Paused)
            {
                GuideWhenReady();
            }

            AfterCommand();
        }

        /// <summary>The sounds of a batch of events (spec 005 FR-042 on the live timeline) and the reveals' real times.</summary>
        private void Heard(IReadOnlyList<RoundEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                RoundEvent e = events[i];
                switch (e.Type)
                {
                    case RoundEventType.Clear:
                        _app.Sound.Collect(Style, _ladder.Next(_podKeys[e.Pod], Math.Max(1, Round.Pods[e.Pod].Count)));
                        break;
                    case RoundEventType.Complete:
                        _app.Sound.Play(SoundCue.PodDone);
                        break;
                    case RoundEventType.Reveal:
                        _revealReal[e.Pod] = UiNow;
                        break;
                }
            }
        }

        private bool AnyGlow()
        {
            for (int p = 0; p < _revealReal.Length; p++)
            {
                if (UiNow - _revealReal[p] < RoundLook.RevealGlowSeconds)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- Before play ----

        /// <summary>
        /// The guided spotlights first (Level 1's entry and first tap, a booster's forced free use, the blocked entry), then
        /// the introduction card of the level's mechanic or its reminder (FR-045).
        /// </summary>
        private void ShowIntro()
        {
            IReadOnlyList<GuideStep> guide = GuideTour.AtStart(Level, Meta.HasSeenDemo, Meta.Economy.IsUnlocked, Play.Timeline);
            if (guide.Count > 0)
            {
                StartGuide(guide);
            }

            Intro = RoundIntroCards.ToShow(Round.Introduction, Meta.HasSeenDemo);
            IntroOpenedAt = UiNow;
        }

        private void StartGuide(IReadOnlyList<GuideStep> steps)
        {
            _guide.Clear();
            _guide.AddRange(steps);
            GuidePod = GuideTour.FirstTapPod(Play.Timeline);
            GuideOpenedAt = UiNow;
        }

        /// <summary>Return's guided demo: once a pod waits in a place with work left and no walker on the way.</summary>
        private void GuideWhenReady()
        {
            if (Targeting != null || Won || Blocked)
            {
                return;
            }

            IReadOnlyList<GuideStep> steps = GuideTour.WhenSettled(Meta.HasSeenDemo, Meta.Economy.IsUnlocked, Play.Timeline);
            if (steps.Count > 0)
            {
                StartGuide(steps);
            }
        }

        /// <summary>The introduction card's button: it is seen, and play starts (or the guide's next step shows).</summary>
        public void CloseIntro()
        {
            if (Intro != null)
            {
                Meta.MarkDemoSeen(Intro.Id);
                Intro = null;
                GuideOpenedAt = UiNow;
                _app.Sound.Play(SoundCue.Click);
            }
        }

        /// <summary>The guided spotlight's next step; a demo is seen when its last step ends.</summary>
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

            GuideOpenedAt = UiNow;
            if (!done.Forced)
            {
                _app.Sound.Play(SoundCue.Click);
            }
        }

        /// <summary>Ends the guided spotlight and the card at once (tests and previews): they count as seen.</summary>
        public void SkipGuide()
        {
            while (_guide.Count > 0)
            {
                Meta.MarkDemoSeen(_guide[0].DemoId);
                _guide.RemoveAt(0);
            }
        }

        // ---- Commands ----

        /// <summary>A tap on the top pod of a column: committed at the current model time, or the rules' refusal shown.</summary>
        public void Tap(int pod)
        {
            if (Targeting != null || Intro != null || Auto || (Guide != null && !(Guide.Kind == GuideKind.FirstTap && pod == GuidePod)))
            {
                return;
            }

            RoundRefusal refusal = Play.Check(RoundAction.Tap(pod));
            if (refusal != RoundRefusal.None)
            {
                _app.Sound.Play(SoundCue.Refused);
                if (refusal == RoundRefusal.PartnerNotExposed)
                {
                    LinkHint = (pod, UiNow);
                }

                if (refusal != RoundRefusal.Finished)
                {
                    Toast(RefusalText(refusal));
                }

                return;
            }

            foreach (int member in Animator.GroupOf(pod))
            {
                LaunchBoxes[member] = PodBoxes[member];
            }

            Play.Commit(RoundAction.Tap(pod));
            Heard(Play.Fresh);
            _app.Sound.Play(SoundCue.Tap);
            if (Guide != null && Guide.Kind == GuideKind.FirstTap)
            {
                NextGuideStep();
            }

            AfterCommand();
        }

        /// <summary>A tap on a bridge site (FR-009): built when the rules accept it.</summary>
        public void BuildBridge(int choice)
        {
            if (Targeting != null || Intro != null || Guide != null)
            {
                return;
            }

            if (Play.Check(RoundAction.BuildBridge(choice)) != RoundRefusal.None)
            {
                _app.Sound.Play(SoundCue.Refused);
                return;
            }

            Play.Commit(RoundAction.BuildBridge(choice));
            _app.Sound.Play(SoundCue.Special);
            AfterCommand();
        }

        /// <summary>The bridge sites the rules let the player build now.</summary>
        public List<int> BuildableBridges() => RoundRules.BridgeChoices(Play.Timeline.State);

        /// <summary>Whether a booster would do something now (FR-012; the bar greys it otherwise).</summary>
        public bool Applicable(BoosterKind kind) => !Won && GuideTour.CanUse(Play.Timeline, kind);

        /// <summary>A booster's tile pressed: Extra Slot and Shuffle at once; Return and Bloom Burst choose their target first.</summary>
        public void PressBooster(BoosterKind kind)
        {
            bool guided = Guide != null && Guide.Kind == GuideKind.Booster && Guide.Booster == kind;
            if (Intro != null || Auto || (Guide != null && !guided))
            {
                return;
            }

            Recovery recovery = LevelScreen.RecoveryOf(kind);
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
            switch (kind)
            {
                case BoosterKind.ExtraSlot:
                    UseBooster(kind, RoundInput.ExtraSlot(0));
                    break;
                case BoosterKind.Shuffle:
                    UseBooster(kind, RoundInput.Shuffle(0));
                    break;
                default:
                    if (!guided && !Meta.Economy.CanAfford(kind))
                    {
                        Toast(PlaytestText.T("gameplay.not_enough_petals"));
                        return;
                    }

                    if (!Applicable(kind))
                    {
                        Toast(PlaytestText.T("gameplay.booster_useless"));
                        return;
                    }

                    Targeting = recovery;
                    JamHidden = true;
                    if (guided)
                    {
                        NextGuideStep();
                    }
                    else
                    {
                        Toast(PlaytestText.T(kind == BoosterKind.Return ? "gameplay.hint_return" : "round.hint_burst"));
                    }

                    break;
            }
        }

        /// <summary>
        /// Uses a booster as a command at the current model time (FR-012): a guided demo's use is free, a use without charges
        /// asks the purchase confirmation first (spec 005 FR-040).
        /// </summary>
        public void UseBooster(BoosterKind kind, RoundInput command, bool free = false, bool confirmed = false, bool prepaid = false)
        {
            bool demo = Guide != null && Guide.Booster == kind && (Guide.Kind == GuideKind.Booster || Guide.Kind == GuideKind.BoosterTarget);
            if (Guide != null && !demo)
            {
                return;
            }

            free |= demo;
            Targeting = null;
            if (Refusal(command) != RoundRefusal.None)
            {
                Toast(PlaytestText.T("gameplay.booster_useless"));
                return;
            }

            if (!free && !confirmed && Meta.Economy.Charges(kind) == 0 && Meta.Economy.CanAfford(kind))
            {
                string id = EndCards.IdOf(kind);
                _app.ConfirmPurchase(
                    PurchaseOffer.ForBooster(PlaytestText.F("store.booster_quantity", EndCards.BoosterName(kind), Client.Services.Economy.EconomyService.PurchaseQuantity(kind)), Meta.Economy.Price(kind), kind.ToString(), buyAndUse: true),
                    (q, well) => Kit.BoosterIcon(q, id, well.Inset(well.Width * 0.1f)),
                    () => { int used = Play.BoostersUsed; UseBooster(kind, command, confirmed: true); return Play.BoostersUsed > used; },
                    () => JamHidden = false, onPurchased: () => UseBooster(kind, command, confirmed: true, prepaid: true));
                return;
            }

            if (!free && !prepaid && !Meta.Economy.TryTakeCharge(kind))
            {
                Toast(PlaytestText.T("gameplay.not_enough_petals"));
                return;
            }

            RoundCommitResult result = Play.Apply(command);
            if (!result.Accepted)
            {
                Toast(PlaytestText.T("gameplay.booster_useless"));
                return;
            }

            Heard(Play.Fresh);
            _app.Sound.Play(SoundCue.Booster);
            LastBooster = (kind, Play.ClockUs);
            EndShownAt = -1f;
            _endSounded = false;
            if (demo)
            {
                DemoBoosterUses++;
                Meta.MarkDemoSeen(Guide!.DemoId);
                NextGuideStep();
            }

            AfterCommand();
        }

        /// <summary>A booster bought from its tile's "+" (the purchase confirmation asks first).</summary>
        public void BuyBooster(BoosterKind kind)
        {
            if (Intro != null || Guide != null || Won || !Meta.Economy.IsUnlocked(kind))
            {
                return;
            }

            Targeting = null;
            JamHidden = false;
            string id = EndCards.IdOf(kind);
            _app.ConfirmPurchase(
                PurchaseOffer.ForBooster(PlaytestText.F("store.booster_quantity", EndCards.BoosterName(kind), Client.Services.Economy.EconomyService.PurchaseQuantity(kind)), Meta.Economy.Price(kind), kind.ToString()),
                (q, well) => Kit.BoosterIcon(q, id, well.Inset(well.Width * .1f)),
                () =>
                {
                    bool bought = Meta.Economy.TryBuy(kind);
                    if (!bought) Toast(PlaytestText.T("gameplay.not_enough_petals"));
                    return bought;
                });
        }

        /// <summary>Return's target: the pod in a place (FR-012: only one with no walker on the way).</summary>
        public void ReturnPod(int pod) => UseBooster(BoosterKind.Return, RoundInput.Return(pod, 0));

        /// <summary>Bloom Burst's target: the material of the tapped cube.</summary>
        public void BurstCell(int cell)
        {
            int material = Animator.MaterialAt(cell);
            if (material < 0)
            {
                return;
            }

            char code = RoundMaterial.All[material].Code;
            if (RoundBoosters.CanBloomBurst(Play.Timeline, code) == RoundRefusal.None)
            {
                UseBooster(BoosterKind.BloomBurst, RoundInput.BloomBurst(code, 0));
            }
        }

        /// <summary>The rules' answer to a booster command now.</summary>
        public RoundRefusal Refusal(RoundInput command) => command.Type switch
        {
            RoundInputType.ExtraSlot => RoundBoosters.CanExtraSlot(Play.Timeline),
            RoundInputType.Shuffle => RoundBoosters.CanShuffle(Play.Timeline),
            RoundInputType.Return => RoundBoosters.CanReturn(Play.Timeline, command.Value),
            RoundInputType.BloomBurst => RoundBoosters.CanBloomBurst(Play.Timeline, command.Code),
            _ => RoundRefusal.UnknownAction,
        };

        /// <summary>The boosters the jam card offers: unlocked, affordable, and accepted by the rules now (FR-012).</summary>
        public List<BoosterKind> JamBoosters()
        {
            var kinds = new List<BoosterKind>();
            foreach ((BoosterKind kind, Recovery _, string _) in LevelScreen.Boosters)
            {
                if (Meta.Economy.IsUnlocked(kind) && Meta.Economy.CanAfford(kind) && Applicable(kind))
                {
                    kinds.Add(kind);
                }
            }

            return kinds;
        }

        /// <summary>The jam rescue: one free booster use that resolves the jam, once per attempt (the playtest has no ads).</summary>
        public (BoosterKind Kind, RoundInput Command)? RescueOffer()
        {
            if (RescueUsed || !Blocked)
            {
                return null;
            }

            if (Meta.Economy.IsUnlocked(BoosterKind.ExtraSlot) && RoundBoosters.CanExtraSlot(Play.Timeline) == RoundRefusal.None)
            {
                return (BoosterKind.ExtraSlot, RoundInput.ExtraSlot(0));
            }

            if (Meta.Economy.IsUnlocked(BoosterKind.Shuffle) && RoundBoosters.CanShuffle(Play.Timeline) == RoundRefusal.None)
            {
                return (BoosterKind.Shuffle, RoundInput.Shuffle(0));
            }

            int pod = GuideTour.ReturnPod(Play.Timeline);
            if (Meta.Economy.IsUnlocked(BoosterKind.Return) && pod >= 0)
            {
                return (BoosterKind.Return, RoundInput.Return(pod, 0));
            }

            return null;
        }

        public void UseRescue()
        {
            (BoosterKind Kind, RoundInput Command)? rescue = RescueOffer();
            if (rescue.HasValue)
            {
                RescueUsed = true;
                UseBooster(rescue.Value.Kind, rescue.Value.Command, free: true);
            }
        }

        public void Restart()
        {
            Play.Restart();
            _ladder.Reset();
            TrayMotion.Clear();
            Targeting = null;
            JamHidden = false;
            RescueUsed = false;
            DemoBoosterUses = 0;
            EndShownAt = -1f;
            _endSounded = false;
            _autoSince = -1f;
            LinkHint = null;
            for (int p = 0; p < _revealReal.Length; p++)
            {
                _revealReal[p] = float.NegativeInfinity;
                LaunchBoxes[p] = null;
            }

            _app.Sound.Play(SoundCue.Click);
        }

        /// <summary>A won level is recorded and paid at once (closing the app during the celebration keeps it).</summary>
        private void AfterCommand()
        {
            if (Won && Payout == null)
            {
                Payout = Meta.CompleteLevel(Level, Difficulty, Play.BoostersUsed - DemoBoosterUses) ?? new WinPayout(null, null);
            }

            if (Play.Settled && (Won || Blocked) && !_endSounded)
            {
                _endSounded = true;
                _app.Sound.Play(Won ? SoundCue.Win : SoundCue.Jam);
            }
        }

        /// <summary>Next on the win: the milestone card when this level had one, else the next level through the lotus iris.</summary>
        public void Next()
        {
            if (!ShowingMilestone && Payout?.Milestone != null)
            {
                ShowingMilestone = true;
                EndShownAt = UiNow;
                _app.Sound.Play(SoundCue.Click);
                return;
            }

            _app.NextLevel();
        }

        public void ToggleSpeed()
        {
            Meta.Save.Settings.Speed2x = !Meta.Save.Settings.Speed2x;
            Meta.Persist();
        }

        /// <summary>The rules' refusal as the player reads it.</summary>
        public static string RefusalText(RoundRefusal refusal) => refusal switch
        {
            RoundRefusal.NotExposed => PlaytestText.T("refusal.not_exposed"),
            RoundRefusal.PartnerNotExposed => PlaytestText.T("refusal.partner_buried"),
            RoundRefusal.PairNeedsTwoSlots => PlaytestText.T("refusal.group"),
            RoundRefusal.BridgeNotReady => PlaytestText.T("round.bridge_not_ready"),
            _ => PlaytestText.T("refusal.no_free_slot"),
        };

        // ---- The end (ILevelEnd) ----

        public (int Width, int Height) PictureSize => Picture != null ? (Picture.Width, Picture.Height) : (Round.Width - 2, Round.Height - 2);

        public void DrawPicture(IPainter p, Box box) => RoundPainter.FinishedPicture(p, box, this);

        public IReadOnlyList<Rgba> ConfettiColors
        {
            get
            {
                var colors = new List<Rgba>();
                foreach (RoundPod pod in Round.Pods)
                {
                    Rgba c = RoundLook.ColorOf(pod.Code);
                    if (!colors.Contains(c))
                    {
                        colors.Add(c);
                    }
                }

                return colors;
            }
        }

        public void Draw(IPainter p) => RoundPainter.Draw(p, this);
    }
}
