using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The 3D heroes of spec 004 (FR-017) on the meta screens only, in the reference look of spec 005 (contracts/look.md
    /// §4.4, §4.5): the group picture, the celebration of the win and milestone cards (light rays, a stone pedestal and
    /// the heroes on it) and the drawn Home stage (the heroes around the lotus fountain on a stone pedestal, only without
    /// the owner's Home picture for now). Without the pictures it shows the four family silhouettes (FR-021).
    /// </summary>
    public static class HeroPictures
    {
        private static readonly Dictionary<Family, Sprite?> Cheers = new Dictionary<Family, Sprite?>();

        /// <summary>
        /// The four heroes side by side (the group picture has no base of its own: callers stand it on a stone pedestal,
        /// feet at <see cref="CharacterArt.GroupFeetShare"/>), fitted into a new rect the caller places.
        /// </summary>
        public static RectTransform Group(string name, Transform parent)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            Sprite? group = CharacterSprites.Group;
            if (group != null)
            {
                Image image = UiFactory.CreateImage("Heroes", root, group, Color.white);
                image.preserveAspect = true;
                image.raycastTarget = false;
                UiFactory.Stretch(image.rectTransform);
                return root;
            }

            for (int i = 0; i < CharacterArt.Families.Count; i++)
            {
                Family family = CharacterArt.Families[i];
                Image body = UiFactory.CreateImage(family.ToString(), root, ProceduralSprites.Silhouette(family), ColorOf(family));
                body.preserveAspect = true;
                body.raycastTarget = false;
                UiFactory.Place(body.rectTransform, 0.06f + (i * 0.22f), 0.2f, 0.26f + (i * 0.22f), 0.8f);
            }

            return root;
        }

        /// <summary>
        /// The owner's celebrating hero of a family (spec 005 pictures.md A7, <c>3d/{family}-cheer.png</c>: arms up, eyes
        /// closed with joy), or null while it is missing; the win then shows the group. It is optional, so a missing picture
        /// logs nothing.
        /// </summary>
        public static Sprite? Cheer(Family family)
        {
            if (Cheers.TryGetValue(family, out Sprite? cached))
            {
                return cached;
            }

            string name = CheerPicture(family);
            Texture2D? texture = Resources.Load<Texture2D>("Characters/" + name);
            Sprite? sprite = null;
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
                sprite.name = name;
            }

            Cheers[family] = sprite;
            return sprite;
        }

        /// <summary>The art set name of a family's celebrating hero (pictures.md A7), in <c>Resources/Characters/</c>.</summary>
        public static string CheerPicture(Family family) => "3d/" + CharacterArt.FamilyName(family) + "-cheer";

        /// <summary>
        /// The family a level celebrates with (pictures.md A7: "the family of the level's main variant"): the family of the
        /// variant whose pods carry the most tiles, the first in the catalog's order on a tie; Sprig when the level has no
        /// pods.
        /// </summary>
        public static Family MainFamily(IEnumerable<Core.Definitions.PodDef> pods)
        {
            var work = new Dictionary<VariantId, int>();
            foreach (Core.Definitions.PodDef pod in pods)
            {
                work.TryGetValue(pod.Variant, out int count);
                work[pod.Variant] = count + pod.Count;
            }

            Family best = Family.Sprig;
            int most = 0;
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                if (work.TryGetValue(info.Id, out int count) && count > most)
                {
                    most = count;
                    best = info.Family;
                }
            }

            return best;
        }

        /// <summary>The color of a family's silhouette when its picture is missing (the playtest's hero fallback).</summary>
        public static Color ColorOf(Family family) => BloomlingFigure.HeroColor(family);

        /// <summary>
        /// The celebration of the win and the milestone (spec 005 §4.4, §6.3; the playtest's <c>EndCards.Celebration</c>),
        /// as children of <paramref name="card"/> (a card, or the full-screen celebration's root): slowly turning light
        /// rays, a stone pedestal, and the heroes standing on it. Place it with
        /// <see cref="CelebrationView.Place(Box, Box, float, float)"/> over a card (the rays clipped above its top edge) or
        /// <see cref="CelebrationView.Place(WinRegions, Box)"/> on the full screen.
        /// </summary>
        public static CelebrationView Celebration(RectTransform card) => new CelebrationView(card);

        /// <summary>
        /// The Home and splash heroes (spec 005 FR-024, §4.5, §6.4; the playtest's <c>HomeScreen.Stage</c>) under
        /// <paramref name="parent"/>: over the owner's garden picture, none for now (<see cref="HomeStage.ShowsHeroes"/>);
        /// without it, the drawn diorama (the stone ring, the lotus fountain and the heroes around it). Place it with
        /// <see cref="HomeStageView.Place"/>.
        /// </summary>
        public static HomeStageView Stage(string name, Transform parent) => new HomeStageView(UiFactory.CreateRect(name, parent));
    }

    /// <summary>The celebration of the win and milestone cards (<see cref="HeroPictures.Celebration"/>).</summary>
    public sealed class CelebrationView
    {
        private readonly RectTransform _clip;
        private readonly RectTransform _rays;
        private readonly RectTransform _pedestal;
        private readonly RectTransform _group;
        private readonly Image _cheer;
        private Family? _family;

        internal CelebrationView(RectTransform card)
        {
            // The rays show above the card's top edge only, fading in (alpha 0.85, as the playtest's).
            _clip = UiFactory.CreateRect("RaysClip", card);
            _clip.gameObject.AddComponent<RectMask2D>();
            UiKit.FadeInOnShow(_clip.gameObject, 0.85f, 0.6f);
            _rays = (RectTransform)UiKit.LightRays("Rays", _clip).transform;
            _pedestal = UiKit.StonePedestal("Pedestal", card);
            _group = HeroPictures.Group("Heroes", card);
            _cheer = UiFactory.CreateImage("Cheer", card, null, Color.white);
            _cheer.preserveAspect = true;
            _cheer.raycastTarget = false;
            _cheer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the owner's celebrating hero of <paramref name="family"/> (pictures.md A7) instead of the group when that
        /// picture exists; null shows the group. Call <see cref="Place(Box, Box, float, float)"/> or
        /// <see cref="Place(WinRegions, Box)"/> after it.
        /// </summary>
        public void ShowHero(Family? family) => _family = family;

        /// <summary>
        /// Brings the pedestal and the heroes in front of everything built after the celebration so far (the full-screen
        /// win's finished picture), keeping the rays behind: the hero overlaps the picture's foot as on the reference.
        /// </summary>
        public void BringHeroesForward()
        {
            _pedestal.SetAsLastSibling();
            _group.SetAsLastSibling();
            _cheer.rectTransform.SetAsLastSibling();
        }

        /// <summary>
        /// Lays the celebration out on the full-screen win or milestone (spec 005 FR-023, contracts/look.md §6.3) whose
        /// screen box is <paramref name="parent"/>: the stone pedestal in <see cref="WinRegions.Pedestal"/> (left out with
        /// <paramref name="pedestal"/> false, when the owner's win picture paints the stage there), the owner's
        /// celebrating hero in the 8:9 <see cref="WinRegions.Hero"/> box, or else the group standing with its feet where
        /// the hero's stand (<see cref="HomeStage.FeetShare"/> of the box), as wide as the pedestal over 0.8 (its heads
        /// inside the box), and the rays (radius <see cref="WinRegions.RaysRadius"/>) turning around the hero, unclipped.
        /// </summary>
        public void Place(WinRegions r, Box parent, bool pedestal = true)
        {
            Sprite? cheer = _family.HasValue ? HeroPictures.Cheer(_family.Value) : null;
            _clip.gameObject.SetActive(true);
            _pedestal.gameObject.SetActive(pedestal);
            _group.gameObject.SetActive(cheer == null);
            _cheer.gameObject.SetActive(cheer != null);
            UiKit.PlaceBox(_clip, parent, parent);
            UiKit.PlaceBox(_rays, Box.FromCenter(r.RaysX, r.RaysY, r.RaysRadius * 2f, r.RaysRadius * 2f), parent);
            UiKit.PlaceBox(_pedestal, r.Pedestal, parent);
            if (cheer != null)
            {
                _cheer.sprite = cheer;
                UiKit.PlaceBox(_cheer.rectTransform, r.Hero, parent);
                return;
            }

            float feet = r.Hero.Top + (r.Hero.Height * HomeStage.FeetShare);
            float span = (CharacterArt.GroupFeetShare - CharacterArt.GroupHeadShare) * CharacterArt.GroupHeight / CharacterArt.GroupWidth;
            float width = Mathf.Min(r.Pedestal.Width / 0.8f, (feet - r.Hero.Top) / Mathf.Max(0.01f, span));
            UiKit.PlaceBox(_group, CharacterArt.GroupStanding(r.Hero.CenterX, feet, width), parent);
        }

        /// <summary>
        /// Lays the celebration out in <paramref name="stage"/> (screen pixels, top-down): the pedestal at the stage's
        /// bottom, the heroes on it, the rays behind them above <paramref name="cardTop"/>. <paramref name="cardBox"/> is the
        /// card's screen box. On a phone too short for it (a stage under 150 units) nothing shows, so the card never moves.
        /// </summary>
        public void Place(Box stage, Box cardBox, float screenWidth, float scale)
        {
            bool fits = stage.Height >= 150f * scale;
            _clip.gameObject.SetActive(fits);
            _pedestal.gameObject.SetActive(fits);
            Sprite? cheer = _family.HasValue ? HeroPictures.Cheer(_family.Value) : null;
            _group.gameObject.SetActive(fits && cheer == null);
            _cheer.gameObject.SetActive(fits && cheer != null);
            if (!fits)
            {
                return;
            }

            // The group picture has no base of its own: it stands on the pedestal with its feet on the top ellipse
            // (HomeStage.Celebration, the playtest's EndCards.Celebration), the rays turning behind the heroes' bodies.
            (Box pedestal, Box group, float raysX, float raysY) = HomeStage.Celebration(stage);
            UiKit.PlaceBox(_pedestal, pedestal, cardBox);

            var clip = new Box(0f, 0f, screenWidth, cardBox.Top);
            UiKit.PlaceBox(_clip, clip, cardBox);
            float radius = Mathf.Max(screenWidth * 0.62f, stage.Height);
            UiKit.PlaceBox(_rays, Box.FromCenter(raysX, raysY, radius * 2f, radius * 2f), clip);
            if (cheer == null)
            {
                UiKit.PlaceBox(_group, group, cardBox);
                return;
            }

            // One celebrating hero, as on the reference's win card: its feet where the group's stand.
            _cheer.sprite = cheer;
            float feet = group.Top + (group.Height * CharacterArt.GroupFeetShare);
            float height = Mathf.Min((feet - stage.Top) / HomeStage.FeetShare, pedestal.Width * 0.75f * CharacterArt.HeroHeight / CharacterArt.HeroWidth);
            UiKit.PlaceBox(_cheer.rectTransform, HomeStage.Figure(stage.CenterX, feet, height), cardBox);
        }
    }

    /// <summary>The Home and splash heroes (<see cref="HeroPictures.Stage"/>).</summary>
    public sealed class HomeStageView
    {
        private readonly RectTransform _root;
        private readonly RectTransform _pedestal;
        private readonly BloomlingFigure[] _heroes = new BloomlingFigure[4];
        private readonly RectTransform _fountain;

        internal HomeStageView(RectTransform root)
        {
            _root = root;
            UiFactory.Stretch(root);
            _pedestal = UiKit.StonePedestal("Pedestal", root);

            // Drawing order as the playtest's: the back row (Bloom, Drop, Sprig), the fountain, Twig in front.
            for (int i = 0; i < 3; i++)
            {
                _heroes[i] = Figure("Hero" + i);
            }

            _fountain = UiKit.LotusFountain("Fountain", root);
            _heroes[3] = Figure("Hero3");
        }

        /// <summary>The stage's rect (it stretches over its parent).</summary>
        public RectTransform Rect => _root;

        /// <summary>
        /// Lays the heroes out on Home or the splash, whose parent's screen box is <paramref name="parent"/> (the whole
        /// screen, as the backdrop). Over the owner's garden picture of <paramref name="scene"/> (pictures.md B1; the splash
        /// takes it while its own is missing) nothing shows for now: the owner deferred the heroes on Home on 2026-10-02
        /// (<see cref="HomeStage.ShowsHeroes"/>; they come back animated later). Without it, the drawn diorama in
        /// <paramref name="stage"/> (<see cref="HomeStage.ReferenceDiorama"/>: the stone ring, the lotus fountain and the
        /// four heroes). Each hero wears <paramref name="outfitOf"/>'s outfit (null: nothing), and <paramref name="front"/>
        /// (the player's hero) swaps places with Sprig at the left front.
        /// </summary>
        public void Place(Box stage, Box parent, BackdropScene scene, Func<Family, Outfit?>? outfitOf = null, Family front = Family.Sprig)
        {
            bool shown = HomeStage.ShowsHeroes(ownerPicture: OwnerArt.BackgroundOf(scene) != null);
            _root.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }

            // The drawn stand-in's heroes ("char.hero.home": in their outfits once the Wardrobe is open).
            HomeDiorama diorama = HomeStage.ReferenceDiorama(stage);
            UiKit.PlaceBox(_pedestal, diorama.Pedestal, parent);
            UiKit.PlaceBox(_fountain, diorama.Fountain, parent);
            IReadOnlyList<(Family Family, Box Box)> heroes = diorama.Heroes;
            for (int i = 0; i < _heroes.Length; i++)
            {
                bool present = i < heroes.Count;
                _heroes[i].Rect.gameObject.SetActive(present);
                if (!present)
                {
                    continue;
                }

                (Family family, Box box) = heroes[i];
                if (front != Family.Sprig && (family == Family.Sprig || family == front))
                {
                    family = family == Family.Sprig ? front : Family.Sprig;
                }

                _heroes[i].ShowHero(family, outfitOf?.Invoke(family));
                UiKit.PlaceBox(_heroes[i].Rect, box, parent);
            }
        }

        private BloomlingFigure Figure(string name)
        {
            BloomlingFigure figure = BloomlingFigure.Create(name, _root);
            figure.Body.raycastTarget = false;
            return figure;
        }
    }
}
