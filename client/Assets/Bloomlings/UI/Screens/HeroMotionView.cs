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
    /// One of the owner's animated heroes (spec 005 FR-028, <see cref="HeroMotion"/>; slots
    /// <c>char.hero3d.motion.{family}</c>) in a frame cell: the rect the caller places (an 8:9 box,
    /// <see cref="HeroMotion.Cell"/> or <see cref="HomeLayers.HeroCell"/>, the feet on its foot line). Each display frame it
    /// shows its player's pose (<see cref="HeroMotionPlayer.Pose"/>): the clip's frame in its picture box
    /// (<see cref="HeroMotion.PictureBox"/>) and, while a reaction cross-fades from the idle frame it interrupted, that idle
    /// frame over it at the pose's alpha. Once the Wardrobe is open it wears an outfit as the still heroes do
    /// (<see cref="BloomlingFigure.ShowHero"/>): the trail behind (<see cref="CharacterArt.TrailBox"/>), the skin pattern
    /// through the frame's own alpha, the expression on its cream badge (<see cref="CharacterArt.ExpressionBadge"/>: the
    /// frames have no blank face) and the hat on the head, turned with it (<see cref="HeroMotion.Hat"/>). Home drives four
    /// of them from its <see cref="HomeMotion"/> (<see cref="Drive"/>, <see cref="Tick"/>); the win and the milestone show
    /// one that celebrates from the moment it shows (Twig's cheer, <see cref="HeroMotionPlayer.Celebrate"/>) and then
    /// idles for as long as it shows (<see cref="Celebrate"/>). It keeps
    /// its family's frames loaded only while it is enabled (<see cref="HeroFrameSet.Hold"/>), changes its images only when
    /// the frame changes and allocates nothing per frame. Never a touch target.
    /// </summary>
    public sealed class HeroMotionView : MonoBehaviour
    {
        /// <summary>The frame cell in its own pixels: boxes are worked out in it and set as shares of the placed rect.</summary>
        private static readonly Box UnitCell = new Box(0f, 0f, HeroMotion.CellWidth, HeroMotion.CellHeight);

        private RectTransform _cell = null!;
        private CanvasGroup _group = null!;
        private Image _trail = null!;
        private Image _frame = null!;
        private Image _skinMask = null!;
        private Image _skin = null!;
        private Image _fade = null!;
        private Image _badge = null!;
        private Image _expression = null!;
        private Image _hat = null!;
        private bool _built;
        private HeroFrameSet? _set;
        private HeroFrameSet? _held;
        private HeroMotionPlayer? _player;
        private bool _celebrates;
        private int _turn;
        private string? _hatShape;
        private bool _wearsHat;
        private bool _wearsSkin;
        private int _shown = -1;
        private int _fadeShown = -1;
        private float _fadeAlpha = -1f;

        /// <summary>A hero view under <paramref name="parent"/>; place its <see cref="Rect"/> at the frame cell.</summary>
        public static HeroMotionView Create(string name, Transform parent)
        {
            RectTransform cell = UiFactory.CreateRect(name, parent);
            var view = cell.gameObject.AddComponent<HeroMotionView>();
            view.Build(cell);
            return view;
        }

        /// <summary>The frame cell's rect (place it at the cell box).</summary>
        public RectTransform Rect => _cell;

        /// <summary>How opaque the hero and what it wears are (the splash fades its heroes in).</summary>
        public float Alpha
        {
            get => _group.alpha;
            set => _group.alpha = value;
        }

        /// <summary>
        /// Shows <paramref name="family"/> in <paramref name="player"/>'s poses (Home: its <see cref="HomeMotion"/>'s
        /// players); the owner calls <see cref="Tick"/> every frame.
        /// </summary>
        public void Drive(Family family, HeroMotionPlayer player)
        {
            Bind(family);
            _celebrates = false;
            _player = player;
        }

        /// <summary>
        /// The win's and the milestone's hero: <paramref name="family"/> celebrates from the moment it shows (now, if it
        /// shows now; else when it is next enabled) with its celebration for its <paramref name="turn"/>-th win
        /// (<see cref="HeroMotion.WinClip"/>), starting on the idle's first pose, and then idles for as long as it shows;
        /// it ticks itself.
        /// </summary>
        public void Celebrate(Family family, int turn = 0)
        {
            Bind(family);
            _celebrates = true;
            _turn = turn;
            _player = null;
            if (isActiveAndEnabled)
            {
                Begin(Time.unscaledTime);
            }
        }

        /// <summary>
        /// Wears <paramref name="outfit"/> (null: nothing; Home passes one once the Wardrobe is open): the trail behind the
        /// hero, the skin pattern through its frame, the expression on its badge and the hat on its head.
        /// </summary>
        public void Wear(Outfit? outfit)
        {
            CosmeticItem? trail = outfit?.Trail;
            _trail.gameObject.SetActive(trail != null);
            if (trail != null)
            {
                _trail.sprite = ProceduralSprites.Accessory(trail.Shape);
                _trail.color = BloomlingFigure.Tint(trail);
                Anchor(_trail.rectTransform, CharacterArt.TrailBox(UnitCell));
            }

            CosmeticItem? skin = outfit?.Skin;
            _wearsSkin = skin != null;
            ShowSkin();
            if (skin != null)
            {
                _skin.sprite = ProceduralSprites.SkinPatternFull(skin.Shape);
                Color tint = BloomlingFigure.Tint(skin);
                tint.a = CosmeticCatalog.SkinOpacity;
                _skin.color = tint;
            }

            // The frames keep their drawn faces: a worn expression goes on the cream badge beside the head.
            CosmeticItem? expression = outfit?.Expression;
            _badge.gameObject.SetActive(expression != null);
            _expression.gameObject.SetActive(expression != null);
            if (expression != null)
            {
                Box disc = CharacterArt.ExpressionBadge(UnitCell);
                _badge.sprite = BloomlingFigure.BadgeDisc;
                Anchor(_badge.rectTransform, disc);
                _expression.sprite = ProceduralSprites.Accessory(expression.Shape);
                _expression.color = UiTheme.Of(DesignTokens.Colors.InkBrown);
                Anchor(_expression.rectTransform, disc.Inset(disc.Width * 0.16f));
            }

            CosmeticItem? hat = outfit?.Hat;
            _wearsHat = hat != null;
            _hatShape = hat?.Shape;
            _hat.gameObject.SetActive(false);
            if (hat != null)
            {
                (Sprite sprite, Color color) = BloomlingFigure.HeroHat(hat);
                _hat.sprite = sprite;
                _hat.color = color;
            }

            // The hat follows the frame shown.
            _shown = -1;
        }

        /// <summary>Shows the pose at <paramref name="now"/> (unscaled seconds); a celebrating view calls it itself.</summary>
        public void Tick(float now)
        {
            if (_held == null || _player == null)
            {
                return;
            }

            HeroFrameSet set = _held;
            HeroPose pose = _player.Pose(now);
            int key = Key(pose.Clip, pose.Index, set);
            if (key != _shown)
            {
                Sprite? sprite = set.Sprite(pose.Clip, pose.Index);
                if (sprite != null)
                {
                    // A missing frame keeps the one before it.
                    HeroFrame frame = set.Frame(pose.Clip, pose.Index);
                    Box box = HeroMotion.PictureBox(UnitCell, frame);
                    _frame.sprite = sprite;
                    _frame.enabled = true;
                    Anchor(_frame.rectTransform, box);
                    _skinMask.sprite = sprite;
                    Anchor(_skinMask.rectTransform, box);
                    ShowSkin();
                    PlaceHat(set, frame);
                    _shown = key;
                }
            }

            int from = pose.FromIdle;
            if (from != _fadeShown || pose.FromAlpha != _fadeAlpha)
            {
                Sprite? sprite = from >= 0 ? set.Sprite(MotionClip.Idle, from) : null;
                _fade.enabled = sprite != null;
                if (sprite != null)
                {
                    if (from != _fadeShown)
                    {
                        _fade.sprite = sprite;
                        Anchor(_fade.rectTransform, HeroMotion.PictureBox(UnitCell, set.Frame(MotionClip.Idle, from)));
                    }

                    Color color = Color.white;
                    color.a = pose.FromAlpha;
                    _fade.color = color;
                }

                _fadeShown = from;
                _fadeAlpha = pose.FromAlpha;
            }
        }

        private void Build(RectTransform cell)
        {
            _cell = cell;
            _group = cell.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            // Back to front: the trail, the frame, the skin through the frame's alpha, the cross-fade, the badge, the hat.
            _trail = Part("Trail", cell);
            _frame = Part("Frame", cell);
            _frame.preserveAspect = false;
            _frame.gameObject.SetActive(true);
            _frame.enabled = false;
            _skinMask = Part("SkinMask", cell);
            _skinMask.preserveAspect = false;
            Mask mask = _skinMask.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            _skin = UiFactory.CreateImage("Skin", _skinMask.transform, null, Color.white);
            _skin.raycastTarget = false;
            UiFactory.Stretch(_skin.rectTransform);
            _fade = Part("CrossFade", cell);
            _fade.preserveAspect = false;
            _fade.gameObject.SetActive(true);
            _fade.enabled = false;
            _badge = Part("ExpressionBadge", cell);
            _expression = Part("Expression", cell);
            _hat = Part("Hat", cell);
            _built = true;
        }

        private void Bind(Family family)
        {
            HeroFrameSet set = HeroFrames.Of(family);
            if (_set != set)
            {
                Hold(false);
                _set = set;
                _shown = -1;
                _fadeShown = -1;
            }

            Hold(isActiveAndEnabled);
        }

        private void Begin(float now)
        {
            if (_set == null)
            {
                return;
            }

            _player = new HeroMotionPlayer(_set.Family, now);
            _player.Celebrate(now, turn: _turn);
            _shown = -1;
            _fadeShown = -1;
            Tick(now);
        }

        /// <summary>Holds the family's frames while the view shows (enabled), and lets them go when it hides.</summary>
        private void Hold(bool on)
        {
            if (on && _held == null && _set != null)
            {
                _held = _set;
                _held.Hold(true);
            }
            else if (!on && _held != null)
            {
                _held.Hold(false);
                _held = null;

                // The textures may be unloaded now: show nothing until the next frame loads them again.
                _frame.sprite = null;
                _frame.enabled = false;
                _skinMask.sprite = null;
                ShowSkin();
                _fade.sprite = null;
                _fade.enabled = false;
                _shown = -1;
                _fadeShown = -1;
            }
        }

        private void OnEnable()
        {
            if (!_built)
            {
                return;
            }

            Hold(true);
            if (_celebrates)
            {
                Begin(Time.unscaledTime);
            }
        }

        private void OnDisable()
        {
            if (_built)
            {
                Hold(false);
            }
        }

        private void OnDestroy()
        {
            if (_built)
            {
                Hold(false);
            }
        }

        private void Update()
        {
            if (_celebrates)
            {
                Tick(Time.unscaledTime);
            }
        }

        /// <summary>The skin pattern shows through the frame shown only (never as a square before a frame loads).</summary>
        private void ShowSkin() => _skinMask.gameObject.SetActive(_wearsSkin && _frame.enabled);

        /// <summary>The worn hat on the frame shown: on the head's line, turned with the head about its box's middle.</summary>
        private void PlaceHat(HeroFrameSet set, HeroFrame frame)
        {
            if (!_wearsHat)
            {
                return;
            }

            (Box box, float degrees) = HeroMotion.Hat(UnitCell, set.Family, frame, _hatShape);
            Anchor(_hat.rectTransform, box);

            // The kit turns clockwise on screen; a positive z turn is counterclockwise.
            _hat.rectTransform.localEulerAngles = new Vector3(0f, 0f, -degrees);
            _hat.gameObject.SetActive(true);
        }

        private static int Key(MotionClip clip, int index, HeroFrameSet set) => set.Slot(clip, index);

        /// <summary>Anchors a rect of the cell to a box in the cell's pixels (y down), so it follows the placed rect's size.</summary>
        private static void Anchor(RectTransform rect, Box box) =>
            UiFactory.Place(rect, box.Left / UnitCell.Width, 1f - (box.Bottom / UnitCell.Height), box.Right / UnitCell.Width, 1f - (box.Top / UnitCell.Height));

        private static Image Part(string name, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
