using System.IO;
using Bloomlings.Client.Art.Variants;
using UnityEditor;
using UnityEngine;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Creates <c>Art/Variants/VariantVisuals.asset</c> (T039) with one entry per launch variant and the default
    /// placeholder colors. Icons stay empty, so the procedural shapes are used until final art is assigned.
    /// </summary>
    public static class PlaceholderArtMenu
    {
        public const string VisualsPath = "Assets/Bloomlings/Art/Variants/VariantVisuals.asset";

        [MenuItem("Tools/Bloomlings/Create Variant Visuals Asset")]
        public static void CreateVariantVisuals()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VariantVisualCatalog>(VisualsPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(VisualsPath)!);
                catalog = ScriptableObject.CreateInstance<VariantVisualCatalog>();
                catalog.ResetToLaunchDefaults();
                AssetDatabase.CreateAsset(catalog, VisualsPath);
            }
            else
            {
                catalog.ResetToLaunchDefaults();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log($"[Bloomlings] {VisualsPath} holds {catalog.Entries.Count} launch variants.");
        }
    }
}
