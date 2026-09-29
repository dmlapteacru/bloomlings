using System;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using UnityEngine;

namespace Bloomlings.Client.UI.Tutorial
{
    /// <summary>One step of a demo: at most one short message, an optional pointer target and optional icons.</summary>
    public sealed class DemoStep
    {
        public DemoStep(string message)
        {
            Message = message;
        }

        public string Message { get; }

        /// <summary>The element the pointer hand points at, resolved when the step starts.</summary>
        public Func<RectTransform?>? PointAt { get; set; }

        /// <summary>Variants shown side by side (the sibling demo).</summary>
        public IReadOnlyList<VariantVisual>? SideBySide { get; set; }

        /// <summary>Shows the first variant's pod ignoring the second variant's tile (a crossed-out arrow).</summary>
        public bool ShowIgnore { get; set; }

        /// <summary>The step ends when the player performs the action (for example the guided first tap).</summary>
        public bool WaitForAction { get; set; }
    }

    /// <summary>A short, skippable demo, shown once (FR-031, FR-071).</summary>
    public sealed class DemoScript
    {
        public DemoScript(string demoId, params DemoStep[] steps)
        {
            DemoId = demoId;
            Steps = steps;
        }

        /// <summary>Stored in <c>unlocks.demosSeen</c> once shown.</summary>
        public string DemoId { get; }

        public IReadOnlyList<DemoStep> Steps { get; }
    }

    /// <summary>The demos of the onboarding levels (T064). Booster demos are added with US5 (T122).</summary>
    public static class DemoScripts
    {
        /// <summary>The Level 1 guided first tap; its id is the roadmap's core-play unlock.</summary>
        public const string FirstTapId = "system.core";

        /// <summary>The same-family sibling demo, shown only once (FR-071).</summary>
        public const string SiblingsId = "demo.siblings";

        public static DemoScript FirstTap(Func<RectTransform?> pod) => new DemoScript(
            FirstTapId,
            new DemoStep("Tap a pod to send its Bloomlings") { PointAt = pod, WaitForAction = true });

        public static DemoScript Siblings(VariantVisual first, VariantVisual second) => new DemoScript(
            SiblingsId,
            new DemoStep("Match the exact symbol") { SideBySide = new[] { first, second } },
            new DemoStep("Match the exact symbol") { SideBySide = new[] { first, second }, ShowIgnore = true });
    }
}
