using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bloomlings.Content.Json;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Rounds
{
    /// <summary>
    /// Reads and writes the round formats of spec 006 (<c>round-level.v1</c>, <c>round-picture.v1</c>,
    /// <c>round-proof.v1</c>; contracts in <c>specs/006-round-levels-in-game/contracts/</c>, embedded as
    /// <see cref="Schemas.SchemaResources.RoundLevel"/> and its siblings) and reads the conformance fixtures
    /// (<c>round-conformance.v1</c>). Like the legacy readers, the checks are written by hand from the schemas (no schema
    /// library in the runtime): unknown properties, missing required ones, ranges, constants and patterns are refused with
    /// the JSON path; the pipeline evaluates the embedded schemas as well. Beyond the schemas, the readers check what the
    /// model needs to be usable: rows of the declared size, entry and bridge cells on the board. The definition rules
    /// (accounting, tray partition, groups …) are <c>RoundLevelValidator</c>'s. Writing is canonical (sorted keys).
    /// </summary>
    public static class RoundJson
    {
        public const string LevelFormat = "round-level.v1";

        public const string PictureFormat = "round-picture.v1";

        public const string ProofFormat = "round-proof.v1";

        public const string HeroId = "sprig-bridge";

        private const string Codes = RoundMaterial.Codes;

        private static readonly Regex PictureId = new Regex("^P[0-9]{4}$", RegexOptions.CultureInvariant);

        private static readonly Regex LevelRow = new Regex("^[" + Codes + ".#]+$", RegexOptions.CultureInvariant);

        private static readonly Regex PictureRow = new Regex("^[" + Codes + ".#b]+$", RegexOptions.CultureInvariant);

        private static readonly Regex PodCode = new Regex("^[" + Codes + "]$", RegexOptions.CultureInvariant);

        private static readonly Regex Group = new Regex("^[A-Z][0-9]*$", RegexOptions.CultureInvariant);

        private static readonly Regex Sha256 = new Regex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant);

        private static readonly Regex Date = new Regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.CultureInvariant);

        private static readonly string[] Mechanics = { "hidden", "links", "bridge" };

        // ---------------------------------------------------------------- round-level.v1

        public static RoundLevel ReadLevel(string json) => ReadLevel(JsonDoc.ParseObject(json, LevelFormat), string.Empty);

        public static RoundLevel ReadLevel(JObject root, string path)
        {
            JsonDoc.AllowOnly(root, path, "format", "number", "definitionVersion", "class", "introduction", "picture", "board", "capacity", "pods", "tray", "mechanics", "exception", "engine");
            Const(JsonDoc.Required(root, path, "format"), JsonDoc.Join(path, "format"), LevelFormat);
            int number = JsonDoc.Int(JsonDoc.Required(root, path, "number"), JsonDoc.Join(path, "number"), min: 1);
            int definitionVersion = JsonDoc.Int(JsonDoc.Required(root, path, "definitionVersion"), JsonDoc.Join(path, "definitionVersion"), min: 1);
            RoundClass roundClass = ParseClass(JsonDoc.Required(root, path, "class"), JsonDoc.Join(path, "class"));

            RoundIntroduction? introduction = null;
            JToken? introToken = JsonDoc.Optional(root, "introduction");
            if (introToken != null)
            {
                string at = JsonDoc.Join(path, "introduction");
                JObject intro = JsonDoc.Object(introToken, at);
                JsonDoc.AllowOnly(intro, at, "mechanic", "second");
                introduction = new RoundIntroduction(
                    OneOf(JsonDoc.Required(intro, at, "mechanic"), JsonDoc.Join(at, "mechanic"), Mechanics),
                    JsonDoc.Bool(JsonDoc.Required(intro, at, "second"), JsonDoc.Join(at, "second")));
            }

            string picturePath = JsonDoc.Join(path, "picture");
            JObject picture = JsonDoc.Object(JsonDoc.Required(root, path, "picture"), picturePath);
            JsonDoc.AllowOnly(picture, picturePath, "id", "version");
            string pictureId = Match(JsonDoc.Required(picture, picturePath, "id"), JsonDoc.Join(picturePath, "id"), PictureId);
            int pictureVersion = JsonDoc.Int(JsonDoc.Required(picture, picturePath, "version"), JsonDoc.Join(picturePath, "version"), min: 1);

            string boardPath = JsonDoc.Join(path, "board");
            JObject board = JsonDoc.Object(JsonDoc.Required(root, path, "board"), boardPath);
            JsonDoc.AllowOnly(board, boardPath, "w", "h", "rowsBottomFirst", "entries", "hero");
            int w = JsonDoc.Int(JsonDoc.Required(board, boardPath, "w"), JsonDoc.Join(boardPath, "w"), 7, RoundLevel.MaxSide);
            int h = JsonDoc.Int(JsonDoc.Required(board, boardPath, "h"), JsonDoc.Join(boardPath, "h"), 8, RoundLevel.MaxSide);
            string rowsPath = JsonDoc.Join(boardPath, "rowsBottomFirst");
            List<string> rows = Strings(JsonDoc.Array(JsonDoc.Required(board, boardPath, "rowsBottomFirst"), rowsPath), rowsPath, LevelRow);
            CheckShape(rows, w, h, rowsPath);
            string entriesPath = JsonDoc.Join(boardPath, "entries");
            JArray entriesArray = JsonDoc.Array(JsonDoc.Required(board, boardPath, "entries"), entriesPath, minItems: 1);
            var entries = new int[entriesArray.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = Cell(entriesArray[i], JsonDoc.Index(entriesPath, i), w * h);
            }

            List<RoundBridgeChoice>? bridge = null;
            JToken? heroToken = JsonDoc.Optional(board, "hero");
            if (heroToken != null)
            {
                string heroPath = JsonDoc.Join(boardPath, "hero");
                JObject hero = JsonDoc.Object(heroToken, heroPath);
                JsonDoc.AllowOnly(hero, heroPath, "id", "choices");
                Const(JsonDoc.Required(hero, heroPath, "id"), JsonDoc.Join(heroPath, "id"), HeroId);
                string choicesPath = JsonDoc.Join(heroPath, "choices");
                JArray choices = JsonDoc.Array(JsonDoc.Required(hero, heroPath, "choices"), choicesPath, 2, 2);
                bridge = new List<RoundBridgeChoice>();
                for (int i = 0; i < choices.Count; i++)
                {
                    string at = JsonDoc.Index(choicesPath, i);
                    JObject choice = JsonDoc.Object(choices[i], at);
                    JsonDoc.AllowOnly(choice, at, "cell", "anchor", "label");
                    bridge.Add(new RoundBridgeChoice(
                        Cell(JsonDoc.Required(choice, at, "cell"), JsonDoc.Join(at, "cell"), w * h),
                        Cell(JsonDoc.Required(choice, at, "anchor"), JsonDoc.Join(at, "anchor"), w * h),
                        JsonDoc.String(JsonDoc.Required(choice, at, "label"), JsonDoc.Join(at, "label"))));
                }
            }

            int capacity = JsonDoc.Int(JsonDoc.Required(root, path, "capacity"), JsonDoc.Join(path, "capacity"));
            if (capacity != 5)
            {
                throw new ContentFormatException(JsonDoc.Join(path, "capacity"), $"must be 5, got {capacity}");
            }

            string podsPath = JsonDoc.Join(path, "pods");
            JArray podsArray = JsonDoc.Array(JsonDoc.Required(root, path, "pods"), podsPath, minItems: 2);
            var pods = new List<RoundPod>(podsArray.Count);
            for (int i = 0; i < podsArray.Count; i++)
            {
                string at = JsonDoc.Index(podsPath, i);
                JObject pod = JsonDoc.Object(podsArray[i], at);
                JsonDoc.AllowOnly(pod, at, "code", "count", "mystery", "group");
                string code = Match(JsonDoc.Required(pod, at, "code"), JsonDoc.Join(at, "code"), PodCode);
                int count = JsonDoc.Int(JsonDoc.Required(pod, at, "count"), JsonDoc.Join(at, "count"), 1, 60);
                RoundMystery mystery = RoundMystery.None;
                JToken? mysteryToken = JsonDoc.Optional(pod, "mystery");
                if (mysteryToken != null)
                {
                    Const(mysteryToken, JsonDoc.Join(at, "mystery"), "depth");
                    mystery = RoundMystery.Depth;
                }

                JToken? groupToken = JsonDoc.Optional(pod, "group");
                string? group = groupToken == null ? null : Match(groupToken, JsonDoc.Join(at, "group"), Group);
                pods.Add(new RoundPod(code[0], count, mystery, group));
            }

            string trayPath = JsonDoc.Join(path, "tray");
            JArray trayArray = JsonDoc.Array(JsonDoc.Required(root, path, "tray"), trayPath, 2, 6);
            var tray = new List<IReadOnlyList<int>>(trayArray.Count);
            for (int c = 0; c < trayArray.Count; c++)
            {
                string at = JsonDoc.Index(trayPath, c);
                JArray column = JsonDoc.Array(trayArray[c], at, minItems: 1);
                var indices = new int[column.Count];
                for (int k = 0; k < indices.Length; k++)
                {
                    indices[k] = JsonDoc.Int(column[k], JsonDoc.Index(at, k), min: 0);
                }

                tray.Add(indices);
            }

            string mechanicsPath = JsonDoc.Join(path, "mechanics");
            JArray mechanicsArray = JsonDoc.Array(JsonDoc.Required(root, path, "mechanics"), mechanicsPath);
            var mechanics = new List<string>(mechanicsArray.Count);
            for (int i = 0; i < mechanicsArray.Count; i++)
            {
                string mechanic = OneOf(mechanicsArray[i], JsonDoc.Index(mechanicsPath, i), Mechanics);
                if (mechanics.Contains(mechanic))
                {
                    throw new ContentFormatException(JsonDoc.Index(mechanicsPath, i), $"'{mechanic}' is listed twice");
                }

                mechanics.Add(mechanic);
            }

            RoundException? exception = null;
            JToken? exceptionToken = JsonDoc.Optional(root, "exception");
            if (exceptionToken != null)
            {
                string at = JsonDoc.Join(path, "exception");
                JObject ex = JsonDoc.Object(exceptionToken, at);
                JsonDoc.AllowOnly(ex, at, "rule", "reason", "decidedBy", "decidedAt");
                exception = new RoundException(
                    JsonDoc.String(JsonDoc.Required(ex, at, "rule"), JsonDoc.Join(at, "rule")),
                    JsonDoc.String(JsonDoc.Required(ex, at, "reason"), JsonDoc.Join(at, "reason")),
                    Const(JsonDoc.Required(ex, at, "decidedBy"), JsonDoc.Join(at, "decidedBy"), "owner"),
                    Match(JsonDoc.Required(ex, at, "decidedAt"), JsonDoc.Join(at, "decidedAt"), Date));
            }

            RoundEngineVersion engine = ReadEngine(JsonDoc.Required(root, path, "engine"), JsonDoc.Join(path, "engine"));

            return new RoundLevel(
                number,
                w,
                h,
                rows,
                pods,
                tray,
                entries,
                bridge,
                capacity,
                roundClass,
                definitionVersion,
                introduction,
                pictureId,
                pictureVersion,
                mechanics,
                exception,
                engine);
        }

        public static string WriteLevel(RoundLevel level, bool indented = true) => CanonicalJson.Write(ToJObject(level), indented);

        public static JObject ToJObject(RoundLevel level)
        {
            if (level.PictureId == null)
            {
                throw new ArgumentException($"L{level.Number}: round-level.v1 requires a picture reference.", nameof(level));
            }

            if (level.Engine == null)
            {
                throw new ArgumentException($"L{level.Number}: round-level.v1 requires the engine fingerprints.", nameof(level));
            }

            var board = new JObject
            {
                ["w"] = level.Width,
                ["h"] = level.Height,
                ["rowsBottomFirst"] = new JArray(ToArray(level.RowsBottomFirst)),
                ["entries"] = new JArray(ToArray(level.Entries)),
            };
            if (level.Bridge != null)
            {
                var choices = new JArray();
                foreach (RoundBridgeChoice choice in level.Bridge)
                {
                    choices.Add(new JObject { ["cell"] = choice.Cell, ["anchor"] = choice.Anchor, ["label"] = choice.Label });
                }

                board["hero"] = new JObject { ["id"] = HeroId, ["choices"] = choices };
            }

            var pods = new JArray();
            foreach (RoundPod pod in level.Pods)
            {
                var item = new JObject { ["code"] = pod.Code.ToString(), ["count"] = pod.Count };
                if (pod.Mystery == RoundMystery.Depth)
                {
                    item["mystery"] = "depth";
                }
                else if (pod.Mystery != RoundMystery.None)
                {
                    throw new ArgumentException($"L{level.Number}: round-level.v1 has only depth mystery pods, not {pod.Mystery}.", nameof(level));
                }

                if (pod.Group != null)
                {
                    item["group"] = pod.Group;
                }

                pods.Add(item);
            }

            var tray = new JArray();
            foreach (IReadOnlyList<int> column in level.Tray)
            {
                tray.Add(new JArray(ToArray(column)));
            }

            var root = new JObject
            {
                ["format"] = LevelFormat,
                ["number"] = level.Number,
                ["definitionVersion"] = level.DefinitionVersion,
                ["class"] = ClassName(level.Class),
                ["picture"] = new JObject { ["id"] = level.PictureId, ["version"] = level.PictureVersion },
                ["board"] = board,
                ["capacity"] = level.Capacity,
                ["pods"] = pods,
                ["tray"] = tray,
                ["mechanics"] = new JArray(ToArray(level.Mechanics)),
                ["engine"] = WriteEngine(level.Engine),
            };
            if (level.Introduction != null)
            {
                root["introduction"] = new JObject { ["mechanic"] = level.Introduction.Mechanic, ["second"] = level.Introduction.Second };
            }

            if (level.Exception != null)
            {
                root["exception"] = new JObject
                {
                    ["rule"] = level.Exception.Rule,
                    ["reason"] = level.Exception.Reason,
                    ["decidedBy"] = level.Exception.DecidedBy,
                    ["decidedAt"] = level.Exception.DecidedAt,
                };
            }

            return root;
        }

        // ---------------------------------------------------------------- round-picture.v1

        public static RoundPicture ReadPicture(string json) => ReadPicture(JsonDoc.ParseObject(json, PictureFormat), string.Empty);

        public static RoundPicture ReadPicture(JObject root, string path)
        {
            JsonDoc.AllowOnly(root, path, "format", "id", "version", "title", "family", "direction", "subject", "source", "w", "h", "rows", "approval", "rights");
            Const(JsonDoc.Required(root, path, "format"), JsonDoc.Join(path, "format"), PictureFormat);
            string id = Match(JsonDoc.Required(root, path, "id"), JsonDoc.Join(path, "id"), PictureId);
            int version = JsonDoc.Int(JsonDoc.Required(root, path, "version"), JsonDoc.Join(path, "version"), min: 1);
            string title = JsonDoc.String(JsonDoc.Required(root, path, "title"), JsonDoc.Join(path, "title"));
            if (title.Length == 0)
            {
                throw new ContentFormatException(JsonDoc.Join(path, "title"), "must not be empty");
            }

            int w = JsonDoc.Int(JsonDoc.Required(root, path, "w"), JsonDoc.Join(path, "w"), 1, RoundPicture.MaxSide);
            int h = JsonDoc.Int(JsonDoc.Required(root, path, "h"), JsonDoc.Join(path, "h"), 1, RoundPicture.MaxSide);
            string rowsPath = JsonDoc.Join(path, "rows");
            List<string> rows = Strings(JsonDoc.Array(JsonDoc.Required(root, path, "rows"), rowsPath, minItems: 1), rowsPath, PictureRow);
            CheckShape(rows, w, h, rowsPath);

            string approvalPath = JsonDoc.Join(path, "approval");
            JObject approval = JsonDoc.Object(JsonDoc.Required(root, path, "approval"), approvalPath);
            JsonDoc.AllowOnly(approval, approvalPath, "status", "by", "decidedAt");
            Const(JsonDoc.Required(approval, approvalPath, "status"), JsonDoc.Join(approvalPath, "status"), "approved");
            Const(JsonDoc.Required(approval, approvalPath, "by"), JsonDoc.Join(approvalPath, "by"), "owner");
            string decidedAt = JsonDoc.String(JsonDoc.Required(approval, approvalPath, "decidedAt"), JsonDoc.Join(approvalPath, "decidedAt"));
            Const(JsonDoc.Required(root, path, "rights"), JsonDoc.Join(path, "rights"), "own");

            return new RoundPicture(
                id,
                version,
                title,
                w,
                h,
                rows,
                decidedAt,
                OptionalString(root, path, "family"),
                OptionalString(root, path, "direction"),
                OptionalString(root, path, "subject"),
                OptionalString(root, path, "source"));
        }

        public static string WritePicture(RoundPicture picture, bool indented = true) => CanonicalJson.Write(ToJObject(picture), indented);

        public static JObject ToJObject(RoundPicture picture)
        {
            var root = new JObject
            {
                ["format"] = PictureFormat,
                ["id"] = picture.Id,
                ["version"] = picture.Version,
                ["title"] = picture.Title,
                ["w"] = picture.Width,
                ["h"] = picture.Height,
                ["rows"] = new JArray(ToArray(picture.RowsTopFirst)),
                ["approval"] = new JObject { ["status"] = "approved", ["by"] = "owner", ["decidedAt"] = picture.DecidedAt },
                ["rights"] = "own",
            };
            AddOptional(root, "family", picture.Family);
            AddOptional(root, "direction", picture.Direction);
            AddOptional(root, "subject", picture.Subject);
            AddOptional(root, "source", picture.Source);
            return root;
        }

        // ---------------------------------------------------------------- round-proof.v1

        public static RoundProof ReadProof(string json) => ReadProof(JsonDoc.ParseObject(json, ProofFormat), string.Empty);

        public static RoundProof ReadProof(JObject root, string path)
        {
            JsonDoc.AllowOnly(root, path, "format", "number", "definitionVersion", "solution", "randomTest", "checks", "audit", "engine");
            Const(JsonDoc.Required(root, path, "format"), JsonDoc.Join(path, "format"), ProofFormat);
            int number = JsonDoc.Int(JsonDoc.Required(root, path, "number"), JsonDoc.Join(path, "number"), min: 1);
            int definitionVersion = JsonDoc.Int(JsonDoc.Required(root, path, "definitionVersion"), JsonDoc.Join(path, "definitionVersion"), min: 1);

            string solutionPath = JsonDoc.Join(path, "solution");
            JObject solution = JsonDoc.Object(JsonDoc.Required(root, path, "solution"), solutionPath);
            JsonDoc.AllowOnly(solution, solutionPath, "commands", "outcome", "endUs");
            string commandsPath = JsonDoc.Join(solutionPath, "commands");
            IReadOnlyList<RoundCommand> commands = ReadCommands(JsonDoc.Array(JsonDoc.Required(solution, solutionPath, "commands"), commandsPath, minItems: 1), commandsPath);
            Const(JsonDoc.Required(solution, solutionPath, "outcome"), JsonDoc.Join(solutionPath, "outcome"), "Won");
            long endUs = Long(JsonDoc.Required(solution, solutionPath, "endUs"), JsonDoc.Join(solutionPath, "endUs"), min: 0);

            string randomPath = JsonDoc.Join(path, "randomTest");
            JObject random = JsonDoc.Object(JsonDoc.Required(root, path, "randomTest"), randomPath);
            JsonDoc.AllowOnly(random, randomPath, "policy", "seedBase", "runs", "wins", "share", "wilson95", "classOfShare", "borderline");
            Const(JsonDoc.Required(random, randomPath, "policy"), JsonDoc.Join(randomPath, "policy"), RoundRandomTest.PolicyName);
            long seedBase = Long(JsonDoc.Required(random, randomPath, "seedBase"), JsonDoc.Join(randomPath, "seedBase"));
            int runs = JsonDoc.Int(JsonDoc.Required(random, randomPath, "runs"), JsonDoc.Join(randomPath, "runs"));
            if (runs != 100 && runs != 400)
            {
                throw new ContentFormatException(JsonDoc.Join(randomPath, "runs"), $"must be 100 or 400, got {runs}");
            }

            int wins = JsonDoc.Int(JsonDoc.Required(random, randomPath, "wins"), JsonDoc.Join(randomPath, "wins"), min: 0);
            decimal share = JsonDoc.Number(JsonDoc.Required(random, randomPath, "share"), JsonDoc.Join(randomPath, "share"), 0m, 1m);
            string wilsonPath = JsonDoc.Join(randomPath, "wilson95");
            JArray wilson = JsonDoc.Array(JsonDoc.Required(random, randomPath, "wilson95"), wilsonPath, 2, 2);
            decimal low = JsonDoc.Number(wilson[0], JsonDoc.Index(wilsonPath, 0), decimal.MinValue, decimal.MaxValue);
            decimal high = JsonDoc.Number(wilson[1], JsonDoc.Index(wilsonPath, 1), decimal.MinValue, decimal.MaxValue);
            RoundClass classOfShare = ParseClass(JsonDoc.Required(random, randomPath, "classOfShare"), JsonDoc.Join(randomPath, "classOfShare"));
            JToken? borderlineToken = JsonDoc.Optional(random, "borderline");
            bool? borderline = borderlineToken == null ? (bool?)null : JsonDoc.Bool(borderlineToken, JsonDoc.Join(randomPath, "borderline"));

            JObject checks = JsonDoc.Object(JsonDoc.Required(root, path, "checks"), JsonDoc.Join(path, "checks"));

            string auditPath = JsonDoc.Join(path, "audit");
            JObject audit = JsonDoc.Object(JsonDoc.Required(root, path, "audit"), auditPath);
            JsonDoc.AllowOnly(audit, auditPath, "result", "report", "notes");
            string result = OneOf(JsonDoc.Required(audit, auditPath, "result"), JsonDoc.Join(auditPath, "result"), new[] { RoundAudit.Ok, RoundAudit.OkWithException });
            string report = JsonDoc.String(JsonDoc.Required(audit, auditPath, "report"), JsonDoc.Join(auditPath, "report"));
            List<string>? notes = null;
            JToken? notesToken = JsonDoc.Optional(audit, "notes");
            if (notesToken != null)
            {
                string notesPath = JsonDoc.Join(auditPath, "notes");
                notes = Strings(JsonDoc.Array(notesToken, notesPath), notesPath, null);
            }

            RoundEngineVersion engine = ReadEngine(JsonDoc.Required(root, path, "engine"), JsonDoc.Join(path, "engine"));
            return new RoundProof(
                number,
                definitionVersion,
                new RoundSolution(commands, endUs),
                new RoundRandomTest(seedBase, runs, wins, share, low, high, classOfShare, borderline),
                checks,
                new RoundAudit(result, report, notes),
                engine);
        }

        public static string WriteProof(RoundProof proof, bool indented = true) => CanonicalJson.Write(ToJObject(proof), indented);

        public static JObject ToJObject(RoundProof proof)
        {
            RoundRandomTest random = proof.RandomTest;
            var randomTest = new JObject
            {
                ["policy"] = random.Policy,
                ["seedBase"] = random.SeedBase,
                ["runs"] = random.Runs,
                ["wins"] = random.Wins,
                ["share"] = new JValue(random.Share),
                ["wilson95"] = new JArray(new JValue(random.WilsonLow), new JValue(random.WilsonHigh)),
                ["classOfShare"] = ClassName(random.ClassOfShare),
            };
            if (random.Borderline.HasValue)
            {
                randomTest["borderline"] = random.Borderline.Value;
            }

            var audit = new JObject { ["result"] = proof.Audit.Result, ["report"] = proof.Audit.Report };
            if (proof.Audit.Notes != null)
            {
                audit["notes"] = new JArray(ToArray(proof.Audit.Notes));
            }

            return new JObject
            {
                ["format"] = ProofFormat,
                ["number"] = proof.Number,
                ["definitionVersion"] = proof.DefinitionVersion,
                ["solution"] = new JObject
                {
                    ["commands"] = WriteCommands(proof.Solution.Commands),
                    ["outcome"] = proof.Solution.Outcome,
                    ["endUs"] = proof.Solution.EndUs,
                },
                ["randomTest"] = randomTest,
                ["checks"] = proof.Checks,
                ["audit"] = audit,
                ["engine"] = WriteEngine(proof.Engine),
            };
        }

        // ---------------------------------------------------------------- round-conformance.v1

        /// <summary>Reads a conformance fixture; unknown properties are kept (the format has no schema contract).</summary>
        public static RoundConformanceFixture ReadFixture(string json)
        {
            const string path = "";
            JObject root = JsonDoc.ParseObject(json, RoundConformanceFixture.Format);
            Const(JsonDoc.Required(root, path, "format"), "format", RoundConformanceFixture.Format);
            int number = JsonDoc.Int(JsonDoc.Required(root, path, "number"), "number", min: 1);
            int definitionVersion = JsonDoc.Int(JsonDoc.Required(root, path, "definitionVersion"), "definitionVersion", min: 1);
            RoundEngineVersion engine = ReadEngine(JsonDoc.Required(root, path, "engine"), "engine");

            JObject solution = JsonDoc.Object(JsonDoc.Required(root, path, "solution"), "solution");
            JArray windowsArray = JsonDoc.Array(JsonDoc.Required(solution, "solution", "windows"), "solution.windows");
            var windows = new List<RoundFixtureWindow>(windowsArray.Count);
            for (int i = 0; i < windowsArray.Count; i++)
            {
                string at = JsonDoc.Index("solution.windows", i);
                JObject window = JsonDoc.Object(windowsArray[i], at);
                windows.Add(new RoundFixtureWindow(
                    ReadCommand(JsonDoc.Required(window, at, "command"), JsonDoc.Join(at, "command")),
                    JsonDoc.String(JsonDoc.Required(window, at, "events"), JsonDoc.Join(at, "events")),
                    JsonDoc.String(JsonDoc.Required(window, at, "signature"), JsonDoc.Join(at, "signature"))));
            }

            JObject random = JsonDoc.Object(JsonDoc.Required(root, path, "random"), "random");
            JArray gamesArray = JsonDoc.Array(JsonDoc.Required(random, "random", "games"), "random.games");
            var games = new List<RoundFixtureGame>(gamesArray.Count);
            for (int i = 0; i < gamesArray.Count; i++)
            {
                string at = JsonDoc.Index("random.games", i);
                JObject game = JsonDoc.Object(gamesArray[i], at);
                games.Add(new RoundFixtureGame(
                    Long(JsonDoc.Required(game, at, "seed"), JsonDoc.Join(at, "seed")),
                    JsonDoc.String(JsonDoc.Required(game, at, "outcome"), JsonDoc.Join(at, "outcome")),
                    Long(JsonDoc.Required(game, at, "endUs"), JsonDoc.Join(at, "endUs"), min: 0),
                    JsonDoc.String(JsonDoc.Required(game, at, "finalSignature"), JsonDoc.Join(at, "finalSignature"))));
            }

            var owner = new List<RoundFixtureAttempt>();
            JToken? ownerToken = JsonDoc.Optional(root, "owner");
            if (ownerToken != null)
            {
                JArray attempts = JsonDoc.Array(ownerToken, "owner");
                for (int i = 0; i < attempts.Count; i++)
                {
                    string at = JsonDoc.Index("owner", i);
                    JObject attempt = JsonDoc.Object(attempts[i], at);
                    owner.Add(new RoundFixtureAttempt(
                        JsonDoc.String(JsonDoc.Required(attempt, at, "source"), JsonDoc.Join(at, "source")),
                        JsonDoc.String(JsonDoc.Required(attempt, at, "attempt"), JsonDoc.Join(at, "attempt")),
                        ReadCommands(JsonDoc.Array(JsonDoc.Required(attempt, at, "commands"), JsonDoc.Join(at, "commands")), JsonDoc.Join(at, "commands")),
                        JsonDoc.String(JsonDoc.Required(attempt, at, "outcome"), JsonDoc.Join(at, "outcome")),
                        Long(JsonDoc.Required(attempt, at, "endUs"), JsonDoc.Join(at, "endUs"), min: 0),
                        JsonDoc.String(JsonDoc.Required(attempt, at, "finalSignature"), JsonDoc.Join(at, "finalSignature"))));
                }
            }

            return new RoundConformanceFixture(
                number,
                definitionVersion,
                engine,
                windows,
                JsonDoc.String(JsonDoc.Required(solution, "solution", "outcome"), "solution.outcome"),
                Long(JsonDoc.Required(solution, "solution", "endUs"), "solution.endUs", min: 0),
                JsonDoc.String(JsonDoc.Required(solution, "solution", "finalSignature"), "solution.finalSignature"),
                JsonDoc.String(JsonDoc.Required(random, "random", "policy"), "random.policy"),
                Long(JsonDoc.Required(random, "random", "seedBase"), "random.seedBase"),
                JsonDoc.Int(JsonDoc.Required(random, "random", "runs"), "random.runs", min: 0),
                games,
                JsonDoc.Int(JsonDoc.Required(random, "random", "wins"), "random.wins", min: 0),
                owner,
                root);
        }

        // ---------------------------------------------------------------- shared

        /// <summary>The wire name of a class: <c>Normal</c>, <c>Hard</c>, <c>Super Hard</c>.</summary>
        public static string ClassName(RoundClass roundClass) => roundClass switch
        {
            RoundClass.Normal => "Normal",
            RoundClass.Hard => "Hard",
            RoundClass.SuperHard => "Super Hard",
            _ => throw new ArgumentOutOfRangeException(nameof(roundClass)),
        };

        public static RoundEngineVersion ReadEngine(JToken token, string path)
        {
            JObject engine = JsonDoc.Object(token, path);
            JsonDoc.AllowOnly(engine, path, "rules", "timeline", "palette", "clock");
            return new RoundEngineVersion(
                Match(JsonDoc.Required(engine, path, "rules"), JsonDoc.Join(path, "rules"), Sha256),
                Match(JsonDoc.Required(engine, path, "timeline"), JsonDoc.Join(path, "timeline"), Sha256),
                Match(JsonDoc.Required(engine, path, "palette"), JsonDoc.Join(path, "palette"), Sha256),
                Match(JsonDoc.Required(engine, path, "clock"), JsonDoc.Join(path, "clock"), Sha256));
        }

        public static JObject WriteEngine(RoundEngineVersion engine) => new JObject
        {
            ["rules"] = engine.Rules,
            ["timeline"] = engine.Timeline,
            ["palette"] = engine.Palette,
            ["clock"] = engine.Clock,
        };

        /// <summary>Whether two fingerprint sets are the same engine.</summary>
        public static bool SameEngine(RoundEngineVersion? a, RoundEngineVersion? b) =>
            a != null && b != null
            && string.Equals(a.Rules, b.Rules, StringComparison.Ordinal)
            && string.Equals(a.Timeline, b.Timeline, StringComparison.Ordinal)
            && string.Equals(a.Palette, b.Palette, StringComparison.Ordinal)
            && string.Equals(a.Clock, b.Clock, StringComparison.Ordinal);

        public static JArray WriteCommands(IEnumerable<RoundCommand> commands)
        {
            var array = new JArray();
            foreach (RoundCommand command in commands)
            {
                array.Add(command.Type == RoundCommandType.Tap
                    ? new JObject { ["type"] = "tap", ["pod"] = command.Index, ["atUs"] = command.AtUs }
                    : new JObject { ["type"] = "bridge", ["choice"] = command.Index, ["atUs"] = command.AtUs });
            }

            return array;
        }

        private static IReadOnlyList<RoundCommand> ReadCommands(JArray array, string path)
        {
            var commands = new List<RoundCommand>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                commands.Add(ReadCommand(array[i], JsonDoc.Index(path, i)));
            }

            return commands;
        }

        private static RoundCommand ReadCommand(JToken token, string path)
        {
            JObject command = JsonDoc.Object(token, path);
            string type = OneOf(JsonDoc.Required(command, path, "type"), JsonDoc.Join(path, "type"), new[] { "tap", "bridge" });
            string indexName = type == "tap" ? "pod" : "choice";
            JsonDoc.AllowOnly(command, path, "type", indexName, "atUs");
            return new RoundCommand(
                type == "tap" ? RoundCommandType.Tap : RoundCommandType.Bridge,
                JsonDoc.Int(JsonDoc.Required(command, path, indexName), JsonDoc.Join(path, indexName), min: 0),
                Long(JsonDoc.Required(command, path, "atUs"), JsonDoc.Join(path, "atUs"), min: 0));
        }

        private static RoundClass ParseClass(JToken token, string path) =>
            OneOf(token, path, new[] { "Normal", "Hard", "Super Hard" }) switch
            {
                "Normal" => RoundClass.Normal,
                "Hard" => RoundClass.Hard,
                _ => RoundClass.SuperHard,
            };

        private static long Long(JToken token, string path, long min = long.MinValue, long max = long.MaxValue)
        {
            if (token.Type != JTokenType.Integer)
            {
                throw new ContentFormatException(path, "expected an integer");
            }

            long value = token.Value<long>();
            if (value < min || value > max)
            {
                throw new ContentFormatException(path, $"{value} is outside {min}..{max}");
            }

            return value;
        }

        private static int Cell(JToken token, string path, int cells)
        {
            int cell = JsonDoc.Int(token, path, min: 0);
            if (cell >= cells)
            {
                throw new ContentFormatException(path, $"cell {cell} is off the board ({cells} cells)");
            }

            return cell;
        }

        private static string Const(JToken token, string path, string expected)
        {
            string value = JsonDoc.String(token, path);
            if (!string.Equals(value, expected, StringComparison.Ordinal))
            {
                throw new ContentFormatException(path, $"must be '{expected}', got '{value}'");
            }

            return value;
        }

        private static string OneOf(JToken token, string path, string[] allowed)
        {
            string value = JsonDoc.String(token, path);
            if (Array.IndexOf(allowed, value) < 0)
            {
                throw new ContentFormatException(path, $"'{value}' is not one of: {string.Join(", ", allowed)}");
            }

            return value;
        }

        private static string Match(JToken token, string path, Regex pattern)
        {
            string value = JsonDoc.String(token, path);
            if (!pattern.IsMatch(value))
            {
                throw new ContentFormatException(path, $"'{value}' must match {pattern}");
            }

            return value;
        }

        private static List<string> Strings(JArray array, string path, Regex? pattern)
        {
            var list = new List<string>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                list.Add(pattern == null ? JsonDoc.String(array[i], JsonDoc.Index(path, i)) : Match(array[i], JsonDoc.Index(path, i), pattern));
            }

            return list;
        }

        private static void CheckShape(IReadOnlyList<string> rows, int w, int h, string path)
        {
            if (rows.Count != h)
            {
                throw new ContentFormatException(path, $"expected {h} rows (h), got {rows.Count}");
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Length != w)
                {
                    throw new ContentFormatException(JsonDoc.Index(path, i), $"expected {w} cells (w), got {rows[i].Length}");
                }
            }
        }

        private static string? OptionalString(JObject obj, string path, string name)
        {
            JToken? token = JsonDoc.Optional(obj, name);
            return token == null ? null : JsonDoc.String(token, JsonDoc.Join(path, name));
        }

        private static void AddOptional(JObject obj, string name, string? value)
        {
            if (value != null)
            {
                obj[name] = value;
            }
        }

        private static object[] ToArray<T>(IReadOnlyList<T> list)
        {
            var array = new object[list.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = list[i]!;
            }

            return array;
        }
    }
}
