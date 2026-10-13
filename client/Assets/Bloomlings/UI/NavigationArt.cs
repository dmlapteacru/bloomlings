using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    public static class NavigationArt
    {
        private static readonly Dictionary<string, Sprite?> Sprites = new Dictionary<string, Sprite?>();
        public static Sprite? Get(string name)
        {
            if (Sprites.TryGetValue(name, out Sprite? cached)) return cached;
            string resource = NavigationSkin.Resource(name);
            Texture2D? texture = Resources.Load<Texture2D>("Navigation/" + resource.Substring(11, resource.Length - 15));
            if (texture == null) return Sprites[name] = null;
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector4 border = Vector4.zero;
            if (name.StartsWith("navigation/icon-", StringComparison.Ordinal))
            {
                var c = NavigationSkin.IconCrop(name, texture.width, texture.height);
                rect = new Rect(c.Left, texture.height - c.Top - c.Height, c.Width, c.Height);
            }
            else if (name.StartsWith("navigation/button-", StringComparison.Ordinal))
            {
                var c = NavigationSkin.ButtonCrop(name, texture.width, texture.height);
                rect = new Rect(c.Left, texture.height-c.Top-c.Height, c.Width, c.Height);
                border = new Vector4(c.Width*NavigationSkin.ButtonBorderX,c.Height*NavigationSkin.ButtonBorderY,c.Width*NavigationSkin.ButtonBorderX,c.Height*NavigationSkin.ButtonBorderY);
            }
            else if (name == "navigation/plus-v2")
            {
                var c=NavigationSkin.PlusCrop(texture.width,texture.height);
                rect=new Rect(c.Left,texture.height-c.Top-c.Height,c.Width,c.Height);
            }
            else border = new Vector4(texture.width * NavigationSkin.SignBorderX, texture.height * NavigationSkin.SignBorderY, texture.width * NavigationSkin.SignBorderX, texture.height * NavigationSkin.SignBorderY);
            return Sprites[name] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
