using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// A route of the water channel as a line through its cells' middles (<see cref="ChannelLook"/>): its points, in order
    /// from the spring, and the length along it to each. Units are the caller's (screen pixels, or cells of the grid).
    /// </summary>
    public sealed class ChannelPath
    {
        private readonly (float X, float Y)[] _points;
        private readonly float[] _at;

        public ChannelPath(IReadOnlyList<(float X, float Y)> points)
        {
            _points = new (float X, float Y)[points.Count];
            _at = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                _points[i] = points[i];
                _at[i] = i == 0 ? 0f : _at[i - 1] + Distance(points[i - 1], points[i]);
            }
        }

        /// <summary>The points, the spring first and the exit last.</summary>
        public IReadOnlyList<(float X, float Y)> Points => _points;

        /// <summary>The length along the line to point <paramref name="index"/>.</summary>
        public float LengthTo(int index) => _at[Math.Max(0, Math.Min(_at.Length - 1, index))];

        /// <summary>The whole length.</summary>
        public float Length => _at.Length == 0 ? 0f : _at[_at.Length - 1];

        /// <summary>The point <paramref name="s"/> along the line (clamped to its ends).</summary>
        public (float X, float Y) PointAt(float s)
        {
            if (_points.Length == 0)
            {
                return (0f, 0f);
            }

            int i = Segment(s);
            if (i < 0)
            {
                return _points[0];
            }

            float len = _at[i + 1] - _at[i];
            float t = len <= 0f ? 0f : Math.Max(0f, Math.Min(1f, (s - _at[i]) / len));
            return (_points[i].X + ((_points[i + 1].X - _points[i].X) * t), _points[i].Y + ((_points[i + 1].Y - _points[i].Y) * t));
        }

        /// <summary>The line's unit direction at <paramref name="s"/> along it.</summary>
        public (float X, float Y) DirectionAt(float s)
        {
            int i = Segment(s);
            if (i < 0)
            {
                return (1f, 0f);
            }

            float dx = _points[i + 1].X - _points[i].X;
            float dy = _points[i + 1].Y - _points[i].Y;
            float len = (float)Math.Sqrt((dx * dx) + (dy * dy));
            return len <= 0f ? (1f, 0f) : (dx / len, dy / len);
        }

        /// <summary>The distance from (<paramref name="x"/>, <paramref name="y"/>) to the line, and the length along it to the nearest point.</summary>
        public float Nearest(float x, float y, out float along)
        {
            along = 0f;
            if (_points.Length == 1)
            {
                return Distance((x, y), _points[0]);
            }

            float best = float.MaxValue;
            for (int i = 0; i + 1 < _points.Length; i++)
            {
                (float ax, float ay) = _points[i];
                (float bx, float by) = _points[i + 1];
                float dx = bx - ax;
                float dy = by - ay;
                float ll = (dx * dx) + (dy * dy);
                float t = ll <= 0f ? 0f : Math.Max(0f, Math.Min(1f, (((x - ax) * dx) + ((y - ay) * dy)) / ll));
                float px = ax + (t * dx) - x;
                float py = ay + (t * dy) - y;
                float d = (px * px) + (py * py);
                if (d < best)
                {
                    best = d;
                    along = _at[i] + (t * (_at[i + 1] - _at[i]));
                }
            }

            return (float)Math.Sqrt(best);
        }

        // The segment holding length s (its start point's index), or -1 for a line of one point.
        private int Segment(float s)
        {
            if (_points.Length < 2)
            {
                return -1;
            }

            for (int i = 0; i + 2 < _points.Length; i++)
            {
                if (s < _at[i + 1])
                {
                    return i;
                }
            }

            return _points.Length - 2;
        }

        private static float Distance((float X, float Y) a, (float X, float Y) b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        }
    }

    /// <summary>
    /// The geometry a stream picture of the channel is drawn from (<see cref="UiRaster.ChannelStream"/>): the route's line in
    /// cells relative to the picture's top left, the picture's size in cells, the bands of the line it owns (band <c>i</c>
    /// runs from point <c>i</c> to point <c>i + 1</c>; a pixel belongs to the band of its nearest point on the line), and the
    /// board's front face in cells. Engine-free.
    /// </summary>
    public sealed class ChannelStreamShape
    {
        internal ChannelStreamShape(ChannelPath path, float widthCells, float heightCells, int firstBand, int lastBand, float front)
        {
            Path = path;
            WidthCells = widthCells;
            HeightCells = heightCells;
            FirstBand = firstBand;
            LastBand = lastBand;
            Front = front;
        }

        public ChannelPath Path { get; }

        public float WidthCells { get; }

        public float HeightCells { get; }

        public int FirstBand { get; }

        public int LastBand { get; }

        /// <summary>The cubes' front face in cells (<see cref="BoardCubeLook.FrontShare"/>): the stream's raised edge.</summary>
        public float Front { get; }
    }

    /// <summary>What the channel shows at a moment (<see cref="ChannelLook.Pose"/>).</summary>
    public readonly struct ChannelPose
    {
        public ChannelPose(float awake, bool calling, float pulse, int chosen, float run, float sinceChosen)
        {
            Awake = awake;
            Calling = calling;
            Pulse = pulse;
            Chosen = chosen;
            Run = run;
            SinceChosen = sinceChosen;
        }

        /// <summary>How awake the spring is, 0 (still dark water) to 1 (rings and the Drop's drop over it).</summary>
        public float Awake { get; }

        /// <summary>Whether the exits ask for the choice (glowing and breathing, taking the tap).</summary>
        public bool Calling { get; }

        /// <summary>The choice's pulse, 0 to 1 (<see cref="RoundBoardLayout.BridgePulse"/>).</summary>
        public float Pulse { get; }

        /// <summary>The chosen exit, or -1.</summary>
        public int Chosen { get; }

        /// <summary>How far the water has run along the chosen route, 0 to 1 (eased).</summary>
        public float Run { get; }

        /// <summary>Seconds since the choice (negative before it).</summary>
        public float SinceChosen { get; }

        /// <summary>Exit <paramref name="exit"/>'s alpha: faint before the choice, whole while calling and once chosen, the other fading.</summary>
        public float ExitAlpha(int exit) =>
            Chosen < 0 ? (Calling ? 1f : ChannelLook.FaintAlpha)
            : Chosen == exit ? 1f
            : ChannelLook.FaintAlpha * (1f - Clamp01(SinceChosen / ChannelLook.FadeSeconds));

        /// <summary>How awake exit <paramref name="exit"/>'s small spring is.</summary>
        public float ExitAwake(int exit) => Chosen == exit || Calling ? 1f : ChannelLook.FaintAwake;

        /// <summary>Exit <paramref name="exit"/>'s splash as it opens, 0 to 1 while it plays, else 0.</summary>
        public float ExitOpen(int exit)
        {
            if (Chosen != exit)
            {
                return 0f;
            }

            float k = (SinceChosen - (ChannelLook.RunSeconds * ChannelLook.OpenAtShare)) / ChannelLook.OpenSeconds;
            return k > 0f && k < 1f ? k : 0f;
        }

        /// <summary>The dotted route's alpha of exit <paramref name="exit"/>: whole before the choice, the other route fading after it.</summary>
        public float DotsAlpha(int exit) =>
            Chosen < 0 ? 1f : Chosen == exit ? (Run < 1f ? 1f : 0f) : 1f - Clamp01(SinceChosen / ChannelLook.FadeSeconds);

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>
    /// The Drop water channel's look on a round board (rules 17 §8 «Drop Водный канал», spec 006 FR-069; the owner's look A of
    /// 2026-11-05, after <c>playtest/preview/ChannelShutterMock.cs</c>): the spring, a round well of the frame's cream stone, at
    /// the channel's start; each exit a smaller well at its cell. Before the choice both routes are dotted, the spring's water is
    /// still and dark and the exits faint in a dashed white ring; once the spring is reached (<c>channelReady</c>) the spring
    /// wakes (rings and the Drop's white drop over it) and both exits breathe in the glow ring of a buildable bridge and take
    /// the tap (the game keeps running); once one is chosen (<c>channelOpen</c>) the water runs from the spring along its route
    /// as an open stream between cream stone banks (<see cref="RunSeconds"/>), drawn over the cubes, the exit opens with a
    /// splash and the other exit and its dots fade. A walker using the chosen exit rides a bubble of its colour along the stream
    /// out and back (<see cref="Rider"/>). Laid out in screen pixels from the board's layout; engine-free; presentation only.
    /// </summary>
    public sealed class ChannelLook
    {
        /// <summary>The asset slot of the channel's pictures.</summary>
        public const string Slot = "board.channel";

        /// <summary>How long the water runs from the spring to the chosen exit (seconds).</summary>
        public const float RunSeconds = 0.9f;

        /// <summary>How long the other exit and its dots fade after the choice (seconds).</summary>
        public const float FadeSeconds = 0.6f;

        /// <summary>How long the spring takes to wake once it is reached (seconds).</summary>
        public const float AwakeSeconds = 0.6f;

        /// <summary>How long the chosen exit's splash plays (seconds), from <see cref="OpenAtShare"/> of the run.</summary>
        public const float OpenSeconds = 0.45f;

        /// <summary>When in the run the chosen exit splashes open.</summary>
        public const float OpenAtShare = 0.85f;

        /// <summary>The stream's width between its outer bank edges, in cells.</summary>
        public const float StreamCells = 1.2f;

        /// <summary>The water's share of the stream's width.</summary>
        public const float WaterShare = 0.6f;

        /// <summary>The dark outline of the banks and the water, in cells.</summary>
        public const float LineCells = 0.05f;

        /// <summary>The spring well's half width, in cells.</summary>
        public const float SpringCells = 1.12f;

        /// <summary>An exit well's half width, in cells.</summary>
        public const float ExitCells = 1.15f;

        /// <summary>A faint exit's alpha before the spring is reached.</summary>
        public const float FaintAlpha = 0.62f;

        /// <summary>How awake a faint exit's well looks.</summary>
        public const float FaintAwake = 0.35f;

        /// <summary>The glow ring's radius round an exit that can be chosen, in cells.</summary>
        public const float GlowCells = 1.6f;

        /// <summary>The dashed ring's radius round a faint exit, in cells.</summary>
        public const float DashCells = 1.5f;

        /// <summary>The spacing of the dotted routes' dots, in cells.</summary>
        public const float DotStepCells = 0.62f;

        /// <summary>A dot's size, in cells.</summary>
        public const float DotCells = 0.26f;

        /// <summary>The foam dashes' spacing along the stream, in cells.</summary>
        public const float FoamPeriodCells = 1.05f;

        /// <summary>The foam's speed along the stream, cells a second.</summary>
        public const float FoamSpeedCells = 2.4f;

        /// <summary>A foam dash's length, in cells.</summary>
        public const float FoamCells = 0.3f;

        /// <summary>A rider's bubble, in cells.</summary>
        public const float BubbleCells = 1.1f;

        /// <summary>The Drop's drop bobbing over the awake spring, in cells.</summary>
        public const float DropCells = 0.95f;

        /// <summary>An exit's touch target at the least, in cells (and never under the touch token, which hosts add).</summary>
        public const float TouchCells = 3.2f;

        /// <summary>The cache key of the spring well's picture (<see cref="UiRaster.ChannelSpring"/>).</summary>
        public static string SpringKey(bool awake, int frame) => Slot + "/spring/" + (awake ? "awake/" + frame.ToString(CultureInfo.InvariantCulture) : "still");

        /// <summary>How many frames the awake spring's rings take a loop.</summary>
        public const int RingFrames = 8;

        /// <summary>How long one loop of the rings lasts (seconds).</summary>
        public const float RingSeconds = 1.25f;

        private readonly RoundBoardLayout _board;
        private readonly RoundChannel _channel;
        private readonly ChannelPath[] _cells;
        private readonly string[] _signatures;

        private ChannelLook(RoundBoardLayout board, RoundChannel channel)
        {
            _board = board;
            _channel = channel;
            _cells = new ChannelPath[channel.Exits.Count];
            _signatures = new string[channel.Exits.Count];
            for (int e = 0; e < channel.Exits.Count; e++)
            {
                var points = new List<(float X, float Y)>();
                var moves = new StringBuilder();
                IReadOnlyList<int> route = channel.Exits[e].Route;
                for (int i = 0; i < route.Count; i++)
                {
                    Box box = board.CellBox(route[i]);
                    points.Add(((box.CenterX - board.Grid.Left) / board.Cell, (box.CenterY - board.Grid.Top) / board.Cell));
                    if (i > 0)
                    {
                        int d = route[i] - route[i - 1];
                        moves.Append(d == 1 ? 'R' : d == -1 ? 'L' : d == board.Width ? 'U' : 'D');
                    }
                }

                _cells[e] = new ChannelPath(points);
                _signatures[e] = string.Format(CultureInfo.InvariantCulture, "{0:0.000}/{1:0.000}/", board.Look.RowShare, board.Look.FrontShare) + moves;
            }
        }

        /// <summary>The channel's look on <paramref name="board"/>, or null for a level without one.</summary>
        public static ChannelLook? For(RoundBoardLayout board, RoundChannel? channel) => channel == null ? null : new ChannelLook(board, channel);

        public RoundChannel Channel => _channel;

        public int ExitCount => _cells.Length;

        /// <summary>A route's line in cells of the grid (its top left at 0, 0; y down), the walkers' units.</summary>
        public ChannelPath RouteCells(int exit) => _cells[exit];

        /// <summary>A screen point of a point in cells of the grid.</summary>
        public (float X, float Y) Screen((float X, float Y) cells) => (_board.Grid.Left + (cells.X * _board.Cell), _board.Grid.Top + (cells.Y * _board.Cell));

        /// <summary>The spring's middle on screen.</summary>
        public (float X, float Y) SpringPoint => WellAt(_channel.Spring);

        /// <summary>Exit <paramref name="exit"/>'s middle on screen.</summary>
        public (float X, float Y) ExitPoint(int exit) => WellAt(_channel.Exits[exit].Cell);

        private (float X, float Y) WellAt(int cell)
        {
            Box box = _board.CellBox(cell);
            return (box.CenterX, box.CenterY - (_board.Cell * 0.05f));
        }

        /// <summary>The spring well's picture box (<see cref="UiRaster.ChannelSpring"/>) at its middle, <paramref name="scale"/> of its size.</summary>
        public Box SpringBox(float scale = 1f) => WellBox(SpringPoint, _board.Cell * SpringCells * scale);

        /// <summary>Exit <paramref name="exit"/>'s well's picture box, <paramref name="scale"/> of its size.</summary>
        public Box ExitBox(int exit, float scale = 1f) => WellBox(ExitPoint(exit), _board.Cell * ExitCells * scale);

        /// <summary>
        /// A well's picture box round its middle (<paramref name="at"/>) with half width <paramref name="rx"/>: its top ring over
        /// its front face and its shadow (<see cref="UiRaster.ChannelSpring"/>'s proportions).
        /// </summary>
        public static Box WellBox((float X, float Y) at, float rx)
        {
            float ry = rx * UiRaster.WellRowShare;
            return new Box(at.X - (rx * UiRaster.WellSide), at.Y - (ry * UiRaster.WellTop), at.X + (rx * UiRaster.WellSide), at.Y + (rx * UiRaster.WellFront) + (ry * UiRaster.WellBottom));
        }

        /// <summary>The spring's calling ring (an oval round its well), swelling with <paramref name="pulse"/>.</summary>
        public Box SpringRing(float pulse)
        {
            (float x, float y) = SpringPoint;
            float rx = _board.Cell * SpringCells * (1.12f + (0.06f * pulse));
            float ry = ((rx * UiRaster.WellRowShare) + (_board.Cell * SpringCells * UiRaster.WellFront * 0.5f)) * 1f;
            return Box.FromCenter(x, y + (_board.Cell * SpringCells * UiRaster.WellFront * 0.5f), rx * 2f, ry * 2f);
        }

        /// <summary>Exit <paramref name="exit"/>'s glow ring while it can be chosen, swelling with <paramref name="pulse"/>.</summary>
        public Box GlowRing(int exit, float pulse)
        {
            (float x, float y) = ExitPoint(exit);
            float r = _board.Cell * GlowCells * (1f + (0.05f * pulse));
            return Box.FromCenter(x, y, r * 2f, r * 2f);
        }

        /// <summary>The glow ring's stroke, in pixels.</summary>
        public float GlowStroke => Math.Max(2f, _board.Cell * RoundBoardLayout.BridgeRingStroke * 1.4f);

        /// <summary>Exit <paramref name="exit"/>'s dashed ring while faint.</summary>
        public Box DashRing(int exit)
        {
            (float x, float y) = ExitPoint(exit);
            float r = _board.Cell * DashCells;
            return Box.FromCenter(x, y, r * 2f, r * 2f);
        }

        /// <summary>Exit <paramref name="exit"/>'s touch target: <see cref="TouchCells"/> square round it, at least <paramref name="minPixels"/> a side.</summary>
        public Box Touch(int exit, float minPixels)
        {
            (float x, float y) = ExitPoint(exit);
            float side = Math.Max(minPixels, _board.Cell * TouchCells);
            return Box.FromCenter(x, y, side, side);
        }

        /// <summary>The exit whose touch target holds the screen point (<paramref name="x"/>, <paramref name="y"/>), the nearer first, or -1.</summary>
        public int ExitAt(float x, float y, float minPixels)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int e = 0; e < _cells.Length; e++)
            {
                Box t = Touch(e, minPixels);
                if (!t.Contains(x, y))
                {
                    continue;
                }

                float d = Math.Abs(x - t.CenterX) + Math.Abs(y - t.CenterY);
                if (d < bestD)
                {
                    bestD = d;
                    best = e;
                }
            }

            return best;
        }

        /// <summary>The awake spring's ring frame at <paramref name="seconds"/>.</summary>
        public static int RingFrame(float seconds)
        {
            float t = seconds / RingSeconds;
            int f = (int)Math.Floor((t - Math.Floor(t)) * RingFrames);
            return Math.Max(0, Math.Min(RingFrames - 1, f));
        }

        /// <summary>The Drop's drop over the awake spring at <paramref name="seconds"/> (bobbing).</summary>
        public Box Drop(float seconds)
        {
            (float x, float y) = SpringPoint;
            float cell = _board.Cell;
            float ry = cell * SpringCells * UiRaster.WellRowShare;
            float bob = (float)Math.Sin(seconds * 3.0) * cell * 0.12f;
            float d = cell * DropCells;
            return Box.FromCenter(x, y - ry - (d * 0.55f) + bob, d * 0.8f, d);
        }

        /// <summary>A small exit's drop (the chosen or calling exit's sign), at <paramref name="seconds"/>.</summary>
        public Box ExitDrop(int exit, float seconds, float scale = 1f)
        {
            (float x, float y) = ExitPoint(exit);
            float cell = _board.Cell;
            float ry = cell * ExitCells * UiRaster.WellRowShare * scale;
            float bob = (float)Math.Sin((seconds * 3.0) + 1.3 + exit) * cell * 0.1f;
            float d = cell * DropCells * 0.8f;
            return Box.FromCenter(x, y - ry - (d * 0.55f) + bob, d * 0.8f, d);
        }

        /// <summary>The dots of route <paramref name="exit"/> (screen points), leaving out the stretch under the wells.</summary>
        public void Dots(int exit, List<(float X, float Y)> into)
        {
            into.Clear();
            ChannelPath path = _cells[exit];
            float end = path.Length - 0.9f;
            for (float s = 0.9f; s <= end; s += DotStepCells)
            {
                into.Add(Screen(path.PointAt(s)));
            }
        }

        /// <summary>A dot's picture box round a screen point.</summary>
        public Box DotBox((float X, float Y) at) => Box.FromCenter(at.X, at.Y, _board.Cell * DotCells, _board.Cell * DotCells);

        /// <summary>
        /// The foam dashes running on the water of route <paramref name="exit"/> at <paramref name="seconds"/>, up to
        /// <paramref name="grown"/> of its length: each a box and whether it lies across the screen (else upright).
        /// </summary>
        public void Foam(int exit, float seconds, float grown, List<(Box Box, bool Across)> into)
        {
            into.Clear();
            ChannelPath path = _cells[exit];
            float limit = path.Length * Math.Max(0f, Math.Min(1f, grown));
            float period = FoamPeriodCells;
            float phase = period - (float)((seconds * FoamSpeedCells) % period);
            float thick = Math.Max(1.5f / Math.Max(1f, _board.Cell), 0.07f);
            for (float s = phase - period; s < limit; s += period)
            {
                float a = Math.Max(0f, s);
                float b = Math.Min(limit, s + FoamCells);
                if (b - a < 0.05f)
                {
                    continue;
                }

                float mid = (a + b) / 2f;
                (float dx, float dy) = path.DirectionAt(mid);
                bool across = Math.Abs(dx) >= Math.Abs(dy);
                (float x, float y) = Screen(path.PointAt(mid));
                float len = (b - a) * _board.Cell;
                float th = thick * _board.Cell;
                into.Add((across ? Box.FromCenter(x, y, len, th) : Box.FromCenter(x, y, th, len), across));
            }
        }

        /// <summary>The water's running head on route <paramref name="exit"/> at <paramref name="run"/> of it (screen box).</summary>
        public Box Head(int exit, float run)
        {
            (float x, float y) = Screen(_cells[exit].PointAt(_cells[exit].Length * run));
            float d = _board.Cell * StreamCells * WaterShare * 1.2f;
            return Box.FromCenter(x, y, d, d);
        }

        /// <summary>The splash drops of an opening exit at <paramref name="open"/> (0 to 1): screen boxes.</summary>
        public void Splash(int exit, float open, List<Box> into)
        {
            into.Clear();
            if (open <= 0f || open >= 1f)
            {
                return;
            }

            (float cx, float cy) = ExitPoint(exit);
            float cell = _board.Cell;
            for (int i = 0; i < 10; i++)
            {
                double a = (i * Math.PI * 2.0 / 10.0) + 0.3;
                float d = cell * (0.4f + (1.6f * open));
                float y = -cell * 1.2f * open * (1f - open) * 2f;
                float r = cell * 0.32f * (1f - open);
                into.Add(Box.FromCenter(cx + (float)(Math.Cos(a) * d), cy + ((float)Math.Sin(a) * d * 0.6f) + y, r, r));
            }
        }

        /// <summary>An exit's breath while it can be chosen (its well's scale), and its pop as it opens.</summary>
        public static float ExitScale(ChannelPose pose, int exit)
        {
            float breath = pose.Calling ? 1f + (RoundBoardLayout.BridgeBreath * 1.5f * pose.Pulse) : 1f;
            float open = pose.ExitOpen(exit);
            float pop = open > 0f ? 1f + (0.15f * (float)Math.Sin(open * Math.PI)) : 1f;
            return breath * pop;
        }

        /// <summary>How many bands route <paramref name="exit"/>'s stream has (one between each two cells).</summary>
        public int Bands(int exit) => Math.Max(1, _cells[exit].Points.Count - 1);

        /// <summary>
        /// The bands of route <paramref name="exit"/> shown while the water runs (<paramref name="run"/> 0 to 1): those whose
        /// middle the water has passed, so the stream never runs far ahead of its head.
        /// </summary>
        public int GrownBands(int exit, float run)
        {
            ChannelPath path = _cells[exit];
            float at = path.Length * run;
            int n = 0;
            for (int i = 0; i < Bands(exit); i++)
            {
                if (at >= (path.LengthTo(i) + path.LengthTo(i + 1)) / 2f)
                {
                    n = i + 1;
                }
            }

            return n;
        }

        /// <summary>
        /// The stream picture of route <paramref name="exit"/>'s bands <paramref name="first"/> to <paramref name="last"/>: its
        /// screen box, its cache key and its renderer (<see cref="UiRaster.ChannelStream"/>). The whole route is bands 0 to
        /// <see cref="Bands"/> − 1; a band alone is a piece of the growing stream.
        /// </summary>
        public (Box Box, string Key, Func<int, int, byte[]> Render) Stream(int exit, int first, int last)
        {
            ChannelPath path = _cells[exit];
            float front = _board.Look.FrontShare;
            float side = (StreamCells / 2f) + LineCells + 0.15f;
            float x0 = float.MaxValue;
            float y0 = float.MaxValue;
            float x1 = float.MinValue;
            float y1 = float.MinValue;
            for (int i = first; i <= last + 1 && i < path.Points.Count; i++)
            {
                (float x, float y) = path.Points[i];
                x0 = Math.Min(x0, x);
                y0 = Math.Min(y0, y);
                x1 = Math.Max(x1, x);
                y1 = Math.Max(y1, y);
            }

            x0 -= side;
            y0 -= side;
            x1 += side + 0.15f;
            y1 += side + front + 0.9f;
            var local = new List<(float X, float Y)>(path.Points.Count);
            foreach ((float x, float y) in path.Points)
            {
                local.Add((x - x0, y - y0));
            }

            var shape = new ChannelStreamShape(new ChannelPath(local), x1 - x0, y1 - y0, first, last, front);
            (float sx, float sy) = Screen((x0, y0));
            var box = new Box(sx, sy, sx + ((x1 - x0) * _board.Cell), sy + ((y1 - y0) * _board.Cell));
            string key = Slot + "/stream/" + _signatures[exit] + "/" + first.ToString(CultureInfo.InvariantCulture) + "-" + last.ToString(CultureInfo.InvariantCulture);
            return (box, key, (w, h) => UiRaster.ChannelStream(w, h, shape));
        }

        /// <summary>
        /// The channel's look at the animator's display time (<paramref name="a"/>): how awake the spring is, whether the exits
        /// call, the chosen exit, how far the water has run and the seconds since the choice.
        /// </summary>
        public static ChannelPose Pose(RoundAnimator a)
        {
            float seconds = a.Now / 1_000_000f;
            float ready = a.SecondsSince(a.ChannelReadyAt);
            float awake = ready < 0f ? 0f : Math.Min(1f, ready / AwakeSeconds);
            int chosen = a.ChannelExit;
            float since = chosen >= 0 ? a.SecondsSince(a.ChannelOpenAt) : -1f;
            float run = chosen >= 0 ? Smooth(Math.Min(1f, Math.Max(0f, since / RunSeconds))) : 0f;
            bool calling = chosen < 0 && a.ChannelReady;
            return new ChannelPose(chosen >= 0 ? 1f : awake, calling, calling ? RoundBoardLayout.BridgePulse(seconds) : 0f, chosen, run, since);
        }

        /// <summary>
        /// Where walker <paramref name="walk"/> rides the stream at display time <paramref name="nowUs"/>, if it does: out from
        /// the spring to the exit in the <see cref="RoundWalk.RideUs"/> before its walk, back from the exit to the spring after it
        /// is home (<see cref="RoundAnimator.HomeUs"/>); its bubble's screen box (popping up at the ends).
        /// </summary>
        public bool Rider(RoundWalk walk, long nowUs, out Box bubble)
        {
            bubble = default;
            if (walk.RideUs <= 0 || walk.Route.Count == 0)
            {
                return false;
            }

            int exit = -1;
            for (int e = 0; e < _channel.Exits.Count; e++)
            {
                if (_channel.Exits[e].Cell == walk.Route[0])
                {
                    exit = e;
                }
            }

            if (exit < 0)
            {
                return false;
            }

            float k;
            long home = RoundAnimator.HomeUs(walk);
            if (nowUs >= walk.Start - walk.RideUs && nowUs < walk.Start)
            {
                k = (nowUs - (walk.Start - walk.RideUs)) / (float)walk.RideUs;
            }
            else if (nowUs >= home && nowUs < home + walk.RideUs)
            {
                k = 1f - ((nowUs - home) / (float)walk.RideUs);
            }
            else
            {
                return false;
            }

            ChannelPath path = _cells[exit];
            (float x, float y) = Screen(path.PointAt(path.Length * Smooth(k)));
            float ends = Math.Min(1f, Math.Min(k, 1f - k) / 0.12f);
            float d = _board.Cell * BubbleCells * (0.55f + (0.45f * ends));
            float bob = (float)Math.Sin((nowUs / 1_000_000.0 * 9.0) + walk.Id) * _board.Cell * 0.05f;
            bubble = Box.FromCenter(x, y - (_board.Cell * 0.12f) + bob, d, d);
            return true;
        }

        private static float Smooth(float t) => t * t * (3f - (2f * t));
    }
}
