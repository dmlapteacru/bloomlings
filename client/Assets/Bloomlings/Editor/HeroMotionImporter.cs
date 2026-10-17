using System;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Imports the accepted modular V2 outfit layers and Home views as full-rect RGBA sprites,
    /// and the matching layered fountain pictures. Runtime contains flat pictures only; source
    /// models and rigs stay outside Resources. Crops keep exact size, smooth alpha edges and
    /// no mipmaps or readable texture copies. Current hero gradients remain uncompressed.
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
            // Current static pictures retain full-color gradients and eye detail without block artifacts.
            // The animated libraries keep their existing compression and memory budget.
            importer.textureCompression = (assetPath.StartsWith(FramesFolder + "homehero-", StringComparison.Ordinal) || assetPath.StartsWith(FramesFolder + "twigv2-", StringComparison.Ordinal) || assetPath.StartsWith(FramesFolder + "sprigv2-", StringComparison.Ordinal) || assetPath.StartsWith(FramesFolder + "bloomv2-", StringComparison.Ordinal) || assetPath.StartsWith(FramesFolder + "dropv2-", StringComparison.Ordinal))
                ? TextureImporterCompression.Uncompressed
                : assetPath.StartsWith(FramesFolder, StringComparison.Ordinal)
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.CompressedHQ;
        }
    }
}
