using System;
using System.Collections.Generic;
using System.Text;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Validation
{
    public enum ValidationResult
    {
        Solvable,
        Unsolvable,

        /// <summary>The solver ran out of budget; counts as a rejection (R8).</summary>
        Unknown,
    }

    /// <summary>
    /// The pipeline's certificate for one level definition (data-model §1.4, T079). It is kept in the repository and CI
    /// artifacts and never shipped. <see cref="DefinitionHash"/> ties it to the exact definition it certifies.
    /// </summary>
    public sealed record ValidationRecord(
        int LevelNumber,
        int DefinitionVersion,
        string DefinitionHash,
        string SolverVersion,
        int NodeBudget,
        int NodesUsed,
        ValidationResult Result,
        IReadOnlyList<Command> SolutionTrace,
        IReadOnlyList<Command>? JamWitness,
        bool? PlayerInfoFair,
        IReadOnlyDictionary<string, int> Metrics,
        IReadOnlyList<string> Checks)
    {
        private static readonly EnumNames<ValidationResult> ResultNames = new EnumNames<ValidationResult>(
            (ValidationResult.Solvable, "solvable"),
            (ValidationResult.Unsolvable, "unsolvable"),
            (ValidationResult.Unknown, "unknown"));

        /// <summary>SHA-256 of the compact canonical definition.</summary>
        public static string HashOf(LevelDefinition definition) =>
            PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(DefinitionJson.Write(definition, indented: false)));

        public string Write()
        {
            var metrics = new JObject();
            foreach (KeyValuePair<string, int> metric in Metrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            var root = new JObject
            {
                ["levelNumber"] = LevelNumber,
                ["definitionVersion"] = DefinitionVersion,
                ["definitionHash"] = DefinitionHash,
                ["solverVersion"] = SolverVersion,
                ["nodeBudget"] = NodeBudget,
                ["nodesUsed"] = NodesUsed,
                ["result"] = ResultNames.ToWire(Result),
                ["solutionTrace"] = Commands(SolutionTrace),
                ["jamWitness"] = JamWitness == null ? JValue.CreateNull() : (JToken)Commands(JamWitness),
                ["playerInfoFair"] = PlayerInfoFair.HasValue ? new JValue(PlayerInfoFair.Value) : JValue.CreateNull(),
                ["metrics"] = metrics,
                ["checks"] = new JArray(Sorted(Checks)),
            };
            return CanonicalJson.Write(root, indented: true);
        }

        public static ValidationRecord Read(string json)
        {
            const string p = "";
            JObject root = JsonDoc.ParseObject(json, "validation");
            JsonDoc.AllowOnly(root, p, "levelNumber", "definitionVersion", "definitionHash", "solverVersion", "nodeBudget", "nodesUsed", "result", "solutionTrace", "jamWitness", "playerInfoFair", "metrics", "checks");
            var metrics = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (JProperty metric in JsonDoc.Object(JsonDoc.Required(root, p, "metrics"), "metrics").Properties())
            {
                metrics[metric.Name] = JsonDoc.Int(metric.Value, "metrics." + metric.Name);
            }

            JToken? jam = JsonDoc.Optional(root, "jamWitness");
            JToken? fair = JsonDoc.Optional(root, "playerInfoFair");
            JArray checks = JsonDoc.Array(JsonDoc.Required(root, p, "checks"), "checks");
            var checkIds = new List<string>();
            for (int i = 0; i < checks.Count; i++)
            {
                checkIds.Add(JsonDoc.String(checks[i], JsonDoc.Index("checks", i)));
            }

            return new ValidationRecord(
                JsonDoc.Int(JsonDoc.Required(root, p, "levelNumber"), "levelNumber", min: 1),
                JsonDoc.Int(JsonDoc.Required(root, p, "definitionVersion"), "definitionVersion", min: 1),
                JsonDoc.String(JsonDoc.Required(root, p, "definitionHash"), "definitionHash"),
                JsonDoc.String(JsonDoc.Required(root, p, "solverVersion"), "solverVersion"),
                JsonDoc.Int(JsonDoc.Required(root, p, "nodeBudget"), "nodeBudget", min: 1),
                JsonDoc.Int(JsonDoc.Required(root, p, "nodesUsed"), "nodesUsed", min: 0),
                JsonDoc.Enum(JsonDoc.Required(root, p, "result"), "result", ResultNames),
                ReadCommands(JsonDoc.Required(root, p, "solutionTrace"), "solutionTrace"),
                jam == null ? null : ReadCommands(jam, "jamWitness"),
                fair == null ? (bool?)null : JsonDoc.Bool(fair, "playerInfoFair"),
                metrics,
                checkIds);
        }

        private static JArray Commands(IReadOnlyList<Command> commands)
        {
            var array = new JArray();
            foreach (Command command in commands)
            {
                array.Add(CommandText.Format(command));
            }

            return array;
        }

        private static IReadOnlyList<Command> ReadCommands(JToken token, string path)
        {
            JArray array = JsonDoc.Array(token, path);
            var commands = new Command[array.Count];
            for (int i = 0; i < commands.Length; i++)
            {
                string itemPath = JsonDoc.Index(path, i);
                try
                {
                    commands[i] = CommandText.Parse(JsonDoc.String(array[i], itemPath));
                }
                catch (FormatException ex)
                {
                    throw new ContentFormatException(itemPath, ex.Message);
                }
            }

            return commands;
        }

        private static object[] Sorted(IReadOnlyList<string> values)
        {
            var list = new List<string>(values);
            list.Sort(StringComparer.Ordinal);
            return list.ConvertAll(v => (object)v).ToArray();
        }
    }
}
