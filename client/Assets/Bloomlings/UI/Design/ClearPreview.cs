using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The Store card's live preview of a clearing style (spec 005 FR-038, contracts/look.md §6.12): a small board of
    /// <see cref="Columns"/> × <see cref="Rows"/> tiles of one variant with an arch under its middle column and one
    /// slot below, cleared by a line of Bloomlings in the style and scheduled as the board is (the same trip time, the
    /// line gap, each walker waiting for its way), then laid again, in a loop of <see cref="Period"/> seconds. In cell
    /// units with y down: the tiles fill x 0–5, y 0–3, the arch's door is at (2.5, 3) and the slot's seat at
    /// <see cref="Seat"/>. Engine-free; the hosts draw the tiles, the ground, the arch and the slot, then the look.
    /// </summary>
    public sealed class ClearPreview
    {
        public const int Columns = 5;
        public const int Rows = 3;

        /// <summary>The pause after the last clear before the board is laid again.</summary>
        public const float Rest = 1.8f;

        private static readonly Dictionary<ClearStyle, ClearPreview> Made = new Dictionary<ClearStyle, ClearPreview>();

        private readonly List<Trip> _trips = new List<Trip>();

        public ClearPreview(ClearStyle style)
        {
            Style = style;
            Variant = VariantOf(style);
            Schedule();
            float end = 0f;
            foreach (Trip trip in _trips)
            {
                end = Math.Max(end, trip.Start + trip.Arrival);
            }

            Period = end + Rest;
        }

        public ClearStyle Style { get; }

        /// <summary>The tiles' variant (a different one for each style, so the cards differ).</summary>
        public VariantId Variant { get; }

        /// <summary>The loop's length, in seconds.</summary>
        public float Period { get; }

        /// <summary>The entry cell's column (the bottom row's middle); the arch stands below it.</summary>
        public static int EntryColumn => Columns / 2;

        /// <summary>The arch's door, where the walkers come out.</summary>
        public static (float X, float Y) Door => (EntryColumn + 0.5f, Rows);

        /// <summary>The slot's plate (below the board, left of the arch, clear of a card's badge) and the seat of its tile.</summary>
        public static Box Slot => Box.FromCenter(0.95f, Rows + 1.15f, 1.1f, 1.1f);

        public static Box Seat => ReferenceGameplayRegions.SlotTile(Slot);

        /// <summary>The whole picture's box: the tiles, the arch and the slot below them.</summary>
        public static Box Bounds => new Box(0f, 0f, Columns, Rows + 1.8f);

        /// <summary>A cell's center (column, row from the top).</summary>
        public static (float X, float Y) Center(int column, int row) => (column + 0.5f, row + 0.5f);

        /// <summary>The time in the loop at <paramref name="now"/> (any clock).</summary>
        public float Local(float now) => Period <= 0f ? 0f : now - ((float)Math.Floor(now / Period) * Period);

        /// <summary>A style's scene, made once and shared by the cards.</summary>
        public static ClearPreview Of(ClearStyle style)
        {
            lock (Made)
            {
                if (!Made.TryGetValue(style, out ClearPreview? scene))
                {
                    scene = new ClearPreview(style);
                    Made[style] = scene;
                }

                return scene;
            }
        }

        /// <summary>
        /// The scene a card shows at <paramref name="now"/> (any clock) and its loop time: the style's own loop, or with
        /// <paramref name="pair"/> the free pair's card, Blossom and Munchers by turns, a loop each.
        /// </summary>
        public static (ClearPreview Scene, float T) At(ClearStyle style, bool pair, float now)
        {
            if (!pair)
            {
                ClearPreview scene = Of(style);
                return (scene, scene.Local(now));
            }

            ClearPreview first = Of(ClearStyle.Blossom);
            ClearPreview second = Of(ClearStyle.Munchers);
            float both = first.Period + second.Period;
            float local = now - ((float)Math.Floor(now / both) * both);
            return local < first.Period ? (first, local) : (second, local - first.Period);
        }

        /// <summary>Whether the tile at (column, row) is cleared at loop time <paramref name="t"/>.</summary>
        public bool Cleared(int column, int row, float t)
        {
            foreach (Trip trip in _trips)
            {
                if (trip.Column == column && trip.Row == row)
                {
                    return t >= trip.Start + trip.Arrival;
                }
            }

            return false;
        }

        /// <summary>Whether the look holds the tile at (column, row) at loop time <paramref name="t"/> (the host leaves it off).</summary>
        public bool Held(int column, int row, float t)
        {
            foreach (Trip trip in _trips)
            {
                if (trip.Column == column && trip.Row == row)
                {
                    return ClearLook.Holds(Style, Walk(trip), t);
                }
            }

            return false;
        }

        /// <summary>The tiles still to clear, as the slot's count shows.</summary>
        public int Count(float t)
        {
            int left = _trips.Count;
            foreach (Trip trip in _trips)
            {
                left -= t >= trip.Start + trip.Arrival ? 1 : 0;
            }

            return left;
        }

        /// <summary>The look's items at loop time <paramref name="t"/>: every walker, then every just-cleared tile.</summary>
        public void Draw(FxList list, float t)
        {
            foreach (Trip trip in _trips)
            {
                ClearLook.Walker(list, Style, Walk(trip), t);
            }

            foreach (Trip trip in _trips)
            {
                ClearLook.Restore(list, Style, new ClearFade(Center(trip.Column, trip.Row), Variant, trip.Start + trip.Arrival, Seat), t);
            }
        }

        /// <summary>The tiles near a just-opened flower sway (Blossom).</summary>
        public float Sway(int column, int row, float t)
        {
            var fades = new List<ClearFade>();
            foreach (Trip trip in _trips)
            {
                fades.Add(new ClearFade(Center(trip.Column, trip.Row), Variant, trip.Start + trip.Arrival, Seat));
            }

            return ClearLook.Sway(Style, fades, Center(column, row), t);
        }

        /// <summary>The preview's tiles: a variant each style shows well on (the fireflies' gold on violet, the bubbles on dew, …).</summary>
        public static VariantId VariantOf(ClearStyle style) => style switch
        {
            ClearStyle.Blossom => VariantId.Flower,
            ClearStyle.Munchers => VariantId.Leaf,
            ClearStyle.Fireflies => VariantId.VioletBud,
            ClearStyle.Bubbles => VariantId.Dew,
            ClearStyle.Pushers => VariantId.Wood,
            ClearStyle.Fireworks => VariantId.Water,
            _ => VariantId.Acorn,
        };

        private ClearWalk Walk(Trip trip) => new ClearWalk(trip.Points, Variant, trip.Start, trip.Arrival, Seat);

        // Nearer tiles first; each walker leaves a line gap after the one before, and late enough that it reaches each
        // cell of its way after that cell's tile is gone (as the boards' timelines schedule them).
        private void Schedule()
        {
            var distance = new int[Columns, Rows];
            var from = new (int X, int Y)[Columns, Rows];
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    distance[x, y] = -1;
                }
            }

            var queue = new Queue<(int X, int Y)>();
            distance[EntryColumn, Rows - 1] = 1;
            queue.Enqueue((EntryColumn, Rows - 1));
            var order = new List<(int X, int Y)>();
            while (queue.Count > 0)
            {
                (int x, int y) = queue.Dequeue();
                order.Add((x, y));
                foreach ((int dx, int dy) in new[] { (0, -1), (-1, 0), (1, 0), (0, 1) })
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Columns && ny < Rows && distance[nx, ny] < 0)
                    {
                        distance[nx, ny] = distance[x, y] + 1;
                        from[nx, ny] = (x, y);
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            var gone = new Dictionary<(int, int), float>();
            float last = -ClearStyles.LineGap;
            foreach ((int X, int Y) cell in order)
            {
                var route = new List<(int X, int Y)> { cell };
                while (route[0] != (EntryColumn, Rows - 1))
                {
                    route.Insert(0, from[route[0].X, route[0].Y]);
                }

                int n = route.Count;
                ClearLegs legs = ClearStyles.LegsOf(Style, n);
                float go = last + ClearStyles.LineGap;
                for (int j = 0; j < n - 1; j++)
                {
                    float reach = legs.Out * (j + 1) / n;
                    if (gone.TryGetValue(route[j], out float free))
                    {
                        go = Math.Max(go, free + 0.05f - reach);
                    }
                }

                var points = new List<(float X, float Y)> { Door };
                foreach ((int x, int y) in route)
                {
                    points.Add(Center(x, y));
                }

                _trips.Add(new Trip(cell.X, cell.Y, points, go, legs.Total));
                gone[cell] = go + legs.Out + legs.Act;
                last = go;
            }
        }

        private sealed class Trip
        {
            public Trip(int column, int row, IReadOnlyList<(float X, float Y)> points, float start, float arrival)
            {
                Column = column;
                Row = row;
                Points = points;
                Start = start;
                Arrival = arrival;
            }

            public int Column { get; }

            public int Row { get; }

            public IReadOnlyList<(float X, float Y)> Points { get; }

            public float Start { get; }

            public float Arrival { get; }
        }
    }
}
