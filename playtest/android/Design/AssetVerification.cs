#if ASSET_VERIFY
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Android.Graphics;
using Bloomlings.Client.UI.Design;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Droid
{
    /// <summary>Opt-in Android integration check. No Skia/desktop decoder and no placeholder rendering.</summary>
    internal static class AssetVerification
    {
        public static void Run(IBundledFiles files, string directory)
        {
            var failures = new List<string>();
            int bitmaps = 0, heroes = 0, aliases = 0, sprites = 0;
            string[] folders = { "backgrounds", "brand", "icons", "badges", "decor", "avatars", "heromotion", "characters/2d", "characters/3d", "shop", "navigation", "gameplay" };
            foreach (string folder in folders)
            {
                int count = 0;
                foreach (string name in files.Names(folder + "/"))
                {
                    if (!name.EndsWith(".png", StringComparison.Ordinal) && !name.EndsWith(".jpg", StringComparison.Ordinal)) continue;
                    count++;
                    try
                    {
                        using Stream stream = files.Open(name) ?? throw new InvalidDataException("Missing asset");
                        if (folder == "heromotion")
                        {
                            PalettePicture picture = PalettePng.Decode(stream) ?? throw new InvalidDataException("Palette decode failed");
                            if (picture.Width <= 0 || picture.Height <= 0 || picture.Pixels.Length != picture.Width * picture.Height)
                                throw new InvalidDataException("Invalid hero dimensions");
                            heroes++;
                        }
                        else
                        {
                            using Bitmap bitmap = BitmapFactory.DecodeStream(stream) ?? throw new InvalidDataException("Android bitmap decode failed");
                            if (bitmap.Width <= 0 || bitmap.Height <= 0) throw new InvalidDataException("Invalid bitmap dimensions");
                            bitmaps++;
                        }
                        if (folder == "backgrounds" || folder == "avatars" || folder == "heromotion")
                        {
                            string key = name.Substring(0, name.LastIndexOf('.'));
                            using Stream alias = files.Open(key) ?? throw new InvalidDataException("Missing extensionless alias");
                            aliases++;
                        }
                    }
                    catch (Exception error) { failures.Add(name + ": " + error.GetType().Name + " " + error.Message); }
                }
                if (count == 0) failures.Add(folder + ": empty image folder");
                Android.Util.Log.Info("BloomlingsAssets", folder + ": checked " + count + " files");
            }

            // Exercise the real painter's resource resolution and Android atlas crops as well as raw codecs.
            var painter = new AndroidPainter();
            string[] keys = {
                "bg/home", "brand/logo", "icon/nav-home", PrimaryActionDesign.Play, PrimaryActionDesign.Plain,
                GameNavigationDesign.Console, GameNavigationDesign.Selected, GameNavigationDesign.Wallet,
                "navigation/game-control-1", "navigation/game-frame-0", PopupDesign.Frame, PopupDesign.Header,
                PopupDesign.SoundIcon, PopupDesign.HapticsIcon, NavigationSkin.CreamBar, "navigation/icon-packs",
                "shop/price-button", "shop/hero-card-v2", MetaScreenArt.Stage, MetaScreenArt.Podium, MetaScreenArt.Shelf,
                "gameplay/atlas", "gameplay/pod-leaf"
            };
            foreach (string key in keys)
            {
                try
                {
                    var size = painter.SpriteSize(key);
                    if (!painter.HasSprite(key) || !size.HasValue || size.Value.Width <= 0 || size.Value.Height <= 0)
                        throw new InvalidDataException("Sprite or atlas crop unavailable");
                    sprites++;
                }
                catch (Exception error) { failures.Add(key + ": " + error.GetType().Name + " " + error.Message); }
            }
            var report = new StringBuilder();
            report.AppendLine(failures.Count == 0 ? "PASS" : "FAIL");
            report.AppendLine("android_bitmaps=" + bitmaps);
            report.AppendLine("hero_frames=" + heroes);
            report.AppendLine("extensionless_aliases=" + aliases);
            report.AppendLine("painter_sprites=" + sprites);
            report.AppendLine("failures=" + failures.Count);
            foreach (string failure in failures) report.AppendLine(failure);
            File.WriteAllText(System.IO.Path.Combine(directory, "asset-verification.txt"), report.ToString());
            Android.Util.Log.Info("BloomlingsAssets", report.ToString());
        }
    }
}
#endif
