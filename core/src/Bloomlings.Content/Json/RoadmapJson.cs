using System.Collections.Generic;
using Bloomlings.Core.Progression;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Json
{
    /// <summary>
    /// Reads and writes <c>content/roadmap/unlock-roadmap.json</c>: <c>{"entries": [{level, unlockId, kind,
    /// demoWithin, optional?}]}</c> in level order (T057). Pipeline validation reads it; the runtime uses the identical
    /// <see cref="UnlockRoadmap.Default"/>.
    /// </summary>
    public static class RoadmapJson
    {
        private static readonly EnumNames<UnlockKind> KindNames = new EnumNames<UnlockKind>(
            (UnlockKind.System, "system"),
            (UnlockKind.Booster, "booster"),
            (UnlockKind.Mechanic, "mechanic"),
            (UnlockKind.Variant, "variant"),
            (UnlockKind.Profile, "profile"));

        public static UnlockRoadmap Read(string json)
        {
            JObject root = JsonDoc.ParseObject(json, "roadmap");
            JsonDoc.AllowOnly(root, string.Empty, "entries");
            JArray array = JsonDoc.Array(JsonDoc.Required(root, string.Empty, "entries"), "entries", minItems: 1);
            var entries = new List<UnlockEntry>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                string path = JsonDoc.Index("entries", i);
                JObject obj = JsonDoc.Object(array[i], path);
                JsonDoc.AllowOnly(obj, path, "level", "unlockId", "kind", "demoWithin", "optional");
                JToken? optional = JsonDoc.Optional(obj, "optional");
                entries.Add(new UnlockEntry(
                    JsonDoc.Int(JsonDoc.Required(obj, path, "level"), JsonDoc.Join(path, "level"), min: 1),
                    JsonDoc.String(JsonDoc.Required(obj, path, "unlockId"), JsonDoc.Join(path, "unlockId")),
                    JsonDoc.Enum(JsonDoc.Required(obj, path, "kind"), JsonDoc.Join(path, "kind"), KindNames),
                    JsonDoc.Int(JsonDoc.Required(obj, path, "demoWithin"), JsonDoc.Join(path, "demoWithin"), 0, 2),
                    optional != null && JsonDoc.Bool(optional, JsonDoc.Join(path, "optional"))));
            }

            try
            {
                return new UnlockRoadmap(entries);
            }
            catch (System.ArgumentException ex)
            {
                throw new ContentFormatException("entries", ex.Message);
            }
        }

        public static string Write(UnlockRoadmap roadmap)
        {
            var entries = new JArray();
            foreach (UnlockEntry entry in roadmap.Entries)
            {
                var obj = new JObject
                {
                    ["level"] = entry.Level,
                    ["unlockId"] = entry.UnlockId,
                    ["kind"] = KindNames.ToWire(entry.Kind),
                    ["demoWithin"] = entry.DemoWithin,
                };
                if (entry.Optional)
                {
                    obj["optional"] = true;
                }

                entries.Add(obj);
            }

            return CanonicalJson.Write(new JObject { ["entries"] = entries }, indented: true);
        }
    }
}
