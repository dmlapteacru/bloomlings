using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.Gameplay.Rounds
{
    /// <summary>
    /// What a round level needs from the game around it (the Unity client's services, or a test's stand-in): the demos
    /// seen, the boosters' unlocks and charges, the speed-up and symbols settings, and the sounds. Presentation and meta
    /// only; the rules never ask for any of it.
    /// </summary>
    public interface IRoundLevelHost
    {
        bool HasSeenDemo(string demoId);

        void MarkDemoSeen(string demoId);

        bool IsUnlocked(BoosterKind kind);

        bool CanAfford(BoosterKind kind);

        int Charges(BoosterKind kind);

        /// <summary>Takes one charge, or buys the booster's pack with Petals and takes one; false when neither works.</summary>
        bool TryTakeCharge(BoosterKind kind);

        /// <summary>The speed-up (the save's <c>settings.speed2x</c>).</summary>
        bool SpeedUp { get; set; }

        /// <summary>The symbols mode (spec 006 FR-047, the save's <c>settings.symbols</c>).</summary>
        bool Symbols { get; }

        void Sound(SoundCue cue);

        /// <summary>A clearing style's collect, <paramref name="step"/> up its pod's ladder (spec 005 FR-042).</summary>
        void Collect(ClearStyle style, int step);
    }

    /// <summary>What a tap on a pod led to.</summary>
    public enum RoundTapOutcome
    {
        /// <summary>Not taken now (a card, a guided step, Auto, a booster's targeting).</summary>
        Ignored,

        /// <summary>The rules refused it (<see cref="RoundLevelCore.LastRefusal"/> says why).</summary>
        Refused,

        Accepted,
    }

    /// <summary>What a booster press or use led to.</summary>
    public enum RoundBoosterOutcome
    {
        /// <summary>Not taken now (a card, a guided step for another booster, Auto).</summary>
        Ignored,

        /// <summary>Return or Bloom Burst now waits for its target (<see cref="RoundLevelCore.Targeting"/>).</summary>
        Targeting,

        /// <summary>A second press on the waiting booster: its targeting ends.</summary>
        Cancelled,

        /// <summary>The rules would not take it now.</summary>
        Useless,

        /// <summary>No charge and too few Petals.</summary>
        NotEnoughPetals,

        /// <summary>No charge left: the use buys a pack for Petals, so the purchase confirmation must ask first (spec 005 FR-040).</summary>
        NeedsPurchase,

        Used,
    }

    /// <summary>How a place shows (spec 005 §3.7 plates on the round rules).</summary>
    public enum RoundPlaceLook
    {
        Empty,

        /// <summary>The last free place while the level plays: dashed red with "!".</summary>
        Danger,

        /// <summary>A pod whose walkers are on the way or whose material has a cube in reach.</summary>
        Working,

        /// <summary>A pod with work left and no cube of its material in reach: greyed with the hourglass.</summary>
        Stuck,
    }

    /// <summary>
    /// A round level being played in the Unity client (spec 006 US5, T058), the engine-free twin of the playtest's
    /// <c>RoundLevelScreen</c>: it owns the kit's <see cref="RoundPlay"/> (the live timeline, the display clock in whole µs
    /// × the display speed, the animator) and decides what the screen does, with no drawing and no Unity type.
    /// <list type="bullet">
    /// <item><description>The display clock runs at real time × the speed: ×1, the speed-up ×2 (the speed pill, saved in the
    /// settings) and Auto ×2 once every column is empty and no bridge is left to choose (FR-011), when the whole HUD fades away
    /// and taps are ignored (FR-044). It stands still while a card, a guided step, Pause, a purchase or a booster's targeting is
    /// open (<see cref="Paused"/>).</description></item>
    /// <item><description>Taps, bridge choices and boosters are committed at the current model time; a refusal keeps the
    /// rules' reason (<see cref="LastRefusal"/>, <see cref="RefusalKey"/>) and a tap waiting for its link partner pulses the
    /// partner (<see cref="LinkHint"/>, FR-008).</description></item>
    /// <item><description>Win and jam come from the rules' status (<see cref="Won"/>, <see cref="Blocked"/>); the jam card
    /// offers the boosters the rules accept now (<see cref="JamBoosters"/>) and the rescue (<see cref="RescueBooster"/>,
    /// FR-012).</description></item>
    /// <item><description>The guided spotlights of spec 005 keep their steps on the round rules (<see cref="GuideTour"/>'s
    /// round overloads; a demo's use is free), and the level's introduction card or reminder opens first (FR-045).</description></item>
    /// </list>
    /// Rules come only from <see cref="Bloomlings.Core.Rounds"/>. Real time (<see cref="UiNow"/>) drives cards, toasts and
    /// glows only.
    /// </summary>
    public sealed class RoundLevelCore
    {
        /// <summary>How long the HUD takes to fade away when Auto starts (real seconds).</summary>
        public const float HudFadeSeconds = 0.35f;

        /// <summary>The longest frame step the clock takes (a stalled frame does not jump the walkers).</summary>
        public const float MaxStep = 0.1f;

        /// <summary>The boosters in the bar's order.</summary>
        public static readonly BoosterKind[] Boosters = { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst };

        private readonly IRoundLevelHost _host;
        private readonly List<GuideStep> _guide = new List<GuideStep>();
        private readonly ClearLadder _ladder = new ClearLadder();
        private readonly string[] _podKeys;
        private readonly float[] _revealReal;
        private readonly bool[] _reachable = new bool[RoundMaterial.Codes.Length];
        private float _autoSince = -1f;
        private bool _winTaken;
        private bool _endSounded;

        public RoundLevelCore(int levelNumber, RoundLevel level, ClearStyle style, IRoundLevelHost host)
        {
            Level = Math.Max(1, levelNumber);
            Round = level ?? throw new ArgumentNullException(nameof(level));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            Style = style;
            Play = new RoundPlay(level);
            _podKeys = new string[level.Pods.Count];
            _revealReal = new float[level.Pods.Count];
            for (int p = 0; p < _podKeys.Length; p++)
            {
                _podKeys[p] = "p" + p.ToString(CultureInfo.InvariantCulture);
                _revealReal[p] = float.NegativeInfinity;
            }

            ShowIntro();
        }

        /// <summary>The level number (Level N).</summary>
        public int Level { get; }

        public RoundLevel Round { get; }

        /// <summary>The rules on the live timeline, the display clock and the animator.</summary>
        public RoundPlay Play { get; }

        public RoundAnimator Animator => Play.Animator;

        /// <summary>The level's clearing style (spec 005 FR-038).</summary>
        public ClearStyle Style { get; }

        /// <summary>Real seconds since the level opened (cards, toasts, glows: never sped up).</summary>
        public float UiNow { get; private set; }

        public bool Symbols => _host.Symbols;

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

        /// <summary>Auto (FR-011): the rest plays at ×2, the HUD is gone and taps are ignored.</summary>
        public bool Auto => Play.Auto && !Won;

        /// <summary>The HUD's opacity: 1, fading to 0 over <see cref="HudFadeSeconds"/> once Auto starts (FR-044).</summary>
        public float HudAlpha => _autoSince < 0f ? 1f : 1f - Clamp01((UiNow - _autoSince) / HudFadeSeconds);

        /// <summary>Whether the HUD takes taps (fully shown).</summary>
        public bool HudLive => HudAlpha >= 1f;

        /// <summary>The display speed now (presentation only).</summary>
        public int Speed => Play.Speed(_host.SpeedUp);

        /// <summary>Whether the speed pill shows ×2 lit: the speed-up on, or Auto.</summary>
        public bool SpeedShown => _host.SpeedUp || Auto;

        /// <summary>Return or Bloom Burst waiting for its target, or null.</summary>
        public BoosterKind? Targeting { get; private set; }

        /// <summary>The jam card stays down while a booster chosen on it waits for its target.</summary>
        public bool JamHidden { get; private set; }

        public RoundIntroCard? Intro { get; private set; }

        public GuideStep? Guide => _guide.Count > 0 ? _guide[0] : null;

        /// <summary>The pod the guided first tap points at, or -1.</summary>
        public int GuidePod { get; private set; } = -1;

        /// <summary>The booster uses of this attempt that were a guided demo's free use (they keep the clean clear).</summary>
        public int DemoBoosterUses { get; private set; }

        /// <summary>The booster uses that count against the clean clear: all uses less the guided demos' (FR-042 as amended).</summary>
        public int BoostersPaid => Play.BoostersUsed - DemoBoosterUses;

        /// <summary>The last booster used and when (display µs), for its effect.</summary>
        public (BoosterKind Kind, long At)? LastBooster { get; private set; }

        /// <summary>The last tap that waited for its link partner (real time): the partner pulses (FR-008, spec 005 FR-043).</summary>
        public (int Pod, float At)? LinkHint { get; private set; }

        /// <summary>The rules' reason for the last refused tap or bridge.</summary>
        public RoundRefusal LastRefusal { get; private set; }

        /// <summary>The pause card, a purchase card, Settings or the app in the background holds the clock.</summary>
        public bool ExternalPause { get; set; }

        /// <summary>The jam card is showing (it holds the clock while the level is not won).</summary>
        public bool JamShown { get; set; }

        /// <summary>Whether the live clock stands still now.</summary>
        public bool Paused => Intro != null || Guide != null || Targeting.HasValue || ExternalPause || (JamShown && !Won);

        /// <summary>The display clock in seconds (the clearing styles' clock).</summary>
        public float ShowSeconds => Play.ClockUs / 1_000_000f;

        /// <summary>The recorded commands of this attempt with their model times (the leaderboard's proof).</summary>
        public IReadOnlyList<RoundInput> Inputs => Play.Inputs;

        /// <summary>A message key to show as a toast (a refusal or a hint); hosts read it with <c>Loc.T</c>.</summary>
        public event Action<string>? Toasted;

        /// <summary>The pod key a sound ladder knows a pod by.</summary>
        public string KeyOf(int pod) => _podKeys[pod];

        /// <summary>The reveal glow of a pod now (FR-045), from its reveal in real time.</summary>
        public float RevealGlow(int pod) => RoundLook.RevealGlow(UiNow - _revealReal[pod]);

        /// <summary>The link partner's pulse now (0 when none), <see cref="PodLinks.Hint"/>.</summary>
        public float LinkPulse => LinkHint.HasValue ? PodLinks.Hint(UiNow - LinkHint.Value.At) : 0f;

        /// <summary>True once, when the level was won: the host records and pays the win (it survives a kill during the celebration).</summary>
        public bool TakeWin()
        {
            if (!Won || _winTaken)
            {
                return false;
            }

            _winTaken = true;
            return true;
        }

        // ---- The clock ----

        /// <summary>
        /// Moves real time on by <paramref name="dt"/> seconds and, unless paused, the live clock by the same × the speed;
        /// plays this step's sounds, records reveals for their glow, notes Auto's start and offers Return's guided demo once
        /// it can show.
        /// </summary>
        public void Tick(float dt)
        {
            float step = Math.Max(0f, Math.Min(MaxStep, dt));
            UiNow += Math.Max(0f, dt);
            if (!Paused)
            {
                Heard(Play.Advance(step, Speed));
            }

            if (Play.Auto && _autoSince < 0f)
            {
                _autoSince = UiNow;
                Targeting = null;
            }

            if (Guide == null && Intro == null && !Paused)
            {
                GuideWhenReady();
            }

            AfterCommand();
        }

        private void Heard(IReadOnlyList<RoundEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                RoundEvent e = events[i];
                switch (e.Type)
                {
                    case RoundEventType.Clear:
                        _host.Collect(Style, _ladder.Next(_podKeys[e.Pod], Math.Max(1, Round.Pods[e.Pod].Count)));
                        break;
                    case RoundEventType.Complete:
                        _host.Sound(SoundCue.PodDone);
                        break;
                    case RoundEventType.Reveal:
                        _revealReal[e.Pod] = UiNow;
                        break;
                }
            }
        }

        // ---- Before play ----

        private void ShowIntro()
        {
            IReadOnlyList<GuideStep> guide = GuideTour.AtStart(Level, _host.HasSeenDemo, _host.IsUnlocked, Play.Timeline);
            if (guide.Count > 0)
            {
                StartGuide(guide);
            }

            Intro = RoundIntroCards.ToShow(Round.Introduction, _host.HasSeenDemo);
        }

        private void StartGuide(IReadOnlyList<GuideStep> steps)
        {
            _guide.Clear();
            _guide.AddRange(steps);
            GuidePod = GuideTour.FirstTapPod(Play.Timeline);
        }

        /// <summary>Return's guided demo: once a pod waits in a place with work left and no walker on the way.</summary>
        private void GuideWhenReady()
        {
            if (Targeting != null || Won || Blocked)
            {
                return;
            }

            IReadOnlyList<GuideStep> steps = GuideTour.WhenSettled(_host.HasSeenDemo, _host.IsUnlocked, Play.Timeline);
            if (steps.Count > 0)
            {
                StartGuide(steps);
            }
        }

        /// <summary>The introduction card's button: the card is seen, and play starts (or the guide's step shows).</summary>
        public void CloseIntro()
        {
            if (Intro != null)
            {
                _host.MarkDemoSeen(Intro.Id);
                Intro = null;
                _host.Sound(SoundCue.Click);
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
                _host.MarkDemoSeen(done.DemoId);
            }

            if (!done.Forced)
            {
                _host.Sound(SoundCue.Click);
            }
        }

        /// <summary>Ends the guided spotlight at once (the level ended under it, tests): its demos count as seen.</summary>
        public void SkipGuide()
        {
            while (_guide.Count > 0)
            {
                _host.MarkDemoSeen(_guide[0].DemoId);
                _guide.RemoveAt(0);
            }
        }

        // ---- Commands ----

        /// <summary>A tap on the top pod of a column: committed at the current model time, or the rules' refusal kept.</summary>
        public RoundTapOutcome Tap(int pod)
        {
            if (Targeting != null || Intro != null || Auto || ExternalPause || (Guide != null && !(Guide.Kind == GuideKind.FirstTap && pod == GuidePod)))
            {
                return RoundTapOutcome.Ignored;
            }

            RoundRefusal refusal = Play.Check(RoundAction.Tap(pod));
            if (refusal != RoundRefusal.None)
            {
                LastRefusal = refusal;
                _host.Sound(SoundCue.Refused);
                if (refusal == RoundRefusal.PartnerNotExposed)
                {
                    LinkHint = (pod, UiNow);
                }

                if (refusal != RoundRefusal.Finished)
                {
                    Toasted?.Invoke(RefusalKey(refusal));
                }

                return RoundTapOutcome.Refused;
            }

            Play.Commit(RoundAction.Tap(pod));
            Heard(Play.Fresh);
            _host.Sound(SoundCue.Tap);
            if (Guide != null && Guide.Kind == GuideKind.FirstTap)
            {
                NextGuideStep();
            }

            AfterCommand();
            return RoundTapOutcome.Accepted;
        }

        /// <summary>A tap on a bridge site (FR-009): built when the rules accept it.</summary>
        public bool BuildBridge(int choice)
        {
            if (Targeting != null || Intro != null || Guide != null || ExternalPause || !HudLive)
            {
                return false;
            }

            RoundRefusal refusal = Play.Check(RoundAction.BuildBridge(choice));
            if (refusal != RoundRefusal.None)
            {
                LastRefusal = refusal;
                _host.Sound(SoundCue.Refused);
                Toasted?.Invoke(RefusalKey(refusal));
                return false;
            }

            Play.Commit(RoundAction.BuildBridge(choice));
            _host.Sound(SoundCue.Special);
            AfterCommand();
            return true;
        }

        /// <summary>The bridge sites the rules let the player build now.</summary>
        public List<int> BuildableBridges() => RoundRules.BridgeChoices(Play.Timeline.State);

        /// <summary>Whether a booster would do something now (FR-012; the bar greys it otherwise).</summary>
        public bool Applicable(BoosterKind kind) => !Won && GuideTour.CanUse(Play.Timeline, kind);

        /// <summary>Whether a booster takes a press now: a guided step lets only its own booster through.</summary>
        public bool TakesBoosterPress(BoosterKind kind) =>
            Intro == null && !Auto && !ExternalPause && (Guide == null || (Guide.Kind == GuideKind.Booster && Guide.Booster == kind));

        /// <summary>A booster's tile pressed: Extra Slot and Shuffle at once; Return and Bloom Burst choose their target first.</summary>
        public RoundBoosterOutcome PressBooster(BoosterKind kind)
        {
            bool guided = Guide != null && Guide.Kind == GuideKind.Booster && Guide.Booster == kind;
            if (!TakesBoosterPress(kind))
            {
                return RoundBoosterOutcome.Ignored;
            }

            if (Targeting == kind)
            {
                Targeting = null;
                JamHidden = false;
                return RoundBoosterOutcome.Cancelled;
            }

            JamHidden = false;
            switch (kind)
            {
                case BoosterKind.ExtraSlot:
                    return UseBooster(kind, RoundInput.ExtraSlot(0));
                case BoosterKind.Shuffle:
                    return UseBooster(kind, RoundInput.Shuffle(0));
                default:
                    if (!guided && _host.Charges(kind) == 0 && !_host.CanAfford(kind))
                    {
                        Toasted?.Invoke("gameplay.not_enough_petals");
                        return RoundBoosterOutcome.NotEnoughPetals;
                    }

                    if (!Applicable(kind))
                    {
                        Toasted?.Invoke("gameplay.booster_useless");
                        return RoundBoosterOutcome.Useless;
                    }

                    Targeting = kind;
                    JamHidden = Blocked;
                    if (guided)
                    {
                        NextGuideStep();
                    }
                    else
                    {
                        Toasted?.Invoke(kind == BoosterKind.Return ? "gameplay.hint_return" : "round.hint_burst");
                    }

                    return RoundBoosterOutcome.Targeting;
            }
        }

        /// <summary>Ends a booster's targeting without a use.</summary>
        public void CancelTargeting()
        {
            Targeting = null;
            JamHidden = false;
        }

        /// <summary>
        /// Uses a booster as a command at the current model time (FR-012): a guided demo's use is free; a use without charges
        /// asks the purchase confirmation first (<see cref="RoundBoosterOutcome.NeedsPurchase"/>; the host calls again with
        /// <paramref name="confirmed"/> on its Buy).
        /// </summary>
        public RoundBoosterOutcome UseBooster(BoosterKind kind, RoundInput command, bool free = false, bool confirmed = false)
        {
            bool demo = Guide != null && Guide.Booster == kind && (Guide.Kind == GuideKind.Booster || Guide.Kind == GuideKind.BoosterTarget);
            if (Guide != null && !demo)
            {
                return RoundBoosterOutcome.Ignored;
            }

            free |= demo;
            Targeting = null;
            if (Refusal(command) != RoundRefusal.None)
            {
                JamHidden = false;
                Toasted?.Invoke("gameplay.booster_useless");
                return RoundBoosterOutcome.Useless;
            }

            if (!free && !confirmed && _host.Charges(kind) == 0 && _host.CanAfford(kind))
            {
                return RoundBoosterOutcome.NeedsPurchase;
            }

            if (!free && !_host.TryTakeCharge(kind))
            {
                JamHidden = false;
                Toasted?.Invoke("gameplay.not_enough_petals");
                return RoundBoosterOutcome.NotEnoughPetals;
            }

            RoundCommitResult result = Play.Apply(command);
            if (!result.Accepted)
            {
                JamHidden = false;
                Toasted?.Invoke("gameplay.booster_useless");
                return RoundBoosterOutcome.Useless;
            }

            Heard(Play.Fresh);
            _host.Sound(SoundCue.Booster);
            LastBooster = (kind, Play.ClockUs);
            JamHidden = false;
            JamShown = false;
            _endSounded = false;
            if (demo)
            {
                DemoBoosterUses++;
                _host.MarkDemoSeen(Guide!.DemoId);
                NextGuideStep();
            }

            AfterCommand();
            return RoundBoosterOutcome.Used;
        }

        /// <summary>Return's target: the pod in a place (FR-012: only one with no walker on the way).</summary>
        public RoundBoosterOutcome ReturnPod(int pod) => UseBooster(BoosterKind.Return, RoundInput.Return(pod, 0));

        /// <summary>Bloom Burst's target: the material of the tapped cube; nothing when it holds none Bloom Burst can take.</summary>
        public RoundBoosterOutcome BurstCell(int cell)
        {
            if (cell < 0 || cell >= Round.CellCount)
            {
                return RoundBoosterOutcome.Ignored;
            }

            int material = Animator.MaterialAt(cell);
            if (material < 0)
            {
                return RoundBoosterOutcome.Ignored;
            }

            char code = RoundMaterial.All[material].Code;
            return RoundBoosters.CanBloomBurst(Play.Timeline, code) == RoundRefusal.None
                ? UseBooster(BoosterKind.BloomBurst, RoundInput.BloomBurst(code, 0))
                : RoundBoosterOutcome.Ignored;
        }

        /// <summary>Whether Bloom Burst could take the material of this cube now (its target ring).</summary>
        public bool CanBurst(int material) =>
            material >= 0 && RoundBoosters.CanBloomBurst(Play.Timeline, RoundMaterial.All[material].Code) == RoundRefusal.None;

        /// <summary>Whether Return could send the pod back now (its place glows while Return waits).</summary>
        public bool CanReturn(int pod) => RoundBoosters.CanReturn(Play.Timeline, pod) == RoundRefusal.None;

        /// <summary>The rules' answer to a booster command now.</summary>
        public RoundRefusal Refusal(RoundInput command) => command.Type switch
        {
            RoundInputType.ExtraSlot => RoundBoosters.CanExtraSlot(Play.Timeline),
            RoundInputType.Shuffle => RoundBoosters.CanShuffle(Play.Timeline),
            RoundInputType.Return => RoundBoosters.CanReturn(Play.Timeline, command.Value),
            RoundInputType.BloomBurst => RoundBoosters.CanBloomBurst(Play.Timeline, command.Code),
            _ => RoundRefusal.UnknownAction,
        };

        /// <summary>The boosters the jam card offers: unlocked, owned or affordable, and accepted by the rules now (FR-012).</summary>
        public List<BoosterKind> JamBoosters()
        {
            var kinds = new List<BoosterKind>();
            foreach (BoosterKind kind in Boosters)
            {
                if (_host.IsUnlocked(kind) && (_host.Charges(kind) > 0 || _host.CanAfford(kind)) && Applicable(kind))
                {
                    kinds.Add(kind);
                }
            }

            return kinds;
        }

        /// <summary>
        /// The jam rescue's booster (a free use that resolves the jam): Extra Slot first, then Shuffle, then Return on the first
        /// place it can take back; Bloom Burst is never given away. Null when none helps or the level is not blocked.
        /// </summary>
        public (BoosterKind Kind, RoundInput Command)? RescueBooster()
        {
            if (!Blocked)
            {
                return null;
            }

            if (_host.IsUnlocked(BoosterKind.ExtraSlot) && RoundBoosters.CanExtraSlot(Play.Timeline) == RoundRefusal.None)
            {
                return (BoosterKind.ExtraSlot, RoundInput.ExtraSlot(0));
            }

            if (_host.IsUnlocked(BoosterKind.Shuffle) && RoundBoosters.CanShuffle(Play.Timeline) == RoundRefusal.None)
            {
                return (BoosterKind.Shuffle, RoundInput.Shuffle(0));
            }

            int pod = GuideTour.ReturnPod(Play.Timeline);
            if (_host.IsUnlocked(BoosterKind.Return) && pod >= 0)
            {
                return (BoosterKind.Return, RoundInput.Return(pod, 0));
            }

            return null;
        }

        /// <summary>A fresh attempt of the same level (the display clock keeps running, so effects never jump back).</summary>
        public void Restart()
        {
            Play.Restart();
            _ladder.Reset();
            Targeting = null;
            JamHidden = false;
            JamShown = false;
            DemoBoosterUses = 0;
            LastBooster = null;
            LinkHint = null;
            _autoSince = -1f;
            _endSounded = false;
            for (int p = 0; p < _revealReal.Length; p++)
            {
                _revealReal[p] = float.NegativeInfinity;
            }

            _guide.Clear();
            GuidePod = -1;
            _host.Sound(SoundCue.Click);
        }

        /// <summary>Toggles the speed-up (the speed pill); presentation only.</summary>
        public void ToggleSpeed() => _host.SpeedUp = !_host.SpeedUp;

        private void AfterCommand()
        {
            if (Play.Settled && (Won || Blocked) && !_endSounded)
            {
                _endSounded = true;
                _host.Sound(Won ? SoundCue.Win : SoundCue.Jam);
            }
        }

        /// <summary>The <c>Strings_en.csv</c> key of the rules' refusal as the player reads it.</summary>
        public static string RefusalKey(RoundRefusal refusal) => refusal switch
        {
            RoundRefusal.NotExposed => "refusal.not_exposed",
            RoundRefusal.PartnerNotExposed => "refusal.partner_buried",
            RoundRefusal.PairNeedsTwoSlots => "refusal.group",
            RoundRefusal.BridgeNotReady => "round.bridge_not_ready",
            _ => "refusal.no_free_slot",
        };

        // ---- What the tray shows ----

        /// <summary>Recomputes the materials with a cube in reach now (call once a frame before <see cref="PlaceLookOf"/>).</summary>
        public void RefreshReach()
        {
            Array.Clear(_reachable, 0, _reachable.Length);
            RoundState state = Play.Timeline.State;
            foreach (int cell in state.Reach().Targets)
            {
                if (state.Board[cell] >= 0)
                {
                    _reachable[state.Board[cell]] = true;
                }
            }
        }

        /// <summary>Whether a material had a cube in reach at the last <see cref="RefreshReach"/>.</summary>
        public bool Reachable(int material) => material >= 0 && material < _reachable.Length && _reachable[material];

        /// <summary>
        /// How place <paramref name="place"/> shows now (the playtest's places): empty, the last free one in danger while the
        /// level plays, a pod working (walkers on the way, or a cube of its material in reach), or stuck.
        /// </summary>
        public RoundPlaceLook PlaceLookOf(int place)
        {
            RoundAnimator a = Animator;
            int pod = a.PodIn(place);
            if (pod < 0)
            {
                int free = 0;
                for (int k = 0; k < a.PlaceCount; k++)
                {
                    free += a.PodIn(k) < 0 ? 1 : 0;
                }

                return free == 1 && Play.Status == RoundStatus.Playing && !Auto ? RoundPlaceLook.Danger : RoundPlaceLook.Empty;
            }

            bool working = Play.Timeline.InFlight[pod] > 0 || (a.Remaining(pod) > 0 && Reachable(a.MaterialOf(pod)));
            return working ? RoundPlaceLook.Working : RoundPlaceLook.Stuck;
        }

        /// <summary>How far a pod's flight from its column to its place has come (0 to 1).</summary>
        public float Arrival(int pod) => Clamp01((Play.ClockUs - Animator.CommittedAt(pod)) / (float)RoundAnimator.FlightUs);

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
