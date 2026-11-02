using System.Collections.Generic;
using Bloomlings.Client.Meta.Album;
using Bloomlings.Client.UI.Design;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The Scene Album's pictures in Unity (spec 007; the playtest's painter draws the same files): the owner's layers from
    /// <c>Resources/Album/{scene}-{object}</c> and <c>Resources/Album/{scene}-background</c>
    /// (<see cref="AlbumCatalog.PictureFolder"/>), and made once from each layer's pixels (read back, the imported texture
    /// keeps no readable copy): its locked copy (<see cref="AlbumLook.LockedPixels"/>: greyscale at 82 %), its silhouette
    /// (white with its alpha, tinted gold for the outline) and its alpha (the tap's hit test,
    /// <see cref="AlbumLook.HitObject"/>). Every loader returns null while its picture is missing. Flat 2D pictures only.
    /// </summary>
    public static class AlbumArt
    {
        private static readonly Dictionary<string, Texture2D?> Pictures = new Dictionary<string, Texture2D?>();
        private static readonly Dictionary<string, Texture2D?> Locked = new Dictionary<string, Texture2D?>();
        private static readonly Dictionary<string, Texture2D?> Silhouettes = new Dictionary<string, Texture2D?>();
        private static readonly Dictionary<string, (byte[] Alpha, int Width, int Height)?> Alphas = new Dictionary<string, (byte[], int, int)?>();

        /// <summary>A scene's background or an object's layer in colour (<c>forest-owl</c>), or null while it is missing.</summary>
        public static Texture2D? Picture(string name)
        {
            if (!Pictures.TryGetValue(name, out Texture2D? texture))
            {
                texture = Resources.Load<Texture2D>(AlbumCatalog.PictureFolder + "/" + name);
                Pictures[name] = texture;
            }

            return texture;
        }

        /// <summary>A layer's locked copy (greyscale, 82 % brightness, its alpha kept), or its colour picture where read-back fails.</summary>
        public static Texture2D? LockedPicture(string name) => Derived(Locked, name, AlbumLook.LockedPixels, "#locked");

        /// <summary>A layer's silhouette (white with its alpha), tinted by the image's colour for the golden outline.</summary>
        public static Texture2D? Silhouette(string name) => Derived(Silhouettes, name, rgba =>
        {
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                rgba[i] = 255;
                rgba[i + 1] = 255;
                rgba[i + 2] = 255;
            }
        }, "#silhouette");

        /// <summary>A layer's alpha, one byte a pixel with rows from the top, and its size; null while it is missing.</summary>
        public static (byte[] Alpha, int Width, int Height)? Alpha(string name)
        {
            if (Alphas.TryGetValue(name, out (byte[] Alpha, int Width, int Height)? cached))
            {
                return cached;
            }

            Texture2D? texture = Picture(name);
            byte[]? rows = texture == null ? null : OwnerArt.ReadBack(texture);
            (byte[] Alpha, int Width, int Height)? alpha = null;
            if (rows != null)
            {
                int w = texture!.width;
                int h = texture.height;
                var bytes = new byte[w * h];
                for (int y = 0; y < h; y++)
                {
                    // Unity's rows run from the bottom; the kit's from the top.
                    int from = (h - 1 - y) * w;
                    for (int x = 0; x < w; x++)
                    {
                        bytes[(y * w) + x] = rows[((from + x) * 4) + 3];
                    }
                }

                alpha = (bytes, w, h);
            }

            Alphas[name] = alpha;
            return alpha;
        }

        private static Texture2D? Derived(Dictionary<string, Texture2D?> cache, string name, System.Action<byte[]> make, string suffix)
        {
            if (cache.TryGetValue(name, out Texture2D? cached))
            {
                return cached;
            }

            Texture2D? texture = Picture(name);
            byte[]? rows = texture == null ? null : OwnerArt.ReadBack(texture);
            Texture2D? result = texture;
            if (rows != null)
            {
                make(rows);
                result = new Texture2D(texture!.width, texture.height, TextureFormat.RGBA32, true)
                {
                    name = name + suffix,
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                result.SetPixelData(rows, 0);
                result.Apply(true, true);
            }

            cache[name] = result;
            return result;
        }
    }
}
