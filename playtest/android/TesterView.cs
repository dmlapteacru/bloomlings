using System;
using System.Globalization;
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
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using Color = Android.Graphics.Color;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The level tester's view (<see cref="PlaytestFlavor.Tester"/>): the first playtest's quick loop in its minimal
    /// look (spec 002 FR-003 keeps it as it was). Levels open straight away and ◀ ▶ move between them. Every booster is
    /// free, and a tap shows its result at once with its sound. There is no Home, progression or economy. The full
    /// playtest draws the design board's screens instead (<c>Droid.DesignView</c>).
    /// <para>
    /// With round content (spec 006 FR-060, FR-062, FR-063; the APKs carry nothing else) ◀ ▶ walk the round levels, and
    /// the level button jumps to any level number, kept as the tester's progress; past the last round level it says "new
    /// levels soon". A round level plays on the round rules' live timeline (<see cref="DrawRound"/>), its boosters free.
    /// </para>
    /// </summary>
    public sealed class TesterView : View
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

        /// <summary>The round levels, when the build carries them (spec 006): then the legacy set is empty and unused.</summary>
        private readonly RoundContentSet? _rounds;

        /// <summary>The round level shown, or null past the last one ("new levels soon").</summary>
        private RoundLevel? _round;

        /// <summary>The round level played on the live timeline (spec 006 T026), with its display clock and animator.</summary>
        private Bloomlings.Client.UI.Design.RoundPlay? _play;
        private readonly ISharedPreferences _prefs;
        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint _text = new Paint(PaintFlags.AntiAlias) { TextAlign = Paint.Align.Center };
        private readonly List<(RectF Rect, Action Action)> _hits = new List<(RectF, Action)>();
        private readonly Dictionary<string, RectF> _podRects = new Dictionary<string, RectF>(StringComparer.Ordinal);
        private readonly RectF?[] _slotRects = new RectF?[WaitingSlots.Capacity];
        private readonly LevelAnimator _animator = new LevelAnimator();
        private readonly PlaytestSound _sound;
        private LevelSession _session = null!;
        private int _level;
        private string? _toast;
        private long _toastUntil;
        private Recovery? _targeting;
        private bool _jamHidden;
        private long _lastFrame;
        private long _lastClearSound;
        private (float Ox, float Oy, float Cell, int Height) _board;
        private int _insetTop;
        private int _insetBottom;

        public TesterView(Context context)
            : base(context)
        {
            _context = context;
            _content = PlaytestContent.Load();
            _rounds = PlaytestContent.HasRounds ? PlaytestContent.LoadRounds() : null;
            _prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            _text.SetTypeface(Typeface.DefaultBold);
            bool sound = _prefs.GetBoolean("sound", true);
            _sound = new PlaytestSound(context) { Enabled = sound, Haptics = sound };
            _animator.Speed = 1f;
            _animator.Arrived += OnArrived;
            _animator.Shown += OnShown;
            LoadLevel(_prefs.GetInt("level", 1));
        }

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
            if (_rounds != null)
            {
                DrawRound(canvas);
                return;
            }

            long now = SystemClock.UptimeMillis();
            float dt = _lastFrame == 0 ? 0f : Math.Min(0.1f, (now - _lastFrame) / 1000f);
            _lastFrame = now;
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

        /// <summary>Plays a level; the tester remembers it, the full playtest follows the progression.</summary>
        private void LoadLevel(int levelNumber)
        {
            if (_rounds != null)
            {
                LoadRound(levelNumber);
                return;
            }

            _level = Math.Max(1, levelNumber);
            _prefs.Edit()!.PutInt("level", _level)!.Apply();

            LevelDefinition definition = _content.GetLevel(Resolve(_level));
            _session = LevelSession.Load(definition, _content.GetPicture(definition.Picture), new SessionOptions(_content.ContentVersion, _content.ShuffleNodeBudget));
            _animator.Reset(_session.View);
            _targeting = null;
            _jamHidden = false;
            ShowLevelIntro();
            Invalidate();
        }

        private int Resolve(int levelNumber) =>
            _content.TryGetLevel(levelNumber, out _) ? levelNumber : _content.LevelNumbers[(levelNumber - 1) % _content.LevelCount];

        /// <summary>Before play: the difficulty label.</summary>
        private void ShowLevelIntro()
        {
            DifficultyClass difficulty = _session.Definition.Difficulty.Class;
            if (difficulty != DifficultyClass.Normal)
            {
                Toast(PlaytestText.T(difficulty == DifficultyClass.Hard ? "difficulty.hard" : "difficulty.super_hard"));
            }
        }

        // ---- Commands ----

        private void Tap(string podId)
        {
            if (_targeting != null)
            {
                return;
            }

            var tap = new TapPod(podId);
            CommandCheck check = _session.Check(tap);
            if (!check.IsAllowed)
            {
                _sound.Play(SoundCue.Refused);
                Toast(check.Reason == RejectReason.NotExposed && Bloomlings.Client.UI.Design.PodLinks.WaitsForPartner(_session.View, podId) ? PlaytestText.T("refusal.partner_buried") : RefusalText(check.Reason));
                Invalidate();
                return;
            }

            CommandResult result = _session.Apply(tap);
            _sound.Play(SoundCue.Tap);
            ShowAtOnce(result);

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

            _animator.Flush(_session.View);
            CommandResult result = _session.Apply(command);
            _sound.Play(SoundCue.Booster);
            ShowAtOnce(result);

            AfterCommand();
        }

        private void PressBooster(BoosterKind kind, Recovery recovery)
        {
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

        /// <summary>A won level moves the remembered level on, so the tester reopens at the next one.</summary>
        private void AfterCommand()
        {
            if (_session.Status == LevelStatus.Won)
            {
                _prefs.Edit()!.PutInt("level", _level + 1)!.Apply();
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

        private void Toast(string message)
        {
            _toast = message;
            _toastUntil = SystemClock.UptimeMillis() + 1600;
            PostDelayed(Invalidate, 1700);
        }

        private void ToggleSound()
        {
            // The tester's one toggle mutes both the sound and the vibration.
            _sound.Enabled = !_sound.Enabled;
            _sound.Haptics = _sound.Enabled;
            _prefs.Edit()!.PutBoolean("sound", _sound.Enabled)!.Apply();
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

        // The round rules' refusals, as the full playtest words them (RoundLevelScreen.RefusalText; the tester does not
        // link the design screens).
        private static string RoundRefusalText(RoundRefusal refusal) => refusal switch
        {
            RoundRefusal.NotExposed => PlaytestText.T("refusal.not_exposed"),
            RoundRefusal.PartnerNotExposed => PlaytestText.T("refusal.partner_buried"),
            RoundRefusal.PairNeedsTwoSlots => PlaytestText.T("refusal.group"),
            RoundRefusal.BridgeNotReady => PlaytestText.T("round.bridge_not_ready"),
            _ => PlaytestText.T("refusal.no_free_slot"),
        };

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
            // The tester's quick loop: ◀ ▶ move between levels.
            Button(canvas, new RectF(bar.Left, bar.Top, bar.Left + (h * 1.2f), bar.Bottom), "◀", Locked, () => LoadLevel(_level - 1));
            Button(canvas, new RectF(bar.Right - (h * 2.5f), bar.Top, bar.Right - (h * 1.3f), bar.Bottom), "▶", Locked, () => LoadLevel(_level + 1));

            Button(canvas, new RectF(bar.Left + (h * 1.3f), bar.Top, bar.Left + (h * 2.5f), bar.Bottom), _sound.Enabled ? "♪" : "×", _sound.Enabled ? Accent : Locked, ToggleSound);
            Button(canvas, new RectF(bar.Right - (h * 1.2f), bar.Top, bar.Right, bar.Bottom), "↻", TextColor, Restart);

            DifficultyClass difficulty = _session.Definition.Difficulty.Class;
            int resolved = Resolve(_level);
            string title = PlaytestText.F("common.level", _level) + (resolved != _level ? " (= L" + resolved + ")" : string.Empty);
            if (difficulty != DifficultyClass.Normal)
            {
                title += " · " + PlaytestText.T(difficulty == DifficultyClass.Hard ? "difficulty.hard" : "difficulty.super_hard");
            }

            Text(canvas, title, bar.CenterX(), bar.Top + (h * 0.27f), h * 0.34f, difficulty == DifficultyClass.Normal ? TextColor : Warning);

            // Direct access to the three oversized mosaic prototypes.
            float shortcutWidth = h * 0.62f;
            float shortcutGap = h * 0.08f;
            float shortcutsWidth = (shortcutWidth * 5f) + (shortcutGap * 4f);
            float shortcutLeft = bar.CenterX() - (shortcutsWidth / 2f);
            string[] labels = { "24", "30×32", "36", "×9", "Gate" };
            int[] levels = { 5001, 5002, 5003, 5004, 5005 };
            for (int i = 0; i < labels.Length; i++)
            {
                int targetLevel = levels[i];
                float left = shortcutLeft + (i * (shortcutWidth + shortcutGap));
                var rect = new RectF(left, bar.Top + (h * 0.53f), left + shortcutWidth, bar.Bottom);
                Button(canvas, rect, labels[i], _level == targetLevel ? Accent : Locked, () => LoadLevel(targetLevel));
            }
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
                    RectF tile = Inset(full, cell * 0.018f);
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
            Fill(canvas, tile, WithAlpha(color, alpha), cell * 0.12f);
            // The next layer's corner, on a peek board only: an icons board keeps it a surprise (FR-036 as amended on 2026-10-06).
            if (info.StackRemaining > 0)
                Text(canvas, info.StackRemaining.ToString(CultureInfo.InvariantCulture), tile.CenterX(), tile.CenterY(), cell * 0.7f, WithAlpha(Color.White, alpha));
            if (info.StackRemaining == 0 && _session.View.BoardLook == BoardLook.Peek && info.RemainingLayers > 1 && info.Next.HasValue)
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

                float walked = _animator.Now - walker.Start;
                float progress = Math.Clamp(walked / Math.Max(0.05f, walker.Arrival), 0f, 1f);
                if (walked < 0f || (progress >= 1f && walked > walker.Arrival + 0.12f))
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
                // Free on screen: the pods show where the animation put them (LevelAnimator.Place).
                if (view.SlotStateOf(slot) != SlotState.Locked && !_animator.HeldSlotLocks.Contains(slot) && _animator.Slots[slot].PodId == null)
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
                int rules = LevelAnimator.RulesSlotOf(view, look.PodId);
                if (_targeting == Recovery.Return && rules >= 0)
                {
                    Hit(rect, () => UseBooster(BoosterKind.Return, new UseReturn(rules)));
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
                bool unlocked = true;
                string label = ShortName(kind);
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
                float buttonTop = card.Top + (card.Height() * 0.62f);
                Button(canvas, new RectF(card.Left + (w * 0.1f), buttonTop, card.Right - (w * 0.1f), card.Bottom - (card.Height() * 0.1f)), PlaytestText.T("common.next") + " ▶", Accent, () => LoadLevel(_level + 1));
                return;

            }

            Text(canvas, PlaytestText.T(status == LevelStatus.Stuck ? "jam.stuck" : "jam.title"), card.CenterX(), card.Top + (card.Height() * 0.16f), w * 0.065f, Warning);
            var options = new List<(string, Color, Action)> { (PlaytestText.T("common.restart"), TextColor, Restart) };
            foreach (Recovery recovery in _session.EligibleRecoveries())
            {
                foreach ((BoosterKind kind, Recovery r, string _) in Boosters)
                {
                    if (r == recovery)
                    {
                        options.Add((ShortName(kind), Accent, () => PressBooster(kind, r)));
                    }
                }
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

        // ---- Round levels (spec 006) ----

        /// <summary>
        /// Shows round level <paramref name="levelNumber"/> and keeps it as the tester's progress (spec 006 FR-063: any
        /// number, past the last round level too, where "new levels soon" shows).
        /// </summary>
        private void LoadRound(int levelNumber)
        {
            _level = Math.Max(1, levelNumber);
            _prefs.Edit()!.PutInt("level", _level)!.Apply();
            _round = _rounds != null && _rounds.Has(_level) ? _rounds.GetLevel(_level) : null;
            _play = _round != null ? new Bloomlings.Client.UI.Design.RoundPlay(_round) : null;
            _lastFrame = 0;
            _toast = null;
            Invalidate();
        }

        /// <summary>The tester's level jump (spec 006 FR-063, T029): the system's dialog with a number field.</summary>
        private void AskJump()
        {
            var field = new Android.Widget.EditText(_context)
            {
                Text = _level.ToString(CultureInfo.InvariantCulture),
                InputType = Android.Text.InputTypes.ClassNumber,
            };
            field.SetSingleLine(true);
            field.SetFilters(new Android.Text.IInputFilter[] { new Android.Text.InputFilterLengthFilter(5) });
            field.SetSelectAllOnFocus(true);
            var frame = new Android.Widget.FrameLayout(_context);
            int pad = (int)(20 * _context.Resources!.DisplayMetrics!.Density);
            frame.SetPadding(pad, pad / 2, pad, 0);
            frame.AddView(field);
            Android.App.AlertDialog dialog = new Android.App.AlertDialog.Builder(_context)
                .SetTitle(PlaytestText.T("round.jump.title"))!
                .SetView(frame)!
                .SetPositiveButton(PlaytestText.T("common.ok"), (sender, args) =>
                {
                    if (int.TryParse((field.Text ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int level) && level >= 1)
                    {
                        LoadRound(level);
                    }
                    else
                    {
                        Toast(PlaytestText.T("round.jump.invalid"));
                        Invalidate();
                    }
                })!
                .SetNegativeButton(PlaytestText.T("common.cancel"), (sender, args) => Invalidate())!
                .Create()!;
            dialog.Window?.SetSoftInputMode(SoftInput.StateAlwaysVisible);
            dialog.Show();
            field.RequestFocus();
        }

        /// <summary>
        /// A round level played on the round rules' live timeline (spec 006 T026, FR-062), in the tester's minimal look: the
        /// top bar (◀, the sound, the level button that jumps, ▶), the board in the round palette (its frame stone, its gate
        /// yellow, the stream blue, cleared cells as ground, the walkers as dots of their material on their couriers'
        /// routes), the level's summary and its state, the places, the four boosters (free, as every tester booster) with
        /// Restart, and the columns, top pod first (a hidden pod shows "?" until it is on top, FR-007); a tap on a top pod or
        /// on a buildable bridge site commits it at the current model time. The clock runs at ×2 (the tester's quick loop),
        /// Auto too. Past the last level the "new levels soon" card.
        /// </summary>
        private void DrawRound(Canvas canvas)
        {
            float width = Width;
            float top = _insetTop + (Width * 0.02f);
            float bottom = Height - _insetBottom - (Width * 0.02f);
            float usable = bottom - top;
            float pad = width * 0.03f;
            var bar = new RectF(pad, top, width - pad, top + (usable * 0.06f));
            float h = bar.Height();
            RoundContentSet rounds = _rounds!;
            int count = rounds.LevelCount;
            Button(canvas, new RectF(bar.Left, bar.Top, bar.Left + (h * 1.2f), bar.Bottom), "◀", Locked, () => LoadRound(Math.Min(_level - 1, count)));
            Button(canvas, new RectF(bar.Left + (h * 1.3f), bar.Top, bar.Left + (h * 2.5f), bar.Bottom), _sound.Enabled ? "♪" : "×", _sound.Enabled ? Accent : Locked, ToggleSound);
            Button(canvas, new RectF(bar.Right - (h * 1.2f), bar.Top, bar.Right, bar.Bottom), "▶", _round != null ? Locked : SlotEmpty, () =>
            {
                if (rounds.Has(_level))
                {
                    LoadRound(_level + 1);
                }
            });

            string title = PlaytestText.F("common.level", _level) + " / " + count;
            if (_round != null && _round.Class != RoundClass.Normal)
            {
                title += " · " + PlaytestText.T(_round.Class == RoundClass.Hard ? "difficulty.hard" : "difficulty.super_hard");
            }

            // The level button: a tap asks for any level number.
            var jump = new RectF(bar.Left + (h * 2.7f), bar.Top, bar.Right - (h * 1.4f), bar.Bottom);
            Fill(canvas, jump, Panel, h * 0.3f);
            Text(canvas, title, jump.CenterX(), jump.CenterY(), h * 0.4f, _round == null || _round.Class == RoundClass.Normal ? TextColor : Warning);
            Hit(jump, AskJump);

            if (_round == null || _play == null)
            {
                var card = new RectF(width * 0.08f, top + (usable * 0.3f), width * 0.92f, top + (usable * 0.6f));
                Fill(canvas, card, Panel, width * 0.04f);
                Text(canvas, PlaytestText.T("round.soon.title"), card.CenterX(), card.Top + (card.Height() * 0.3f), width * 0.07f, Accent);
                Text(canvas, PlaytestText.F("round.soon.body", count), card.CenterX(), card.Top + (card.Height() * 0.62f), width * 0.032f, TextColor);
                DrawToast(canvas, card);
                return;
            }

            // The live clock: real time × 2, as whole µs (the rules never see the frame rate).
            long now = SystemClock.UptimeMillis();
            float dt = _lastFrame == 0 ? 0f : Math.Min(0.1f, (now - _lastFrame) / 1000f);
            _lastFrame = now;
            foreach (RoundEvent e in _play.Advance(dt, _play.Speed(true)))
            {
                if (e.Type == RoundEventType.Complete)
                {
                    _sound.Play(SoundCue.PodDone);
                }
                else if (e.Type == RoundEventType.Clear && now - _lastClearSound > 60)
                {
                    _lastClearSound = now;
                    _sound.Play(SoundCue.Clear);
                }
            }

            RoundLevel level = _round;
            var boardArea = new RectF(pad, top + (usable * 0.07f), width - pad, top + (usable * 0.52f));
            DrawRoundBoard(canvas, level, boardArea);

            float line = usable * 0.024f;
            float y = top + (usable * 0.545f);
            Text(canvas, PlaytestText.F("round.tester.board", level.Width, level.Height, level.Pods.Count, level.Tray.Count, level.Capacity), width / 2f, y, line * 0.72f, TextColor);
            y += line;
            string mechanics = level.Mechanics.Count == 0 ? "-" : string.Join(", ", level.Mechanics);
            if (level.Introduction != null)
            {
                mechanics += " (" + level.Introduction.Mechanic + (level.Introduction.Second ? " 2" : " 1") + ")";
            }

            Text(canvas, PlaytestText.F("round.tester.picture", level.PictureId ?? "-", level.PictureVersion, level.DefinitionVersion) + " · " + PlaytestText.F("round.tester.mechanics", mechanics), width / 2f, y, line * 0.72f, level.Exception != null ? Warning : TextColor);
            if (level.Exception != null)
            {
                y += line;
                Text(canvas, PlaytestText.F("round.tester.exception", level.Exception.Rule), width / 2f, y, line * 0.72f, Warning);
            }

            y += line;
            RoundStatus status = _play.Status;
            string state = status == RoundStatus.Won ? PlaytestText.T("win.title")
                : status == RoundStatus.Jammed ? PlaytestText.T("jam.title")
                : status == RoundStatus.Stuck ? PlaytestText.T("jam.stuck")
                : string.Empty;
            if (state.Length > 0)
            {
                Text(canvas, state, width / 2f, y, line * 0.9f, status == RoundStatus.Won ? Accent : Warning);
            }

            DrawRoundPlaces(canvas, new RectF(pad, top + (usable * 0.60f), width - pad, top + (usable * 0.67f)));
            DrawRoundBoosters(canvas, new RectF(pad, top + (usable * 0.685f), width - pad, top + (usable * 0.735f)));
            DrawRoundTray(canvas, new RectF(pad, top + (usable * 0.75f), width - pad, bottom));
            DrawToast(canvas, boardArea);
            if (_play.Timeline.Pending || _play.Animator.Busy)
            {
                PostInvalidateOnAnimation();
            }
            else
            {
                _lastFrame = 0;
            }
        }

        /// <summary>A command of the round level at the current model time; a refusal shows the rules' reason.</summary>
        private void CommitRound(RoundAction action)
        {
            if (_play == null)
            {
                return;
            }

            RoundCommitResult result = _play.Commit(action);
            if (!result.Accepted)
            {
                _sound.Play(SoundCue.Refused);
                if (result.Refusal != RoundRefusal.Finished)
                {
                    Toast(RoundRefusalText(result.Refusal));
                }

                return;
            }

            _sound.Play(SoundCue.Tap);
            Invalidate();
        }

        /// <summary>A free booster of the tester on the round rules (FR-012): Return and Bloom Burst take the first target the rules accept.</summary>
        private void RoundBooster(BoosterKind kind)
        {
            if (_play == null)
            {
                return;
            }

            RoundInput? command = null;
            switch (kind)
            {
                case BoosterKind.ExtraSlot:
                    command = RoundInput.ExtraSlot(0);
                    break;
                case BoosterKind.Shuffle:
                    command = RoundInput.Shuffle(0);
                    break;
                case BoosterKind.Return:
                    int pod = Bloomlings.Client.UI.Design.GuideTour.ReturnPod(_play.Timeline);
                    command = pod >= 0 ? RoundInput.Return(pod, 0) : (RoundInput?)null;
                    break;
                default:
                    IReadOnlyList<char> codes = Bloomlings.Client.UI.Design.GuideTour.BurstMaterials(_play.Timeline);
                    command = codes.Count > 0 ? RoundInput.BloomBurst(codes[0], 0) : (RoundInput?)null;
                    break;
            }

            if (command == null || !_play.Apply(command.Value).Accepted)
            {
                _sound.Play(SoundCue.Refused);
                Toast(PlaytestText.T("gameplay.booster_useless"));
                return;
            }

            _sound.Play(SoundCue.Booster);
            Invalidate();
        }

        private void DrawRoundBoard(Canvas canvas, RoundLevel level, RectF area)
        {
            var a = _play!.Animator;
            int w = level.Width;
            int h = level.Height;
            float cell = Math.Min(area.Width() / w, area.Height() / h);
            float ox = area.CenterX() - (cell * w / 2f);
            float oy = area.Top + ((area.Height() - (cell * h)) / 2f);
            var entries = new HashSet<int>(level.Entries);
            var sites = new Dictionary<int, int>();
            if (level.Bridge != null)
            {
                for (int i = 0; i < level.Bridge.Count; i++)
                {
                    sites[level.Bridge[i].Cell] = i;
                }
            }

            // A wall laid over the picture (the level's overlay) is grey stone; the picture's own strip stays water.
            var stone = new HashSet<int>(level.OverlayCells());

            List<int> buildable = RoundRules.BridgeChoices(_play.Timeline.State);
            RectF? plank = null;
            for (int y = 0; y < h; y++)
            {
                string row = level.RowsBottomFirst[y];
                for (int x = 0; x < w; x++)
                {
                    int index = (y * w) + x;
                    char c = row[x];
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    var full = new RectF(ox + (x * cell), oy + ((h - 1 - y) * cell), ox + ((x + 1) * cell), oy + ((h - y) * cell));
                    int shown = a.MaterialAt(index);
                    bool site = sites.TryGetValue(index, out int choice);
                    Color strip = stone.Contains(index) ? Color.ParseColor("#9A9488") : Color.ParseColor("#7FB8D8");
                    Color color = entries.Contains(index) ? EntryColor
                        : edge ? StoneColor
                        : site && a.Bridge == choice ? strip
                        : site ? (buildable.Contains(choice) ? EntryColor : Color.ParseColor("#C9A46A"))
                        : c == '#' ? strip
                        : shown >= 0 ? MaterialColor(RoundMaterial.Codes[shown])
                        : Ground;
                    Fill(canvas, shown < 0 || edge ? full : Inset(full, cell * 0.04f), color, 0f);
                    if (site && a.Bridge == choice)
                    {
                        // The built bridge: a plank along its crossing (up or down, left or right), onto both banks.
                        bool across = Math.Abs(level.Bridge![choice].Cell - level.Bridge[choice].Anchor) == w;
                        float reach = cell * 0.22f;
                        float side = cell * 0.11f;
                        plank = across
                            ? new RectF(full.Left + side, full.Top - reach, full.Right - side, full.Bottom + reach)
                            : new RectF(full.Left - reach, full.Top + side, full.Right + reach, full.Bottom - side);
                    }

                    if (site && buildable.Contains(choice))
                    {
                        int built = choice;
                        Hit(Inset(full, -cell), () => CommitRound(RoundAction.BuildBridge(built)));
                    }
                }
            }

            if (plank != null)
            {
                Fill(canvas, plank, Color.ParseColor("#A87A45"), 0f);
            }

            // The walkers: a dot of their material where the timeline puts each courier now.
            _paint.SetStyle(Paint.Style.Fill);
            foreach (RoundCourier courier in _play.Timeline.Couriers)
            {
                RoundCourierPose pose = _play.Timeline.PoseOf(courier, _play.Timeline.Now);
                float cx = ox + (((float)pose.X + 0.5f) * cell);
                float cy = oy + ((h - 1 - (float)pose.Y + 0.5f) * cell);
                _paint.Color = Color.White;
                canvas.DrawCircle(cx, cy, cell * 0.36f, _paint);
                _paint.Color = MaterialColor(level.Pods[courier.Pod].Code);
                canvas.DrawCircle(cx, cy, cell * 0.28f, _paint);
            }
        }

        private void DrawRoundPlaces(Canvas canvas, RectF area)
        {
            var a = _play!.Animator;
            int places = a.PlaceCount;
            float size = Math.Min(area.Height(), area.Width() / places * 0.9f);
            float gap = (area.Width() - (size * places)) / Math.Max(1, places + 1);
            for (int k = 0; k < places; k++)
            {
                float left = area.Left + gap + (k * (size + gap));
                var rect = new RectF(left, area.CenterY() - (size / 2f), left + size, area.CenterY() + (size / 2f));
                int pod = a.PodIn(k);
                if (pod < 0)
                {
                    Fill(canvas, rect, SlotEmpty, size * 0.2f);
                    continue;
                }

                Color color = MaterialColor(_round!.Pods[pod].Code);
                Fill(canvas, rect, color, size * 0.2f);
                Text(canvas, a.Remaining(pod).ToString(CultureInfo.InvariantCulture), rect.CenterX(), rect.CenterY(), size * 0.4f, Ink(color));
            }
        }

        private void DrawRoundBoosters(Canvas canvas, RectF area)
        {
            float w = area.Width() / 5f;
            int i = 0;
            foreach ((BoosterKind kind, Recovery _, string id) in Boosters)
            {
                BoosterKind used = kind;
                var rect = new RectF(area.Left + (i * w) + (w * 0.05f), area.Top, area.Left + ((i + 1) * w) - (w * 0.05f), area.Bottom);
                Button(canvas, rect, PlaytestText.T(id), Accent, () => RoundBooster(used));
                i++;
            }

            Button(canvas, new RectF(area.Left + (4 * w) + (w * 0.05f), area.Top, area.Right, area.Bottom), PlaytestText.T("common.restart"), Locked, () =>
            {
                LoadRound(_level);
            });
        }

        private void DrawRoundTray(Canvas canvas, RectF area)
        {
            var a = _play!.Animator;
            RoundLevel level = _round!;
            int stacks = a.ColumnCount;
            if (stacks == 0)
            {
                return;
            }

            float columnWidth = area.Width() / stacks;
            float size = Math.Min(columnWidth * 0.86f, area.Height() / 4f * 0.94f);
            int visible = Math.Max(1, (int)(area.Height() / (size * 1.04f)));
            for (int s = 0; s < stacks; s++)
            {
                int waiting = a.Waiting(s);
                float cx = area.Left + (columnWidth * (s + 0.5f));
                for (int i = 0; i < waiting && i < visible; i++)
                {
                    float y = area.Top + (i * size * 1.04f);
                    var rect = new RectF(cx - (size / 2f), y, cx + (size / 2f), y + size);
                    if (i == visible - 1 && waiting > visible)
                    {
                        Text(canvas, "+" + (waiting - i), cx, rect.CenterY(), size * 0.3f, Locked);
                        break;
                    }

                    int index = a.At(s, i);
                    RoundPod pod = level.Pods[index];
                    bool hidden = !a.Revealed(index);
                    Color color = hidden ? Locked : MaterialColor(pod.Code);
                    if (i > 0)
                    {
                        color = Color.Argb(150, color.R, color.G, color.B);
                    }

                    Fill(canvas, rect, color, size * 0.2f);
                    Color ink = hidden ? Color.White : Ink(MaterialColor(pod.Code));
                    Text(canvas, hidden ? "?" : pod.Code.ToString(), rect.CenterX(), rect.Top + (size * 0.36f), size * 0.3f, ink);
                    Text(canvas, hidden ? "?" : a.Remaining(index).ToString(CultureInfo.InvariantCulture), rect.CenterX(), rect.Top + (size * 0.72f), size * 0.34f, ink);
                    if (pod.Group != null)
                    {
                        Text(canvas, "∞" + pod.Group, rect.Right - (size * 0.18f), rect.Top + (size * 0.18f), size * 0.22f, EntryColor);
                    }

                    if (i == 0)
                    {
                        int tapped = index;
                        Hit(rect, () => CommitRound(RoundAction.Tap(tapped)));
                    }
                }
            }
        }

        private static Color MaterialColor(char code)
        {
            int index = RoundMaterial.IndexOf(code);
            return index >= 0 ? Color.ParseColor(RoundMaterial.All[index].ColorHex) : Color.Gray;
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
