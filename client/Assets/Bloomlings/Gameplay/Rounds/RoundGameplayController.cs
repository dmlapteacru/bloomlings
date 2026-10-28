using System;
using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.App;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.Gameplay.Slots;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Tray;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Analytics;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Gameplay;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Client.UI.Tutorial;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Simulation;
using UnityEngine;

namespace Bloomlings.Client.Gameplay.Rounds
{
    /// <summary>
    /// Runs one round level in the Gameplay scene (spec 006 US5, T058, T059), the Unity twin of the playtest's
    /// <c>RoundLevelScreen</c> and <c>RoundPainter</c>. <see cref="GameplayController"/> hands the scene over when the flow's
    /// attempt is a round attempt (<see cref="GameplayRouting"/>).
    /// <list type="bullet">
    /// <item><description>The engine-free <see cref="RoundLevelCore"/> owns the kit's <see cref="RoundPlay"/>: the display
    /// clock runs at real time × the speed in whole µs (×1, the speed pill's ×2, Auto ×2 on the timeline's tail with the whole
    /// HUD faded away and taps ignored); win and jam come from the rules' status; refusals keep the rules' reason and a tap
    /// waiting for its link partner pulses it.</description></item>
    /// <item><description>The views read the animator each frame: <see cref="RoundBoardView"/> (frame, gates, water, bridge,
    /// cubes without icons, cleared cells), <see cref="TrayView.ShowRound"/> (pods with colours, "?" then the reveal glow, link
    /// tubes under the pods), <see cref="RoundPlacesView"/> (places and their tubes), <see cref="RoundFlightsView"/> and the
    /// clearing style's walkers as tinted family heroes in two <see cref="ClearFxView"/> layers.</description></item>
    /// <item><description>A win is recorded at once with the attempt's command log hash (<see cref="GameFlow.OnLevelWon"/>;
    /// Boot passes the round content version to the leaderboard) and paid by its class; the jam card offers the boosters the
    /// rules accept now and the rewarded rescue; the guided spotlights and the introduction cards show as in the
    /// playtest.</description></item>
    /// </list>
    /// </summary>
    public sealed class RoundGameplayController : MonoBehaviour, IRoundLevelHost
    {
        private readonly HashSet<string> _devDemosSeen = new HashSet<string>(StringComparer.Ordinal);
        private readonly RoundFx _fx = new RoundFx();
        private RectTransform _root = null!;
        private GameplayHud _hud = null!;
        private RoundBoardView _board = null!;
        private ClearFxView _fxBoard = null!;
        private ClearFxView _fxOver = null!;
        private RoundPlacesView _places = null!;
        private TrayView _tray = null!;
        private RoundFlightsView _flights = null!;
        private BoosterBar _boosters = null!;
        private WinScreen _win = null!;
        private MilestoneCard _milestoneCard = null!;
        private JamScreen _jam = null!;
        private PauseScreen _pause = null!;
        private DifficultyBanner _banner = null!;
        private GuideOverlay _guide = null!;
        private RoundIntroCardView _intro = null!;
        private SettingsScreen? _settings;
        private PurchaseCardView? _confirm;
        private bool _confirming;

        private RoundAttempt? _attempt;
        private RoundLevelCore? _core;
        private ReferenceGameplayRegions? _regions;
        private ReferenceGameplayRegions? _podGrid;
        private IReadOnlyList<Box> _placeCells = Array.Empty<Box>();
        private readonly List<Box> _placeTiles = new List<Box>();
        private RoundBoardLayout? _layout;
        private (float W, float H, int Places, bool Boosters, bool Badge) _layoutKey;
        private bool _labelled;
        private bool _hasBoosters;
        private Box?[] _launch = Array.Empty<Box?>();
        private GuideStep? _shownStep;
        private int _boosterSignature = int.MinValue;
        private bool _appPaused;
        private bool _autoShown;

        private LevelReward? _reward;
        private MilestoneGrant? _milestone;
        private bool _milestoneShown;
        private bool _winShown;

        // Analytics of the current attempt (T147).
        private float _attemptStart;
        private int _attemptIndex;
        private int _peakSlots;
        private bool _jamLogged;

        /// <summary>The round level being played (engine-free), once the scene is built.</summary>
        public RoundLevelCore? Core => _core;

        private static T? Service<T>()
            where T : class =>
            AppServices.Current != null && AppServices.Current.TryGet(out T? service) ? service : null;

        private static GameFlow? Flow => Service<GameFlow>();

        private static ProgressionService? Progression => Service<ProgressionService>();

        private static EconomyService? Economy => Service<EconomyService>();

        private static GameAnalytics? Analytics => Service<GameAnalytics>();

        private static GameFeedback? Feedback => GameFeedback.Current;

        private LevelInfo? Info => _attempt == null || _core == null
            ? null
            : new LevelInfo(_attempt.LevelNumber, _attempt.Level.DefinitionVersion, _core.Difficulty, _attempt.Picture.Id);

        private long AttemptMilliseconds => (long)((Time.unscaledTime - _attemptStart) * 1000f);

        // ---- IRoundLevelHost ----

        public bool HasSeenDemo(string demoId) => Progression?.HasSeenDemo(demoId) ?? _devDemosSeen.Contains(demoId);

        public void MarkDemoSeen(string demoId)
        {
            if (Progression != null)
            {
                Progression.MarkDemoSeen(demoId);
            }
            else
            {
                _devDemosSeen.Add(demoId);
            }

            Analytics?.TutorialStep(Info, demoId, 1, true);
        }

        public bool IsUnlocked(BoosterKind kind) => Economy?.IsUnlocked(kind) ?? false;

        public bool CanAfford(BoosterKind kind) => Economy?.CanAfford(kind) ?? false;

        public int Charges(BoosterKind kind) => Economy?.Charges(kind) ?? 0;

        public bool TryTakeCharge(BoosterKind kind) => Economy?.TryTakeCharge(kind) ?? false;

        public bool SpeedUp
        {
            get => Service<PlayerSave>()?.Settings.Speed2x ?? _hud.FastForward;
            set
            {
                PlayerSave? save = Service<PlayerSave>();
                if (save != null && save.Settings.Speed2x != value)
                {
                    save.Settings.Speed2x = value;
                    Service<SaveService>()?.Save();
                }

                _hud.SetFastForward(value);
            }
        }

        public bool Symbols => Service<PlayerSave>()?.Settings.Symbols ?? false;

        public void Sound(SoundCue cue) => Feedback?.Play(cue);

        public void Collect(ClearStyle style, int step) => Feedback?.Collect(style, step);

        // ---- Building ----

        private IEnumerator Start()
        {
            Canvas canvas = UiFactory.CreateCanvas("RoundGameplayCanvas", 0);
            canvas.transform.SetParent(transform, false);
            _root = (RectTransform)canvas.transform;
            RectTransform hudRoot = UiFactory.Stretch(UiFactory.CreateRect("Hud", _root));
            _hud = GameplayHud.Create(hudRoot, OpenPause, OnSpeedChanged);

            // The board on its own full-screen layer right over the HUD's board area, under the slot panel and the tray.
            RectTransform boardLayer = UiFactory.Stretch(UiFactory.CreateRect("RoundBoard", hudRoot));
            boardLayer.SetSiblingIndex(_hud.BoardArea.GetSiblingIndex() + 1);
            _board = RoundBoardView.Create(boardLayer);
            _fxBoard = ClearFxView.Create(_board.Over, "ClearFx");
            _places = RoundPlacesView.Create(_hud.SlotArea);
            _tray = TrayView.Create(_hud.TrayArea, null, OnPodTapped);
            _boosters = BoosterBar.Create(_hud.BoosterArea, OnBoosterTilePressed);
            _boosters.Places = _hud.BoosterCells;
            _tray.Grid = _hud.PodGrid;
            _board.BridgeTapped += OnBridgeTapped;
            _board.CellTapped += OnCellTapped;
            _places.PlaceTapped += OnPlaceTapped;

            // Over the slots and the tray: the committed pods' flights, then the clearing style's items flying to the places.
            _flights = RoundFlightsView.Create(UiFactory.Stretch(UiFactory.CreateRect("RoundFlights", _root)));
            _fxOver = ClearFxView.Create(_root, "ClearFxOver");
            _fxBoard.HeroWalkers = true;
            _fxOver.HeroWalkers = true;

            // The end screens, then the pause card over them (as the legacy level: Pause stays usable during a jam).
            _win = WinScreen.Create(_root, Next);
            _milestoneCard = MilestoneCard.Create(_root, Next);
            WardrobeService? wardrobe = Service<WardrobeService>();
            if (wardrobe != null)
            {
                _win.OutfitOf = wardrobe.OutfitOf;
                _milestoneCard.OutfitOf = wardrobe.OutfitOf;
            }

            _jam = JamScreen.Create(_root, RestartFromJam, OnRecovery, _hud.TopBar);
            _pause = PauseScreen.Create(_root, ClosePause, RestartFromPause, Leave, OpenSettings);
            _banner = DifficultyBanner.Create(_root);
            _intro = RoundIntroCardView.Create(_root);
            _guide = GuideOverlay.Create(_root);

            // Let the canvas scaler size the layout before the board measures its area.
            yield return null;
            Canvas.ForceUpdateCanvases();

            _attempt = Flow?.CurrentRoundAttempt;
            if (_attempt == null)
            {
                // A round controller without a round attempt has nothing to play: back Home (never a dev level).
                Flow?.Leave();
                yield break;
            }

            int levelNumber = _attempt.LevelNumber;
            ClearStyle style = Service<ClearingService>()?.StyleFor(levelNumber) ?? ClearStyles.ForLevel(levelNumber);
            Feedback?.Prewarm(style);
            _core = new RoundLevelCore(levelNumber, _attempt.Level, style, this);
            _core.Toasted += key => _hud.Toast(Loc.T(key));
            _launch = new Box?[_attempt.Level.Pods.Count];
            _hud.SetFastForward(Service<PlayerSave>()?.Settings.Speed2x ?? false);
            _hud.SetLevel(levelNumber);
            LayoutForLevel();
            ApplyTheme(levelNumber);
            Service<AdPolicy>()?.OnAttemptStarted();
            BeginAttemptAnalytics();
            ProgressionService? progression = Progression;
            _banner.TryShow(_core.Difficulty, progression?.IsUnlocked("profile.hard") ?? true, progression?.IsUnlocked("profile.super_hard") ?? true);
        }

        /// <summary>
        /// The screen regions and the board for this level (the playtest's <c>RoundPainter.Layout</c>): the badge line of a
        /// labelled Hard or Super Hard level, the booster row once a booster is unlocked, one pod column per column of the
        /// tray, and the round board in the board area (<see cref="RoundBoardLayout.In"/>), the controls aligned to it.
        /// </summary>
        private void LayoutForLevel()
        {
            RoundLevelCore core = _core!;
            RoundLevel level = core.Round;
            ProgressionService? progression = Progression;
            DifficultyClass difficulty = core.Difficulty;
            _labelled = difficulty == DifficultyClass.Hard ? progression?.IsUnlocked("profile.hard") ?? true
                : difficulty == DifficultyClass.SuperHard && (progression?.IsUnlocked("profile.super_hard") ?? true);
            EconomyService? economy = Economy;
            _hasBoosters = false;
            foreach (BoosterKind kind in RoundLevelCore.Boosters)
            {
                _hasBoosters |= economy != null && economy.IsUnlocked(kind);
            }

            _hud.LayoutRound(_labelled, _hasBoosters, Math.Max(1, level.Tray.Count));
            _hud.SetDifficulty(difficulty, _labelled);
            Canvas.ForceUpdateCanvases();
            Relayout(force: true);
        }

        /// <summary>The regions and the board's layout for the screen and the places now (cached by both).</summary>
        private void Relayout(bool force = false)
        {
            RoundLevelCore core = _core!;
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            int places = Math.Max(RoundRules.Capacity, core.Animator.PlaceCount);
            var key = (w, h, places, _hasBoosters, _labelled);
            if (!force && _regions != null && key.Equals(_layoutKey))
            {
                return;
            }

            bool resized = _layout == null || key.w != _layoutKey.W || key.h != _layoutKey.H;
            _layoutKey = key;
            ReferenceGameplayRegions r = ScreenLayout.RoundGameplay(w, h, insets, Math.Max(1, core.Round.Tray.Count), places, _hasBoosters, _labelled);
            RoundBoardLayout board = RoundBoardLayout.In(r, core.Round);
            _regions = ScreenLayout.AlignRound(r, board.Outer);

            if (resized && _layout != null)
            {
                // A new screen size: the HUD places its areas again first.
                _hud.LayoutRound(_labelled, _hasBoosters, Math.Max(1, core.Round.Tray.Count));
            }

            // The tray's grid and the places in their areas' units, and the places' tiles on the screen: the same objects
            // until the layout changes, so the views place nothing again from frame to frame.
            _podGrid = _hud.PodGrid(Math.Max(1, core.Round.Tray.Count));
            _placeCells = _hud.SlotCells(places);
            _placeTiles.Clear();
            for (int k = 0; k < places && k < _regions.Slots.Count; k++)
            {
                _placeTiles.Add(ReferenceGameplayRegions.SlotTile(_regions.Slots[k]));
            }

            if (_layout == null || force)
            {
                _layout = board;
                _board.Build(core.Round, board, core.Symbols);
                _fx.Reset();
            }
            else if (resized)
            {
                _layout = board;
                _board.Relayout(board);
                _fx.Reset();
            }
        }

        /// <summary>The garden backdrop of the level band (FR-066), and the win's.</summary>
        private void ApplyTheme(int level)
        {
            BackgroundTheme? theme = Progression != null ? ThemeRotation.Default.ThemeFor(level) : null;
            _hud.SetTheme(theme);
            _win.SetTheme(theme);
            _milestoneCard.SetTheme(theme);
        }

        // ---- The frame ----

        private void Update()
        {
            RoundLevelCore? core = _core;
            if (core == null)
            {
                return;
            }

            core.ExternalPause = _appPaused || _pause.IsOpen || (_settings != null && _settings.IsOpen) || _confirming;
            core.JamShown = _jam.IsOpen;
            core.Tick(Time.unscaledDeltaTime);
            if (core.TakeWin())
            {
                RecordWin();
            }

            TrackAnalytics();
            SyncCards();
            SyncEnd();
            _hud.SetChromeAlpha(core.HudAlpha);
            if (core.Auto != _autoShown)
            {
                _autoShown = core.Auto;
                _hud.ShowAutoSpeed(core.Auto);
            }

            RefreshBoosters();
        }

        /// <summary>Draws the level after every Update: the board, the clearing style, the places, the columns and the flights.</summary>
        private void LateUpdate()
        {
            RoundLevelCore? core = _core;
            if (core == null || _layout == null)
            {
                return;
            }

            Relayout();
            RoundBoardLayout layout = _layout;
            _fx.Build(core.Animator, layout, core.Style, core.Play.ClockUs, Seat);
            _board.Sync(core, _fx, core.BuildableBridges(), core.UiNow);
            RenderFx(layout);

            _places.Show(core, _placeCells);
            _tray.ShowRound(core, _podGrid!, core.HudLive && !core.Targeting.HasValue);
            _flights.Show(core, _placeTiles, _launch);
        }

        /// <summary>Where a pod's tiles go in its place (the plate's tile), in cell units of the grid; null when in none.</summary>
        private Box? Seat(int pod)
        {
            int place = _core!.Animator.PlaceOf(pod);
            if (place < 0 || _regions == null || place >= _regions.Slots.Count || _layout == null)
            {
                return null;
            }

            return RoundFx.InCells(_layout, ReferenceGameplayRegions.SlotTile(_regions.Slots[place]));
        }

        /// <summary>The style's items: over the board in the grid's own units, over the slots on the root's layer.</summary>
        private void RenderFx(RoundBoardLayout layout)
        {
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float cell = layout.Cell / ppu;
            float rows = layout.Grid.Height / Mathf.Max(0.0001f, layout.Cell); // the grid's height in column pitches (oblique rows, FR-064)
            _fxBoard.Symbols = _core!.Symbols;
            _fxOver.Symbols = _core.Symbols;
            _fxBoard.Cell = cell;
            _fxBoard.Map = (x, y) => new Vector2(x * cell, (rows - y) * cell);
            _fxBoard.Render(_fx.List.Items, FxLayer.Board);
            (float _, float h, Insets _) = UiKit.ScreenFrame();
            Box grid = layout.Grid;
            float px = layout.Cell;
            _fxOver.Cell = cell;
            _fxOver.Map = (x, y) => new Vector2((grid.Left + (x * px)) / ppu, (h - (grid.Top + (y * px))) / ppu);
            _fxOver.Render(_fx.List.Items, FxLayer.Over);
        }

        // ---- Cards: the introduction, the guided spotlights ----

        private void SyncCards()
        {
            RoundLevelCore core = _core!;
            if (core.Guide != null && (core.Won || core.Blocked))
            {
                core.SkipGuide();
            }

            if (core.Intro != null)
            {
                if (!_intro.IsOpen)
                {
                    _intro.Show(core.Intro, core.CloseIntro);
                }

                HideGuide();
                return;
            }

            GuideStep? step = core.Guide;
            if (step == null)
            {
                HideGuide();
                return;
            }

            if (!ReferenceEquals(step, _shownStep))
            {
                _shownStep = step;
                Analytics?.TutorialStep(Info, step.DemoId, 1, false);
                Action<RectTransform>? icon = step.Booster.HasValue
                    ? GuideOverlay.BoosterIcon(GuideTour.Key(step.Booster.Value))
                    : step.Kind == GuideKind.Blocked ? MaterialIcon(GuideTour.BlockingMaterial(core.Play.Timeline)) : null;
                _guide.Show(step, () => GuideHoles(step), icon, core.NextGuideStep);
            }
        }

        private void HideGuide()
        {
            if (_shownStep != null || _guide.IsShowing)
            {
                _shownStep = null;
                _guide.Hide();
            }
        }

        /// <summary>The bubble's icon for the blocked entry: the material tile that blocks the way.</summary>
        private Action<RectTransform>? MaterialIcon(int material)
        {
            if (material < 0)
            {
                return null;
            }

            bool symbols = Symbols;
            return slot =>
            {
                UnityEngine.UI.Image tile = UiFactory.CreateImage("Tile", slot, RoundSprites.PodCube(material, RoundPodLook.Placed, symbols, 128), Color.white);
                tile.preserveAspect = true;
                UiFactory.Stretch(tile.rectTransform);
            };
        }

        /// <summary>The places a guided step lights, on screen now (the first one ringed).</summary>
        private List<Box> GuideHoles(GuideStep step)
        {
            var holes = new List<Box>();
            RoundLevelCore? core = _core;
            if (core == null)
            {
                return holes;
            }

            switch (step.Kind)
            {
                case GuideKind.Entry:
                    AddUnion(holes, _board.ArchBoxes());
                    break;
                case GuideKind.Blocked:
                    AddUnion(holes, _board.ArchBoxes());
                    foreach (int cell in GuideTour.BlockingCells(core.Play.Timeline))
                    {
                        holes.Add(_board.CellBox(cell));
                    }

                    break;
                case GuideKind.FirstTap:
                    if (core.GuidePod >= 0 && _tray.RoundRectOf(core.GuidePod) is RectTransform guided)
                    {
                        holes.Add(_guide.ScreenOf(guided));
                    }

                    break;
                case GuideKind.BoosterTarget when step.Booster == BoosterKind.Return:
                {
                    var plates = new List<Box>();
                    for (int k = 0; k < core.Animator.PlaceCount; k++)
                    {
                        int pod = core.Animator.PodIn(k);
                        if (pod >= 0 && core.CanReturn(pod) && _places.PlateRect(k) is RectTransform plate)
                        {
                            plates.Add(_guide.ScreenOf(plate));
                        }
                    }

                    AddUnion(holes, plates);
                    break;
                }

                case GuideKind.BoosterTarget:
                    if (_layout != null)
                    {
                        holes.Add(_layout.Grid);
                    }

                    break;
                default:
                    if (step.Booster.HasValue)
                    {
                        holes.Add(_guide.ScreenOf(_boosters.RectOf(step.Booster.Value)));
                    }

                    break;
            }

            return holes;
        }

        private static void AddUnion(List<Box> holes, IEnumerable<Box> boxes)
        {
            Box? all = null;
            foreach (Box b in boxes)
            {
                all = all.HasValue ? new Box(Mathf.Min(all.Value.Left, b.Left), Mathf.Min(all.Value.Top, b.Top), Mathf.Max(all.Value.Right, b.Right), Mathf.Max(all.Value.Bottom, b.Bottom)) : b;
            }

            if (all.HasValue)
            {
                holes.Add(all.Value);
            }
        }

        // ---- The end: the win and the jam ----

        private void SyncEnd()
        {
            RoundLevelCore core = _core!;
            if (!core.Play.Settled || core.Intro != null)
            {
                return;
            }

            if (core.Won && !_winShown)
            {
                _winShown = true;
                _jam.Hide();
                LevelReward? earned = _reward;
                _win.Show(this, RewardText(earned), DoubleRewardOffer(), _milestone != null, earned != null && earned.Petals > 0 ? (earned.Petals, n => RewardText(earned with { Petals = (int)n })) : null, earned, null, WinPicture());
                return;
            }

            if (core.Blocked && !core.JamHidden && !_jam.IsOpen && !_pause.IsOpen && !_confirming)
            {
                ShowJam();
            }
            else if (!core.Blocked && _jam.IsOpen)
            {
                _jam.Hide();
            }
        }

        private RoundWinPicture WinPicture()
        {
            RoundAttempt attempt = _attempt!;
            RoundLevel level = attempt.Level;
            var colors = new List<Rgba>();
            foreach (RoundPod pod in level.Pods)
            {
                Rgba c = RoundLook.ColorOf(pod.Code);
                if (!colors.Contains(c))
                {
                    colors.Add(c);
                }
            }

            RoundPicture picture = attempt.Picture;
            return new RoundWinPicture(attempt.LevelNumber, picture.Width, picture.Height, picture.RowsTopFirst, colors, Symbols);
        }

        /// <summary>
        /// The jam card (FR-012): the boosters the rules accept now that are unlocked and owned or affordable, the rewarded
        /// rescue, the places' contents and Restart.
        /// </summary>
        private void ShowJam()
        {
            RoundLevelCore core = _core!;
            var recoveries = new List<Recovery>();
            foreach (BoosterKind kind in core.JamBoosters())
            {
                recoveries.Add(RecoveryOf(kind));
            }

            var slots = new List<JamSlot>();
            RoundAnimator a = core.Animator;
            for (int k = 0; k < a.PlaceCount; k++)
            {
                int pod = a.PodIn(k);
                slots.Add(pod < 0 ? new JamSlot(SlotPlateState.Empty, null, 0) : new JamSlot(SlotPlateState.Working, null, a.Remaining(pod), a.MaterialOf(pod), core.Symbols));
            }

            _jam.Show(core.Play.Status == RoundStatus.Stuck, recoveries, RecoveryCost, slots, RescueOffer());
        }

        /// <summary>The jam rescue: a rewarded ad for one free use of a jam-resolving booster, once per attempt (FR-027, FR-048).</summary>
        private (Recovery Booster, Action Watch)? RescueOffer()
        {
            IAdsService? ads = Service<IAdsService>();
            AdPolicy? policy = Service<AdPolicy>();
            (BoosterKind Kind, RoundInput Command)? rescue = _core!.RescueBooster();
            if (ads == null || policy == null || !ads.IsRewardedReady || !policy.MayOfferRescue || rescue == null)
            {
                return null;
            }

            (BoosterKind kind, RoundInput command) = rescue.Value;
            return (RecoveryOf(kind), () =>
            {
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

                        _jam.Hide();
                        Use(kind, command, free: true);
                    }
                });
            });
        }

        /// <summary>Records the win as soon as the rules say so (a kill during the celebration keeps it), and pays it.</summary>
        private void RecordWin()
        {
            RoundLevelCore core = _core!;
            RoundAttempt attempt = _attempt!;
            GameFlow? flow = Flow;
            if (flow != null && flow.OnLevelWon(attempt.LevelNumber, LeaderboardClient.CommandLogHash(core.Inputs)) && Economy != null)
            {
                _reward = Economy.GrantLevelReward(attempt.LevelNumber, core.Difficulty, core.BoostersPaid);
                MilestoneGrant? grant = Service<MilestoneService>()?.LastGrant;
                _milestone = grant != null && grant.Level == attempt.LevelNumber ? grant : null;
            }

            LevelInfo? info = Info;
            if (info != null)
            {
                int boosters = core.BoostersPaid;
                Analytics?.LevelWin(info, AttemptMilliseconds, Taps(core), boosters, boosters == 0, _peakSlots, _attemptIndex);
            }
        }

        // ---- Input ----

        private void OnPodTapped(string id)
        {
            RoundLevelCore? core = _core;
            if (core == null || _pause.IsOpen || !int.TryParse(id, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int pod))
            {
                return;
            }

            // Where each member's tile is now: its flight to its place starts there.
            foreach (int member in core.Animator.GroupOf(pod))
            {
                _launch[member] = TileScreenBox(member);
            }

            RoundTapOutcome outcome = core.Tap(pod);
            if (outcome == RoundTapOutcome.Refused)
            {
                _tray.ShowRefused(id);
            }
        }

        /// <summary>A shown pod's tile box in screen pixels, from the tray's own units.</summary>
        private Box? TileScreenBox(int pod)
        {
            Box? local = _tray.RoundTileBox(pod);
            ReferenceGameplayRegions? hud = _hud.Regions;
            if (!local.HasValue || hud == null)
            {
                return null;
            }

            float ppu = UiKit.PixelsPerUnit;
            Box row = hud.PodRow;
            Box b = local.Value;
            return new Box(row.Left + (b.Left * ppu), row.Top + (b.Top * ppu), row.Left + (b.Right * ppu), row.Top + (b.Bottom * ppu));
        }

        private void OnBridgeTapped(int choice)
        {
            if (_core != null && !_pause.IsOpen)
            {
                _core.BuildBridge(choice);
            }
        }

        private void OnCellTapped(int cell)
        {
            RoundLevelCore? core = _core;
            if (core == null || core.Targeting != BoosterKind.BloomBurst)
            {
                return;
            }

            int material = core.Animator.MaterialAt(cell);
            if (material >= 0 && core.CanBurst(material))
            {
                Use(BoosterKind.BloomBurst, RoundInput.BloomBurst(RoundMaterial.All[material].Code, 0));
            }
        }

        private void OnPlaceTapped(int place)
        {
            RoundLevelCore? core = _core;
            if (core == null || core.Targeting != BoosterKind.Return)
            {
                return;
            }

            int pod = core.Animator.PodIn(place);
            if (pod >= 0)
            {
                Use(BoosterKind.Return, RoundInput.Return(pod, 0));
            }
        }

        private void OnSpeedChanged(bool fast)
        {
            if (_core != null)
            {
                SpeedUp = fast;
            }
        }

        // ---- Boosters ----

        /// <summary>A booster's tile: with no charge left (and no guided step) it buys a pack first, as the legacy bar does.</summary>
        private void OnBoosterTilePressed(BoosterKind kind)
        {
            RoundLevelCore? core = _core;
            EconomyService? economy = Economy;
            if (core == null || _pause.IsOpen)
            {
                return;
            }

            if (core.Guide != null || economy == null || economy.Charges(kind) > 0)
            {
                OnBoosterPressed(kind);
                return;
            }

            if (!core.TakesBoosterPress(kind) || core.Won)
            {
                return;
            }

            core.CancelTargeting();
            Confirm(kind, () =>
            {
                if (!economy.TryBuy(kind))
                {
                    _hud.Toast(Loc.T("gameplay.not_enough_petals"));
                }
            });
        }

        private void OnBoosterPressed(BoosterKind kind)
        {
            RoundLevelCore core = _core!;
            string source = Source(kind, false);
            RoundBoosterOutcome outcome = core.PressBooster(kind);
            switch (outcome)
            {
                case RoundBoosterOutcome.Used:
                    AfterBooster(kind, source);
                    break;
                case RoundBoosterOutcome.NeedsPurchase:
                    ConfirmUse(kind, kind == BoosterKind.ExtraSlot ? RoundInput.ExtraSlot(0) : RoundInput.Shuffle(0));
                    break;
            }
        }

        private void OnRecovery(Recovery recovery)
        {
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

        /// <summary>Uses a booster command now; one that would buy its charge asks the purchase confirmation first.</summary>
        private void Use(BoosterKind kind, RoundInput command, bool free = false)
        {
            RoundLevelCore core = _core!;
            string source = Source(kind, free);
            RoundBoosterOutcome outcome = core.UseBooster(kind, command, free);
            if (outcome == RoundBoosterOutcome.Used)
            {
                AfterBooster(kind, source);
            }
            else if (outcome == RoundBoosterOutcome.NeedsPurchase)
            {
                ConfirmUse(kind, command);
            }
        }

        private void ConfirmUse(BoosterKind kind, RoundInput command) =>
            Confirm(kind, () =>
            {
                string source = Source(kind, false);
                if (_core!.UseBooster(kind, command, confirmed: true) == RoundBoosterOutcome.Used)
                {
                    AfterBooster(kind, source);
                }
            });

        /// <summary>The purchase confirmation (spec 005 FR-040) for a booster pack; <paramref name="buy"/> runs on its Buy.</summary>
        private void Confirm(BoosterKind kind, Action buy)
        {
            EconomyService? economy = Economy;
            if (economy == null)
            {
                return;
            }

            _confirm ??= UiKit.PurchaseCard(_root);
            _confirming = true;
            string boosterId = MethodName(kind);
            _confirm.ShowPetals(
                PurchaseOffer.ForPetals(Loc.F("store.booster_quantity", BoosterTitle(kind), EconomyService.PurchaseQuantity(kind)), economy.Price(kind)),
                economy.Petals,
                well => UiFactory.Place(UiKit.BoosterIcon("Icon", well, boosterId).rectTransform, 0.08f, 0.08f, 0.92f, 0.92f),
                () =>
                {
                    _confirming = false;
                    buy();
                },
                () => _confirming = false,
                () => _confirming = false);
        }

        private string Source(BoosterKind kind, bool free)
        {
            RoundLevelCore core = _core!;
            GuideStep? step = core.Guide;
            bool demo = step != null && step.Booster == kind;
            return demo ? "demo" : free ? "ad" : Charges(kind) > 0 ? "charge" : "petals";
        }

        private void AfterBooster(BoosterKind kind, string source)
        {
            LevelInfo? info = Info;
            if (info != null)
            {
                Analytics?.BoosterUse(info, MethodName(kind), source);
            }

            _boosters.Pulse(kind);
            _jam.Hide();
            if (kind == BoosterKind.Shuffle)
            {
                _tray.ResetRound();
            }
        }

        private void RefreshBoosters()
        {
            RoundLevelCore core = _core!;
            EconomyService? economy = Economy;
            int signature = 17;
            foreach (BoosterKind kind in RoundLevelCore.Boosters)
            {
                bool unlocked = economy == null || economy.IsUnlocked(kind);
                bool applicable = core.Applicable(kind);
                int charges = economy?.Charges(kind) ?? 1;
                bool affordable = economy == null || economy.Petals >= economy.Price(kind);
                signature = (signature * 31) + (unlocked ? 1 : 0) + (applicable ? 2 : 0) + (affordable ? 4 : 0) + (charges * 8);
            }

            signature = (signature * 31) + (core.Targeting.HasValue ? (int)core.Targeting.Value + 1 : 0);
            if (signature == _boosterSignature)
            {
                return;
            }

            _boosterSignature = signature;
            _boosters.Refresh(kind => new BoosterButtonState(
                economy == null || economy.IsUnlocked(kind),
                core.Applicable(kind),
                economy?.Charges(kind) ?? 1,
                economy?.Price(kind) ?? 0,
                economy == null || economy.Petals >= economy.Price(kind)));
            _boosters.SetTargeting(core.Targeting);
        }

        /// <summary>A recovery's cost on the jam card: ×N charges while owned, else the lotus and its price.</summary>
        private static Cost? RecoveryCost(Recovery recovery)
        {
            EconomyService? economy = Economy;
            if (economy == null)
            {
                return null;
            }

            BoosterKind kind = KindOf(recovery);
            return economy.Charges(kind) > 0 ? Cost.Charges(economy.Charges(kind)) : Cost.Petals(economy.Price(kind));
        }

        /// <summary>The doubled win reward: the same Petals again after a rewarded ad (FR-052).</summary>
        private Action<Action<string>>? DoubleRewardOffer()
        {
            IAdsService? ads = Service<IAdsService>();
            LevelReward? reward = _reward;
            EconomyService? economy = Economy;
            if (ads == null || reward == null || !ads.IsRewardedReady || economy == null)
            {
                return null;
            }

            return update => ads.ShowRewarded(AdPlacements.DoubleWin, earned =>
            {
                if (earned)
                {
                    Analytics?.AdRewarded("double_reward");
                    economy.Grant(reward.Petals, null);
                    update(RewardText(reward with { Petals = reward.Petals * 2 }));
                }
            });
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

        private static string MethodName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => "extra_slot",
            BoosterKind.Shuffle => "shuffle",
            BoosterKind.Return => "return",
            _ => "bloom_burst",
        };

        private static string BoosterTitle(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => Loc.T("booster.extra_slot"),
            BoosterKind.Shuffle => Loc.T("booster.shuffle"),
            BoosterKind.Return => Loc.T("booster.return"),
            _ => Loc.T("booster.bloom_burst"),
        };

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

        // ---- Flow ----

        private void OpenPause()
        {
            _pause.Show();
        }

        /// <summary>Settings from the pause card (spec 002 FR-018): the same toggles as on Home, the symbols mode among them.</summary>
        private void OpenSettings()
        {
            if (_settings == null && Service<PlayerSave>() is PlayerSave save && Service<SaveService>() is SaveService saves)
            {
                _settings = SettingsScreen.Create(_root, save.Settings, saves.Save);
            }

            _settings?.Show();
        }

        private void ClosePause() => _pause.Hide();

        private void RestartFromPause()
        {
            ClosePause();
            Restart("pause");
        }

        /// <summary>The jam card's Restart; inert while the pause card is open over it.</summary>
        private void RestartFromJam()
        {
            if (!_pause.IsOpen)
            {
                Restart("jam");
            }
        }

        /// <summary>Restart rebuilds the same level for free (FR-028, FR-040).</summary>
        private void Restart(string from)
        {
            RoundLevelCore? core = _core;
            if (core == null)
            {
                return;
            }

            LevelInfo? info = Info;
            if (info != null)
            {
                Analytics?.LevelRestart(info, from);
            }

            core.Restart();
            Array.Clear(_launch, 0, _launch.Length);
            _fx.Reset();
            _tray.ResetRound();
            _flights.Clear();
            _fxBoard.Clear();
            _fxOver.Clear();
            _win.Hide();
            _jam.Hide();
            _milestoneCard.Hide();
            HideGuide();
            _reward = null;
            _milestone = null;
            _milestoneShown = false;
            _winShown = false;
            _boosterSignature = int.MinValue;
            Service<AdPolicy>()?.OnAttemptStarted();

            // The board stays built: it shows the fresh attempt from the animator on the next frame.
            Relayout();
            BeginAttemptAnalytics();
        }

        private void Next()
        {
            // A milestone win shows its milestone card first (spec 002 FR-021), then Continue goes on.
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
            if (info != null && _core != null && !_core.Won)
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

        /// <summary>Pauses with the app and resumes exactly where the clock stopped.</summary>
        private void OnApplicationPause(bool paused) => _appPaused = paused;

        // ---- Analytics ----

        private void BeginAttemptAnalytics()
        {
            _attemptIndex++;
            _attemptStart = Time.unscaledTime;
            _peakSlots = 0;
            _jamLogged = false;
            LevelInfo? info = Info;
            GameAnalytics? analytics = Analytics;
            if (info != null && analytics != null)
            {
                analytics.SetLevelContext(info);
                analytics.LevelStart(info, _attemptIndex);
            }
        }

        /// <summary>Peak places in use, and <c>level_jam</c> once when the level blocks.</summary>
        private void TrackAnalytics()
        {
            RoundLevelCore core = _core!;
            RoundAnimator a = core.Animator;
            int used = 0;
            for (int k = 0; k < a.PlaceCount; k++)
            {
                used += a.PodIn(k) >= 0 ? 1 : 0;
            }

            _peakSlots = Math.Max(_peakSlots, used);
            if (core.Blocked && core.Play.Settled && !_jamLogged)
            {
                _jamLogged = true;
                LevelInfo? info = Info;
                if (info != null)
                {
                    int work = 0;
                    foreach (sbyte cell in core.Play.Timeline.State.Board)
                    {
                        work += cell >= 0 ? 1 : 0;
                    }

                    Analytics?.LevelJam(info, core.Play.Status == RoundStatus.Stuck ? "stuck" : "jam", AttemptMilliseconds, Taps(core), used, work);
                }
            }
            else if (core.Play.Status == RoundStatus.Playing)
            {
                _jamLogged = false;
            }
        }

        /// <summary>Pod taps of this attempt.</summary>
        private static int Taps(RoundLevelCore core)
        {
            int taps = 0;
            foreach (RoundInput input in core.Inputs)
            {
                taps += input.Type == RoundInputType.Tap ? 1 : 0;
            }

            return taps;
        }
    }
}
