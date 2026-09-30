using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// SC-003 both ways: every placeholder the playtest drew is a registered slot, which the render checks cover, and
    /// every registered slot is used somewhere in the game. A slot counts as used when:
    /// <list type="bullet">
    /// <item><description>a rendered frame drew or marked it;</description></item>
    /// <item><description>the Unity client references it;</description></item>
    /// <item><description>it is a sound cue the synthesizer plays.</description></item>
    /// </list>
    /// Slots the game does not draw yet (the app icon, music) are listed as such in the inventory, not failed.
    /// </summary>
    public static class Coverage
    {
        public static IEnumerable<string> Check(ISet<string> playtest, string root)
        {
            ISet<string> unity = UnityReferences(root);
            foreach (AssetSlot slot in AssetSlots.All)
            {
                if (!IsUsed(slot, playtest, unity) && !NotDrawnYet(slot))
                {
                    yield return "asset slot " + slot.Id + " is registered but nothing draws it";
                }
            }
        }

        public static bool IsUsed(AssetSlot slot, ISet<string> playtest, ISet<string> unity) =>
            playtest.Contains(slot.Id) || unity.Contains(slot.Id) || slot.Id.StartsWith("audio.cue.", StringComparison.Ordinal);

        /// <summary>A slot with no placeholder in the game yet (External, or a silent music track).</summary>
        public static bool NotDrawnYet(AssetSlot slot) => slot.Kind == PlaceholderKind.External || slot.Placeholder.StartsWith("none", StringComparison.Ordinal);

        /// <summary>Slot ids the Unity client uses: literal ids, the sprite properties that stand for them, and the families of shapes it draws by variant, family or cosmetic.</summary>
        public static ISet<string> UnityReferences(string root)
        {
            string client = Path.Combine(root, "client", "Assets", "Bloomlings");
            string sprites = File.ReadAllText(Path.Combine(client, "Art", "Procedural", "ProceduralSprites.cs"));
            var properties = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(sprites, @"public static Sprite (\w+) => (?:Shape|Get)\(""([a-z_.0-9]+)"""))
            {
                properties[m.Groups[1].Value] = m.Groups[2].Value;
            }

            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(client, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains(Path.DirectorySeparatorChar + "Tests" + Path.DirectorySeparatorChar) && !f.EndsWith("AssetSlots.cs", StringComparison.Ordinal) && !f.EndsWith("ShapeLibrary.cs", StringComparison.Ordinal)))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in Regex.Matches(text, @"""([a-z]+(?:\.[a-z0-9_]+)+)"""))
                {
                    used.Add(m.Groups[1].Value);
                }

                foreach (Match m in Regex.Matches(text, @"ProceduralSprites\.(\w+)"))
                {
                    if (properties.TryGetValue(m.Groups[1].Value, out string? id))
                    {
                        used.Add(id);
                    }

                    switch (m.Groups[1].Value)
                    {
                        case "Icon":
                            used.UnionWith(AssetSlots.All.Where(s => s.Category == AssetCategory.VariantSymbol).Select(s => s.Id));
                            break;
                        case "Silhouette":
                            used.UnionWith(new[] { "char.sprig", "char.bloom", "char.drop", "char.twig" });
                            break;
                        case "Accessory":
                        case "SkinPattern":
                            used.UnionWith(AssetSlots.All.Where(s => s.Category == AssetCategory.Cosmetic).Select(s => s.Id));
                            break;
                    }
                }
            }

            return used;
        }
    }
}
