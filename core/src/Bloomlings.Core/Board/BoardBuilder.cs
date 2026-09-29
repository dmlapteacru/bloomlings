using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Boards
{
    /// <summary>
    /// Expands a <see cref="LevelDefinition"/> and its <see cref="BasePicture"/> into a <see cref="Board"/>
    /// (data-model §1.3 "Derived board"; research R5):
    /// 1. cell(x,y) starts as grid[y][x'], where x' = mirror ? width-1-x : x;
    /// 2. the top-layer variant is mapping[role];
    /// 3. the overlays layersBelow, stone, hole, mystery and keyId are applied;
    /// 4. special objects occupy their cells.
    /// Every inconsistency throws <see cref="InvalidLevelException"/>.
    /// </summary>
    public static class BoardBuilder
    {
        /// <summary>Hard format limit for hidden layers; band limits (2 before L125, 3 after) are checked by validation.</summary>
        public const int MaxLayersBelow = 3;

        public static Board Build(LevelDefinition definition, BasePicture picture, VariantCatalog catalog)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (picture == null)
            {
                throw new ArgumentNullException(nameof(picture));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            ValidatePicture(definition, picture);
            VariantId[] roleVariant = ResolveMapping(definition, picture, catalog);

            int width = picture.Width;
            int height = picture.Height;
            EntryDef[] entries = ValidateEntries(definition, width, height);
            var board = new Board(width, height, entries);

            // 1–2: picture and mapping.
            for (int y = 0; y < height; y++)
            {
                IReadOnlyList<int> row = picture.Grid[y];
                for (int x = 0; x < width; x++)
                {
                    int sourceX = definition.Picture.Mirror == Mirror.Horizontal ? width - 1 - x : x;
                    int value = row[sourceX];
                    int index = (y * width) + x;
                    if (value == BasePicture.Empty)
                    {
                        board.SetOpen(index);
                    }
                    else if (value == BasePicture.Stone)
                    {
                        board.SetStone(index);
                    }
                    else
                    {
                        board.SetTarget(index, new[] { roleVariant[value] }, false, null);
                    }
                }
            }

            // 3: overlays.
            ApplyOverlays(definition, catalog, board);

            // 4: special objects.
            ApplySpecials(definition, board);

            // Entries must lead into a walkable or target cell.
            foreach (EntryDef entry in entries)
            {
                CellKind kind = board.KindAt(board.IndexOf(entry.Cell));
                if (kind == CellKind.Stone || kind == CellKind.Special)
                {
                    throw new InvalidLevelException($"Garden Entry at {entry.Cell} is blocked by a {kind} cell.");
                }
            }

            return board;
        }

        private static void ValidatePicture(LevelDefinition definition, BasePicture picture)
        {
            if (!string.Equals(picture.Id, definition.Picture.Id, StringComparison.Ordinal) || picture.Version != definition.Picture.Version)
            {
                throw new InvalidLevelException(
                    $"Level {definition.LevelNumber} references picture {definition.Picture.Id} v{definition.Picture.Version}, got {picture.Id} v{picture.Version}.");
            }

            if (picture.Review.Status != ReviewStatus.Approved)
            {
                throw new InvalidLevelException($"Picture {picture.Id} is not approved (FR-084).");
            }

            if (picture.Width < BasePicture.MinWidth || picture.Width > BasePicture.MaxWidth
                || picture.Height < BasePicture.MinHeight || picture.Height > BasePicture.MaxHeight)
            {
                throw new InvalidLevelException(
                    $"Picture {picture.Id} is {picture.Width}×{picture.Height}; allowed is 7 ≤ width ≤ 14, 8 ≤ height ≤ 16 (FR-008).");
            }

            if (picture.Roles.Count < 2)
            {
                throw new InvalidLevelException($"Picture {picture.Id} needs at least 2 roles.");
            }

            var roleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PictureRole role in picture.Roles)
            {
                if (!roleIds.Add(role.RoleId))
                {
                    throw new InvalidLevelException($"Picture {picture.Id} has duplicate role '{role.RoleId}'.");
                }
            }

            if (picture.Grid.Count != picture.Height)
            {
                throw new InvalidLevelException($"Picture {picture.Id} has {picture.Grid.Count} rows, expected {picture.Height}.");
            }

            for (int y = 0; y < picture.Height; y++)
            {
                IReadOnlyList<int> row = picture.Grid[y];
                if (row.Count != picture.Width)
                {
                    throw new InvalidLevelException($"Picture {picture.Id} row {y} has {row.Count} cells, expected {picture.Width}.");
                }

                foreach (int value in row)
                {
                    if (value < BasePicture.Stone || value >= picture.Roles.Count)
                    {
                        throw new InvalidLevelException($"Picture {picture.Id} row {y} has invalid cell value {value}.");
                    }
                }
            }
        }

        private static VariantId[] ResolveMapping(LevelDefinition definition, BasePicture picture, VariantCatalog catalog)
        {
            var byRole = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < picture.Roles.Count; i++)
            {
                byRole.Add(picture.Roles[i].RoleId, i);
            }

            foreach (string roleId in definition.Mapping.Keys)
            {
                if (!byRole.ContainsKey(roleId))
                {
                    throw new InvalidLevelException($"Mapping names role '{roleId}', which picture {picture.Id} does not have.");
                }
            }

            var used = new bool[picture.Roles.Count];
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int value in row)
                {
                    if (value >= 0)
                    {
                        used[value] = true;
                    }
                }
            }

            var result = new VariantId[picture.Roles.Count];
            for (int i = 0; i < picture.Roles.Count; i++)
            {
                PictureRole role = picture.Roles[i];
                if (!definition.Mapping.TryGetValue(role.RoleId, out VariantId variant))
                {
                    if (used[i])
                    {
                        throw new InvalidLevelException($"Role '{role.RoleId}' is used by the picture but not mapped to a variant.");
                    }

                    continue;
                }

                if (!catalog.TryGet(variant, out VariantInfo info))
                {
                    throw new InvalidLevelException($"Role '{role.RoleId}' maps to unknown variant '{variant}'.");
                }

                if (info.ColorGroup != role.ColorGroup)
                {
                    throw new InvalidLevelException(
                        $"Role '{role.RoleId}' ({role.ColorGroup}) cannot map to '{variant}' ({info.ColorGroup}): color groups must match (FR-006).");
                }

                result[i] = variant;
            }

            return result;
        }

        private static EntryDef[] ValidateEntries(LevelDefinition definition, int width, int height)
        {
            if (definition.Entries.Count < 1)
            {
                throw new InvalidLevelException("A level needs at least 1 Garden Entry (FR-009).");
            }

            var seen = new HashSet<int>();
            var entries = new EntryDef[definition.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                EntryDef entry = definition.Entries[i];
                CellPos c = entry.Cell;
                if (c.X >= width || c.Y >= height)
                {
                    throw new InvalidLevelException($"Garden Entry {c} lies outside the {width}×{height} board.");
                }

                bool onEdge = entry.Side switch
                {
                    EntrySide.Bottom => c.Y == 0,
                    EntrySide.Top => c.Y == height - 1,
                    EntrySide.Left => c.X == 0,
                    EntrySide.Right => c.X == width - 1,
                    _ => false,
                };
                if (!onEdge)
                {
                    throw new InvalidLevelException($"Garden Entry {c} is not on the {entry.Side} edge.");
                }

                if (!seen.Add(c.ToIndex(width)))
                {
                    throw new InvalidLevelException($"Duplicate Garden Entry at {c}.");
                }

                entries[i] = entry;
            }

            return entries;
        }

        private static void ApplyOverlays(LevelDefinition definition, VariantCatalog catalog, Board board)
        {
            var seen = new HashSet<int>();
            foreach (CellOverlay overlay in definition.Overlays)
            {
                CellPos c = overlay.Cell;
                if (c.X >= board.Width || c.Y >= board.Height)
                {
                    throw new InvalidLevelException($"Overlay {c} lies outside the board.");
                }

                int index = board.IndexOf(c);
                if (!seen.Add(index))
                {
                    throw new InvalidLevelException($"Duplicate overlay at {c}.");
                }

                bool hasTargetExtras = overlay.LayersBelow.Count > 0 || overlay.Mystery || overlay.KeyId != null;
                if (overlay.Stone && overlay.Hole)
                {
                    throw new InvalidLevelException($"Overlay {c} cannot be both stone and hole.");
                }

                if ((overlay.Stone || overlay.Hole) && hasTargetExtras)
                {
                    throw new InvalidLevelException($"Overlay {c}: stone or hole cannot carry layers, mystery or a key.");
                }

                if (overlay.Stone)
                {
                    board.SetStone(index);
                    continue;
                }

                if (overlay.Hole)
                {
                    board.SetOpen(index);
                    continue;
                }

                if (!board.IsTarget(index))
                {
                    throw new InvalidLevelException($"Overlay {c}: layers, mystery and keys need a target cell (FR-033, FR-036).");
                }

                if (overlay.LayersBelow.Count > MaxLayersBelow)
                {
                    throw new InvalidLevelException($"Overlay {c} has {overlay.LayersBelow.Count} hidden layers; the maximum is {MaxLayersBelow}.");
                }

                var layers = new VariantId[1 + overlay.LayersBelow.Count];
                layers[0] = board.TopLayer(index);
                for (int i = 0; i < overlay.LayersBelow.Count; i++)
                {
                    VariantId hidden = overlay.LayersBelow[i];
                    if (!catalog.Contains(hidden))
                    {
                        throw new InvalidLevelException($"Overlay {c} has unknown hidden variant '{hidden}'.");
                    }

                    layers[i + 1] = hidden;
                }

                board.SetTarget(index, layers, overlay.Mystery, overlay.KeyId);
            }
        }

        private static void ApplySpecials(LevelDefinition definition, Board board)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpecialDef special in definition.Specials)
            {
                if (!ids.Add(special.Id))
                {
                    throw new InvalidLevelException($"Duplicate special id '{special.Id}'.");
                }

                if (special.Cells.Count == 0)
                {
                    throw new InvalidLevelException($"Special '{special.Id}' has no cells.");
                }

                foreach (CellPos c in special.Cells)
                {
                    if (c.X >= board.Width || c.Y >= board.Height)
                    {
                        throw new InvalidLevelException($"Special '{special.Id}' cell {c} lies outside the board.");
                    }

                    int index = board.IndexOf(c);
                    CellKind kind = board.KindAt(index);
                    if (kind == CellKind.Target)
                    {
                        throw new InvalidLevelException($"Special '{special.Id}' cell {c} covers a target tile.");
                    }

                    if (kind == CellKind.Special)
                    {
                        throw new InvalidLevelException($"Special '{special.Id}' cell {c} overlaps special '{board.SpecialAt(index)}'.");
                    }

                    board.SetSpecial(index, special.Id);
                }
            }
        }
    }
}
