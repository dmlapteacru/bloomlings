using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Playtest.Design;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// Checks that keep the painters' pictures cheap (the lag the volume look brought, spec 005 FR-044 to FR-048, and the
    /// frame-time harness <see cref="Perf"/>): an animation draws the pictures its first frame made, never a new size each
    /// frame (a press's squash and sink, a breath, a card's pop, a sheet's rise, a switch's knob), and the
    /// <see cref="PictureStore"/> gives back exactly what it kept, for its key and size only.
    /// </summary>
    public static class PictureChecks
    {
        private const float Frame = 1f / 60f;

        public static IEnumerable<string> Run()
        {
            var problems = new List<string>();
            problems.AddRange(Animations());
            problems.AddRange(Store());
            return problems;
        }

        /// <summary>Draws each animated kit element for a second and fails one that makes a picture after its first frame.</summary>
        private static IEnumerable<string> Animations()
        {
            var cases = new (string Name, Action<SkiaPainter, int> Draw)[]
            {
                ("a pressed and released Play button, breathing", (p, frame) =>
                {
                    Box box = Box.FromCenter(p.Width / 2f, p.Height * 0.6f, p.U(560f), p.U(150f));
                    Kit.PrimaryButton(p, box, "Play", () => { }, decorate: true, playArrow: true, breathe: true);
                }),
                ("a pressed and released round button", (p, frame) => Kit.RoundButton(p, p.Width / 2f, p.Height * 0.3f, p.U(110f), "ui.settings", () => { })),
                ("a pressed and released cream button", (p, frame) =>
                    Kit.SecondaryButton(p, Box.FromCenter(p.Width / 2f, p.Height * 0.45f, p.U(520f), p.U(120f)), "Restart", () => { }, "ui.restart")),
                ("a pressed switch", (p, frame) => Kit.Toggle(p, Box.FromCenter(p.Width / 2f, p.Height * 0.8f, p.U(150f), p.U(80f)), true, () => { })),
                ("a popping card", (p, frame) =>
                {
                    Kit.Card(p, 600f, "Settings", () => { }, Kit.Pop(frame * Frame), T.Title);
                    Kit.EndCard(p);
                }),
                ("a rising sheet", (p, frame) =>
                {
                    Kit.Sheet(p, 500f, "Out of space", "Choose a rescue", Kit.SheetRise(frame * Frame));
                    Kit.EndSheet(p);
                }),
            };

            foreach ((string name, Action<SkiaPainter, int> draw) in cases)
            {
                using var p = new SkiaPainter(1080, 2340, new Insets(110, 63), draw: false);
                long late = 0;
                for (int frame = 0; frame < 60; frame++)
                {
                    // A finger down from the 5th to the 20th frame over the screen's middle column, then the spring-back.
                    if (frame == 5)
                    {
                        Box target = p.Targets.Count > 0 ? p.Targets[p.Targets.Count - 1] : new Box(0f, 0f, p.Width, p.Height);
                        p.TouchDown(target.CenterX, target.CenterY);
                    }
                    else if (frame == 20)
                    {
                        p.TouchUp(p.Finger?.X ?? 0f, p.Finger?.Y ?? 0f);
                    }

                    p.Now += Frame;
                    p.BeginFrame();
                    PaintStats.Counts before = PaintStats.Snapshot();
                    draw(p, frame);
                    if (frame > 0)
                    {
                        late += (PaintStats.Snapshot() - before).Rasters;
                    }
                }

                if (late > 0)
                {
                    yield return "pictures: " + name + " made " + late + " pictures after its first frame (a new size each frame?)";
                }
            }
        }

        /// <summary>The store's round trip in a fresh folder: the same bytes back, nothing for another key or size, old builds dropped.</summary>
        private static IEnumerable<string> Store()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloomlings-picture-store-check");
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }

            var problems = new List<string>();
            var old = new PictureStore(root, "build-1", 1024 * 1024);
            byte[] kept = UiRaster.ButtonPlate(64, 40, 0.5f);
            old.Write("ui.button.plate/0.5", 64, 40, (byte[])kept.Clone(), kept.Length);
            if (!old.Flush(TimeSpan.FromSeconds(30)))
            {
                problems.Add("store: the write did not finish");
            }

            var buffer = new byte[kept.Length + 16];
            if (!old.TryRead("ui.button.plate/0.5", 64, 40, 4, buffer) || !buffer.Take(kept.Length).SequenceEqual(kept))
            {
                problems.Add("store: a kept picture does not read back the same");
            }

            if (old.TryRead("ui.button.plate/0.34", 64, 40, 4, buffer) || old.TryRead("ui.button.plate/0.5", 64, 48, 4, buffer))
            {
                problems.Add("store: a picture read back for another key or size");
            }

            // Another build's store drops the first build's pictures.
            var next = new PictureStore(root, "build-2", 1024 * 1024);
            next.Flush(TimeSpan.FromSeconds(30));
            if (Directory.Exists(Path.Combine(root, "build-1")) || next.TryRead("ui.button.plate/0.5", 64, 40, 4, buffer))
            {
                problems.Add("store: another build's pictures were kept");
            }

            // Over its budget the store drops the least recently used pictures.
            var small = new PictureStore(Path.Combine(root, "small"), "b", 3 * kept.Length);
            for (int i = 0; i < 6; i++)
            {
                small.Write("p" + i, 64, 40, (byte[])kept.Clone(), kept.Length);
            }

            small.Flush(TimeSpan.FromSeconds(30));
            long bytes = Directory.GetFiles(Path.Combine(root, "small", "b")).Sum(f => new FileInfo(f).Length);
            if (bytes > 3 * (kept.Length + 64) || !small.TryRead("p5", 64, 40, 4, buffer))
            {
                problems.Add("store: over budget it keeps " + bytes + " bytes, or dropped the newest picture");
            }

            small.Flush(TimeSpan.FromSeconds(30));
            old.Flush(TimeSpan.FromSeconds(30));
            next.Flush(TimeSpan.FromSeconds(30));
            if (!old.Enabled || !next.Enabled || !small.Enabled)
            {
                problems.Add("store: an I/O failure turned it off");
            }

            Directory.Delete(root, recursive: true);
            return problems;
        }
    }
}
