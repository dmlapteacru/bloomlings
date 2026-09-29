using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using Color = Android.Graphics.Color;

namespace Bloomlings.Playtest
{
    /// <summary>
    /// The whole playtest screen, drawn on a canvas from <see cref="LevelView"/> after every command: the top bar
    /// (level, class, restart and tester-only level skipping), the board (restored cells show the finished picture), the
    /// Waiting Slots, free boosters, the Source Tray, and the win and jam overlays. There are no walker animations: a tap
    /// applies at once, exactly as the rules core resolves it.
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

        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>
        {
            ["leaf"] = "Lf", ["moss"] = "Ms", ["flower"] = "Fl", ["violet_bud"] = "Vb", ["water"] = "Wa", ["dew"] = "Dw",
            ["wood"] = "Wd", ["acorn"] = "Ac", ["vine"] = "Vn", ["berry"] = "Br", ["mist"] = "Mi", ["bark"] = "Bk",
        };

        private readonly ContentSet _content;
        private readonly ISharedPreferences _prefs;
        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint _text = new Paint(PaintFlags.AntiAlias) { TextAlign = Paint.Align.Center };
        private readonly List<(RectF Rect, Action Action)> _hits = new List<(RectF, Action)>();
        private LevelSession _session = null!;
        private int _level;
        private string? _toast;
        private long _toastUntil;
        private Recovery? _targeting;
        private bool _jamHidden;
        private int _insetTop;
        private int _insetBottom;

        public GameView(Context context)
            : base(context)
        {
            _content = PlaytestContent.Load();
            _prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            _text.SetTypeface(Typeface.DefaultBold);
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
            float width = Width;
            float top = _insetTop + (Width * 0.02f);
            float bottom = Height - _insetBottom - (Width * 0.02f);
            float usable = bottom - top;
            float pad = width * 0.03f;

            DrawTopBar(canvas, new RectF(pad, top, width - pad, top + (usable * 0.07f)));
            var board = new RectF(pad, top + (usable * 0.08f), width - pad, top + (usable * 0.58f));
            DrawBoard(canvas, board);
            DrawSlots(canvas, new RectF(pad, top + (usable * 0.60f), width - pad, top + (usable * 0.68f)));
            DrawBoosters(canvas, new RectF(pad, top + (usable * 0.695f), width - pad, top + (usable * 0.745f)));
            DrawTray(canvas, new RectF(pad, top + (usable * 0.76f), width - pad, bottom));
            DrawToast(canvas, board);
            DrawOverlay(canvas);
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

        // ---- Levels and commands ----

        private void LoadLevel(int levelNumber)
        {
            _level = Math.Max(1, levelNumber);
            _prefs.Edit()!.PutInt("level", _level)!.Apply();
            LevelDefinition definition = _content.GetLevel(Resolve(_level));
            _session = LevelSession.Load(definition, _content.GetPicture(definition.Picture), new SessionOptions(_content.ContentVersion, _content.ShuffleNodeBudget));
            _targeting = null;
            _jamHidden = false;
            Invalidate();
        }

        /// <summary>Past the last playtest level the levels repeat from the start.</summary>
        private int Resolve(int levelNumber) =>
            _content.TryGetLevel(levelNumber, out _) ? levelNumber : _content.LevelNumbers[(levelNumber - 1) % _content.LevelCount];

        private void Run(Command command)
        {
            _targeting = null;
            _jamHidden = false;
            CommandCheck check = _session.Check(command);
            if (!check.IsAllowed)
            {
                Toast(RefusalText(check.Reason));
                Invalidate();
                return;
            }

            _session.Apply(command);
            if (_session.Status == LevelStatus.Won)
            {
                _prefs.Edit()!.PutInt("level", _level + 1)!.Apply();
            }

            Invalidate();
        }

        private void UseRecovery(Recovery recovery)
        {
            switch (recovery)
            {
                case Recovery.ExtraSlot:
                    Run(new UseExtraSlot());
                    break;
                case Recovery.Shuffle:
                    Run(new UseShuffle());
                    break;
                default:
                    _targeting = recovery;
                    _jamHidden = true;
                    Toast(recovery == Recovery.Return ? "Tap a waiting pod to send it back" : "Tap a tile to clear its symbol everywhere");
                    Invalidate();
                    break;
            }
        }

        private void Toast(string message)
        {
            _toast = message;
            _toastUntil = SystemClock.UptimeMillis() + 1600;
            PostDelayed(Invalidate, 1700);
        }

        private static string RefusalText(RejectReason? reason) => reason switch
        {
            RejectReason.NoFreeSlot => "No free slot",
            RejectReason.NotExposed => "Take the top pod first",
            RejectReason.Locked => "Locked: collect its key",
            RejectReason.NotEnoughSlotsForGroup => "Needs more free slots",
            RejectReason.BoosterNotApplicable => "That booster can't help here",
            _ => "Not now",
        };

        // ---- Drawing ----

        private void DrawTopBar(Canvas canvas, RectF bar)
        {
            float h = bar.Height();
            Button(canvas, new RectF(bar.Left, bar.Top, bar.Left + (h * 1.2f), bar.Bottom), "◀", Locked, () => LoadLevel(_level - 1));
            Button(canvas, new RectF(bar.Right - (h * 2.5f), bar.Top, bar.Right - (h * 1.3f), bar.Bottom), "▶", Locked, () => LoadLevel(_level + 1));
            Button(canvas, new RectF(bar.Right - (h * 1.2f), bar.Top, bar.Right, bar.Bottom), "↻", TextColor, () => Run(new Restart()));

            DifficultyClass difficulty = _session.Definition.Difficulty.Class;
            int resolved = Resolve(_level);
            string title = "Level " + _level + (resolved != _level ? " (= L" + resolved + ")" : string.Empty);
            Text(canvas, title, bar.CenterX() - (h * 0.6f), bar.Top + (h * 0.42f), h * 0.42f, TextColor);
            string subtitle = difficulty == DifficultyClass.Normal ? "playtest build" : (difficulty == DifficultyClass.Hard ? "Hard" : "Super Hard") + " · playtest";
            Text(canvas, subtitle, bar.CenterX() - (h * 0.6f), bar.Top + (h * 0.85f), h * 0.26f, difficulty == DifficultyClass.Normal ? Locked : Warning);
        }

        private void DrawBoard(Canvas canvas, RectF area)
        {
            LevelView view = _session.View;
            int w = view.Width;
            int h = view.Height;
            float cell = Math.Min(area.Width() / w, area.Height() / h);
            float ox = area.CenterX() - (cell * w / 2f);
            float oy = area.CenterY() - (cell * h / 2f);
            var specials = new Dictionary<string, SpecialInfo>();
            foreach (SpecialInfo special in view.Specials)
            {
                specials[special.Id] = special;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pos = new CellPos(x, y);
                    CellInfo info = view.Cell(pos);
                    float left = ox + (x * cell);
                    float top = oy + ((h - 1 - y) * cell);
                    var full = new RectF(left, top, left + cell, top + cell);
                    var tile = new RectF(left + (cell * 0.06f), top + (cell * 0.06f), left + (cell * 0.94f), top + (cell * 0.94f));
                    switch (info.Kind)
                    {
                        case CellKind.Open:
                            Fill(canvas, full, PictureColor(x, y), 0f);
                            break;
                        case CellKind.Stone:
                            Fill(canvas, tile, StoneColor, cell * 0.18f);
                            break;
                        case CellKind.Special:
                            Fill(canvas, tile, Color.ParseColor("#5C804D"), cell * 0.12f);
                            if (info.SpecialId != null && specials.TryGetValue(info.SpecialId, out SpecialInfo s) && !s.Triggered)
                            {
                                Text(canvas, s.Progress + "/" + s.Total, full.CenterX(), full.CenterY(), cell * 0.32f, Color.White);
                            }

                            break;
                        default:
                            DrawTile(canvas, info, full, tile, cell);
                            break;
                    }

                    if (info.IsEntry)
                    {
                        Fill(canvas, new RectF(left + (cell * 0.2f), top + (cell * 0.84f), left + (cell * 0.8f), top + cell), EntryColor, cell * 0.08f);
                    }
                }
            }
        }

        private void DrawTile(Canvas canvas, CellInfo info, RectF full, RectF tile, float cell)
        {
            if (info.MysteryHidden || !info.Visible.HasValue)
            {
                Fill(canvas, tile, Locked, cell * 0.18f);
                Text(canvas, "?", full.CenterX(), full.CenterY(), cell * 0.5f, Color.White);
                return;
            }

            VariantId variant = info.Visible.Value;
            Fill(canvas, tile, VariantColor(variant), cell * 0.18f);
            Text(canvas, Code(variant), full.CenterX(), full.CenterY(), cell * 0.36f, Color.White);
            if (info.RemainingLayers > 1 && info.Next.HasValue)
            {
                Fill(canvas, new RectF(tile.Right - (cell * 0.32f), tile.Top, tile.Right, tile.Top + (cell * 0.32f)), VariantColor(info.Next.Value), cell * 0.1f);
            }

            if (info.KeyId != null)
            {
                Text(canvas, "🔑", tile.Left + (cell * 0.2f), tile.Top + (cell * 0.2f), cell * 0.3f, Color.White);
            }

            if (_targeting == Recovery.BloomBurst)
            {
                Hit(full, () => Run(new UseBloomBurst(variant)));
            }
        }

        private void DrawSlots(Canvas canvas, RectF area)
        {
            LevelView view = _session.View;
            var slots = new List<int>();
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                if (view.SlotStateOf(i) != SlotState.Absent)
                {
                    slots.Add(i);
                }
            }

            float gap = area.Width() * 0.02f;
            float size = Math.Min(area.Height(), (area.Width() - (gap * (slots.Count - 1))) / slots.Count);
            float x = area.CenterX() - ((size * slots.Count) + (gap * (slots.Count - 1))) / 2f;
            foreach (int slot in slots)
            {
                var rect = new RectF(x, area.CenterY() - (size / 2f), x + size, area.CenterY() + (size / 2f));
                x += size + gap;
                SlotState state = view.SlotStateOf(slot);
                if (state == SlotState.Locked)
                {
                    Fill(canvas, rect, Locked, size * 0.16f);
                    Text(canvas, "🔒", rect.CenterX(), rect.CenterY(), size * 0.4f, Color.White);
                    continue;
                }

                string? podId = view.PodInSlot(slot);
                if (podId == null)
                {
                    Fill(canvas, rect, SlotEmpty, size * 0.16f);
                    continue;
                }

                DrawPod(canvas, view.Pod(podId), rect, size, exposed: true);
                if (_targeting == Recovery.Return)
                {
                    int index = slot;
                    Hit(rect, () => Run(new UseReturn(index)));
                }
            }
        }

        private void DrawBoosters(Canvas canvas, RectF area)
        {
            var boosters = new (string Label, Recovery Recovery)[]
            {
                ("+Slot", Recovery.ExtraSlot), ("Shuffle", Recovery.Shuffle), ("Return", Recovery.Return), ("Burst", Recovery.BloomBurst),
            };
            float gap = area.Width() * 0.02f;
            float w = (area.Width() - (gap * (boosters.Length - 1))) / boosters.Length;
            for (int i = 0; i < boosters.Length; i++)
            {
                (string label, Recovery recovery) = boosters[i];
                var rect = new RectF(area.Left + (i * (w + gap)), area.Top, area.Left + (i * (w + gap)) + w, area.Bottom);
                Button(canvas, rect, label, _targeting == recovery ? Warning : Accent, () =>
                {
                    if (_targeting == recovery)
                    {
                        _targeting = null;
                        Invalidate();
                    }
                    else
                    {
                        UseRecovery(recovery);
                    }
                });
            }
        }

        private void DrawTray(Canvas canvas, RectF area)
        {
            LevelView view = _session.View;
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
                    DrawPod(canvas, view.Pod(id), rect, size, exposed);
                    Hit(rect, () =>
                    {
                        if (_targeting == null)
                        {
                            Run(new TapPod(id));
                        }
                    });
                }
            }
        }

        private void DrawPod(Canvas canvas, PodInfo pod, RectF rect, float size, bool exposed)
        {
            Color color = pod.Variant.HasValue ? VariantColor(pod.Variant.Value) : Locked;
            if (!exposed)
            {
                color = Color.Argb(110, color.R, color.G, color.B);
            }

            Fill(canvas, rect, color, size * 0.2f);
            string label = pod.Variant.HasValue ? Code(pod.Variant.Value) : "?";
            Text(canvas, label, rect.CenterX(), rect.Top + (size * 0.36f), size * 0.3f, Color.White);
            Text(canvas, pod.Remaining.ToString(), rect.CenterX(), rect.Top + (size * 0.72f), size * 0.34f, Color.White);
            if (pod.Locked)
            {
                Fill(canvas, rect, Color.Argb(150, 40, 40, 40), size * 0.2f);
                Text(canvas, "🔒", rect.CenterX(), rect.CenterY(), size * 0.42f, Color.White);
            }

            if (pod.ConnectedGroupId != null)
            {
                Text(canvas, "∞", rect.Right - (size * 0.16f), rect.Top + (size * 0.18f), size * 0.26f, EntryColor);
            }
        }

        private void DrawToast(Canvas canvas, RectF board)
        {
            if (_toast == null || SystemClock.UptimeMillis() > _toastUntil)
            {
                _toast = null;
                return;
            }

            float size = Width * 0.042f;
            var rect = new RectF(board.Left, board.Bottom - (size * 2.2f), board.Right, board.Bottom);
            Fill(canvas, rect, Color.Argb(220, 255, 255, 255), size * 0.5f);
            Text(canvas, _toast, rect.CenterX(), rect.CenterY(), size, Warning);
        }

        private void DrawOverlay(Canvas canvas)
        {
            LevelStatus status = _session.Status;
            bool won = status == LevelStatus.Won;
            bool jam = (status == LevelStatus.Jammed || status == LevelStatus.Stuck) && !_jamHidden;
            if (!won && !jam)
            {
                return;
            }

            float w = Width;
            // Restored cells of a won level show the finished picture: keep the board visible, dim only the controls.
            var shade = new RectF(0, won ? Height * 0.6f : 0, Width, Height);
            Fill(canvas, shade, Color.Argb(won ? 90 : 120, 30, 30, 35), 0f);
            Hit(new RectF(0, 0, Width, Height), () => { });
            var card = new RectF(w * 0.1f, Height * 0.62f, w * 0.9f, Height * 0.92f);
            Fill(canvas, card, Panel, w * 0.04f);
            if (won)
            {
                Text(canvas, "Restored!", card.CenterX(), card.Top + (card.Height() * 0.3f), w * 0.08f, Accent);
                Button(canvas, new RectF(card.Left + (w * 0.1f), card.Top + (card.Height() * 0.55f), card.Right - (w * 0.1f), card.Bottom - (card.Height() * 0.12f)), "Next ▶", Accent, () => LoadLevel(_level + 1));
                return;
            }

            Text(canvas, status == LevelStatus.Stuck ? "No pod can move!" : "No room left!", card.CenterX(), card.Top + (card.Height() * 0.18f), w * 0.065f, Warning);
            var options = new List<(string, Action)> { ("Restart", () => Run(new Restart())) };
            foreach (Recovery recovery in _session.EligibleRecoveries())
            {
                Recovery r = recovery;
                options.Add((RecoveryName(r), () => UseRecovery(r)));
            }

            float rowTop = card.Top + (card.Height() * 0.34f);
            float rowHeight = (card.Bottom - rowTop - (card.Height() * 0.06f)) / Math.Max(1, (options.Count + 1) / 2);
            for (int i = 0; i < options.Count; i++)
            {
                (string label, Action action) = options[i];
                float left = i % 2 == 0 ? card.Left + (w * 0.04f) : card.CenterX() + (w * 0.01f);
                float top = rowTop + ((i / 2) * rowHeight);
                Button(canvas, new RectF(left, top, left + (card.Width() / 2f) - (w * 0.05f), top + (rowHeight * 0.85f)), label, i == 0 ? TextColor : Accent, action);
            }
        }

        private static string RecoveryName(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => "+Slot",
            Recovery.Shuffle => "Shuffle",
            Recovery.Return => "Return",
            _ => "Burst",
        };

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

        private static string Code(VariantId variant) =>
            Codes.TryGetValue(variant.Key, out string? code) ? code : variant.Key.Substring(0, Math.Min(2, variant.Key.Length));

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
            Text(canvas, label, rect.CenterX(), rect.CenterY(), rect.Height() * 0.42f, Color.White);
            Hit(rect, action);
        }

        private void Hit(RectF rect, Action action) => _hits.Add((rect, action));
    }
}
