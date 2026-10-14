using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Render the shipping layer animation for review; never ships in the APK.</summary>
    internal static class PromoMotion
    {
        public static void Write(string folder)
        {
            Directory.CreateDirectory(folder);
            using var p = new SkiaPainter(680, 340, new Insets(0, 0), true);
            for (int i = 0; i < (int)(HomePromo.CycleSeconds * 12); i++)
            {
                float t = i / 12f;
                p.Now = t;
                p.BeginFrame();
                p.FillRect(new Box(0, 0, 680, 340), Rgba.FromHex("#F5E8CF"));
                HomeScreen.Promo(p, PromoScene.NoAds, HomePromo.SceneBox(25, 110, 290), t, true, 680);
                HomeScreen.Promo(p, PromoScene.Daily, HomePromo.SceneBox(365, 110, 290), t, true, 680);
                File.WriteAllBytes(Path.Combine(folder, $"{i:000}.png"), p.Png());
            }
        }
    }
}
