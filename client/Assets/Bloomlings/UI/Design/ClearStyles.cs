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
        /// How much faster than the calm pace (1.1 s a cell plus 1.4 s, 0.42 s apart) every trip and line plays (the
        /// owner, 2026-10-06: each cell clears 1.5 times as fast). The legs below are written at the calm pace and divided
        /// by it, so the walkers keep their spacing and every style its proportions.
        /// </summary>
        public const float SpeedUp = 1.5f;

        /// <summary>Seconds a tile's trip takes for each route cell from its entry (the owner, 2026-10-06: "in between", then 1.5 times as fast).</summary>
        public const float PerCell = 1.1f / SpeedUp;

        /// <summary>Seconds every trip takes besides its cells (the act at the tile and the tile's last leg).</summary>
        public const float Base = 1.4f / SpeedUp;

        /// <summary>The least time between two Bloomlings leaving the same arch: a line, never a crowd.</summary>
        public const float LineGap = 0.42f / SpeedUp;

        /// <summary>The levels of the onboarding, which always play the plainer free style (Blossom).</summary>
        public const int OnboardingLevels = 10;

        /// <summary>The level from which the bought styles can be bought (the Wardrobe's unlock, with the other cosmetics).</summary>
        public const int BuyFromLevel = 40;

        /// <summary>How long a restored tile's effect lasts after the clear (the flower, the confetti, the slot's ring).</summary>
        public const float RestoreSeconds = 1f;

        /// <summary>The free pair, in the order the levels play them.</summary>
        public static IReadOnlyList<ClearStyle> Free { get; } = new[] { ClearStyle.Blossom, ClearStyle.Munchers };

        /// <summary>The bought styles, in the Store's order.</summary>
        public static IReadOnlyList<ClearStyle> Bought { get; } = new[]
        {
            ClearStyle.Fireflies, ClearStyle.Bubbles, ClearStyle.Pushers, ClearStyle.Fireworks, ClearStyle.Parade,
        };

        public static bool IsFree(ClearStyle style) => style == ClearStyle.Blossom || style == ClearStyle.Munchers;

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
        /// The style a level plays: the chosen bought style when there is one, else the free pair by level, Blossom on
        /// the onboarding's levels and odd levels, Munchers on even levels after it (the owner, 2026-10-06).
        /// </summary>
        public static ClearStyle ForLevel(int level, ClearStyle? chosen = null)
        {
            if (chosen.HasValue && !IsFree(chosen.Value))
            {
                return chosen.Value;
            }

            return level > OnboardingLevels && level % 2 == 0 ? ClearStyle.Munchers : ClearStyle.Blossom;
        }

        /// <summary>A trip of <paramref name="cells"/> route cells, in every style: <see cref="PerCell"/> a cell plus <see cref="Base"/>.</summary>
        public static float TripSeconds(int cells) => (Math.Max(1, cells) * PerCell) + Base;

        /// <summary>
        /// How a style spends a trip of <paramref name="cells"/> route cells; the legs always add up to
        /// <see cref="TripSeconds"/>. They are written at the calm pace and divided by <see cref="SpeedUp"/>.
        /// </summary>
        public static ClearLegs LegsOf(ClearStyle style, int cells)
        {
            float n = Math.Max(1, cells);
            switch (style)
            {
                case ClearStyle.Munchers:
                    return Paced(0.5f * n, 1.1f, 0.6f * n, 0.3f);
                case ClearStyle.Fireflies:
                    return Paced(0.9f * n, 0.4f, 0f, (0.2f * n) + 1f);
                case ClearStyle.Bubbles:
                    return Paced(0.9f * n, 0.5f, 0f, (0.2f * n) + 0.9f);
                case ClearStyle.Pushers:
                    return Paced(0.45f * n, 0.4f, 0.65f * n, 1f);
                case ClearStyle.Fireworks:
                    return Paced(1.1f * n, 0.5f, 0f, 0.9f);
                default:
                    // Blossom and the Parade: out at the trip's pace, then the act at the tile.
                    return Paced(1.1f * n, 1.4f, 0f, 0f);
            }
        }

        private static ClearLegs Paced(float outward, float act, float back, float fin) =>
            new ClearLegs(outward / SpeedUp, act / SpeedUp, back / SpeedUp, fin / SpeedUp);

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
