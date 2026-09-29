using System.Collections.Generic;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.App;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Slots;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Gameplay.Tray;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Client.UI.Tutorial;
using Bloomlings.Client.UI;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using UnityEngine;

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
        private LevelSession? _session;
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
        private readonly System.Collections.Generic.HashSet<string> _devDemosSeen = new System.Collections.Generic.HashSet<string>();

        public LevelSession? Session => _session;

        private IEnumerator Start()
        {
            Canvas canvas = UiFactory.CreateCanvas("GameplayCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            _hud = GameplayHud.Create(UiFactory.Stretch(UiFactory.CreateRect("Hud", root)), OpenPause, doubleSpeed => _timeline.Speed = doubleSpeed ? 2f : 1f);
            _board = BoardView.Create(_hud.BoardArea, _visuals);
            _slots = SlotRowView.Create(_hud.SlotArea, _visuals);
            _tray = TrayView.Create(_hud.TrayArea, _visuals, OnPodTapped);
            _timeline = gameObject.AddComponent<EventTimeline>();
            _timeline.Bind(this);
            _workers = WorkerPool.Create(gameObject, _board, _timeline, _visuals, _workerCapacity);
            _pause = PauseScreen.Create(root, ClosePause, RestartFromPause, Leave);
            _win = WinScreen.Create(root, Next);
            _jam = JamScreen.Create(root, Restart, _ => { });
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
            if (AppServices.Current != null && AppServices.Current.TryGet(out PlayerSave? save) && save!.Settings.Speed2x)
            {
                _hud.SetDoubleSpeed(true);
                _timeline.Speed = 2f;
            }

            RebuildViews();
            ShowLevelIntro();
        }

        // ---- Input ----

        private void OnPodTapped(string podId)
        {
            if (_session == null || _pause.IsOpen)
            {
                return;
            }

            var tap = new TapPod(podId);
            CommandCheck check = _session.Check(tap);
            if (!check.IsAllowed)
            {
                _tray.ShowRefused(podId);
                _hud.Toast(RefusalText(check.Reason!.Value));
                return;
            }

            int countBefore = _session.View.Pod(podId).Remaining;
            CommandResult result = _session.Apply(tap);
            foreach (GameEvent e in result.Events)
            {
                if (e.Round != 0)
                {
                    break;
                }

                switch (e)
                {
                    case PodCommitted committed:
                        _slots.Commit(committed.SlotIndex, committed.PodId, _session.View.Pod(committed.PodId).Variant, countBefore);
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

            // Record a win as soon as it happens logically, so a kill during the win animation keeps it (R15).
            if (_session.Status == LevelStatus.Won && Flow != null && Flow.CurrentAttempt != null)
            {
                Flow.OnLevelWon(Flow.CurrentAttempt.LevelNumber);
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
            switch (unit.Reveal)
            {
                case CellOpened opened:
                    _board.ShowOpened(opened.Cell);
                    break;
                case LayerRevealed layer:
                    _board.ShowLayer(layer.Cell, layer.NewTopVariant, _session!.View);
                    break;
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
                    _slots.Complete(completed.PodId);
                    break;
                case MysteryTileRevealed revealed:
                    _board.ShowMysteryRevealed(revealed.Cell, revealed.Variant);
                    break;
                case LevelWon _:
                    _board.RevealAll();
                    _win.Show(this, "Reward: coming with Petals (US5)");
                    break;
                case LevelJammed jammed:
                    _jam.Show(stuck: false, jammed.EligibleRecoveries);
                    break;
                case LevelStuck stuck:
                    _jam.Show(stuck: true, stuck.EligibleRecoveries);
                    break;
            }
        }

        // ---- Flow ----

        private void OpenPause()
        {
            _timeline.Paused = true;
            _pause.Show();
        }

        private void ClosePause()
        {
            _pause.Hide();
            _timeline.Paused = false;
        }

        private void RestartFromPause()
        {
            ClosePause();
            Restart();
        }

        /// <summary>Restart rebuilds the same level for free (FR-028, FR-040).</summary>
        private void Restart()
        {
            if (_session == null)
            {
                return;
            }

            _session.Apply(new Restart());
            RebuildViews();
        }

        private static GameFlow? Flow => AppServices.Current != null && AppServices.Current.TryGet(out GameFlow? flow) ? flow : null;

        private static ProgressionService? Progression =>
            AppServices.Current != null && AppServices.Current.TryGet(out ProgressionService? progression) ? progression : null;

        private void Next()
        {
            if (Flow != null)
            {
                Flow.Next();
            }
            else
            {
                Restart();
            }
        }

        private void Leave()
        {
            ClosePause();
            if (Flow != null)
            {
                Flow.Leave();
            }
            else
            {
                Restart();
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

            if (session.Definition.LevelNumber == 1 && !SeenDemo(DemoScripts.FirstTapId))
            {
                string? pod = RecommendedFirstPod(session);
                _demo.Show(DemoScripts.FirstTap(() => pod == null ? null : _tray.RectOf(pod)), OnDemoDone);
                return;
            }

            (VariantId First, VariantId Second)? siblings = SiblingPair(session);
            if (siblings.HasValue && !SeenDemo(DemoScripts.SiblingsId))
            {
                VariantVisual first = _visuals != null ? _visuals.Get(siblings.Value.First) : VariantVisualCatalog.Default(siblings.Value.First);
                VariantVisual second = _visuals != null ? _visuals.Get(siblings.Value.Second) : VariantVisualCatalog.Default(siblings.Value.Second);
                _demo.Show(DemoScripts.Siblings(first, second), OnDemoDone);
            }
        }

        private bool SeenDemo(string demoId) => Progression?.HasSeenDemo(demoId) ?? _devDemosSeen.Contains(demoId);

        private void OnDemoDone(DemoScript script)
        {
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
            _hud.SetLevel(session.Definition.LevelNumber);
            _board.Build(session.View, session.Definition, session.Picture);
            _workers.SetEntries(session.View.Entries);
            _slots.Reset(session.View);
            _tray.Refresh(session.View);
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
            RejectReason.NoFreeSlot => "No free slot",
            RejectReason.NotExposed => "Take the top pod first",
            RejectReason.Locked => "Locked: collect its key",
            RejectReason.NotEnoughSlotsForGroup => "Needs more free slots",
            _ => string.Empty,
        };
    }
}
