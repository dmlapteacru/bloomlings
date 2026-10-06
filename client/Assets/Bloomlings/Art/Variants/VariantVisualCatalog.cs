using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.Art.Variants
{
    /// <summary>How one variant looks: color, icon, tile and pod skin, and the family whose worker clears it.</summary>
    public readonly struct VariantVisual
    {
        public VariantVisual(VariantId id, Color color, Sprite icon, Sprite? tile, Sprite? podSkin, Family family)
        {
            Id = id;
            Color = color;
            Icon = icon;
            Tile = tile;
            PodSkin = podSkin;
            Family = family;
        }

        public VariantId Id { get; }

        public Color Color { get; }

        public Sprite Icon { get; }

        /// <summary>Optional bespoke tile art; null uses the framed color tile.</summary>
        public Sprite? Tile { get; }

        /// <summary>Optional bespoke pod art; null uses the rounded color pod.</summary>
        public Sprite? PodSkin { get; }

        public Family Family { get; }

        /// <summary>The icon and count color drawn on this variant: dark on light variants, else white (<see cref="InkContrast"/>).</summary>
        public Color Ink => InkOn(Color);

        public static Color InkOn(Color color) => InkContrast.UseDarkInk(color.r, color.g, color.b)
            ? new Color((float)InkContrast.DarkInk, (float)InkContrast.DarkInk, (float)InkContrast.DarkInk, 1f)
            : Color.white;
    }

    /// <summary>
    /// Per-variant visuals (T039). Entries in the asset override the defaults; any variant without an entry falls
    /// back to the core catalog color and the procedural placeholder icon, so the game runs before final art exists.
    /// Hue alone never carries meaning: every variant has its own icon shape (FR-005, FR-072).
    /// </summary>
    [CreateAssetMenu(menuName = "Bloomlings/Variant Visual Catalog", fileName = "VariantVisuals")]
    public sealed class VariantVisualCatalog : ScriptableObject
    {
        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        private static readonly Dictionary<VariantId, VariantVisual> Defaults = new Dictionary<VariantId, VariantVisual>();

        public IReadOnlyList<Entry> Entries => _entries;

        public VariantVisual Get(VariantId id)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.VariantId == id.Key)
                {
                    VariantVisual fallback = Default(id);
                    return new VariantVisual(id, entry.Color, entry.Icon != null ? entry.Icon : fallback.Icon, entry.Tile, entry.PodSkin, fallback.Family);
                }
            }

            return Default(id);
        }

        /// <summary>Visuals from the core catalog (color hex, icon id, family) and procedural placeholder art.</summary>
        public static VariantVisual Default(VariantId id)
        {
            if (Defaults.TryGetValue(id, out VariantVisual visual))
            {
                return visual;
            }

            VariantInfo info = VariantCatalog.Default.Get(id);
            Color color = ColorUtility.TryParseHtmlString(info.ColorHex, out Color parsed) ? parsed : Color.gray;
            visual = new VariantVisual(id, color, ProceduralSprites.Icon(info.IconId), null, null, info.Family);
            Defaults[id] = visual;
            return visual;
        }

        /// <summary>Fills the asset with one entry per launch variant, using the default colors (editor tooling).</summary>
        public void ResetToLaunchDefaults()
        {
            _entries.Clear();
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                if (info.Status != VariantStatus.Launch)
                {
                    continue;
                }

                VariantVisual visual = Default(info.Id);
                _entries.Add(new Entry { VariantId = info.Id.Key, Color = visual.Color, Family = info.Family });
            }
        }

        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Variant wire id, for example leaf or violet_bud.")]
            public string VariantId = string.Empty;

            public Color Color = Color.white;

            [Tooltip("Icon sprite; empty uses the placeholder shape.")]
            public Sprite? Icon;

            public Sprite? Tile;

            public Sprite? PodSkin;

            [Tooltip("Informational: the family always comes from the core catalog.")]
            public Family Family;
        }
    }
}
