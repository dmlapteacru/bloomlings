using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    public static class GameplayArt
    {
        private static readonly Dictionary<string, Sprite?> Icons = new Dictionary<string, Sprite?>();
        private static Texture2D? _greyAtlas;
        public static Sprite? Pod(VariantId variant, bool grey = false)
        {
            string key = variant.Key + (grey ? "#grey" : "");
            if (Icons.TryGetValue(key, out Sprite? icon)) return icon;
            var cell = GameplaySkin.PodCell(variant.Key);
            Texture2D? atlas = Resources.Load<Texture2D>("Gameplay/" + GameplaySkin.Atlas);
            if (!cell.HasValue || atlas == null) return Icons[key] = null;
            if (grey && _greyAtlas == null)
            {
                byte[]? pixels = OwnerArt.ReadBack(atlas);
                if (pixels != null)
                {
                    OwnerPictures.GreyPixels(pixels);
                    _greyAtlas = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    _greyAtlas.SetPixelData(pixels, 0); _greyAtlas.Apply(false, true);
                }
            }
            if (grey && _greyAtlas != null) atlas = _greyAtlas;
            int w = atlas.width / 4, h = atlas.height / 3;
            return Icons[key] = Sprite.Create(atlas, new Rect(cell.Value.Column * w, atlas.height - (cell.Value.Row + 1) * h, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        }
    }
}
