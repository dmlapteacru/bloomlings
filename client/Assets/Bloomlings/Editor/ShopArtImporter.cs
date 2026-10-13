using System;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>Keeps the shop atlas and nine-slice source coordinates identical on every platform.</summary>
    public sealed class ShopArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/Bloomlings/Art/Shop/Resources/Shop/";

        private void OnPreprocessTexture()
        {
            if (!(assetPath.StartsWith(Folder, StringComparison.Ordinal) || assetPath.StartsWith("Assets/Bloomlings/UI/Resources/Navigation/", StringComparison.Ordinal) || assetPath.StartsWith("Assets/Bloomlings/Art/Gameplay/Resources/Gameplay/", StringComparison.Ordinal)) || !(assetImporter is TextureImporter importer))
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
