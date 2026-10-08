using System;
using System.Collections.Generic;
using Bloomlings.Core.Simulation;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The connected pods' marks in the tray (spec 005 FR-043, the owner, 2026-10-08: "the line linking them disappears
    /// once one column moves on, and it is unclear which pods are linked"; contracts/look.md §6.17). Presentation only: the
    /// rule stays (spec 001 FR-035, a group commits together once every member is on top).
    /// <list type="bullet">
    /// <item><description>Every connected pod the tray shows carries the link badge (<see cref="UiRaster.LinkBadge"/>, a
    /// chain on a disc of its group's color) over its frame's top right corner (<see cref="BadgeBox"/>), wherever its
    /// partners stand; the bar still joins members side by side in one row.</description></item>
    /// <item><description>A column whose "+N" hides a member shows a smaller link badge at its "+N" disc
    /// (<see cref="Hidden"/>, <see cref="HiddenMark"/>), so the player knows where the partner waits.</description></item>
    /// <item><description>A tap on a connected pod on top whose partner is not (<see cref="WaitsForPartner"/>) is refused
    /// with "Its linked pod isn't on top yet" (<c>refusal.partner_buried</c>), and the partner, or the "+N" that hides it,
    /// pulses in its group's color (<see cref="Hint"/>) while the pod shakes.</description></item>
    /// </list>
    /// Engine-free; the playtest's <c>PodPainter</c> and Unity's <c>TrayView</c> draw it. Which pods wait for a partner is in
    /// <c>PodLinkRules.cs</c>, which the level tester links alone.
    /// </summary>
    public static partial class PodLinks
    {
        /// <summary>The link badge picture's side, as a share of the pod's height (its disc a little smaller than the "+N" disc).</summary>
        public const float PictureShare = 0.52f;

        /// <summary>The hidden partner's mark at a "+N" disc, as a share of the link badge.</summary>
        public const float HiddenShare = 0.74f;

        /// <summary>How long the refusal's hint lasts, in seconds of real time.</summary>
        public const float HintSeconds = 1.2f;

        /// <summary>How many times the hint pulses in <see cref="HintSeconds"/>.</summary>
        public const int HintPulses = 2;

        /// <summary>The connected groups' colors, by the groups' order (<c>state.link</c>, <c>state.link_2</c>, <c>state.link_3</c>).</summary>
        public static readonly Rgba[] Palette = { C.StateLink, C.StateLink2, C.StateLink3 };

        /// <summary>The color of a connected group (<see cref="Palette"/> by its place in <see cref="Groups"/>).</summary>
        public static Rgba ColorOf(LevelView view, string group) => Palette[Math.Max(0, Groups(view).IndexOf(group)) % Palette.Length];

        /// <summary>The link badge's picture box over a pod's <paramref name="frame"/>: its top right corner, as the "+N" disc takes the top left.</summary>
        public static Box BadgeBox(Box frame)
        {
            float h = frame.Height;
            float side = h * PictureShare;
            return Box.FromCenter(frame.Right - (h * 0.12f), frame.Top + (h * 0.12f), side, side);
        }

        /// <summary>
        /// The hidden partner's mark of a column: a smaller link badge at the right end of the "+N" badge of its last shown
        /// pod (<paramref name="last"/>), clear of its digits and over its ring.
        /// </summary>
        public static Box HiddenMark(PodChip last)
        {
            Box badge = last.Badge;
            float side = last.Frame.Height * PictureShare * HiddenShare;
            return Box.FromCenter(badge.CenterX + (badge.Height * 0.95f), badge.CenterY + (badge.Height * 0.12f), side, side);
        }

        /// <summary>
        /// The refusal hint's strength <paramref name="seconds"/> after the tap: <see cref="HintPulses"/> soft pulses (0 to 1
        /// and back) over <see cref="HintSeconds"/>, 0 before and after.
        /// </summary>
        public static float Hint(float seconds)
        {
            if (seconds < 0f || seconds >= HintSeconds)
            {
                return 0f;
            }

            float phase = seconds / HintSeconds * HintPulses;
            return (float)Math.Sin((phase - Math.Floor(phase)) * Math.PI);
        }
    }
}
