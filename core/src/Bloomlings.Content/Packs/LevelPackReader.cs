using System.Collections.Generic;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>Reads a <c>levels</c> pack: 250 level definitions per pack in ascending level order (research R5).</summary>
    public static class LevelPackReader
    {
        /// <summary>
        /// Verifies the pack against its manifest entry, then parses it. The levels must be exactly
        /// <c>levelRange[0]..levelRange[1]</c>, ascending and without gaps.
        /// </summary>
        public static IReadOnlyList<LevelDefinition> Read(PackEntry entry, byte[] packBytes)
        {
            if (entry.Kind != PackKind.Levels)
            {
                throw new ContentIntegrityException(entry.Id, $"expected a levels pack, got {entry.Kind}");
            }

            PackIntegrity.Verify(entry, packBytes);
            IReadOnlyList<LevelDefinition> levels = ReadUnverified(packBytes, entry.Id);

            int first = entry.FirstLevel!.Value;
            int last = entry.LastLevel!.Value;
            if (levels.Count != last - first + 1)
            {
                throw new ContentIntegrityException(entry.Id, $"levelRange [{first}, {last}] lists {last - first + 1} levels, the pack has {levels.Count}");
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].LevelNumber != first + i)
                {
                    throw new ContentIntegrityException(entry.Id, $"line {i + 1} is level {levels[i].LevelNumber}, expected {first + i}");
                }
            }

            return levels;
        }

        /// <summary>Parses a pack without checking it against a manifest (tools and tests).</summary>
        public static IReadOnlyList<LevelDefinition> ReadUnverified(byte[] packBytes, string packId)
        {
            IReadOnlyList<string> lines = JsonLinesPack.ReadLines(packBytes, packId);
            var levels = new LevelDefinition[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                string where = $"{packId}:{i + 1}";
                levels[i] = DefinitionJson.Read(JsonDoc.ParseObject(lines[i], where), where);
            }

            return levels;
        }

        /// <summary>Builds pack bytes from definitions, one compact canonical document per line.</summary>
        public static byte[] Write(IEnumerable<LevelDefinition> levels)
        {
            var lines = new List<string>();
            foreach (LevelDefinition level in levels)
            {
                lines.Add(DefinitionJson.Write(level, indented: false));
            }

            return JsonLinesPack.Compress(lines);
        }
    }
}
