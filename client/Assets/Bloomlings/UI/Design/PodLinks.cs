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
    /// Engine-free; the playtest's <c>PodPainter</c> and Unity's <c>TrayView</c> draw it.
    /// </summary>
    public static class PodLinks
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

        /// <summary>The level's connected groups, sorted by id: a group's place picks its color.</summary>
        public static List<string> Groups(LevelView view)
        {
            var groups = new List<string>();
            foreach (string id in view.PodIds)
            {
                string? group = view.Pod(id).ConnectedGroupId;
                if (group != null && !groups.Contains(group))
                {
                    groups.Add(group);
                }
            }

            groups.Sort(StringComparer.Ordinal);
            return groups;
        }

        /// <summary>The color of a connected group (<see cref="Palette"/> by its place in <see cref="Groups"/>).</summary>
        public static Rgba ColorOf(LevelView view, string group) => Palette[Math.Max(0, Groups(view).IndexOf(group)) % Palette.Length];

        /// <summary>
        /// The members of the pod's connected group that are not on top of their stacks, the partners a tap on it waits for;
        /// empty when the pod is not connected or every member is on top.
        /// </summary>
        public static IReadOnlyList<string> Buried(LevelView view, string podId)
        {
            var buried = new List<string>();
            if (view.Pod(podId).ConnectedGroupId == null)
            {
                return buried;
            }

            foreach (string member in view.ConnectedGroup(podId))
            {
                if (member != podId && !view.IsExposed(member))
                {
                    buried.Add(member);
                }
            }

            return buried;
        }

        /// <summary>Whether a tap on the pod waits for a partner: the pod is on top and connected, and a member is not on top.</summary>
        public static bool WaitsForPartner(LevelView view, string podId) => view.IsExposed(podId) && Buried(view, podId).Count > 0;

        /// <summary>Where a pod stands in the tray: its stack and depth (0 on top), or null when it is not in a stack.</summary>
        public static (int Stack, int Depth)? Place(LevelView view, string podId)
        {
            for (int stack = 0; stack < view.StackCount; stack++)
            {
                IReadOnlyList<string> ids = view.Stack(stack);
                for (int depth = 0; depth < ids.Count; depth++)
                {
                    if (ids[depth] == podId)
                    {
                        return (stack, depth);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// For each stack, the connected group of a member deeper than the <paramref name="rowsShown"/> rows the tray shows
        /// (under its "+N"), or null.
        /// </summary>
        public static string?[] Hidden(LevelView view, int rowsShown)
        {
            var hidden = new string?[view.StackCount];
            for (int stack = 0; stack < hidden.Length; stack++)
            {
                IReadOnlyList<string> ids = view.Stack(stack);
                for (int depth = Math.Max(0, rowsShown); depth < ids.Count && hidden[stack] == null; depth++)
                {
                    hidden[stack] = view.Pod(ids[depth]).ConnectedGroupId;
                }
            }

            return hidden;
        }

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
