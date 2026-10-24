using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>Engine/SDK-free readers for ExecuteEconomy. No reflection or mutable server documents escape.</summary>
    public static class EconomyProtocol
    {
        public const long MaxSafeInteger = 9007199254740991L;
        public static readonly string[] Boosters = { "extraSlot", "shuffle", "return", "bloomBurst" };
        public static bool IsId(string? value) => value != null && Regex.IsMatch(value, "^[a-zA-Z0-9_.:-]{1,128}$") && value != "__proto__" && value != "constructor" && value != "prototype";
        internal static void Require(bool valid, string message) { if (!valid) throw new ContentFormatException("economy", message); }
        internal static string Id(JToken? value)
        {
            Require(value?.Type == JTokenType.String && IsId((string?)value), "invalid identifier");
            return (string)value!;
        }
        internal static long Number(JToken? value, long min = 0, long max = MaxSafeInteger)
        {
            Require(value?.Type == JTokenType.Integer, "expected integer");
            long number;
            try { number = value!.Value<long>(); }
            catch (Exception) { throw new ContentFormatException("economy", "integer out of range"); }
            Require(number >= min && number <= max, "integer out of range");
            return number;
        }
        internal static bool Bool(JToken? value) { Require(value?.Type == JTokenType.Boolean, "expected boolean"); return (bool)value!; }
        internal static JObject Obj(JToken? value) { Require(value is JObject, "expected object"); return (JObject)value!; }
        internal static JArray Array(JToken? value, int max = 10000) { Require(value is JArray a && a.Count <= max, "invalid array"); return (JArray)value!; }
        internal static void Day(JToken? value)
        {
            if (value == null || value.Type == JTokenType.Null) return;
            Require(value.Type == JTokenType.String && DateTime.TryParseExact((string?)value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _), "invalid UTC date");
        }
    }

    public sealed record EconomyItemPrice(string Id, int Price, bool Free, int MinLevel);
    public sealed record EconomyBoosterPrice(int Price, int Quantity, int MinLevel);
    public sealed record EconomyQuote(string Kind, string Target, string CatalogVersion, int Price);

    public sealed class EconomyCatalog
    {
        private readonly JObject _json;
        private readonly Dictionary<string, EconomyItemPrice> _items = new Dictionary<string, EconomyItemPrice>(StringComparer.Ordinal);
        private readonly Dictionary<string, EconomyBoosterPrice> _boosters = new Dictionary<string, EconomyBoosterPrice>(StringComparer.Ordinal);
        public string Version { get; }
        public System.Collections.Generic.IEnumerable<EconomyItemPrice> Items => _items.Values;
        public EconomyCatalog(JObject json)
        {
            JsonDoc.AllowOnly(json, "catalog", "version", "cosmetics", "boosters");
            Version = EconomyProtocol.Id(json["version"]);
            JObject cosmetics = EconomyProtocol.Obj(json["cosmetics"]);
            EconomyProtocol.Require(cosmetics.Count <= 4096, "catalog too large");
            foreach (JProperty property in cosmetics.Properties())
            {
                EconomyProtocol.Require(EconomyProtocol.IsId(property.Name), "invalid item ID");
                JObject item = EconomyProtocol.Obj(property.Value);
                JsonDoc.AllowOnly(item, "item", "price", "free", "minLevel");
                _items.Add(property.Name, new EconomyItemPrice(property.Name, (int)EconomyProtocol.Number(item["price"], 0, int.MaxValue), EconomyProtocol.Bool(item["free"]), (int)EconomyProtocol.Number(item["minLevel"], 1, 2501)));
            }
            JObject boosters = EconomyProtocol.Obj(json["boosters"]);
            JsonDoc.AllowOnly(boosters, "boosters", EconomyProtocol.Boosters);
            foreach (string kind in EconomyProtocol.Boosters)
            {
                JObject booster = EconomyProtocol.Obj(boosters[kind]);
                JsonDoc.AllowOnly(booster, "booster", "price", "quantity", "minLevel");
                _boosters.Add(kind, new EconomyBoosterPrice((int)EconomyProtocol.Number(booster["price"], 1, int.MaxValue), (int)EconomyProtocol.Number(booster["quantity"], 1, 999999), (int)EconomyProtocol.Number(booster["minLevel"], 1, 2501)));
            }
            _json = (JObject)json.DeepClone();
        }
        public EconomyItemPrice? Item(string id) => _items.TryGetValue(id, out var item) ? item : null;
        public EconomyBoosterPrice? Booster(string kind) => _boosters.TryGetValue(kind, out var price) ? price : null;
        public EconomyQuote? QuoteItem(string id) => Item(id) is EconomyItemPrice item && !item.Free && item.Price > 0 ? new EconomyQuote("buy_cosmetic", id, Version, item.Price) : null;
        public EconomyQuote? QuoteBooster(string kind, bool buyAndConsume = false) => Booster(kind) is EconomyBoosterPrice price ? new EconomyQuote(buyAndConsume ? "consume_booster" : "buy_booster", kind, Version, price.Price) : null;
        internal JObject ToJson() => (JObject)_json.DeepClone();
        public static EconomyCatalog Parse(string json) => new EconomyCatalog(JsonDoc.ParseObject(json, "catalog"));
    }

    public sealed class EconomySnapshot
    {
        private readonly JObject _json;
        private readonly HashSet<string> _owned;
        public string OwnerId { get; }
        public long Revision { get; }
        public int Petals { get; }
        public int HighestCompletedLevel { get; }
        public string CatalogVersion { get; }
        public EconomySnapshot(JObject json)
        {
            JsonDoc.AllowOnly(json, "snapshot", "schemaVersion", "ownerId", "catalogVersion", "revision", "petals", "highestCompletedLevel", "boosters", "owned", "milestones", "daily", "challengeDate", "boosterAdDate");
            EconomyProtocol.Require(EconomyProtocol.Number(json["schemaVersion"]) == 1, "unsupported snapshot");
            OwnerId = EconomyProtocol.Id(json["ownerId"]);
            CatalogVersion = EconomyProtocol.Id(json["catalogVersion"]);
            Revision = EconomyProtocol.Number(json["revision"]);
            Petals = (int)EconomyProtocol.Number(json["petals"], 0, int.MaxValue);
            HighestCompletedLevel = (int)EconomyProtocol.Number(json["highestCompletedLevel"], 0, 2500);
            JObject boosters = EconomyProtocol.Obj(json["boosters"]);
            JsonDoc.AllowOnly(boosters, "boosters", EconomyProtocol.Boosters);
            foreach (string kind in EconomyProtocol.Boosters) EconomyProtocol.Number(boosters[kind], 0, 999999);
            _owned = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken item in EconomyProtocol.Array(json["owned"], 4096)) EconomyProtocol.Require(_owned.Add(EconomyProtocol.Id(item)), "duplicate item");
            var milestones = new HashSet<long>();
            foreach (JToken item in EconomyProtocol.Array(json["milestones"], 2500)) EconomyProtocol.Require(milestones.Add(EconomyProtocol.Number(item, 1, HighestCompletedLevel)), "duplicate milestone");
            JObject daily = EconomyProtocol.Obj(json["daily"]);
            JsonDoc.AllowOnly(daily, "daily", "date", "step");
            EconomyProtocol.Day(daily["date"]);
            EconomyProtocol.Number(daily["step"], 0, 5);
            EconomyProtocol.Require(daily["date"]?.Type != JTokenType.Null || (long?)daily["step"] == 0, "claims without date");
            EconomyProtocol.Day(json["challengeDate"]); EconomyProtocol.Day(json["boosterAdDate"]);
            _json = (JObject)json.DeepClone();
        }
        public string? DailyDate => (string?)_json["daily"]!["date"];
        public int DailyStep => (int)_json["daily"]!["step"]!;
        public string? ChallengeDate => (string?)_json["challengeDate"];
        public string? BoosterAdDate => (string?)_json["boosterAdDate"];
        public bool HasMilestone(int level) { foreach (JToken n in (JArray)_json["milestones"]!) if ((int)n == level) return true; return false; }
        public System.Collections.Generic.IEnumerable<string> OwnedIds => _owned;
        public bool Owns(string item) => _owned.Contains(item);
        public int Charges(string kind) => EconomyProtocol.Boosters.Contains(kind) ? (int)_json["boosters"]![kind]! : 0;
        internal JObject ToJson() => (JObject)_json.DeepClone();
    }

    public sealed class EconomyReply
    {
        public bool Ok { get; }
        public string Code { get; }
        public bool Consumed { get; }
        public bool Retryable { get; }
        public long? NextSequence { get; }
        public EconomySnapshot? State { get; }
        public EconomyCatalog? Catalog { get; }
        public string? CatalogVersion { get; }
        public string? ServerUtcDate { get; }
        public EconomyReply(JObject json)
        {
            JsonDoc.AllowOnly(json, "reply", "ok", "code", "consumed", "retryable", "replayed", "effects", "state", "nextSequence", "catalogVersion", "catalog", "serverUtcDate");
            if (json["serverUtcDate"] != null) { EconomyProtocol.Day(json["serverUtcDate"]); ServerUtcDate = (string?)json["serverUtcDate"]; }
            Ok = EconomyProtocol.Bool(json["ok"]); Code = EconomyProtocol.Id(json["code"]);
            Consumed = EconomyProtocol.Bool(json["consumed"]); Retryable = EconomyProtocol.Bool(json["retryable"]);
            EconomyProtocol.Require(!Consumed || !Retryable, "ambiguous acknowledgement");
            if (json["state"] is JObject state) State = new EconomySnapshot(state);
            else EconomyProtocol.Require(json["state"] == null || json["state"]!.Type == JTokenType.Null, "invalid snapshot");
            if (json["nextSequence"]?.Type != JTokenType.Null && json["nextSequence"] != null) NextSequence = EconomyProtocol.Number(json["nextSequence"], 1);
            if (json["catalogVersion"]?.Type != JTokenType.Null && json["catalogVersion"] != null) CatalogVersion = EconomyProtocol.Id(json["catalogVersion"]);
            if (json["catalog"] is JObject catalog)
            {
                EconomyProtocol.Require(CatalogVersion != null, "catalog without version");
                Catalog = new EconomyCatalog(new JObject { ["version"] = CatalogVersion, ["cosmetics"] = catalog["cosmetics"]?.DeepClone(), ["boosters"] = catalog["boosters"]?.DeepClone() });
            }
            EconomyProtocol.Require(!Consumed || State != null, "acknowledgement without state");
            EconomyProtocol.Require(!(Consumed || (Ok && Code == "loaded")) || json.Property("nextSequence") != null, "missing server cursor");
        }
        public static EconomyReply Parse(string json) => new EconomyReply(JsonDoc.ParseObject(json, "economy reply"));
        public static EconomyReply Unavailable { get; } = Parse("{\"ok\":false,\"code\":\"temporarily-unavailable\",\"consumed\":false,\"retryable\":true,\"state\":null}");
    }
}
