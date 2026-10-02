using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>A picture's box in a larger picture's pixels (the owner's layered Home, <see cref="HomeLayers"/>).</summary>
    public readonly struct PictureBox
    {
        public PictureBox(string name, int x, int y, int width, int height)
        {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>The picture's name in the Backgrounds folder: <c>home-fountain-back</c>.</summary>
        public string Name { get; }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }
    }

    /// <summary>
    /// The owner's layered Home (spec 005 FR-028, pictures.md B1; <c>bloomlings_home_assets.zip</c>, prepared by
    /// <c>tools/heroanim/layers.mjs</c>, boxes in <c>HomeLayersData.cs</c>), back to front: the garden (<see cref="Back"/>,
    /// the <c>home</c> picture every Home backdrop already draws), the fountain's back (<see cref="FountainBack"/>), the
    /// heroes at the back with their shadows (Drop, Bloom), the lotus again (<see cref="Lotus"/>, so Bloom stands behind
    /// it), the heroes in front with their shadows (Sprig, Twig), the fountain's front stones and flowers
    /// (<see cref="FountainFront"/>, over the heroes' feet), the drifting petals (<see cref="Petals"/>), then the UI. The
    /// layers share one box over the screen (<see cref="Cover"/>, the backdrop's own cover fit), so the heroes stay on the
    /// fountain on every screen shape. Engine-free.
    /// </summary>
    public static partial class HomeLayers
    {
        /// <summary>The opacity of the heroes' shadows.</summary>
        public const float ShadowAlpha = 0.85f;

        /// <summary>The opacity of the drifting petals.</summary>
        public const float PetalsAlpha = 0.9f;

        /// <summary>How fast the petals drift down, in the picture's pixels per second (a lap of its height in about 84 s).</summary>
        public const float PetalsSpeed = 22f;

        /// <summary>How far the petals sway sideways, in the picture's pixels, and how long a sway takes.</summary>
        public const float PetalsSway = 14f;

        public const float PetalsSwaySeconds = 7f;

        /// <summary>When Home's first reaction comes after it opens, and how often one follows, in seconds.</summary>
        public const float FirstReaction = 1.5f;

        public const float ReactionEvery = 6f;

        /// <summary>Every layer picture (the drawing order is the class's: see its summary).</summary>
        public static IReadOnlyList<PictureBox> All => new[] { Back, FountainBack, Lotus, Shadow, FountainFront, Petals };

        /// <summary>The asset slot of a layer: <c>bg.home</c> (the garden), <c>bg.home.fountain_back</c>, …</summary>
        public static string SlotOf(PictureBox layer) => layer.Name == Back.Name
            ? OwnerPictures.SlotOf(OwnerPictures.Home)
            : "bg.home." + layer.Name.Substring("home-".Length).Replace('-', '_');

        /// <summary>The heroes back to front: Drop at the right back, Bloom behind the lotus, Sprig at the left front, Twig at the right front.</summary>
        public static IReadOnlyList<Family> DrawOrder { get; } = new[] { Family.Drop, Family.Bloom, Family.Sprig, Family.Twig };

        /// <summary>The heroes standing behind the lotus, drawn before it (<see cref="Lotus"/>).</summary>
        public static bool BehindLotus(Family family) => family == Family.Drop || family == Family.Bloom;

        /// <summary>
        /// Where a hero stands, measured on the reference (its Home, look.md §6.4) and fitted to the layered fountain:
        /// its feet's middle as shares of the picture's width and height, and its seam pose's height as a share of the
        /// picture's width. Bloom stands behind the lotus, its feet hidden; Sprig on the left rim and Twig on the right
        /// rim, their feet behind the fountain's front flowers.
        /// </summary>
        public static (float X, float Feet, float Height) Placement(Family family) => family switch
        {
            Family.Bloom => (0.505f, 0.49f, 0.45f),
            Family.Drop => (0.705f, 0.532f, 0.34f),
            Family.Sprig => (0.235f, 0.56f, 0.40f),
            _ => (0.83f, 0.568f, 0.33f),
        };

        /// <summary>How far into its idle loop a hero starts (seconds), so the four do not breathe together.</summary>
        public static float Phase(Family family) => family switch
        {
            Family.Bloom => 1.3f,
            Family.Drop => 2.6f,
            Family.Twig => 0.7f,
            _ => 0f,
        };

        /// <summary>The order Home's reactions take turns in: Bloom in the middle first.</summary>
        public static IReadOnlyList<Family> ReactionOrder { get; } = new[] { Family.Bloom, Family.Sprig, Family.Drop, Family.Twig };

        /// <summary>The layered picture over <paramref name="screen"/>: cover-fitted and centered, as the backdrop draws <c>home</c>.</summary>
        public static Box Cover(Box screen)
        {
            float scale = Math.Max(screen.Width / PictureWidth, screen.Height / PictureHeight);
            return Box.FromCenter(screen.CenterX, screen.CenterY, PictureWidth * scale, PictureHeight * scale);
        }

        /// <summary>A layer's box on screen, the layered picture lying at <paramref name="picture"/>.</summary>
        public static Box Place(Box picture, PictureBox layer)
        {
            float s = picture.Width / PictureWidth;
            return new Box(picture.Left + (layer.X * s), picture.Top + (layer.Y * s), picture.Left + ((layer.X + layer.Width) * s), picture.Top + ((layer.Y + layer.Height) * s));
        }

        /// <summary>
        /// A hero's frame cell on screen (<see cref="HeroMotion.Cell"/>'s 8:9 box, the feet on its foot line): its seam
        /// pose <see cref="Placement"/>'s height tall. Without baked frames, the still hero's box (feet at 90%) with the
        /// figure about 0.8 of it.
        /// </summary>
        public static Box HeroCell(Box picture, Family family)
        {
            (float x, float feet, float height) = Placement(family);
            float fill = HeroMotion.Has(family) ? HeroMotion.Fill(family) : 0.8f;
            float cellHeight = height * picture.Width / Math.Max(0.05f, fill);
            float cellWidth = cellHeight * HeroMotion.CellWidth / HeroMotion.CellHeight;
            float fx = picture.Left + (x * picture.Width);
            float fy = picture.Top + (feet * picture.Height);
            return new Box(fx - (cellWidth / 2f), fy - (cellHeight * HeroMotion.FootLine), fx + (cellWidth / 2f), fy + (cellHeight * (1f - HeroMotion.FootLine)));
        }

        /// <summary>The soft shadow under a hero's feet (<see cref="Shadow"/>): as wide as its seam pose, its middle on the feet.</summary>
        public static Box ShadowBox(Box picture, Family family)
        {
            Box cell = HeroCell(picture, family);
            float width = cell.Width * (HeroMotion.Has(family) ? HeroMotion.SeamWidth(family) : 0.7f);
            float height = width * Shadow.Height / Shadow.Width;
            float feet = cell.Top + (cell.Height * HeroMotion.FootLine);
            return Box.FromCenter(cell.CenterX, feet + (height * 0.08f), width, height);
        }

        /// <summary>
        /// The petals' box at <paramref name="seconds"/> after Home opened: drifting down and swaying, wrapping round the
        /// picture's height. The host draws it twice: here and one picture height higher.
        /// </summary>
        public static Box PetalsAt(Box picture, float seconds)
        {
            float s = picture.Width / PictureWidth;
            float down = (seconds * PetalsSpeed) % PictureHeight;
            float side = PetalsSway * (float)Math.Sin(seconds * 2.0 * Math.PI / PetalsSwaySeconds);
            return Place(picture, Petals).Offset(side * s, down * s);
        }
    }

    /// <summary>
    /// Home's four heroes in motion (spec 005 FR-028): each idles from its own <see cref="HomeLayers.Phase"/>, and every
    /// <see cref="HomeLayers.ReactionEvery"/> seconds (the first after <see cref="HomeLayers.FirstReaction"/>) the next of
    /// <see cref="HomeLayers.ReactionOrder"/> reacts at its next seam; a tap on a hero makes it react at once. The host
    /// calls <see cref="Update"/> every frame, then draws each hero's <see cref="HeroMotionPlayer.Pose"/>. Engine-free.
    /// </summary>
    public sealed class HomeMotion
    {
        private readonly Dictionary<Family, HeroMotionPlayer> _players = new Dictionary<Family, HeroMotionPlayer>();
        private float _nextReaction;
        private int _turn;

        public HomeMotion(float start)
        {
            foreach (Family family in HomeLayers.DrawOrder)
            {
                _players[family] = new HeroMotionPlayer(family, start - HomeLayers.Phase(family));
            }

            _nextReaction = start + HomeLayers.FirstReaction;
        }

        public HeroMotionPlayer Player(Family family) => _players[family];

        /// <summary>Starts the reactions whose turn came by <paramref name="now"/>.</summary>
        public void Update(float now)
        {
            while (now >= _nextReaction)
            {
                Family family = HomeLayers.ReactionOrder[_turn % HomeLayers.ReactionOrder.Count];
                _players[family].React(_nextReaction, waitForSeam: true);
                _turn++;
                _nextReaction += HomeLayers.ReactionEvery;
            }
        }

        /// <summary>A tap on a hero: it reacts now (cross-fading from its idle) unless it already reacts.</summary>
        public void Tap(Family family, float now) => _players[family].React(now);
    }
}
