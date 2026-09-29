using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Tests.Fixtures
{
    /// <summary>
    /// Small rule-test levels drawn as ASCII art (top row first): l = Leaf, m = Moss (both Sprig/green), w = Water,
    /// d = Dew (both Drop/blue), f = Flower, v = Violet Bud (both Bloom/pink), o = Wood, '.' = open, '#' = stone. The entry is bottom-center.
    /// </summary>
    public static class RuleLevels
    {
        public static readonly (char Symbol, string RoleId, ColorGroup Group)[] Legend =
        {
            ('l', "leaf", ColorGroup.Green),
            ('m', "moss", ColorGroup.Green),
            ('w', "water", ColorGroup.BlueCyan),
            ('d', "dew", ColorGroup.BlueCyan),
            ('f', "flower", ColorGroup.PinkPurple),
            ('o', "wood", ColorGroup.BrownOrange),
            ('v', "violet", ColorGroup.PinkPurple),
        };

        public static Dictionary<string, VariantId> Mapping() => new Dictionary<string, VariantId>(StringComparer.Ordinal)
        {
            ["leaf"] = VariantId.Leaf,
            ["moss"] = VariantId.Moss,
            ["water"] = VariantId.Water,
            ["dew"] = VariantId.Dew,
            ["flower"] = VariantId.Flower,
            ["wood"] = VariantId.Wood,
            ["violet"] = VariantId.VioletBud,
        };

        public static SessionOptions Options { get; } = new SessionOptions(1, 20000);

        public static PodDef Pod(string id, VariantId variant, int count) => new PodDef(id, variant, count, false, null, null);

        /// <param name="rowsTopFirst">The board, top row first.</param>
        /// <param name="stacks">Stacks of pod ids, top first; by default one stack per pod.</param>
        public static LevelDefinition Definition(string[] rowsTopFirst, PodDef[] pods, string[][]? stacks = null, CellOverlay[]? overlays = null)
        {
            BasePicture picture = Picture(rowsTopFirst);
            stacks ??= pods.Select(p => new[] { p.Id }).ToArray();
            return TestContent.Level(
                picture,
                Mapping(),
                overlays: overlays,
                pods: pods,
                stacks: stacks.Select(s => (IReadOnlyList<string>)s).ToArray());
        }

        public static BasePicture Picture(string[] rowsTopFirst) => TestContent.Picture("rules", Legend, rowsTopFirst);

        public static LevelSession Session(string[] rowsTopFirst, PodDef[] pods, string[][]? stacks = null, CellOverlay[]? overlays = null)
        {
            LevelDefinition definition = Definition(rowsTopFirst, pods, stacks, overlays);
            return LevelSession.Load(definition, Picture(rowsTopFirst), Options);
        }

        public static CommandResult Tap(this LevelSession session, string podId) => session.Apply(new TapPod(podId));

        public static IEnumerable<TileCleared> Clears(this CommandResult result) => result.Events.OfType<TileCleared>();

        public static IEnumerable<CellPos> ClearedBy(this CommandResult result, string podId) =>
            result.Clears().Where(c => c.PodId == podId).Select(c => c.Cell);
    }
}
