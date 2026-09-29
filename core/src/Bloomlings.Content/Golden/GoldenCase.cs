using System;
using System.Collections.Generic;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Golden
{
    /// <summary>
    /// A golden replay case (<c>core/tests/golden/*.golden.json</c>, format in <c>core/tests/golden/README.md</c>):
    /// a level, its picture, a command list and the expected outcome. The same cases run under .NET and under
    /// Unity's compiler (SC-005).
    /// </summary>
    public sealed record GoldenCase(
        string Name,
        string Description,
        LevelDefinition Definition,
        BasePicture Picture,
        IReadOnlyList<Command> Commands,
        int ContentVersion,
        int ShuffleNodeBudget,
        string ExpectedEventsDigest,
        string ExpectedStateHash,
        LevelStatus ExpectedStatus)
    {
        private static readonly EnumNames<LevelStatus> StatusNames = new EnumNames<LevelStatus>(
            (LevelStatus.Playing, "playing"),
            (LevelStatus.Won, "won"),
            (LevelStatus.Jammed, "jammed"),
            (LevelStatus.Stuck, "stuck"));

        public static GoldenCase Read(string json)
        {
            const string path = "";
            JObject root = JsonDoc.ParseObject(json, "golden");
            JsonDoc.AllowOnly(
                root,
                path,
                "name",
                "description",
                "definition",
                "picture",
                "commands",
                "contentVersion",
                "shuffleNodeBudget",
                "expectedEventsDigest",
                "expectedStateHash",
                "expectedStatus");

            string name = JsonDoc.String(JsonDoc.Required(root, path, "name"), "name");
            string description = JsonDoc.String(JsonDoc.Required(root, path, "description"), "description");
            LevelDefinition definition = DefinitionJson.Read(JsonDoc.Object(JsonDoc.Required(root, path, "definition"), "definition"), "definition");
            BasePicture picture = BasePictureJson.Read(JsonDoc.Object(JsonDoc.Required(root, path, "picture"), "picture"), "picture");

            JArray commandsArray = JsonDoc.Array(JsonDoc.Required(root, path, "commands"), "commands");
            var commands = new Command[commandsArray.Count];
            for (int i = 0; i < commands.Length; i++)
            {
                string itemPath = JsonDoc.Index("commands", i);
                string text = JsonDoc.String(commandsArray[i], itemPath);
                try
                {
                    commands[i] = CommandText.Parse(text);
                }
                catch (FormatException ex)
                {
                    throw new ContentFormatException(itemPath, ex.Message);
                }
            }

            int contentVersion = JsonDoc.Int(JsonDoc.Required(root, path, "contentVersion"), "contentVersion", min: 0);
            int budget = JsonDoc.Int(JsonDoc.Required(root, path, "shuffleNodeBudget"), "shuffleNodeBudget", min: 1);
            string digest = JsonDoc.String(JsonDoc.Required(root, path, "expectedEventsDigest"), "expectedEventsDigest");
            string hash = JsonDoc.String(JsonDoc.Required(root, path, "expectedStateHash"), "expectedStateHash");
            LevelStatus status = JsonDoc.Enum(JsonDoc.Required(root, path, "expectedStatus"), "expectedStatus", StatusNames);
            return new GoldenCase(name, description, definition, picture, commands, contentVersion, budget, digest, hash, status);
        }

        public string Write()
        {
            var commands = new JArray();
            foreach (Command command in Commands)
            {
                commands.Add(CommandText.Format(command));
            }

            var root = new JObject
            {
                ["name"] = Name,
                ["description"] = Description,
                ["definition"] = DefinitionJson.ToJObject(Definition),
                ["picture"] = BasePictureJson.ToJObject(Picture),
                ["commands"] = commands,
                ["contentVersion"] = ContentVersion,
                ["shuffleNodeBudget"] = ShuffleNodeBudget,
                ["expectedEventsDigest"] = ExpectedEventsDigest,
                ["expectedStateHash"] = ExpectedStateHash,
                ["expectedStatus"] = StatusNames.ToWire(ExpectedStatus),
            };
            return CanonicalJson.Write(root, indented: true);
        }

        public static string StatusToWire(LevelStatus status) => StatusNames.ToWire(status);
    }
}
