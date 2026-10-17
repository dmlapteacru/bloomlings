using System;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// Reads the accepted V2 pipeline's flat pictures. The retired procedural hero sculptor is removed;
    /// the character tool cannot silently regenerate another character design.
    /// </summary>
    public static class Heroes3D
    {
        public static byte[] Solo(Family family, bool blank = false, int rowStride = 1) =>
            Read(CharacterArt.Hero(family, blank));

        public static byte[] Group(int rowStride = 1) => Read(CharacterArt.Group);

        public static (double X, double Y) FaceCenter(Family family)
        {
            (float x, float y) = CharacterArt.FaceCenterHero(family);
            return (x, y);
        }

        private static byte[] Read(string name)
        {
            string? root = AppContext.BaseDirectory;
            while (root != null && !File.Exists(Path.Combine(root, "CLAUDE.md"))) root = Path.GetDirectoryName(root);
            if (root == null) throw new InvalidOperationException("Repository root not found");
            string file = Path.Combine(root, "client/Assets/Bloomlings/Art/Characters/Resources/Characters", name + ".png");
            if (!File.Exists(file)) throw new InvalidOperationException("Rebuild current hero art with tools/heroanim/pack-current-heroes.py: " + name);
            return Png.Decode(File.ReadAllBytes(file)).Rgba;
        }
    }
}
