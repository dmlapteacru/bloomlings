using UnityEngine;

namespace Bloomlings.Client.UI
{
    /// <summary>Placeholder palette and sizes for the light, minimal look (doc 12). Replace with final art values.</summary>
    public static class UiTheme
    {
        public static readonly Color Background = new Color(0.96f, 0.95f, 0.90f);
        public static readonly Color Ground = new Color(0.86f, 0.83f, 0.74f);
        public static readonly Color StoneColor = new Color(0.55f, 0.55f, 0.58f);
        public static readonly Color Panel = new Color(1f, 1f, 1f, 0.92f);
        public static readonly Color PanelShade = new Color(0.12f, 0.12f, 0.14f, 0.45f);
        public static readonly Color Text = new Color(0.20f, 0.22f, 0.25f);
        public static readonly Color TextOnColor = Color.white;
        public static readonly Color Accent = new Color(0.30f, 0.62f, 0.36f);
        public static readonly Color Warning = new Color(0.93f, 0.45f, 0.25f);
        public static readonly Color SlotEmpty = new Color(0.88f, 0.87f, 0.82f);
        public static readonly Color SlotLocked = new Color(0.62f, 0.60f, 0.56f);
        public static readonly Color TileFrame = new Color(0.18f, 0.20f, 0.22f, 0.35f);
        public static readonly Color EntryMarker = new Color(0.98f, 0.80f, 0.25f);

        /// <summary>Reference resolution of the portrait canvas.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        /// <summary>A soft light version of a variant color for the finished picture (FR-007).</summary>
        public static Color Light(Color color) => Color.Lerp(color, Color.white, 0.55f);

        /// <summary>A darker version for frames and outlines.</summary>
        public static Color Dark(Color color) => Color.Lerp(color, Color.black, 0.35f);
    }
}
