using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
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
            Animator.Speed = app.Meta.Save.Settings.Speed2x ? 2f : 1f;
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

        /// <summary>Level 1's one-line hint above the board until the first tap (spec 001 US2).</summary>
        public bool FirstTapHint { get; private set; }

        /// <summary>The win card is closed and the milestone card shows (frame 16).</summary>
        public bool ShowingMilestone { get; private set; }

        /// <summary>When the end card (win or jam) first showed, for its rise and pop.</summary>
        public float EndShownAt { get; set; } = -1f;

        /// <summary>Where each tray pod was drawn last frame (flights start there).</summary>
        public Dictionary<string, Box> PodBoxes { get; } = new Dictionary<string, Box>(StringComparer.Ordinal);

        /// <summary>Where each slot was drawn last frame.</summary>
        public Box?[] SlotBoxes { get; } = new Box?[WaitingSlots.Capacity];

        /// <summary>The board's cell size and origin, for walkers and flights.</summary>
        public (float Ox, float Oy, float Cell, int Height) Board { get; set; }

        public bool Won => Session.Status == LevelStatus.Won;

        public bool Blocked => Session.Status == LevelStatus.Jammed || Session.Status == LevelStatus.Stuck;

        public bool NeedsFrames => !Animator.Idle || (LastBooster.HasValue && Animator.Now - LastBooster.Value.At < 0.7f) || (_toast != null && _app.Now < _toastUntil) || (EndShownAt >= 0f && _app.Now - EndShownAt < 2.3f) || ShowingMilestone || Targeting.HasValue || (Demo != null && _app.Now - DemoOpenedAt < 0.3f);

        public void Advance(float dt) => Animator.Advance(dt, Session.View);

        public string? ToastText => _toast != null && _app.Now < _toastUntil ? _toast : null;

        public void Toast(string message)
        {
            _toast = message;
            _toastUntil = _app.Now + 1.6f;
        }

        // ---- Before play ----

        /// <summary>At most one demo before play (FR-031, FR-042, FR-059, FR-071).</summary>
        private void ShowIntro()
        {
            FirstTapHint = Level == 1 && !Meta.HasSeenDemo("system.core");
            if (FirstTapHint)
            {
                return;
            }

            foreach ((BoosterKind kind, Recovery _, string id) in Boosters)
            {
                string unlockId = "booster." + id;
                if (Meta.Economy.IsUnlocked(kind) && !Meta.HasSeenDemo(unlockId))
                {
                    OpenDemo(new DemoCard(unlockId, Lines("demo." + id), Array.Empty<VariantId>(), false));
                    return;
                }
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
            if (Targeting != null || Demo != null)
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

            var before = new Dictionary<string, (int Count, VariantId? Variant, float X, float Y)>(StringComparer.Ordinal);
            var members = new List<string>(Session.View.ConnectedGroup(podId)) { podId };
            foreach (string member in members)
            {
                PodInfo info = Session.View.Pod(member);
                Box? box = PodBoxes.TryGetValue(member, out Box b) ? b : (Box?)null;
                before[member] = (info.Remaining, info.Variant, box?.CenterX ?? float.NaN, box?.CenterY ?? float.NaN);
            }

            CommandResult result = Session.Apply(tap);
            _app.Sound.Play(SoundCue.Tap);
            if (FirstTapHint)
            {
                FirstTapHint = false;
                Meta.MarkDemoSeen("system.core");
            }

            Animator.Tapped(result, Session.View, before);
            AfterCommand();
        }

        public void UseBooster(BoosterKind kind, Command command, bool free = false)
        {
            Targeting = null;
            CommandCheck check = Session.Check(command);
            if (!check.IsAllowed)
            {
                Toast(PlaytestText.T("gameplay.booster_useless"));
                return;
            }

            if (!free && !Meta.Economy.TryTakeCharge(kind))
            {
                Toast(PlaytestText.T("gameplay.not_enough_petals"));
                return;
            }

            Animator.Flush(Session.View);
            CommandResult result = Session.Apply(command);
            _app.Sound.Play(SoundCue.Booster);
            Animator.Boosted(result, Session.View);
            LastBooster = (kind, Animator.Now);
            EndShownAt = -1f;
            AfterCommand();
        }

        public void PressBooster(BoosterKind kind, Recovery recovery)
        {
            if (Demo != null)
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
                    Toast(PlaytestText.T(recovery == Recovery.Return ? "gameplay.hint_return" : "gameplay.hint_burst"));
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
            Animator.Reset(Session.View);
            Targeting = null;
            JamHidden = false;
            RescueUsed = false;
            EndShownAt = -1f;
            _app.Sound.Play(SoundCue.Click);
        }

        /// <summary>A won level is recorded and paid at once, so closing the app during the animation keeps it (R15).</summary>
        private void AfterCommand()
        {
            if (Won && Payout == null)
            {
                Payout = Meta.CompleteLevel(Level, Session.Definition.Difficulty.Class, Session.BoostersUsed, Session.Definition) ?? new WinPayout(null, null);
            }
        }

        /// <summary>NEXT on the win card: the milestone card when this level had one, else the next level.</summary>
        public void Next()
        {
            if (!ShowingMilestone && Payout?.Milestone != null)
            {
                ShowingMilestone = true;
                EndShownAt = _app.Now;
                _app.Sound.Play(SoundCue.Click);
                return;
            }

            _app.StartLevel();
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
            Animator.Speed = Meta.Save.Settings.Speed2x ? 2f : 1f;
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

        public void Draw(IPainter p)
        {
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

            GameplayRegions r = ScreenLayout.Gameplay(p.Width, p.Height, p.Insets, badge.HasValue, hasBoosters);
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, Level);

            // Top bar: round Pause, the level pill, the 2× pill (frame 7).
            float bar = r.TopBar.Height;
            Kit.RoundButton(p, r.TopBar.Left + (bar / 2f), r.TopBar.CenterY, bar, "ui.pause", () => _app.OpenOverlay(Overlay.Pause));
            p.Mark("ui.pause");
            Box pill = Box.FromCenter(r.TopBar.CenterX, r.TopBar.CenterY, Math.Min(p.U(420f), r.TopBar.Width - (bar * 3.4f)), bar * 0.82f);
            Kit.LevelPill(p, pill, PlaytestText.F("common.level", NumberText.Group(Level)), Session.Definition.Difficulty.Class == DifficultyClass.SuperHard && badge.HasValue);
            Box speed = new Box(r.TopBar.Right - (bar * 1.3f), r.TopBar.CenterY - (bar * 0.36f), r.TopBar.Right, r.TopBar.CenterY + (bar * 0.36f));
            Kit.DarkPill(p, speed, Animator.Speed > 1f ? "2×" : "1×", ToggleSpeed);
            if (badge.HasValue)
            {
                Kit.Badge(p, r.Badge.Inset(0f, p.U(2f)).Offset(0f, -p.U(10f)), badge.Value.Text, badge.Value.Color, badge.Value.Slot);
            }

            BoardPainter.Draw(p, r.Board, this);
            SlotPainter.DrawRow(p, r.Slots, this);
            PodPainter.DrawTray(p, r.Tray, this);
            if (hasBoosters)
            {
                BoosterBarPainter.Draw(p, r.Boosters, this);
            }

            PodPainter.DrawFlights(p, this);

            if (FirstTapHint)
            {
                Kit.Toast(p, new Box(r.Board.Left, r.Board.Top, r.Board.Right, r.Board.Top + p.U(140f)), PlaytestText.T("demo.first_tap"));
            }

            string? toast = ToastText;
            if (toast != null)
            {
                Kit.Toast(p, r.Board, toast);
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
            }

            if (Demo != null)
            {
                EndCards.Demo(p, this, _app.Now - DemoOpenedAt);
            }
        }

        internal DesignApp App => _app;
    }
}
