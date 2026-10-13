using System;
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
        private static byte[]? _atlasPixels;
        private static readonly Dictionary<string, (byte[] Rgba, int Width, int Height)?> Pixels = new Dictionary<string, (byte[], int, int)?>();
        /// <summary>Top-down pixels for finished-board baking, from the same atlas as live pods and board tiles.</summary>
        public static (byte[] Rgba, int Width, int Height)? PicturePixels(string name)
        {
            string iconId = name.StartsWith("field-", StringComparison.Ordinal) ? name.Substring(6) : name.StartsWith("variant-", StringComparison.Ordinal) ? name.Substring(8) : name;
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                if (info.IconId != iconId) continue;
                if (Pixels.TryGetValue(info.Id.Key, out var cached)) return cached;
                var cell = GameplaySkin.PodCell(info.Id.Key);
                Texture2D? atlas = Resources.Load<Texture2D>("Gameplay/" + GameplaySkin.Atlas);
                if (atlas == null || !cell.HasValue) return Pixels[info.Id.Key] = null;
                _atlasPixels ??= OwnerArt.ReadBack(atlas);
                if (_atlasPixels == null) return Pixels[info.Id.Key] = null;
                int w = atlas.width / 4, h = atlas.height / 3;
                byte[] pixels = new byte[w * h * 4];
                for (int y = 0; y < h; y++)
                    Buffer.BlockCopy(_atlasPixels, ((atlas.height - 1 - cell.Value.Row * h - y) * atlas.width + cell.Value.Column * w) * 4, pixels, y * w * 4, w * 4);
                return Pixels[info.Id.Key] = (pixels, w, h);
            }
            return null;
        }
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
