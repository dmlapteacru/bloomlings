using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The Garden look's font and label looks in TextMeshPro (spec 003 contracts/fonts.md, research R2, R4, R12).
    /// <list type="bullet">
    /// <item><description><see cref="Display"/> (Nunito ExtraBold) draws every bold type style, <see cref="Body"/>
    /// (Nunito SemiBold) the others. Each is made once at runtime from <c>UI/Fonts/Resources</c>.</description></item>
    /// <item><description><see cref="Apply"/> gives a label its look: the vertex gradient for the fill, and one shared
    /// material per font and look for the outline and the extrusion (a hard underlay), so labels never get their own
    /// material instances.</description></item>
    /// </list>
    /// If a font cannot be loaded, labels keep the TextMeshPro default font and stay readable.
    /// </summary>
    public static class UiFonts
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static TMP_FontAsset? s_display;
        private static TMP_FontAsset? s_body;
        private static bool s_loaded;

        /// <summary>Nunito ExtraBold (bold styles), or null when it could not be loaded.</summary>
        public static TMP_FontAsset? Display
        {
            get
            {
                Load();
                return s_display;
            }
        }

        /// <summary>Nunito SemiBold (regular styles), or null when it could not be loaded.</summary>
        public static TMP_FontAsset? Body
        {
            get
            {
                Load();
                return s_body;
            }
        }

        /// <summary>The font of a type style.</summary>
        public static TMP_FontAsset? For(TypeStyle style) => style.Bold ? Display : Body;

        /// <summary>
        /// Gives a label its look: the gradient fill as vertex colors, and the shared outline and extrusion material.
        /// A plain look only sets the color.
        /// </summary>
        public static void Apply(TextMeshProUGUI label, TypeStyle style, TextLook look)
        {
            if (!look.Volumetric)
            {
                label.enableVertexGradient = false;
                label.color = UiTheme.Of(look.FillTop);
                return;
            }

            label.color = Color.white;
            label.enableVertexGradient = true;
            Color top = UiTheme.Of(look.FillTop);
            Color bottom = UiTheme.Of(look.FillBottom);
            label.colorGradient = new VertexGradient(top, top, bottom, bottom);
            TMP_FontAsset? font = For(style);
            if (font != null)
            {
                label.fontSharedMaterial = Material(font, look);
            }
        }

        /// <summary>
        /// The shared material of a font and a look: the font's material with the outline in the look's line and a hard
        /// underlay below the letters for the extrusion (research R4: TextMeshPro has one underlay, so no soft shadow).
        /// </summary>
        public static Material Material(TMP_FontAsset font, TextLook look)
        {
            string key = font.name + "|" + look.Outline.Hex + "|" + look.OutlineEm + "|" + look.ExtrudeEm;
            if (Materials.TryGetValue(key, out Material? material))
            {
                return material;
            }

            material = new Material(font.material) { name = "Garden " + key };
            Color line = UiTheme.Of(look.Outline);

            // In the SDF shader a width of 1 is the sampling padding; Nunito is sampled at 90 with a padding of 9, so one em
            // is 10 units of the underlay offset and of the outline (contracts/fonts.md "Materials").
            const float emInShaderUnits = 10f;
            material.SetColor("_OutlineColor", line);
            material.SetFloat("_OutlineWidth", Mathf.Min(1f, look.OutlineEm * emInShaderUnits));
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", line);
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", -Mathf.Min(1f, look.ExtrudeEm * emInShaderUnits));
            material.SetFloat("_UnderlayDilate", Mathf.Min(1f, look.OutlineEm * emInShaderUnits));
            material.SetFloat("_UnderlaySoftness", 0f);
            Materials[key] = material;
            return material;
        }

        private static void Load()
        {
            if (s_loaded)
            {
                return;
            }

            s_loaded = true;
            s_display = Create("Nunito-ExtraBold");
            s_body = Create("Nunito-SemiBold");
        }

        private static TMP_FontAsset? Create(string resource)
        {
            Font? font = Resources.Load<Font>(resource);
            if (font == null)
            {
                Debug.LogWarning("UiFonts: missing " + resource + "; labels keep the default font.");
                return null;
            }

            TMP_FontAsset? asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            if (asset == null)
            {
                Debug.LogWarning("UiFonts: no font asset from " + resource);
                return null;
            }

            asset.name = resource;
            return asset;
        }
    }
}
