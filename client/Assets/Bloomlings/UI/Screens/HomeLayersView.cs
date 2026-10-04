using System;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The owner's layered Home in Unity (spec 005 FR-028, pictures.md B1; <see cref="HomeLayers"/>, the playtest's Home
    /// stage) over the garden the backdrop already draws (<c>home</c>, <c>bg.home</c>), back to front: the fountain's back
    /// (<c>bg.home.fountain_back</c>), Drop and Bloom each on their soft shadow (<c>bg.home.shadow</c>), the lotus again
    /// (<c>bg.home.lotus</c>, so Bloom stands behind it), Sprig and Twig on their shadows, the fountain's front stones and
    /// flowers (<c>bg.home.fountain_front</c>, over the heroes' feet) and the petals drifting down
    /// (<c>bg.home.petals</c>, drawn twice so they wrap); the screen's UI goes over it. Every layer lies at
    /// <see cref="HomeLayers.Place"/> in the picture cover-fitted over the screen (<see cref="HomeLayers.Cover"/>, the
    /// backdrop's own fit), so the heroes stay on the fountain on every screen shape. The heroes are the owner's animated
    /// ones (<see cref="HeroMotionView"/>, slots <c>char.hero3d.motion.{family}</c>), each idling from its own phase and
    /// taking turns to react (<see cref="HomeMotion"/>); a tap on a hero (its seam picture's box) makes it react at once.
    /// The splash and Home share one motion while both show (<see cref="Join"/>), so the splash turns into Home without a
    /// jump. A family whose frames are missing shows its still hero picture in its cell.
    /// </summary>
    public sealed class HomeLayersView : MonoBehaviour
    {
        private static Clock? _clock;

        private readonly Family[] _families = new Family[4];
        private readonly HeroMotionView?[] _heroes = new HeroMotionView?[4];
        private readonly BloomlingFigure?[] _stills = new BloomlingFigure?[4];
        private readonly CanvasGroup?[] _stillFades = new CanvasGroup?[4];
        private readonly RawImage[] _shadows = new RawImage[4];
        private readonly RectTransform?[] _touches = new RectTransform?[4];
        private RawImage _fountainBack = null!;
        private RawImage _lotus = null!;
        private RawImage _fountainFront = null!;
        private RawImage _petals = null!;
        private RawImage _petalsAbove = null!;
        private Clock _shared = null!;
        private bool _built;
        private bool _placed;

        /// <summary>
        /// Whether the petals drift (Settings' "Falling petals", the owner's switch of 2026-10-04); null: always. Read every
        /// frame, so a switch in Settings shows at once.
        /// </summary>
        public Func<bool>? PetalsOn { get; set; }
        private Box _picture;
        private Box _parent;
        private float _heroAlpha = 1f;

        /// <summary>
        /// Whether <paramref name="scene"/> (Home or the splash) shows the owner's layered Home: its owner picture is the Home
        /// garden (the splash takes it while its own picture is missing, <see cref="OwnerPictures.Resolve"/>) and the
        /// fountain's back, the lotus and the fountain's front are there (<see cref="HomeLayers.IsLayered"/>, the
        /// playtest's rule too).
        /// </summary>
        public static bool Shows(BackdropScene scene) =>
            HomeLayers.IsLayered(OwnerPictures.Resolve(scene, string.Empty, name => OwnerArt.Background(name) != null), name => OwnerArt.Background(name) != null);

        /// <summary>
        /// The layered stage under <paramref name="parent"/> (it stretches over it; place it with <see cref="Place"/>), its
        /// heroes taking taps when <paramref name="tappable"/> (Home; the splash takes none).
        /// </summary>
        public static HomeLayersView Create(string name, Transform parent, bool tappable)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = root.gameObject.AddComponent<HomeLayersView>();
            view.Build(root, tappable);
            return view;
        }

        /// <summary>How opaque the heroes and their shadows are (the splash fades them in over the fountain).</summary>
        public float HeroAlpha
        {
            get => _heroAlpha;
            set
            {
                _heroAlpha = Mathf.Clamp01(value);
                for (int i = 0; i < _families.Length; i++)
                {
                    if (_heroes[i] != null)
                    {
                        _heroes[i]!.Alpha = _heroAlpha;
                    }

                    if (_stillFades[i] != null)
                    {
                        _stillFades[i]!.alpha = _heroAlpha;
                    }

                    _shadows[i].color = Faded(HomeLayers.ShadowAlpha * _heroAlpha);
                }
            }
        }

        /// <summary>
        /// Lays the layers out over <paramref name="screen"/> (the full-screen box the backdrop cover-fits into, in screen
        /// pixels), the stage's parent lying at <paramref name="parent"/>; each hero wears <paramref name="outfitOf"/>'s
        /// outfit (null: nothing; Home passes it once the Wardrobe is open).
        /// </summary>
        public void Place(Box screen, Box parent, Func<Family, Outfit?>? outfitOf = null)
        {
            _picture = HomeLayers.Cover(screen);
            _parent = parent;
            PlaceLayer(_fountainBack, HomeLayers.FountainBack);
            PlaceLayer(_lotus, HomeLayers.Lotus);
            PlaceLayer(_fountainFront, HomeLayers.FountainFront);
            for (int i = 0; i < _families.Length; i++)
            {
                Family family = _families[i];
                Box cell = HomeLayers.HeroCell(_picture, family);
                Outfit? outfit = outfitOf?.Invoke(family);
                UiKit.PlaceBox(_shadows[i].rectTransform, HomeLayers.ShadowBox(_picture, family), parent);
                Box touch = cell;
                if (_heroes[i] != null)
                {
                    UiKit.PlaceBox(_heroes[i]!.Rect, cell, parent);
                    _heroes[i]!.Wear(outfit);
                    touch = HeroMotion.PictureBox(cell, HeroFrames.Of(family).Frame(MotionClip.Idle, 0));
                }
                else if (_stills[i] != null)
                {
                    UiKit.PlaceBox(_stills[i]!.Rect, cell, parent);
                    _stills[i]!.ShowHero(family, outfit);
                }

                if (_touches[i] != null)
                {
                    UiKit.PlaceBox(_touches[i]!, touch, parent);
                }
            }

            _placed = true;
            Animate(Time.unscaledTime);
        }

        /// <summary>
        /// Joins the motion Home and the splash share: a new one when neither shows, else the one already running (the
        /// splash's, under which Home is built), so the heroes and petals go on where they were.
        /// </summary>
        private static Clock Join()
        {
            if (_clock == null || _clock.Users <= 0)
            {
                _clock = new Clock(Time.unscaledTime);
            }

            _clock.Users++;
            return _clock;
        }

        private void Build(RectTransform root, bool tappable)
        {
            _shared = Join();
            _fountainBack = Layer("FountainBack", root, HomeLayers.FountainBack, 1f);
            bool lotus = false;
            for (int i = 0; i < _families.Length; i++)
            {
                Family family = HomeLayers.DrawOrder[i];
                _families[i] = family;
                if (!lotus && !HomeLayers.BehindLotus(family))
                {
                    _lotus = Layer("Lotus", root, HomeLayers.Lotus, 1f);
                    lotus = true;
                }

                string name = family.ToString();
                _shadows[i] = Layer("Shadow" + name, root, HomeLayers.Shadow, HomeLayers.ShadowAlpha);
                if (HeroFrames.Has(family))
                {
                    HeroMotionView hero = HeroMotionView.Create("Hero" + name, root);
                    hero.Drive(family, _shared.Motion.Player(family));
                    _heroes[i] = hero;
                }
                else
                {
                    // The still hero ("char.hero3d.{family}") while the family's frames are missing.
                    BloomlingFigure still = BloomlingFigure.Create("Hero" + name, root);
                    still.Body.raycastTarget = false;
                    _stillFades[i] = still.Rect.gameObject.AddComponent<CanvasGroup>();
                    _stillFades[i]!.blocksRaycasts = false;
                    _stills[i] = still;
                }
            }

            if (!lotus)
            {
                _lotus = Layer("Lotus", root, HomeLayers.Lotus, 1f);
            }

            _fountainFront = Layer("FountainFront", root, HomeLayers.FountainFront, 1f);
            _petals = Layer("Petals", root, HomeLayers.Petals, HomeLayers.PetalsAlpha);
            _petalsAbove = Layer("PetalsAbove", root, HomeLayers.Petals, HomeLayers.PetalsAlpha);

            // Clear touch boxes over the heroes, front heroes last; the screen's buttons are built after the stage, so
            // they lie above it and keep every tap on them.
            if (tappable)
            {
                for (int i = 0; i < _families.Length; i++)
                {
                    Image touch = UiFactory.CreateImage("Touch" + _families[i].ToString(), root, null, Color.clear, raycast: true);
                    touch.gameObject.AddComponent<HeroTap>().Down = Tap(_families[i]);
                    _touches[i] = touch.rectTransform;
                }
            }

            _built = true;
        }

        private Action Tap(Family family) => () => _shared.Motion.Tap(family, Time.unscaledTime);

        private void Update()
        {
            if (_built && _placed)
            {
                Animate(Time.unscaledTime);
            }
        }

        /// <summary>The heroes' poses and the petals' drift at <paramref name="now"/>.</summary>
        private void Animate(float now)
        {
            _shared.Motion.Update(now);
            for (int i = 0; i < _heroes.Length; i++)
            {
                _heroes[i]?.Tick(now);
            }

            bool drift = _petals.texture != null && (PetalsOn == null || PetalsOn());
            if (_petals.gameObject.activeSelf != drift)
            {
                _petals.gameObject.SetActive(drift);
                _petalsAbove.gameObject.SetActive(drift);
            }

            if (drift)
            {
                Box petals = HomeLayers.PetalsAt(_picture, now - _shared.Start);
                UiKit.PlaceBox(_petals.rectTransform, petals, _parent);
                UiKit.PlaceBox(_petalsAbove.rectTransform, petals.Offset(0f, -_picture.Height), _parent);
            }
        }

        private void OnDestroy()
        {
            if (_built)
            {
                _shared.Users--;
            }
        }

        private void PlaceLayer(RawImage image, PictureBox layer) =>
            UiKit.PlaceBox(image.rectTransform, HomeLayers.Place(_picture, layer), _parent);

        /// <summary>A layer picture (hidden while it is missing), at <paramref name="alpha"/>; never a touch target.</summary>
        private static RawImage Layer(string name, Transform parent, PictureBox layer, float alpha)
        {
            var image = UiFactory.CreateRect(name, parent).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            Texture2D? texture = OwnerArt.Background(layer.Name);
            image.texture = texture;
            image.color = Faded(alpha);
            image.gameObject.SetActive(texture != null);
            return image;
        }

        private static Color Faded(float alpha)
        {
            Color color = Color.white;
            color.a = alpha;
            return color;
        }

        /// <summary>The motion and the petals' start Home and the splash share (<see cref="Join"/>).</summary>
        private sealed class Clock
        {
            public Clock(float now)
            {
                Motion = new HomeMotion(now);
                Start = now;
            }

            public HomeMotion Motion { get; }

            public float Start { get; }

            public int Users { get; set; }
        }
    }

    /// <summary>A hero's touch box on Home: a press makes the hero react at once (no click sound; the hero answers).</summary>
    public sealed class HeroTap : MonoBehaviour, IPointerDownHandler
    {
        public Action? Down { get; set; }

        public void OnPointerDown(PointerEventData e) => Down?.Invoke();
    }
}
