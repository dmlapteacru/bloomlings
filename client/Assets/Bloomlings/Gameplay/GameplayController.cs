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
using Bloomlings.Client.Services.Economy;
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
        private readonly System.Collections.Generic.HashSet<string> _devDemosSeen = new System.Collections.Generic.HashSet<string>();

        public LevelSession? Session => _session;

        private IEnumerator Start()
        {
            Canvas canvas = UiFactory.CreateCanvas("GameplayCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            _root = root;
            _hud = GameplayHud.Create(UiFactory.Stretch(UiFactory.CreateRect("Hud", root)), OpenPause, doubleSpeed => _timeline.Speed = doubleSpeed ? 2f : 1f);
            _board = BoardView.Create(_hud.BoardArea, _visuals);
            _slots = SlotRowView.Create(_hud.SlotArea, _visuals);
            _tray = TrayView.Create(_hud.TrayArea, _visuals, OnPodTapped);
            _boosters = BoosterBar.Create(_hud.BoosterArea, OnBoosterPressed);
            _slots.SlotTapped += OnSlotTapped;
            _board.CellTapped += OnCellTapped;
            _timeline = gameObject.AddComponent<EventTimeline>();
            _timeline.Bind(this);
            _workers = WorkerPool.Create(gameObject, _board, _timeline, _visuals, _workerCapacity);
            _pause = PauseScreen.Create(root, ClosePause, RestartFromPause, Leave);
            _win = WinScreen.Create(root, Next);
            _jam = JamScreen.Create(root, Restart, OnRecovery);
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

            if (_targeting != null)
            {
                CancelTargeting();
            }

            var tap = new TapPod(podId);
            CommandCheck check = _session.Check(tap);
            if (!check.IsAllowed)
            {
                _tray.ShowRefused(podId);
                _hud.Toast(RefusalText(check.Reason!.Value));
                if (check.Reason == RejectReason.Locked)
                {
                    FlashKeysOf(podId);
                }

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

            RecordWinIfWon();
            RefreshBoosters();
        }

        /// <summary>Records a win as soon as it happens logically, so a kill during the win animation keeps it (R15), and pays it.</summary>
        private void RecordWinIfWon()
        {
            if (_session!.Status != LevelStatus.Won || Flow == null || Flow.CurrentAttempt == null)
            {
                return;
            }

            if (Flow.OnLevelWon(Flow.CurrentAttempt.LevelNumber) && Economy != null)
            {
                _reward = Economy.GrantLevelReward(Flow.CurrentAttempt.LevelNumber, _session.Definition.Difficulty.Class, _session.BoostersUsed);
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
                case KeyCollected key:
                    FlyKey(key);
                    break;
                case SpecialProgressed progressed:
                    _board.ShowSpecialProgress(progressed.SpecialId, progressed.Progress, progressed.Total, SpecialKind(progressed.SpecialId));
                    break;
                case SpecialTriggered triggered:
                    _board.TriggerSpecial(triggered.SpecialId, triggered.EffectCells, _session!.View);
                    break;
                case LevelWon _:
                    _board.RevealAll();
                    _win.Show(this, RewardText(_reward));
                    break;
                case LevelJammed _:
                case LevelStuck _:
                    // The recoveries of the current state (the timeline may be behind), owned or affordable (FR-027).
                    if (_session!.Status == LevelStatus.Jammed || _session.Status == LevelStatus.Stuck)
                    {
                        var usable = new List<Recovery>();
                        foreach (Recovery recovery in _session.EligibleRecoveries())
                        {
                            if (Economy == null || Economy.CanAfford(KindOf(recovery)))
                            {
                                usable.Add(recovery);
                            }
                        }

                        _jam.Show(_session.Status == LevelStatus.Stuck, usable, RecoveryLabel);
                    }

                    break;
            }
        }

        // ---- Boosters (T120, T121) ----

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
                    StartTargeting(kind, "Tap a waiting pod to send it back");
                    break;
                default:
                    StartTargeting(kind, "Tap a tile to clear its symbol everywhere");
                    break;
            }
        }

        private void OnRecovery(Recovery recovery)
        {
            _jam.Hide();
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
            }
        }

        /// <summary>
        /// Uses a booster: the level checks it first (FR-046), then a charge is taken, or bought with Petals; without
        /// either, the player is told, and the Store is never forced (FR-027). The pending animation is played out first,
        /// so the slots and tray are rebuilt from the settled state.
        /// </summary>
        private void UseBooster(BoosterKind kind, Command command)
        {
            LevelSession session = _session!;
            CommandCheck check = session.Check(command);
            if (!check.IsAllowed)
            {
                _hud.Toast("That booster can't help here");
                return;
            }

            if (Economy != null && !Economy.TryTakeCharge(kind))
            {
                _hud.Toast("Not enough Petals");
                return;
            }

            _timeline.Flush();
            _workers.RecallAll();
            _workInFlight.Clear();
            CommandResult result = session.Apply(command);
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
                        foreach (Core.Boards.CellPos cell in burst.Cells)
                        {
                            _board.Refresh(session.View, cell);
                        }

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

            _slots.Reset(session.View);
            _tray.Refresh(session.View);
            _timeline.Enqueue(result.Events);
            RecordWinIfWon();
            RefreshBoosters();
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

            string text = "+" + reward.Petals.ToString(System.Globalization.CultureInfo.InvariantCulture) + " Petals";
            return reward.DroppedBooster.HasValue ? text + "  +1 " + JamScreen.Label(RecoveryOf(reward.DroppedBooster.Value)) : text;
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

            KeyView.Fly(_root, from, to, _board.CellSize * 0.8f, () =>
            {
                if (lockDef.TargetKind == LockTargetKind.Slot)
                {
                    _slots.PlayUnlock(int.Parse(lockDef.TargetId, System.Globalization.CultureInfo.InvariantCulture), _session!.View);
                }
                else if (lockDef.TargetKind == LockTargetKind.Pod)
                {
                    _tray.ShowAccepted(lockDef.TargetId);
                }
            });
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

            // A booster unlocked on the way here: its demo and free charge (FR-042, T122).
            foreach ((string unlockId, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                if (progression != null && progression.IsUnlocked(unlockId) && !SeenDemo(unlockId))
                {
                    DemoScript? demo = BoosterDemos.For(unlockId, () => _boosters.RectOf(kind));
                    if (demo != null)
                    {
                        _demo.Show(demo, OnDemoDone);
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
                    _demo.Show(demo, OnDemoDone);
                    return;
                }
            }

            (VariantId First, VariantId Second)? siblings = SiblingPair(session);
            if (siblings.HasValue && !SeenDemo(DemoScripts.SiblingsId))
            {
                VariantVisual first = _visuals != null ? _visuals.Get(siblings.Value.First) : VariantVisualCatalog.Default(siblings.Value.First);
                VariantVisual second = _visuals != null ? _visuals.Get(siblings.Value.Second) : VariantVisualCatalog.Default(siblings.Value.Second);
                _demo.Show(DemoScripts.Siblings(first, second), OnDemoDone);
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
            };
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
            _reward = null;
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
            RejectReason.NoFreeSlot => "No free slot",
            RejectReason.NotExposed => "Take the top pod first",
            RejectReason.Locked => "Locked: collect its key",
            RejectReason.NotEnoughSlotsForGroup => "Needs more free slots",
            _ => string.Empty,
        };
    }
}
