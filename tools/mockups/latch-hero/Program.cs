// MOCK ONLY (the owner's latch request of 2026-11-02, not game code): plays a latch level's saved solution in the full
// playtest's painter and writes real frames around the latch's opening, the layout boxes, and the pieces the composited
// stills use: the board's key standing upright (bow up) and a padlock whose body is the held pod's colour.
// Usage: dotnet run --project tools/mockups/latch-hero -- <rounds dir> <out dir> [level]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;
using Bloomlings.Playtest.Preview;
using SkiaSharp;

string roundsDir = Path.GetFullPath(args[0]);
string outDir = Path.GetFullPath(args[1]);
int level = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 63;
Directory.CreateDirectory(outDir);

string folder = Path.Combine(roundsDir, "published");
RoundContentSet rounds = RoundContentSet.Read(path =>
{
    string file = Path.Combine(folder, path);
    return File.Exists(file) ? File.ReadAllBytes(file) : null;
});
var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
string data = Path.Combine(Path.GetTempPath(), "bloomlings-latch-hero-" + Guid.NewGuid().ToString("N"));
using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
void Frame(float dt)
{
    painter.Now += dt;
    painter.BeginFrame();
    app.Draw(painter, dt);
}

void Save(string name, byte[] png) => File.WriteAllBytes(Path.Combine(outDir, name + ".png"), png);

app.LoadLevel(level);
RoundLevelScreen s = app.Round!;
s.SkipGuide();
for (int i = 0; i < 30; i++)
{
    Frame(1f / 30f);
}

if (s.Intro != null)
{
    s.CloseIntro();
}

for (int i = 0; i < 30; i++)
{
    Frame(1f / 30f);
}

RoundLatch latch = s.Round.Latches[0];
RoundAnimator a = s.Animator;
var proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(roundsDir, "proofs", "L" + level.ToString("0000", CultureInfo.InvariantCulture) + ".json")));

// Play to the last frame with the closed latch on top of its column (its key still on the board), then hide the latch for
// one frame by reflection (no key, the pod plain) and put it back.
FieldInfo latched = typeof(RoundAnimator).GetField("_latched", BindingFlags.NonPublic | BindingFlags.Instance)!;
FieldInfo openedAt = typeof(RoundAnimator).GetField("_latchOpenedAt", BindingFlags.NonPublic | BindingFlags.Instance)!;
byte[]? baseFrame = null;
byte[]? noKey = null;
bool Top() => a.LatchClosed(0) && s.Play.Timeline.State.Heads[latch.Column] == latch.Depth;
bool opened = PlayTo(s, proof, Frame, () =>
{
    if (a.LatchOpenSince(0) >= 0f)
    {
        return true;
    }

    if (Top() && !s.Play.Timeline.Pending)
    {
        baseFrame = painter.Png();
        bool[] l = (bool[])latched.GetValue(a)!;
        long[] o = (long[])openedAt.GetValue(a)!;
        bool keepL = l[0];
        long keepO = o[0];
        l[0] = false;
        o[0] = RoundAnimator.Before;
        painter.BeginFrame();
        app.Draw(painter, 0f);
        noKey = painter.Png();
        l[0] = keepL;
        o[0] = keepO;
        painter.BeginFrame();
        app.Draw(painter, 0f);
    }

    return false;
});
if (!opened || baseFrame == null || noKey == null)
{
    Console.WriteLine("No closed latch on top before its opening on L" + level);
    return 1;
}

Save("frame-base", baseFrame);
Save("frame-nokey", noKey);
Save("frame-opening", painter.Png());
for (int i = 0; i < 24; i++)
{
    Frame(1f / 30f);
}

Save("frame-after", painter.Png());

painter.BeginFrame();
DesignApp.DrawBackdrop(painter, BackdropScene.Gameplay, level);
Save("backdrop", painter.Png());

// The layout boxes.
RoundPainter.LayoutCache cache = RoundPainter.Layout(painter, s, RoundPainter.HasBoosters(s), RoundPainter.Badge(s).HasValue);
Box keyCell = cache.Board.CellBox(latch.Key);
Box keyBox = RoundLook.KeyBox(keyCell, cache.Board.Surface);
int material = a.KeyMaterial(0);
Rgba metal = RoundLook.KeyMetal(material);
var json = new StringBuilder();
string B(Box b) => string.Format(CultureInfo.InvariantCulture, "[{0:0.##},{1:0.##},{2:0.##},{3:0.##}]", b.Left, b.Top, b.Right, b.Bottom);
json.Append("{\n");
json.Append("  \"level\": " + level + ",\n");
json.Append("  \"column\": " + latch.Column + ",\n");
json.Append("  \"count\": " + s.Round.Pods[s.Round.Tray[latch.Column][latch.Depth]].Count + ",\n");
json.Append("  \"metal\": \"#" + metal.R.ToString("X2") + metal.G.ToString("X2") + metal.B.ToString("X2") + "\",\n");
json.Append("  \"keyCell\": " + B(keyCell) + ",\n");
json.Append("  \"keyBox\": " + B(keyBox) + ",\n");
json.Append("  \"surface\": " + B(cache.Board.Surface) + ",\n");
var pods = new List<string>();
for (int c = 0; c < s.Round.Tray.Count; c++)
{
    for (int r = 0; r < RoundScreenLook.PodRows; r++)
    {
        pods.Add("\"" + c + "," + r + "\": {\"place\": " + B(cache.Regions.Pod(c, r)) + ", \"cube\": " + B(PodChip.Cube(cache.Regions.Pod(c, r)).Frame) + "}");
    }
}

json.Append("  \"pods\": {" + string.Join(", ", pods) + "},\n");
var cols = new List<string>();
for (int c = 0; c < s.Round.Tray.Count; c++)
{
    cols.Add(B(cache.Regions.Columns[c]));
}

json.Append("  \"columns\": [" + string.Join(", ", cols) + "]\n}\n");
File.WriteAllText(Path.Combine(outDir, "geometry.json"), json.ToString());

// The pieces.
WritePng(Path.Combine(outDir, "piece-key-upright.png"), 600, 1000, Mock.UprightKey(600, 1000, 800f, metal));
foreach (bool open in new[] { false, true })
{
    foreach (bool arms in new[] { false, true })
    {
        string name = "piece-padlock" + (open ? "-open" : string.Empty) + (arms ? "-arms" : string.Empty);
        WritePng(Path.Combine(outDir, name + ".png"), 600, 600, Mock.Padlock(600, metal, open, arms));
    }
}

Console.WriteLine("L" + level + " frames and pieces in " + outDir);
if (Directory.Exists(data))
{
    Directory.Delete(data, true);
}

return 0;

static bool PlayTo(RoundLevelScreen s, RoundProof proof, Action<float> frame, Func<bool> done)
{
    // as LatchStills.Play: the saved solution as a patient player, until done() holds after a frame
    IReadOnlyList<RoundCommand> commands = proof.Solution.Commands;
    for (int c = 0; c <= commands.Count; c++)
    {
        for (float t = 0f; t < 60f; t += 1f / 30f)
        {
            frame(1f / 30f);
            if (done())
            {
                return true;
            }

            if (!s.Play.Timeline.Pending)
            {
                break;
            }
        }

        if (c == commands.Count)
        {
            break;
        }

        RoundCommand command = commands[c];
        if (command.Type == RoundCommandType.Tap) s.Tap(command.Index); else s.BuildBridge(command.Index);
    }

    return false;
}

static void WritePng(string path, int w, int h, byte[] rgba)
{
    var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
    using var bitmap = new SKBitmap(info);
    System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
    using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
    File.WriteAllBytes(path, encoded.ToArray());
}

/// <summary>Mock rasters after UiRaster.RoundKey and UiRaster.Padlock (copied and changed here, the game's stay as they are).</summary>
internal static class Mock
{
    private struct C
    {
        public float R, G, B;
        public C(Rgba c) { R = c.R; G = c.G; B = c.B; }
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    private static float Length(float x, float y) => MathF.Sqrt((x * x) + (y * y));
    private static float Coverage(float d) => Clamp01(0.5f - d);

    private static float RoundRect(float x, float y, float l, float t, float r, float b, float radius)
    {
        float hx = (r - l) / 2f;
        float hy = (b - t) / 2f;
        float rad = Math.Min(radius, Math.Min(hx, hy));
        float qx = Math.Abs(x - ((l + r) / 2f)) - hx + rad;
        float qy = Math.Abs(y - ((t + b) / 2f)) - hy + rad;
        return Length(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - rad;
    }

    private static float Segment(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        float t = Clamp01((((px - ax) * dx) + ((py - ay) * dy)) / ((dx * dx) + (dy * dy)));
        return Length(px - (ax + (dx * t)), py - (ay + (dy * t)));
    }

    private static void Over(ref C c, ref float a, Rgba col, float t)
    {
        t = Clamp01(t) * (col.A / 255f);
        if (t <= 0f)
        {
            return;
        }

        float na = t + (a * (1f - t));
        float k = t / na;
        c.R += (col.R - c.R) * k;
        c.G += (col.G - c.G) * k;
        c.B += (col.B - c.B) * k;
        a = na;
    }

    private static void Put(byte[] pixels, int width, int px, int py, C c, float alpha)
    {
        int i = ((py * width) + px) * 4;
        pixels[i] = (byte)Math.Clamp((int)MathF.Round(c.R), 0, 255);
        pixels[i + 1] = (byte)Math.Clamp((int)MathF.Round(c.G), 0, 255);
        pixels[i + 2] = (byte)Math.Clamp((int)MathF.Round(c.B), 0, 255);
        pixels[i + 3] = (byte)Math.Clamp((int)MathF.Round(Clamp01(alpha) * 255f), 0, 255);
    }

    // The game's key shape (UiRaster.KeyShape), its axis pointing straight down: the bow up, the bit's teeth to the left.
    private static float KeyShape(float x, float y, float cx, float cy, float len)
    {
        float u = (y - cy) / len;
        float v = -(x - cx) / len;
        float bowCx = -0.29f;
        float ring = Math.Max(Length(u - bowCx, v) - 0.21f, 0.08f - Length(u - (bowCx - 0.015f), v + 0.01f));
        float collar = RoundRect(u, v, -0.12f, -0.085f, -0.05f, 0.085f, 0.03f);
        float shaft = RoundRect(u, v, -0.12f, -0.058f, 0.48f, 0.058f, 0.05f);
        float bit = RoundRect(u, v, 0.2f, -0.01f, 0.46f, 0.22f, 0.03f);
        float notch = RoundRect(u, v, 0.285f, 0.11f, 0.355f, 0.32f, 0.018f);
        float bitCut = Math.Max(bit, -notch);
        return Math.Min(Math.Min(ring, collar), Math.Min(shaft, bitCut)) * len;
    }

    /// <summary>The board's key standing upright, len px long, lit from the upper left as UiRaster.RoundKey.</summary>
    public static byte[] UprightKey(int width, int height, float len, Rgba metal)
    {
        var pixels = new byte[width * height * 4];
        float cx = width / 2f;
        float cy = height / 2f;
        float line = Math.Max(1.2f, len * 0.02f);
        float halo = len * 0.1f;
        float depthX = len * 0.04f;
        float depthY = len * 0.035f;
        Rgba ink = metal.Darken(0.62f);
        Rgba side = metal.Darken(0.38f);
        float bevel = len * 0.035f;
        for (int py = 0; py < height; py++)
        {
            float y = py + 0.5f;
            for (int px = 0; px < width; px++)
            {
                float x = px + 0.5f;
                float sweep = float.MaxValue;
                for (int k = 1; k <= 12; k++)
                {
                    float t = k / 12f;
                    sweep = Math.Min(sweep, KeyShape(x - (depthX * t), y - (depthY * t), cx, cy, len));
                }

                float face = KeyShape(x, y, cx, cy, len);
                float all = Math.Min(face, sweep);
                if (all > halo + line)
                {
                    continue;
                }

                var c = new C(Rgba.White);
                float a = 0f;
                float glow = Clamp01(1f - ((all - line) / halo));
                Over(ref c, ref a, RoundLook.KeyHalo, 0.95f * MathF.Sqrt(glow) * glow);
                Over(ref c, ref a, ink, Coverage(all - line));
                Over(ref c, ref a, side, Coverage(sweep));
                float cover = Coverage(face);
                if (cover > 0f)
                {
                    float tone = Clamp01(0.5f + ((x - cx) / (len * 0.25f)) + ((y - cy) / (len * 3f)));
                    Rgba col = metal.Lighten(0.2f).Mix(metal.Darken(0.12f), tone);
                    float inside = -face;
                    if (inside < bevel)
                    {
                        float gx = KeyShape(x + 1f, y, cx, cy, len) - KeyShape(x - 1f, y, cx, cy, len);
                        float gy = KeyShape(x, y + 1f, cx, cy, len) - KeyShape(x, y - 1f, cx, cy, len);
                        float gl = Math.Max(0.001f, Length(gx, gy));
                        float facing = -((gx / gl) + (gy / gl)) * 0.7071f;
                        float k = Clamp01(1f - (inside / bevel));
                        col = facing > 0f ? col.Mix(Rgba.White, 0.55f * facing * k) : col.Mix(metal.Darken(0.35f), -0.5f * facing * k);
                    }

                    float u = (y - cy);
                    float shaftShine = (u > -len * 0.1f && u < len * 0.42f) ? Clamp01(1f - (Math.Abs((x - cx) + (len * 0.022f)) / (len * 0.012f))) : 0f;
                    float bowShine = Length((y - cy) + (len * 0.36f), ((x - cx) + (len * 0.1f)) * 1.6f) / (len * 0.06f);
                    col = col.Mix(Rgba.White, Math.Max(0.55f * shaftShine, 0.6f * Clamp01(1f - bowShine)));
                    Over(ref c, ref a, col, cover);
                }

                if (a > 0f)
                {
                    Put(pixels, width, px, py, c, a);
                }
            }
        }

        return pixels;
    }

    /// <summary>
    /// UiRaster.Padlock with its body in <paramref name="body"/> (the held pod's colour) and a brass shackle in place of the
    /// steel one; <paramref name="arms"/> the acorn's arms as in the game's.
    /// </summary>
    public static byte[] Padlock(int size, Rgba bodyColor, bool open, bool arms)
    {
        var pixels = new byte[size * size * 4];
        float s = size;
        Rgba brass = bodyColor;
        Rgba steel = RoundLook.PadlockBrass;
        Rgba ink = RoundLook.PadlockInk;
        float line = Math.Max(1f, s * 0.03f);
        float bl = s * 0.24f;
        float br = s * 0.76f;
        float bt = s * 0.53f;
        float bb = s * 0.95f;
        float bodyR = s * 0.09f;
        float depth = s * 0.075f;
        float lift = open ? s * 0.12f : 0f;
        float shCx = s * 0.5f;
        float shR = s * 0.15f;
        float shBar = s * 0.07f;
        float shCy = (s * 0.45f) - lift;
        float legBottom = bt + (s * 0.04f) - lift;
        Rgba nut = Rgba.FromHex("#C07A3A");
        float armW = s * 0.06f;
        (float X, float Y)[] armA = { (s * 0.17f, s * 0.05f), (s * 0.2f, s * 0.3f), (s * 0.33f, s * 0.44f) };
        (float X, float Y)[] armB = { (s * 0.83f, s * 0.05f), (s * 0.8f, s * 0.3f), (s * 0.67f, s * 0.44f) };
        for (int py = 0; py < size; py++)
        {
            float y = py + 0.5f;
            for (int px = 0; px < size; px++)
            {
                float x = px + 0.5f;
                var c = new C(ink);
                float a = 0f;
                float ring = Math.Abs(Length(x - shCx, y - shCy) - shR) - (shBar / 2f);
                if (y > shCy)
                {
                    float legL = Math.Abs(x - (shCx - shR)) - (shBar / 2f);
                    float legR = Math.Abs(x - (shCx + shR)) - (shBar / 2f);
                    float legY = y - legBottom;
                    ring = Math.Max(Math.Min(legL, open ? Math.Max(legR, y - (shCy + (s * 0.06f))) : legR), legY);
                }

                if (ring - line < 0.5f)
                {
                    Over(ref c, ref a, ink, Coverage(ring - line));
                    float sc = Coverage(ring);
                    if (sc > 0f)
                    {
                        float lit = Clamp01(0.5f - ((x - shCx) / (shR * 2.4f)) - ((y - shCy) / (shR * 3f)));
                        Rgba col = steel.Darken(0.25f).Mix(steel.Lighten(0.45f), lit);
                        float core = Math.Abs(Length(x - shCx, y - shCy) - shR);
                        col = col.Mix(Rgba.White, 0.35f * Clamp01(1f - (core / (shBar * 0.22f))) * (y < shCy ? 1f : 0.6f));
                        Over(ref c, ref a, col, sc);
                    }
                }

                float bodyD = RoundRect(x, y, bl, bt, br, bb, bodyR);
                if (bodyD - line < 0.5f)
                {
                    Over(ref c, ref a, ink, Coverage(bodyD - line));
                    float cover = Coverage(bodyD);
                    if (cover > 0f)
                    {
                        bool front = y > bb - depth;
                        float shade = Clamp01((x - bl) / (br - bl));
                        Rgba col = front
                            ? brass.Darken(0.28f).Mix(brass.Darken(0.42f), shade)
                            : brass.Lighten(0.22f).Mix(brass.Darken(0.12f), Clamp01((shade * 0.7f) + ((y - bt) / (bb - bt) * 0.5f)));
                        float rim = -bodyD;
                        if (!front && rim < s * 0.05f)
                        {
                            float k = Clamp01(1f - (rim / (s * 0.05f)));
                            bool litRim = (y - bt) < (s * 0.1f) || (x - bl) < (s * 0.1f);
                            col = col.Mix(litRim ? Rgba.White : brass.Darken(0.3f), (litRim ? 0.45f : 0.25f) * k);
                        }

                        float shine = Length((x - (bl + (s * 0.13f))) / (s * 0.08f), (y - (bt + (s * 0.09f))) / (s * 0.04f));
                        col = col.Mix(Rgba.White, 0.5f * Clamp01(1f - shine));
                        Over(ref c, ref a, col, cover);

                        // the keyhole, in the shackle's brass so it reads on any body colour
                        float kx = s * 0.5f;
                        float ky = bt + ((bb - depth - bt) * 0.42f);
                        float head = Length(x - kx, y - ky) - (s * 0.05f);
                        float slotHalf = (s * 0.025f) + ((y - ky) * 0.18f);
                        float slot = Math.Max(Math.Abs(x - kx) - slotHalf, Math.Max(ky - y, y - (ky + (s * 0.12f))));
                        float hole = Math.Min(head, slot);
                        Over(ref c, ref a, steel.Lighten(0.2f), Coverage(hole - Math.Max(1.5f, s * 0.022f)));
                        Over(ref c, ref a, ink, Coverage(hole));
                    }
                }

                if (arms)
                {
                    foreach ((float X, float Y)[] arm in new[] { armA, armB })
                    {
                        float d = Math.Min(Segment(x, y, arm[0].X, arm[0].Y, arm[1].X, arm[1].Y), Segment(x, y, arm[1].X, arm[1].Y, arm[2].X, arm[2].Y)) - armW;
                        float hand = Length(x - arm[2].X, y - arm[2].Y) - (armW * 1.35f);
                        float limb = Math.Min(d, hand);
                        if (limb - line < 0.5f)
                        {
                            Over(ref c, ref a, Rgba.FromHex("#3F2810"), Coverage(limb - line));
                            float lit = Clamp01(0.5f - ((y - arm[2].Y) / (s * 0.3f)));
                            Over(ref c, ref a, nut.Darken(0.1f).Mix(nut.Lighten(0.2f), lit), Coverage(limb));
                        }
                    }
                }

                if (a > 0f)
                {
                    Put(pixels, size, px, py, c, a);
                }
            }
        }

        return pixels;
    }
}
