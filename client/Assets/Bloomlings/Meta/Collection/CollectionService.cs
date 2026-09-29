using System;
using System.Collections.Generic;
using System.Text;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.Meta.Collection
{
    /// <summary>
    /// The Collection (FR-065, T145): every won level adds its finished picture as
    /// <c>{pictureId, pictureVersion, mappingHash, levelNumber}</c>, once per level. The mapping hash identifies the
    /// colors the picture was finished in, so the Collection can redraw it. The Collection only shows pictures; it is
    /// never a level selector. Engine-free.
    /// </summary>
    public sealed class CollectionService
    {
        private readonly PlayerSave _save;
        private readonly Action _persist;

        public CollectionService(PlayerSave save, Action persist)
        {
            _save = save;
            _persist = persist;
        }

        /// <summary>Entries in the order they were added (ascending level for a linear player).</summary>
        public IReadOnlyList<CollectionEntry> Entries => _save.Collection;

        public int Count => _save.Collection.Count;

        /// <summary>Adds the finished picture of a won level; false when that level is already in the Collection.</summary>
        public bool Add(LevelDefinition definition, int levelNumber)
        {
            foreach (CollectionEntry existing in _save.Collection)
            {
                if (existing.LevelNumber == levelNumber)
                {
                    return false;
                }
            }

            _save.Collection.Add(new CollectionEntry(definition.Picture.Id, definition.Picture.Version, MappingHash(definition.Mapping), levelNumber));
            _persist();
            return true;
        }

        /// <summary>The first 16 hex digits of the SHA-256 of the sorted <c>role=variant</c> pairs.</summary>
        public static string MappingHash(IReadOnlyDictionary<string, VariantId> mapping)
        {
            var roles = new List<string>(mapping.Keys);
            roles.Sort(StringComparer.Ordinal);
            var text = new StringBuilder();
            foreach (string role in roles)
            {
                text.Append(role).Append('=').Append(mapping[role].Key).Append(';');
            }

            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text.ToString())).Substring(0, 16);
        }
    }
}
