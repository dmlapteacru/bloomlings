using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// Where Munchers' walkers step aside to eat their cubes (<see cref="WalkerLook.AsideMunch"/>; the owner, 2026-11-04: «отошли,
    /// съели и ушли обратно», out of the others' way, spread out, never in a heap): each walker gets a spot when it reaches its
    /// cube, a short step (<see cref="WalkerLook.AsideNear"/> to <see cref="WalkerLook.AsideFar"/> walker boxes) from where it
    /// stops, on open ground (a path cell or an eaten cell, never on a cube or off the board), off the lanes the walkers use if it
    /// can, and at least <see cref="WalkerLook.AsideSpacing"/> walker boxes from every other walker still eating aside. Walkers
    /// are placed in the order they arrive (then by id) and try the sides in turn, so a pod's walkers alternate left and right.
    /// One per level screen in each host (the playtest's <c>RoundPainter</c>, Unity's <c>RoundFx</c>); engine-free, presentation
    /// only. Points are in the walkers' cell units (<see cref="ClearWalk.Points"/>).
    /// </summary>
    public sealed class AsideSpots
    {
        // The candidates' directions from the stop, in degrees from straight back along the way (toward home): both sides first.
        private static readonly float[] Angles = { 90f, 60f, 120f, 35f, 145f };

        private readonly Dictionary<int, Spot> _spots = new Dictionary<int, Spot>();
        private readonly HashSet<int> _lanes = new HashSet<int>();
        private readonly List<RoundWalk> _pending = new List<RoundWalk>();
        private readonly List<int> _gone = new List<int>();
        private readonly HashSet<int> _shown = new HashSet<int>();

        private readonly struct Spot
        {
            public Spot(float x, float y, float until)
            {
                X = x;
                Y = y;
                Until = until;
            }

            public float X { get; }

            public float Y { get; }

            // Until when (seconds) it holds its place against the others' spacing: back on its way by then.
            public float Until { get; }
        }

        private RoundBoardLayout? _layout;
        private RoundAnimator? _animator;
        private Func<float, float, int>? _cellAt;
        private Func<int, bool>? _open;

        /// <summary>
        /// <see cref="Update(IReadOnlyList{RoundWalk}, float, float, Func{RoundWalk, IReadOnlyList{ValueTuple{float, float}}}, Func{float, float, int}, Func{int, bool})"/>
        /// on a round board: <paramref name="layout"/>'s open rows (a path cell, or a cell whose cube <paramref name="a"/> shows gone).
        /// </summary>
        public void Update(RoundAnimator a, RoundBoardLayout layout, float now, float walkerCells, Func<RoundWalk, IReadOnlyList<(float X, float Y)>> pointsOf)
        {
            _layout = layout;
            _animator = a;
            _cellAt ??= (x, y) => _layout!.CellAt(_layout.Grid.Left + (x * _layout.Cell), _layout.Grid.Top + (y * _layout.Cell));
            _open ??= cell => _layout!.KindOf(cell) == RoundCellKind.Path || (_layout.KindOf(cell) == RoundCellKind.Material && _animator!.MaterialAt(cell) < 0);
            Update(a.ShownWalkers, now, walkerCells, pointsOf, _cellAt, _open);
        }

        /// <summary>Forgets every spot (a new level, a restart).</summary>
        public void Reset() => _spots.Clear();

        /// <summary>The spot of walker <paramref name="id"/>, or null (none chosen, or none found: it eats where it stands).</summary>
        public (float X, float Y)? Of(int id) => _spots.TryGetValue(id, out Spot spot) && !float.IsNaN(spot.X) ? (spot.X, spot.Y) : ((float, float)?)null;

        /// <summary>
        /// Chooses the spots of the shown <paramref name="walkers"/> that reached their cubes by <paramref name="now"/> (seconds) and
        /// have none, and forgets those of walkers no longer shown. <paramref name="pointsOf"/> gives a walker's way,
        /// <paramref name="walkerCells"/> its size in cells, <paramref name="cellAt"/> the rules cell under a point (-1 off the open
        /// rows), and <paramref name="open"/> whether a cell is open ground now (a path, or a cell whose cube is gone).
        /// </summary>
        public void Update(IReadOnlyList<RoundWalk> walkers, float now, float walkerCells, Func<RoundWalk, IReadOnlyList<(float X, float Y)>> pointsOf,
            Func<float, float, int> cellAt, Func<int, bool> open)
        {
            _shown.Clear();
            _pending.Clear();
            foreach (RoundWalk walk in walkers)
            {
                _shown.Add(walk.Id);
                if (!_spots.ContainsKey(walk.Id) && walk.Route.Count > 0 && now >= walk.Arrive / 1_000_000f)
                {
                    _pending.Add(walk);
                }
            }

            _gone.Clear();
            foreach (int id in _spots.Keys)
            {
                if (!_shown.Contains(id))
                {
                    _gone.Add(id);
                }
            }

            foreach (int id in _gone)
            {
                _spots.Remove(id);
            }

            if (_pending.Count == 0)
            {
                return;
            }

            // The lanes: every cell a shown walker's way crosses.
            _lanes.Clear();
            foreach (RoundWalk walk in walkers)
            {
                foreach (int cell in walk.Route)
                {
                    _lanes.Add(cell);
                }
            }

            _pending.Sort((a, b) => a.Arrive != b.Arrive ? a.Arrive.CompareTo(b.Arrive) : a.Id.CompareTo(b.Id));
            foreach (RoundWalk walk in _pending)
            {
                float until = (walk.Clear / 1_000_000f) + ClearStyles.Linger + WalkerLook.AsideBackSeconds;
                (float X, float Y)? spot = Choose(walk, pointsOf(walk), walkerCells, now, cellAt, open);
                _spots[walk.Id] = spot.HasValue ? new Spot(spot.Value.X, spot.Value.Y, until) : new Spot(float.NaN, float.NaN, until);
            }
        }

        private (float X, float Y)? Choose(RoundWalk walk, IReadOnlyList<(float X, float Y)> points, float w, float now, Func<float, float, int> cellAt, Func<int, bool> open)
        {
            if (points.Count == 0)
            {
                return null;
            }

            (float sx, float sy, float dx, float dy) = ClearLook.StopOf(points, w);
            int first = walk.Id % 2 == 0 ? 1 : -1;
            float[] radii = (walk.Id / 2) % 2 == 0
                ? new[] { WalkerLook.AsideNear, (WalkerLook.AsideNear + WalkerLook.AsideFar) / 2f, WalkerLook.AsideFar }
                : new[] { (WalkerLook.AsideNear + WalkerLook.AsideFar) / 2f, WalkerLook.AsideNear, WalkerLook.AsideFar };
            (float X, float Y)? best = null;
            float bestScore = float.NegativeInfinity;
            int order = 0;
            foreach (float r in radii)
            {
                foreach (float angle in Angles)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        int sign = side == 0 ? first : -first;
                        double a = (angle * Math.PI / 180.0) * sign;

                        // Straight back along the way (toward home) is (-dx, -dy); turn it by the angle.
                        float bx = -dx;
                        float by = -dy;
                        float cx = sx + (r * w * (float)((bx * Math.Cos(a)) - (by * Math.Sin(a))));
                        float cy = sy + (r * w * (float)((bx * Math.Sin(a)) + (by * Math.Cos(a))));
                        order++;
                        if (!Clear(cx, cy, w, cellAt, open, out bool onLane))
                        {
                            continue;
                        }

                        float room = Room(cx, cy, now) / w;
                        float score = (room >= WalkerLook.AsideSpacing ? 100f : room * 50f) + (onLane ? 0f : 20f) - (order * 0.01f);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = (cx, cy);
                        }
                    }
                }

                if (bestScore >= 120f)
                {
                    // Off the lanes with room: the nearest such spot wins.
                    break;
                }
            }

            return best;
        }

        // Whether a walker standing at (x, y) is on open ground only (its feet, its sides and its head), and on a lane.
        private bool Clear(float x, float y, float w, Func<float, float, int> cellAt, Func<int, bool> open, out bool onLane)
        {
            onLane = false;
            float[,] probes = { { 0f, 0f }, { -0.3f, 0f }, { 0.3f, 0f }, { 0f, -0.45f }, { 0f, 0.3f } };
            for (int i = 0; i < probes.GetLength(0); i++)
            {
                int cell = cellAt(x + (probes[i, 0] * w), y + (probes[i, 1] * w));
                if (cell < 0 || !open(cell))
                {
                    return false;
                }

                onLane |= i == 0 && _lanes.Contains(cell);
            }

            return true;
        }

        // How far (x, y) is from the nearest spot still held.
        private float Room(float x, float y, float now)
        {
            float room = float.MaxValue;
            foreach (Spot spot in _spots.Values)
            {
                if (float.IsNaN(spot.X) || spot.Until < now)
                {
                    continue;
                }

                float d = (float)Math.Sqrt(((spot.X - x) * (spot.X - x)) + ((spot.Y - y) * (spot.Y - y)));
                room = Math.Min(room, d);
            }

            return room;
        }
    }
}
