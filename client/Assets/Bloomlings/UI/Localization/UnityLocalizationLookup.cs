#if BLOOMLINGS_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Bloomlings.Client.UI.Localization
{
    /// <summary>
    /// Connects <see cref="Loc"/> to the Unity Localization string table <c>UI</c> for the selected locale (R18). A key
    /// missing from the table falls back to the bundled English text.
    /// </summary>
    internal static class UnityLocalizationLookup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Loc.Lookup = key =>
            {
                StringTable? table = LocalizationSettings.HasSettings ? LocalizationSettings.StringDatabase.GetTable(Loc.Table) : null;
                StringTableEntry? entry = table != null ? table.GetEntry(key) : null;
                return entry?.GetLocalizedString();
            };
        }
    }
}
#endif
