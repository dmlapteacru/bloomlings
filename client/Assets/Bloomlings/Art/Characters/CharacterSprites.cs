using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.Art
{
    /// <summary>
    /// The generated character pictures of spec 004 (contracts/hosts.md "Unity"): the 2D variant characters and the 3D
    /// family heroes, loaded from <c>Art/Characters/Resources/Characters/</c> by their names in the art set
    /// (<see cref="CharacterArt"/>). Each picture becomes a sprite once and is cached. A missing picture gives null and one
    /// warning, and callers draw the spec 002 silhouette instead (FR-021).
    /// </summary>
    public static class CharacterSprites
    {
        private static readonly Dictionary<string, Sprite?> Cache = new Dictionary<string, Sprite?>(StringComparer.Ordinal);

        /// <summary>A picture by its art set name (<c>2d/leaf-happy</c>, <c>3d/group</c>), or null when it is missing.</summary>
        public static Sprite? Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite? cached))
            {
                return cached;
            }

            Texture2D? texture = Resources.Load<Texture2D>("Characters/" + name);
            Sprite? sprite = null;
            if (texture == null)
            {
                Debug.LogWarning("Character picture Characters/" + name + " is missing; drawing the fallback figure (run tools/artgen build).");
            }
            else
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
                sprite.name = name;
            }

            Cache[name] = sprite;
            return sprite;
        }

        /// <summary>A variant's 2D character in a mood (spec 004 FR-005 to FR-007).</summary>
        public static Sprite? Character(VariantId variant, CharacterMood mood) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Get(CharacterArt.Picture2D(info.IconId, mood)) : null;

        /// <summary>A family's 3D hero (meta screens only), or its blank-faced picture under a worn expression.</summary>
        public static Sprite? Hero(Family family, bool blank) => Get(CharacterArt.Hero(family, blank));

        /// <summary>The four 3D heroes on their stone pedestal.</summary>
        public static Sprite? Group => Get(CharacterArt.Group);

        private static IReadOnlyDictionary<string, string?>? _sources;

        /// <summary>
        /// Whether a family's blank-faced 3D hero (<c>3d/{family}-blank</c>, shown under a worn expression) is the same
        /// character as its solo hero: both from the same source in the art set's manifest (both generated, or both the
        /// owner's, pictures.md A5). An owner hero beside a generated blank is a different design, so the hero keeps its own
        /// face and the expression shows as a badge (<see cref="CharacterArt.ExpressionBadge"/>). Without the manifest it
        /// trusts the blank (the generated set); without the blank there is none.
        /// </summary>
        public static bool HasMatchingBlank(Family family)
        {
            if (Get(CharacterArt.Hero(family, blank: true)) == null)
            {
                return false;
            }

            if (_sources == null)
            {
                TextAsset? manifest = Resources.Load<TextAsset>("Characters/manifest");
                _sources = manifest != null ? ManifestSources(manifest.text) : new Dictionary<string, string?>();
            }

            if (_sources.Count == 0)
            {
                return true;
            }

            _sources.TryGetValue(CharacterArt.Hero(family), out string? hero);
            _sources.TryGetValue(CharacterArt.Hero(family, blank: true), out string? blank);
            return string.Equals(hero, blank, StringComparison.Ordinal);
        }

        /// <summary>
        /// The source of each picture in the art set's <c>manifest.json</c> (tools/artgen), by its art set name without the
        /// extension (<c>3d/sprig</c>): <c>"owner"</c> for an adopted owner picture, null for a generated one. Engine-free.
        /// </summary>
        public static IReadOnlyDictionary<string, string?> ManifestSources(string json)
        {
            var sources = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (System.Text.RegularExpressions.Match entry in System.Text.RegularExpressions.Regex.Matches(json, "\\{[^{}]*\\}"))
            {
                System.Text.RegularExpressions.Match path = System.Text.RegularExpressions.Regex.Match(entry.Value, "\"path\"\\s*:\\s*\"([^\"]+)\"");
                if (!path.Success)
                {
                    continue;
                }

                System.Text.RegularExpressions.Match source = System.Text.RegularExpressions.Regex.Match(entry.Value, "\"source\"\\s*:\\s*\"([^\"]+)\"");
                string name = path.Groups[1].Value;
                int dot = name.LastIndexOf('.');
                sources[dot > 0 ? name.Substring(0, dot) : name] = source.Success ? source.Groups[1].Value : null;
            }

            return sources;
        }
    }
}
