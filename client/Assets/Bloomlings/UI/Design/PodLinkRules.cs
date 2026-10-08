using System;
using System.Collections.Generic;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The connected pods' groups and partners (spec 005 FR-043), from the level's state alone: no picture, color or box,
    /// so the level tester (<c>playtest/tester</c>) links this file without the rest of the kit.
    /// </summary>
    public static partial class PodLinks
    {
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
    }
}
