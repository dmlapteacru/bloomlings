using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>A variant character's face (spec 004 FR-007, data-model.md "CharacterMood").</summary>
    public enum CharacterMood
    {
        /// <summary>Open eyes and a smile: exposed pods, working slots, walkers, board tiles.</summary>
        Happy,

        /// <summary>Closed eyes, muted colors: pods still queued in their stack.</summary>
        Asleep,

        /// <summary>A small frown, greyed colors: a stuck pod in a Waiting Slot.</summary>
        Worried,

        /// <summary>No eyes and no mouth: a worn cosmetic expression draws the face.</summary>
        Blank,
    }

    /// <summary>
    /// The generated character art of spec 004 (contracts/hosts.md "Shared kit"): the picture names the hosts load, their
    /// asset slots, and where a picture goes on a pod, a slot, a board tile and around it (cosmetics). The pictures come
    /// from <c>tools/artgen</c>, which uses these names too (contracts/art-files.md):
    /// <list type="bullet">
    /// <item><description><c>2d/{icon}-{mood}</c>: a variant's 2D character, whose shape is its symbol;</description></item>
    /// <item><description><c>3d/{family}</c> and <c>3d/{family}-blank</c>: a family's 3D hero (meta screens only);</description></item>
    /// <item><description><c>3d/group</c>: the four heroes side by side, without a base of their own (the hosts stand
    /// them on their stone pedestal, feet at <see cref="GroupFeetShare"/>).</description></item>
    /// <item><description><c>3d/{family}-cheer</c>: optional celebrating heroes the owner makes (spec 005 pictures.md A7);
    /// the win card shows the group while they are missing.</description></item>
    /// </list>
    /// Engine-free.
    /// </summary>
    public static class CharacterArt
    {
        /// <summary>The side of a 2D character picture in pixels.</summary>
        public const int Size2D = 256;

        /// <summary>The 2D drawing's design square (0..100) lies inside this margin of the picture on each side.</summary>
        public const float Margin2D = 0.04f;

        public const int HeroWidth = 512;

        public const int HeroHeight = 576;

        public const int GroupWidth = 1200;

        public const int GroupHeight = 720;

        /// <summary>The group picture's name.</summary>
        public const string Group = "3d/group";

        /// <summary>The group picture's asset slot.</summary>
        public const string GroupSlot = "char.hero3d.group";

        /// <summary>Where the group's feet stand, as a share of its picture's height from the top (y down).</summary>
        public const float GroupFeetShare = 0.62f;

        /// <summary>Where the group's heads begin, as a share of its picture's height from the top (the margin above is clear).</summary>
        public const float GroupHeadShare = 0.15f;

        /// <summary>Every mood, in file order.</summary>
        public static IReadOnlyList<CharacterMood> Moods { get; } = new[] { CharacterMood.Happy, CharacterMood.Asleep, CharacterMood.Worried, CharacterMood.Blank };

        /// <summary>The four families, in the order the group picture shows them.</summary>
        public static IReadOnlyList<Family> Families { get; } = new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig };

        public static string MoodName(CharacterMood mood) => mood switch
        {
            CharacterMood.Happy => "happy",
            CharacterMood.Asleep => "asleep",
            CharacterMood.Worried => "worried",
            _ => "blank",
        };

        public static string FamilyName(Family family) => family switch
        {
            Family.Sprig => "sprig",
            Family.Bloom => "bloom",
            Family.Drop => "drop",
            Family.Twig => "twig",
            _ => family.ToString().ToLowerInvariant(),
        };

        /// <summary>A variant's 2D character picture: <c>2d/leaf-happy</c>.</summary>
        public static string Picture2D(string iconId, CharacterMood mood) => "2d/" + iconId + "-" + MoodName(mood);

        /// <summary>A family's 3D hero: <c>3d/bloom</c>, or <c>3d/bloom-blank</c> without eyes and mouth.</summary>
        public static string Hero(Family family, bool blank = false) => "3d/" + FamilyName(family) + (blank ? "-blank" : string.Empty);

        public static string Slot2D(string iconId) => "char.v." + iconId;

        public static string HeroSlot(Family family) => "char.hero3d." + FamilyName(family);

        /// <summary>A family's celebrating hero (spec 005 pictures.md A7, optional, 512 × 576): <c>3d/bloom-cheer</c>.</summary>
        public static string Cheer(Family family) => "3d/" + FamilyName(family) + "-cheer";

        /// <summary>The asset slot of a family's celebrating hero: <c>char.hero3d.cheer.bloom</c>.</summary>
        public static string CheerSlot(Family family) => "char.hero3d.cheer." + FamilyName(family);

        /// <summary>
        /// The families that take turns celebrating won levels, in their order: Twig, then Sprig (the owner's choices of
        /// 2026-10-03, spec 005 FR-028; Twig alone before Sprig's animated model came). They replaced the level's main
        /// family (pictures.md A7). Both builds take them from here.
        /// </summary>
        public static IReadOnlyList<Family> Celebrants { get; } = new[] { Family.Twig, Family.Sprig };

        /// <summary>
        /// The family that celebrates won level <paramref name="level"/>, on the win and on the milestone that follows it:
        /// the celebrants take turns level by level (L1 Twig, L2 Sprig, L3 Twig, …).
        /// </summary>
        public static Family CelebrantOf(int level) => Celebrants[CelebrationIndex(level) % Celebrants.Count];

        /// <summary>
        /// Which of its own turns the celebrant of <paramref name="level"/> is on, counted from 0 (L2 is Sprig's 0th, L4
        /// its 1st): a celebrant with two celebrations alternates them by it (<see cref="HeroMotion.WinClip"/>; Sprig's
        /// celebrate on L2, its clap on L4, …).
        /// </summary>
        public static int CelebrationTurn(int level) => CelebrationIndex(level) / Celebrants.Count;

        private static int CelebrationIndex(int level) => Math.Max(0, level - 1);

        /// <summary>The asset slot a picture name belongs to.</summary>
        public static string SlotOf(string picture)
        {
            if (picture == Group)
            {
                return GroupSlot;
            }

            if (picture.StartsWith("2d/", StringComparison.Ordinal))
            {
                string stem = picture.Substring(3);
                int dash = stem.LastIndexOf('-');
                return Slot2D(dash > 0 ? stem.Substring(0, dash) : stem);
            }

            string name = picture.Substring(3);
            if (name.EndsWith("-cheer", StringComparison.Ordinal))
            {
                return "char.hero3d.cheer." + name.Substring(0, name.Length - 6);
            }

            int blank = name.IndexOf('-');
            return "char.hero3d." + (blank > 0 ? name.Substring(0, blank) : name);
        }

        /// <summary>Every picture of the set (57): 12 icons × 4 moods, 4 heroes × 2, and the group.</summary>
        public static IReadOnlyList<string> AllPictures(IEnumerable<VariantInfo> variants)
        {
            var names = new List<string>();
            foreach (VariantInfo variant in variants)
            {
                foreach (CharacterMood mood in Moods)
                {
                    names.Add(Picture2D(variant.IconId, mood));
                }
            }

            foreach (Family family in Families)
            {
                names.Add(Hero(family));
                names.Add(Hero(family, blank: true));
            }

            names.Add(Group);
            return names;
        }

        /// <summary>The pixel size of a picture.</summary>
        public static (int Width, int Height) SizeOf(string picture) =>
            picture.StartsWith("2d/", StringComparison.Ordinal) ? (Size2D, Size2D) : picture == Group ? (GroupWidth, GroupHeight) : (HeroWidth, HeroHeight);

        // ---- Placement (y down) ----

        /// <summary>
        /// The character's box on a pod or slot card face: 84% of its width (at most 80% of its height), centered across,
        /// 3% below the top, so the "xN" in the bottom-right corner only meets the picture's transparent margin.
        /// </summary>
        public static Box OnCard(Box face)
        {
            float side = Math.Min(face.Width * 0.84f, face.Height * 0.8f);
            return Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.03f) + (side / 2f), side, side);
        }

        /// <summary>A box inside <paramref name="parent"/> as Unity anchors (x0, y0, x1, y1), y up.</summary>
        public static (float X0, float Y0, float X1, float Y1) Anchors(Box box, Box parent) =>
            ((box.Left - parent.Left) / parent.Width, 1f - ((box.Bottom - parent.Top) / parent.Height), (box.Right - parent.Left) / parent.Width, 1f - ((box.Top - parent.Top) / parent.Height));

        /// <summary>Where the "xN" count goes on a pod or slot face: the bottom-right corner, inset 6%.</summary>
        public static Box CountBox(Box face)
        {
            float right = face.Right - (face.Width * 0.06f);
            float bottom = face.Bottom - (face.Height * 0.04f);
            return new Box(right - (face.Width * 0.56f), bottom - (face.Height * 0.3f), right, bottom);
        }

        /// <summary>The white outline around "xN", as a share of its size.</summary>
        public const float CountOutlineEm = 0.08f;

        /// <summary>
        /// The "xN" count's look (data-model.md "PodCount"): dark brown letters with a thin white outline, the same on every
        /// card (the queued and stuck cards are muted already, and a grey count would fall under 4.5:1 there). The text is
        /// the localized <c>pod.count</c>.
        /// </summary>
        public static TextLook CountLook => new TextLook(CountColor, CountColor, Rgba.White, CountOutlineEm, 0f, 0f);

        /// <summary>The "xN" letters: <c>garden.label_plain</c>.</summary>
        public static Rgba CountColor => DesignTokens.Colors.GardenLabelPlain;

        /// <summary>The character's box on a board tile face: 86% of the face, centered, 3% low.</summary>
        public static Box OnTile(Box face)
        {
            float side = Math.Min(face.Width, face.Height) * 0.86f;
            return Box.FromCenter(face.CenterX, face.CenterY + (face.Height * 0.03f), side, side);
        }

        /// <summary>Where a 2D character's face is drawn, in the drawing's 0..100 design square (y down).</summary>
        public static (float X, float Y) FaceDesign2D(string iconId) => iconId switch
        {
            "leaf" => (50f, 64f),
            "moss" => (50f, 62f),
            "flower" => (50f, 54f),
            "bud" => (50f, 66f),
            "drop" => (52f, 66f),
            "dew" => (50f, 60f),
            "log" => (50f, 60f),
            "acorn" => (50f, 72f),
            "vine" => (50f, 64f),
            "berry" => (50f, 62f),
            "mist" => (50f, 63f),
            _ => (50f, 60f),
        };

        /// <summary>Where a 2D character's face is, as a share of its picture.</summary>
        public static (float X, float Y) FaceCenter2D(string iconId)
        {
            (float x, float y) = FaceDesign2D(iconId);
            return (Margin2D + ((1f - (2f * Margin2D)) * x / 100f), Margin2D + ((1f - (2f * Margin2D)) * y / 100f));
        }

        /// <summary>
        /// Where a 3D hero's face is (between the eyes and the mouth), as a share of its solo picture: measured on the
        /// owner's heroes (spec 005 pictures.md A1 to A4; tools/artgen's check notes that an owner's hero is set by hand).
        /// The heroes stand slightly turned, so Sprig's and Twig's faces are right of center.
        /// </summary>
        public static (float X, float Y) FaceCenterHero(Family family) => family switch
        {
            Family.Sprig => (0.6f, 0.55f),
            Family.Bloom => (0.51f, 0.57f),
            Family.Drop => (0.5f, 0.55f),
            _ => (0.56f, 0.56f),
        };

        /// <summary>
        /// The families whose solo 3D hero (<c>3d/{family}</c>) is the owner's picture (spec 005 pictures.md A1 to A4,
        /// recorded by tools/artgen <c>adopt</c>; its art check keeps this list equal to the manifest).
        /// </summary>
        public static IReadOnlyList<Family> OwnerHeroes { get; } = new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig };

        /// <summary>
        /// The families whose blank twin (<c>3d/{family}-blank</c>, no eyes and no mouth) is the owner's picture
        /// (pictures.md A5); none yet, so the blanks are still the generated heroes (the art check keeps this list equal
        /// to the manifest).
        /// </summary>
        public static IReadOnlyList<Family> OwnerBlanks { get; } = Array.Empty<Family>();

        /// <summary>
        /// Whether a family's blank twin is the same character as its solo hero (both the owner's or both generated), so a
        /// worn expression may swap the hero for its blank. Otherwise the hosts keep the solo hero and show the expression
        /// as a badge beside its face (<see cref="ExpressionBadge"/>), never over the drawn face.
        /// </summary>
        public static bool HasMatchingBlank(Family family) => Contains(OwnerHeroes, family) == Contains(OwnerBlanks, family);

        /// <summary>
        /// A worn expression shown beside a hero whose blank twin does not match (<see cref="HasMatchingBlank"/>): a cream
        /// disc 0.22 of the picture's width near its top right (0.18 W in from the right, 0.30 H down), the expression's
        /// glyph on it. Returns the disc.
        /// </summary>
        public static Box ExpressionBadge(Box picture)
        {
            float size = picture.Width * 0.22f;
            return Box.FromCenter(picture.Right - (picture.Width * 0.18f), picture.Top + (picture.Height * 0.3f), size, size);
        }

        private static bool Contains(IReadOnlyList<Family> families, Family family)
        {
            for (int i = 0; i < families.Count; i++)
            {
                if (families[i] == family)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The 3D group standing on a card's top edge (the win and milestone cards, FR-017): at most
        /// <paramref name="widthShare"/> of the card's width, its pedestal overlapping the edge by 18 units, in the room
        /// above the card inside the safe area. Null when that room is under 160 units (a short phone): the card never moves.
        /// </summary>
        public static Box? GroupOnCard(Box card, Box safe, float scale, float widthShare)
        {
            float overlap = 18f * scale;
            float room = card.Top - safe.Top - (8f * scale) + overlap;
            float height = Math.Min(room, card.Width * widthShare * GroupHeight / GroupWidth);
            if (height < 160f * scale)
            {
                return null;
            }

            float width = height * GroupWidth / GroupHeight;
            return new Box(card.CenterX - (width / 2f), card.Top + overlap - height, card.CenterX + (width / 2f), card.Top + overlap);
        }

        /// <summary>
        /// The group picture box whose heroes' feet stand on the line <paramref name="feet"/>, centered on
        /// <paramref name="cx"/>, <paramref name="width"/> wide (y down; the picture's lower part below the feet is clear).
        /// </summary>
        public static Box GroupStanding(float cx, float feet, float width)
        {
            float height = width * GroupHeight / GroupWidth;
            return new Box(cx - (width / 2f), feet - (height * GroupFeetShare), cx + (width / 2f), feet + (height * (1f - GroupFeetShare)));
        }

        /// <summary>A picture of <paramref name="width"/> × <paramref name="height"/> fitted into a box: aspect kept, centered.</summary>
        public static Box FitBox(Box box, float width, float height)
        {
            float scale = Math.Min(box.Width / Math.Max(1f, width), box.Height / Math.Max(1f, height));
            return Box.FromCenter(box.CenterX, box.CenterY, width * scale, height * scale);
        }

        /// <summary>A worn expression's box over a face at (fx, fy) of a picture box.</summary>
        public static Box ExpressionBox(Box picture, (float X, float Y) face) =>
            Box.FromCenter(picture.Left + (picture.Width * face.X), picture.Top + (picture.Height * face.Y), picture.Width * 0.36f, picture.Width * 0.24f);

        /// <summary>
        /// Where a 3D hero's head line is, as a share of its solo picture (y down), measured on the owner's heroes: the top
        /// of Sprig's and Bloom's face discs (their leaves and petals poke out beside the hat), a little below Drop's tip
        /// (it pokes into a hat) and the top of Twig's acorn cap (the hat sits on it). The hat's brim rests 0.15 of its
        /// size below this line, just above the eyebrows.
        /// </summary>
        public static (float X, float Y) HeadTopHero(Family family) => family switch
        {
            Family.Sprig => (0.58f, 0.31f),
            Family.Bloom => (0.51f, 0.31f),
            Family.Drop => (0.5f, 0.27f),
            _ => (0.55f, 0.2f),
        };

        /// <summary>
        /// A worn hat's box on a 3D hero picture (meta screens): half the picture wide (the owner's head discs are about
        /// that wide), centered over the head line (<see cref="HeadTopHero"/>), the hat's brim (about 71% down its box)
        /// overlapping the head by 15% of its size. A hat without a brim whose stem reaches its box's foot
        /// (<paramref name="shape"/> <c>sprout</c>, a <c>CosmeticCatalog</c> shape) rises by a quarter of its size, so the
        /// stem grows from the head's top instead of hanging over the face.
        /// </summary>
        public static Box HatOnHero(Box picture, Family family, string? shape = null)
        {
            (float x, float y) = HeadTopHero(family);
            float size = picture.Width * 0.5f;
            float head = picture.Top + (picture.Height * y);
            float lift = shape == "sprout" ? size * 0.25f : 0f;
            return Box.FromCenter(picture.Left + (picture.Width * x), head - (size * 0.06f) - lift, size, size);
        }

        /// <summary>A worn hat's box: over the top of the picture (the 2D figures).</summary>
        public static Box HatBox(Box picture) =>
            Box.FromCenter(picture.CenterX, picture.Top + (picture.Height * 0.12f), picture.Width * 0.5f, picture.Width * 0.5f);

        /// <summary>A worn trail's box: behind the picture's lower left.</summary>
        public static Box TrailBox(Box picture) =>
            new Box(picture.Left - (picture.Width * 0.22f), picture.Top + (picture.Height * 0.5f), picture.Left + (picture.Width * 0.2f), picture.Top + (picture.Height * 0.9f));

    }
}
