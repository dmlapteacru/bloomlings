using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The milestone of the design board's frame 16 (spec 002 FR-021; spec 001 FR-061), shown after a milestone win's
    /// Next, as the win's full-screen celebration (spec 005 FR-023, contracts/look.md §4.4 and §6.3; the playtest's
    /// <c>EndCards.Milestone</c>), laid out by <see cref="ScreenLayout.WinScreen"/> on the win's garden (<c>bg.win</c>):
    /// <list type="bullet">
    /// <item><description>the wooden sign with flower clusters, "Level N", and under it the cream "Milestone reached!" mark
    /// with the gold medal;</description></item>
    /// <item><description>each reward (the cosmetic item, the Petals, each booster) as its icon on a cream tile with its
    /// amount in a cream pill over the tile's bottom edge, rising in, where the win shows its picture;</description></item>
    /// <item><description>the level's celebrating hero (else the group) on the stone pedestal in light rays, petals falling
    /// and, for the first seconds, confetti in the level's colors around the sign;</description></item>
    /// <item><description>Continue in its wooden rim, decorated and breathing, where the win shows Next.</description></item>
    /// </list>
    /// It covers the gameplay and takes every tap; Continue goes on.
    /// </summary>
    public sealed class MilestoneCard : MonoBehaviour
    {
        private readonly List<GameObject> _items = new List<GameObject>();
        private GameObject _root = null!;
        private RectTransform _rect = null!;
        private BackdropView? _backdrop;
        private BackgroundTheme? _theme;
        private CelebrationView _celebration = null!;
        private CelebrationSignView _sign = null!;
        private CostPillView _reached = null!;
        private RectTransform _medal = null!;
        private RectTransform _row = null!;
        private CanvasGroup _rowFade = null!;
        private Button _continue = null!;
        private RectTransform _petals = null!;
        private RectTransform _confettiClip = null!;
        private ConfettiView _confetti = null!;

        public bool IsOpen => _root.activeSelf;

        public static MilestoneCard Create(Transform parent, Action onContinue)
        {
            // The whole screen takes every tap while the celebration shows; its garden covers the gameplay below.
            Image root = UiFactory.CreateImage("MilestoneCard", parent, null, UiTheme.Of(C.LawnLight), raycast: true);
            UiFactory.Stretch(root.rectTransform);
            var screen = root.gameObject.AddComponent<MilestoneCard>();
            screen._root = root.gameObject;
            screen._rect = root.rectTransform;
            Transform t = root.transform;
            screen._celebration = HeroPictures.Celebration(root.rectTransform);
            screen._sign = UiKit.CelebrationSign("Title", t, string.Empty);
            screen._sign.gameObject.AddComponent<PopMotion>();
            screen._reached = UiKit.TextPill("Reached", t, CostKind.Charges, Loc.T("milestone.reached"));
            screen._medal = UiKit.ShapeImage("Medal", screen._reached.transform, "ui.medal", C.MedalGold).rectTransform;
            screen._row = UiFactory.Stretch(UiFactory.CreateRect("Rewards", t));
            screen._rowFade = screen._row.gameObject.AddComponent<CanvasGroup>();
            screen._rowFade.blocksRaycasts = false;

            // Continue: the main button, decorated and breathing while it waits (spec 003 FR-011a, FR-019).
            screen._continue = UiKit.PrimaryButton("Continue", t, Loc.T("milestone.continue"), onContinue, T.ButtonLarge, decorate: true, breathe: true);
            screen._petals = (RectTransform)UiKit.FallingPetals("Petals", t).transform;
            UiKit.FadeInOnShow(screen._petals.gameObject, 1f, 0.5f);
            screen._confettiClip = UiFactory.CreateRect("ConfettiClip", t);
            screen._confettiClip.gameObject.AddComponent<RectMask2D>();
            screen._confetti = UiKit.Confetti("Confetti", screen._confettiClip);
            root.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>
        /// The level's backdrop theme (null: the first theme): the win's garden is rendered now, at the level's start (the
        /// win's screen shares the picture).
        /// </summary>
        public void SetTheme(BackgroundTheme? theme)
        {
            _theme = theme;
            if (_backdrop == null)
            {
                _backdrop = BackdropView.Create(_rect, BackdropScene.Win);
            }

            _backdrop.Show(theme);
        }

        /// <param name="catalog">The cosmetic catalog, to draw a granted item's shape; null draws a generic star.</param>
        /// <param name="level">
        /// The won level: its main family celebrates (the owner's cheering hero when it exists, pictures.md A7) and its
        /// colors make the confetti; null shows the group and no confetti.
        /// </param>
        public void Show(MilestoneGrant grant, CosmeticCatalog? catalog, LevelDefinition? level = null)
        {
            if (_backdrop == null)
            {
                SetTheme(_theme);
            }

            _sign.Text = Loc.F("common.level", NumberText.Group(grant.Level));
            _celebration.ShowHero(level != null ? HeroPictures.MainFamily(level.Pods) : (Family?)null);
            _confettiClip.gameObject.SetActive(level != null);
            if (level != null)
            {
                _confetti.SetColors(ConfettiView.ColorsOf(level.Pods));
            }

            foreach (GameObject item in _items)
            {
                Destroy(item);
            }

            _items.Clear();
            var rewards = new List<(Image Icon, string Amount)>();
            if (grant.Item != null)
            {
                CosmeticItem? item = null;
                bool known = catalog != null && catalog.TryGet(grant.Item, out item);
                Image icon = UiFactory.CreateImage("Item", _row, known ? ProceduralSprites.Accessory(item!.Shape) : ProceduralSprites.Star, known ? BloomlingFigure.Tint(item!) : UiTheme.Of(C.MedalGold));
                icon.preserveAspect = true;
                rewards.Add((icon, known ? WardrobeScreen.Name(item!) : grant.Item));
            }

            if (grant.Petals > 0)
            {
                rewards.Add((UiKit.PetalIcon("Petals", _row), NumberText.Plus(grant.Petals)));
            }

            if (grant.Boosters != null)
            {
                foreach ((string id, int count) in new[] { ("extra_slot", grant.Boosters.ExtraSlot), ("shuffle", grant.Boosters.Shuffle), ("return", grant.Boosters.Return), ("bloom_burst", grant.Boosters.BloomBurst) })
                {
                    if (count > 0)
                    {
                        rewards.Add((UiKit.BoosterIcon(id, _row, id), "+" + count.ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }

            // Shown first, so the new pills' text engine is awake when the layout measures them.
            _root.SetActive(true);
            Layout(rewards);
            StartCoroutine(RiseIn());
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>The celebration's regions (<see cref="ScreenLayout.WinScreen"/>), in screen pixels.</summary>
        private void Layout(List<(Image Icon, string Amount)> rewards)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            var screen = new Box(0f, 0f, w, h);
            WinRegions r = ScreenLayout.WinScreen(w, h, insets);
            UiKit.PlaceBox((RectTransform)_sign.transform, r.Sign, screen);
            _celebration.Place(r, screen);

            // "Milestone reached!" on a cream pill with the medal, where the win's picture starts.
            float mh = 70f * u;
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float measured = KitText.Measure(UiKit.PillText(_reached), mh * 0.56f / ppu) * ppu;
            float markWidth = Mathf.Min(r.Picture.Width, (measured > 0f ? measured : mh * 0.3f * Loc.T("milestone.reached").Length) + (mh * 1.9f));
            Box mark = Box.FromCenter(r.Picture.CenterX, r.Picture.Top + (mh * 0.7f), markWidth, mh);
            UiKit.PlaceBox((RectTransform)_reached.transform, mark, screen);
            UiKit.PlaceBox(_medal, Box.FromCenter(mark.Left + (mh * 0.62f), mark.CenterY, mh * 0.8f, mh * 0.8f), mark);

            // Each reward on a cream tile with its amount in a pill, in the room between the mark and the hero's head.
            float room = r.Hero.Top - mark.Bottom;
            float cellHeight = Mathf.Min(room * 0.8f, 300f * u);
            var row = Box.FromCenter(r.Safe.CenterX, mark.Bottom + (room / 2f), r.Picture.Width, cellHeight);
            Box[] cells = ScreenLayout.Row(row, Mathf.Max(1, rewards.Count), 30f * u, Mathf.Min(240f * u, cellHeight / 1.25f), square: false);
            for (int i = 0; i < rewards.Count; i++)
            {
                (Image icon, string amount) = rewards[i];
                RectTransform tile = UiKit.RewardTile("Reward" + i.ToString(CultureInfo.InvariantCulture), _row, icon, amount);
                UiKit.PlaceBox(tile, cells[i], screen);
                _items.Add(tile.gameObject);
            }

            UiKit.PlaceBox((RectTransform)_continue.transform, r.Next, screen);
            UiKit.PlaceBox(_petals, r.Safe, screen);
            UiKit.PlaceBox(_confettiClip, new Box(0f, 0f, w, mark.Top), screen);
        }

        /// <summary>
        /// The rewards rise in a moment after the screen shows (motion.reward). It follows the win's celebration on the same
        /// garden, so it shows at once (the sign pops) instead of fading in over the gameplay.
        /// </summary>
        private IEnumerator RiseIn()
        {
            _rowFade.alpha = 0f;
            yield return new WaitForSecondsRealtime(0.15f);
            float seconds = DesignTokens.Motion.Reward.Seconds;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = FadeIn.Ease(t / seconds);
                _rowFade.alpha = k;
                _row.anchoredPosition = new Vector2(0f, -(1f - k) * UiKit.Units(30f));
                yield return null;
            }

            _rowFade.alpha = 1f;
            _row.anchoredPosition = Vector2.zero;
        }
    }
}
