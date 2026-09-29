using System;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using UnityEngine;
using Bloomlings.Client.UI.Localization;

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
            new DemoStep(Loc.T("demo.first_tap")) { PointAt = pod, WaitForAction = true });

        public static DemoScript Siblings(VariantVisual first, VariantVisual second) => new DemoScript(
            SiblingsId,
            new DemoStep(Loc.T("demo.exact_symbol")) { SideBySide = new[] { first, second } },
            new DemoStep(Loc.T("demo.exact_symbol")) { SideBySide = new[] { first, second }, ShowIgnore = true });

        /// <summary>The demo id of a variant that joins the pool at a milestone (L45, L200).</summary>
        public static string NewVariantId(string variantKey) => "demo.variant." + variantKey;

        /// <summary>
        /// A variant from a pool expansion appears for the first time (roadmap L45, L200): it is shown beside its family's
        /// other variants with the exact-symbol reminder, once.
        /// </summary>
        public static DemoScript NewVariant(string variantKey, IReadOnlyList<VariantVisual> family) => new DemoScript(
            NewVariantId(variantKey),
            new DemoStep(Loc.T("demo.new_variant")) { SideBySide = family });

        /// <summary>
        /// A system unlock seen on Home for the first time (roadmap L10–L100): one message pointing at its button. Null
        /// for systems without a Home demo.
        /// </summary>
        public static DemoScript? HomeSystem(string unlockId, Func<RectTransform?> target) => unlockId switch
        {
            "system.leaderboard" => new DemoScript(unlockId, new DemoStep(Loc.T("demo.leaderboard")) { PointAt = target }),
            "system.store" => new DemoScript(unlockId, new DemoStep(Loc.T("demo.store")) { PointAt = target }),
            "system.wardrobe" => new DemoScript(unlockId, new DemoStep(Loc.T("demo.wardrobe")) { PointAt = target }),
            "system.daily_challenge" => new DemoScript(unlockId, new DemoStep(Loc.T("demo.daily_challenge")) { PointAt = target }),
            "system.milestone_25" => new DemoScript(unlockId, new DemoStep(Loc.T("demo.milestones")) { PointAt = target }),
            "system.theme_rotation" => new DemoScript(unlockId, new DemoStep(Loc.T("demo.themes"))),
            _ => null,
        };

        /// <summary>The systems that have a Home demo, in roadmap order.</summary>
        public static IReadOnlyList<string> HomeSystems { get; } = new[]
        {
            "system.leaderboard", "system.store", "system.milestone_25", "system.wardrobe", "system.daily_challenge", "system.theme_rotation",
        };
    }
}
