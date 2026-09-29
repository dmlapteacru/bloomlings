#if BLOOMLINGS_LOCALIZATION
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.UI.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Bloomlings.Client.Editor
{
    /// <summary>
    /// Builds the Unity Localization string table collection <c>UI</c> from the English source
    /// <c>UI/Localization/Resources/Strings_en.csv</c> (R18, T148). Run it after changing the CSV; translators then add
    /// locales to the same collection (or round-trip CSV with the Localization package's CSV extension).
    /// </summary>
    internal static class LocalizationMenu
    {
        private const string Folder = "Assets/Bloomlings/UI/Localization/Tables";
        private const string CsvPath = "Assets/Bloomlings/UI/Localization/Resources/" + Loc.EnglishResource + ".csv";

        [MenuItem("Tools/Bloomlings/Localization/Import English Strings")]
        private static void ImportEnglish()
        {
            Directory.CreateDirectory(Folder);
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, Folder + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            Locale? english = LocalizationEditorSettings.GetLocale(new LocaleIdentifier("en"));
            if (english == null)
            {
                english = Locale.CreateLocale(new LocaleIdentifier("en"));
                AssetDatabase.CreateAsset(english, Folder + "/English (en).asset");
                LocalizationEditorSettings.AddLocale(english);
            }

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(Loc.Table)
                ?? LocalizationEditorSettings.CreateStringTableCollection(Loc.Table, Folder);
            var table = (StringTable)(collection.GetTable(english.Identifier) ?? collection.AddNewTable(english.Identifier));
            Dictionary<string, string> strings = Loc.ParseCsv(File.ReadAllText(CsvPath));
            foreach (KeyValuePair<string, string> pair in strings)
            {
                table.AddEntry(pair.Key, pair.Value);
            }

            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Localization] {strings.Count} English strings imported into the '{Loc.Table}' table.");
        }
    }
}
#endif
