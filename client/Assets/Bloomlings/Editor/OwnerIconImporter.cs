using System;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Import settings for the owner's icon pictures (spec 005 pictures.md D1–D4 and G9–G24: the booster icons, the
    /// variant icons and the lotus in <c>Art/Icons/</c>; D14–D22: the Home promo scenes' layers in <c>Art/Decor/</c>), loaded
    /// at runtime by <c>OwnerArt</c>: straight-alpha PNG files
    /// drawn far below their size (a 512 px icon on a 60 px tile), so mipmaps keep them smooth; alpha is transparency,
    /// edges clamp, sizes are kept as made and the compression is the high-quality one so the dark outlines stay crisp. They
    /// keep no readable copy: the grey copy and the finished picture read their pixels back once (<c>OwnerArt.IconPixels</c>).
    /// </summary>
    public sealed class OwnerIconImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/Bloomlings/Art/Icons/";

        // The Home promo scenes' layers (pictures.md D14–D22) are drawn at about a third of their size too.
        private const string Promo = "Assets/Bloomlings/Art/Decor/Resources/Decor/promo-";

        private void OnPreprocessTexture()
        {
            bool ours = assetPath.StartsWith(Folder, StringComparison.Ordinal) || assetPath.StartsWith(Promo, StringComparison.Ordinal);
            if (!ours || !(assetImporter is TextureImporter importer))
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
