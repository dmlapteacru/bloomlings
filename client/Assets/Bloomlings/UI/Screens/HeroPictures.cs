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
    /// <summary>What Home and the splash stand their heroes in (<see cref="HeroPictures.StageOf"/>).</summary>
    public enum HomeHeroes
    {
        /// <summary>No heroes: an owner picture without the layered fountain (a splash picture of its own).</summary>
        None,

        /// <summary>The drawn stand-in: the stone ring, the lotus fountain and the four still heroes around it.</summary>
        Drawn,

        /// <summary>The owner's layered Home: the animated heroes on the painted fountain (<see cref="HomeLayersView"/>).</summary>
        Layered,
    }

    /// <summary>
    /// The 3D heroes of spec 004 (FR-017) on the meta screens only, in the reference look of spec 005 (contracts/look.md
    /// §4.4, §4.5): the group picture, the celebration of the win and milestone cards (light rays, a stone pedestal and
    /// the heroes on it; the owner's animated hero when its frames are there, FR-028) and the Home stage (the owner's
    /// layered Home with the animated heroes, or the drawn stand-in: the heroes around the lotus fountain on a stone
    /// pedestal). Without the pictures it shows the four family silhouettes (FR-021).
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
        /// The Home and splash heroes (spec 005 FR-024, FR-028, §4.5, §6.4; the playtest's <c>HomeScreen.Stage</c>) under
        /// <paramref name="parent"/> (<see cref="StageOf"/>): over the owner's garden picture, its layered fountain with
        /// the animated heroes (<see cref="HomeLayersView"/>; a tap on a hero makes it react when
        /// <paramref name="tappable"/>); without it, the drawn diorama (the stone ring, the lotus fountain and the heroes
        /// around it). Place it with <see cref="HomeStageView.Place"/>.
        /// </summary>
        public static HomeStageView Stage(string name, Transform parent, bool tappable = true) => new HomeStageView(UiFactory.CreateRect(name, parent), tappable);

        /// <summary>
        /// What Home and the splash stand their heroes in (spec 005 FR-024, FR-028): over the owner's garden picture
        /// (<paramref name="ownerPicture"/>) the layered Home with the animated heroes when its layers are there
        /// (<paramref name="layered"/>), else none; without the owner's picture the drawn stand-in with the still heroes.
        /// </summary>
        public static HomeHeroes StageOf(bool ownerPicture, bool layered) =>
            !HomeStage.ShowsHeroes(ownerPicture, layered) ? HomeHeroes.None : ownerPicture ? HomeHeroes.Layered : HomeHeroes.Drawn;

        /// <summary>
        /// Whether the win and the milestone show <paramref name="family"/>'s animated hero (spec 005 FR-028): its frames
        /// are baked and there (<see cref="HeroFrames.Has"/>). Else they show its still celebrating picture
        /// (<see cref="Cheer"/>), or the group while that is missing too.
        /// </summary>
        public static bool Animated(Family family) => HeroFrames.Has(family);
    }

    /// <summary>The celebration of the win and milestone cards (<see cref="HeroPictures.Celebration"/>).</summary>
    public sealed class CelebrationView
    {
        private readonly RectTransform _clip;
        private readonly RectTransform _rays;
        private readonly RectTransform _pedestal;
        private readonly RectTransform _group;
        private readonly Image _cheer;
        private readonly HeroMotionView _motion;
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

            // The owner's animated hero ("char.hero3d.motion.{family}", FR-028), where the cheer picture would stand.
            _motion = HeroMotionView.Create("Motion", card);
            _motion.Rect.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows <paramref name="family"/>'s hero instead of the group: its animated hero when its frames are there
        /// (<see cref="HeroPictures.Animated"/>: the reaction from the moment it shows, then the idle for as long as it
        /// shows), else the owner's celebrating picture (pictures.md A7) when it exists; null shows the group. Call
        /// <see cref="Place(Box, Box, float, float)"/> or <see cref="Place(WinRegions, Box, bool)"/> after it.
        /// </summary>
        public void ShowHero(Family? family) => _family = family;

        /// <summary>
        /// What stands on the pedestal for the family shown: the animated hero, else the still cheer picture, else (both
        /// none) the group.
        /// </summary>
        private (bool Animated, Sprite? Cheer) HeroOf()
        {
            if (!_family.HasValue)
            {
                return (false, null);
            }

            bool animated = HeroPictures.Animated(_family.Value);
            return (animated, animated ? null : HeroPictures.Cheer(_family.Value));
        }

        /// <summary>
        /// Shows the animated hero in the frame cell fitted into <paramref name="box"/> (screen pixels, the card or screen
        /// lying at <paramref name="parent"/>), reacting from the moment it shows, or hides it.
        /// </summary>
        private void ShowMotion(bool shown, Box box, Box parent)
        {
            if (!shown || !_family.HasValue)
            {
                _motion.Rect.gameObject.SetActive(false);
                return;
            }

            UiKit.PlaceBox(_motion.Rect, HeroMotion.Cell(box), parent);
            _motion.Rect.gameObject.SetActive(true);
            _motion.Celebrate(_family.Value);
        }

        /// <summary>
        /// Brings the pedestal and the heroes in front of everything built after the celebration so far (the full-screen
        /// win's finished picture), keeping the rays behind: the hero overlaps the picture's foot as on the reference.
        /// </summary>
        public void BringHeroesForward()
        {
            _pedestal.SetAsLastSibling();
            _group.SetAsLastSibling();
            _cheer.rectTransform.SetAsLastSibling();
            _motion.Rect.SetAsLastSibling();
        }

        /// <summary>
        /// Lays the celebration out on the full-screen win or milestone (spec 005 FR-023, contracts/look.md §6.3) whose
        /// screen box is <paramref name="parent"/>: the stone pedestal in <see cref="WinRegions.Pedestal"/> (left out with
        /// <paramref name="pedestal"/> false, when the owner's win picture paints the stage there), the owner's
        /// celebrating hero in the 8:9 <see cref="WinRegions.Hero"/> box (animated in the frame cell fitted into it,
        /// <see cref="HeroMotion.Cell"/>, when its frames are there), or else the group standing with its feet where the
        /// hero's stand (<see cref="HomeStage.FeetShare"/> of the box), as wide as the pedestal over 0.8 (its heads inside
        /// the box), and the rays (radius <see cref="WinRegions.RaysRadius"/>) turning around the hero, unclipped.
        /// </summary>
        public void Place(WinRegions r, Box parent, bool pedestal = true)
        {
            (bool animated, Sprite? cheer) = HeroOf();
            _clip.gameObject.SetActive(true);
            _pedestal.gameObject.SetActive(pedestal);
            _group.gameObject.SetActive(!animated && cheer == null);
            _cheer.gameObject.SetActive(cheer != null);
            UiKit.PlaceBox(_clip, parent, parent);
            UiKit.PlaceBox(_rays, Box.FromCenter(r.RaysX, r.RaysY, r.RaysRadius * 2f, r.RaysRadius * 2f), parent);
            UiKit.PlaceBox(_pedestal, r.Pedestal, parent);
            ShowMotion(animated, r.Hero, parent);
            if (animated)
            {
                return;
            }

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
            (bool animated, Sprite? cheer) = fits ? HeroOf() : (false, null);
            _group.gameObject.SetActive(fits && !animated && cheer == null);
            _cheer.gameObject.SetActive(fits && cheer != null);
            if (!fits)
            {
                ShowMotion(false, stage, cardBox);
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
            if (!animated && cheer == null)
            {
                ShowMotion(false, stage, cardBox);
                UiKit.PlaceBox(_group, group, cardBox);
                return;
            }

            // One celebrating hero, as on the reference's win card: its feet where the group's stand.
            float feet = group.Top + (group.Height * CharacterArt.GroupFeetShare);
            float height = Mathf.Min((feet - stage.Top) / HomeStage.FeetShare, pedestal.Width * 0.75f * CharacterArt.HeroHeight / CharacterArt.HeroWidth);
            Box hero = HomeStage.Figure(stage.CenterX, feet, height);
            ShowMotion(animated, hero, cardBox);
            if (cheer != null)
            {
                _cheer.sprite = cheer;
                UiKit.PlaceBox(_cheer.rectTransform, hero, cardBox);
            }
        }
    }

    /// <summary>The Home and splash heroes (<see cref="HeroPictures.Stage"/>).</summary>
    public sealed class HomeStageView
    {
        private readonly RectTransform _root;
        private readonly RectTransform _drawn;
        private readonly RectTransform _pedestal;
        private readonly BloomlingFigure[] _heroes = new BloomlingFigure[4];
        private readonly RectTransform _fountain;
        private readonly bool _tappable;
        private HomeLayersView? _layers;

        internal HomeStageView(RectTransform root, bool tappable)
        {
            _root = root;
            _tappable = tappable;
            UiFactory.Stretch(root);

            // The drawn stand-in, in the playtest's drawing order: the back row (Bloom, Drop, Sprig), the fountain, Twig
            // in front. The owner's layered Home is built the first time it shows (Place).
            _drawn = UiFactory.Stretch(UiFactory.CreateRect("Diorama", root));
            _pedestal = UiKit.StonePedestal("Pedestal", _drawn);
            for (int i = 0; i < 3; i++)
            {
                _heroes[i] = Figure("Hero" + i);
            }

            _fountain = UiKit.LotusFountain("Fountain", _drawn);
            _heroes[3] = Figure("Hero3");
        }

        /// <summary>The stage's rect (it stretches over its parent).</summary>
        public RectTransform Rect => _root;

        /// <summary>The owner's layered Home while it shows (the splash fades its heroes in), else null.</summary>
        public HomeLayersView? Layers => _layers != null && _root.gameObject.activeSelf && _layers.gameObject.activeSelf ? _layers : null;

        /// <summary>
        /// Lays the heroes out on Home or the splash, whose parent's screen box is <paramref name="parent"/> (the whole
        /// screen, as the backdrop; <see cref="HeroPictures.StageOf"/>). Over the owner's garden picture of
        /// <paramref name="scene"/> (pictures.md B1; the splash takes it while its own is missing) with its fountain
        /// layers, the layered Home with the four animated heroes where the reference stands them
        /// (<see cref="HomeLayersView"/>, over the whole screen); over an owner picture without the layers, none. Without
        /// the owner's picture, the drawn diorama in <paramref name="stage"/> (<see cref="HomeStage.ReferenceDiorama"/>:
        /// the stone ring, the lotus fountain and the four still heroes), where <paramref name="front"/> (the player's
        /// hero) swaps places with Sprig at the left front. Each hero wears <paramref name="outfitOf"/>'s outfit (null:
        /// nothing).
        /// </summary>
        public void Place(Box stage, Box parent, BackdropScene scene, Func<Family, Outfit?>? outfitOf = null, Family front = Family.Sprig)
        {
            bool owner = OwnerArt.BackgroundOf(scene) != null;
            HomeHeroes kind = HeroPictures.StageOf(owner, owner && HomeLayersView.Shows(scene));
            _root.gameObject.SetActive(kind != HomeHeroes.None);
            _drawn.gameObject.SetActive(kind == HomeHeroes.Drawn);
            if (kind == HomeHeroes.Layered)
            {
                // The layered Home lies over the whole screen: the box the backdrop cover-fits the garden into.
                if (_layers == null)
                {
                    _layers = HomeLayersView.Create("Layers", _root, _tappable);
                }

                _layers.gameObject.SetActive(true);
                _layers.Place(parent, parent, outfitOf);
                return;
            }

            if (_layers != null)
            {
                _layers.gameObject.SetActive(false);
            }

            if (kind == HomeHeroes.None)
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
            BloomlingFigure figure = BloomlingFigure.Create(name, _drawn);
            figure.Body.raycastTarget = false;
            return figure;
        }
    }
}
