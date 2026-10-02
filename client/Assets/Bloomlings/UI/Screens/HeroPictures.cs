using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The 3D heroes of spec 004 (FR-017) on the meta screens only, in the reference look of spec 005 (contracts/look.md
    /// §4.4, §4.5): the group picture, the celebration of the win and milestone cards (light rays, a stone pedestal and
    /// the heroes on it) and the drawn Home stage (the heroes around the lotus fountain on a stone pedestal). Without the
    /// pictures it shows the four family silhouettes (FR-021).
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
        /// The Leafling experiment, a guest on Home (spec 004 research R17): the owner's Meshy model as a pre-rendered
        /// picture. Hidden when the picture is missing.
        /// </summary>
        public static Image Guest(string name, Transform parent)
        {
            Sprite? picture = CharacterSprites.Get(CharacterArt.Leafling);
            Image image = UiFactory.CreateImage(name, parent, picture, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(picture != null);
            return image;
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
        /// The celebration over a win or milestone card (spec 005 §4.4, the playtest's <c>EndCards.Celebration</c>), as
        /// children of <paramref name="card"/> so it pops with it: slowly turning light rays clipped above the card's top
        /// edge, a stone pedestal, and the heroes standing on it. Place it with <see cref="CelebrationView.Place"/>.
        /// </summary>
        public static CelebrationView Celebration(RectTransform card) => new CelebrationView(card);

        /// <summary>
        /// The drawn Home stage (spec 005 §4.5, the playtest's <c>HomeScreen.Stage</c>) under <paramref name="parent"/>:
        /// until the owner's garden picture, the stone pedestal with the four heroes in an arc around the lotus fountain
        /// (<see cref="HomeStage.Diorama"/>) and the guest; over the owner's picture, the group picture and the guest. Place
        /// it with <see cref="HomeStageView.Place"/>.
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
        /// picture exists; null shows the group. Call <see cref="Place"/> after it.
        /// </summary>
        public void ShowHero(Family? family) => _family = family;

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

    /// <summary>The drawn Home stage (<see cref="HeroPictures.Stage"/>).</summary>
    public sealed class HomeStageView
    {
        private readonly RectTransform _root;
        private readonly RectTransform _pedestal;
        private readonly BloomlingFigure[] _heroes = new BloomlingFigure[4];
        private readonly RectTransform _fountain;
        private readonly Image _guest;
        private readonly RectTransform _group;

        internal HomeStageView(RectTransform root)
        {
            _root = root;
            UiFactory.Stretch(root);
            _pedestal = UiKit.StonePedestal("Pedestal", root);

            // Drawing order as the playtest's: the back row (Bloom, Sprig, Drop), the fountain and the guest, Twig in front.
            for (int i = 0; i < 3; i++)
            {
                _heroes[i] = Figure("Hero" + i);
            }

            _fountain = UiKit.LotusFountain("Fountain", root);
            _group = HeroPictures.Group("Group", root);
            _guest = HeroPictures.Guest("Leafling", root);
            _heroes[3] = Figure("Hero3");
        }

        /// <summary>The stage's rect (it stretches over its parent).</summary>
        public RectTransform Rect => _root;

        /// <summary>
        /// Lays the stage out in <paramref name="stage"/> (screen pixels) of a parent whose screen box is
        /// <paramref name="parent"/>. Over the owner's garden picture of <paramref name="scene"/> (pictures.md B1, B6; it
        /// has its own well and fountain) the group picture stands in front of it; until then the drawn diorama. The guest
        /// shows when <paramref name="guest"/> and its picture exists.
        /// </summary>
        public void Place(Box stage, Box parent, BackdropScene scene, bool guest)
        {
            bool owner = OwnerArt.Background(OwnerPictures.Background(scene, string.Empty)) != null;
            bool hasGuest = guest && CharacterSprites.Get(CharacterArt.Leafling) != null;
            _pedestal.gameObject.SetActive(!owner);
            _fountain.gameObject.SetActive(!owner);
            _group.gameObject.SetActive(owner);
            foreach (BloomlingFigure hero in _heroes)
            {
                hero.Rect.gameObject.SetActive(!owner);
            }

            _guest.gameObject.SetActive(hasGuest);
            if (owner)
            {
                (Box group, Box beside) = CharacterArt.GroupWithGuest(stage);
                UiKit.PlaceBox(_group, group, parent);
                UiKit.PlaceBox(_guest.rectTransform, beside, parent);
                return;
            }

            HomeDiorama diorama = HomeStage.Diorama(stage, guest);
            UiKit.PlaceBox(_pedestal, diorama.Pedestal, parent);
            UiKit.PlaceBox(_fountain, diorama.Fountain, parent);
            UiKit.PlaceBox(_guest.rectTransform, diorama.Guest, parent);
            for (int i = 0; i < _heroes.Length && i < diorama.Heroes.Count; i++)
            {
                (Family family, Box box) = diorama.Heroes[i];
                _heroes[i].ShowHero(family, null);
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
