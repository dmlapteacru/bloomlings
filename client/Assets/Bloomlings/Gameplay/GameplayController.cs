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
using Bloomlings.Client.Meta.Clearing;
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
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Gameplay;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Client.UI.Tutorial;
using Bloomlings.Client.UI.Tutorial.Demos;
using Bloomlings.Client.UI;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Boards;
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

        // The level's clearing style (spec 005 FR-038): its walkers and their tiles over the board, their flights over the
        // slots, drawn from the kit's list (ClearLook), as the playtest draws it.
        private ClearFxView _fxBoard = null!;
        private ClearFxView _fxOver = null!;
        private readonly FxList _fx = new FxList();
        private readonly List<ClearTrip> _trips = new List<ClearTrip>();
        private readonly List<(ClearFade Fade, string PodId)> _cleared = new List<(ClearFade, string)>();
        private readonly HashSet<CellPos> _heldCells = new HashSet<CellPos>();
        private readonly Dictionary<CellPos, float> _swayCells = new Dictionary<CellPos, float>();
        private PauseScreen _pause = null!;
        private WinScreen _win = null!;
        private JamScreen _jam = null!;

        // The purchase confirmation (spec 005 FR-040), made the first time a booster would buy its charge.
        private PurchaseCardView? _confirm;
        private DifficultyBanner _banner = null!;
        private DemoOverlay _demo = null!;
        private GuideOverlay _guide = null!;
        private readonly List<GuideStep> _guideSteps = new List<GuideStep>();
        private string? _guidePod;

        /// <summary>
        /// The booster uses of this attempt that were a guided demo's free use (spec 001 FR-042 as amended on 2026-10-05):
        /// they take no charge and keep the clean-clear bonus.
        /// </summary>
        private int _demoUses;
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

            // Over the slots and the tray, under the end screens and the pause card.
            _fxOver = ClearFxView.Create(root, "ClearFxOver");
            _board = BoardView.Create(_hud.BoardArea, _visuals);
            _fxBoard = ClearFxView.Create(_board.Grid, "ClearFx");
            _slots = SlotRowView.Create(_hud.SlotArea, _visuals);
            _tray = TrayView.Create(_hud.TrayArea, _visuals, OnPodTapped);
            _boosters = BoosterBar.Create(_hud.BoosterArea, OnBoosterPressed);

            // The views take their places from the reference layout's regions (spec 005 FR-020, contracts/look.md §6.1).
            _board.Fit = _hud.FitBoard;
            _slots.CellsFor = _hud.SlotCells;
            _tray.Grid = _hud.PodGrid;
            _boosters.Places = _hud.BoosterCells;
            _slots.SlotTapped += OnSlotTapped;
            _board.CellTapped += OnCellTapped;
            _timeline = gameObject.AddComponent<EventTimeline>();
            _timeline.Bind(this);
            _workers = WorkerPool.Create(gameObject, _board, _timeline, _visuals, _workerCapacity);
            WardrobeService? wardrobe = Service<WardrobeService>();
            if (wardrobe != null)
            {
                _workers.Outfits = wardrobe.OutfitOf;
                _fxBoard.Outfits = wardrobe.OutfitOf;
                _fxOver.Outfits = wardrobe.OutfitOf;
            }
            // The end screens first, then the pause card over them: the win and the milestone cover the whole screen, top
            // bar included (spec 005 FR-023), while Pause stays usable during a jam (its scrim lets taps through over the
            // top bar), so the pause card must cover the jam card (Settings, built when first opened, comes above it).
            _win = WinScreen.Create(root, Next);
            _milestoneCard = MilestoneCard.Create(root, Next);
            _jam = JamScreen.Create(root, RestartFromJam, OnRecovery, _hud.TopBar);
            _pause = PauseScreen.Create(root, ClosePause, RestartFromPause, Leave, OpenSettings);
            _banner = DifficultyBanner.Create(root);
            _demo = DemoOverlay.Create(root);
            _guide = GuideOverlay.Create(root);

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
                _hud.SetFastForward(true);
            }

            RefreshSpeed();

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
                int boosters = session.BoostersUsed - _demoUses;
                analytics.LevelWin(info, AttemptMilliseconds, TapsThisAttempt(session), boosters, boosters == 0, _peakSlots, _attemptIndex);
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

        /// <summary>A booster's player-facing name (the purchase confirmation's question).</summary>
        private static string BoosterTitle(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => Loc.T("booster.extra_slot"),
            BoosterKind.Shuffle => Loc.T("booster.shuffle"),
            BoosterKind.Return => Loc.T("booster.return"),
            _ => Loc.T("booster.bloom_burst"),
        };

        private static bool IsDaily => Flow?.CurrentAttempt?.IsDaily ?? false;

        /// <summary>The garden backdrop of the level band (FR-066); the Daily Challenge uses the player's current band.</summary>
        private void ApplyTheme()
        {
            int level = IsDaily ? Progression?.CurrentLevel ?? 1 : Flow?.CurrentAttempt?.LevelNumber ?? _session!.Definition.LevelNumber;
            BackgroundTheme? theme = Progression != null ? ThemeRotation.Default.ThemeFor(level) : null;
            _hud.SetTheme(theme);

            // The win's garden is the level's lawn, rendered now so the celebration shows without a pause.
            _win.SetTheme(theme);
            _milestoneCard.SetTheme(theme);
        }

        /// <summary>
        /// The screen regions for this level (spec 002 FR-009, FR-010, FR-014; spec 005 §6.1): the badge line of a labelled
        /// Hard or Super Hard level, the booster row once a booster is unlocked, and one pod column per Source stack (the
        /// Garden Entries take no room: they have no arch). Laid out before the views are built.
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

            _hud.Layout(labelled, boosters, session.View.StackCount);
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

            // A guided spotlight lets only its lit pod take the tap (spec 005 FR-035).
            GuideStep? guided = _guide.Step;
            if (guided != null && !(guided.Kind == GuideKind.FirstTap && podId == _guidePod))
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

            // A pod goes in only onto a plate that shows no pod (the owner, 2026-10-05; spec 001 FR-070 as amended): the
            // rules free a finished pod's slot at once, but the player waits until its plate is empty on screen, so quick
            // taps cannot stack pods behind the ones still working. The rules never see a refused tap.
            int group = _session.View.ConnectedGroup(podId).Count;
            if (_slots.FreeOnScreen(_session.View) < group)
            {
                Feedback?.Play(SoundCue.Refused);
                _tray.ShowRefused(podId);
                _hud.Toast(RefusalText(group > 1 ? RejectReason.NotEnoughSlotsForGroup : RejectReason.NoFreeSlot));
                return;
            }

            // What the tray shows before the commit: each group member's count, shown variant ("?" for a hidden mystery
            // pod) and the position of its tile in its column, where it flies from (spec 005 §6.1).
            var before = new Dictionary<string, (int Count, VariantId? Variant, Vector3? From)>(System.StringComparer.Ordinal);
            foreach (string member in _session.View.ConnectedGroup(podId))
            {
                PodInfo info = _session.View.Pod(member);
                before[member] = (info.Remaining, info.Variant, _tray.TilePosition(member));
            }

            if (!before.ContainsKey(podId))
            {
                PodInfo info = _session.View.Pod(podId);
                before[podId] = (info.Remaining, info.Variant, _tray.TilePosition(podId));
            }

            CommandResult result = _session.Apply(tap);
            RefreshSpeed();
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

                        // It shows on a plate free on screen, maybe not its slot in the rules (SlotPlaces), and flies there.
                        int place = _slots.Commit(_session.View, committed.SlotIndex, committed.PodId, shown, count);
                        if (from.HasValue)
                        {
                            FlyCard(shown, from.Value, _slots.TilePosition(place), _tray.TileSize(committed.PodId), _slots.TileSize);
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
            if (guided != null)
            {
                NextGuideStep();
            }

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
                _reward = Economy.GrantLevelReward(attempt.LevelNumber, _session.Definition.Difficulty.Class, _session.BoostersUsed - _demoUses);
                MilestoneGrant? grant = Service<MilestoneService>()?.LastGrant;
                _milestone = grant != null && grant.Level == attempt.LevelNumber ? grant : null;
            }
        }

        // ---- Timeline sink ----

        public void OnWorkStarted(IReadOnlyList<WorkUnit> batch, float start, float travelSeconds)
        {
            WorkUnit lead = batch[batch.Count - 1];
            IReadOnlyList<CellPos> route = lead.Clear.RouteFromEntry;
            if (route.Count > 0 && _trips.Count < _workers.Capacity)
            {
                // Its way in the kit's cell units (y down): the arch's door, then the route's cells.
                var points = new List<(float X, float Y)>(route.Count + 1);
                EntryDef? door = null;
                foreach (EntryDef entry in _session!.View.Entries)
                {
                    if (entry.Cell == route[0])
                    {
                        door = entry;
                    }
                }

                if (door != null)
                {
                    points.Add(KitPoint(_board.EntryPoint(door)));
                }

                foreach (CellPos cell in route)
                {
                    points.Add(KitPoint(_board.CellCenter(cell)));
                }

                _trips.Add(new ClearTrip(points, lead.Clear.Variant, start, travelSeconds, lead.Clear.PodId, lead.Clear.Cell));
            }

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
                    // The clearing style drew the tile's restore; its flower or confetti follows from here.
                    _board.ShowCleared(opened.Cell);
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
            _cleared.Add((new ClearFade(KitPoint(_board.CellCenter(unit.Clear.Cell)), unit.Clear.Variant, _timeline.Now, null), pod));
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

            _jam.Show(session.Status == LevelStatus.Stuck, usable, RecoveryCost, JamScreen.SlotsOf(session.View), RescueOffer());
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

            // During a guided spotlight only its lit booster takes the tap (spec 005 FR-035).
            GuideStep? step = _guide.Step;
            bool guided = step != null && step.Kind == GuideKind.Booster && step.Booster == kind;
            if (step != null && !guided)
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
                    StartTargeting(kind, guided ? null : Loc.T("gameplay.hint_return"));
                    break;
                default:
                    StartTargeting(kind, guided ? null : Loc.T("gameplay.hint_burst"));
                    break;
            }

            if (guided && (kind == BoosterKind.Return || kind == BoosterKind.BloomBurst))
            {
                // The guided demo lights the targets and says what to tap.
                NextGuideStep();
            }
        }

        private void OnRecovery(Recovery recovery)
        {
            // The jam's choices are inert while the pause card is open over the sheet.
            if (_pause.IsOpen)
            {
                return;
            }

            _jam.Hide();
            LevelInfo? info = Info;
            if (info != null)
            {
                Analytics?.LevelRecover(info, MethodName(KindOf(recovery)));
            }

            OnBoosterPressed(KindOf(recovery));
        }

        private void StartTargeting(BoosterKind kind, string? hint)
        {
            _targeting = kind;
            _boosters.SetTargeting(kind);
            if (hint != null)
            {
                _hud.Toast(hint);
            }

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

        /// <summary>A plate was tapped: Return takes back the pod shown there, by its slot in the rules (it may show on another plate).</summary>
        private void OnSlotTapped(int place)
        {
            if (_targeting == BoosterKind.Return && _session != null)
            {
                int slot = SlotPlaces.RulesSlotOf(_session.View, _slots.PodAt(place));
                CancelTargeting();
                if (slot >= 0)
                {
                    UseBooster(BoosterKind.Return, new UseReturn(slot));
                }
                else
                {
                    ShowJamIfBlocked();
                }
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
        /// so the slots and tray are rebuilt from the settled state. A use that would buy its charge with Petals asks the
        /// purchase confirmation first (spec 005 FR-040): it is bought and used only on its Buy (<paramref name="confirmed"/>),
        /// a cancel buys nothing.
        /// </summary>
        private void UseBooster(BoosterKind kind, Command command, bool free = false, bool confirmed = false)
        {
            LevelSession session = _session!;

            // A guided demo's forced use is free: the unlock's free charge stays (spec 001 FR-042 as amended on 2026-10-05).
            GuideStep? step = _guide.Step;
            bool demo = step != null && step.Booster == kind && (step.Kind == GuideKind.Booster || step.Kind == GuideKind.BoosterTarget);
            if (step != null && !demo)
            {
                return;
            }

            CommandCheck check = session.Check(command);
            if (!check.IsAllowed)
            {
                _hud.Toast(Loc.T("gameplay.booster_useless"));
                ShowJamIfBlocked();
                return;
            }

            if (!free && !demo && !confirmed && Economy != null && Economy.Charges(kind) == 0 && Economy.CanAfford(kind))
            {
                // No charge left: the use buys one for Petals, so the confirmation asks first (over the jam card too).
                _confirm ??= UiKit.PurchaseCard(_root);
                string boosterId = MethodName(kind);
                _confirm.ShowPetals(
                    PurchaseOffer.ForPetals(BoosterTitle(kind), Economy.Price(kind)),
                    Economy.Petals,
                    well => UiFactory.Place(UiKit.BoosterIcon("Icon", well, boosterId).rectTransform, 0.08f, 0.08f, 0.92f, 0.92f),
                    () => UseBooster(kind, command, confirmed: true),
                    () => { },
                    ShowJamIfBlocked);
                return;
            }

            string source = demo ? "demo" : free ? "ad" : Economy == null || Economy.Charges(kind) > 0 ? "charge" : "petals";
            free |= demo;
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
            ForgetTrips();
            _workInFlight.Clear();
            (string PodId, Vector3 From, VariantId? Variant)? returning = null;
            if (command is UseReturn back && session.View.PodInSlot(back.SlotIndex) is string returned)
            {
                // It flies back from the plate it shows on (maybe not its slot in the rules, SlotPlaces).
                int place = _slots.PlaceOf(returned);
                returning = (returned, _slots.TilePosition(place >= 0 ? place : back.SlotIndex), session.View.Pod(returned).Variant);
            }

            CommandResult result = session.Apply(command);
            RefreshSpeed();
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

            _slots.Rebuild(session.View, result.Events);
            _tray.Refresh(session.View);
            if (kind == BoosterKind.Shuffle)
            {
                _tray.PlayShuffle();
            }
            else if (returning.HasValue && _tray.TilePosition(returning.Value.PodId) is Vector3 back2)
            {
                FlyCard(returning.Value.Variant, returning.Value.From, back2, _slots.TileSize, _tray.TileSize(returning.Value.PodId));
                _tray.PlayReturned(returning.Value.PodId);
            }

            _timeline.Enqueue(result.Events);
            if (demo)
            {
                // Seen at once, so leaving the level now never gives the free use again.
                _demoUses++;
                MarkSeen(step!.DemoId);
                NextGuideStep();
            }

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
        private (Recovery Booster, Action Watch)? RescueOffer()
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
            return (RecoveryOf(kind), () =>
            {
                // Inert while the pause card is open over the jam sheet.
                if (_pause.IsOpen)
                {
                    return;
                }

                ads.ShowRewarded(AdPlacements.JamRescue, earned =>
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
                });
            });
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

        /// <summary>A recovery's cost on the jam sheet (spec 005 §4.3): ×N charges while owned, else the lotus and its price.</summary>
        private static UI.Design.Cost? RecoveryCost(Recovery recovery)
        {
            EconomyService? economy = Economy;
            if (economy == null)
            {
                return null;
            }

            BoosterKind kind = KindOf(recovery);
            return economy.Charges(kind) > 0 ? UI.Design.Cost.Charges(economy.Charges(kind)) : UI.Design.Cost.Petals(economy.Price(kind));
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

        /// <summary>
        /// A pod's sticker tile flying between the tray and a slot (commit or Return; the playtest's
        /// <c>PodPainter.DrawFlights</c>): the variant's candy tile (the lilac "?" for a mystery pod) leaving one tile at its
        /// size (<paramref name="fromSize"/>: the pod's tile or the slot's) and growing or shrinking to the other's
        /// (<paramref name="toSize"/>), arcing 40% of a slot's height over the straight line; decorative only. A size of 0
        /// (a tile not shown) falls back to the slot's.
        /// </summary>
        private void FlyCard(VariantId? variant, Vector3 from, Vector3 to, float fromSize, float toSize)
        {
            float slot = _slots.SlotSize;
            float start = fromSize > 0f ? fromSize : slot * 0.64f;
            float end = toSize > 0f ? toSize : slot * 0.64f;
            int pixels = ShapeRaster.Quantize(Mathf.Max(start, end) * UiKit.PixelsPerUnit);

            // With the owner's icon picture (spec 005 pictures.md G9–G16) the face flies with the picture on it.
            Sprite? icon = null;
            Sprite tile;
            if (variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info)
                && (icon = OwnerArt.TileIcon(info.IconId, TileStyle.Sticker, TileState.Normal)) != null)
            {
                tile = Art.ProceduralSprites.CandyFace(Rgba.FromHex(info.ColorHex), TileStyle.Sticker, TileState.Normal, pixels);
            }
            else
            {
                tile = Art.ProceduralSprites.CandyTile(variant, TileStyle.Sticker, TileState.Normal, pixels);
            }

            UiFx.FlyTile(_root, tile, from, to, start, end, slot * 0.4f, 0.16f, icon: icon);
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

        /// <summary>Fast forward (FR-069) also becomes the default for the next levels.</summary>
        private void OnSpeedChanged(bool fastForward)
        {
            RefreshSpeed();
            PlayerSave? save = Service<PlayerSave>();
            if (save != null && save.Settings.Speed2x != fastForward)
            {
                save.Settings.Speed2x = fastForward;
                Service<SaveService>()?.Save();
            }
        }

        /// <summary>
        /// The clock's speed after every command: fast forward (<see cref="PlaySpeed.Fast"/>, 3×) when the player chose it
        /// (FR-069 as amended on 2026-10-06), and also while no pod can be tapped (the owner, 2026-10-04: every pod picked,
        /// only the animation left), so the rest plays fast; the pill is lit then.
        /// Animation only: never an outcome.
        /// </summary>
        private void RefreshSpeed()
        {
            bool auto = _session != null && !CanTapAny(_session);
            _timeline.Speed = PlaySpeed.Of(_hud.FastForward || auto);
            _hud.ShowAutoSpeed(auto);
        }

        /// <summary>Whether the rules allow a tap on any exposed pod (the top of a Source stack).</summary>
        private static bool CanTapAny(LevelSession session)
        {
            LevelView view = session.View;
            for (int i = 0; i < view.StackCount; i++)
            {
                IReadOnlyList<string> stack = view.Stack(i);
                if (stack.Count > 0 && session.Check(new TapPod(stack[0])).IsAllowed)
                {
                    return true;
                }
            }

            return false;
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

        /// <summary>The jam sheet's Restart; inert while the pause card is open over the sheet.</summary>
        private void RestartFromJam()
        {
            if (!_pause.IsOpen)
            {
                Restart("jam");
            }
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
            _demoUses = 0;
            _guideSteps.Clear();
            _guide.Hide();
            RefreshSpeed();
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
                _milestoneCard.Show(_milestone, Service<WardrobeService>()?.Catalog, _session?.Definition);
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

            // The guided spotlights first (spec 005 FR-035): Level 1's entry and forced first tap, a booster's forced and
            // free first use (FR-042), the blocked entry.
            IReadOnlyList<GuideStep> guide = GuideTour.AtStart(session.Definition.LevelNumber, IsDaily, SeenDemo, BoosterUnlocked, session);
            if (guide.Count > 0)
            {
                StartGuide(guide);
                return;
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

        // ---- Guided spotlights (spec 005 FR-035) ----

        private bool BoosterUnlocked(BoosterKind kind) => Economy?.IsUnlocked(kind) ?? false;

        private void StartGuide(IReadOnlyList<GuideStep> steps)
        {
            _guideSteps.Clear();
            _guideSteps.AddRange(steps);
            _guidePod = GuideTour.FirstTapPod(_session!);
            ShowGuideStep();
        }

        private void ShowGuideStep()
        {
            GuideStep step = _guideSteps[0];
            Analytics?.TutorialStep(Info, step.DemoId, 1, false);
            System.Action<RectTransform>? icon = step.Booster.HasValue
                ? GuideOverlay.BoosterIcon(GuideTour.Key(step.Booster.Value))
                : step.Kind == GuideKind.Blocked && GuideTour.BlockingVariant(_session!.View) is VariantId variant ? GuideOverlay.TileIcon(variant) : null;
            _guide.Show(step, () => GuideHoles(step), icon, NextGuideStep);
        }

        /// <summary>The guided step goes on; a demo is seen when its last step ends.</summary>
        private void NextGuideStep()
        {
            if (_guideSteps.Count == 0)
            {
                _guide.Hide();
                return;
            }

            GuideStep done = _guideSteps[0];
            _guideSteps.RemoveAt(0);
            if (_guideSteps.Count == 0 || _guideSteps[0].DemoId != done.DemoId)
            {
                Analytics?.TutorialStep(Info, done.DemoId, 1, true);
                MarkSeen(done.DemoId);
            }

            if (_guideSteps.Count > 0)
            {
                ShowGuideStep();
            }
            else
            {
                _guide.Hide();
            }
        }

        private void MarkSeen(string demoId)
        {
            if (Progression != null)
            {
                Progression.MarkDemoSeen(demoId);
            }
            else
            {
                _devDemosSeen.Add(demoId);
            }
        }

        /// <summary>The places a guided step lights, on screen now (the first one ringed).</summary>
        private System.Collections.Generic.List<Box> GuideHoles(GuideStep step)
        {
            var holes = new System.Collections.Generic.List<Box>();
            LevelView view = _session!.View;
            Box? Union(System.Collections.Generic.IEnumerable<RectTransform> rects)
            {
                Box? all = null;
                foreach (RectTransform rect in rects)
                {
                    Box b = _guide.ScreenOf(rect);
                    all = all.HasValue ? new Box(Mathf.Min(all.Value.Left, b.Left), Mathf.Min(all.Value.Top, b.Top), Mathf.Max(all.Value.Right, b.Right), Mathf.Max(all.Value.Bottom, b.Bottom)) : b;
                }

                return all;
            }

            void Add(Box? box)
            {
                if (box.HasValue)
                {
                    holes.Add(box.Value);
                }
            }

            switch (step.Kind)
            {
                case GuideKind.Entry:
                    Add(Union(_board.ArchRects));
                    break;
                case GuideKind.Blocked:
                    Add(Union(_board.ArchRects));
                    foreach (Core.Boards.CellPos cell in GuideTour.BlockingCells(view))
                    {
                        holes.Add(_guide.ScreenOf(_board.CellRect(cell)));
                    }

                    break;
                case GuideKind.FirstTap:
                    Add(_guidePod != null && _tray.RectOf(_guidePod) is RectTransform pod ? _guide.ScreenOf(pod) : (Box?)null);
                    break;
                case GuideKind.BoosterTarget when step.Booster == BoosterKind.Return:
                {
                    var plates = new System.Collections.Generic.List<RectTransform>();
                    for (int slot = 0; slot < view.SlotCapacity; slot++)
                    {
                        int place = view.PodInSlot(slot) is string waiting && _session.Check(new UseReturn(slot)).IsAllowed ? _slots.PlaceOf(waiting) : -1;
                        if (place >= 0)
                        {
                            plates.Add(_slots.SlotRect(place));
                        }
                    }

                    Add(Union(plates));
                    break;
                }

                case GuideKind.BoosterTarget:
                    Add(_guide.ScreenOf(_board.GridRect));
                    break;
                default:
                    if (step.Booster.HasValue)
                    {
                        Add(_guide.ScreenOf(_boosters.RectOf(step.Booster.Value)));
                    }

                    break;
            }

            return holes;
        }

        /// <summary>
        /// Return's guided demo starts once a pod waits in a slot and the board has settled (Level 6); a level that ends
        /// under a step (its last "kept" step) leaves the end screens alone.
        /// </summary>
        private void Update()
        {
            if (_session != null && _guide != null && _guide.IsShowing && _session.Status != LevelStatus.Playing)
            {
                while (_guideSteps.Count > 0)
                {
                    MarkSeen(_guideSteps[0].DemoId);
                    _guideSteps.RemoveAt(0);
                }

                _guide.Hide();
            }

            if (_session == null || _guide == null || _guide.IsShowing || _demo.IsShowing || _pause.IsOpen || _targeting != null || !_timeline.IsIdle)
            {
                return;
            }

            IReadOnlyList<GuideStep> steps = GuideTour.WhenSettled(IsDaily, SeenDemo, BoosterUnlocked, _session);
            if (steps.Count > 0)
            {
                StartGuide(steps);
            }
        }

        // ---- The clearing style (spec 005 FR-038) ----

        /// <summary>
        /// Draws the clearing style after every Update (the timeline's clock is this frame's): each walker on its trip and
        /// each just-cleared tile's restore, from the kit's list; the tiles the style holds show their ground, and
        /// Blossom's flowers sway their neighbours.
        /// </summary>
        private void LateUpdate()
        {
            if (_session == null || _timeline == null || _board.CellSize <= 0f)
            {
                return;
            }

            float now = _timeline.Now;
            ClearStyle style = _timeline.Style;
            _trips.RemoveAll(trip => now > trip.Start + trip.Arrival + 0.05f);
            _cleared.RemoveAll(c => now - c.Fade.Start >= ClearStyles.RestoreSeconds);
            _fx.Clear();
            _heldCells.Clear();
            var fades = new List<ClearFade>(_cleared.Count);
            foreach ((ClearFade fade, string pod) in _cleared)
            {
                var seated = new ClearFade(fade.Center, fade.Variant, fade.Start, Seat(pod));
                fades.Add(seated);
                ClearLook.Restore(_fx, style, seated, now);
            }

            foreach (ClearTrip trip in _trips)
            {
                var walk = new ClearWalk(trip.Points, trip.Variant, trip.Start, trip.Arrival, Seat(trip.PodId));
                if (ClearLook.Holds(style, walk, now))
                {
                    _heldCells.Add(trip.Target);
                }

                ClearLook.Walker(_fx, style, walk, now);
            }

            _board.SetHeld(_heldCells);
            _swayCells.Clear();
            if (style == ClearStyle.Blossom && fades.Count > 0)
            {
                LevelView view = _session.View;
                for (int y = 0; y < view.Height; y++)
                {
                    for (int x = 0; x < view.Width; x++)
                    {
                        var cell = new CellPos(x, y);
                        float sway = ClearLook.Sway(style, fades, KitPoint(_board.CellCenter(cell)), now);
                        if (sway != 0f)
                        {
                            _swayCells[cell] = sway;
                        }
                    }
                }
            }

            _board.SetSway(_swayCells);

            // Over the board: the grid's own coordinates. Over the slots: the same points on the root's layer.
            float cellSize = _board.CellSize;
            int rows = _board.Rows;
            _fxBoard.Cell = cellSize;
            _fxBoard.Map = (x, y) => new Vector2(x * cellSize, (rows - y) * cellSize);
            _fxBoard.Render(_fx.Items, FxLayer.Board);
            _fxBoard.transform.SetAsLastSibling();
            RectTransform grid = _board.Grid;
            var over = (RectTransform)_fxOver.transform;
            Vector2 Over(float x, float y)
            {
                Rect g = grid.rect;
                Vector3 world = grid.TransformPoint(new Vector3((x * cellSize) + g.xMin, ((rows - y) * cellSize) + g.yMin, 0f));
                Vector3 local = over.InverseTransformPoint(world);
                Rect o = over.rect;
                return new Vector2(local.x - o.xMin, local.y - o.yMin);
            }

            Vector2 zero = Over(0f, 0f);
            Vector2 one = Over(1f, 0f);
            _fxOver.Cell = Mathf.Abs(one.x - zero.x);
            _fxOver.Map = Over;
            _fxOver.Render(_fx.Items, FxLayer.Over);
        }

        /// <summary>A point on the board's grid (from its bottom left) in the kit's cell units (y down from the top row).</summary>
        private (float X, float Y) KitPoint(Vector2 point) => (point.x / _board.CellSize, _board.Rows - (point.y / _board.CellSize));

        /// <summary>Where a pod's tiles go in the slot it shows in (its plate's tile), in cell units; null while in none.</summary>
        private Box? Seat(string podId)
        {
            int place = _slots.PlaceOf(podId);
            if (place < 0)
            {
                return null;
            }

            RectTransform grid = _board.Grid;
            Vector3 local = grid.InverseTransformPoint(_slots.TilePosition(place));
            Rect g = grid.rect;
            (float x, float y) = KitPoint(new Vector2(local.x - g.xMin, local.y - g.yMin));
            float side = _slots.TileSize / Mathf.Max(0.001f, _board.CellSize);
            return Box.FromCenter(x, y, side, side);
        }

        /// <summary>Forgets the walkers and clears drawn (a restart, a booster showing everything at once).</summary>
        private void ForgetTrips()
        {
            _trips.Clear();
            _cleared.Clear();
            _heldCells.Clear();
            _swayCells.Clear();
            _board.SetHeld(_heldCells);
            _board.SetSway(_swayCells);
            _fxBoard.Clear();
            _fxOver.Clear();
        }

        /// <summary>One walker on its trip: its way in cell units, its variant, its start and trip on the clock, its pod and tile.</summary>
        private sealed class ClearTrip
        {
            public ClearTrip(IReadOnlyList<(float X, float Y)> points, VariantId variant, float start, float arrival, string podId, CellPos target)
            {
                Points = points;
                Variant = variant;
                Start = start;
                Arrival = arrival;
                PodId = podId;
                Target = target;
            }

            public IReadOnlyList<(float X, float Y)> Points { get; }

            public VariantId Variant { get; }

            public float Start { get; }

            public float Arrival { get; }

            public string PodId { get; }

            public CellPos Target { get; }
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
            ForgetTrips();
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

            // The board pictures of earlier levels at other cell sizes go; the last level's, still shown, stay for this one.
            Art.ProceduralSprites.ReleaseUnused(Art.ProceduralSprites.BoardFamilies);
            _board.Build(session.View, session.Definition, session.Picture);
            _fxBoard.transform.SetAsLastSibling();

            // The level's clearing style: the free pair by level, or the chosen bought one (spec 005 FR-038).
            int styleLevel = Flow?.CurrentAttempt?.LevelNumber ?? session.Definition.LevelNumber;
            _timeline.Style = Service<ClearingService>()?.StyleFor(styleLevel) ?? ClearStyles.ForLevel(styleLevel);
            _hasCountedSpecials = false;
            foreach (SpecialInfo special in session.View.Specials)
            {
                _hasCountedSpecials |= special.Condition.Kind != SpecialConditionKind.Key;
            }

            _slots.Reset(session.View);
            _tray.Refresh(session.View, slide: false);
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
