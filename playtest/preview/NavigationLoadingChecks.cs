using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>Real menu layouts, with one more resource released per draw, using the APK's publication gate.</summary>
    public static class NavigationLoadingChecks
    {
        public static IEnumerable<string> Run(ContentSet content)
        {
            var failures = new List<string>();
            string folder = Path.Combine(Path.GetTempPath(), "bloomlings-navigation-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var painter = new SkiaPainter(540, 1170, new Insets(55, 32), draw: false);
                using var frames = new ReadyFrame<string>(_ => { });
                IPainter proxy = DispatchProxy.Create<IPainter, DelayedPictures>();
                var probe = (DelayedPictures)(object)proxy;
                probe.Painter = painter; probe.Frames = frames;
                var app = new DesignApp(folder, content, new Silence(), false);
                app.Meta.SkipTo(87); app.Meta.Collection.Add(content.GetLevel(11), 11); app.GoHome();
                bool Draw(string label)
                {
                    frames.Begin(); painter.BeginFrame(); app.Draw(proxy, 0f);
                    return frames.Publish(label);
                }
                probe.ReleaseAll = true;
                Draw("home");
                probe.ReleaseAll = false;
                var steps = new (string Name, Action Open)[]
                {
                    ("twig-home", () => { app.Meta.Wardrobe.Equip(Family.Twig, TwigGearArt.AcornHatId); app.GoHome(); }),
                    ("shop", () => app.Navigate(NavPlace.Shop)),
                    ("shop-packs-scroll", () => app.ShopScrollOffset = 900f),
                    ("cosmetics", () => { app.StoreTab = 1; app.ShopScrollOffset = 0; }),
                    ("twig-cosmetics", () => app.StoreFamily = 3),
                    ("animations", () => { app.StoreTab = 2; app.StoreFamily = 0; app.ShopScrollOffset = 0; }),
                    ("wardrobe", () => app.Navigate(NavPlace.Wardrobe)),
                    ("leaderboard", () => app.Navigate(NavPlace.Leaderboard)),
                    ("collection", () => app.Navigate(NavPlace.Collection)),
                    ("profile", app.OpenProfile),
                    ("settings", () => app.OpenOverlay(Overlay.Settings)),
                };
                int delayed = 0, draws = 0;
                foreach (var step in steps)
                {
                    string? previous = frames.Current;
                    step.Open();
                    bool ready = Draw(step.Name); draws++;
                    if (!ready) delayed++;
                    for (int i = 0; !ready && i < 250; i++)
                    {
                        if (frames.Current != previous) failures.Add("navigation loading: partial " + step.Name + " replaced the previous page");
                        if (probe.Pending.Count == 0) { failures.Add("navigation loading: no pending dependency for " + step.Name); break; }
                        string key = probe.Pending.First(); probe.Pending.Remove(key); probe.Loaded.Add(key);
                        ready = Draw(step.Name); draws++;
                    }
                    if (!ready || frames.Current != step.Name) failures.Add("navigation loading: " + step.Name + " never becomes complete");
                }
                if (delayed != steps.Length) failures.Add("navigation loading: every cold destination must exercise a delayed dependency");
                Console.WriteLine($"navigation loading: {steps.Length} cold destinations, {draws} staged frames, {probe.Loaded.Count} resources; previous page retained until complete");
                app.OpenStore(); app.StoreTab = 2;
                probe.HoldOnlyTransient = true;
                for (int i = 0; i < 100; i++)
                {
                    frames.Begin(); painter.Now += .05f; painter.BeginFrame(); app.Draw(proxy, .05f);
                    if (!frames.Publish("live-preview")) failures.Add("navigation loading: transient particles paused a complete animation preview");
                }
                if (probe.TransientMisses == 0) failures.Add("navigation loading: live preview never exercised a delayed particle");
                Console.WriteLine($"transient loading: 100 live-preview frames continued with {probe.TransientMisses} delayed effect draws");
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
            return failures;
        }

        public class DelayedPictures : DispatchProxy, ITransientPictures
        {
            public IPainter Painter = null!;
            public ReadyFrame<string> Frames = null!;
            public bool ReleaseAll;
            public bool HoldOnlyTransient;
            public int TransientMisses;
            private bool _transient;
            private readonly Dictionary<string, bool> _synchronousHeroes = new(StringComparer.Ordinal);
            private bool SynchronousHero(string name)
            {
                if (!PainterBase.IsHeroFrame(name)) return false;
                if (!_synchronousHeroes.TryGetValue(name, out bool indexed))
                {
                    using Stream? stream = PlaytestFiles.Open(PainterBase.SpriteResource(name));
                    indexed = stream != null && PalettePng.Decode(stream) != null;
                    _synchronousHeroes[name] = indexed;
                }
                return indexed;
            }
            public void Transient(Action draw)
            {
                bool previous = _transient; _transient = true;
                try { draw(); } finally { _transient = previous; }
            }
            public readonly HashSet<string> Loaded = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> Pending = new HashSet<string>(StringComparer.Ordinal);
            protected override object? Invoke(MethodInfo? method, object?[]? args)
            {
                string name = method!.Name;
                bool sprite = name == nameof(IPainter.HasSprite) || name == nameof(IPainter.SpriteSize)
                    || name == nameof(IPainter.SpriteAlpha) || name == nameof(IPainter.Sprite)
                    || name == nameof(IPainter.SpriteTint) || name == nameof(IPainter.SpriteSkin);
                bool picture = name == nameof(IPainter.Picture) || name == nameof(IPainter.Shape) || name == nameof(IPainter.ShapeOf);
                if (sprite || picture)
                {
                    string key = (sprite ? "sprite/" : "picture/") + (string)args![0]!;
                    // Indexed clips decode synchronously; full-color outfit sprites use the staged bitmap loader.
                    bool exists = !sprite || !SynchronousHero((string)args![0]!) && Painter.HasSprite((string)args[0]!);
                    if (exists)
                    {
                        if (ReleaseAll || HoldOnlyTransient && !_transient) Loaded.Add(key);
                        else if (HoldOnlyTransient && _transient || !Loaded.Contains(key))
                        {
                            Pending.Add(key);
                            if (!_transient) Frames.Missing();
                            else TransientMisses++;
                            return name == nameof(IPainter.HasSprite) ? true : null;
                        }
                    }
                }
                return method.Invoke(Painter, args);
            }
        }
    }
}
