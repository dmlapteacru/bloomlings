using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Content.Json;
using Bloomlings.Core.Variants;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Generator.Profiles
{
    /// <summary>Reads generation profiles and the difficulty thresholds (T083).</summary>
    public static class ProfileLoader
    {
        private static readonly EnumNames<BufferPressure> Pressures = new EnumNames<BufferPressure>(
            (BufferPressure.Relaxed, "relaxed"),
            (BufferPressure.Normal, "normal"),
            (BufferPressure.Tense, "tense"),
            (BufferPressure.Critical, "critical"));

        public static GenerationProfile ReadFile(string path) => Read(File.ReadAllText(path), path);

        public static GenerationProfile Read(string json, string name = "profile")
        {
            const string p = "";
            JObject root = JsonDoc.ParseObject(json, name);
            JsonDoc.AllowOnly(root, p, "bandId", "levelRange", "boardSize", "picturePool", "structureTargets", "variantCount", "allowedVariants", "mappingConstraints", "entryLayouts", "maxLayerDepth", "allowedMechanics", "stacks", "podCount", "podSize", "work", "bufferPressureTarget", "durationTarget", "hardMode", "solver", "note");

            JObject board = Obj(root, "boardSize", "width", "height");
            JObject pool = Obj(root, "picturePool", "themes");
            JObject structure = Obj(root, "structureTargets", "nestingDepth", "backgroundSharePermille");
            JObject mapping = Obj(root, "mappingConstraints", "allowRoleMerge");
            JObject hard = Obj(root, "hardMode", "extraPods", "maxInjections", "hardPressure", "superHardPressure");
            JObject solver = Obj(root, "solver", "nodeBudget", "maxCandidatesPerLevel");

            var variants = new List<VariantId>();
            JArray variantArray = JsonDoc.Array(JsonDoc.Required(root, p, "allowedVariants"), "allowedVariants", minItems: 1);
            for (int i = 0; i < variantArray.Count; i++)
            {
                variants.Add(DefinitionJsonVariant(variantArray[i], JsonDoc.Index("allowedVariants", i)));
            }

            var profile = new GenerationProfile(
                JsonDoc.String(JsonDoc.Required(root, p, "bandId"), "bandId"),
                Range(JsonDoc.Required(root, p, "levelRange"), "levelRange"),
                Range(JsonDoc.Required(board, "boardSize", "width"), "boardSize.width"),
                Range(JsonDoc.Required(board, "boardSize", "height"), "boardSize.height"),
                Strings(pool, "picturePool", "themes"),
                new StructureTargets(
                    Range(JsonDoc.Required(structure, "structureTargets", "nestingDepth"), "structureTargets.nestingDepth"),
                    Range(JsonDoc.Required(structure, "structureTargets", "backgroundSharePermille"), "structureTargets.backgroundSharePermille")),
                Range(JsonDoc.Required(root, p, "variantCount"), "variantCount"),
                variants,
                JsonDoc.Bool(JsonDoc.Required(mapping, "mappingConstraints", "allowRoleMerge"), "mappingConstraints.allowRoleMerge"),
                Strings(root, p, "entryLayouts"),
                JsonDoc.Int(JsonDoc.Required(root, p, "maxLayerDepth"), "maxLayerDepth", 0, 3),
                Strings(root, p, "allowedMechanics"),
                Range(JsonDoc.Required(root, p, "stacks"), "stacks"),
                Range(JsonDoc.Required(root, p, "podCount"), "podCount"),
                Range(JsonDoc.Required(root, p, "podSize"), "podSize"),
                Range(JsonDoc.Required(root, p, "work"), "work"),
                JsonDoc.Enum(JsonDoc.Required(root, p, "bufferPressureTarget"), "bufferPressureTarget", Pressures),
                Range(JsonDoc.Required(root, p, "durationTarget"), "durationTarget"),
                new HardMode(
                    JsonDoc.Int(JsonDoc.Required(hard, "hardMode", "extraPods"), "hardMode.extraPods", 0, 10),
                    JsonDoc.Int(JsonDoc.Required(hard, "hardMode", "maxInjections"), "hardMode.maxInjections", 0, 100),
                    JsonDoc.Enum(JsonDoc.Required(hard, "hardMode", "hardPressure"), "hardMode.hardPressure", Pressures),
                    JsonDoc.Enum(JsonDoc.Required(hard, "hardMode", "superHardPressure"), "hardMode.superHardPressure", Pressures)),
                JsonDoc.Int(JsonDoc.Required(solver, "solver", "nodeBudget"), "solver.nodeBudget", min: 100),
                JsonDoc.Int(JsonDoc.Required(solver, "solver", "maxCandidatesPerLevel"), "solver.maxCandidatesPerLevel", min: 1));

            if (profile.Stacks.Min < 2 || profile.Stacks.Max > 6)
            {
                throw new ContentFormatException("stacks", "stacks must stay within 2–6");
            }

            return profile;
        }

        /// <summary>
        /// Reads <c>difficulty-thresholds.json</c>: global integer weights per metric, and per-band class thresholds,
        /// because scores grow with level scale while the class is relative to the band (FR-082).
        /// </summary>
        public static DifficultyThresholds ReadThresholds(string json, string bandId)
        {
            JObject root = JsonDoc.ParseObject(json, "difficulty-thresholds");
            JsonDoc.AllowOnly(root, string.Empty, "weights", "bands", "note");
            var weights = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (JProperty weight in JsonDoc.Object(JsonDoc.Required(root, string.Empty, "weights"), "weights").Properties())
            {
                weights[weight.Name] = JsonDoc.Int(weight.Value, "weights." + weight.Name);
            }

            JObject bands = JsonDoc.Object(JsonDoc.Required(root, string.Empty, "bands"), "bands");
            JObject band = JsonDoc.Object(bands[bandId] ?? throw new ContentFormatException("bands." + bandId, "no thresholds for this band"), "bands." + bandId);
            JsonDoc.AllowOnly(band, "bands." + bandId, "hardMin", "superHardMin");
            int hardMin = JsonDoc.Int(JsonDoc.Required(band, "bands." + bandId, "hardMin"), "hardMin", min: 0);
            int superHardMin = JsonDoc.Int(JsonDoc.Required(band, "bands." + bandId, "superHardMin"), "superHardMin", min: hardMin);
            return new DifficultyThresholds(weights, hardMin, superHardMin);
        }

        private static JObject Obj(JObject root, string name, params string[] allowed)
        {
            JObject obj = JsonDoc.Object(JsonDoc.Required(root, string.Empty, name), name);
            JsonDoc.AllowOnly(obj, name, allowed);
            return obj;
        }

        private static IntRange Range(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path, 2, 2);
            int min = JsonDoc.Int(array[0], JsonDoc.Index(path, 0));
            int max = JsonDoc.Int(array[1], JsonDoc.Index(path, 1), min: min);
            return new IntRange(min, max);
        }

        private static IReadOnlyList<string> Strings(JObject obj, string path, string name)
        {
            JArray array = JsonDoc.Array(JsonDoc.Required(obj, path, name), JsonDoc.Join(path, name));
            var values = new List<string>();
            for (int i = 0; i < array.Count; i++)
            {
                values.Add(JsonDoc.String(array[i], JsonDoc.Index(JsonDoc.Join(path, name), i)));
            }

            return values;
        }

        private static VariantId DefinitionJsonVariant(JToken token, string path)
        {
            string key = JsonDoc.String(token, path);
            if (!VariantId.IsValidKey(key) || !VariantCatalog.Default.Contains(new VariantId(key)))
            {
                throw new ContentFormatException(path, $"'{key}' is not a known variant");
            }

            return new VariantId(key);
        }
    }
}
