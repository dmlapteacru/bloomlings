using System;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Import settings for the owner's animated heroes and layered Home (spec 005 FR-028): the hero frames
    /// <c>tools/heroanim</c> pre-renders into <c>Art/Heroes/Resources/HeroMotion/</c> (576 palette PNG files, loaded one
    /// at a time by <c>HeroFrames</c>) and the Home layers in <c>Art/Backgrounds/Resources/Backgrounds/home-*.png</c> (the
    /// fountain's back and front, the lotus, the shadow and the petals, loaded by <c>OwnerArt</c>). They are single
    /// full-rect sprites drawn about their own size, so no mipmaps; alpha is transparency (no dark fringes on the cut-out
    /// edges), edges clamp, sizes are kept as made and they keep no readable copy. They are compressed: the frames with the
    /// normal quality (72 a family, four families on Home), the few large layers with the high one so their soft gradients
    /// stay smooth.
    /// </summary>
    public sealed class HeroMotionImporter : AssetPostprocessor
    {
        /// <summary>The hero frames' folder (<c>HeroMotion.Folder</c> under the heroes' Resources folder).</summary>
        public const string FramesFolder = "Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/";

        /// <summary>The start of every Home layer's path (the garden itself, <c>home.jpg</c>, keeps the backgrounds' import).</summary>
        public const string LayersPrefix = "Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/home-";

        /// <summary>Whether a texture at <paramref name="path"/> is a hero frame or a Home layer.</summary>
        public static bool Handles(string path) =>
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && (path.StartsWith(FramesFolder, StringComparison.Ordinal) || path.StartsWith(LayersPrefix, StringComparison.Ordinal));

        private void OnPreprocessTexture()
        {
            if (!Handles(assetPath) || !(assetImporter is TextureImporter importer))
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            importer.spritePixelsPerUnit = 100f;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.textureCompression = assetPath.StartsWith(FramesFolder, StringComparison.Ordinal)
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.CompressedHQ;
        }
    }
}
