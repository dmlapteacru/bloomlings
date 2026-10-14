using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Exercises actual navigation with a deliberately stalled resource producer.</summary>
    public static class LoadingChecks
    {
        public static IEnumerable<string> Run(ContentSet content)
        {
            var failures = new List<string>();
            string folder = Path.Combine(Path.GetTempPath(), "bloomlings-loading-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var painter = new SkiaPainter(540, 1170, new Insets(55, 32), draw: false);
                IPainter proxy = DispatchProxy.Create<IPainter, PreparationProbe>();
                var probe = (PreparationProbe)(object)proxy;
                probe.Painter = painter;
                var app = new DesignApp(folder, content, new Silence());
                void Frames(int count)
                {
                    for (int i = 0; i < count; i++)
                    {
                        painter.Now += .1f;
                        painter.BeginFrame();
                        app.Draw(proxy, .1f);
                    }
                }
                Frames(60);
                if (app.Screen != Screen.Splash || app.SplashLoaded >= 1)
                    failures.Add("loading: a slow resource producer must keep the splash closed beyond the former three-second timeout");
                probe.Ready = true;
                Frames(30);
                if (app.Screen != Screen.Level || app.SplashOpening)
                    failures.Add("loading: completing resource preparation must release the splash");
                app.Meta.CompleteLevel(1, Core.Definitions.DifficultyClass.Normal, 0);
                app.NextLevel();
                probe.Ready = false;
                Frames(45);
                if (!app.Transitioning || app.Level?.Level != 2)
                    failures.Add("loading: Next must switch under cover and wait for the new level's resources");
                probe.Ready = true;
                Frames(30);
                if (app.Transitioning || app.Level?.Level != 2)
                    failures.Add("loading: a ready next level must open and finish its transition");
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
            return failures;
        }

        public class PreparationProbe : DispatchProxy, IPictureWarmer
        {
            public IPainter Painter = null!;
            public bool Ready;
            public bool WarmsPictures => true;
            public float WarmProgress => Ready ? 1f : .5f;
            public void Warm(Action draw) => draw();
            protected override object? Invoke(MethodInfo? method, object?[]? args)
            {
                // Bundled sprites exist before the worker decodes their size/alpha. Layout must tolerate that state.
                if (!Ready)
                {
                    if (method!.Name == nameof(IPainter.HasSprite)) return true;
                    if (method.Name == nameof(IPainter.SpriteSize) || method.Name == nameof(IPainter.SpriteAlpha)
                        || method.Name == nameof(IPainter.Sprite) || method.Name == nameof(IPainter.SpriteTint)
                        || method.Name == nameof(IPainter.SpriteSkin)) return null;
                }
                return method!.Invoke(Painter, args);
            }
        }
    }
}
