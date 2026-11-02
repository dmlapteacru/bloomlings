using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>The board's clearing styles (spec 005 FR-038): two free, five bought once with Petals.</summary>
    public enum ClearStyle
    {
        /// <summary>The tile sinks, turning, and a flower opens where it was (free).</summary>
        Blossom,

        /// <summary>The Bloomling eats the tile in three bites and waddles home full (free).</summary>
        Munchers,

        /// <summary>The tile breaks into fireflies that swarm to the slot.</summary>
        Fireflies,

        /// <summary>A soap bubble carries the tile to the slot and pops.</summary>
        Bubbles,

        /// <summary>The Bloomling pushes the tile home, tumbling a cell at a time, and it hops into the slot.</summary>
        Pushers,

        /// <summary>The tile swells and pops, its sparkles flying to the slot.</summary>
        Fireworks,

        /// <summary>A line of small Bloomlings; the tile squashes, stretches and pops into confetti.</summary>
        Parade,
    }

    /// <summary>
    /// One Bloomling's trip in legs, in timeline seconds (<see cref="ClearStyles.LegsOf"/>): out to its tile, its act
    /// there, back home (Munchers, Pushers) and the tile's last leg into the slot (Pushers, Bubbles, Fireflies,
    /// Fireworks). The rules' clear (the slot's count going down) is at the trip's end.
    /// </summary>
    public readonly struct ClearLegs
    {
        public ClearLegs(float outward, float act, float back, float fin)
        {
            Out = outward;
            Act = act;
            Back = back;
            Fin = fin;
        }

        public float Out { get; }

        public float Act { get; }

        public float Back { get; }

        public float Fin { get; }

        public float Total => Out + Act + Back + Fin;

        /// <summary>The share of the trip after which the tile is gone from its cell (out and act).</summary>
        public float GoneShare => Total <= 0f ? 1f : (Out + Act) / Total;
    }

    /// <summary>
    /// The fast-forward speed of the animation clock in both builds (spec 001 FR-069 as amended on 2026-10-06, the owner:
    /// 3×, shown as ▶▶▶ with no number and lit while on): the player's choice on the speed pill, and the speed on its own
    /// while no pod can be tapped. Animation only: never an outcome.
    /// </summary>
    public static class PlaySpeed
    {
        public const float Fast = 3f;

        public static float Of(bool fast) => fast ? Fast : 1f;
    }

    /// <summary>
    /// The clearing styles' catalog and timing (spec 005 FR-038; spec 001 research R4, amendment of 2026-10-06), shared by
    /// both builds' timelines (<c>LevelAnimator</c>, <c>TimelinePlayer</c>), the board's drawing (<see cref="ClearLook"/>)
    /// and the Store's previews (<see cref="ClearPreview"/>). Every style takes the same time for a tile
    /// (<see cref="TripSeconds"/>), so no style changes how fast a level plays. Engine-free.
    /// </summary>
    public static class ClearStyles
    {
        /// <summary>
        /// How much faster than the calm pace (1.1 s a cell) the Bloomlings walk: the reference game's walkers (the owner's
        /// video of 2026-10-08, Level 11 at 1×: about 330 px a second on its 36 px cells, 9 cells a second), so a cell takes
        /// <see cref="PerCell"/>. The walking legs below are written at the calm pace and divided by it.
        /// </summary>
        public const float WalkSpeedUp = 10f;

        /// <summary>
        /// How much faster than the calm pace (1.4 s) every style acts at its tile (the owner, 2026-10-06: 1.5 times as
        /// fast); the fixed legs below are divided by it, so each style keeps its act.
        /// </summary>
        public const float ActSpeedUp = 1.5f;

        /// <summary>Seconds a tile's trip takes for each route cell from its entry: the reference game's walking pace (<see cref="WalkSpeedUp"/>).</summary>
        public const float PerCell = 1.1f / WalkSpeedUp;

        /// <summary>Seconds every trip takes besides its cells (the act at the tile and the tile's last leg).</summary>
        public const float Base = 1.4f / ActSpeedUp;

        /// <summary>
        /// The least time between two Bloomlings of a pod leaving the same arch, a line, never a crowd: the reference game's
        /// walkers leave their slot every 0.30 s (the owner's video of 2026-10-08), so a pod clears about 3.3 tiles a second.
        /// </summary>
        public const float LineGap = 0.3f;

        /// <summary>The level from which the bought styles can be bought (the Wardrobe's unlock, with the other cosmetics).</summary>
        public const int BuyFromLevel = 40;

        /// <summary>How long a restored tile's effect lasts after the clear (the flower, the confetti, the slot's ring).</summary>
        public const float RestoreSeconds = 1f;

        /// <summary>
        /// Seconds a round level's walker keeps acting at its cube after the rules clear the cell (the owner, 2026-10-31: the
        /// clearing animations must be seen, the walk out and home keeping its pace). The rules give the act 0.1 s; the look
        /// plays it until the clear plus this, and the walk home, at the rules' own pace, starts and ends that much later.
        /// Presentation only: the rules' clear, return and every outcome stay.
        /// </summary>
        public const float LiveLinger = 0.6f;

        /// <summary>
        /// A round level's walker's size in cubes on the biggest cubes (<see cref="BigCubesAcross"/> or fewer across the board's
        /// box) and on the smallest (<see cref="SmallCubesAcross"/> or more), the same on its way and at its cube; in between it
        /// goes linearly (the owner, 2026-10-31: "1.6 when the cubes get small, 1.4 on big ones"; it was 1.17 throughout, and
        /// 0.74 before; the reference game's ants are about 1.4 cubes long in their body whatever the board).
        /// </summary>
        public const float WalkerCubesBig = 1.4f;

        /// <inheritdoc cref="WalkerCubesBig"/>
        public const float WalkerCubesSmall = 1.6f;

        /// <summary>The fewest cubes across the board's box (L1's 16 columns fill about 19): the biggest cubes.</summary>
        public const float BigCubesAcross = 19f;

        /// <summary>The most cubes across the board's box (a 48-wide board's 46 columns): the smallest cubes.</summary>
        public const float SmallCubesAcross = 46f;

        /// <summary>
        /// How much bigger a round walker is drawn on small cubes (the owner's phone report of 2026-11-02, L54: "the heroes on
        /// such small cubes should be bigger, about one and a half times"): <see cref="SmallCubeBoost"/> is this from
        /// <see cref="BoostFullCubesAcross"/> cubes across the board's box on.
        /// </summary>
        public const float SmallCubeWalkerBoost = 1.5f;

        /// <summary>
        /// Up to this many cubes across the board's box a walker keeps its size (<see cref="SmallCubeBoost"/> 1): the big cubes
        /// of the first levels (L1 about 19 across, L2 20, L3 23).
        /// </summary>
        public const float BoostFromCubesAcross = 24f;

        /// <summary>
        /// From this many cubes across the board's box on a walker is <see cref="SmallCubeWalkerBoost"/> bigger (L54's 39-wide
        /// board shows 37 across); in between the boost grows linearly.
        /// </summary>
        public const float BoostFullCubesAcross = 34f;

        /// <summary>
        /// The factor a round walker's size takes on small cubes (presentation only): 1 up to <see cref="BoostFromCubesAcross"/>
        /// cubes across the board's box, <see cref="SmallCubeWalkerBoost"/> from <see cref="BoostFullCubesAcross"/> on, linear
        /// in between. One rule for every clearing style and both builds (<see cref="WalkerCubes"/>, <c>ClearWalk.Live</c>);
        /// the Store's previews draw no live walks and keep their look.
        /// </summary>
        public static float SmallCubeBoost(float cubesAcross)
        {
            float k = Math.Max(0f, Math.Min(1f, (cubesAcross - BoostFromCubesAcross) / (BoostFullCubesAcross - BoostFromCubesAcross)));
            return 1f + ((SmallCubeWalkerBoost - 1f) * k);
        }

        /// <summary>
        /// A round walker's size in cubes when <paramref name="cubesAcross"/> cubes span the board's box: its body size
        /// (<see cref="WalkerCubesBig"/> to <see cref="WalkerCubesSmall"/>) times the small cubes' boost (<see cref="SmallCubeBoost"/>).
        /// </summary>
        public static float WalkerCubes(float cubesAcross) => WalkerBodyCubes(cubesAcross) * SmallCubeBoost(cubesAcross);

        /// <summary>
        /// A round walker's body size in cubes before the small cubes' boost: <see cref="WalkerCubesBig"/> on the biggest cubes,
        /// <see cref="WalkerCubesSmall"/> on the smallest, linear in between.
        /// </summary>
        public static float WalkerBodyCubes(float cubesAcross)
        {
            float k = Math.Max(0f, Math.Min(1f, (cubesAcross - BigCubesAcross) / (SmallCubesAcross - BigCubesAcross)));
            return WalkerCubesBig + ((WalkerCubesSmall - WalkerCubesBig) * k);
        }

        /// <summary>
        /// Seconds a round walker takes to step out of its gate and, on the way home, into it (the owner, 2026-10-31: they
        /// shot out of the gate and flew into it): it grows from <see cref="GateScale"/> and fades in as it leaves the arch,
        /// and shrinks and fades as it goes in.
        /// </summary>
        public const float GateSeconds = 0.25f;

        /// <summary>A round walker's size in the arch, as a share of its own (<see cref="GateSeconds"/>).</summary>
        public const float GateScale = 0.55f;

        /// <summary>
        /// The share of a round walker's way out it takes to reach its pace from a standstill at the gate, and of its way home
        /// to slow down to one (constant acceleration; the walk's other times stay).
        /// </summary>
        public const float GateEaseShare = 0.3f;

        /// <summary>
        /// When a round walker's look is home again (seconds of the host's clock): its act lasts to the rules' clear plus
        /// <see cref="LiveLinger"/>, then it walks home at its way out's pace (not the rules' quicker way back), so a trip
        /// looks calm end to end. Presentation only: the rules' return stays.
        /// </summary>
        public static float LiveHomeAt(float start, float arrive, float clear, float done) =>
            clear + LiveLinger + Math.Max(done - clear, arrive - start);

        /// <summary>The free style, every level's while no bought style is chosen (the owner, 2026-10-11: "Munchers stays the main one").</summary>
        public const ClearStyle Default = ClearStyle.Munchers;

        /// <summary>The free styles: Munchers alone since 2026-10-11 (Blossom went to the Store).</summary>
        public static IReadOnlyList<ClearStyle> Free { get; } = new[] { Default };

        /// <summary>The bought styles, in the Store's order (Blossom first since the owner moved it there on 2026-10-11).</summary>
        public static IReadOnlyList<ClearStyle> Bought { get; } = new[]
        {
            ClearStyle.Blossom, ClearStyle.Fireflies, ClearStyle.Bubbles, ClearStyle.Pushers, ClearStyle.Fireworks, ClearStyle.Parade,
        };

        public static bool IsFree(ClearStyle style) => style == Default;

        /// <summary>The style's id: its save id for a bought one (<c>clear.fireflies</c>), its name key's tail for all.</summary>
        public static string Id(ClearStyle style) => "clear." + Key(style);

        /// <summary>The style's name in the strings table (<c>clearing.fireflies</c>).</summary>
        public static string NameKey(ClearStyle style) => "clearing." + Key(style);

        /// <summary>The style an id names, or null.</summary>
        public static ClearStyle? Parse(string? id)
        {
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)))
            {
                if (id == Id(style))
                {
                    return style;
                }
            }

            return null;
        }

        /// <summary>
        /// The style a level plays: the chosen bought style when there is one, else Munchers on every level (the owner,
        /// 2026-10-11; before, Blossom and Munchers alternated by level). <paramref name="level"/> stays for the callers: a
        /// rule by level may come back.
        /// </summary>
        public static ClearStyle ForLevel(int level, ClearStyle? chosen = null) =>
            chosen.HasValue && !IsFree(chosen.Value) ? chosen.Value : Default;

        /// <summary>A trip of <paramref name="cells"/> route cells, in every style: <see cref="PerCell"/> a cell plus <see cref="Base"/>.</summary>
        public static float TripSeconds(int cells) => (Math.Max(1, cells) * PerCell) + Base;

        /// <summary>
        /// How a style spends a trip of <paramref name="cells"/> route cells; the legs always add up to
        /// <see cref="TripSeconds"/>. Each leg is written at the calm pace as a share of 1.1 s a cell plus fixed seconds:
        /// the cells' part is divided by <see cref="WalkSpeedUp"/>, the fixed part by <see cref="ActSpeedUp"/>.
        /// </summary>
        public static ClearLegs LegsOf(ClearStyle style, int cells)
        {
            float n = Math.Max(1, cells);
            switch (style)
            {
                case ClearStyle.Munchers:
                    return new ClearLegs(Leg(0.5f, 0f, n), Leg(0f, 1.1f, n), Leg(0.6f, 0f, n), Leg(0f, 0.3f, n));
                case ClearStyle.Fireflies:
                    return new ClearLegs(Leg(0.9f, 0f, n), Leg(0f, 0.4f, n), 0f, Leg(0.2f, 1f, n));
                case ClearStyle.Bubbles:
                    return new ClearLegs(Leg(0.9f, 0f, n), Leg(0f, 0.5f, n), 0f, Leg(0.2f, 0.9f, n));
                case ClearStyle.Pushers:
                    return new ClearLegs(Leg(0.45f, 0f, n), Leg(0f, 0.4f, n), Leg(0.65f, 0f, n), Leg(0f, 1f, n));
                case ClearStyle.Fireworks:
                    return new ClearLegs(Leg(1.1f, 0f, n), Leg(0f, 0.5f, n), 0f, Leg(0f, 0.9f, n));
                default:
                    // Blossom and the Parade: out at the trip's pace, then the act at the tile.
                    return new ClearLegs(Leg(1.1f, 0f, n), Leg(0f, 1.4f, n), 0f, 0f);
            }
        }

        /// <summary>A leg of <paramref name="perCell"/> calm seconds a cell over <paramref name="n"/> cells plus <paramref name="fixedSeconds"/>.</summary>
        private static float Leg(float perCell, float fixedSeconds, float n) => (perCell * n / WalkSpeedUp) + (fixedSeconds / ActSpeedUp);

        private static string Key(ClearStyle style) => style switch
        {
            ClearStyle.Blossom => "blossom",
            ClearStyle.Munchers => "munchers",
            ClearStyle.Fireflies => "fireflies",
            ClearStyle.Bubbles => "bubbles",
            ClearStyle.Pushers => "pushers",
            ClearStyle.Fireworks => "fireworks",
            _ => "parade",
        };
    }
}
