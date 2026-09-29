using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.App.Progression
{
    /// <summary>The reward tier of a milestone cadence (FR-061).</summary>
    public enum MilestoneTier
    {
        Bundle,
        Cosmetic,
        Major,
        Prestige,
    }

    /// <summary>
    /// One milestone cadence: every <see cref="Every"/> levels, <see cref="Petals"/>, <see cref="BoosterCharges"/> of
    /// the unlocked booster with the fewest charges, <see cref="EachBooster"/> charges of every booster, and the first
    /// item of <see cref="Items"/> the player does not own yet (a generated level badge or marker once all are owned).
    /// </summary>
    public sealed record MilestoneCadence(int Every, MilestoneTier Tier, int Petals, int BoosterCharges, int EachBooster, IReadOnlyList<string> Items);

    /// <summary>
    /// The milestone cadences (FR-061, T142). <see cref="Default"/> mirrors <c>content/roadmap/milestones.json</c>
    /// (a client test keeps them equal): every 25 levels a bundle, every 50 a cosmetic or profile reward, every 100 a
    /// major milestone, and every 250, 500 and 1000 a prestige reward. A level that matches several cadences gets only
    /// the largest cadence's reward.
    /// </summary>
    public sealed class MilestoneTable
    {
        public MilestoneTable(IEnumerable<MilestoneCadence> cadences)
        {
            var list = new List<MilestoneCadence>(cadences);
            list.Sort((a, b) => a.Every.CompareTo(b.Every));
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Every < 1 || (i > 0 && list[i].Every == list[i - 1].Every))
                {
                    throw new ArgumentException($"Milestone cadences must be positive and distinct; found {list[i].Every}.", nameof(cadences));
                }
            }

            Cadences = list;
        }

        /// <summary>Ascending by <see cref="MilestoneCadence.Every"/>.</summary>
        public IReadOnlyList<MilestoneCadence> Cadences { get; }

        /// <summary>
        /// The milestone rewards. REMOTE-CONFIG-DEFERRED: local on purpose; whether they become Remote Config (FR-085) is
        /// decided at the end (tasks.md, "Local values, Remote Config decided at the end").
        /// </summary>
        public static MilestoneTable Default { get; } = new MilestoneTable(new[]
        {
            new MilestoneCadence(25, MilestoneTier.Bundle, 50, 1, 0, Array.Empty<string>()),
            new MilestoneCadence(50, MilestoneTier.Cosmetic, 100, 0, 0, new[]
            {
                "hat.leaf_cap", "trail.petal_sparkle", "expression.wink", "frame.daisy", "hat.acorn_cap", "trail.dew_sparkle",
                "expression.smile", "badge.sprout",
            }),
            new MilestoneCadence(100, MilestoneTier.Major, 300, 0, 1, new[]
            {
                "hat.straw_hat", "hat.flower_crown", "expression.starry_eyes", "trail.leaf_swirl", "hat.mushroom_cap",
                "frame.ivy", "hat.night_cap", "trail.moon_dust",
            }),
            new MilestoneCadence(250, MilestoneTier.Prestige, 500, 0, 0, new[] { "frame.silver_vine", "skin.moon_frost", "marker.silver_leaf", "frame.silver_bloom" }),
            new MilestoneCadence(500, MilestoneTier.Prestige, 500, 0, 0, new[] { "badge.golden_leaf", "skin.golden_petals", "hat.golden_crown", "badge.golden_bloom" }),
            new MilestoneCadence(1000, MilestoneTier.Prestige, 500, 0, 0, new[] { "marker.crown", "frame.aurora", "hat.star_crown" }),
        });

        /// <summary>The cadence whose reward <paramref name="level"/> grants: the largest one it is a multiple of, or null.</summary>
        public MilestoneCadence? CadenceFor(int level)
        {
            if (level < 1)
            {
                return null;
            }

            for (int i = Cadences.Count - 1; i >= 0; i--)
            {
                if (level % Cadences[i].Every == 0)
                {
                    return Cadences[i];
                }
            }

            return null;
        }

        /// <summary>The first milestone level after <paramref name="highestCompleted"/>, or null without cadences.</summary>
        public int? NextMilestoneLevel(int highestCompleted)
        {
            int? next = null;
            foreach (MilestoneCadence cadence in Cadences)
            {
                int candidate = ((Math.Max(0, highestCompleted) / cadence.Every) + 1) * cadence.Every;
                if (!next.HasValue || candidate < next.Value)
                {
                    next = candidate;
                }
            }

            return next;
        }

        /// <summary>Reads <c>content/roadmap/milestones.json</c>: <c>{"cadences": [{every, tier, petals, boosterCharges, eachBooster, items}]}</c>.</summary>
        public static MilestoneTable Parse(string json)
        {
            JObject root = JsonDoc.ParseObject(json, "milestones");
            JsonDoc.AllowOnly(root, string.Empty, "note", "cadences");
            JArray array = JsonDoc.Array(JsonDoc.Required(root, string.Empty, "cadences"), "cadences");
            var cadences = new List<MilestoneCadence>();
            for (int i = 0; i < array.Count; i++)
            {
                string path = JsonDoc.Index("cadences", i);
                JObject item = JsonDoc.Object(array[i], path);
                JsonDoc.AllowOnly(item, path, "every", "tier", "petals", "boosterCharges", "eachBooster", "items");
                string tier = JsonDoc.String(JsonDoc.Required(item, path, "tier"), JsonDoc.Join(path, "tier"));
                var items = new List<string>();
                JToken? list = JsonDoc.Optional(item, "items");
                if (list != null)
                {
                    foreach (JToken id in JsonDoc.Array(list, JsonDoc.Join(path, "items")))
                    {
                        items.Add(JsonDoc.String(id, JsonDoc.Join(path, "items")));
                    }
                }

                cadences.Add(new MilestoneCadence(
                    JsonDoc.Int(JsonDoc.Required(item, path, "every"), JsonDoc.Join(path, "every"), min: 1),
                    ParseTier(tier, JsonDoc.Join(path, "tier")),
                    OptionalInt(item, path, "petals"),
                    OptionalInt(item, path, "boosterCharges"),
                    OptionalInt(item, path, "eachBooster"),
                    items));
            }

            return new MilestoneTable(cadences);
        }

        private static int OptionalInt(JObject item, string path, string name)
        {
            JToken? token = JsonDoc.Optional(item, name);
            return token == null ? 0 : JsonDoc.Int(token, JsonDoc.Join(path, name), min: 0);
        }

        private static MilestoneTier ParseTier(string text, string path) => text switch
        {
            "bundle" => MilestoneTier.Bundle,
            "cosmetic" => MilestoneTier.Cosmetic,
            "major" => MilestoneTier.Major,
            "prestige" => MilestoneTier.Prestige,
            _ => throw new ContentFormatException(path, string.Format(CultureInfo.InvariantCulture, "'{0}' is not bundle, cosmetic, major or prestige", text)),
        };
    }
}
