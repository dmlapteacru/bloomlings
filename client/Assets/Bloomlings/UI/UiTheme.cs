using Bloomlings.Client.UI.Design;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The design board's tokens as Unity colors and sizes (spec 002 FR-005, research R6). The values live once in the
    /// engine-free <see cref="DesignTokens"/>, shared with the playtest; this class converts them. The older names
    /// (Accent, Panel, SlotEmpty, …) map onto the tokens they became.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Background = Of(DesignTokens.Colors.BackdropSkyBottom);
        public static readonly Color Ground = Of(DesignTokens.Colors.TileGround);
        public static readonly Color StoneColor = Of(DesignTokens.Colors.TileStone);
        public static readonly Color Panel = Of(DesignTokens.Colors.SurfacePanel);
        public static readonly Color PanelEdge = Of(DesignTokens.Colors.SurfacePanelEdge);
        public static readonly Color PanelShade = Of(DesignTokens.Colors.SurfaceScrim);
        public static readonly Color Sunk = Of(DesignTokens.Colors.SurfaceSunk);
        public static readonly Color Text = Of(DesignTokens.Colors.TextPrimary);
        public static readonly Color TextSecondary = Of(DesignTokens.Colors.TextSecondary);
        public static readonly Color TextOnColor = Of(DesignTokens.Colors.TextOnColor);
        public static readonly Color TextOutline = Of(DesignTokens.Colors.TextOutline);
        public static readonly Color Accent = Of(DesignTokens.Colors.ButtonPrimary);
        public static readonly Color AccentTop = Of(DesignTokens.Colors.ButtonPrimaryTop);
        public static readonly Color AccentEdge = Of(DesignTokens.Colors.ButtonPrimaryEdge);
        public static readonly Color Secondary = Of(DesignTokens.Colors.ButtonSecondary);
        public static readonly Color SecondaryEdge = Of(DesignTokens.Colors.ButtonSecondaryEdge);
        public static readonly Color IconButton = Of(DesignTokens.Colors.ButtonIcon);
        public static readonly Color IconButtonEdge = Of(DesignTokens.Colors.ButtonIconEdge);
        public static readonly Color IconGlyph = Of(DesignTokens.Colors.ButtonIconGlyph);
        public static readonly Color DarkButton = Of(DesignTokens.Colors.ButtonDark);
        public static readonly Color Warning = Of(DesignTokens.Colors.StateDanger);
        /// <summary>An empty Waiting Slot: a sunk well (spec 003 FR-022).</summary>
        public static readonly Color SlotEmpty = Of(DesignTokens.Colors.GardenWell);
        public static readonly Color SlotLocked = Of(DesignTokens.Colors.StateLockBg);
        public static readonly Color LockGlyph = Of(DesignTokens.Colors.StateLock);
        public static readonly Color Stuck = Of(DesignTokens.Colors.StateStuck);
        public static readonly Color TileFrame = new Color(0.18f, 0.20f, 0.22f, 0.35f);
        public static readonly Color EntryMarker = Of(DesignTokens.Colors.PetalCenter);
        public static readonly Color GateColor = new Color(0.36f, 0.50f, 0.30f);
        public static readonly Color FountainColor = new Color(0.58f, 0.66f, 0.74f);
        public static readonly Color LinkColor = Of(DesignTokens.Colors.StateLink);
        public static readonly Color Petal = Of(DesignTokens.Colors.PetalFill);
        public static readonly Color PetalCenter = Of(DesignTokens.Colors.PetalCenter);

        /// <summary>Reference resolution of the portrait canvas.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(DesignTokens.ReferenceWidth, 1920f);

        /// <summary>A design token color as a Unity color.</summary>
        public static Color Of(Rgba color) => new Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

        /// <summary>A Unity color as a design token color.</summary>
        public static Rgba ToRgba(Color color) => new Rgba(Byte(color.r), Byte(color.g), Byte(color.b), Byte(color.a));

        /// <summary>A soft light version of a variant color for the finished picture (FR-007).</summary>
        public static Color Light(Color color) => Color.Lerp(color, Color.white, 0.55f);

        /// <summary>A darker version for frames and outlines.</summary>
        public static Color Dark(Color color) => Color.Lerp(color, Color.black, 0.35f);

        private static byte Byte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
    }
}
