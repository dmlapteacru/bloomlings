using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Economy.Online
{
    /// <summary>Facts derived by replay against caller-supplied trusted content. This is not reward authorization.</summary>
    public sealed record ValidatedWinEvidence(int Level, DifficultyClass Difficulty, int ActualBoostersUsed);

    /// <summary>
    /// Immutable, bounded replay envelope for one completed attempt. Validation is preflight, not authenticity:
    /// a caller can fabricate a valid solution or replay it again. A server must separately enforce trusted attempt
    /// permits, duplicate claims and paid booster entitlements. No time bonus or free-demo allowance is accepted here.
    /// </summary>
    public sealed class WinEvidence
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxBytes = 1024 * 1024;
        public const int MaxCommands = 20000;

        private readonly string _json;
        public int SchemaVersion => CurrentSchemaVersion;
        public Guid AttemptId { get; }
        public int Level { get; }
        public int ContentVersion { get; }
        public string DefinitionHash { get; }
        public string PictureHash { get; }
        public string FinalStateHash { get; }
        public IReadOnlyList<string> Commands { get; }
        public string Hash { get; }

        private WinEvidence(Guid attemptId, int level, int contentVersion, string definitionHash,
            string pictureHash, string finalStateHash, string[] commands)
        {
            AttemptId = attemptId; Level = level; ContentVersion = contentVersion;
            DefinitionHash = definitionHash; PictureHash = pictureHash; FinalStateHash = finalStateHash;
            Commands = Array.AsReadOnly(commands);
            var list = new JArray();
            foreach (string command in commands) list.Add(command);
            _json = CanonicalJson.Write(new JObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["attemptId"] = AttemptId.ToString("N"),
                ["level"] = Level,
                ["contentVersion"] = ContentVersion,
                ["definitionHash"] = DefinitionHash,
                ["pictureHash"] = PictureHash,
                ["finalStateHash"] = FinalStateHash,
                ["commands"] = list,
            }, false);
            RequireSize(_json);
            Hash = Sha256(_json);
        }

        public static WinEvidence Create(LevelSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            Require(session.Status == LevelStatus.Won, "only a won session can produce evidence");
            Require(session.CommandLog.Count <= MaxCommands, "too many commands");
            Require(session.Definition.LevelNumber >= 1 && session.Options.ContentVersion >= 0, "invalid level or content version");
            var commands = new string[session.CommandLog.Count];
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i] = CommandText.Format(session.CommandLog[i]);
                ParseCommand(commands[i]);
            }
            return new WinEvidence(Guid.NewGuid(), session.Definition.LevelNumber, session.Options.ContentVersion,
                Sha256(DefinitionJson.Write(session.Definition, false)), Sha256(BasePictureJson.Write(session.Picture, false)),
                session.StateHash.ToString("x16", CultureInfo.InvariantCulture), commands);
        }

        /// <summary>Canonical JSON; no mutable JSON document or session object escapes the envelope.</summary>
        public string Write() => _json;

        public static WinEvidence Read(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            RequireSize(json);
            JObject root = JsonDoc.ParseObject(json, "winEvidence");
            JsonDoc.AllowOnly(root, "winEvidence", "schemaVersion", "attemptId", "level", "contentVersion",
                "definitionHash", "pictureHash", "finalStateHash", "commands");
            Require(Integer(root, "schemaVersion", 1) == CurrentSchemaVersion, "unsupported schema version");
            string id = Text(root, "attemptId");
            Require(Guid.TryParseExact(id, "N", out Guid attemptId) && attemptId != Guid.Empty, "invalid attempt ID");
            int level = Integer(root, "level", 1);
            int version = Integer(root, "contentVersion", 0);
            string definitionHash = ReadHash(root, "definitionHash", 64);
            string pictureHash = ReadHash(root, "pictureHash", 64);
            string finalHash = ReadHash(root, "finalStateHash", 16);
            JArray list = JsonDoc.Array(JsonDoc.Required(root, "winEvidence", "commands"), "commands", maxItems: MaxCommands);
            var commands = new string[list.Count];
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i] = JsonDoc.String(list[i], JsonDoc.Index("commands", i));
                ParseCommand(commands[i]);
            }
            return new WinEvidence(attemptId, level, version, definitionHash, pictureHash, finalHash, commands);
        }

        /// <summary>
        /// Replay only with definitions, pictures and options obtained from a trusted catalog, never from this
        /// envelope's sender. Boosters are counted by the simulation; they are not proof of entitlement to use them.
        /// </summary>
        public ValidatedWinEvidence ValidateAndReplay(LevelDefinition trustedDefinition, BasePicture trustedPicture,
            SessionOptions trustedOptions)
        {
            if (trustedDefinition == null) throw new ArgumentNullException(nameof(trustedDefinition));
            if (trustedPicture == null) throw new ArgumentNullException(nameof(trustedPicture));
            if (trustedOptions == null) throw new ArgumentNullException(nameof(trustedOptions));
            Require(Level == trustedDefinition.LevelNumber, "level does not match trusted content");
            Require(ContentVersion == trustedOptions.ContentVersion, "content version does not match trusted options");
            Require(DefinitionHash == Sha256(DefinitionJson.Write(trustedDefinition, false)), "definition hash mismatch");
            Require(PictureHash == Sha256(BasePictureJson.Write(trustedPicture, false)), "picture hash mismatch");
            LevelSession replay = LevelSession.Load(trustedDefinition, trustedPicture, trustedOptions);
            foreach (string text in Commands)
                Require(replay.Apply(ParseCommand(text)).Accepted, "replay contains a rejected command");
            Require(replay.Status == LevelStatus.Won, "replay did not win");
            Require(replay.StateHash.ToString("x16", CultureInfo.InvariantCulture) == FinalStateHash, "final state hash mismatch");
            return new ValidatedWinEvidence(trustedDefinition.LevelNumber, trustedDefinition.Difficulty.Class, replay.BoostersUsed);
        }

        private static Command ParseCommand(string text)
        {
            Command command;
            try { command = CommandText.Parse(text); }
            catch (FormatException) { throw new ContentFormatException("commands", "invalid command"); }
            // Restart clears LevelSession.CommandLog; it cannot be part of one accepted attempt log.
            Require(!(command is Restart) && CommandText.Format(command) == text, "command is not an accepted attempt-log command");
            return command;
        }

        private static int Integer(JObject root, string key, int min)
        {
            try { return JsonDoc.Int(JsonDoc.Required(root, "winEvidence", key), key, min); }
            catch (OverflowException) { throw new ContentFormatException(key, "integer out of range"); }
        }
        private static string Text(JObject root, string key) => JsonDoc.String(JsonDoc.Required(root, "winEvidence", key), key);
        private static string ReadHash(JObject root, string key, int length)
        {
            string hash = Text(root, key);
            Require(hash.Length == length, "invalid " + key);
            foreach (char c in hash) Require((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'), "invalid " + key);
            return hash;
        }
        private static string Sha256(string text) => PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text));
        private static void RequireSize(string json) => Require(json.Length <= MaxBytes && Encoding.UTF8.GetByteCount(json) <= MaxBytes, "evidence exceeds 1 MiB");
        private static void Require(bool valid, string message)
        {
            if (!valid) throw new ContentFormatException("winEvidence", message);
        }
    }
}
