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

        /// <summary>How long the jam sheet's rise and settle animate (seconds after it shows).</summary>
        public const float EndCardSeconds = 2.3f;

        /// <summary>
        /// How long the win and milestone cards' celebration animates after the card shows (rays turning, petals falling,
        /// Next breathing). Then the screen holds still until the next input, so a card left open does not keep redrawing
        /// the whole level.
        /// </summary>
        public const float CelebrationSeconds = 8f;

        /// <summary>
        /// Whether the screen still moves: animations, a toast, a demo opening, booster targeting, and the end cards (the
        /// jam's rise for <see cref="EndCardSeconds"/>; the win and milestone cards' celebration for
        /// <see cref="CelebrationSeconds"/>).
        /// </summary>
        public bool NeedsFrames => !Animator.Idle || (LastBooster.HasValue && Animator.Now - LastBooster.Value.At < 0.7f) || (_toast != null && _app.Now < _toastUntil) || (EndShownAt >= 0f && _app.Now - EndShownAt < (Won ? CelebrationSeconds : EndCardSeconds)) || Targeting.HasValue || (Demo != null && _app.Now - DemoOpenedAt < 0.3f);

        /// <summary>
        /// Whether only the win's own motion moves the screen (the win or milestone card is open and every other animation
        /// is done): a host may then redraw at a lower frame rate to save battery.
        /// </summary>
        public bool OnlyCelebrating => Won && EndShownAt >= 0f && _app.Now - EndShownAt >= EndCardSeconds && Animator.Idle && Targeting == null && Demo == null && (_toast == null || _app.Now >= _toastUntil);

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

        /// <summary>The level sign's width in top bar heights (the reference's plank is about 2.9 Pause buttons wide).</summary>
        public const float SignWidth = 2.9f;

        /// <summary>The level sign's height in top bar heights.</summary>
        public const float SignHeight = 0.9f;

        /// <summary>The speed pill's width in top bar heights.</summary>
        public const float SpeedWidth = 1.5f;

        /// <summary>
        /// The tray's parchment (spec 005 FR-012, §4.1), as in the reference: a frame from just below the board to past the
        /// bottom of the screen, holding one parchment band for the Waiting Slots, one for the Source Tray and one for the
        /// booster bar, parted by grooves in the gaps between the regions.
        /// </summary>
        public static void TrayBoard(IPainter p, GameplayRegions r, bool hasBoosters)
        {
            float side = p.U(8f);
            float margin = p.U(7f);
            float groove = p.U(8f) / 2f;
            var board = new Box(r.Safe.Left + side, r.Slots.Top - p.U(14f), r.Safe.Right - side, p.Height + p.U(80f));
            TrayFrame(p, board, p.U(40f));
            float left = board.Left + margin;
            float right = board.Right - margin;
            float seam = r.Slots.Bottom + ((r.Tray.Top - r.Slots.Bottom) * 0.3f);
            TrayBand(p, new Box(left, board.Top + margin, right, seam - groove), p.U(34f));
            if (!hasBoosters)
            {
                TrayBand(p, new Box(left, seam + groove, right, board.Bottom), p.U(22f));
                return;
            }

            float seam2 = r.Tray.Bottom + ((r.Boosters.Top - r.Tray.Bottom) * 0.5f);
            TrayBand(p, new Box(left, seam + groove, right, seam2 - groove), p.U(22f));
            TrayBand(p, new Box(left, seam2 + groove, right, board.Bottom), p.U(22f));
        }

        /// <summary>
        /// The tray's frame (<c>mat.parchment</c>): the deep parchment seen in the grooves between its bands, a dark
        /// <c>parchment.line</c> outline and a soft halo on the lawn.
        /// </summary>
        public static void TrayFrame(IPainter p, Box box, float radius)
        {
            p.Mark("mat.parchment");
            float r = Math.Min(radius, box.Height / 2f);
            float line = p.U(4f);
            p.FillRound(box.Inset(-p.U(9f)), r + p.U(9f), C.GardenShadow.WithAlpha(0.06f));
            p.FillRound(box.Inset(-p.U(4f)), r + p.U(4f), C.GardenShadow.WithAlpha(0.12f));
            p.FillRound(box, r, C.ParchmentEdge.Mix(C.ParchmentLine, 0.45f));
            p.StrokeRound(box.Inset(line / 2f), r - (line / 2f), line, C.ParchmentLine.Darken(0.22f));
        }

        /// <summary>
        /// One band of the tray's parchment (<c>mat.parchment</c>): a board a little deeper than a card's parchment, so the
        /// cream plates and tiles stand out on it as in the reference (<c>parchment.well</c> with light wood at the top,
        /// with <c>parchment.edge</c> at the bottom), its edges aged darker in fine steps, a light bevel along its top and
        /// a thin <c>parchment.line</c> outline.
        /// </summary>
        public static void TrayBand(IPainter p, Box box, float radius)
        {
            p.Mark("mat.parchment");
            float r = Math.Min(radius, box.Height / 2f);
            p.FillRoundGradient(box, r, C.ParchmentWell.Mix(C.WoodLight, 0.55f), C.ParchmentEdge.Mix(C.ParchmentWell, 0.4f));
            float step = p.U(2.5f);
            for (int k = 0; k < 10; k++)
            {
                float inset = step * k;
                p.StrokeRound(box.Inset(inset), Math.Max(0f, r - inset), step * 1.6f, C.ParchmentLine.WithAlpha(0.15f * (1f - (k / 10f))));
            }

            // The light bevel along the top edge, fading out round the upper corners (no hook down the sides).
            float bevel = p.U(3f);
            float[] reach = { r * 0.5f, r, r + p.U(6f) };
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
            p.StrokeRound(box.Inset(p.U(1f)), Math.Max(0f, r - p.U(1f)), p.U(2f), C.ParchmentLine.WithAlpha(0.55f));
        }

        public void Draw(IPainter p)
        {
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

            GameplayRegions r = ScreenLayout.Gameplay(p.Width, p.Height, p.Insets, badge.HasValue, hasBoosters);
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, Level);

            // Top bar (frame 7, spec 005 §3.3, §4.1): the cream Pause squircle, the level on a wide wooden sign with ivy,
            // and the cream speed pill, sized like the reference's. It stays while the win and milestone cards show, under
            // their scrim and celebration, and Pause and the speed pill keep their taps (see below; spec 005 FR-002).
            float bar = r.TopBar.Height;
            Action openPause = () => _app.OpenOverlay(Overlay.Pause);
            var pause = Box.FromCenter(r.TopBar.Left + (bar / 2f), r.TopBar.CenterY, bar, bar);
            Kit.RoundButton(p, pause.CenterX, pause.CenterY, bar, "ui.pause", openPause, squircle: true);
            p.Mark("ui.pause");
            Box sign = Box.FromCenter(r.TopBar.CenterX, r.TopBar.CenterY, Math.Min(bar * SignWidth, r.TopBar.Width - (bar * 4f)), bar * SignHeight);
            Kit.LevelPill(p, sign, PlaytestText.F("common.level", NumberText.Group(Level)), Session.Definition.Difficulty.Class == DifficultyClass.SuperHard && badge.HasValue);
            // The speed pill is as tall as Pause and half as wide again.
            var speed = new Box(r.TopBar.Right - (bar * SpeedWidth), r.TopBar.CenterY - (bar / 2f), r.TopBar.Right, r.TopBar.CenterY + (bar / 2f));
            Kit.SpeedPill(p, speed, Animator.Speed > 1f ? "2×" : "1×", ToggleSpeed);
            if (badge.HasValue)
            {
                // HARD or SUPER HARD hangs from the sign's lower edge.
                Kit.Badge(p, r.Badge.Inset(0f, p.U(2f)).Offset(0f, -p.U(10f)), badge.Value.Text, badge.Value.Color, badge.Value.Slot);
            }

            BoardPainter.Draw(p, r.Board, this);
            TrayBoard(p, r, hasBoosters);
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
