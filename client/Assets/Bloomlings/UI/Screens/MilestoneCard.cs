using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The milestone card of the design board's frame 16 (spec 002 FR-021; spec 001 FR-061), shown after a milestone
    /// win's NEXT. It shows:
    /// <list type="bullet">
    /// <item><description>"LEVEL N" and "Milestone reached!";</description></item>
    /// <item><description>one icon with an amount per reward: the cosmetic item, the Petals, each booster;</description></item>
    /// <item><description>CONTINUE.</description></item>
    /// </list>
    /// </summary>
    public sealed class MilestoneCard : MonoBehaviour
    {
        private CardView _card = null!;
        private RectTransform _row = null!;

        public bool IsOpen => _card.Root.activeSelf;

        public static MilestoneCard Create(Transform parent, Action onContinue)
        {
            CardView card = UiKit.Card("MilestoneCard", parent, string.Empty, 60f + 300f + DesignTokens.Size.CardPrimaryHeight + 70f, null, DesignTokens.Type.TitleCaps);
            var screen = card.Root.AddComponent<MilestoneCard>();
            screen._card = card;
            TextMeshProUGUI reached = UiKit.Label("Reached", card.Body, Loc.T("milestone.reached"), DesignTokens.Type.Body, UiTheme.TextSecondary);
            UiFactory.Place(reached.rectTransform, 0f, 0.86f, 1f, 1f);
            screen._row = UiFactory.Place(UiFactory.CreateRect("Rewards", card.Body), 0f, 0.3f, 1f, 0.84f);
            // CONTINUE: the card's narrower main button, decorated and breathing while it waits (spec 003 FR-011a, FR-019).
            Button go = UiKit.PrimaryButton("Continue", card.Body, Loc.T("milestone.continue"), onContinue, decorate: true);
            go.GetComponent<GardenButton>().Breathe = true;
            UiFactory.Place((RectTransform)go.transform, 0.16f, 0.02f, 0.84f, 0.26f);
            card.Root.SetActive(false);
            return screen;
        }

        /// <param name="catalog">The cosmetic catalog, to draw a granted item's shape; null draws a generic badge.</param>
        public void Show(MilestoneGrant grant, CosmeticCatalog? catalog)
        {
            _card.Title.text = Loc.F("common.level", NumberText.Group(grant.Level));
            for (int i = _row.childCount - 1; i >= 0; i--)
            {
                Destroy(_row.GetChild(i).gameObject);
            }

            var items = new List<(Sprite Icon, Color Color, string Amount, bool Petal, bool Disc)>();
            if (grant.Item != null)
            {
                CosmeticItem? item = null;
                bool known = catalog != null && catalog.TryGet(grant.Item, out item);
                Sprite icon = known ? ProceduralSprites.Accessory(item!.Shape) : ProceduralSprites.Star;
                items.Add((icon, known ? BloomlingTint(item!) : UiTheme.Of(DesignTokens.Colors.MedalGold), known ? WardrobeScreen.Name(item!) : grant.Item, false, false));
            }

            if (grant.Petals > 0)
            {
                items.Add((ProceduralSprites.Petal, UiTheme.Petal, NumberText.Plus(grant.Petals), true, false));
            }

            if (grant.Boosters != null)
            {
                foreach ((string id, int count) in new[] { ("extra_slot", grant.Boosters.ExtraSlot), ("shuffle", grant.Boosters.Shuffle), ("return", grant.Boosters.Return), ("bloom_burst", grant.Boosters.BloomBurst) })
                {
                    if (count > 0)
                    {
                        items.Add((ProceduralSprites.Shape("booster." + id), UiTheme.Of(DesignTokens.BoosterColor(id)), "+" + count.ToString(CultureInfo.InvariantCulture), false, true));
                    }
                }
            }

            float width = items.Count == 0 ? 0f : 1f / items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                (Sprite icon, Color color, string amount, bool petal, bool disc) = items[i];
                RectTransform cell = UiFactory.Place(UiFactory.CreateRect("Reward " + i, _row), i * width, 0f, (i + 1) * width, 1f);
                if (disc)
                {
                    Image back = UiFactory.CreateImage("Disc", cell, ProceduralSprites.Circle, color);
                    back.preserveAspect = true;
                    UiFactory.Place(back.rectTransform, 0.15f, 0.36f, 0.85f, 1f);
                    UiKit.IconWithAmount(cell, "Item", icon, Color.white, amount, petal);
                }
                else
                {
                    UiKit.IconWithAmount(cell, "Item", icon, color, amount, petal);
                }
            }

            _card.Root.SetActive(true);
        }

        public void Hide() => _card.Root.SetActive(false);

        private static Color BloomlingTint(CosmeticItem item) =>
            ColorUtility.TryParseHtmlString(item.Tint, out Color c) ? c : UiTheme.Of(DesignTokens.Colors.MedalGold);
    }
}
