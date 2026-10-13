using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    /// <summary>The store's illustrated atlas; sprites are sliced at runtime, with no baked labels or prices.</summary>
    public static class ShopArt
    {
        private static readonly Dictionary<string, Sprite?> Sprites = new Dictionary<string, Sprite?>();
        public static Sprite? Get(string name)
        {
            if (Sprites.TryGetValue(name, out Sprite? cached)) return cached;
            string key = name.StartsWith("shop/", StringComparison.Ordinal) ? name.Substring(5) : name;
            Texture2D? texture = Resources.Load<Texture2D>("Shop/" + ShopBodyDesign.ResourceName(key));
            if (texture == null) return Sprites[name] = null;
            Rect rect;
            if (key == "background" || key == "hero-card-v2" || key == "noads-basket-v2") rect = new Rect(0, 0, texture.width, texture.height);
            else if (key == "price-button") rect = new Rect(109, texture.height - 605, 1985, 505);
            else if (key.StartsWith("boost-", StringComparison.Ordinal))
            {
                var cell = ShopBodyDesign.BoosterCell(key);
                if (!cell.HasValue) return Sprites[name] = null;
                int w = texture.width / 2, h = texture.height / 2;
                rect = new Rect(cell.Value.Column * w, (1 - cell.Value.Row) * h, w, h);
            }
            else
            {
                (int Column, int Row)? cell = ShopBodyDesign.AtlasCell(key);
                if (!cell.HasValue) return Sprites[name] = null;
                int w = texture.width / 3, h = texture.height / 2;
                var crop = ShopBodyDesign.AtlasCrop(key, w, h);
                rect = new Rect(cell.Value.Column * w + crop.Left, texture.height - (cell.Value.Row * h + crop.Top + crop.Height), crop.Width, crop.Height);
            }
            Vector4 border = key == "price-button" ? new Vector4(350, 190, 350, 190) : Vector4.zero;
            return Sprites[name] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
