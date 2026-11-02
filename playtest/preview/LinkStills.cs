using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--link-stills &lt;rounds dir&gt; &lt;out dir&gt; [level] [tag]</c>: the owner's phone report of 2026-11-02 (L54) for review, on
    /// a rounds folder with <c>published/</c> and <c>proofs/</c>: the level at its start, the same after a tap on a top pod
    /// whose link partner waits deeper than the rows show (the straight tube towards the hidden partner and its pulse,
    /// <see cref="RoundAnimator.LinkMarks"/>, <see cref="LinkTube.HiddenTube"/>), and a frame with walkers under way (with a close-up of the board) on that level
    /// and on Level 1 (big cubes), so the walkers' size on small cubes (<see cref="ClearStyles.SmallCubeBoost"/>) can be
    /// compared; <c>tag</c> names the walker stills (e.g. <c>before</c> / <c>after</c>). Review material only.
    /// </summary>
    public static class LinkStills
    {
        public static int Write(string roundsDir, string outDir, int level, string tag)
        {
            string folder = Path.Combine(roundsDir, "published");
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-link-stills-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);
            try
            {
                using var painter = new SkiaPainter(1080, 2424, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                void Frame(float dt)
                {
                    painter.Now += dt;
                    painter.BeginFrame();
                    app.Draw(painter, dt);
                }

                void Save(string name, byte[] png) => File.WriteAllBytes(Path.Combine(outDir, name + ".png"), png);

                RoundLevelScreen Open(int n)
                {
                    app.LoadLevel(n);
                    RoundLevelScreen screen = app.Round!;
                    screen.SkipGuide();
                    for (int i = 0; i < 30; i++)
                    {
                        Frame(1f / 30f);
                    }

                    if (screen.Intro != null)
                    {
                        screen.CloseIntro();
                    }

                    for (int i = 0; i < 30; i++)
                    {
                        Frame(1f / 30f);
                    }

                    return screen;
                }

                string L(int n) => "L" + n.ToString("00", CultureInfo.InvariantCulture);
                RoundLevelScreen s;
                // ---- The level at its start, then a hidden partner's tube (next column and across one), each before and after a tap ----
                s = Open(level);
                Save(L(level) + "-start", painter.Png());
                RoundPainter.LayoutCache cache = RoundPainter.Layout(painter, s, true, false);
                var marks = new List<RoundLinkMark>();
                var saved = new HashSet<string>();
                Box tray = new Box(0f, cache.Regions.Columns[0].Top - 40f, 1080f, 2424f);

                // A top pod whose partner waits deeper than the rows, in the next column ("next") or across one ("across").
                string? Kind(out int pod)
                {
                    pod = -1;
                    if (s.Animator.Busy || s.Play.Timeline.Pending)
                    {
                        return null;
                    }

                    s.Animator.LinkMarks(cache.Regions.PodRows, marks);
                    foreach (RoundLinkMark mark in marks)
                    {
                        if (s.Animator.DepthOf(mark.Pod) != 0)
                        {
                            continue;
                        }

                        string kind = Math.Abs(mark.Column - mark.ShownColumn) > 1 ? "across" : "next";
                        if (!saved.Contains(kind))
                        {
                            pod = mark.Pod;
                            return kind;
                        }
                    }

                    return null;
                }

                void Shoot(string kind, int pod)
                {
                    saved.Add(kind);
                    for (int i = 0; i < 20; i++)
                    {
                        Frame(1f / 30f);
                    }

                    Save(L(level) + "-tube-" + kind, painter.Png());
                    s.Tap(pod);
                    var frames = new List<byte[]>();
                    for (int i = 0; i < 36; i++)
                    {
                        Frame(1f / 30f);
                        if (i == 4 || i == 9 || i == 17)
                        {
                            frames.Add(painter.Png());
                        }
                    }

                    Save(L(level) + "-tube-" + kind + "-tap", frames[1]);
                    LatchStills.Strip(frames.ToArray(), new[] { tray }, Path.Combine(outDir, L(level) + "-tube-" + kind + "-tap-strip.png"), 0.5f);
                    Console.WriteLine(L(level) + ": tube " + kind + ", tapped pod " + pod);
                }

                // A player's own order (seeded random taps on accepted top pods) until each kind has been seen.
                for (int seed = 1; seed <= 60 && saved.Count < 2; seed++)
                {
                    s = Open(level);
                    cache = RoundPainter.Layout(painter, s, true, false);
                    var random = new Random(seed);
                    for (int step = 0; step < 60 && !s.Won && saved.Count < 2; step++)
                    {
                        for (float t = 0f; t < 20f && (s.Play.Timeline.Pending || s.Animator.Busy); t += 1f / 30f)
                        {
                            Frame(1f / 30f);
                        }

                        string? kind = Kind(out int pod);
                        if (kind != null)
                        {
                            Shoot(kind, pod);
                            break;
                        }

                        var choices = new List<int>();
                        for (int c = 0; c < s.Animator.ColumnCount; c++)
                        {
                            int top = s.Animator.At(c, 0);
                            if (top >= 0 && s.Play.Check(RoundAction.Tap(top)) == RoundRefusal.None)
                            {
                                choices.Add(top);
                            }
                        }

                        if (choices.Count == 0)
                        {
                            break;
                        }

                        s.Tap(choices[random.Next(choices.Count)]);
                        Frame(1f / 30f);
                    }
                }

                Console.WriteLine(L(level) + ": hidden partners' tubes shown: " + string.Join(", ", saved));

                // ---- Walkers under way: this level and Level 1 (big cubes) ----
                foreach (int n in new[] { level, 1 })
                {
                    s = Open(n);
                    RoundPainter.LayoutCache layout = RoundPainter.Layout(painter, s, true, false);
                    var proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(roundsDir, "proofs", "L" + n.ToString("0000", CultureInfo.InvariantCulture) + ".json")));
                    int seen = 0;
                    LatchStills.Play(s, proof, Frame, () => s.Animator.ShownWalkers.Count >= 6 && ++seen >= 20);
                    byte[] png = painter.Png();
                    Save(L(n) + "-walkers-" + tag, png);
                    Box surface = layout.Board.Surface;
                    Box zoom = Box.FromCenter(surface.CenterX, surface.CenterY, surface.Width, surface.Height);
                    LatchStills.Strip(new[] { png }, new[] { zoom }, Path.Combine(outDir, L(n) + "-walkers-" + tag + "-board.png"), 1f);
                    Console.WriteLine(L(n) + ": " + s.Animator.ShownWalkers.Count + " walkers, " + (surface.Width / layout.Board.Cell).ToString("0.0", CultureInfo.InvariantCulture) + " cubes across");
                }

                Console.WriteLine("Link stills in " + outDir);
                return 0;
            }
            finally
            {
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }
        }
    }
}
