using System;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Import settings for the generated character pictures of spec 004 (contracts/art-files.md): straight-alpha PNG
    /// files in <c>Art/Characters/</c>, loaded at runtime by <c>CharacterSprites</c>. Alpha is transparency, mipmaps keep
    /// the small board and pod sizes smooth, edges clamp, sizes are kept as made (no power-of-two scaling), and the
    /// compression is the high-quality one so outlines and faces stay crisp.
    /// </summary>
    public sealed class CharacterArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/Bloomlings/Art/Characters/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal) || !(assetImporter is TextureImporter importer))
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
