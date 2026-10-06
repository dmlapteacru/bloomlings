using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The clips of a hero's motion (spec 005 FR-028): the idle loop, the reaction and, for a hero that has them, the win's
    /// celebrations (<see cref="HeroMotion.HasWin"/>: Twig's cheer; Sprig's celebrate and, as <see cref="Win2"/>, its clap).
    /// </summary>
    public enum MotionClip
    {
        Idle,
        React,
        Win,

        /// <summary>A hero's second celebration, played on every other of its turns (<see cref="HeroMotion.WinClip"/>).</summary>
        Win2,

        /// <summary>
        /// The idle of a hero that celebrates, baked facing the player for the win and the milestone, where it stands alone
        /// (the owner, 2026-10-05: on Home the heroes turn toward the fountain's middle; <see cref="HeroMotion.HasFront"/>).
        /// Its celebrations are baked the same way.
        /// </summary>
        WinIdle,
    }

    /// <summary>
    /// One pre-rendered frame of a hero's motion: where its picture lies in the frame cell (pixels of a
    /// <see cref="HeroMotion.CellWidth"/> × <see cref="HeroMotion.CellHeight"/> cell) and two head points as shares of the
    /// cell: the head bone (at the chin) and the head's top (at the brow). Engine-free.
    /// </summary>
    public readonly struct HeroFrame
    {
        public HeroFrame(string name, int x, int y, int width, int height, float headX, float headY, float topX, float topY)
        {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Head = (headX, headY);
            Top = (topX, topY);
        }

        /// <summary>The picture's name in the <see cref="HeroMotion.Folder"/> folder: <c>sprig-idle-07</c>.</summary>
        public string Name { get; }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public (float X, float Y) Head { get; }

        public (float X, float Y) Top { get; }

        /// <summary>The head's tilt in degrees, clockwise on screen: the angle from straight up of the head-to-top line.</summary>
        public float Roll =>
            (float)(Math.Atan2((Top.X - Head.X) * HeroMotion.CellWidth, (Head.Y - Top.Y) * HeroMotion.CellHeight) * 180.0 / Math.PI);
    }

    /// <summary>
    /// The owner's animated heroes as flat frames (spec 005 FR-028, constitution VII): <c>tools/heroanim</c> renders each
    /// family's clips (its idle loop, its reaction and maybe the win's celebration) offline into pictures (<c>Art/Heroes/Resources/HeroMotion/</c>) and writes their crops and
    /// head points into <c>HeroMotionData.cs</c>. Every clip starts and ends on the idle's first pose (the seam), so the
    /// idle loops and a reaction or the win's celebration joins it without a jump. Home and the win show them; every other screen keeps the still
    /// pictures (<see cref="CharacterArt.Hero"/>). Engine-free.
    /// </summary>
    public static partial class HeroMotion
    {
        /// <summary>The frames' folder under a Resources folder (Unity) or the embedded prefix (playtest, lower case).</summary>
        public const string Folder = "HeroMotion";

        /// <summary>How long a reaction asked for between two seams cross-fades from the idle frame it interrupts.</summary>
        public const float DissolveSeconds = 0.12f;

        /// <summary>A reaction asked for this close to the next seam waits for it instead of cross-fading.</summary>
        public const float MaxSeamWait = 0.35f;

        /// <summary>The clip's name in frame names: <c>idle</c>, <c>react</c>, <c>win</c>, <c>win2</c>, <c>winidle</c>.</summary>
        public static string ClipName(MotionClip clip) => clip switch
        {
            MotionClip.Idle => "idle",
            MotionClip.React => "react",
            MotionClip.Win2 => "win2",
            MotionClip.WinIdle => "winidle",
            _ => "win",
        };

        /// <summary>Every clip, in the frames' order (<see cref="AllFrames"/>).</summary>
        public static IReadOnlyList<MotionClip> Clips { get; } = new[] { MotionClip.Idle, MotionClip.React, MotionClip.Win, MotionClip.Win2, MotionClip.WinIdle };

        /// <summary>A frame's picture name: <c>sprig-idle-07</c>.</summary>
        public static string FrameName(Family family, MotionClip clip, int index) =>
            CharacterArt.FamilyName(family) + "-" + ClipName(clip) + "-" + index.ToString("00", CultureInfo.InvariantCulture);

        /// <summary>The asset slot of a family's frames: <c>char.hero3d.motion.sprig</c>.</summary>
        public static string Slot(Family family) => "char.hero3d.motion." + CharacterArt.FamilyName(family);

        /// <summary>Whether the family's idle and reaction were baked.</summary>
        public static bool Has(Family family) => Find(family, MotionClip.Idle) != null && Find(family, MotionClip.React) != null;

        /// <summary>Whether the family has its own celebration for the win (<see cref="MotionClip.Win"/>).</summary>
        public static bool HasWin(Family family) => Has(family) && Find(family, MotionClip.Win) != null;

        /// <summary>
        /// Whether the family's win set was baked facing the player (<see cref="MotionClip.WinIdle"/>, heroes.json
        /// <c>winYaw</c>): the win and the milestone then idle on it (<see cref="HeroMotionPlayer"/>'s <c>front</c>).
        /// </summary>
        public static bool HasFront(Family family) => HasWin(family) && Find(family, MotionClip.WinIdle) != null;

        /// <summary>
        /// The celebration a family plays on its <paramref name="turn"/>-th win (counted from 0, <see cref="CharacterArt.CelebrationTurn"/>):
        /// with two baked (Sprig's celebrate and clap, the owner's choice of 2026-10-03) they take turns, the first on even
        /// turns; with one, always it; without, the reaction.
        /// </summary>
        public static MotionClip WinClip(Family family, int turn)
        {
            if (!HasWin(family))
            {
                return MotionClip.React;
            }

            return turn % 2 != 0 && Find(family, MotionClip.Win2) != null ? MotionClip.Win2 : MotionClip.Win;
        }

        /// <summary>The number of frames of a clip (0 when it was not baked).</summary>
        public static int FrameCount(Family family, MotionClip clip) => Find(family, clip)?.Count ?? 0;

        /// <summary>A clip's length in seconds.</summary>
        public static float Seconds(Family family, MotionClip clip) => FrameCount(family, clip) / (float)Fps;

        /// <summary>A frame of a clip; the index wraps.</summary>
        public static HeroFrame Frame(Family family, MotionClip clip, int index)
        {
            ClipData data = Find(family, clip) ?? throw new ArgumentException("No baked " + ClipName(clip) + " clip of " + family, nameof(family));
            return data.Frame(((index % data.Count) + data.Count) % data.Count);
        }

        /// <summary>Every frame picture's name, the families in <see cref="CharacterArt.Families"/> order.</summary>
        public static IEnumerable<string> AllFrames()
        {
            foreach (Family family in CharacterArt.Families)
            {
                foreach (MotionClip clip in Clips)
                {
                    for (int i = 0; i < FrameCount(family, clip); i++)
                    {
                        yield return FrameName(family, clip, i);
                    }
                }
            }
        }

        /// <summary>The frame cell fitted into <paramref name="box"/>: the largest 8:9 box, centered (the still hero's shape).</summary>
        public static Box Cell(Box box) => CharacterArt.FitBox(box, CellWidth, CellHeight);

        /// <summary>A frame's picture box inside a cell box.</summary>
        public static Box PictureBox(Box cell, HeroFrame frame)
        {
            float sx = cell.Width / CellWidth;
            float sy = cell.Height / CellHeight;
            return new Box(cell.Left + (frame.X * sx), cell.Top + (frame.Y * sy), cell.Left + ((frame.X + frame.Width) * sx), cell.Top + ((frame.Y + frame.Height) * sy));
        }

        /// <summary>A point given as shares of the cell, inside a cell box.</summary>
        public static (float X, float Y) At(Box cell, (float X, float Y) share) =>
            (cell.Left + (cell.Width * share.X), cell.Top + (cell.Height * share.Y));

        /// <summary>
        /// The share of the cell's height the seam pose fills from its top down to the foot line: a cell is
        /// figure height ÷ this tall (<see cref="HomeLayers.HeroCell"/>).
        /// </summary>
        public static float Fill(Family family) => ((FootLine * CellHeight) - Frame(family, MotionClip.Idle, 0).Y) / CellHeight;

        /// <summary>The seam pose's width as a share of the cell's width (the shadow under it is that wide).</summary>
        public static float SeamWidth(Family family) => Frame(family, MotionClip.Idle, 0).Width / (float)CellWidth;

        /// <summary>
        /// How far above the brow a hat sits, as a share of the chin-to-brow line: the brow is the top of Sprig's and
        /// Bloom's face discs (their leaves and petals poke out beside the hat, as on the still pictures,
        /// <see cref="CharacterArt.HeadTopHero"/>) and halfway up Drop's head (its tip pokes into the hat). Twig's top
        /// point is its leaf's stem on top of the acorn cap (its Blender rig, 2026-10-03), so the hat sits right there,
        /// on the cap.
        /// </summary>
        public static float HatLift(Family family) => family switch
        {
            Family.Drop => 0.35f,
            _ => 0f,
        };

        /// <summary>
        /// A worn hat on a frame: half the cell wide (as <see cref="CharacterArt.HatOnHero"/> on the stills), its middle
        /// on the head's line <see cref="HatLift"/> above the brow and 6% of its size higher (so its brim, about 71% down its
        /// box, overlaps the head by 15% of its size),
        /// turned with the head (<see cref="HeroFrame.Roll"/>, degrees clockwise about the box's middle). A <c>sprout</c>
        /// rises by a quarter of its size, so its stem grows from the head's top.
        /// </summary>
        public static (Box Box, float Degrees) Hat(Box cell, Family family, HeroFrame frame, string? shape = null)
        {
            (float hx, float hy) = At(cell, frame.Head);
            (float tx, float ty) = At(cell, frame.Top);
            float dx = tx - hx;
            float dy = ty - hy;
            float length = (float)Math.Sqrt((dx * dx) + (dy * dy));
            float ux = length > 0f ? dx / length : 0f;
            float uy = length > 0f ? dy / length : -1f;
            float size = cell.Width * 0.5f;
            float up = (HatLift(family) * length) + (size * 0.06f) + (shape == "sprout" ? size * 0.25f : 0f);
            return (Box.FromCenter(tx + (ux * up), ty + (uy * up), size, size), frame.Roll);
        }

        // The clips by family and clip, found once: a pose is looked up for every hero at the display rate, so the lookup
        // must not allocate.
        private static ClipData?[]? _index;

        private static ClipData? Find(Family family, MotionClip clip)
        {
            ClipData?[] index = _index ??= BuildIndex();
            int i = (((int)family) * ClipCount) + (int)clip;
            return i >= 0 && i < index.Length ? index[i] : null;
        }

        private static ClipData?[] BuildIndex()
        {
            int families = 0;
            foreach (Family family in CharacterArt.Families)
            {
                families = Math.Max(families, ((int)family) + 1);
            }

            var index = new ClipData?[families * ClipCount];
            foreach (Family family in CharacterArt.Families)
            {
                string name = CharacterArt.FamilyName(family);
                foreach (ClipData data in ClipTable)
                {
                    foreach (MotionClip clip in Clips)
                    {
                        if (data.Family == name && data.Clip == ClipName(clip))
                        {
                            index[(((int)family) * ClipCount) + (int)clip] = data;
                        }
                    }
                }
            }

            return index;
        }

        private const int ClipCount = 5;

        private sealed class ClipData
        {
            private readonly short[] _crops;
            private readonly float[] _points;
            private readonly string[] _names;

            public ClipData(string family, string clip, short[] crops, float[] points)
            {
                if (crops.Length % 4 != 0 || points.Length != crops.Length)
                {
                    throw new ArgumentException("A clip needs four crop numbers and four head numbers per frame: " + family + " " + clip);
                }

                Family = family;
                Clip = clip;
                _crops = crops;
                _points = points;
                _names = new string[crops.Length / 4];
                for (int i = 0; i < _names.Length; i++)
                {
                    _names[i] = family + "-" + clip + "-" + i.ToString("00", CultureInfo.InvariantCulture);
                }
            }

            public string Family { get; }

            public string Clip { get; }

            public int Count => _crops.Length / 4;

            public HeroFrame Frame(int i) => new HeroFrame(
                _names[i],
                _crops[i * 4],
                _crops[(i * 4) + 1],
                _crops[(i * 4) + 2],
                _crops[(i * 4) + 3],
                _points[i * 4],
                _points[(i * 4) + 1],
                _points[(i * 4) + 2],
                _points[(i * 4) + 3]);
        }
    }

    /// <summary>What a hero shows at a moment: a clip's frame, maybe cross-fading from an idle frame.</summary>
    public readonly struct HeroPose
    {
        public HeroPose(MotionClip clip, int index, int fromIdle, float fromAlpha, MotionClip fromClip = MotionClip.Idle)
        {
            Clip = clip;
            Index = index;
            FromIdle = fromIdle;
            FromAlpha = fromAlpha;
            FromClip = fromClip;
        }

        public MotionClip Clip { get; }

        public int Index { get; }

        /// <summary>The idle frame a reaction cross-fades from (drawn over it at <see cref="FromAlpha"/>), or −1.</summary>
        public int FromIdle { get; }

        /// <summary>The idle clip <see cref="FromIdle"/> is a frame of (<see cref="MotionClip.WinIdle"/> on the win).</summary>
        public MotionClip FromClip { get; }

        public float FromAlpha { get; }
    }

    /// <summary>
    /// One hero's motion over time (spec 005 FR-028): the idle loop from <c>idleOrigin</c> (its first frame, the seam,
    /// shows then and every idle length after), and reactions on request (<see cref="React"/>, or the win's celebration,
    /// <see cref="Celebrate"/>). A reaction starts at the next seam when asked to wait or when that seam is near
    /// (<see cref="HeroMotion.MaxSeamWait"/>), else at once, cross-fading from the idle frame it interrupts over
    /// <see cref="HeroMotion.DissolveSeconds"/>; it ends on the seam pose and the idle starts over from it. With
    /// <c>front</c> (the win and the milestone) a hero whose win set was baked facing the player idles on it
    /// (<see cref="MotionClip.WinIdle"/>). Deterministic in its inputs; engine-free.
    /// </summary>
    public sealed class HeroMotionPlayer
    {
        private float _idleOrigin;
        private float _reactStart = float.NaN;
        private int _fromIdle = -1;
        private MotionClip _shot = MotionClip.React;

        public HeroMotionPlayer(Family family, float idleOrigin, bool front = false)
        {
            Family = family;
            _idleOrigin = idleOrigin;
            IdleClip = front && HeroMotion.HasFront(family) ? MotionClip.WinIdle : MotionClip.Idle;
        }

        public Family Family { get; }

        /// <summary>The idle this player loops: <see cref="MotionClip.WinIdle"/> on the win for a hero baked facing the player.</summary>
        public MotionClip IdleClip { get; }

        private float IdleSeconds => Math.Max(1f / HeroMotion.Fps, HeroMotion.Seconds(Family, IdleClip));

        private float ReactSeconds => HeroMotion.Seconds(Family, _shot);

        /// <summary>Whether a reaction plays or waits for its seam at <paramref name="now"/>.</summary>
        public bool Busy(float now)
        {
            Settle(now);
            return !float.IsNaN(_reactStart);
        }

        /// <summary>When the idle next shows its first frame (now, if it shows it now); after a reaction, when it ends.</summary>
        public float NextSeam(float now)
        {
            Settle(now);
            if (!float.IsNaN(_reactStart))
            {
                return _reactStart + ReactSeconds;
            }

            double laps = Math.Ceiling(((now - _idleOrigin) / IdleSeconds) - 1e-4);
            return _idleOrigin + (float)(laps * IdleSeconds);
        }

        /// <summary>Asks for the reaction (ignored while one plays or waits).</summary>
        public void React(float now, bool waitForSeam = false) => Play(MotionClip.React, now, waitForSeam);

        /// <summary>
        /// Asks for the win's celebration (the win and the milestone): the hero's own celebration for its
        /// <paramref name="turn"/>-th win when it has one (<see cref="HeroMotion.WinClip"/>), else its reaction; at the next
        /// seam unless <paramref name="waitForSeam"/> is false. Ignored while a reaction plays or waits.
        /// </summary>
        public void Celebrate(float now, bool waitForSeam = true, int turn = 0) =>
            Play(HeroMotion.WinClip(Family, turn), now, waitForSeam);

        private void Play(MotionClip clip, float now, bool waitForSeam)
        {
            Settle(now);
            if (!float.IsNaN(_reactStart) || HeroMotion.Seconds(Family, clip) <= 0f)
            {
                return;
            }

            _shot = clip;
            float seam = NextSeam(now);
            if (waitForSeam || seam - now <= HeroMotion.MaxSeamWait)
            {
                _reactStart = seam;
                _fromIdle = -1;
            }
            else
            {
                _fromIdle = IdleIndex(now);
                _reactStart = now;
            }
        }

        /// <summary>The frame to show at <paramref name="now"/>.</summary>
        public HeroPose Pose(float now)
        {
            Settle(now);
            if (!float.IsNaN(_reactStart) && now >= _reactStart)
            {
                float t = now - _reactStart;
                int count = HeroMotion.FrameCount(Family, _shot);
                int index = Math.Min(count - 1, Math.Max(0, (int)Math.Floor(t * HeroMotion.Fps)));
                float alpha = _fromIdle >= 0 && t < HeroMotion.DissolveSeconds ? 1f - (t / HeroMotion.DissolveSeconds) : 0f;
                return new HeroPose(_shot, index, alpha > 0f ? _fromIdle : -1, alpha, IdleClip);
            }

            return new HeroPose(IdleClip, IdleIndex(now), -1, 0f, IdleClip);
        }

        private int IdleIndex(float now)
        {
            float t = (now - _idleOrigin) % IdleSeconds;
            if (t < 0f)
            {
                t += IdleSeconds;
            }

            int count = Math.Max(1, HeroMotion.FrameCount(Family, IdleClip));
            return Math.Min(count - 1, Math.Max(0, (int)Math.Floor(t * HeroMotion.Fps)));
        }

        // A finished reaction hands over to the idle, which starts over from its seam.
        private void Settle(float now)
        {
            if (!float.IsNaN(_reactStart) && now >= _reactStart + ReactSeconds)
            {
                _idleOrigin = _reactStart + ReactSeconds;
                _reactStart = float.NaN;
                _fromIdle = -1;
            }
        }
    }
}
