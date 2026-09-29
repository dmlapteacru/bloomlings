using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Json
{
    /// <summary>
    /// Reads and writes <c>level-definition.v1</c> documents (contracts/level-definition.schema.json).
    /// Reading enforces the schema by hand (types, ranges, enums, required and unknown properties); the rules that span
    /// several fields, such as colorGroup matching, are checked later by <c>BoardBuilder</c> and the pipeline.
    /// Writing is canonical: sorted keys, optional fields only when they differ from their default, so
    /// <c>Write(Read(x)) == x</c> for every canonical document.
    /// </summary>
    public static class DefinitionJson
    {
        public static LevelDefinition Read(string json) => Read(JsonDoc.ParseObject(json, "level"), string.Empty);

        public static string Write(LevelDefinition level, bool indented = true) =>
            CanonicalJson.Write(ToJObject(level), indented);

        internal static LevelDefinition Read(JObject root, string path)
        {
            JsonDoc.AllowOnly(
                root,
                path,
                "levelNumber",
                "definitionVersion",
                "seed",
                "generatorVersion",
                "picture",
                "mapping",
                "entries",
                "overlays",
                "specials",
                "locks",
                "slots",
                "tray",
                "pods",
                "difficulty",
                "rewardProfile",
                "mechanics");

            int levelNumber = JsonDoc.Int(JsonDoc.Required(root, path, "levelNumber"), JsonDoc.Join(path, "levelNumber"), min: 1);
            int definitionVersion = JsonDoc.Int(
                JsonDoc.Required(root, path, "definitionVersion"),
                JsonDoc.Join(path, "definitionVersion"),
                min: 1);
            ulong seed = ReadSeed(JsonDoc.Required(root, path, "seed"), JsonDoc.Join(path, "seed"));
            string generatorVersion = JsonDoc.String(
                JsonDoc.Required(root, path, "generatorVersion"),
                JsonDoc.Join(path, "generatorVersion"));

            PictureRef picture = ReadPicture(JsonDoc.Required(root, path, "picture"), JsonDoc.Join(path, "picture"));
            IReadOnlyDictionary<string, VariantId> mapping = ReadMapping(
                JsonDoc.Required(root, path, "mapping"),
                JsonDoc.Join(path, "mapping"));
            IReadOnlyList<EntryDef> entries = ReadEntries(JsonDoc.Required(root, path, "entries"), JsonDoc.Join(path, "entries"));

            JToken? overlaysToken = JsonDoc.Optional(root, "overlays");
            IReadOnlyList<CellOverlay> overlays = overlaysToken == null
                ? Array.Empty<CellOverlay>()
                : ReadOverlays(overlaysToken, JsonDoc.Join(path, "overlays"));

            JToken? specialsToken = JsonDoc.Optional(root, "specials");
            IReadOnlyList<SpecialDef> specials = specialsToken == null
                ? Array.Empty<SpecialDef>()
                : ReadSpecials(specialsToken, JsonDoc.Join(path, "specials"));

            JToken? locksToken = JsonDoc.Optional(root, "locks");
            IReadOnlyList<LockDef> locks = locksToken == null
                ? Array.Empty<LockDef>()
                : ReadLocks(locksToken, JsonDoc.Join(path, "locks"));

            SlotsDef slots = ReadSlots(JsonDoc.Required(root, path, "slots"), JsonDoc.Join(path, "slots"));
            TrayDef tray = ReadTray(JsonDoc.Required(root, path, "tray"), JsonDoc.Join(path, "tray"));
            IReadOnlyList<PodDef> pods = ReadPods(JsonDoc.Required(root, path, "pods"), JsonDoc.Join(path, "pods"));
            DifficultyDef difficulty = ReadDifficulty(
                JsonDoc.Required(root, path, "difficulty"),
                JsonDoc.Join(path, "difficulty"));
            string rewardProfile = JsonDoc.String(
                JsonDoc.Required(root, path, "rewardProfile"),
                JsonDoc.Join(path, "rewardProfile"));

            JToken? mechanicsToken = JsonDoc.Optional(root, "mechanics");
            IReadOnlyList<string> mechanics = mechanicsToken == null
                ? Array.Empty<string>()
                : ReadMechanics(mechanicsToken, JsonDoc.Join(path, "mechanics"));

            return new LevelDefinition(
                levelNumber,
                definitionVersion,
                seed,
                generatorVersion,
                picture,
                mapping,
                entries,
                overlays,
                specials,
                locks,
                slots,
                tray,
                pods,
                difficulty,
                rewardProfile,
                mechanics);
        }

        internal static JObject ToJObject(LevelDefinition level)
        {
            var root = new JObject
            {
                ["levelNumber"] = level.LevelNumber,
                ["definitionVersion"] = level.DefinitionVersion,
                ["seed"] = level.Seed.ToString(CultureInfo.InvariantCulture),
                ["generatorVersion"] = level.GeneratorVersion,
                ["picture"] = WritePicture(level.Picture),
                ["mapping"] = WriteMapping(level.Mapping),
                ["entries"] = WriteEntries(level.Entries),
                ["slots"] = WriteSlots(level.Slots),
                ["tray"] = WriteTray(level.Tray),
                ["pods"] = WritePods(level.Pods),
                ["difficulty"] = WriteDifficulty(level.Difficulty),
                ["rewardProfile"] = level.RewardProfile,
            };

            if (level.Overlays.Count > 0)
            {
                root["overlays"] = new JObject { ["cells"] = WriteOverlays(level.Overlays) };
            }

            if (level.Specials.Count > 0)
            {
                root["specials"] = WriteSpecials(level.Specials);
            }

            if (level.Locks.Count > 0)
            {
                root["locks"] = WriteLocks(level.Locks);
            }

            if (level.Mechanics.Count > 0)
            {
                root["mechanics"] = new JArray(ToObjects(level.Mechanics));
            }

            return root;
        }

        // ---- Reading ----

        private static ulong ReadSeed(JToken token, string path)
        {
            string text = JsonDoc.String(token, path);
            bool digitsOnly = text.Length >= 1 && text.Length <= 20;
            foreach (char c in text)
            {
                digitsOnly &= c >= '0' && c <= '9';
            }

            if (!digitsOnly || !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
            {
                throw new ContentFormatException(path, $"'{text}' is not a uint64 decimal string");
            }

            return seed;
        }

        private static PictureRef ReadPicture(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "id", "version", "mirror", "backgroundTreatment");
            string id = JsonDoc.Id(JsonDoc.Required(obj, path, "id"), JsonDoc.Join(path, "id"));
            int version = JsonDoc.Int(JsonDoc.Required(obj, path, "version"), JsonDoc.Join(path, "version"), min: 1);
            JToken? mirrorToken = JsonDoc.Optional(obj, "mirror");
            Mirror mirror = mirrorToken == null
                ? Mirror.None
                : JsonDoc.Enum(mirrorToken, JsonDoc.Join(path, "mirror"), WireNames.Mirrors);
            JToken? treatmentToken = JsonDoc.Optional(obj, "backgroundTreatment");
            string treatment = treatmentToken == null
                ? DefaultBackgroundTreatment
                : JsonDoc.String(treatmentToken, JsonDoc.Join(path, "backgroundTreatment"));
            return new PictureRef(id, version, mirror, treatment);
        }

        private static IReadOnlyDictionary<string, VariantId> ReadMapping(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            var mapping = new SortedDictionary<string, VariantId>(StringComparer.Ordinal);
            foreach (JProperty property in obj.Properties())
            {
                mapping.Add(property.Name, ReadVariant(property.Value, JsonDoc.Join(path, property.Name)));
            }

            if (mapping.Count < 2)
            {
                throw new ContentFormatException(path, $"expected at least 2 roles, got {mapping.Count}");
            }

            return mapping;
        }

        private static IReadOnlyList<EntryDef> ReadEntries(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path, minItems: 1, maxItems: 3);
            var entries = new EntryDef[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                JObject obj = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(obj, itemPath, "x", "y", "side");
                CellPos cell = ReadCellCoordinates(obj, itemPath);
                EntrySide side = JsonDoc.Enum(
                    JsonDoc.Required(obj, itemPath, "side"),
                    JsonDoc.Join(itemPath, "side"),
                    WireNames.EntrySides);
                entries[i] = new EntryDef(cell, side);
            }

            return entries;
        }

        private static IReadOnlyList<CellOverlay> ReadOverlays(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "cells");
            JToken? cellsToken = JsonDoc.Optional(obj, "cells");
            if (cellsToken == null)
            {
                return Array.Empty<CellOverlay>();
            }

            string cellsPath = JsonDoc.Join(path, "cells");
            JArray array = JsonDoc.Array(cellsToken, cellsPath);
            var overlays = new CellOverlay[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(cellsPath, i);
                JObject cell = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(cell, itemPath, "x", "y", "layersBelow", "mystery", "stone", "hole", "keyId");
                CellPos pos = ReadCellCoordinates(cell, itemPath);

                IReadOnlyList<VariantId> layersBelow = Array.Empty<VariantId>();
                JToken? layersToken = JsonDoc.Optional(cell, "layersBelow");
                if (layersToken != null)
                {
                    string layersPath = JsonDoc.Join(itemPath, "layersBelow");
                    JArray layers = JsonDoc.Array(layersToken, layersPath, maxItems: 3);
                    var list = new VariantId[layers.Count];
                    for (int j = 0; j < layers.Count; j++)
                    {
                        list[j] = ReadVariant(layers[j], JsonDoc.Index(layersPath, j));
                    }

                    layersBelow = list;
                }

                overlays[i] = new CellOverlay(
                    pos,
                    layersBelow,
                    OptionalBool(cell, itemPath, "mystery"),
                    OptionalBool(cell, itemPath, "stone"),
                    OptionalBool(cell, itemPath, "hole"),
                    OptionalString(cell, itemPath, "keyId"));
            }

            return overlays;
        }

        private static IReadOnlyList<SpecialDef> ReadSpecials(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path);
            var specials = new SpecialDef[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                JObject obj = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(obj, itemPath, "id", "type", "cells", "condition", "effect");
                string id = JsonDoc.String(JsonDoc.Required(obj, itemPath, "id"), JsonDoc.Join(itemPath, "id"));
                SpecialType type = JsonDoc.Enum(
                    JsonDoc.Required(obj, itemPath, "type"),
                    JsonDoc.Join(itemPath, "type"),
                    WireNames.SpecialTypes);
                IReadOnlyList<CellPos> cells = ReadCells(
                    JsonDoc.Required(obj, itemPath, "cells"),
                    JsonDoc.Join(itemPath, "cells"),
                    minItems: 1);
                SpecialCondition condition = ReadCondition(
                    JsonDoc.Required(obj, itemPath, "condition"),
                    JsonDoc.Join(itemPath, "condition"));
                SpecialEffect effect = ReadEffect(JsonDoc.Required(obj, itemPath, "effect"), JsonDoc.Join(itemPath, "effect"));
                specials[i] = new SpecialDef(id, type, cells, condition, effect);
            }

            return specials;
        }

        private static SpecialCondition ReadCondition(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "kind", "keyId", "variantId", "count", "regionCells");
            SpecialConditionKind kind = JsonDoc.Enum(
                JsonDoc.Required(obj, path, "kind"),
                JsonDoc.Join(path, "kind"),
                WireNames.ConditionKinds);
            JToken? variantToken = JsonDoc.Optional(obj, "variantId");
            VariantId? variant = variantToken == null ? (VariantId?)null : ReadVariant(variantToken, JsonDoc.Join(path, "variantId"));
            JToken? countToken = JsonDoc.Optional(obj, "count");
            int? count = countToken == null ? (int?)null : JsonDoc.Int(countToken, JsonDoc.Join(path, "count"), min: 1);
            JToken? regionToken = JsonDoc.Optional(obj, "regionCells");
            IReadOnlyList<CellPos> region = regionToken == null
                ? Array.Empty<CellPos>()
                : ReadCells(regionToken, JsonDoc.Join(path, "regionCells"), minItems: 0);
            return new SpecialCondition(kind, OptionalString(obj, path, "keyId"), variant, count, region);
        }

        private static SpecialEffect ReadEffect(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "kind", "cells");
            SpecialEffectKind kind = JsonDoc.Enum(
                JsonDoc.Required(obj, path, "kind"),
                JsonDoc.Join(path, "kind"),
                WireNames.EffectKinds);
            JToken? cellsToken = JsonDoc.Optional(obj, "cells");
            IReadOnlyList<CellPos> cells = cellsToken == null
                ? Array.Empty<CellPos>()
                : ReadCells(cellsToken, JsonDoc.Join(path, "cells"), minItems: 0);
            return new SpecialEffect(kind, cells);
        }

        private static IReadOnlyList<LockDef> ReadLocks(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path);
            var locks = new LockDef[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                JObject obj = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(obj, itemPath, "keyId", "target");
                string keyId = JsonDoc.String(JsonDoc.Required(obj, itemPath, "keyId"), JsonDoc.Join(itemPath, "keyId"));
                string targetPath = JsonDoc.Join(itemPath, "target");
                JObject target = JsonDoc.Object(JsonDoc.Required(obj, itemPath, "target"), targetPath);
                JsonDoc.AllowOnly(target, targetPath, "kind", "id");
                LockTargetKind kind = JsonDoc.Enum(
                    JsonDoc.Required(target, targetPath, "kind"),
                    JsonDoc.Join(targetPath, "kind"),
                    WireNames.LockTargets);
                string targetId = JsonDoc.String(JsonDoc.Required(target, targetPath, "id"), JsonDoc.Join(targetPath, "id"));
                locks[i] = new LockDef(keyId, kind, targetId);
            }

            return locks;
        }

        private static SlotsDef ReadSlots(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "count", "locked");
            int count = JsonDoc.Int(
                JsonDoc.Required(obj, path, "count"),
                JsonDoc.Join(path, "count"),
                min: SlotsDef.DefaultCount,
                max: SlotsDef.DefaultCount);

            LockedSlotDef? locked = null;
            JToken? lockedToken = JsonDoc.Optional(obj, "locked");
            if (lockedToken != null)
            {
                string lockedPath = JsonDoc.Join(path, "locked");
                JObject lockedObj = JsonDoc.Object(lockedToken, lockedPath);
                JsonDoc.AllowOnly(lockedObj, lockedPath, "slotIndex", "keyId");
                int slotIndex = JsonDoc.Int(
                    JsonDoc.Required(lockedObj, lockedPath, "slotIndex"),
                    JsonDoc.Join(lockedPath, "slotIndex"),
                    min: 0,
                    max: SlotsDef.DefaultCount - 1);
                string keyId = JsonDoc.String(
                    JsonDoc.Required(lockedObj, lockedPath, "keyId"),
                    JsonDoc.Join(lockedPath, "keyId"));
                locked = new LockedSlotDef(slotIndex, keyId);
            }

            return new SlotsDef(count, locked);
        }

        private static TrayDef ReadTray(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "stacks");
            string stacksPath = JsonDoc.Join(path, "stacks");
            JArray stacksArray = JsonDoc.Array(JsonDoc.Required(obj, path, "stacks"), stacksPath, minItems: 2, maxItems: 6);
            var stacks = new IReadOnlyList<string>[stacksArray.Count];
            for (int i = 0; i < stacksArray.Count; i++)
            {
                string stackPath = JsonDoc.Index(stacksPath, i);
                JArray stack = JsonDoc.Array(stacksArray[i], stackPath, minItems: 1);
                var ids = new string[stack.Count];
                for (int j = 0; j < stack.Count; j++)
                {
                    ids[j] = JsonDoc.String(stack[j], JsonDoc.Index(stackPath, j));
                }

                stacks[i] = ids;
            }

            return new TrayDef(stacks);
        }

        private static IReadOnlyList<PodDef> ReadPods(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path, minItems: 2);
            var pods = new PodDef[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                JObject obj = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(obj, itemPath, "id", "variantId", "count", "mystery", "lockKeyId", "connectedGroupId");
                string id = JsonDoc.String(JsonDoc.Required(obj, itemPath, "id"), JsonDoc.Join(itemPath, "id"));
                VariantId variant = ReadVariant(JsonDoc.Required(obj, itemPath, "variantId"), JsonDoc.Join(itemPath, "variantId"));
                int count = JsonDoc.Int(JsonDoc.Required(obj, itemPath, "count"), JsonDoc.Join(itemPath, "count"), min: 1);
                pods[i] = new PodDef(
                    id,
                    variant,
                    count,
                    OptionalBool(obj, itemPath, "mystery"),
                    OptionalString(obj, itemPath, "lockKeyId"),
                    OptionalString(obj, itemPath, "connectedGroupId"));
            }

            return pods;
        }

        private static DifficultyDef ReadDifficulty(JToken token, string path)
        {
            JObject obj = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(obj, path, "class", "score", "overridden");
            DifficultyClass difficultyClass = JsonDoc.Enum(
                JsonDoc.Required(obj, path, "class"),
                JsonDoc.Join(path, "class"),
                WireNames.DifficultyClasses);
            int score = JsonDoc.Int(JsonDoc.Required(obj, path, "score"), JsonDoc.Join(path, "score"), min: 0);
            return new DifficultyDef(difficultyClass, score, OptionalBool(obj, path, "overridden"));
        }

        private static IReadOnlyList<string> ReadMechanics(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var mechanics = new string[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                mechanics[i] = JsonDoc.String(array[i], itemPath);
                if (!seen.Add(mechanics[i]))
                {
                    throw new ContentFormatException(itemPath, $"duplicate mechanic '{mechanics[i]}'");
                }
            }

            return mechanics;
        }

        private static IReadOnlyList<CellPos> ReadCells(JToken token, string path, int minItems)
        {
            JArray array = JsonDoc.Array(token, path, minItems);
            var cells = new CellPos[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                JObject obj = JsonDoc.Object(array[i], itemPath);
                JsonDoc.AllowOnly(obj, itemPath, "x", "y");
                cells[i] = ReadCellCoordinates(obj, itemPath);
            }

            return cells;
        }

        private static CellPos ReadCellCoordinates(JObject obj, string path)
        {
            int x = JsonDoc.Int(JsonDoc.Required(obj, path, "x"), JsonDoc.Join(path, "x"), min: 0, max: CellPos.MaxWidth - 1);
            int y = JsonDoc.Int(JsonDoc.Required(obj, path, "y"), JsonDoc.Join(path, "y"), min: 0, max: CellPos.MaxHeight - 1);
            return new CellPos(x, y);
        }

        internal static VariantId ReadVariant(JToken token, string path)
        {
            string key = JsonDoc.String(token, path);
            if (!VariantId.IsValidKey(key) || !VariantCatalog.Default.Contains(new VariantId(key)))
            {
                throw new ContentFormatException(path, $"'{key}' is not a known variant id");
            }

            return new VariantId(key);
        }

        private static bool OptionalBool(JObject obj, string path, string name)
        {
            JToken? token = JsonDoc.Optional(obj, name);
            return token != null && JsonDoc.Bool(token, JsonDoc.Join(path, name));
        }

        private static string? OptionalString(JObject obj, string path, string name)
        {
            JToken? token = JsonDoc.Optional(obj, name);
            return token == null ? null : JsonDoc.String(token, JsonDoc.Join(path, name));
        }

        // ---- Writing ----

        private const string DefaultBackgroundTreatment = "default";

        private static JObject WritePicture(PictureRef picture)
        {
            var obj = new JObject { ["id"] = picture.Id, ["version"] = picture.Version };
            if (picture.Mirror != Mirror.None)
            {
                obj["mirror"] = WireNames.Mirrors.ToWire(picture.Mirror);
            }

            if (!string.Equals(picture.BackgroundTreatment, DefaultBackgroundTreatment, StringComparison.Ordinal))
            {
                obj["backgroundTreatment"] = picture.BackgroundTreatment;
            }

            return obj;
        }

        private static JObject WriteMapping(IReadOnlyDictionary<string, VariantId> mapping)
        {
            var obj = new JObject();
            foreach (KeyValuePair<string, VariantId> pair in mapping)
            {
                obj[pair.Key] = pair.Value.Key;
            }

            return obj;
        }

        private static JArray WriteEntries(IReadOnlyList<EntryDef> entries)
        {
            var array = new JArray();
            foreach (EntryDef entry in entries)
            {
                JObject obj = WriteCell(entry.Cell);
                obj["side"] = WireNames.EntrySides.ToWire(entry.Side);
                array.Add(obj);
            }

            return array;
        }

        private static JArray WriteOverlays(IReadOnlyList<CellOverlay> overlays)
        {
            var array = new JArray();
            foreach (CellOverlay overlay in overlays)
            {
                JObject obj = WriteCell(overlay.Cell);
                if (overlay.LayersBelow.Count > 0)
                {
                    var layers = new JArray();
                    foreach (VariantId layer in overlay.LayersBelow)
                    {
                        layers.Add(layer.Key);
                    }

                    obj["layersBelow"] = layers;
                }

                AddFlag(obj, "mystery", overlay.Mystery);
                AddFlag(obj, "stone", overlay.Stone);
                AddFlag(obj, "hole", overlay.Hole);
                AddString(obj, "keyId", overlay.KeyId);
                array.Add(obj);
            }

            return array;
        }

        private static JArray WriteSpecials(IReadOnlyList<SpecialDef> specials)
        {
            var array = new JArray();
            foreach (SpecialDef special in specials)
            {
                var condition = new JObject { ["kind"] = WireNames.ConditionKinds.ToWire(special.Condition.Kind) };
                AddString(condition, "keyId", special.Condition.KeyId);
                if (special.Condition.Variant.HasValue)
                {
                    condition["variantId"] = special.Condition.Variant.Value.Key;
                }

                if (special.Condition.Count.HasValue)
                {
                    condition["count"] = special.Condition.Count.Value;
                }

                if (special.Condition.RegionCells.Count > 0)
                {
                    condition["regionCells"] = WriteCells(special.Condition.RegionCells);
                }

                var effect = new JObject { ["kind"] = WireNames.EffectKinds.ToWire(special.Effect.Kind) };
                if (special.Effect.Cells.Count > 0)
                {
                    effect["cells"] = WriteCells(special.Effect.Cells);
                }

                array.Add(new JObject
                {
                    ["id"] = special.Id,
                    ["type"] = WireNames.SpecialTypes.ToWire(special.Type),
                    ["cells"] = WriteCells(special.Cells),
                    ["condition"] = condition,
                    ["effect"] = effect,
                });
            }

            return array;
        }

        private static JArray WriteLocks(IReadOnlyList<LockDef> locks)
        {
            var array = new JArray();
            foreach (LockDef lockDef in locks)
            {
                array.Add(new JObject
                {
                    ["keyId"] = lockDef.KeyId,
                    ["target"] = new JObject
                    {
                        ["kind"] = WireNames.LockTargets.ToWire(lockDef.TargetKind),
                        ["id"] = lockDef.TargetId,
                    },
                });
            }

            return array;
        }

        private static JObject WriteSlots(SlotsDef slots)
        {
            var obj = new JObject { ["count"] = slots.Count };
            if (slots.Locked != null)
            {
                obj["locked"] = new JObject { ["slotIndex"] = slots.Locked.SlotIndex, ["keyId"] = slots.Locked.KeyId };
            }

            return obj;
        }

        private static JObject WriteTray(TrayDef tray)
        {
            var stacks = new JArray();
            foreach (IReadOnlyList<string> stack in tray.Stacks)
            {
                stacks.Add(new JArray(ToObjects(stack)));
            }

            return new JObject { ["stacks"] = stacks };
        }

        private static JArray WritePods(IReadOnlyList<PodDef> pods)
        {
            var array = new JArray();
            foreach (PodDef pod in pods)
            {
                var obj = new JObject { ["id"] = pod.Id, ["variantId"] = pod.Variant.Key, ["count"] = pod.Count };
                AddFlag(obj, "mystery", pod.Mystery);
                AddString(obj, "lockKeyId", pod.LockKeyId);
                AddString(obj, "connectedGroupId", pod.ConnectedGroupId);
                array.Add(obj);
            }

            return array;
        }

        private static JObject WriteDifficulty(DifficultyDef difficulty)
        {
            var obj = new JObject
            {
                ["class"] = WireNames.DifficultyClasses.ToWire(difficulty.Class),
                ["score"] = difficulty.Score,
            };
            AddFlag(obj, "overridden", difficulty.Overridden);
            return obj;
        }

        private static JArray WriteCells(IReadOnlyList<CellPos> cells)
        {
            var array = new JArray();
            foreach (CellPos cell in cells)
            {
                array.Add(WriteCell(cell));
            }

            return array;
        }

        private static JObject WriteCell(CellPos cell) => new JObject { ["x"] = cell.X, ["y"] = cell.Y };

        private static void AddFlag(JObject obj, string name, bool value)
        {
            if (value)
            {
                obj[name] = true;
            }
        }

        private static void AddString(JObject obj, string name, string? value)
        {
            if (value != null)
            {
                obj[name] = value;
            }
        }

        private static object[] ToObjects(IReadOnlyList<string> values)
        {
            var result = new object[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return result;
        }
    }
}
