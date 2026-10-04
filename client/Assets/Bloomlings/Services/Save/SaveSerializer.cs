using System;
using System.Collections.Generic;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>
    /// Forward migrations of the save document: each step turns version n into version n + 1 (R15). Version 1 is
    /// current, so the default set is empty; a future schema change registers its step here.
    /// </summary>
    public sealed class SaveMigrations
    {
        private readonly Dictionary<int, Func<JObject, JObject>> _steps = new Dictionary<int, Func<JObject, JObject>>();

        public static SaveMigrations Default => new SaveMigrations();

        public void Register(int fromVersion, Func<JObject, JObject> step) => _steps[fromVersion] = step;

        internal JObject Apply(JObject document)
        {
            int version = JsonDoc.Int(JsonDoc.Required(document, string.Empty, "schemaVersion"), "schemaVersion", min: 0);
            while (version < PlayerSave.CurrentSchemaVersion)
            {
                if (!_steps.TryGetValue(version, out Func<JObject, JObject>? step))
                {
                    throw new ContentFormatException("schemaVersion", $"no migration from version {version}");
                }

                document = step(document);
                int next = JsonDoc.Int(JsonDoc.Required(document, string.Empty, "schemaVersion"), "schemaVersion", min: 0);
                if (next <= version)
                {
                    throw new ContentFormatException("schemaVersion", $"migration from {version} did not advance the version");
                }

                version = next;
            }

            if (version > PlayerSave.CurrentSchemaVersion)
            {
                throw new ContentFormatException("schemaVersion", $"save version {version} is newer than this app ({PlayerSave.CurrentSchemaVersion})");
            }

            return document;
        }
    }

    /// <summary>
    /// Reads and writes <c>player-save.v1</c> documents with Newtonsoft (T058). Reading checks the schema by hand
    /// (no reflection, IL2CPP-safe), including the rules that Petals and charges are never negative and that
    /// milestone claims and ledger transaction ids are unique. Writing is canonical (sorted keys).
    /// </summary>
    public static class SaveSerializer
    {
        private static readonly string[] BoosterNames = { "extraSlot", "shuffle", "return", "bloomBurst" };

        private static readonly string[] CosmeticsOwners = { "sprig", "bloom", "drop", "twig", CosmeticsData.ProfileOwner };

        public static PlayerSave Read(string json, SaveMigrations? migrations = null) =>
            Read(JsonDoc.ParseObject(json, "save"), migrations);

        public static PlayerSave Read(JObject document, SaveMigrations? migrations = null)
        {
            JObject root = (migrations ?? SaveMigrations.Default).Apply(document);
            const string p = "";
            JsonDoc.AllowOnly(root, p, "schemaVersion", "localPlayerId", "linkedIdentity", "deviceId", "updatedAt", "progression", "wallet", "purchases", "boosters", "unlocks", "milestones", "cosmetics", "daily", "collection", "settings", "stats");

            var save = new PlayerSave
            {
                SchemaVersion = JsonDoc.Int(JsonDoc.Required(root, p, "schemaVersion"), "schemaVersion", PlayerSave.CurrentSchemaVersion, PlayerSave.CurrentSchemaVersion),
                LocalPlayerId = JsonDoc.String(JsonDoc.Required(root, p, "localPlayerId"), "localPlayerId"),
                UpdatedAt = JsonDoc.String(JsonDoc.Required(root, p, "updatedAt"), "updatedAt"),
            };

            JToken? linked = JsonDoc.Optional(root, "linkedIdentity");
            if (linked != null)
            {
                string identity = JsonDoc.String(linked, "linkedIdentity");
                if (identity != "apple" && identity != "google_play_games")
                {
                    throw new ContentFormatException("linkedIdentity", $"'{identity}' is not apple or google_play_games");
                }

                save.LinkedIdentity = identity;
            }

            JToken? device = JsonDoc.Optional(root, "deviceId");
            save.DeviceId = device == null ? null : JsonDoc.String(device, "deviceId");

            JObject progression = Obj(root, "progression", "highestCompletedLevel", "contentVersionSeen");
            save.Progression.HighestCompletedLevel = JsonDoc.Int(JsonDoc.Required(progression, "progression", "highestCompletedLevel"), "progression.highestCompletedLevel", min: 0);
            save.Progression.ContentVersionSeen = JsonDoc.Int(JsonDoc.Required(progression, "progression", "contentVersionSeen"), "progression.contentVersionSeen", min: 1);

            JObject wallet = Obj(root, "wallet", "petals");
            save.Wallet.SetPetals(JsonDoc.Int(JsonDoc.Required(wallet, "wallet", "petals"), "wallet.petals", min: 0));

            ReadPurchases(Obj(root, "purchases", "ledger", "entitlements", "starterPackOffered"), save.Purchases);
            BoosterGrant charges = ReadBoosters(Obj(root, "boosters", BoosterNames), "boosters");
            save.Boosters.Set(BoosterKind.ExtraSlot, charges.ExtraSlot);
            save.Boosters.Set(BoosterKind.Shuffle, charges.Shuffle);
            save.Boosters.Set(BoosterKind.Return, charges.Return);
            save.Boosters.Set(BoosterKind.BloomBurst, charges.BloomBurst);

            JObject unlocks = Obj(root, "unlocks", "flags", "demosSeen");
            JObject flags = JsonDoc.Object(JsonDoc.Required(unlocks, "unlocks", "flags"), "unlocks.flags");
            foreach (JProperty flag in flags.Properties())
            {
                save.Unlocks.Flags[flag.Name] = JsonDoc.Bool(flag.Value, "unlocks.flags." + flag.Name);
            }

            foreach (string demo in UniqueStrings(unlocks, "unlocks", "demosSeen"))
            {
                save.Unlocks.MarkDemoSeen(demo);
            }

            JObject milestones = Obj(root, "milestones", "claimed");
            JArray claimed = JsonDoc.Array(JsonDoc.Required(milestones, "milestones", "claimed"), "milestones.claimed");
            for (int i = 0; i < claimed.Count; i++)
            {
                int level = JsonDoc.Int(claimed[i], JsonDoc.Index("milestones.claimed", i), min: 1);
                if (!save.Milestones.TryClaim(level))
                {
                    throw new ContentFormatException(JsonDoc.Index("milestones.claimed", i), $"milestone {level} is claimed twice");
                }
            }

            JObject cosmetics = Obj(root, "cosmetics", "owned", "equipped");
            foreach (string owned in UniqueStrings(cosmetics, "cosmetics", "owned"))
            {
                save.Cosmetics.Owned.Add(owned);
            }

            JObject equipped = JsonDoc.Object(JsonDoc.Required(cosmetics, "cosmetics", "equipped"), "cosmetics.equipped");
            JsonDoc.AllowOnly(equipped, "cosmetics.equipped", CosmeticsOwners);
            foreach (JProperty owner in equipped.Properties())
            {
                string path = "cosmetics.equipped." + owner.Name;
                if (owner.Value.Type == JTokenType.String && owner.Name != CosmeticsData.ProfileOwner)
                {
                    // Early saves held one item per family: its slot is the kind its id names.
                    string id = JsonDoc.String(owner.Value, path);
                    int dot = id.IndexOf('.');
                    string kind = dot > 0 ? id.Substring(0, dot) : string.Empty;
                    if (Array.IndexOf(CosmeticsData.KindsOf(owner.Name), kind) < 0)
                    {
                        throw new ContentFormatException(path, $"'{id}' is not a worn cosmetic");
                    }

                    save.Cosmetics.Equipped[CosmeticsData.Slot(owner.Name, kind)] = id;
                    continue;
                }

                JObject slots = JsonDoc.Object(owner.Value, path);
                JsonDoc.AllowOnly(slots, path, CosmeticsData.KindsOf(owner.Name));
                foreach (JProperty slot in slots.Properties())
                {
                    save.Cosmetics.Equipped[CosmeticsData.Slot(owner.Name, slot.Name)] = JsonDoc.String(slot.Value, JsonDoc.Join(path, slot.Name));
                }
            }

            JObject daily = Obj(root, "daily", "rewardLastClaimUtcDate", "rewardStreak", "challengeLastCompletedUtcDate", "freeBoosterAdUtcDate");
            save.Daily.RewardLastClaimUtcDate = OptionalString(daily, "daily", "rewardLastClaimUtcDate");
            JToken? streak = JsonDoc.Optional(daily, "rewardStreak");
            save.Daily.RewardStreak = streak == null ? 0 : JsonDoc.Int(streak, "daily.rewardStreak", min: 0);
            save.Daily.ChallengeLastCompletedUtcDate = OptionalString(daily, "daily", "challengeLastCompletedUtcDate");
            save.Daily.FreeBoosterAdUtcDate = OptionalString(daily, "daily", "freeBoosterAdUtcDate");

            JArray collection = JsonDoc.Array(JsonDoc.Required(root, p, "collection"), "collection");
            for (int i = 0; i < collection.Count; i++)
            {
                string path = JsonDoc.Index("collection", i);
                JObject entry = JsonDoc.Object(collection[i], path);
                JsonDoc.AllowOnly(entry, path, "pictureId", "pictureVersion", "mappingHash", "levelNumber");
                save.Collection.Add(new CollectionEntry(
                    JsonDoc.String(JsonDoc.Required(entry, path, "pictureId"), JsonDoc.Join(path, "pictureId")),
                    JsonDoc.Int(JsonDoc.Required(entry, path, "pictureVersion"), JsonDoc.Join(path, "pictureVersion"), min: 1),
                    JsonDoc.String(JsonDoc.Required(entry, path, "mappingHash"), JsonDoc.Join(path, "mappingHash")),
                    JsonDoc.Int(JsonDoc.Required(entry, path, "levelNumber"), JsonDoc.Join(path, "levelNumber"), min: 1)));
            }

            JObject settings = Obj(root, "settings", "music", "sfx", "haptics", "speed2x", "homePetals", "language");
            save.Settings.Music = JsonDoc.Bool(JsonDoc.Required(settings, "settings", "music"), "settings.music");
            save.Settings.Sfx = JsonDoc.Bool(JsonDoc.Required(settings, "settings", "sfx"), "settings.sfx");
            save.Settings.Haptics = JsonDoc.Bool(JsonDoc.Required(settings, "settings", "haptics"), "settings.haptics");
            save.Settings.Speed2x = JsonDoc.Bool(JsonDoc.Required(settings, "settings", "speed2x"), "settings.speed2x");
            JToken? homePetals = JsonDoc.Optional(settings, "homePetals");
            save.Settings.HomePetals = homePetals == null || JsonDoc.Bool(homePetals, "settings.homePetals");
            save.Settings.Language = OptionalString(settings, "settings", "language") ?? "en";

            JToken? stats = JsonDoc.Optional(root, "stats");
            if (stats != null)
            {
                foreach (JProperty stat in JsonDoc.Object(stats, "stats").Properties())
                {
                    string path = "stats." + stat.Name;
                    if (stat.Value.Type == JTokenType.Integer)
                    {
                        save.Stats.Counters[stat.Name] = stat.Value.Value<long>();
                        continue;
                    }

                    foreach (JProperty inner in JsonDoc.Object(stat.Value, path).Properties())
                    {
                        if (inner.Value.Type != JTokenType.Integer)
                        {
                            throw new ContentFormatException(path + "." + inner.Name, "expected an integer");
                        }

                        save.Stats.Increment(stat.Name, inner.Name, inner.Value.Value<long>());
                    }
                }
            }

            return save;
        }

        public static string Write(PlayerSave save, bool indented = true) => CanonicalJson.Write(ToJObject(save), indented);

        public static JObject ToJObject(PlayerSave save)
        {
            var ledger = new JArray();
            foreach (LedgerEntry entry in save.Purchases.Ledger)
            {
                var item = new JObject
                {
                    ["transactionId"] = entry.TransactionId,
                    ["productId"] = entry.ProductId,
                    ["grantedAt"] = entry.GrantedAt,
                };
                var grants = new JObject();
                if (entry.GrantedPetals > 0)
                {
                    grants["petals"] = entry.GrantedPetals;
                }

                if (entry.GrantedBoosters != null)
                {
                    grants["boosters"] = BoostersObject(entry.GrantedBoosters);
                }

                if (grants.Count > 0)
                {
                    item["grants"] = grants;
                }

                ledger.Add(item);
            }

            var flags = new JObject();
            foreach (KeyValuePair<string, bool> flag in save.Unlocks.Flags)
            {
                flags[flag.Key] = flag.Value;
            }

            var equipped = new JObject();
            foreach (KeyValuePair<string, string> pair in save.Cosmetics.Equipped)
            {
                int dot = pair.Key.IndexOf('.');
                string owner = pair.Key.Substring(0, dot);
                if (!(equipped[owner] is JObject slots))
                {
                    slots = new JObject();
                    equipped[owner] = slots;
                }

                slots[pair.Key.Substring(dot + 1)] = pair.Value;
            }

            var collection = new JArray();
            foreach (CollectionEntry entry in save.Collection)
            {
                collection.Add(new JObject
                {
                    ["pictureId"] = entry.PictureId,
                    ["pictureVersion"] = entry.PictureVersion,
                    ["mappingHash"] = entry.MappingHash,
                    ["levelNumber"] = entry.LevelNumber,
                });
            }

            var daily = new JObject
            {
                ["rewardLastClaimUtcDate"] = save.Daily.RewardLastClaimUtcDate,
                ["rewardStreak"] = save.Daily.RewardStreak,
                ["challengeLastCompletedUtcDate"] = save.Daily.ChallengeLastCompletedUtcDate,
                ["freeBoosterAdUtcDate"] = save.Daily.FreeBoosterAdUtcDate,
            };

            var root = new JObject
            {
                ["schemaVersion"] = save.SchemaVersion,
                ["localPlayerId"] = save.LocalPlayerId,
                ["updatedAt"] = save.UpdatedAt,
                ["progression"] = new JObject
                {
                    ["highestCompletedLevel"] = save.Progression.HighestCompletedLevel,
                    ["contentVersionSeen"] = save.Progression.ContentVersionSeen,
                },
                ["wallet"] = new JObject { ["petals"] = save.Wallet.Petals },
                ["purchases"] = new JObject
                {
                    ["ledger"] = ledger,
                    ["entitlements"] = new JObject { ["removeAds"] = save.Purchases.RemoveAds },
                    ["starterPackOffered"] = save.Purchases.StarterPackOffered,
                },
                ["boosters"] = BoostersObject(new BoosterGrant(
                    save.Boosters.Get(BoosterKind.ExtraSlot),
                    save.Boosters.Get(BoosterKind.Shuffle),
                    save.Boosters.Get(BoosterKind.Return),
                    save.Boosters.Get(BoosterKind.BloomBurst))),
                ["unlocks"] = new JObject { ["flags"] = flags, ["demosSeen"] = new JArray(ToObjects(save.Unlocks.DemosSeen)) },
                ["milestones"] = new JObject { ["claimed"] = new JArray(ToObjects(save.Milestones.Claimed)) },
                ["cosmetics"] = new JObject { ["owned"] = new JArray(ToObjects(save.Cosmetics.Owned)), ["equipped"] = equipped },
                ["daily"] = daily,
                ["collection"] = collection,
                ["settings"] = new JObject
                {
                    ["music"] = save.Settings.Music,
                    ["sfx"] = save.Settings.Sfx,
                    ["haptics"] = save.Settings.Haptics,
                    ["speed2x"] = save.Settings.Speed2x,
                    ["homePetals"] = save.Settings.HomePetals,
                    ["language"] = save.Settings.Language,
                },
            };

            if (save.LinkedIdentity != null)
            {
                root["linkedIdentity"] = save.LinkedIdentity;
            }

            if (save.DeviceId != null)
            {
                root["deviceId"] = save.DeviceId;
            }

            if (save.Stats.Counters.Count > 0 || save.Stats.Groups.Count > 0)
            {
                var stats = new JObject();
                foreach (KeyValuePair<string, long> counter in save.Stats.Counters)
                {
                    stats[counter.Key] = counter.Value;
                }

                foreach (KeyValuePair<string, SortedDictionary<string, long>> group in save.Stats.Groups)
                {
                    var inner = new JObject();
                    foreach (KeyValuePair<string, long> counter in group.Value)
                    {
                        inner[counter.Key] = counter.Value;
                    }

                    stats[group.Key] = inner;
                }

                root["stats"] = stats;
            }

            return root;
        }

        private static void ReadPurchases(JObject purchases, PurchasesData data)
        {
            JArray ledger = JsonDoc.Array(JsonDoc.Required(purchases, "purchases", "ledger"), "purchases.ledger");
            for (int i = 0; i < ledger.Count; i++)
            {
                string path = JsonDoc.Index("purchases.ledger", i);
                JObject entry = JsonDoc.Object(ledger[i], path);
                JsonDoc.AllowOnly(entry, path, "transactionId", "productId", "grantedAt", "grants");
                int petals = 0;
                BoosterGrant? boosters = null;
                JToken? grants = JsonDoc.Optional(entry, "grants");
                if (grants != null)
                {
                    string grantsPath = JsonDoc.Join(path, "grants");
                    JObject grantsObj = JsonDoc.Object(grants, grantsPath);
                    JsonDoc.AllowOnly(grantsObj, grantsPath, "petals", "boosters");
                    JToken? petalsToken = JsonDoc.Optional(grantsObj, "petals");
                    petals = petalsToken == null ? 0 : JsonDoc.Int(petalsToken, JsonDoc.Join(grantsPath, "petals"), min: 0);
                    JToken? boostersToken = JsonDoc.Optional(grantsObj, "boosters");
                    if (boostersToken != null)
                    {
                        string boostersPath = JsonDoc.Join(grantsPath, "boosters");
                        JObject boostersObj = JsonDoc.Object(boostersToken, boostersPath);
                        JsonDoc.AllowOnly(boostersObj, boostersPath, BoosterNames);
                        boosters = ReadBoosters(boostersObj, boostersPath);
                    }
                }

                var ledgerEntry = new LedgerEntry(
                    JsonDoc.String(JsonDoc.Required(entry, path, "transactionId"), JsonDoc.Join(path, "transactionId")),
                    JsonDoc.String(JsonDoc.Required(entry, path, "productId"), JsonDoc.Join(path, "productId")),
                    JsonDoc.String(JsonDoc.Required(entry, path, "grantedAt"), JsonDoc.Join(path, "grantedAt")),
                    petals,
                    boosters);
                if (!data.TryAdd(ledgerEntry))
                {
                    throw new ContentFormatException(JsonDoc.Join(path, "transactionId"), $"transaction '{ledgerEntry.TransactionId}' appears twice");
                }
            }

            JObject entitlements = JsonDoc.Object(JsonDoc.Required(purchases, "purchases", "entitlements"), "purchases.entitlements");
            JsonDoc.AllowOnly(entitlements, "purchases.entitlements", "removeAds");
            data.RemoveAds = JsonDoc.Bool(JsonDoc.Required(entitlements, "purchases.entitlements", "removeAds"), "purchases.entitlements.removeAds");
            JToken? offered = JsonDoc.Optional(purchases, "starterPackOffered");
            data.StarterPackOffered = offered != null && JsonDoc.Bool(offered, "purchases.starterPackOffered");
        }

        private static BoosterGrant ReadBoosters(JObject obj, string path)
        {
            int Count(string name)
            {
                JToken? token = JsonDoc.Optional(obj, name);
                return token == null ? 0 : JsonDoc.Int(token, JsonDoc.Join(path, name), min: 0);
            }

            return new BoosterGrant(Count("extraSlot"), Count("shuffle"), Count("return"), Count("bloomBurst"));
        }

        private static JObject BoostersObject(BoosterGrant grant) => new JObject
        {
            ["extraSlot"] = grant.ExtraSlot,
            ["shuffle"] = grant.Shuffle,
            ["return"] = grant.Return,
            ["bloomBurst"] = grant.BloomBurst,
        };

        private static JObject Obj(JObject root, string name, params string[] allowed)
        {
            JObject obj = JsonDoc.Object(JsonDoc.Required(root, string.Empty, name), name);
            JsonDoc.AllowOnly(obj, name, allowed);
            return obj;
        }

        private static IEnumerable<string> UniqueStrings(JObject obj, string path, string name)
        {
            string listPath = JsonDoc.Join(path, name);
            JArray array = JsonDoc.Array(JsonDoc.Required(obj, path, name), listPath);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                string value = JsonDoc.String(array[i], JsonDoc.Index(listPath, i));
                if (!seen.Add(value))
                {
                    throw new ContentFormatException(JsonDoc.Index(listPath, i), $"'{value}' appears twice");
                }

                yield return value;
            }
        }

        private static string? OptionalString(JObject obj, string path, string name)
        {
            JToken? token = JsonDoc.Optional(obj, name);
            return token == null ? null : JsonDoc.String(token, JsonDoc.Join(path, name));
        }

        private static object[] ToObjects<T>(IEnumerable<T> values)
        {
            var list = new List<object>();
            foreach (T value in values)
            {
                list.Add(value!);
            }

            return list.ToArray();
        }
    }
}
