using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Tests.Fixtures
{
    /// <summary>Builds pictures and level definitions from ASCII art for tests.</summary>
    public static class TestContent
    {
        /// <summary>
        /// Builds an approved picture. <paramref name="rowsTopFirst"/> are written top row first, like the picture looks;
        /// '.' = empty, '#' = stone, any other character is looked up in <paramref name="legend"/>.
        /// </summary>
        public static BasePicture Picture(
            string id,
            IReadOnlyList<(char Symbol, string RoleId, ColorGroup Group)> legend,
            params string[] rowsTopFirst)
        {
            int height = rowsTopFirst.Length;
            int width = rowsTopFirst[0].Length;
            var roles = legend.Select(l => new PictureRole(l.RoleId, l.RoleId, l.Group, l.RoleId == "bg")).ToList();
            var grid = new List<IReadOnlyList<int>>();
            for (int y = 0; y < height; y++)
            {
                string row = rowsTopFirst[height - 1 - y];
                if (row.Length != width)
                {
                    throw new ArgumentException("All rows must have the same width.");
                }

                var cells = new List<int>();
                foreach (char c in row)
                {
                    if (c == '.')
                    {
                        cells.Add(BasePicture.Empty);
                    }
                    else if (c == '#')
                    {
                        cells.Add(BasePicture.Stone);
                    }
                    else
                    {
                        int index = legend.ToList().FindIndex(l => l.Symbol == c);
                        if (index < 0)
                        {
                            throw new ArgumentException($"Unknown symbol '{c}'.");
                        }

                        cells.Add(index);
                    }
                }

                grid.Add(cells);
            }

            return new BasePicture(
                id,
                1,
                id,
                width,
                height,
                roles,
                grid,
                new FinishedLook(FinishedLookMode.Auto, null),
                new PictureTags(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()),
                null,
                new PictureReview(ReviewStatus.Approved, "test", null, null),
                new PictureSource(PictureSourceKind.Hand, "test", "owned"));
        }

        public static LevelDefinition Level(
            BasePicture picture,
            IReadOnlyDictionary<string, VariantId> mapping,
            IReadOnlyList<EntryDef>? entries = null,
            IReadOnlyList<CellOverlay>? overlays = null,
            IReadOnlyList<SpecialDef>? specials = null,
            IReadOnlyList<PodDef>? pods = null,
            IReadOnlyList<IReadOnlyList<string>>? stacks = null,
            Mirror mirror = Mirror.None,
            int levelNumber = 1)
        {
            entries ??= new[] { new EntryDef(new CellPos(picture.Width / 2, 0), EntrySide.Bottom) };
            pods ??= new[]
            {
                new PodDef("p1", mapping.Values.First(), 1, false, null, null),
                new PodDef("p2", mapping.Values.First(), 1, false, null, null),
            };
            stacks ??= new[] { new[] { pods[0].Id }, pods.Skip(1).Select(p => p.Id).ToArray() };
            return new LevelDefinition(
                levelNumber,
                1,
                42UL,
                "test",
                new PictureRef(picture.Id, picture.Version, mirror, "default"),
                mapping,
                entries,
                overlays ?? Array.Empty<CellOverlay>(),
                specials ?? Array.Empty<SpecialDef>(),
                Array.Empty<LockDef>(),
                new SlotsDef(SlotsDef.DefaultCount, null),
                new TrayDef(stacks),
                pods,
                new DifficultyDef(DifficultyClass.Normal, 0, false),
                "test",
                Array.Empty<string>());
        }

        public static CellOverlay Overlay(
            int x,
            int y,
            VariantId[]? layersBelow = null,
            bool mystery = false,
            bool stone = false,
            bool hole = false,
            string? keyId = null)
            => new CellOverlay(new CellPos(x, y), layersBelow ?? Array.Empty<VariantId>(), mystery, stone, hole, keyId);

        /// <summary>Legend with a blue background (Water/Dew), green leaves (Leaf/Moss) and pink petals (Flower/Violet).</summary>
        public static readonly (char Symbol, string RoleId, ColorGroup Group)[] GardenLegend =
        {
            ('b', "bg", ColorGroup.BlueCyan),
            ('l', "leaf", ColorGroup.Green),
            ('p', "petal", ColorGroup.PinkPurple),
            ('w', "pot", ColorGroup.BrownOrange),
        };

        public static Dictionary<string, VariantId> GardenMapping() => new Dictionary<string, VariantId>(StringComparer.Ordinal)
        {
            ["bg"] = VariantId.Water,
            ["leaf"] = VariantId.Leaf,
            ["petal"] = VariantId.Flower,
            ["pot"] = VariantId.Wood,
        };
    }
}
