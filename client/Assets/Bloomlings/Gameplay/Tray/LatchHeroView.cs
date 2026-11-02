using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// A latch's hero (17 §8, spec 006 FR-068; the owner's look of 2026-11-02, "Twig hero 100%"; the playtest's
    /// <c>Kit.LatchHero</c>): in the place of the pod the latch holds, our Twig hero in the player's outfit (the owner's modular
    /// V2 frames, <see cref="HeroGearView"/>; stock Twig without one) on a soft contact shadow, holding the padlock in the held
    /// pod's colour in front of his belly, his hands drawn again over its sides (the hero's layers in a disc mask round each
    /// hand, <see cref="RoundLook.HeroHands"/>), lightened in rows 2 and 3 (a white veil masked by his base layer); opening, the
    /// padlock springs open and drops (<see cref="RoundLook.PadlockOpen"/>) and he steps aside and fades
    /// (<see cref="RoundLook.HeroStep"/>). Placed in its parent's top-down canvas units. Presentation only.
    /// </summary>
    public sealed class LatchHeroView
    {
        private readonly RectTransform _root;
        private readonly CanvasGroup _body;
        private readonly Image _shadow;
        private readonly RectTransform _cell;
        private readonly HeroGearView _gear;
        private readonly (Image Mask, Image Veil) _veil;
        private readonly Image _padlock;
        private readonly Hand[] _hands = new Hand[2];
        private Outfit? _worn;
        private bool _dressed;
        private string? _padlockKey;

        private sealed class Hand
        {
            public Image Mask = null!;
            public CanvasGroup Group = null!;
            public RectTransform Cell = null!;
            public HeroGearView Gear = null!;
            public (Image Mask, Image Veil) Veil;
        }

        private LatchHeroView(Transform parent)
        {
            _root = UiFactory.Stretch(UiFactory.CreateRect("LatchHero", parent));
            RectTransform body = UiFactory.Stretch(UiFactory.CreateRect("Body", _root));
            _body = body.gameObject.AddComponent<CanvasGroup>();
            _body.blocksRaycasts = false;
            _shadow = UiFactory.CreateImage("Shadow", body, null, Color.white);
            _shadow.raycastTarget = false;
            PictureFit.On(_shadow, (w, h) => RoundSprites.SoftOval(w, h));
            _cell = UiFactory.CreateRect("Twig", body);
            _gear = HeroGearView.Create(_cell);
            _veil = Veil(_cell);
            _padlock = UiFactory.CreateImage("Padlock", _root, null, Color.white);
            _padlock.raycastTarget = false;
            for (int i = 0; i < _hands.Length; i++)
            {
                Image mask = UiFactory.CreateImage("Hand", _root, ProceduralSprites.Circle, Color.white);
                mask.raycastTarget = false;
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                CanvasGroup group = mask.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                RectTransform cell = UiFactory.CreateRect("Twig", mask.transform);
                _hands[i] = new Hand { Mask = mask, Group = group, Cell = cell, Gear = HeroGearView.Create(cell), Veil = Veil(cell) };
            }
        }

        public static LatchHeroView Create(Transform parent) => new LatchHeroView(parent);

        public RectTransform Root => _root;

        /// <summary>
        /// Shows the hero in the pod's place <paramref name="place"/> (the parent's top-down canvas units) in
        /// <paramref name="outfit"/>, holding a padlock of <paramref name="body"/> with <paramref name="pips"/>,
        /// <paramref name="since"/> seconds after the latch opened (-1: closed), stepping toward <paramref name="side"/>;
        /// <paramref name="waiting"/> in rows 2 and 3.
        /// </summary>
        public void Show(Box place, Outfit? outfit, Rgba body, int pips, float since, int side, bool waiting)
        {
            if (!_dressed || !Equals(_worn, outfit))
            {
                _dressed = true;
                _worn = outfit;
                Dress(outfit);
            }

            Box cell = RoundLook.HeroCell(place);
            (float hx, float hy, float ha) = RoundLook.HeroStep(since, side);
            Box hero = cell.Offset(hx * place.Width, -hy * place.Height);
            float veil = waiting ? RoundLook.HeroWaitingVeil : 0f;
            _body.alpha = ha;
            _body.gameObject.SetActive(ha > 0f);
            BoxLayout.Place(_shadow.rectTransform, RoundLook.HeroShadow(hero));
            BoxLayout.Place(_cell, hero);
            Veil(_veil, veil);

            Box padlock = RoundLook.HeroLock(cell);
            (bool open, float drop, float scale, float alpha) = RoundLook.PadlockOpen(since);
            Rgba face = waiting ? RoundLook.WaitingColor(body) : body;
            string key = UiRaster.PadlockKey(face, pips, open);
            if (_padlockKey != key)
            {
                _padlockKey = key;
                PictureFit.On(_padlock, (w, h) => RoundSprites.Padlock(face, pips, open, Mathf.Min(w, h)), square: true);
            }

            BoxLayout.Place(_padlock.rectTransform, padlock.Offset(0f, drop * padlock.Width));
            _padlock.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _padlock.color = new Color(1f, 1f, 1f, alpha);
            _padlock.gameObject.SetActive(alpha > 0f);

            (Box left, Box right) = RoundLook.HeroHands(hero);
            bool hold = ha > 0f && since < RoundLook.PadlockDropAt;
            for (int i = 0; i < _hands.Length; i++)
            {
                Hand hand = _hands[i];
                Box disc = i == 0 ? left : right;
                hand.Mask.gameObject.SetActive(hold);
                if (!hold)
                {
                    continue;
                }

                hand.Group.alpha = ha;
                BoxLayout.Place(hand.Mask.rectTransform, disc);
                BoxLayout.Place(hand.Cell, hero.Offset(-disc.Left, -disc.Top));
                Veil(hand.Veil, veil);
            }

            _root.gameObject.SetActive(true);
        }

        public void Hide() => _root.gameObject.SetActive(false);

        private void Dress(Outfit? outfit)
        {
            _gear.Wear(Family.Twig, outfit);
            foreach (Hand hand in _hands)
            {
                hand.Gear.Wear(Family.Twig, outfit);
            }

            // the veils follow his base layer's picture and box in the cell
            HeroFrame? layer = HeroGearArt.Layer(Family.Twig, HeroGearArt.DisplayOutfit(outfit), 0);
            Sprite? picture = layer.HasValue ? HeroGearView.Picture(layer.Value.Name) : null;
            foreach (Image mask in new[] { _veil.Mask, _hands[0].Veil.Mask, _hands[1].Veil.Mask })
            {
                mask.sprite = picture;
                if (layer.HasValue)
                {
                    var cell = new Box(0f, 0f, HeroMotion.CellWidth, HeroMotion.CellHeight);
                    Box box = HeroMotion.PictureBox(cell, layer.Value);
                    UiFactory.Place(mask.rectTransform, box.Left / cell.Width, 1f - (box.Bottom / cell.Height), box.Right / cell.Width, 1f - (box.Top / cell.Height));
                }
            }
        }

        // A white veil clipped to the hero's base layer (the waiting rows' lightening), off by default.
        private static (Image Mask, Image Veil) Veil(RectTransform cell)
        {
            Image mask = UiFactory.CreateImage("VeilMask", cell, null, Color.white);
            mask.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Image veil = UiFactory.CreateImage("Veil", mask.transform, null, Color.white);
            veil.raycastTarget = false;
            UiFactory.Stretch(veil.rectTransform);
            mask.gameObject.SetActive(false);
            return (mask, veil);
        }

        private static void Veil((Image Mask, Image Veil) parts, float veil)
        {
            bool on = veil > 0f && parts.Mask.sprite != null;
            parts.Mask.gameObject.SetActive(on);
            if (on)
            {
                parts.Veil.color = new Color(1f, 1f, 1f, veil);
            }
        }
    }
}
