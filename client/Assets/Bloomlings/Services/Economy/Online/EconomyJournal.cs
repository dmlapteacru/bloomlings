using System;
using System.IO;
using System.Text;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>One exclusive writer per owner. Snapshot, cache, queue ACK and sequence are one atomic document.</summary>
    public sealed class EconomyJournal : IDisposable
    {
        private const int MaxBytes = 2 * 1024 * 1024;
        private readonly FileStream _lock;
        private bool _disposed;
        private bool _recovering;
        internal JObject Data { get; private set; }
        public string OwnerId { get; }
        public string MainPath { get; }
        public bool RecoveredFromBackup { get; }
        internal Action? AfterTempWrite { get; set; }
        internal string Folder => Path.GetDirectoryName(MainPath)!;
        public string InstallationId => (string)Data["installationId"]!;
        public int PendingCount => ((JArray)Data["pending"]!).Count;
        public long? NextSequence => Data["nextSequence"]?.Type == JTokenType.Null ? null : (long?)Data["nextSequence"];
        public EconomyCatalog Catalog => new EconomyCatalog((JObject)Data["catalog"]!);
        public EconomySnapshot? Snapshot => Data["snapshot"] is JObject json ? new EconomySnapshot(json) : null;

        public EconomyJournal(string root, string ownerId, EconomyCatalog bundled) : this(root, ownerId, bundled, null) { }
        internal EconomyJournal(string root, string ownerId, EconomyCatalog bundled, Action? recoveryWriteHook)
        {
            EconomyProtocol.Require(EconomyProtocol.IsId(ownerId), "invalid owner");
            OwnerId = ownerId;
            string key = Digest(ownerId);
            string folder = Path.Combine(root, key);
            bool ownerFolderExisted = Directory.Exists(folder);
            Directory.CreateDirectory(folder);
            MainPath = Path.Combine(folder, "economy-client-v1.json");
            _lock = new FileStream(MainPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Data = new JObject();
            try
            {
                if (TryRead(MainPath, out JObject? main)) Data = main!;
                else if (TryRead(MainPath + ".bak", out JObject? backup)) { Data = backup!; RecoveredFromBackup = true; }
                else
                {
                    if (ownerFolderExisted || File.Exists(MainPath) || File.Exists(MainPath + ".bak") || File.Exists(MainPath + ".tmp"))
                        throw new InvalidDataException("Economy journal is unreadable; refusing to reset request identity.");
                    Data = new JObject { ["format"] = 1, ["ownerId"] = ownerId, ["installationId"] = Guid.NewGuid().ToString("N"), ["nextSequence"] = 1,
                        ["catalog"] = bundled.ToJson(), ["snapshot"] = null, ["pending"] = new JArray(), ["rewards"] = new JObject(), ["outfit"] = new JObject(), ["lastResult"] = null };
                    Commit(_ => { }); // persist installation identity before any request, even read
                }
                Validate(Data);
                if (RecoveredFromBackup)
                {
                    // A rolled-back backup cannot prove a pending request was never dispatched.
                    _recovering = true;
                    AfterTempWrite = recoveryWriteHook;
                    Commit(doc => { foreach (JObject entry in (JArray)doc["pending"]!) entry["uncertain"] = true; });
                    AfterTempWrite = null;
                }
            }
            catch { _lock.Dispose(); throw; }
        }

        internal void Commit(Action<JObject> change)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EconomyJournal));
            var next = (JObject)Data.DeepClone(); change(next); Validate(next);
            string document = CanonicalJson.Write(next, false);
            byte[] bytes = Encoding.UTF8.GetBytes(CanonicalJson.Write(new JObject { ["checksum"] = Digest(document), ["data"] = next }, false));
            if (bytes.Length > MaxBytes) throw new InvalidDataException("Economy journal size limit reached.");
            using (var stream = new FileStream(MainPath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            AfterTempWrite?.Invoke();
            if (_recovering)
            {
                // Keep the known-good backup intact. A crash before the rename leaves it recoverable again;
                // no trusted main is ever installed before uncertain flags are in the durable document.
                if (File.Exists(MainPath)) File.Delete(MainPath);
                File.Move(MainPath + ".tmp", MainPath);
                _recovering = false;
            }
            else if (File.Exists(MainPath)) File.Replace(MainPath + ".tmp", MainPath, MainPath + ".bak");
            else File.Move(MainPath + ".tmp", MainPath);
            Data = next;
        }

        internal string StoreEvidence(string json)
        {
            EconomyProtocol.Require(Encoding.UTF8.GetByteCount(json) <= 1024 * 1024, "reward evidence too large");
            JsonDoc.ParseObject(json, "reward evidence");
            string hash = Digest(json);
            string folder = Path.Combine(Folder, "evidence"); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, hash + ".json");
            if (File.Exists(path)) { if (Digest(File.ReadAllText(path, Encoding.UTF8)) != hash) throw new InvalidDataException("Corrupt reward evidence."); return hash; }
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            File.Move(path + ".tmp", path);
            return hash;
        }
        public string ReadRewardEvidence(string claimId)
        {
            JObject? reward = ((JObject)Data["rewards"]!)[claimId] as JObject;
            if (reward == null) throw new ArgumentException("Unknown reward.", nameof(claimId));
            string hash = (string)reward["evidenceHash"]!;
            string json = File.ReadAllText(Path.Combine(Folder, "evidence", hash + ".json"), Encoding.UTF8);
            if (Digest(json) != hash) throw new InvalidDataException("Corrupt reward evidence.");
            return json;
        }
        private bool TryRead(string path, out JObject? data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                if (new FileInfo(path).Length > MaxBytes) return false;
                JObject envelope = JsonDoc.ParseObject(File.ReadAllText(path, Encoding.UTF8), "economy journal");
                JsonDoc.AllowOnly(envelope, "envelope", "checksum", "data");
                var candidate = EconomyProtocol.Obj(envelope["data"]);
                if ((string?)envelope["checksum"] != Digest(CanonicalJson.Write(candidate, false))) return false;
                Validate(candidate); data = candidate; return true;
            }
            catch (ContentFormatException) { return false; }
            catch (IOException) { return false; }
        }
        private void Validate(JObject data)
        {
            JsonDoc.AllowOnly(data, "journal", "format", "ownerId", "installationId", "nextSequence", "catalog", "snapshot", "pending", "rewards", "outfit", "lastResult");
            EconomyProtocol.Require(EconomyProtocol.Number(data["format"]) == 1 && EconomyProtocol.Id(data["ownerId"]) == OwnerId, "journal owner/format mismatch");
            string installation = EconomyProtocol.Id(data["installationId"]);
            long? nextSequence = data["nextSequence"]?.Type == JTokenType.Null ? null : EconomyProtocol.Number(data["nextSequence"], 1);
            _ = new EconomyCatalog(EconomyProtocol.Obj(data["catalog"]));
            if (data["snapshot"] is JObject state) EconomyProtocol.Require(new EconomySnapshot(state).OwnerId == OwnerId, "snapshot owner mismatch");
            else EconomyProtocol.Require(data["snapshot"]?.Type == JTokenType.Null, "invalid cached snapshot");
            long previous = 0;
            foreach (JObject entry in EconomyProtocol.Array(data["pending"], 128))
            {
                JsonDoc.AllowOnly(entry, "entry", "operation", "attempted", "blockCode", "rewardId", "alternatives", "uncertain");
                string json = entry["operation"]?.Type == JTokenType.String ? (string)entry["operation"]! : throw new ContentFormatException("entry", "missing operation");
                JObject operation = JsonDoc.ParseObject(json, "operation");
                long sequence = EconomyProtocol.Number(operation["sequence"], 1);
                EconomyProtocol.Require(EconomyProtocol.Id(operation["installationId"]) == installation && sequence > previous && (nextSequence == null || sequence < nextSequence), "invalid queue sequence");
                previous = sequence; EconomyProtocol.Bool(entry["attempted"]); EconomyProtocol.Bool(entry["uncertain"]);
                EconomyProtocol.Require(entry["blockCode"]?.Type == JTokenType.Null || EconomyProtocol.IsId((string?)entry["blockCode"]), "invalid queue block");
                if (entry["rewardId"]?.Type != JTokenType.Null)
                {
                    string rewardId = EconomyProtocol.Id(entry["rewardId"]);
                    EconomyProtocol.Require(((JObject)data["rewards"]!)[rewardId] is JObject, "queue references unknown reward");
                }
                foreach (JToken old in EconomyProtocol.Array(entry["alternatives"], 5))
                {
                    JObject alternative = JsonDoc.ParseObject(old.Value<string>()!, "prior operation");
                    EconomyProtocol.Require((long?)alternative["sequence"] == sequence && (string?)alternative["installationId"] == installation, "invalid prior operation");
                }
            }
            JObject rewards = EconomyProtocol.Obj(data["rewards"]);
            EconomyProtocol.Require(rewards.Count <= 10000, "too many reward intents");
            foreach (JProperty property in rewards.Properties())
            {
                EconomyProtocol.Require(EconomyProtocol.IsId(property.Name), "invalid reward ID");
                JObject reward = EconomyProtocol.Obj(property.Value);
                JsonDoc.AllowOnly(reward, "reward", "kind", "level", "evidenceHash", "settled");
                EconomyProtocol.Require((string?)reward["kind"] == "claim_level" || (string?)reward["kind"] == "claim_challenge", "invalid reward kind");
                EconomyProtocol.Number(reward["level"], 1, 2500);
                EconomyProtocol.Require(RegexHash((string?)reward["evidenceHash"]), "invalid evidence hash");
                EconomyProtocol.Bool(reward["settled"]);
            }
            foreach (JProperty slot in EconomyProtocol.Obj(data["outfit"]).Properties())
                EconomyProtocol.Require(EconomyProtocol.IsId(slot.Name) && EconomyProtocol.IsId((string?)slot.Value), "invalid outfit selection");
        }
        private static bool RegexHash(string? text) => text != null && System.Text.RegularExpressions.Regex.IsMatch(text, "^[a-f0-9]{64}$");
        internal static string Digest(string text) => PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text));
        public void Dispose() { if (_disposed) return; _disposed = true; _lock.Dispose(); }
    }
}
