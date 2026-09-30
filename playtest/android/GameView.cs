using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using Color = Android.Graphics.Color;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The playtest screens, drawn on a canvas: Home (Level N, Play, Petals, booster charges, the next milestone and
    /// tester controls) and the level (top bar, board, Waiting Slots, boosters, Source Tray, demo, win and jam cards).
    /// The rules resolve every tap at once in the core; <see cref="LevelAnimator"/> then plays the result as walking
    /// Bloomlings, tiles shrinking away and counts dropping. Progress, Petals, booster unlocks and charges and milestones
    /// come from the Unity client's engine-free services (<see cref="PlaytestMeta"/>). The very first launch goes
    /// straight into Level 1; later launches open Home.
    /// <para>
    /// The level tester build (<see cref="PlaytestFlavor.Tester"/>) keeps the first playtest's quick loop: levels open
    /// straight away, ◀ ▶ move between them, every booster is free, and a tap shows its result at once.
    /// </para>
    /// </summary>
    public sealed class GameView : View
    {
        private const string PrefsName = "bloomlings-playtest";

        private static readonly Color BackgroundColor = Color.ParseColor("#F5F2E6");
        private static readonly Color Ground = Color.ParseColor("#DCD4BD");
        private static readonly Color StoneColor = Color.ParseColor("#8C8C94");
        private static readonly Color TextColor = Color.ParseColor("#33383F");
        private static readonly Color Accent = Color.ParseColor("#4D9E5C");
        private static readonly Color Warning = Color.ParseColor("#ED7340");
        private static readonly Color Panel = Color.ParseColor("#FFFFFF");
        private static readonly Color SlotEmpty = Color.ParseColor("#E0DED1");
        private static readonly Color Locked = Color.ParseColor("#9E998F");
        private static readonly Color EntryColor = Color.ParseColor("#FACC40");
        private static readonly Color Band = Color.ParseColor("#DDEBCF");

        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>
        {
            ["leaf"] = "Lf", ["moss"] = "Ms", ["flower"] = "Fl", ["violet_bud"] = "Vb", ["water"] = "Wa", ["dew"] = "Dw",
            ["wood"] = "Wd", ["acorn"] = "Ac", ["vine"] = "Vn", ["berry"] = "Br", ["mist"] = "Mi", ["bark"] = "Bk",
        };

        private static readonly (BoosterKind Kind, Recovery Recovery, string Key)[] Boosters =
        {
            (BoosterKind.ExtraSlot, Recovery.ExtraSlot, "booster.extra_slot"),
            (BoosterKind.Shuffle, Recovery.Shuffle, "booster.shuffle"),
            (BoosterKind.Return, Recovery.Return, "booster.return"),
            (BoosterKind.BloomBurst, Recovery.BloomBurst, "booster.bloom_burst"),
        };

        private readonly Context _context;
        private readonly ContentSet _content;
        private readonly ISharedPreferences _prefs;
        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint _text = new Paint(PaintFlags.AntiAlias) { TextAlign = Paint.Align.Center };
        private readonly List<(RectF Rect, Action Action)> _hits = new List<(RectF, Action)>();
        private readonly Dictionary<string, RectF> _podRects = new Dictionary<string, RectF>(StringComparer.Ordinal);
        private readonly RectF?[] _slotRects = new RectF?[WaitingSlots.Capacity];
        private readonly LevelAnimator _animator = new LevelAnimator();
        private readonly PlaytestSound _sound;
        private PlaytestMeta _meta;
        private bool _home;
        private LevelSession _session = null!;
        private int _level;
        private string? _toast;
        private long _toastUntil;
        private Recovery? _targeting;
        private bool _jamHidden;
        private WinPayout? _payout;
        private Demo? _demo;
        private bool _firstTapHint;
        private long _lastFrame;
        private long _lastClearSound;
        private (float Ox, float Oy, float Cell, int Height) _board;
        private int _insetTop;
        private int _insetBottom;

        public GameView(Context context)
            : base(context)
        {
            _context = context;
            _content = PlaytestContent.Load();
            _prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            _text.SetTypeface(Typeface.DefaultBold);
            _sound = new PlaytestSound(context) { Enabled = _prefs.GetBoolean("sound", true) };
            _meta = new PlaytestMeta(context.FilesDir!.AbsolutePath);
            _animator.Speed = _meta.Save.Settings.Speed2x ? 2f : 1f;
            _animator.Arrived += OnArrived;
            _animator.Shown += OnShown;
            if (PlaytestFlavor.Tester)
            {
                _animator.Speed = 1f;
                LoadLevel(_prefs.GetInt("level", 1));
            }
            else if (_meta.FirstLaunch)
            {
                StartLevel();
            }
            else
            {
                _home = true;
            }
        }

        /// <summary>A short modal message: a demo or an unlock (FR-031, FR-071), with optional variant tiles.</summary>
        private sealed record Demo(string Id, IReadOnlyList<string> Lines, IReadOnlyList<VariantId> Variants, bool ShowIgnore);

        public override WindowInsets OnApplyWindowInsets(WindowInsets? insets)
        {
            if (insets != null)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    Insets bars = insets.GetInsets(WindowInsets.Type.SystemBars());
                    _insetTop = bars.Top;
                    _insetBottom = bars.Bottom;
                }
                else
                {
#pragma warning disable CA1422, CS0618 // The pre-Android 11 inset API.
                    _insetTop = insets.SystemWindowInsetTop;
                    _insetBottom = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422, CS0618
                }

                Invalidate();
            }

            return base.OnApplyWindowInsets(insets)!;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            _hits.Clear();
            canvas.DrawColor(BackgroundColor);
            long now = SystemClock.UptimeMillis();
            float dt = _lastFrame == 0 ? 0f : Math.Min(0.1f, (now - _lastFrame) / 1000f);
            _lastFrame = now;
            if (_home)
            {
                DrawHome(canvas);
                _lastFrame = 0;
                return;
            }

            _animator.Advance(dt, _session.View);
            float width = Width;
            float top = _insetTop + (Width * 0.02f);
            float bottom = Height - _insetBottom - (Width * 0.02f);
            float usable = bottom - top;
            float pad = width * 0.03f;

            Fill(canvas, new RectF(0, top + (usable * 0.59f), width, Height), Band, 0f);
            DrawTopBar(canvas, new RectF(pad, top, width - pad, top + (usable * 0.07f)));
            var board = new RectF(pad, top + (usable * 0.08f), width - pad, top + (usable * 0.58f));
            DrawBoard(canvas, board);
            DrawSlots(canvas, new RectF(pad, top + (usable * 0.60f), width - pad, top + (usable * 0.68f)));
            DrawBoosters(canvas, new RectF(pad, top + (usable * 0.695f), width - pad, top + (usable * 0.745f)));
            DrawTray(canvas, new RectF(pad, top + (usable * 0.76f), width - pad, bottom));
            DrawFlights(canvas);
            DrawToast(canvas, board);
            DrawOverlay(canvas);
            DrawDemo(canvas);
            if (!_animator.Idle)
            {
                PostInvalidateOnAnimation();
            }
            else
            {
                _lastFrame = 0;
            }
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null)
            {
                return false;
            }

            if (e.Action == MotionEventActions.Up)
            {
                for (int i = _hits.Count - 1; i >= 0; i--)
                {
                    if (_hits[i].Rect.Contains(e.GetX(), e.GetY()))
                    {
                        _hits[i].Action();
                        break;
                    }
                }
            }

            return true;
        }

        // ---- Screens and levels ----

        /// <summary>Plays the current level (the progression's Level N; the playtest levels repeat past the last one).</summary>
        private void StartLevel() => LoadLevel(_meta.CurrentLevel);

        /// <summary>Plays a level; the tester remembers it, the full playtest follows the progression.</summary>
        private void LoadLevel(int levelNumber)
        {
            _home = false;
            _level = Math.Max(1, levelNumber);
            if (PlaytestFlavor.Tester)
            {
                _prefs.Edit()!.PutInt("level", _level)!.Apply();
            }

            LevelDefinition definition = _content.GetLevel(Resolve(_level));
            _session = LevelSession.Load(definition, _content.GetPicture(definition.Picture), new SessionOptions(_content.ContentVersion, _content.ShuffleNodeBudget));
            _animator.Reset(_session.View);
            _targeting = null;
            _jamHidden = false;
            _payout = null;
            _demo = null;
            ShowLevelIntro();
            Invalidate();
        }

        private void GoHome()
        {
            _home = true;
            _demo = null;
            Invalidate();
        }

        private int Resolve(int levelNumber) =>
            _content.TryGetLevel(levelNumber, out _) ? levelNumber : _content.LevelNumbers[(levelNumber - 1) % _content.LevelCount];

        /// <summary>Before play: the difficulty label, then at most one demo (FR-031, FR-042, FR-059, FR-071).</summary>
        private void ShowLevelIntro()
        {
            DifficultyClass difficulty = _session.Definition.Difficulty.Class;
            if (difficulty != DifficultyClass.Normal && (PlaytestFlavor.Tester || _meta.Progression.IsUnlocked(difficulty == DifficultyClass.Hard ? "profile.hard" : "profile.super_hard")))
            {
                Toast(PlaytestText.T(difficulty == DifficultyClass.Hard ? "difficulty.hard" : "difficulty.super_hard"));
            }

            if (PlaytestFlavor.Tester)
            {
                return; // The tester shows no demos.
            }

            _firstTapHint = _level == 1 && !_meta.HasSeenDemo("system.core");
            if (_firstTapHint)
            {
                return;
            }

            foreach ((BoosterKind kind, Recovery _, string key) in Boosters)
            {
                string unlockId = "booster." + key.Substring("booster.".Length);
                if (_meta.Economy.IsUnlocked(kind) && !_meta.HasSeenDemo(unlockId))
                {
                    _demo = new Demo(unlockId, Lines("demo." + unlockId.Substring("booster.".Length)), Array.Empty<VariantId>(), false);
                    return;
                }
            }

            foreach (string unlockId in LevelMechanics.UnlocksUsed(_session.Definition, _session.Picture))
            {
                if (!unlockId.StartsWith("mechanic.", StringComparison.Ordinal) || _meta.HasSeenDemo(unlockId) || !_meta.Progression.IsUnlocked(unlockId))
                {
                    continue;
                }

                IReadOnlyList<string> lines = Lines("demo." + unlockId.Substring("mechanic.".Length));
                if (lines.Count > 0)
                {
                    _demo = new Demo(unlockId, lines, Array.Empty<VariantId>(), false);
                    return;
                }
            }

            var variants = new List<VariantId>();
            foreach (PodDef pod in _session.Definition.Pods)
            {
                if (!variants.Contains(pod.Variant))
                {
                    variants.Add(pod.Variant);
                }
            }

            foreach (VariantId variant in variants)
            {
                VariantInfo info = VariantCatalog.Default.Get(variant);
                if (info.Status == VariantStatus.Expansion && !_meta.HasSeenDemo("demo.variant." + variant.Key))
                {
                    _demo = new Demo("demo.variant." + variant.Key, new[] { PlaytestText.T("demo.new_variant") }, new[] { variant }, false);
                    return;
                }
            }

            if (!_meta.HasSeenDemo("demo.siblings"))
            {
                var byFamily = new Dictionary<Family, VariantId>();
                foreach (VariantId variant in variants)
                {
                    Family family = VariantCatalog.Default.Get(variant).Family;
                    if (byFamily.TryGetValue(family, out VariantId other))
                    {
                        _demo = new Demo("demo.siblings", new[] { PlaytestText.T("demo.exact_symbol") }, new[] { other, variant }, true);
                        return;
                    }

                    byFamily[family] = variant;
                }
            }
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

        private void CloseDemo()
        {
            if (_demo != null)
            {
                _meta.MarkDemoSeen(_demo.Id);
                _demo = null;
                _sound.Play(SoundCue.Click);
                Invalidate();
            }
        }

        // ---- Commands ----

        private void Tap(string podId)
        {
            if (_targeting != null || _demo != null)
            {
                return;
            }

            var tap = new TapPod(podId);
            CommandCheck check = _session.Check(tap);
            if (!check.IsAllowed)
            {
                _sound.Play(SoundCue.Refused);
                Toast(RefusalText(check.Reason));
                Invalidate();
                return;
            }

            var before = new Dictionary<string, (int Count, VariantId? Variant, float X, float Y)>(StringComparer.Ordinal);
            var members = new List<string>(_session.View.ConnectedGroup(podId)) { podId };
            foreach (string member in members)
            {
                PodInfo info = _session.View.Pod(member);
                RectF? rect = _podRects.TryGetValue(member, out RectF? r) ? r : null;
                before[member] = (info.Remaining, info.Variant, rect?.CenterX() ?? float.NaN, rect?.CenterY() ?? float.NaN);
            }

            CommandResult result = _session.Apply(tap);
            _sound.Play(SoundCue.Tap);
            if (_firstTapHint)
            {
                _firstTapHint = false;
                _meta.MarkDemoSeen("system.core");
            }

            if (PlaytestFlavor.Tester)
            {
                ShowAtOnce(result);
            }
            else
            {
                _animator.Tapped(result, _session.View, before);
            }

            AfterCommand();
        }

        private void UseBooster(BoosterKind kind, Command command)
        {
            _targeting = null;
            CommandCheck check = _session.Check(command);
            if (!check.IsAllowed)
            {
                Toast(PlaytestText.T("gameplay.booster_useless"));
                Invalidate();
                return;
            }

            if (!PlaytestFlavor.Tester && !_meta.Economy.TryTakeCharge(kind))
            {
                Toast(PlaytestText.T("gameplay.not_enough_petals"));
                Invalidate();
                return;
            }

            _animator.Flush(_session.View);
            CommandResult result = _session.Apply(command);
            _sound.Play(SoundCue.Booster);
            if (PlaytestFlavor.Tester)
            {
                ShowAtOnce(result);
            }
            else
            {
                _animator.Boosted(result, _session.View);
            }

            AfterCommand();
        }

        private void PressBooster(BoosterKind kind, Recovery recovery)
        {
            if (_demo != null)
            {
                return;
            }

            if (!PlaytestFlavor.Tester && !_meta.Economy.IsUnlocked(kind))
            {
                Toast("Opens at Level " + UnlockLevel(kind));
                Invalidate();
                return;
            }

            if (_targeting == recovery)
            {
                _targeting = null;
                Invalidate();
                return;
            }

            _jamHidden = false;
            switch (recovery)
            {
                case Recovery.ExtraSlot:
                    UseBooster(kind, new UseExtraSlot());
                    break;
                case Recovery.Shuffle:
                    UseBooster(kind, new UseShuffle());
                    break;
                default:
                    if (!PlaytestFlavor.Tester && !_meta.Economy.CanAfford(kind))
                    {
                        Toast(PlaytestText.T("gameplay.not_enough_petals"));
                        Invalidate();
                        return;
                    }

                    _targeting = recovery;
                    _jamHidden = true;
                    Toast(PlaytestText.T(recovery == Recovery.Return ? "gameplay.hint_return" : "gameplay.hint_burst"));
                    Invalidate();
                    break;
            }
        }

        private void Restart()
        {
            _session.Apply(new Restart());
            _animator.Reset(_session.View);
            _targeting = null;
            _jamHidden = false;
            _sound.Play(SoundCue.Click);
            Invalidate();
        }

        /// <summary>A won level is recorded and paid at once, so closing the app during the animation keeps it (R15).</summary>
        private void AfterCommand()
        {
            if (PlaytestFlavor.Tester)
            {
                if (_session.Status == LevelStatus.Won)
                {
                    _prefs.Edit()!.PutInt("level", _level + 1)!.Apply();
                }
            }
            else if (_session.Status == LevelStatus.Won && _payout == null)
            {
                _payout = _meta.CompleteLevel(_level, _session.Definition.Difficulty.Class, _session.BoostersUsed) ?? new WinPayout(null, null);
            }

            Invalidate();
        }

        /// <summary>The tester: the result shows at once, with the command's cue and then its most notable outcome.</summary>
        private void ShowAtOnce(CommandResult result)
        {
            _animator.Reset(_session.View);
            SoundCue? outcome = _session.Status switch
            {
                LevelStatus.Won => SoundCue.Win,
                LevelStatus.Jammed or LevelStatus.Stuck => SoundCue.Jam,
                _ => null,
            };
            foreach (GameEvent e in result.Events)
            {
                SoundCue? cue = e switch
                {
                    SpecialTriggered => SoundCue.Special,
                    KeyCollected => SoundCue.Key,
                    PodCompleted => SoundCue.PodDone,
                    TileCleared => SoundCue.Clear,
                    _ => null,
                };
                if (cue.HasValue && (!outcome.HasValue || Rank(cue.Value) > Rank(outcome.Value)))
                {
                    outcome = cue;
                }
            }

            if (outcome.HasValue)
            {
                SoundCue later = outcome.Value;
                PostDelayed(() => _sound.Play(later), 160);
            }
        }

        private static int Rank(SoundCue cue) => cue switch
        {
            SoundCue.Win => 6,
            SoundCue.Jam => 5,
            SoundCue.Special => 4,
            SoundCue.Key => 3,
            SoundCue.PodDone => 2,
            SoundCue.Clear => 1,
            _ => 0,
        };

        private void OnArrived(TileCleared clear)
        {
            long now = SystemClock.UptimeMillis();
            if (now - _lastClearSound > 45)
            {
                _lastClearSound = now;
                _sound.Play(SoundCue.Clear);
            }
        }

        private void OnShown(GameEvent e)
        {
            switch (e)
            {
                case PodCompleted:
                    _sound.Play(SoundCue.PodDone);
                    break;
                case KeyCollected:
                    _sound.Play(SoundCue.Key);
                    break;
                case SpecialTriggered:
                    _sound.Play(SoundCue.Special);
                    break;
                case LevelWon:
                    _sound.Play(SoundCue.Win);
                    break;
                case LevelJammed:
                case LevelStuck:
                    _sound.Play(SoundCue.Jam);
                    break;
            }
        }

        private static int UnlockLevel(BoosterKind kind)
        {
            foreach ((string id, BoosterKind k) in EconomyService.BoosterUnlocks)
            {
                if (k == kind)
                {
                    return UnlockRoadmap.Default.LevelOf(id) ?? 0;
                }
            }

            return 0;
        }

        private void Toast(string message)
        {
            _toast = message;
            _toastUntil = SystemClock.UptimeMillis() + 1600;
            PostDelayed(Invalidate, 1700);
        }

        private void ToggleSound()
        {
            _sound.Enabled = !_sound.Enabled;
            _prefs.Edit()!.PutBoolean("sound", _sound.Enabled)!.Apply();
            Invalidate();
        }

        private void ToggleSpeed()
        {
            _meta.Save.Settings.Speed2x = !_meta.Save.Settings.Speed2x;
            _meta.Persist();
            _animator.Speed = _meta.Save.Settings.Speed2x ? 2f : 1f;
            Invalidate();
        }

        private static string RefusalText(RejectReason? reason) => reason switch
        {
            RejectReason.NoFreeSlot => PlaytestText.T("refusal.no_free_slot"),
            RejectReason.NotExposed => PlaytestText.T("refusal.not_exposed"),
            RejectReason.Locked => PlaytestText.T("refusal.locked"),
            RejectReason.NotEnoughSlotsForGroup => PlaytestText.T("refusal.group"),
            RejectReason.BoosterNotApplicable => PlaytestText.T("gameplay.booster_useless"),
            _ => "Not now",
        };

        // ---- Home ----

        private void DrawHome(Canvas canvas)
        {
            float w = Width;
            float top = _insetTop + (w * 0.04f);
            float bottom = Height - _insetBottom - (w * 0.04f);
            float h = bottom - top;
            float pad = w * 0.06f;

            Button(canvas, new RectF(pad, top, pad + (w * 0.12f), top + (w * 0.12f)), _sound.Enabled ? "♪" : "×", _sound.Enabled ? Accent : Locked, ToggleSound);
            Text(canvas, "✿ " + _meta.Economy.Petals, w - pad - (w * 0.15f), top + (w * 0.06f), w * 0.055f, TextColor);
            Text(canvas, PlaytestText.T("home.logo"), w / 2f, top + (h * 0.17f), w * 0.13f, Accent);
            Text(canvas, "playtest build", w / 2f, top + (h * 0.23f), w * 0.04f, Locked);

            Fill(canvas, new RectF(pad, top + (h * 0.3f), w - pad, top + (h * 0.56f)), Band, w * 0.05f);
            int level = _meta.CurrentLevel;
            Text(canvas, PlaytestText.F("common.level", level), w / 2f, top + (h * 0.36f), w * 0.1f, TextColor);
            Button(canvas, new RectF(w * 0.22f, top + (h * 0.42f), w * 0.78f, top + (h * 0.52f)), PlaytestText.T(level == 1 ? "common.play" : "home.continue"), Accent, StartLevel);

            (int Level, MilestoneCadence Cadence, int WinsToGo)? next = _meta.Milestones.Next(_meta.Progression.HighestCompletedLevel);
            if (next.HasValue)
            {
                Text(canvas, PlaytestText.F("home.milestone_teaser", next.Value.Level, next.Value.WinsToGo), w / 2f, top + (h * 0.6f), w * 0.045f, TextColor);
            }

            // Booster charges, or the level each one opens at.
            float chipTop = top + (h * 0.65f);
            float chipW = (w - (pad * 2f) - (w * 0.06f)) / 4f;
            for (int i = 0; i < Boosters.Length; i++)
            {
                (BoosterKind kind, Recovery _, string key) = Boosters[i];
                var rect = new RectF(pad + (i * (chipW + (w * 0.02f))), chipTop, pad + (i * (chipW + (w * 0.02f))) + chipW, chipTop + (h * 0.09f));
                bool unlocked = _meta.Economy.IsUnlocked(kind);
                Fill(canvas, rect, unlocked ? Panel : SlotEmpty, w * 0.03f);
                Text(canvas, ShortName(kind), rect.CenterX(), rect.Top + (rect.Height() * 0.32f), w * 0.034f, unlocked ? TextColor : Locked);
                Text(canvas, unlocked ? "×" + _meta.Economy.Charges(kind) : "L" + UnlockLevel(kind), rect.CenterX(), rect.Top + (rect.Height() * 0.7f), w * 0.045f, unlocked ? Accent : Locked);
            }

            if (_meta.NewUnlocks.Count > 0)
            {
                var names = new List<string>();
                foreach (UnlockEntry entry in _meta.NewUnlocks)
                {
                    names.Add(entry.UnlockId);
                }

                Text(canvas, "New: " + string.Join(", ", names), w / 2f, top + (h * 0.78f), w * 0.034f, Warning);
            }

            // Tester controls (not in the product): skip levels, or start a new profile.
            float testTop = bottom - (h * 0.08f);
            Text(canvas, "tester", w / 2f, testTop - (h * 0.025f), w * 0.03f, Locked);
            float bw = (w - (pad * 2f) - (w * 0.04f)) / 3f;
            Button(canvas, new RectF(pad, testTop, pad + bw, bottom), "◀ −1", Locked, () =>
            {
                _meta.StepBack();
                Invalidate();
            });
            Button(canvas, new RectF(pad + bw + (w * 0.02f), testTop, pad + (bw * 2f) + (w * 0.02f), bottom), "+1 ▶", Locked, () =>
            {
                _meta.SkipTo(_meta.Progression.HighestCompletedLevel + 1);
                Invalidate();
            });
            Button(canvas, new RectF(pad + (bw * 2f) + (w * 0.04f), testTop, w - pad, bottom), "Reset", Warning, () =>
            {
                _meta = PlaytestMeta.ResetProfile(_context.FilesDir!.AbsolutePath);
                _animator.Speed = 1f;
                Toast("New profile");
                Invalidate();
            });

            DrawToast(canvas, new RectF(pad, top, w - pad, top + (h * 0.3f)));
        }

        private static string ShortName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => "+Slot",
            BoosterKind.Shuffle => "Shuffle",
            BoosterKind.Return => "Return",
            _ => "Burst",
        };

        // ---- Level drawing ----

        private void DrawTopBar(Canvas canvas, RectF bar)
        {
            float h = bar.Height();
            if (PlaytestFlavor.Tester)
            {
                // The tester's quick loop: ◀ ▶ move between levels.
                Button(canvas, new RectF(bar.Left, bar.Top, bar.Left + (h * 1.2f), bar.Bottom), "◀", Locked, () => LoadLevel(_level - 1));
                Button(canvas, new RectF(bar.Right - (h * 2.5f), bar.Top, bar.Right - (h * 1.3f), bar.Bottom), "▶", Locked, () => LoadLevel(_level + 1));
            }
            else
            {
                Button(canvas, new RectF(bar.Left, bar.Top, bar.Left + (h * 1.2f), bar.Bottom), "⌂", TextColor, GoHome);
                Button(canvas, new RectF(bar.Right - (h * 2.5f), bar.Top, bar.Right - (h * 1.3f), bar.Bottom), _animator.Speed > 1f ? "2×" : "1×", Accent, ToggleSpeed);
            }

            Button(canvas, new RectF(bar.Left + (h * 1.3f), bar.Top, bar.Left + (h * 2.5f), bar.Bottom), _sound.Enabled ? "♪" : "×", _sound.Enabled ? Accent : Locked, ToggleSound);
            Button(canvas, new RectF(bar.Right - (h * 1.2f), bar.Top, bar.Right, bar.Bottom), "↻", TextColor, Restart);

            DifficultyClass difficulty = _session.Definition.Difficulty.Class;
            int resolved = Resolve(_level);
            string title = PlaytestText.F("common.level", _level) + (resolved != _level ? " (= L" + resolved + ")" : string.Empty);
            Text(canvas, title, bar.CenterX(), bar.Top + (h * 0.42f), h * 0.42f, TextColor);
            string tail = PlaytestFlavor.Tester ? "tester" : "✿ " + _meta.Economy.Petals;
            string subtitle = difficulty == DifficultyClass.Normal ? tail
                : PlaytestText.T(difficulty == DifficultyClass.Hard ? "difficulty.hard" : "difficulty.super_hard") + " · " + tail;
            Text(canvas, subtitle, bar.CenterX(), bar.Top + (h * 0.85f), h * 0.26f, difficulty == DifficultyClass.Normal ? Locked : Warning);
        }

        private void DrawBoard(Canvas canvas, RectF area)
        {
            LevelView view = _session.View;
            int w = view.Width;
            int h = view.Height;
            float cell = Math.Min(area.Width() / w, area.Height() / (h + 0.6f));
            float ox = area.CenterX() - (cell * w / 2f);
            float oy = area.Top + ((area.Height() - (cell * (h + 0.6f))) / 2f);
            _board = (ox, oy, cell, h);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pos = new CellPos(x, y);
                    CellInfo info = _animator.Cell(pos);
                    RectF full = CellRect(pos);
                    RectF tile = Inset(full, cell * 0.06f);
                    switch (info.Kind)
                    {
                        case CellKind.Open:
                            Fill(canvas, full, PictureColor(x, y), 0f);
                            break;
                        case CellKind.Stone:
                            Fill(canvas, tile, StoneColor, cell * 0.18f);
                            break;
                        case CellKind.Special:
                            DrawSpecialCell(canvas, info, full, tile, cell);
                            break;
                        default:
                            DrawTile(canvas, info, full, tile, cell, 255);
                            if (_targeting == Recovery.BloomBurst && info.Visible.HasValue && !info.MysteryHidden)
                            {
                                VariantId variant = info.Visible.Value;
                                Hit(full, () => UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant)));
                            }

                            break;
                    }
                }
            }

            // Restored tiles shrink away; the picture shows beneath.
            foreach (Fade fade in _animator.Fades)
            {
                float k = Math.Clamp((_animator.Now - fade.Start) / LevelAnimator.FadeSeconds, 0f, 1f);
                RectF full = CellRect(fade.Cell);
                float inset = cell * (0.06f + (0.44f * k));
                if (fade.Look.Kind == CellKind.Target)
                {
                    DrawTile(canvas, fade.Look, full, Inset(full, inset), cell * (1f - k), (int)(255 * (1f - k)));
                }
            }

            // The Garden Entry markers below the board.
            foreach (EntryDef entry in view.Entries)
            {
                (float ex, float ey) = EntryPoint(entry);
                Fill(canvas, new RectF(ex - (cell * 0.35f), ey - (cell * 0.18f), ex + (cell * 0.35f), ey + (cell * 0.18f)), EntryColor, cell * 0.12f);
            }

            DrawWalkers(canvas, view, cell);
            if (_firstTapHint)
            {
                var hint = new RectF(area.Left, area.Top, area.Right, area.Top + (cell * 1.2f));
                Fill(canvas, hint, Color.Argb(230, 255, 255, 255), cell * 0.4f);
                Text(canvas, PlaytestText.T("demo.first_tap"), hint.CenterX(), hint.CenterY(), Math.Min(cell * 0.45f, Width * 0.045f), Accent);
            }
        }

        private RectF CellRect(CellPos pos)
        {
            float left = _board.Ox + (pos.X * _board.Cell);
            float top = _board.Oy + ((_board.Height - 1 - pos.Y) * _board.Cell);
            return new RectF(left, top, left + _board.Cell, top + _board.Cell);
        }

        private (float X, float Y) EntryPoint(EntryDef entry)
        {
            RectF c = CellRect(entry.Cell);
            float d = _board.Cell * 0.8f;
            return entry.Side switch
            {
                EntrySide.Top => (c.CenterX(), c.CenterY() - d),
                EntrySide.Left => (c.CenterX() - d, c.CenterY()),
                EntrySide.Right => (c.CenterX() + d, c.CenterY()),
                _ => (c.CenterX(), c.CenterY() + d),
            };
        }

        private void DrawSpecialCell(Canvas canvas, CellInfo info, RectF full, RectF tile, float cell)
        {
            (int progress, int total, bool triggered) = info.SpecialId != null ? _animator.Special(info.SpecialId) : (0, 0, true);
            SpecialType type = SpecialType.Gate;
            foreach (SpecialInfo special in _session.View.Specials)
            {
                if (special.Id == info.SpecialId)
                {
                    type = special.Type;
                }
            }

            Color color = type switch
            {
                SpecialType.Fountain => Color.ParseColor("#94A8BD"),
                SpecialType.Chest => Color.ParseColor("#A8784D"),
                SpecialType.Statue => Color.ParseColor("#9E9EAD"),
                SpecialType.Bridge => Color.ParseColor("#8C6647"),
                _ => Color.ParseColor("#5C804D"),
            };
            Fill(canvas, tile, color, cell * 0.12f);
            if (!triggered)
            {
                Text(canvas, total > 1 ? progress + "/" + total : "🔑", full.CenterX(), full.CenterY(), cell * 0.3f, Color.White);
            }
        }

        private void DrawTile(Canvas canvas, CellInfo info, RectF full, RectF tile, float cell, int alpha)
        {
            if (info.MysteryHidden || !info.Visible.HasValue)
            {
                Fill(canvas, tile, WithAlpha(Locked, alpha), cell * 0.18f);
                Text(canvas, "?", tile.CenterX(), tile.CenterY(), cell * 0.5f, WithAlpha(Color.White, alpha));
                return;
            }

            VariantId variant = info.Visible.Value;
            Color color = VariantColor(variant);
            Fill(canvas, tile, WithAlpha(color, alpha), cell * 0.18f);
            Text(canvas, Code(variant), tile.CenterX(), tile.CenterY(), cell * 0.36f, WithAlpha(Ink(color), alpha));
            if (info.RemainingLayers > 1 && info.Next.HasValue)
            {
                Fill(canvas, new RectF(tile.Right - (cell * 0.32f), tile.Top, tile.Right, tile.Top + (cell * 0.32f)), WithAlpha(VariantColor(info.Next.Value), alpha), cell * 0.1f);
            }

            if (info.KeyId != null)
            {
                Text(canvas, "🔑", tile.Left + (cell * 0.2f), tile.Top + (cell * 0.2f), cell * 0.3f, Color.White);
            }
        }

        /// <summary>Bloomlings on their way: a small figure in the variant color with its code, hopping along the route.</summary>
        private void DrawWalkers(Canvas canvas, LevelView view, float cell)
        {
            foreach (Walker walker in _animator.Walkers)
            {
                if (walker.Route.Count == 0)
                {
                    continue;
                }

                var points = new List<(float X, float Y)>();
                EntryDef? entry = null;
                foreach (EntryDef candidate in view.Entries)
                {
                    if (candidate.Cell == walker.Route[0])
                    {
                        entry = candidate;
                    }
                }

                if (entry != null)
                {
                    points.Add(EntryPoint(entry));
                }

                foreach (CellPos pos in walker.Route)
                {
                    RectF r = CellRect(pos);
                    points.Add((r.CenterX(), r.CenterY()));
                }

                float progress = Math.Clamp(_animator.WaveTime / Math.Max(0.05f, walker.Arrival), 0f, 1f);
                if (progress >= 1f && _animator.WaveTime > walker.Arrival + 0.12f)
                {
                    continue;
                }

                float t = progress * (points.Count - 1);
                int i = Math.Min((int)t, points.Count - 2);
                float f = points.Count == 1 ? 0f : t - i;
                float x = points.Count == 1 ? points[0].X : points[i].X + ((points[i + 1].X - points[i].X) * f);
                float y = points.Count == 1 ? points[0].Y : points[i].Y + ((points[i + 1].Y - points[i].Y) * f);
                y -= Math.Abs((float)Math.Sin(f * Math.PI)) * cell * 0.18f;
                Color color = VariantColor(walker.Variant);
                _paint.SetStyle(Paint.Style.Fill);
                _paint.Color = Color.White;
                canvas.DrawCircle(x, y, cell * 0.3f, _paint);
                _paint.Color = color;
                canvas.DrawCircle(x, y, cell * 0.25f, _paint);
                Text(canvas, Code(walker.Variant), x, y, cell * 0.22f, Ink(color));
            }
        }

        private void DrawSlots(Canvas canvas, RectF area)
        {
            LevelView view = _session.View;
            var slots = new List<int>();
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                _slotRects[i] = null;
                if (view.SlotStateOf(i) != SlotState.Absent)
                {
                    slots.Add(i);
                }
            }

            int free = 0;
            foreach (int slot in slots)
            {
                if (view.SlotStateOf(slot) == SlotState.Free && !_animator.HeldSlotLocks.Contains(slot) && _animator.Slots[slot].PodId == null)
                {
                    free++;
                }
            }

            float gap = area.Width() * 0.02f;
            float size = Math.Min(area.Height(), (area.Width() - (gap * (slots.Count - 1))) / slots.Count);
            float x = area.CenterX() - (((size * slots.Count) + (gap * (slots.Count - 1))) / 2f);
            foreach (int slot in slots)
            {
                var rect = new RectF(x, area.CenterY() - (size / 2f), x + size, area.CenterY() + (size / 2f));
                _slotRects[slot] = rect;
                x += size + gap;
                if (view.SlotStateOf(slot) == SlotState.Locked || _animator.HeldSlotLocks.Contains(slot))
                {
                    Fill(canvas, rect, Locked, size * 0.16f);
                    Text(canvas, "🔒", rect.CenterX(), rect.CenterY(), size * 0.4f, Color.White);
                    continue;
                }

                SlotLook look = _animator.Slots[slot];
                if (slot >= WaitingSlots.DefaultCount)
                {
                    Text(canvas, "+", rect.Left + (size * 0.12f), rect.Top - (size * 0.08f), size * 0.3f, Accent);
                }

                if (look.PodId == null)
                {
                    bool risk = free == 1;
                    Fill(canvas, rect, risk ? Color.Argb(255, 250, 214, 196) : SlotEmpty, size * 0.16f);
                    if (risk)
                    {
                        Text(canvas, "!", rect.CenterX(), rect.CenterY(), size * 0.45f, Warning);
                    }

                    continue;
                }

                DrawSlotPod(canvas, look, rect, size);
                if (_targeting == Recovery.Return)
                {
                    int index = slot;
                    Hit(rect, () => UseBooster(BoosterKind.Return, new UseReturn(index)));
                }
            }
        }

        private void DrawSlotPod(Canvas canvas, SlotLook look, RectF rect, float size)
        {
            float now = _animator.Now;
            float scale = 1f;
            if (now - look.PoppedAt < 0.16f)
            {
                scale += 0.12f * (float)Math.Sin((now - look.PoppedAt) / 0.16f * Math.PI);
            }

            int alpha = 255;
            if (look.IsLeaving)
            {
                float k = Math.Clamp((now - look.LeavingAt) / LevelAnimator.ExitSeconds, 0f, 1f);
                scale += 0.3f * k;
                alpha = (int)(255 * (1f - k));
            }

            bool working = look.InFlight > 0;
            if (!working && !look.IsLeaving)
            {
                scale *= 0.9f;
            }

            float scaleX = scale;
            bool hidden = !look.Variant.HasValue;
            float flip = (now - look.RevealAt) / 0.3f;
            if (flip >= 0f && flip < 1f)
            {
                scaleX *= Math.Abs((float)Math.Cos(flip * Math.PI));
                hidden = flip < 0.5f;
            }

            RectF r = Scale(rect, scaleX, scale);
            Color color = hidden ? Locked : VariantColor(look.Variant!.Value);
            Fill(canvas, r, WithAlpha(color, alpha), size * 0.16f);
            Color ink = hidden ? Color.White : Ink(color);
            Text(canvas, hidden ? "?" : Code(look.Variant!.Value), r.CenterX(), r.Top + (r.Height() * 0.36f), size * 0.3f, WithAlpha(ink, alpha));
            float bump = now - look.BumpedAt < 0.15f ? 1f + (0.35f * (float)Math.Sin((now - look.BumpedAt) / 0.15f * Math.PI)) : 1f;
            Text(canvas, look.Count.ToString(), r.CenterX(), r.Top + (r.Height() * 0.72f), size * 0.34f * bump, WithAlpha(ink, alpha));
            if (!working && !look.IsLeaving && look.Count > 0)
            {
                Text(canvas, "⌛", rect.Right - (size * 0.1f), rect.Top + (size * 0.1f), size * 0.24f, TextColor);
            }
        }

        private void DrawBoosters(Canvas canvas, RectF area)
        {
            float gap = area.Width() * 0.02f;
            float w = (area.Width() - (gap * (Boosters.Length - 1))) / Boosters.Length;
            for (int i = 0; i < Boosters.Length; i++)
            {
                (BoosterKind kind, Recovery recovery, string _) = Boosters[i];
                var rect = new RectF(area.Left + (i * (w + gap)), area.Top, area.Left + (i * (w + gap)) + w, area.Bottom);
                bool unlocked = PlaytestFlavor.Tester || _meta.Economy.IsUnlocked(kind);
                string label = PlaytestFlavor.Tester ? ShortName(kind)
                    : !unlocked ? ShortName(kind) + " L" + UnlockLevel(kind)
                    : _meta.Economy.Charges(kind) > 0 ? ShortName(kind) + " ×" + _meta.Economy.Charges(kind)
                    : ShortName(kind) + " " + _meta.Economy.Price(kind) + "✿";
                Color color = !unlocked ? Locked : _targeting == recovery ? Warning : Accent;
                Button(canvas, rect, label, color, () => PressBooster(kind, recovery));
            }
        }

        private void DrawTray(Canvas canvas, RectF area)
        {
            LevelView view = _session.View;
            _podRects.Clear();
            int stacks = view.StackCount;
            if (stacks == 0)
            {
                return;
            }

            int deepest = 1;
            for (int s = 0; s < stacks; s++)
            {
                deepest = Math.Max(deepest, view.Stack(s).Count);
            }

            float columnWidth = area.Width() / stacks;
            float size = Math.Min(columnWidth * 0.86f, area.Height() / Math.Min(deepest, 4) * 0.94f);
            int visible = Math.Max(1, (int)(area.Height() / (size * 1.04f)));
            for (int s = 0; s < stacks; s++)
            {
                IReadOnlyList<string> stack = view.Stack(s);
                float cx = area.Left + (columnWidth * (s + 0.5f));
                for (int i = 0; i < stack.Count && i < visible; i++)
                {
                    float top = area.Top + (i * size * 1.04f);
                    var rect = new RectF(cx - (size / 2f), top, cx + (size / 2f), top + size);
                    bool last = i == visible - 1 && stack.Count > visible;
                    if (last)
                    {
                        Text(canvas, "+" + (stack.Count - i), cx, rect.CenterY(), size * 0.3f, Locked);
                        break;
                    }

                    string id = stack[i];
                    bool exposed = view.IsExposed(id);
                    _podRects[id] = rect;
                    DrawPod(canvas, view.Pod(id), rect, size, exposed);
                    Hit(rect, () => Tap(id));
                }
            }
        }

        private void DrawPod(Canvas canvas, PodInfo pod, RectF rect, float size, bool exposed)
        {
            Color color = pod.Variant.HasValue ? VariantColor(pod.Variant.Value) : Locked;
            Color ink = pod.Variant.HasValue ? Ink(color) : Color.White;
            if (!exposed)
            {
                color = Color.Argb(110, color.R, color.G, color.B);
            }

            Fill(canvas, rect, color, size * 0.2f);
            string label = pod.Variant.HasValue ? Code(pod.Variant.Value) : "?";
            Text(canvas, label, rect.CenterX(), rect.Top + (size * 0.36f), size * 0.3f, ink);
            Text(canvas, pod.Remaining.ToString(), rect.CenterX(), rect.Top + (size * 0.72f), size * 0.34f, ink);
            if (pod.Locked || _animator.HeldPodLocks.Contains(pod.Id))
            {
                Fill(canvas, rect, Color.Argb(150, 40, 40, 40), size * 0.2f);
                Text(canvas, "🔒", rect.CenterX(), rect.CenterY(), size * 0.42f, Color.White);
            }

            if (pod.ConnectedGroupId != null)
            {
                Text(canvas, "∞", rect.Right - (size * 0.16f), rect.Top + (size * 0.18f), size * 0.26f, EntryColor);
            }
        }

        /// <summary>A committed pod's card flying from the tray to its slot.</summary>
        private void DrawFlights(Canvas canvas)
        {
            foreach (Flight flight in _animator.Flights)
            {
                RectF? target = _slotRects[flight.Slot];
                if (target == null)
                {
                    continue;
                }

                float k = Math.Clamp((_animator.Now - flight.Start) / LevelAnimator.FlightSeconds, 0f, 1f);
                float x = flight.FromX + ((target.CenterX() - flight.FromX) * k);
                float y = flight.FromY + ((target.CenterY() - flight.FromY) * k);
                float s = target.Width() * (0.9f - (0.2f * k));
                var rect = new RectF(x - (s / 2f), y - (s / 2f), x + (s / 2f), y + (s / 2f));
                Color color = flight.Variant.HasValue ? VariantColor(flight.Variant.Value) : Locked;
                Fill(canvas, rect, WithAlpha(color, 220), s * 0.2f);
                Text(canvas, flight.Variant.HasValue ? Code(flight.Variant.Value) : "?", rect.CenterX(), rect.CenterY(), s * 0.34f, flight.Variant.HasValue ? Ink(color) : Color.White);
            }
        }

        private void DrawToast(Canvas canvas, RectF area)
        {
            if (_toast == null || SystemClock.UptimeMillis() > _toastUntil)
            {
                _toast = null;
                return;
            }

            float size = Width * 0.042f;
            var rect = new RectF(area.Left, area.Bottom - (size * 2.2f), area.Right, area.Bottom);
            Fill(canvas, rect, Color.Argb(220, 255, 255, 255), size * 0.5f);
            Text(canvas, _toast, rect.CenterX(), rect.CenterY(), size, Warning);
        }

        /// <summary>The win and jam cards, once the animation has shown the end (FR-025, FR-027).</summary>
        private void DrawOverlay(Canvas canvas)
        {
            LevelStatus status = _session.Status;
            bool won = status == LevelStatus.Won;
            bool jam = (status == LevelStatus.Jammed || status == LevelStatus.Stuck) && !_jamHidden;
            if ((!won && !jam) || !_animator.Settled)
            {
                return;
            }

            float w = Width;
            // Restored cells of a won level show the finished picture: keep the board visible, dim only the controls.
            var shade = new RectF(0, won ? Height * 0.6f : 0, Width, Height);
            Fill(canvas, shade, Color.Argb(won ? 90 : 120, 30, 30, 35), 0f);
            Hit(new RectF(0, 0, Width, Height), () => { });
            var card = new RectF(w * 0.08f, Height * 0.6f, w * 0.92f, Height * 0.94f);
            Fill(canvas, card, Panel, w * 0.04f);
            if (won)
            {
                Text(canvas, PlaytestText.T("win.title"), card.CenterX(), card.Top + (card.Height() * 0.16f), w * 0.08f, Accent);
                Text(canvas, RewardText(), card.CenterX(), card.Top + (card.Height() * 0.36f), w * 0.042f, TextColor);
                string milestone = MilestoneText();
                if (milestone.Length > 0)
                {
                    Text(canvas, milestone, card.CenterX(), card.Top + (card.Height() * 0.5f), w * 0.04f, Warning);
                }

                float buttonTop = card.Top + (card.Height() * 0.62f);
                if (PlaytestFlavor.Tester)
                {
                    Button(canvas, new RectF(card.Left + (w * 0.1f), buttonTop, card.Right - (w * 0.1f), card.Bottom - (card.Height() * 0.1f)), PlaytestText.T("common.next") + " ▶", Accent, () => LoadLevel(_level + 1));
                    return;
                }

                Button(canvas, new RectF(card.Left + (w * 0.05f), buttonTop, card.CenterX() - (w * 0.02f), card.Bottom - (card.Height() * 0.1f)), "⌂", TextColor, GoHome);
                Button(canvas, new RectF(card.CenterX() + (w * 0.02f), buttonTop, card.Right - (w * 0.05f), card.Bottom - (card.Height() * 0.1f)), PlaytestText.T("common.next") + " ▶", Accent, StartLevel);
                return;
            }

            Text(canvas, PlaytestText.T(status == LevelStatus.Stuck ? "jam.stuck" : "jam.title"), card.CenterX(), card.Top + (card.Height() * 0.16f), w * 0.065f, Warning);
            var options = new List<(string, Color, Action)> { (PlaytestText.T("common.restart"), TextColor, Restart) };
            foreach (Recovery recovery in _session.EligibleRecoveries())
            {
                foreach ((BoosterKind kind, Recovery r, string _) in Boosters)
                {
                    if (r == recovery && PlaytestFlavor.Tester)
                    {
                        options.Add((ShortName(kind), Accent, () => PressBooster(kind, r)));
                    }
                    else if (r == recovery && _meta.Economy.IsUnlocked(kind) && _meta.Economy.CanAfford(kind))
                    {
                        string cost = _meta.Economy.Charges(kind) > 0 ? "×" + _meta.Economy.Charges(kind) : _meta.Economy.Price(kind) + "✿";
                        options.Add((ShortName(kind) + " " + cost, Accent, () => PressBooster(kind, r)));
                    }
                }
            }

            if (!PlaytestFlavor.Tester)
            {
                options.Add(("⌂", Locked, GoHome));
            }

            float rowTop = card.Top + (card.Height() * 0.3f);
            int rows = Math.Max(1, (options.Count + 1) / 2);
            float rowHeight = (card.Bottom - rowTop - (card.Height() * 0.05f)) / rows;
            for (int i = 0; i < options.Count; i++)
            {
                (string label, Color color, Action action) = options[i];
                float left = i % 2 == 0 ? card.Left + (w * 0.04f) : card.CenterX() + (w * 0.01f);
                float top = rowTop + ((i / 2) * rowHeight);
                Button(canvas, new RectF(left, top, left + (card.Width() / 2f) - (w * 0.05f), top + (rowHeight * 0.85f)), label, color, action);
            }
        }

        private string RewardText()
        {
            LevelReward? reward = _payout?.Reward;
            if (reward == null)
            {
                return string.Empty;
            }

            string text = PlaytestText.F("common.petals_plus", reward.Petals);
            return reward.DroppedBooster.HasValue ? text + "   +1 " + ShortName(reward.DroppedBooster.Value) : text;
        }

        private string MilestoneText()
        {
            MilestoneGrant? grant = _payout?.Milestone;
            if (grant == null)
            {
                return string.Empty;
            }

            string text = PlaytestText.F("win.milestone", grant.Petals);
            if (grant.Boosters != null)
            {
                int charges = grant.Boosters.ExtraSlot + grant.Boosters.Shuffle + grant.Boosters.Return + grant.Boosters.BloomBurst;
                text += "  " + (charges == 1 ? PlaytestText.T("win.boosters_one") : PlaytestText.F("win.boosters_many", charges));
            }

            return grant.Item != null ? text + "  + " + grant.Item : text;
        }

        /// <summary>A demo card; a tap anywhere closes it (it is shown once).</summary>
        private void DrawDemo(Canvas canvas)
        {
            if (_demo == null)
            {
                return;
            }

            float w = Width;
            Fill(canvas, new RectF(0, 0, Width, Height), Color.Argb(110, 20, 20, 25), 0f);
            Hit(new RectF(0, 0, Width, Height), CloseDemo);
            var card = new RectF(w * 0.08f, Height * 0.3f, w * 0.92f, Height * 0.62f);
            Fill(canvas, card, Panel, w * 0.04f);
            float y = card.Top + (card.Height() * 0.16f);
            foreach (string line in _demo.Lines)
            {
                Text(canvas, line, card.CenterX(), y, Math.Min(w * 0.045f, card.Width() * 0.9f / Math.Max(12, line.Length) * 1.8f), TextColor);
                y += card.Height() * 0.14f;
            }

            if (_demo.Variants.Count > 0)
            {
                float size = card.Height() * 0.3f;
                float total = (_demo.Variants.Count * size) + ((_demo.Variants.Count - 1) * size * 0.6f);
                float x = card.CenterX() - (total / 2f);
                float top = card.Bottom - (card.Height() * 0.46f);
                for (int i = 0; i < _demo.Variants.Count; i++)
                {
                    var tile = new RectF(x, top, x + size, top + size);
                    Color color = VariantColor(_demo.Variants[i]);
                    Fill(canvas, tile, color, size * 0.18f);
                    Text(canvas, Code(_demo.Variants[i]), tile.CenterX(), tile.CenterY(), size * 0.4f, Ink(color));
                    x += size * 1.6f;
                }

                if (_demo.ShowIgnore)
                {
                    Text(canvas, "✕", card.CenterX(), top + (size / 2f), size * 0.6f, Warning);
                }
            }

            Text(canvas, "tap to continue", card.CenterX(), card.Bottom - (card.Height() * 0.07f), w * 0.03f, Locked);
        }

        // ---- Colors and primitives ----

        /// <summary>The finished picture under a restored cell: a light version of its role's variant color.</summary>
        private Color PictureColor(int x, int y)
        {
            BasePicture picture = _session.Picture;
            LevelDefinition definition = _session.Definition;
            int px = definition.Picture.Mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
            if (px < 0 || px >= picture.Width || y < 0 || y >= picture.Height)
            {
                return Ground;
            }

            int value = picture.CellAt(px, y);
            if (value >= 0 && definition.Mapping.TryGetValue(picture.Roles[value].RoleId, out VariantId variant))
            {
                Color c = VariantColor(variant);
                return Color.Rgb(c.R + ((255 - c.R) * 55 / 100), c.G + ((255 - c.G) * 55 / 100), c.B + ((255 - c.B) * 55 / 100));
            }

            return value == BasePicture.Stone ? StoneColor : Ground;
        }

        private static Color VariantColor(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Color.ParseColor(info.ColorHex) : Color.Gray;

        /// <summary>Dark text on light variants, white on dark ones (the WCAG contrast rule of the Unity client).</summary>
        private static Color Ink(Color color) =>
            Client.Art.Variants.InkContrast.UseDarkInk(color.R / 255.0, color.G / 255.0, color.B / 255.0) ? Color.ParseColor("#2B2B2B") : Color.White;

        private static Color WithAlpha(Color color, int alpha) => Color.Argb(Math.Clamp(alpha, 0, 255) * color.A / 255, color.R, color.G, color.B);

        private static string Code(VariantId variant) =>
            Codes.TryGetValue(variant.Key, out string? code) ? code : variant.Key.Substring(0, Math.Min(2, variant.Key.Length));

        private static RectF Inset(RectF rect, float by) => new RectF(rect.Left + by, rect.Top + by, rect.Right - by, rect.Bottom - by);

        private static RectF Scale(RectF rect, float sx, float sy)
        {
            float hw = rect.Width() * sx / 2f;
            float hh = rect.Height() * sy / 2f;
            return new RectF(rect.CenterX() - hw, rect.CenterY() - hh, rect.CenterX() + hw, rect.CenterY() + hh);
        }

        private void Fill(Canvas canvas, RectF rect, Color color, float radius)
        {
            _paint.Color = color;
            _paint.SetStyle(Paint.Style.Fill);
            if (radius > 0f)
            {
                canvas.DrawRoundRect(rect, radius, radius, _paint);
            }
            else
            {
                canvas.DrawRect(rect, _paint);
            }
        }

        private void Text(Canvas canvas, string text, float cx, float cy, float size, Color color)
        {
            _text.Color = color;
            _text.TextSize = size;
            canvas.DrawText(text, cx, cy - ((_text.Descent() + _text.Ascent()) / 2f), _text);
        }

        private void Button(Canvas canvas, RectF rect, string label, Color color, Action action)
        {
            Fill(canvas, rect, color, rect.Height() * 0.3f);
            // The label shrinks to fit narrow buttons ("Shuffle ×2").
            float size = Math.Min(rect.Height() * 0.42f, rect.Width() * 0.9f / Math.Max(1, label.Length) * 1.7f);
            Text(canvas, label, rect.CenterX(), rect.CenterY(), size, Color.White);
            Hit(rect, action);
        }

        private void Hit(RectF rect, Action action) => _hits.Add((rect, action));
    }
}
