using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// One of Home's promo scenes in Unity (spec 005 FR-032, <see cref="HomePromo"/>; slots <c>ui.promo.no_ads</c> and
    /// <c>ui.promo.daily</c>): the owner's layer pictures of the scene (<c>Decor/promo-*.png</c>), one image each, posed every
    /// frame from <see cref="HomePromo.Layers"/> back to front, with the scene's label (<see cref="HomePromo.LabelKey"/>) on
    /// the stand's wooden plaque right after the stand, in the wooden sign's brown letters. The time is the seconds since
    /// the scene was shown (Home opening, or the card opening), so each attention sequence first plays a little after
    /// Home opens; a scene that is not <see cref="Calling"/> only idles. The whole scene box is the touch target when it
    /// takes taps: it clicks and squashes like Home's profile avatar (<see cref="PressMotion"/>). While the stand's picture
    /// is missing, the label shows on a wooden sign (<c>ui.sign.wood</c>) inside the box instead.
    /// </summary>
    public sealed class HomePromoView : MonoBehaviour
    {
        /// <summary>The stand-in sign's size in the scene box: shares of the box's width.</summary>
        private const float SignWidthShare = 0.9f;

        private const float SignHeightShare = 0.32f;

        private readonly Dictionary<string, Image> _pictures = new Dictionary<string, Image>(StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();
        private readonly HashSet<string> _shown = new HashSet<string>(StringComparer.Ordinal);
        private PromoScene _scene;
        private RectTransform _root = null!;
        private TextMeshProUGUI? _label;
        private WoodSignView? _sign;
        private Box _box;
        private float _start;
        private bool _placed;

        /// <summary>Whether the scene calls for attention (its sequence plays every cycle); else it only idles.</summary>
        public bool Calling { get; set; }

        /// <summary>
        /// The scene under <paramref name="parent"/> (place it with <see cref="Place"/>), taking taps on its whole box when
        /// <paramref name="onTap"/> is given (Home), none otherwise (the Remove Ads card's picture).
        /// </summary>
        public static HomePromoView Create(string name, Transform parent, PromoScene scene, bool calling, Action? onTap = null)
        {
            Image touch = UiFactory.CreateImage(name, parent, null, Color.clear, raycast: onTap != null);
            var view = touch.gameObject.AddComponent<HomePromoView>();
            view._scene = scene;
            view._root = touch.rectTransform;
            view.Calling = calling;
            if (onTap != null)
            {
                // The squash about the stand's foot, as Home's other round buttons press.
                touch.rectTransform.pivot = new Vector2(0.5f, 0f);
                UiKit.TapTarget(touch, onTap);
                touch.gameObject.AddComponent<PressMotion>();
            }

            view.Build();
            return view;
        }

        /// <summary>Lays the scene out in <paramref name="box"/> (screen pixels), its parent lying at <paramref name="parent"/>.</summary>
        public void Place(Box box, Box parent)
        {
            _box = box;
            UiKit.PlaceBox(_root, box, parent);
            if (_sign != null)
            {
                UiKit.PlaceBox((RectTransform)_sign.transform, Box.FromCenter(box.CenterX, box.CenterY, box.Width * SignWidthShare, box.Width * SignHeightShare), box);
            }

            if (_label != null)
            {
                // Centered on the plaque's face, its letters LabelShare of the face's height, fitted to its width.
                Box plaque = UiKit.ToLocal(HomePromo.Plaque(box, _scene), box);
                KitText.Place(_label, T.LevelPill, plaque.CenterX, plaque.CenterY, plaque.Height * HomePromo.LabelShare, plaque.Width);
            }

            _placed = true;
            Animate(Time.unscaledTime - _start);
        }

        /// <summary>Starts the scene's time again (the attention sequences count from here).</summary>
        public void Restart() => _start = Time.unscaledTime;

        private void OnEnable() => Restart();

        private void Update()
        {
            if (_placed)
            {
                Animate(Time.unscaledTime - _start);
            }
        }

        private void Build()
        {
            string label = Loc.T(HomePromo.LabelKey(_scene));
            if (OwnerArt.Decor(_scene == PromoScene.NoAds ? HomePromo.NoAdsStand : HomePromo.DailyStand) == null)
            {
                _sign = UiKit.WoodSign("Sign", _root, label, T.LevelPill);
                return;
            }

            // One image per picture of the scene, hidden until posed; a missing prop is left out.
            foreach (string picture in HomePromo.Pictures)
            {
                Sprite? sprite = OwnerPictures.SlotOf(picture) == HomePromo.Slot(_scene) ? OwnerArt.Decor(picture) : null;
                if (sprite != null)
                {
                    Image image = UiFactory.CreateImage(picture, _root, sprite, Color.white);
                    image.enabled = false;
                    _pictures[picture] = image;
                }
            }

            _label = UiKit.KitLabel("Label", _root, label, T.LevelPill, GardenLook.SignLetters(C.InkBrown));
        }

        /// <summary>
        /// Poses the pictures at <paramref name="seconds"/>: each canvas in its box with its pivot on the kit's point (Unity's
        /// pivot is y up), scaled, turned clockwise on screen (Unity turns counterclockwise) and faded; pictures not drawn
        /// this frame hide. The draw order follows the kit's list, the label right after the stand.
        /// </summary>
        private void Animate(float seconds)
        {
            if (_pictures.Count == 0)
            {
                return;
            }

            IReadOnlyList<PromoLayer> layers = HomePromo.Layers(_scene, _box, seconds, Calling);
            Reorder(layers);
            _shown.Clear();
            for (int i = 0; i < layers.Count; i++)
            {
                PromoLayer layer = layers[i];
                if (layer.Alpha <= 0f || !_pictures.TryGetValue(layer.Picture, out Image? image))
                {
                    continue;
                }

                RectTransform rect = image.rectTransform;
                rect.pivot = new Vector2(layer.PivotX, 1f - layer.PivotY);
                UiKit.PlaceBox(rect, layer.Box, _box);
                rect.localScale = new Vector3(layer.ScaleX, layer.ScaleY, 1f);
                rect.localEulerAngles = new Vector3(0f, 0f, -layer.Rotation);
                Color color = Color.white;
                color.a = layer.Alpha;
                image.color = color;
                if (!image.enabled)
                {
                    image.enabled = true;
                }

                _shown.Add(layer.Picture);
            }

            // Only the pictures that just left the frame change state, so the others keep their graphics.
            foreach (KeyValuePair<string, Image> picture in _pictures)
            {
                if (picture.Value.enabled && !_shown.Contains(picture.Key))
                {
                    picture.Value.enabled = false;
                }
            }
        }

        /// <summary>Puts the images in the list's order (only when it changes), the label after the first one.</summary>
        private void Reorder(IReadOnlyList<PromoLayer> layers)
        {
            bool same = layers.Count == _order.Count;
            for (int i = 0; same && i < layers.Count; i++)
            {
                same = layers[i].Picture == _order[i];
            }

            if (same)
            {
                return;
            }

            _order.Clear();
            for (int i = 0; i < layers.Count; i++)
            {
                _order.Add(layers[i].Picture);
                if (_pictures.TryGetValue(layers[i].Picture, out Image? image))
                {
                    image.transform.SetAsLastSibling();
                }

                if (i == 0 && _label != null)
                {
                    _label.transform.SetAsLastSibling();
                }
            }
        }
    }
}
