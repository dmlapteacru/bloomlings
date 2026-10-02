using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
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
    /// The milestone card of the design board's frame 16 (spec 002 FR-021; spec 001 FR-061), shown after a milestone
    /// win's Next, in the win's language (spec 005 contracts/look.md §4.4; the playtest's <c>EndCards.Milestone</c>):
    /// <list type="bullet">
    /// <item><description>the wooden sign with flower clusters, "Level N", across the card's top edge, the heroes on a
    /// stone pedestal in light rays above it and petals falling;</description></item>
    /// <item><description>"Milestone reached!";</description></item>
    /// <item><description>each reward (the cosmetic item, the Petals, each booster) as its icon on a cream tile with
    /// its amount in a cream pill over the tile's bottom edge, rising in;</description></item>
    /// <item><description>Continue, decorated and breathing.</description></item>
    /// </list>
    /// </summary>
    public sealed class MilestoneCard : MonoBehaviour
    {
        private const float RowUnits = 270f;
        private const float SignUnits = 146f;

        private readonly List<GameObject> _items = new List<GameObject>();
        private GameObject _root = null!;
        private RectTransform _card = null!;
        private CelebrationView _celebration = null!;
        private WoodSignView _sign = null!;
        private TextMeshProUGUI _reached = null!;
        private RectTransform _row = null!;
        private CanvasGroup _rowFade = null!;
        private Button _continue = null!;
        private RectTransform _petals = null!;

        public bool IsOpen => _root.activeSelf;

        public static MilestoneCard Create(Transform parent, Action onContinue)
        {
            Image shade = UiFactory.CreateImage("MilestoneCard", parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<MilestoneCard>();
            screen._root = shade.gameObject;
            Image card = UiKit.Paper("Card", shade.transform, b => Mathf.Max(UiKit.Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            card.gameObject.AddComponent<PopMotion>();
            screen._card = card.rectTransform;
            screen._celebration = HeroPictures.Celebration(card.rectTransform);
            screen._sign = UiKit.WoodSign("Title", card.transform, string.Empty, T.LevelHome, SignDecor.Flowers);
            screen._reached = UiKit.Label("Reached", card.transform, Loc.T("milestone.reached"), T.ButtonSecondary, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));
            screen._row = UiFactory.Stretch(UiFactory.CreateRect("Rewards", card.transform));
            screen._rowFade = screen._row.gameObject.AddComponent<CanvasGroup>();
            screen._rowFade.blocksRaycasts = false;

            // Continue: the card's main button, decorated and breathing while it waits (spec 003 FR-011a, FR-019).
            screen._continue = UiKit.PrimaryButton("Continue", card.transform, Loc.T("milestone.continue"), onContinue, decorate: true, breathe: true);
            screen._petals = (RectTransform)UiKit.FallingPetals("Petals", shade.transform).transform;
            UiKit.FadeInOnShow(screen._petals.gameObject, 1f, 0.5f);
            shade.gameObject.SetActive(false);
            return screen;
        }

        /// <param name="catalog">The cosmetic catalog, to draw a granted item's shape; null draws a generic star.</param>
        public void Show(MilestoneGrant grant, CosmeticCatalog? catalog)
        {
            _sign.Text = Loc.F("common.level", NumberText.Group(grant.Level));
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

        /// <summary>The card's regions as the playtest's <c>EndCards.Milestone</c> computes them, in screen pixels.</summary>
        private void Layout(List<(Image Icon, string Amount)> rewards)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            Box safe = ScreenLayout.SafeArea(w, h, insets);
            CardRegions r = ScreenLayout.Card(w, h, insets, 64f + RowUnits + 50f + DesignTokens.Size.CardPrimaryHeight + 40f);
            Box card = r.Card;
            UiKit.PlaceScreen(_card, card);

            float sh = SignUnits * u;
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float signWidth = Mathf.Min(card.Width * 0.8f, (KitText.Measure(_sign.Label, T.LevelHome.Size * u / ppu) * ppu) + (sh * 1.5f));
            Box sign = Box.FromCenter(card.CenterX, card.Top + (30f * u), signWidth, sh);
            UiKit.PlaceBox((RectTransform)_sign.transform, sign, card);
            _celebration.Place(new Box(card.Left, safe.Top + (12f * u), card.Right, sign.Top + (sh * 0.3f)), card, w, u);
            UiKit.PlaceBox(_reached.rectTransform, Box.FromCenter(r.Body.CenterX, r.Body.Top + (30f * u), r.Body.Width, 64f * u), card);

            // Each reward: its icon on a cream tile, the amount in a cream pill over the tile's bottom edge.
            var row = new Box(r.Body.Left, r.Body.Top + (84f * u), r.Body.Right, r.Body.Top + ((84f + RowUnits) * u));
            Box[] cells = ScreenLayout.Row(row, Mathf.Max(1, rewards.Count), 36f * u, 230f * u, square: false);
            for (int i = 0; i < rewards.Count; i++)
            {
                (Image icon, string amount) = rewards[i];
                Box cell = cells[i];
                float tile = Mathf.Min(cell.Width * 0.86f, 190f * u);
                Box tileBox = Box.FromCenter(cell.CenterX, cell.Top + (tile / 2f), tile, tile);
                GardenButton face = UiKit.IconFace("Tile" + i, _row, GardenLook.White, b => b.Height * 0.26f, square: true);
                UiKit.PlaceBox((RectTransform)face.transform, tileBox, card);
                icon.transform.SetParent(face.Content, false);
                icon.raycastTarget = false;
                UiFactory.Place(icon.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);

                float pillHeight = tile * 0.36f;
                CostPillView pill = UiKit.TextPill("Amount" + i, _row, CostKind.Charges, amount);
                TextMeshProUGUI label = UiKit.PillText(pill);
                float measured = KitText.Measure(label, pillHeight * 0.56f / ppu) * ppu;
                float pillWidth = Mathf.Min(cell.Width, Mathf.Max(tile * 0.9f, measured + (pillHeight * 1.1f)));
                UiKit.PlaceBox((RectTransform)pill.transform, Box.FromCenter(tileBox.CenterX, tileBox.Bottom + (pillHeight * 0.2f), pillWidth, pillHeight), card);
                _items.Add(face.gameObject);
                _items.Add(pill.gameObject);
            }

            Box go = ScreenLayout.CardButton(r.Body, r.Body.Bottom - (DesignTokens.Size.CardPrimaryHeight * u) - (20f * u), true, u);
            UiKit.PlaceBox((RectTransform)_continue.transform, go, card);
            UiKit.PlaceScreen(_petals, new Box(safe.Left, safe.Top, safe.Right, Mathf.Min(card.Bottom, sign.Bottom + (260f * u))));
        }

        /// <summary>The rewards rise in a moment after the card (motion.reward).</summary>
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
