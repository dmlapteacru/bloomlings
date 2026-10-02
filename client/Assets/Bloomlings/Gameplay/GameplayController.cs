using System;
using System.Collections.Generic;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.App;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Slots;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Gameplay.Tray;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.DailyChallenge;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Analytics;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Gameplay;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Client.UI.Tutorial;
using Bloomlings.Client.UI.Tutorial.Demos;
using Bloomlings.Client.UI;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using UnityEngine;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.Gameplay
{
    /// <summary>
    /// Runs one level (T047). It owns the <see cref="LevelSession"/>: a tap is checked and applied at once against
    /// the logical state, with feedback in the same frame even while the timeline is behind (SC-008); the events go to
    /// the <see cref="EventTimeline"/>, which drives the board, slots and workers (R4). Won, Jammed and Stuck open
    /// their screens when their wave plays. There is no mid-level save: a killed app restarts the level.
    /// </summary>
    public sealed class GameplayController : MonoBehaviour, ITimelineSink
    {
        [SerializeField]
        [Tooltip("Optional art overrides; empty uses the placeholder visuals.")]
        private VariantVisualCatalog? _visuals;

        [SerializeField]
        [Tooltip("Active worker cap; 60 on low-end devices (R4).")]
        private int _workerCapacity = 60;

        private readonly Dictionary<string, int> _workInFlight = new Dictionary<string, int>(System.StringComparer.Ordinal);

        // Key-door specials whose key is still flying, and their trigger waiting for it to land (T107).
        private readonly HashSet<string> _keyFlights = new HashSet<string>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, SpecialTriggered> _heldTriggers = new Dictionary<string, SpecialTriggered>(System.StringComparer.Ordinal);
        private LevelSession? _session;
        private RectTransform _root = null!;
        private GameplayHud _hud = null!;
        private BoardView _board = null!;
        private TrayView _tray = null!;
        private SlotRowView _slots = null!;
        private EventTimeline _timeline = null!;
        private WorkerPool _workers = null!;
        private PauseScreen _pause = null!;
        private WinScreen _win = null!;
        private JamScreen _jam = null!;
        private DifficultyBanner _banner = null!;
        private DemoOverlay _demo = null!;
        private BoosterBar _boosters = null!;
        private BoosterKind? _targeting;
        private LevelReward? _reward;
        private MilestoneGrant? _milestone;
        private SettingsScreen? _settings;
        private MilestoneCard _milestoneCard = null!;
        private bool _milestoneShown;
        private bool _hasCountedSpecials;

        // Analytics of the current attempt (T147).
        private float _attemptStart;
        private int _attemptIndex;
        private int _peakSlots;
        private bool _winLogged;
        private bool _jamLogged;
        private readonly System.Collections.Generic.HashSet<string> _devDemosSeen = new System.Collections.Generic.HashSet<string>();

        public LevelSession? Session => _session;

        private IEnumerator Start()
        {
            Canvas canvas = UiFactory.CreateCanvas("GameplayCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            _root = root;
            _hud = GameplayHud.Create(UiFactory.Stretch(UiFactory.CreateRect("Hud", root)), OpenPause, OnSpeedChanged);
            _board = BoardView.Create(_hud.BoardArea, _visuals);
            _slots = SlotRowView.Create(_hud.SlotArea, _visuals);
            _tray = TrayView.Create(_hud.TrayArea, _visuals, OnPodTapped);
            _boosters = BoosterBar.Create(_hud.BoosterArea, OnBoosterPressed);
            _slots.SlotTapped += OnSlotTapped;
            _board.CellTapped += OnCellTapped;
            _timeline = gameObject.AddComponent<EventTimeline>();
            _timeline.Bind(this);
            _workers = WorkerPool.Create(gameObject, _board, _timeline, _visuals, _workerCapacity);
            WardrobeService? wardrobe = Service<WardrobeService>();
            if (wardrobe != null)
            {
                _workers.Outfits = wardrobe.OutfitOf;
            }
            _pause = PauseScreen.Create(root, ClosePause, RestartFromPause, Leave, OpenSettings);
            _win = WinScreen.Create(root, Next);
            _milestoneCard = MilestoneCard.Create(root, Next);
            _jam = JamScreen.Create(root, () => Restart("jam"), OnRecovery);
            _banner = DifficultyBanner.Create(root);
            _demo = DemoOverlay.Create(root);

            if (AppServices.Current != null && AppServices.Current.TryGet(out IRemoteConfigService? config))
            {
                _timeline.BacklogThresholdSeconds = config!.Get(RemoteConfigKeys.FxBacklogThresholdMs) / 1000f;
            }

            // Let the canvas scaler size the layout before the board measures its area.
            yield return null;
            Canvas.ForceUpdateCanvases();

            (LevelDefinition level, BasePicture picture, SessionOptions options) = ResolveLevel();
            _session = LevelSession.Load(level, picture, options);
            LayoutForLevel(_session);
            Service<AdPolicy>()?.OnAttemptStarted();
            if (AppServices.Current != null && AppServices.Current.TryGet(out PlayerSave? save) && save!.Settings.Speed2x)
            {
                _hud.SetDoubleSpeed(true);
                _timeline.Speed = 2f;
            }

            RebuildViews();
            ApplyTheme();
            BeginAttemptAnalytics();
            ShowLevelIntro();
        }

        private static GameAnalytics? Analytics => Service<GameAnalytics>();

        private LevelInfo? Info => _session == null ? null : LevelInfo.Of(_session.Definition, Flow?.CurrentAttempt?.LevelNumber ?? _session.Definition.LevelNumber);

        private long AttemptMilliseconds => (long)((Time.unscaledTime - _attemptStart) * 1000f);

        /// <summary>A new attempt (level load or restart): <c>level_start</c> and the crash keys of this level (R14).</summary>
        private void BeginAttemptAnalytics()
        {
            _attemptIndex++;
            _attemptStart = Time.unscaledTime;
            _peakSlots = 0;
            _winLogged = false;
            _jamLogged = false;
            LevelInfo? info = Info;
            GameAnalytics? analytics = Analytics;
            if (info != null && analytics != null)
            {
                analytics.SetLevelContext(info);
                analytics.LevelStart(info, _attemptIndex);
            }
        }

        /// <summary>After each accepted command: peak slots, and <c>level_win</c> or <c>level_jam</c> once when they happen.</summary>
        private void TrackAnalytics()
        {
            LevelSession session = _session!;
            int used = SlotsUsed(session.View);
            _peakSlots = Math.Max(_peakSlots, used);
            GameAnalytics? analytics = Analytics;
            LevelInfo? info = Info;
            if (analytics == null || info == null)
            {
                return;
            }

            if (session.Status == LevelStatus.Won && !_winLogged)
            {
                _winLogged = true;
                analytics.LevelWin(info, AttemptMilliseconds, TapsThisAttempt(session), session.BoostersUsed, session.BoostersUsed == 0, _peakSlots, _attemptIndex);
            }
            else if ((session.Status == LevelStatus.Jammed || session.Status == LevelStatus.Stuck) && !_jamLogged)
            {
                _jamLogged = true;
                analytics.LevelJam(info, session.Status == LevelStatus.Stuck ? "stuck" : "jam", AttemptMilliseconds, TapsThisAttempt(session), used, session.View.RemainingWork);
            }
            else if (session.Status == LevelStatus.Playing)
            {
                _jamLogged = false;
            }
        }

        private static int SlotsUsed(LevelView view)
        {
            int used = 0;
            for (int slot = 0; slot < view.SlotCapacity; slot++)
            {
                if (view.PodInSlot(slot) != null)
                {
                    used++;
                }
            }

            return used;
        }

        /// <summary>Pod taps since the last restart.</summary>
        private static int TapsThisAttempt(LevelSession session)
        {
            int taps = 0;
            foreach (Command command in session.CommandLog)
            {
                if (command is Restart)
                {
                    taps = 0;
                }
                else if (command is TapPod)
                {
                    taps++;
                }
            }

            return taps;
        }

        private static string MethodName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => "extra_slot",
            BoosterKind.Shuffle => "shuffle",
            BoosterKind.Return => "return",
            _ => "bloom_burst",
        };

        private static bool IsDaily => Flow?.CurrentAttempt?.IsDaily ?? false;

        /// <summary>The garden backdrop of the level band (FR-066); the Daily Challenge uses the player's current band.</summary>
        private void ApplyTheme()
        {
            int level = IsDaily ? Progression?.CurrentLevel ?? 1 : Flow?.CurrentAttempt?.LevelNumber ?? _session!.Definition.LevelNumber;
            _hud.SetTheme(Progression != null ? ThemeRotation.Default.ThemeFor(level) : null);
        }

        /// <summary>
        /// The screen regions for this level (spec 002 FR-009, FR-010, FR-014): the badge line of a labelled Hard or
        /// Super Hard level, and the booster bar once a booster is unlocked. Laid out before the views are built.
        /// </summary>
        private void LayoutForLevel(LevelSession session)
        {
            ProgressionService? progression = Progression;
            DifficultyClass difficulty = session.Definition.Difficulty.Class;
            bool labelled = difficulty == DifficultyClass.Hard ? progression?.IsUnlocked("profile.hard") ?? true
                : difficulty == DifficultyClass.SuperHard && (progression?.IsUnlocked("profile.super_hard") ?? true);
            EconomyService? economy = Economy;
            bool boosters = economy == null;
            foreach ((string _, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                boosters |= economy != null && economy.IsUnlocked(kind);
            }

            _hud.Layout(labelled, boosters);
            _hud.SetDifficulty(difficulty, labelled);
            Canvas.ForceUpdateCanvases();
        }

        // ---- Input ----

        private void OnPodTapped(string podId)
        {
            if (_session == null || _pause.IsOpen)
            {
                return;
            }

            if (_targeting != null)
            {
                CancelTargeting();
            }

            var tap = new TapPod(podId);
            CommandCheck check = _session.Check(tap);
            if (!check.IsAllowed)
            {
                Feedback?.Play(SoundCue.Refused);
                _tray.ShowRefused(podId);
                _hud.Toast(RefusalText(check.Reason!.Value));
                if (check.Reason == RejectReason.Locked)
                {
                    FlashKeysOf(podId);
                }

                return;
            }

            // What the tray shows before the commit: each group member's count, shown variant ("?" for a hidden mystery
            // pod) and card position, where it flies from.
            var before = new Dictionary<string, (int Count, VariantId? Variant, Vector3? From)>(System.StringComparer.Ordinal);
            foreach (string member in _session.View.ConnectedGroup(podId))
            {
                PodInfo info = _session.View.Pod(member);
                before[member] = (info.Remaining, info.Variant, _tray.RectOf(member)?.position);
            }

            if (!before.ContainsKey(podId))
            {
                PodInfo info = _session.View.Pod(podId);
                before[podId] = (info.Remaining, info.Variant, _tray.RectOf(podId)?.position);
            }

            CommandResult result = _session.Apply(tap);
            Feedback?.Play(SoundCue.Tap);
            HoldLocksOpenedBy(result.Events);
            foreach (GameEvent e in result.Events)
            {
                if (e.Round != 0)
                {
                    break;
                }

                switch (e)
                {
                    case PodCommitted committed:
                        (int count, VariantId? shown, Vector3? from) = before.TryGetValue(committed.PodId, out var seen)
                            ? seen
                            : (_session.View.Pod(committed.PodId).Remaining, _session.View.Pod(committed.PodId).Variant, (Vector3?)null);
                        _slots.Commit(committed.SlotIndex, committed.PodId, shown, count);
                        if (from.HasValue)
                        {
                            FlyCard(shown, from.Value, _slots.SlotPosition(committed.SlotIndex));
                        }

                        break;
                    case MysteryPodRevealed revealed:
                        _slots.RevealVariant(revealed.PodId, revealed.Variant);
                        break;
                }
            }

            _tray.Refresh(_session.View);
            _slots.UpdateStates(_session.View);
            _timeline.Enqueue(result.Events);
            _demo.NotifyAction();

            RecordWinIfWon();
            TrackAnalytics();
            RefreshBoosters();
        }

        /// <summary>Records a win as soon as it happens logically, so a kill during the win animation keeps it (R15), and pays it.</summary>
        private void RecordWinIfWon()
        {
            if (_session!.Status != LevelStatus.Won || Flow == null || Flow.CurrentAttempt == null)
            {
                return;
            }

            LevelAttempt attempt = Flow.CurrentAttempt;
            if (attempt.IsDaily)
            {
                // The Daily Challenge pays its own reward once per day and never changes Level N (FR-064).
                DailyChallengeService? daily = Service<DailyChallengeService>();
                int paid = _reward == null && daily != null ? daily.Complete(attempt) : 0;
                if (paid > 0)
                {
                    _reward = new LevelReward(paid, null);
                    Analytics?.DailyChallengeComplete(attempt.DailyUtcDate!);
                }

                return;
            }

            if (Flow.OnLevelWon(attempt.LevelNumber, LeaderboardClient.CommandLogHash(_session.CommandLog)) && Economy != null)
            {
                _reward = Economy.GrantLevelReward(attempt.LevelNumber, _session.Definition.Difficulty.Class, _session.BoostersUsed);
                MilestoneGrant? grant = Service<MilestoneService>()?.LastGrant;
                _milestone = grant != null && grant.Level == attempt.LevelNumber ? grant : null;
            }
        }

        // ---- Timeline sink ----

        public void OnWorkStarted(IReadOnlyList<WorkUnit> batch, float travelSeconds)
        {
            _workers.Launch(batch, travelSeconds);
            foreach (WorkUnit unit in batch)
            {
                string pod = unit.Clear.PodId;
                _workInFlight[pod] = (_workInFlight.TryGetValue(pod, out int n) ? n : 0) + 1;
                _slots.SetWorking(pod, true);
            }
        }

        public void OnWorkArrived(WorkUnit unit)
        {
            // A slightly different pitch per cell, so a run of clears does not drone.
            Feedback?.Play(SoundCue.Clear, 1f + (((unit.Clear.Cell.X + unit.Clear.Cell.Y) % 5) * 0.04f));
            switch (unit.Reveal)
            {
                case CellOpened opened:
                    _board.ShowOpened(opened.Cell);
                    break;
                case LayerRevealed layer:
                    _board.ShowLayer(layer.Cell, layer.NewTopVariant, _session!.View);
                    break;
            }

            if (_hasCountedSpecials)
            {
                _board.UpdateCounted(_session!.View);
            }

            string pod = unit.Clear.PodId;
            _slots.Decrement(pod);
            int left = (_workInFlight.TryGetValue(pod, out int n) ? n : 1) - 1;
            _workInFlight[pod] = left;
            if (left <= 0)
            {
                _slots.SetWorking(pod, false);
            }
        }

        public void OnEvent(GameEvent e)
        {
            switch (e)
            {
                case PodCompleted completed:
                    Feedback?.Play(SoundCue.PodDone);
                    _slots.Complete(completed.PodId);
                    break;
                case MysteryTileRevealed revealed:
                    _board.ShowMysteryRevealed(revealed.Cell, revealed.Variant);
                    break;
                case KeyCollected key:
                    Feedback?.Play(SoundCue.Key);
                    FlyKey(key);
                    break;
                case SpecialProgressed progressed:
                    _board.ShowSpecialProgress(progressed.SpecialId, progressed.Progress, progressed.Total, SpecialKind(progressed.SpecialId));
                    break;
                case SpecialTriggered triggered:
                    if (_keyFlights.Contains(triggered.SpecialId))
                    {
                        // A key door opens when its key lands, not before (T107).
                        _heldTriggers[triggered.SpecialId] = triggered;
                    }
                    else
                    {
                        PlayTrigger(triggered);
                    }

                    break;
                case LevelWon _:
                    Feedback?.Play(SoundCue.Win);
                    _board.RevealAll();
                    _workers.Celebrate(LevelVariants(_session!.Definition));
                    UiFx.Confetti(_root, ConfettiColors(_session.Definition), _milestone != null ? 80 : 40, _milestone != null ? 2.6f : 1.8f);
                    LevelReward? earned = _reward;
                    _win.Show(this, RewardText(earned), DoubleRewardOffer(), _milestone != null, earned != null && earned.Petals > 0 ? (earned.Petals, n => RewardText(earned with { Petals = (int)n })) : null, earned, _session);
                    break;
                case LevelJammed _:
                case LevelStuck _:
                    Feedback?.Play(SoundCue.Jam);
                    _slots.ShowBlocked();
                    ShowJamIfBlocked();
                    break;
            }
        }

        /// <summary>
        /// Shows the Jam screen while the board is Jammed or Stuck (FR-027): the recoveries of the current state (the
        /// timeline may be behind) that are unlocked and owned or affordable, the ad rescue and Restart. Also called when
        /// a recovery started from the Jam screen changed nothing or was cancelled, since no new jam event follows then.
        /// </summary>
        private void ShowJamIfBlocked()
        {
            LevelSession? session = _session;
            if (session == null || (session.Status != LevelStatus.Jammed && session.Status != LevelStatus.Stuck))
            {
                return;
            }

            EconomyService? economy = Economy;
            var usable = new List<Recovery>();
            foreach (Recovery recovery in session.EligibleRecoveries())
            {
                BoosterKind kind = KindOf(recovery);
                if (economy == null || (economy.IsUnlocked(kind) && economy.CanAfford(kind)))
                {
                    usable.Add(recovery);
                }
            }

            _jam.Show(session.Status == LevelStatus.Stuck, usable, RecoveryLabel, RescueOffer());
        }

        // ---- Boosters (T120, T121) ----

        private static GameFeedback? Feedback => GameFeedback.Current;

        private static EconomyService? Economy =>
            AppServices.Current != null && AppServices.Current.TryGet(out EconomyService? economy) ? economy : null;

        private void OnBoosterPressed(BoosterKind kind)
        {
            if (_session == null || _pause.IsOpen)
            {
                return;
            }

            if (_targeting == kind)
            {
                CancelTargeting();
                ShowJamIfBlocked();
                return;
            }

            CancelTargeting();
            switch (kind)
            {
                case BoosterKind.ExtraSlot:
                    UseBooster(kind, new UseExtraSlot());
                    break;
                case BoosterKind.Shuffle:
                    UseBooster(kind, new UseShuffle());
                    break;
                case BoosterKind.Return:
                    StartTargeting(kind, Loc.T("gameplay.hint_return"));
                    break;
                default:
                    StartTargeting(kind, Loc.T("gameplay.hint_burst"));
                    break;
            }
        }

        private void OnRecovery(Recovery recovery)
        {
            _jam.Hide();
            LevelInfo? info = Info;
            if (info != null)
            {
                Analytics?.LevelRecover(info, MethodName(KindOf(recovery)));
            }

            OnBoosterPressed(KindOf(recovery));
        }

        private void StartTargeting(BoosterKind kind, string hint)
        {
            _targeting = kind;
            _boosters.SetTargeting(kind);
            _hud.Toast(hint);
            if (kind == BoosterKind.Return)
            {
                _slots.SetTargeting(true);
            }
            else
            {
                _board.SetTargeting(true);
            }
        }

        private void CancelTargeting()
        {
            _targeting = null;
            _boosters.SetTargeting(null);
            _slots.SetTargeting(false);
            _board.SetTargeting(false);
        }

        private void OnSlotTapped(int slot)
        {
            if (_targeting == BoosterKind.Return)
            {
                CancelTargeting();
                UseBooster(BoosterKind.Return, new UseReturn(slot));
            }
        }

        private void OnCellTapped(Core.Boards.CellPos cell)
        {
            if (_targeting == BoosterKind.BloomBurst && _session != null)
            {
                VariantId? variant = _session.View.Cell(cell).Visible;
                CancelTargeting();
                if (variant.HasValue)
                {
                    UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant.Value));
                }
                else
                {
                    ShowJamIfBlocked();
                }
            }
        }

        /// <summary>
        /// Uses a booster: the level checks it first (FR-046), then a charge is taken, or bought with Petals; without
        /// either, the player is told, and the Store is never forced (FR-027). The pending animation is played out first,
        /// so the slots and tray are rebuilt from the settled state.
        /// </summary>
        private void UseBooster(BoosterKind kind, Command command, bool free = false)
        {
            LevelSession session = _session!;
            CommandCheck check = session.Check(command);
            if (!check.IsAllowed)
            {
                _hud.Toast(Loc.T("gameplay.booster_useless"));
                ShowJamIfBlocked();
                return;
            }

            string source = free ? "ad" : Economy == null || Economy.Charges(kind) > 0 ? "charge" : "petals";
            if (!free && Economy != null && !Economy.TryTakeCharge(kind))
            {
                _hud.Toast(Loc.T("gameplay.not_enough_petals"));
                ShowJamIfBlocked();
                return;
            }

            LevelInfo? boosted = Info;
            if (boosted != null)
            {
                Analytics?.BoosterUse(boosted, MethodName(kind), source);
            }

            _timeline.Flush();
            _workers.RecallAll();
            _workInFlight.Clear();
            (string PodId, Vector3 From, VariantId? Variant)? returning = null;
            if (command is UseReturn back && session.View.PodInSlot(back.SlotIndex) is string returned)
            {
                returning = (returned, _slots.SlotPosition(back.SlotIndex), session.View.Pod(returned).Variant);
            }

            CommandResult result = session.Apply(command);
            HoldLocksOpenedBy(result.Events);
            Feedback?.Play(SoundCue.Booster);
            _jam.Hide();
            _boosters.Pulse(kind);
            foreach (GameEvent e in result.Events)
            {
                if (e.Round != 0)
                {
                    continue;
                }

                switch (e)
                {
                    case VariantBurst burst:
                        _board.ShowBurst(burst.Cells, session.View);
                        break;
                    case ExtraSlotAdded _:
                    case PodReturned _:
                    case TrayShuffled _:
                        break;
                    default:
                        // Keys, locks and specials a burst resolved.
                        OnEvent(e);
                        break;
                }
            }

            _slots.Reset(session.View, result.Events);
            _tray.Refresh(session.View);
            if (kind == BoosterKind.Shuffle)
            {
                _tray.PlayShuffle();
            }
            else if (returning.HasValue && _tray.RectOf(returning.Value.PodId) is RectTransform back2)
            {
                FlyCard(returning.Value.Variant, returning.Value.From, back2.position);
                _tray.PlayReturned(returning.Value.PodId);
            }

            _timeline.Enqueue(result.Events);
            RecordWinIfWon();
            TrackAnalytics();
            RefreshBoosters();
            ShowJamIfBlocked();
        }

        // ---- Rewarded placements (T130); every one is started by the player (FR-052) ----

        private static T? Service<T>()
            where T : class =>
            AppServices.Current != null && AppServices.Current.TryGet(out T? service) ? service : null;

        /// <summary>
        /// The jam rescue: a rewarded ad for one free use of a jam-resolving booster, once per attempt (FR-027, FR-048).
        /// Only a booster the level accepts in this state is offered, so the rescue always changes the outcome: Extra Slot
        /// first, then Shuffle (a Stuck board only), then Return on the first slot whose pod can make room. Bloom Burst is
        /// never given away. Null when none of them helps or is unlocked yet, or no ad is ready.
        /// </summary>
        private (string Label, Action Watch)? RescueOffer()
        {
            IAdsService? ads = Service<IAdsService>();
            AdPolicy? policy = Service<AdPolicy>();
            if (ads == null || policy == null || !ads.IsRewardedReady || !policy.MayOfferRescue)
            {
                return null;
            }

            (BoosterKind Kind, Command Command)? rescue = RescueBooster(_session!);
            if (rescue == null)
            {
                return null;
            }

            (BoosterKind kind, Command command) = rescue.Value;
            return (Loc.F("jam.free_rescue", JamScreen.Label(RecoveryOf(kind))), () => ads.ShowRewarded(AdPlacements.JamRescue, earned =>
            {
                if (earned)
                {
                    policy.OnRescueUsed();
                    Analytics?.AdRewarded("rescue");
                    LevelInfo? info = Info;
                    if (info != null)
                    {
                        Analytics?.LevelRecover(info, "ad_rescue");
                    }

                    UseBooster(kind, command, free: true);
                }
            }));
        }

        private static (BoosterKind Kind, Command Command)? RescueBooster(LevelSession session)
        {
            EconomyService? economy = Economy;
            bool Usable(BoosterKind kind, Command command) =>
                (economy == null || economy.IsUnlocked(kind)) && session.Check(command).IsAllowed;

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

        /// <summary>The doubled win reward: the same Petals again after a rewarded ad (FR-052).</summary>
        private Action<Action<string>>? DoubleRewardOffer()
        {
            IAdsService? ads = Service<IAdsService>();
            LevelReward? reward = _reward;
            if (ads == null || reward == null || !ads.IsRewardedReady || Economy == null)
            {
                return null;
            }

            return update => ads.ShowRewarded(AdPlacements.DoubleWin, earned =>
            {
                if (earned)
                {
                    Analytics?.AdRewarded("double_reward");
                    Economy.Grant(reward.Petals, null);
                    update(RewardText(reward with { Petals = reward.Petals * 2 }));
                }
            });
        }

        private void RefreshBoosters()
        {
            if (_session == null)
            {
                return;
            }

            var eligible = new HashSet<Recovery>(_session.EligibleRecoveries());
            EconomyService? economy = Economy;
            _boosters.Refresh(kind => new BoosterButtonState(
                economy == null || economy.IsUnlocked(kind),
                eligible.Contains(RecoveryOf(kind)),
                economy?.Charges(kind) ?? 1,
                economy?.Price(kind) ?? 0,
                economy == null || economy.Petals >= economy.Price(kind)));
        }

        private string RecoveryLabel(Recovery recovery)
        {
            string name = JamScreen.Label(recovery);
            EconomyService? economy = Economy;
            if (economy == null)
            {
                return name;
            }

            BoosterKind kind = KindOf(recovery);
            return economy.Charges(kind) > 0
                ? name + " ×" + economy.Charges(kind).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : name + " " + economy.Price(kind).ToString(System.Globalization.CultureInfo.InvariantCulture) + " ✿";
        }

        private static string RewardText(LevelReward? reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            string text = Loc.F("common.petals_plus", reward.Petals);
            return reward.DroppedBooster.HasValue ? text + "  " + Loc.F("win.drop", JamScreen.Label(RecoveryOf(reward.DroppedBooster.Value))) : text;
        }

        private static BoosterKind KindOf(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => BoosterKind.ExtraSlot,
            Recovery.Shuffle => BoosterKind.Shuffle,
            Recovery.Return => BoosterKind.Return,
            _ => BoosterKind.BloomBurst,
        };

        private static Recovery RecoveryOf(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => Recovery.ExtraSlot,
            BoosterKind.Shuffle => Recovery.Shuffle,
            BoosterKind.Return => Recovery.Return,
            _ => Recovery.BloomBurst,
        };

        // ---- Keys and locks (T107, T109, T110) ----

        /// <summary>The collected key leaves its tile and flies to its lock, which then plays its opening.</summary>
        private void FlyKey(KeyCollected key)
        {
            LevelView view = _session!.View;
            Vector3 from = _board.KeyPosition(key.Cell) ?? _board.CellWorldPosition(key.Cell);
            _board.HideKey(key.Cell);
            LockDef? lockDef = null;
            foreach (LockDef candidate in view.Locks)
            {
                if (candidate.KeyId == key.KeyId)
                {
                    lockDef = candidate;
                }
            }

            if (lockDef == null)
            {
                return;
            }

            Vector3 to = lockDef.TargetKind switch
            {
                LockTargetKind.Slot => _slots.SlotPosition(int.Parse(lockDef.TargetId, System.Globalization.CultureInfo.InvariantCulture)),
                LockTargetKind.Special => _board.SpecialPosition(lockDef.TargetId) ?? from,
                _ => _tray.RectOf(lockDef.TargetId) is RectTransform pod ? pod.position : _hud.TrayArea.position,
            };

            if (lockDef.TargetKind == LockTargetKind.Special)
            {
                _keyFlights.Add(lockDef.TargetId);
            }

            LevelSession flightSession = _session;
            KeyView.Fly(_root, from, to, _board.CellSize * 0.8f, () =>
            {
                if (_session != flightSession || !this)
                {
                    return; // The level was left or rebuilt meanwhile.
                }

                if (lockDef.TargetKind == LockTargetKind.Slot)
                {
                    _slots.PlayUnlock(int.Parse(lockDef.TargetId, System.Globalization.CultureInfo.InvariantCulture), _session!.View);
                }
                else if (lockDef.TargetKind == LockTargetKind.Pod)
                {
                    _tray.ShowAccepted(lockDef.TargetId);
                }
                else
                {
                    _keyFlights.Remove(lockDef.TargetId);
                    if (_heldTriggers.TryGetValue(lockDef.TargetId, out SpecialTriggered? held))
                    {
                        _heldTriggers.Remove(lockDef.TargetId);
                        PlayTrigger(held);
                    }
                }
            });
        }

        /// <summary>
        /// The rules open a lock as soon as its key is collected, but the key still has to fly there on the timeline: the
        /// pods and slots it opens keep their lock drawn until it lands (T107).
        /// </summary>
        private void HoldLocksOpenedBy(IReadOnlyList<GameEvent> events)
        {
            foreach (GameEvent e in events)
            {
                if (!(e is KeyCollected key))
                {
                    continue;
                }

                foreach (LockDef lockDef in _session!.View.Locks)
                {
                    if (lockDef.KeyId != key.KeyId)
                    {
                        continue;
                    }

                    if (lockDef.TargetKind == LockTargetKind.Pod)
                    {
                        _tray.HoldLock(lockDef.TargetId);
                    }
                    else if (lockDef.TargetKind == LockTargetKind.Slot)
                    {
                        _slots.HoldLock(int.Parse(lockDef.TargetId, System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        private void PlayTrigger(SpecialTriggered triggered)
        {
            Feedback?.Play(SoundCue.Special);
            _board.TriggerSpecial(triggered.SpecialId, triggered.EffectCells, _session!.View);
        }

        /// <summary>A pod card flying between the tray and a slot (commit or Return); decorative only.</summary>
        private void FlyCard(VariantId? variant, Vector3 from, Vector3 to)
        {
            Color body = UiTheme.SlotLocked;
            Sprite? icon = Art.ProceduralSprites.Question;
            Color ink = Color.white;
            if (variant.HasValue)
            {
                VariantVisual visual = _visuals != null ? _visuals.Get(variant.Value) : VariantVisualCatalog.Default(variant.Value);
                body = visual.Color;
                icon = visual.Icon;
                ink = visual.Ink;
            }

            UiFx.Fly(_root, body, icon, ink, from, to, _slots.SlotSize * 0.8f, 0.16f);
        }

        /// <summary>The exact variants of a level, in pod order (the win celebration).</summary>
        private static List<VariantId> LevelVariants(LevelDefinition definition)
        {
            var variants = new List<VariantId>();
            foreach (PodDef pod in definition.Pods)
            {
                if (!variants.Contains(pod.Variant))
                {
                    variants.Add(pod.Variant);
                }
            }

            return variants;
        }

        private Color[] ConfettiColors(LevelDefinition definition)
        {
            var colors = new List<Color> { UiTheme.EntryMarker, Color.white };
            foreach (VariantId variant in LevelVariants(definition))
            {
                colors.Add(UiTheme.Light(_visuals != null ? _visuals.Get(variant).Color : VariantVisualCatalog.Default(variant).Color));
            }

            return colors.ToArray();
        }

        /// <summary>The 2× toggle (FR-069) also becomes the default for the next levels.</summary>
        private void OnSpeedChanged(bool doubleSpeed)
        {
            _timeline.Speed = doubleSpeed ? 2f : 1f;
            PlayerSave? save = Service<PlayerSave>();
            if (save != null && save.Settings.Speed2x != doubleSpeed)
            {
                save.Settings.Speed2x = doubleSpeed;
                Service<SaveService>()?.Save();
            }
        }

        /// <summary>A locked pod was tapped: point at the key that opens it (the pod or its group).</summary>
        private void FlashKeysOf(string podId)
        {
            LevelView view = _session!.View;
            foreach (string member in view.ConnectedGroup(podId))
            {
                foreach (PodDef pod in _session.Definition.Pods)
                {
                    if (pod.Id == member && pod.LockKeyId != null && !view.IsKeyCollected(pod.LockKeyId))
                    {
                        _board.FlashKey(view, pod.LockKeyId);
                    }
                }
            }
        }

        private SpecialConditionKind SpecialKind(string specialId)
        {
            foreach (SpecialInfo special in _session!.View.Specials)
            {
                if (special.Id == specialId)
                {
                    return special.Condition.Kind;
                }
            }

            return SpecialConditionKind.ClearCountAdjacent;
        }

        // ---- Flow ----

        private void OpenPause()
        {
            _timeline.Paused = true;
            _pause.Show();
        }

        /// <summary>Settings from the pause card (spec 002 FR-018): the same toggles as on Home.</summary>
        private void OpenSettings()
        {
            if (_settings == null && AppServices.Current != null && AppServices.Current.TryGet(out PlayerSave? save) && AppServices.Current.TryGet(out SaveService? saves))
            {
                _settings = SettingsScreen.Create(_root, save!.Settings, saves!.Save);
            }

            _settings?.Show();
        }

        private void ClosePause()
        {
            _pause.Hide();
            _timeline.Paused = false;
        }

        private void RestartFromPause()
        {
            ClosePause();
            Restart("pause");
        }

        /// <summary>Restart rebuilds the same level for free (FR-028, FR-040).</summary>
        /// <param name="from"><c>pause</c> or <c>jam</c> (the <c>level_restart</c> event).</param>
        private void Restart(string from)
        {
            if (_session == null)
            {
                return;
            }

            LevelInfo? info = Info;
            if (info != null)
            {
                Analytics?.LevelRestart(info, from);
            }

            _session.Apply(new Restart());
            Service<AdPolicy>()?.OnAttemptStarted();
            RebuildViews();
            BeginAttemptAnalytics();
        }

        private static GameFlow? Flow => AppServices.Current != null && AppServices.Current.TryGet(out GameFlow? flow) ? flow : null;

        private static ProgressionService? Progression =>
            AppServices.Current != null && AppServices.Current.TryGet(out ProgressionService? progression) ? progression : null;

        private void Next()
        {
            // A milestone win shows its milestone card first (spec 002 FR-021), then CONTINUE goes on.
            if (_milestone != null && !_milestoneShown)
            {
                _milestoneShown = true;
                _win.Hide();
                _milestoneCard.Show(_milestone, Service<WardrobeService>()?.Catalog);
                return;
            }

            _milestoneCard.Hide();
            if (Flow != null)
            {
                Flow.Next();
            }
            else
            {
                Restart("pause");
            }
        }

        private void Leave()
        {
            ClosePause();
            LevelInfo? info = Info;
            if (info != null && _session!.Status != LevelStatus.Won)
            {
                Analytics?.LevelQuit(info, AttemptMilliseconds);
            }

            Analytics?.SetLevelContext(null);
            if (Flow != null)
            {
                Flow.Leave();
            }
            else
            {
                Restart("pause");
            }
        }

        /// <summary>Before play: the difficulty label (FR-059), then at most one demo (FR-031, FR-071).</summary>
        private void ShowLevelIntro()
        {
            LevelSession session = _session!;
            ProgressionService? progression = Progression;
            _banner.TryShow(
                session.Definition.Difficulty.Class,
                progression?.IsUnlocked("profile.hard") ?? true,
                progression?.IsUnlocked("profile.super_hard") ?? true);

            if (session.Definition.LevelNumber == 1 && !IsDaily && !SeenDemo(DemoScripts.FirstTapId))
            {
                string? pod = RecommendedFirstPod(session);
                ShowDemo(DemoScripts.FirstTap(() => pod == null ? null : _tray.RectOf(pod)));
                return;
            }

            // A booster unlocked on the way here: its demo and free charge (FR-042, T122).
            foreach ((string unlockId, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                if (progression != null && progression.IsUnlocked(unlockId) && !SeenDemo(unlockId))
                {
                    DemoScript? demo = BoosterDemos.For(unlockId, () => _boosters.RectOf(kind));
                    if (demo != null)
                    {
                        ShowDemo(demo);
                        return;
                    }
                }
            }

            // The first time the player meets an unlocked mechanic, its demo (FR-031, T111).
            foreach (string unlockId in LevelMechanics.UnlocksUsed(session.Definition, session.Picture))
            {
                if (SeenDemo(unlockId) || !(progression?.IsUnlocked(unlockId) ?? true))
                {
                    continue;
                }

                DemoScript? demo = MechanicDemos.For(unlockId, DemoTargetsFor(session));
                if (demo != null)
                {
                    ShowDemo(demo);
                    return;
                }
            }

            // A variant from a pool expansion (L45, L200) seen for the first time: shown beside its family (roadmap).
            foreach (VariantId variant in LevelVariants(session.Definition))
            {
                VariantInfo info = VariantCatalog.Default.Get(variant);
                if (info.Status != VariantStatus.Expansion || SeenDemo(DemoScripts.NewVariantId(variant.Key)))
                {
                    continue;
                }

                var family = new List<VariantVisual>();
                foreach (VariantInfo member in VariantCatalog.Default.All)
                {
                    if (member.Family == info.Family && (member.Status == VariantStatus.Launch || member.Id == variant))
                    {
                        family.Add(_visuals != null ? _visuals.Get(member.Id) : VariantVisualCatalog.Default(member.Id));
                    }
                }

                ShowDemo(DemoScripts.NewVariant(variant.Key, family));
                return;
            }

            (VariantId First, VariantId Second)? siblings = SiblingPair(session);
            if (siblings.HasValue && !SeenDemo(DemoScripts.SiblingsId))
            {
                VariantVisual first = _visuals != null ? _visuals.Get(siblings.Value.First) : VariantVisualCatalog.Default(siblings.Value.First);
                VariantVisual second = _visuals != null ? _visuals.Get(siblings.Value.Second) : VariantVisualCatalog.Default(siblings.Value.Second);
                ShowDemo(DemoScripts.Siblings(first, second));
            }
        }

        /// <summary>Pointer targets for the mechanic demos, resolved from what is on screen when each step starts.</summary>
        private DemoTargets DemoTargetsFor(LevelSession session)
        {
            LevelView view = session.View;
            string? FirstPod(System.Func<PodInfo, bool> match)
            {
                foreach (string id in view.PodIds)
                {
                    if (match(view.Pod(id)))
                    {
                        return id;
                    }
                }

                return null;
            }

            RectTransform? PodRect(System.Func<PodInfo, bool> match) => FirstPod(match) is string id ? _tray.RectOf(id) : null;

            RectTransform? LockRect()
            {
                foreach (LockDef lockDef in view.Locks)
                {
                    return lockDef.TargetKind switch
                    {
                        LockTargetKind.Pod => _tray.RectOf(lockDef.TargetId),
                        LockTargetKind.Slot => _slots.SlotRect(int.Parse(lockDef.TargetId, System.Globalization.CultureInfo.InvariantCulture)),
                        _ => _board.SpecialRect(lockDef.TargetId),
                    };
                }

                return null;
            }

            RectTransform? LockedSlot()
            {
                for (int i = 0; i < view.SlotCapacity; i++)
                {
                    if (view.SlotStateOf(i) == Core.Slots.SlotState.Locked)
                    {
                        return _slots.SlotRect(i);
                    }
                }

                return null;
            }

            return new DemoTargets
            {
                Stone = () => _board.FindCell(view, c => c.Kind == Core.Boards.CellKind.Stone),
                KeyTile = () => _board.FindCell(view, c => c.KeyId != null),
                KeyLock = LockRect,
                LockedPod = () => PodRect(p => p.Locked),
                ConnectedPod = () => PodRect(p => p.ConnectedGroupId != null && view.IsExposed(p.Id)),
                LayeredTile = () => _board.FindCell(view, c => c.Next.HasValue),
                Gate = () => _board.SpecialRectOfType(SpecialType.Gate),
                Fountain = () => _board.SpecialRectOfType(SpecialType.Fountain),
                LockedSlot = LockedSlot,
                MysteryTile = () => _board.FindCell(view, c => c.MysteryHidden),
                MysteryPod = () => PodRect(p => p.Mystery && p.Variant == null),
                Chest = () => _board.SpecialRectOfType(SpecialType.Chest),
                Environment2 = () => _board.SpecialRectOfType(SpecialType.Statue) ?? _board.SpecialRectOfType(SpecialType.Bridge),
                TriplePod = () => PodRect(p => p.ConnectedGroupId != null && view.ConnectedGroup(p.Id).Count >= 3 && view.IsExposed(p.Id)),
            };
        }

        private bool SeenDemo(string demoId) => Progression?.HasSeenDemo(demoId) ?? _devDemosSeen.Contains(demoId);

        /// <summary>Shows a demonstration and reports its <c>tutorial_step</c> (T147).</summary>
        private void ShowDemo(DemoScript script)
        {
            Analytics?.TutorialStep(Info, script.DemoId, 1, false);
            _demo.Show(script, OnDemoDone);
        }

        private void OnDemoDone(DemoScript script)
        {
            Analytics?.TutorialStep(Info, script.DemoId, 1, true);
            if (Progression != null)
            {
                Progression.MarkDemoSeen(script.DemoId);
            }
            else
            {
                _devDemosSeen.Add(script.DemoId);
            }
        }

        /// <summary>An exposed pod that clears tiles at once: the target of the guided first tap.</summary>
        private static string? RecommendedFirstPod(LevelSession session)
        {
            foreach (string id in session.View.PodIds)
            {
                if (!session.View.IsExposed(id))
                {
                    continue;
                }

                LevelSession probe = session.Clone();
                foreach (GameEvent e in probe.Apply(new TapPod(id)).Events)
                {
                    if (e is TileCleared)
                    {
                        return id;
                    }
                }
            }

            return null;
        }

        /// <summary>Two variants of one family among the level's pods, for the sibling demo (FR-071).</summary>
        private static (VariantId First, VariantId Second)? SiblingPair(LevelSession session)
        {
            var byFamily = new System.Collections.Generic.Dictionary<Family, VariantId>();
            foreach (PodDef pod in session.Definition.Pods)
            {
                Family family = VariantCatalog.Default.Get(pod.Variant).Family;
                if (byFamily.TryGetValue(family, out VariantId other) && other != pod.Variant)
                {
                    return (other, pod.Variant);
                }

                byFamily[family] = pod.Variant;
            }

            return null;
        }

        private void RebuildViews()
        {
            LevelSession session = _session!;
            _timeline.Clear();
            _workers.RecallAll();
            _workInFlight.Clear();
            _win.Hide();
            _jam.Hide();
            if (IsDaily)
            {
                _hud.SetTitle(Loc.T("daily.title"));
            }
            else
            {
                _hud.SetLevel(Flow?.CurrentAttempt?.LevelNumber ?? session.Definition.LevelNumber);
            }

            _keyFlights.Clear();
            _heldTriggers.Clear();
            _tray.ReleaseLocks();
            _slots.ReleaseLocks();
            _board.Build(session.View, session.Definition, session.Picture);
            _hasCountedSpecials = false;
            foreach (SpecialInfo special in session.View.Specials)
            {
                _hasCountedSpecials |= special.Condition.Kind != SpecialConditionKind.Key;
            }

            _workers.SetEntries(session.View.Entries);
            _slots.Reset(session.View);
            _tray.Refresh(session.View);
            _reward = null;
            _milestone = null;
            _milestoneShown = false;
            _milestoneCard.Hide();
            CancelTargeting();
            RefreshBoosters();
        }

        /// <summary>Pauses with the app and resumes exactly where the timeline stopped.</summary>
        private void OnApplicationPause(bool paused)
        {
            if (_timeline == null)
            {
                return;
            }

            if (paused)
            {
                _timeline.Paused = true;
            }
            else if (!_pause.IsOpen)
            {
                _timeline.Paused = false;
            }
        }

        private static (LevelDefinition, BasePicture, SessionOptions) ResolveLevel()
        {
            LevelAttempt? attempt = Flow?.CurrentAttempt;
            if (attempt != null)
            {
                return (attempt.Definition, attempt.Picture, attempt.Options);
            }

            // Played directly in the Editor: the level picked in Tools/Bloomlings/Play Dev Level.
            (LevelDefinition devLevel, BasePicture devPicture) = DevContent.LoadSelected();
            return (devLevel, devPicture, new SessionOptions(LooseContentFolder.DevContentVersion, LooseContentFolder.DevShuffleNodeBudget));
        }

        private static string RefusalText(RejectReason reason) => reason switch
        {
            RejectReason.NoFreeSlot => Loc.T("refusal.no_free_slot"),
            RejectReason.NotExposed => Loc.T("refusal.not_exposed"),
            RejectReason.Locked => Loc.T("refusal.locked"),
            RejectReason.NotEnoughSlotsForGroup => Loc.T("refusal.group"),
            _ => string.Empty,
        };
    }
}
