using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>One garden object: transparent layers and a short contact shadow, with a fixed touch target.
    /// A leaf seal marks unseen rewards; the check is reserved for the completed five-step day.</summary>
    public sealed class HomePromoView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>The stand-in sign's size in the scene box: shares of the box's width.</summary>
        private const float SignWidthShare = 0.9f;

        private const float SignHeightShare = 0.32f;

        private readonly Dictionary<string, Image> _pictures = new Dictionary<string, Image>(StringComparer.Ordinal);
        private readonly Dictionary<string, Image> _shadows = new Dictionary<string, Image>(StringComparer.Ordinal);
        private readonly Dictionary<string, (float Width, float Height, float Pad, float Blur)> _shadowSizes = new Dictionary<string, (float, float, float, float)>(StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();
        private readonly HashSet<string> _shown = new HashSet<string>(StringComparer.Ordinal);
        private PromoScene _scene;
        private RectTransform _root = null!;
        private RectTransform _art = null!;
        private bool _down;
        private float _releasedAt = -10f;
        private float _pressScale = 1f;
        private TextMeshProUGUI? _label;
        private WoodSignView? _sign;
        private Box _box;
        private float _start;
        private bool _placed;
        private bool _plated;
        private float _w;
        private RectTransform? _shadowLayer;
        private RectTransform? _badge;
        private RectTransform? _done;

        // Home's floating button (the owner, 2026-10-31, as the reference game's): the round badge and the label's pill.
        private Image? _disc;
        private Image? _pill;
        public bool Completed { get; set; }

        /// <summary>Whether the scene calls for attention (its sequence plays every cycle); else it only idles.</summary>
        public bool Calling { get; set; }
        public Func<float>? Clock { get; set; }
        private float Seconds => Clock != null ? Clock() : Time.unscaledTime - _start;

        /// <summary>
        /// The scene under <paramref name="parent"/> (place it with <see cref="Place"/>), taking taps on its whole box when
        /// <paramref name="onTap"/> is given (Home), none otherwise (the Remove Ads card's picture); on its cream plate with
        /// the pictures' soft shadows when <paramref name="plate"/> (Home).
        /// </summary>
        public static HomePromoView Create(string name, Transform parent, PromoScene scene, bool calling, Action? onTap = null, bool plate = false)
        {
            Image touch = UiFactory.CreateImage(name, parent, null, Color.clear, raycast: onTap != null);
            var view = touch.gameObject.AddComponent<HomePromoView>();
            view._scene = scene;
            view._root = touch.rectTransform;
            view._art = UiFactory.CreateRect("Visual", touch.transform);
            UiFactory.Stretch(view._art);
            view._art.pivot = new Vector2(.5f, 0f);
            view._plated = plate;
            view.Calling = calling;
            if (onTap != null)
            {
                // The squash about the stand's foot, as Home's other round buttons press.
                touch.rectTransform.pivot = new Vector2(0.5f, 0f);
                UiKit.TapTarget(touch, onTap);
            }

            view.Build();
            return view;
        }

        /// <summary>
        /// Lays the scene out in <paramref name="box"/> (screen pixels), its parent lying at <paramref name="parent"/>; the
        /// plate's and the shadows' blur and drop follow the safe width <paramref name="w"/>.
        /// </summary>
        public void Place(Box box, Box parent, float w = 0f)
        {
            _box = box;
            _w = w > 0f ? w : box.Width / Mathf.Max(0.01f, HomePromo.WidthShare);
            UiKit.PlaceBox(_root, box, parent);

            if (_shadowLayer != null)
            {
                UiFactory.Stretch(_shadowLayer);
            }

            if (_sign != null)
            {
                UiKit.PlaceBox((RectTransform)_sign.transform, Box.FromCenter(box.CenterX, box.CenterY, box.Width * SignWidthShare, box.Width * SignHeightShare), box);
            }

            Box disc = HomePromo.BadgeDisc(box);
            if (_disc != null)
            {
                UiKit.PlaceBox(_disc.rectTransform, disc, box);
                UiKit.PlaceBox(_pill!.rectTransform, HomePromo.BadgePill(disc), box);
            }

            if (_label != null)
            {
                if (_plated)
                {
                    // On the badge's pill, its letters BadgeLabelShare of the pill's height.
                    Box pill = UiKit.ToLocal(HomePromo.BadgePill(disc), box);
                    KitText.Place(_label, HomePromo.LabelStyle, pill.CenterX, pill.CenterY, pill.Height * HomePromo.BadgeLabelShare, pill.Width * 0.86f);
                }
                else
                {
                    // Centered on the plaque's face, its letters LabelShare of the face's height, fitted to its width.
                    Box plaque = UiKit.ToLocal(HomePromo.Plaque(box, _scene), box);
                    KitText.Place(_label, HomePromo.LabelStyle, plaque.CenterX, plaque.CenterY, plaque.Height * HomePromo.LabelShare, plaque.Width);
                }
            }

            PlaceBadge();
            if (_done != null) UiKit.PlaceBox(_done, SealOf(box), box);
            _placed = true;
            Animate(Seconds);
        }

        /// <summary>
        /// Shows or hides the "!" over the plate's top-right corner (spec 005 FR-050, the Daily scene while a Daily Reward
        /// step waits and its card was not opened today; <see cref="UiKit.AlertBadge"/>, <see cref="DailyRewardCard.BadgeDisc"/>),
        /// pressing with the scene and pulsing softly.
        /// </summary>
        public void ShowBadge(bool on)
        {
            if (on && _badge == null)
            {
                _badge = UiKit.PromoSeal("Ready", _art, completed: false);
                if (_placed)
                {
                    PlaceBadge();
                }
            }

            if (_badge != null)
            {
                _badge.gameObject.SetActive(on);
                _badge.SetAsLastSibling();
            }
        }

        private void PlaceBadge()
        {
            if (_badge != null)
            {
                UiKit.PlaceBox(_badge, SealOf(_box), _box);
            }
        }

        // The seal's box: over the badge's top right on Home, the stand's on the Remove Ads card.
        private Box SealOf(Box box) => _plated ? HomePromo.BadgeSeal(HomePromo.BadgeDisc(box)) : HomePromo.SealBox(box);

        /// <summary>Starts the scene's time again (the attention sequences count from here).</summary>
        public void Restart() => _start = Time.unscaledTime;

        private void OnEnable() { Restart(); _down = false; _releasedAt = -10f; }
        // Keep the touch rectangle fixed while only the artwork squashes.
        public void OnPointerDown(PointerEventData e) => _down = true;
        public void OnPointerUp(PointerEventData e)
        {
            if (_down) { _down = false; _releasedAt = Time.unscaledTime; }
        }
        public void OnPointerExit(PointerEventData e) => OnPointerUp(e);

        private void Update()
        {
            float scale = HomePromo.PressScale(_down, Time.unscaledTime - _releasedAt) * (_plated ? HomePromo.HomeVisualScale : 1f);
            if (scale != _pressScale) { _pressScale = scale; _art.localScale = Vector3.one * scale; }
            if (_placed)
            {
                Animate(Seconds);
                if (_badge != null) _badge.SetAsLastSibling();
            }

            if (_scene == PromoScene.Daily)
            {
                if (Completed && _done == null)
                {
                    _done = UiKit.PromoSeal("Completed", _art, completed: true);
                    UiKit.PlaceBox(_done, SealOf(_box), _box);
                }
                if (_done != null) { _done.gameObject.SetActive(Completed); _done.SetAsLastSibling(); }
            }
        }

        private void Build()
        {
            string label = Loc.T(HomePromo.LabelKey(_scene));
            if (OwnerArt.Decor(_scene == PromoScene.NoAds ? HomePromo.NoAdsStand : HomePromo.DailyStand) == null)
            {
                _sign = UiKit.WoodSign("Sign", _art, label, T.LevelPill);
                return;
            }

            // On Home the scene's props stand on the round badge (the owner, 2026-10-31), no stand and no shadows.
            if (_plated)
            {
                _disc = UiFactory.CreateImage("Badge", _art, null, Color.white);
                _disc.raycastTarget = false;
                PictureFit.On(_disc, (pw, ph) => ProceduralSprites.Picture("ui.promo.badge", Math.Min(pw, ph), Math.Min(pw, ph), (x, y) => UiRaster.PromoBadge(Math.Min(x, y), C.PromoBadge, HomePromo.BadgeRingShare)), square: true);
            }

            // One image per picture of the scene, hidden until posed; a missing prop is left out.
            foreach (string picture in HomePromo.Pictures)
            {
                Sprite? sprite = OwnerPictures.SlotOf(picture) == HomePromo.Slot(_scene) ? OwnerArt.Decor(picture) : null;
                if (sprite != null)
                {
                    Image image = UiFactory.CreateImage(picture, _art, sprite, Color.white);
                    image.enabled = false;
                    _pictures[picture] = image;
                    (byte[] Alpha, int Width, int Height)? mask = _shadowLayer != null && HomePromo.CastsShadow(picture) ? OwnerArt.DecorAlpha(picture) : null;
                    if (mask != null)
                    {
                        string name = picture;
                        (byte[] alpha, int mw, int mh) = mask.Value;
                        Image shadow = UiFactory.CreateImage(picture + "Shadow", _shadowLayer!, null, Color.white);
                        shadow.raycastTarget = false;
                        shadow.enabled = false;
                        PictureFit.On(shadow, (pw, ph) =>
                        {
                            // Posed once before it shows (Animate); until then nothing.
                            if (!_shadowSizes.TryGetValue(name, out (float Width, float Height, float Pad, float Blur) size))
                            {
                                return ProceduralSprites.Picture(HomePromo.ShadowKey("none"), 1, 1, (x, y) => new byte[4]);
                            }

                            return ProceduralSprites.Picture(HomePromo.ShadowKey(name), pw, ph, (x, y) => HomePromo.Shadow(alpha, mw, mh, x, y, size.Width, size.Height, size.Pad, size.Blur));
                        });
                        _shadows[picture] = shadow;
                    }
                }
            }

            if (_plated)
            {
                _pill = UiFactory.CreateImage("LabelPill", _art, null, Color.white);
                _pill.raycastTarget = false;
                PictureFit.On(_pill, (pw, ph) =>
                {
                    float ring = Math.Max(1f, ph / 0.3f * HomePromo.BadgeRingShare);
                    return ProceduralSprites.Picture("ui.promo.badge_label/" + (int)Math.Round(ring), pw, ph, (x, y) => UiRaster.PromoBadgePill(x, y, C.PromoBadgeLabel, ring));
                });
                _label = UiKit.KitLabel("Label", _art, label, HomePromo.LabelStyle, TextLook.Plain(Rgba.White));
                return;
            }

            _label = UiKit.KitLabel("Label", _art, label, HomePromo.LabelStyle, GardenLook.SignLetters(C.InkBrown));
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

            IReadOnlyList<PromoLayer> layers = _plated ? HomePromo.BadgeLayers(_scene, _box, seconds, Calling) : HomePromo.Layers(_scene, _box, seconds, Calling);
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
                if (_shadows.TryGetValue(layer.Picture, out Image? shade))
                {
                    (PromoLayer s, float pad, float blur) = HomePromo.ShadowOf(layer, _w);
                    _shadowSizes[layer.Picture] = (s.Width, s.Height, pad, blur);
                    RectTransform sr = shade.rectTransform;
                    sr.pivot = new Vector2(s.PivotX, 1f - s.PivotY);
                    UiKit.PlaceBox(sr, s.Box, _box);
                    sr.localScale = new Vector3(s.ScaleX, s.ScaleY, 1f);
                    sr.localEulerAngles = new Vector3(0f, 0f, -s.Rotation);
                    shade.color = color;
                    if (!shade.enabled)
                    {
                        shade.enabled = true;
                    }
                }
            }

            // Only the pictures that just left the frame change state, so the others keep their graphics.
            foreach (KeyValuePair<string, Image> picture in _pictures)
            {
                if (picture.Value.enabled && !_shown.Contains(picture.Key))
                {
                    picture.Value.enabled = false;
                }
            }

            foreach (KeyValuePair<string, Image> shadow in _shadows)
            {
                if (shadow.Value.enabled && !_shown.Contains(shadow.Key))
                {
                    shadow.Value.enabled = false;
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

                if (i == 0 && _label != null && !_plated)
                {
                    _label.transform.SetAsLastSibling();
                }
            }

            // On Home the label's pill and its letters lie over the props, as the reference game's.
            if (_plated && _pill != null && _label != null)
            {
                _pill.transform.SetAsLastSibling();
                _label.transform.SetAsLastSibling();
            }
        }
    }
}
