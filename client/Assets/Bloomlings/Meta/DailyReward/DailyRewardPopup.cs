using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.Meta.DailyReward
{
    /// <summary>
    /// The Daily Reward popup of the design board's frame 4 (spec 002 FR-022; FR-055, T134). It shows:
    /// <list type="bullet">
    /// <item><description>"Daily Rewards" on a wooden sign and "Day N";</description></item>
    /// <item><description>the reward basket heaped with lotuses in the win's soft turning light, and "+N" with the lotus
    /// on a cream pill;</description></item>
    /// <item><description>Claim, and the optional rewarded-ad bonus the player may start (FR-052).</description></item>
    /// </list>
    /// The bonus claims the reward together with its extra Petals, so it is earned at most once a day. The popup opens by
    /// itself once a day while a claim is due, and from Home's Daily scene at any time once unlocked (spec 005 FR-032;
    /// after the claim with Claim greyed). In the reference look of spec 005 (contracts/look.md §4.3, §4.6; the
    /// playtest's <c>MetaCards.DailyReward</c>).
    /// </summary>
    public sealed class DailyRewardPopup : MonoBehaviour
    {
        /// <summary>The lotuses heaped over the basket: center offsets and sizes in shares of the art's width.</summary>
        private static readonly (float X, float Y, float S)[] Heap = { (-0.22f, 0.02f, 0.3f), (0.2f, 0.0f, 0.32f), (0f, -0.12f, 0.34f), (-0.08f, 0.1f, 0.28f), (0.12f, 0.12f, 0.26f) };

        private CardView _card = null!;
        private TextMeshProUGUI _day = null!;
        private TextMeshProUGUI _amount = null!;
        private Button _claim = null!;
        private Button _bonus = null!;

        public static DailyRewardPopup Create(Transform parent)
        {
            float content = 60f + 330f + 110f + DesignTokens.Size.CardPrimaryHeight + DesignTokens.Size.SecondaryHeight + 90f;
            DailyRewardPopup popup = null!;
            CardView card = UiKit.Card("DailyReward", parent, Loc.T("daily_reward.title"), content, () => popup.Hide(), sign: SignDecor.None);
            popup = card.Root.AddComponent<DailyRewardPopup>();
            popup._card = card;

            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top;
            popup._day = UiKit.Label("Day", card.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(popup._day.rectTransform, Box.FromCenter(body.CenterX, y + (20f * u), body.Width, 56f * u), body);
            y += 60f * u;

            // The reward: a heap of lotuses over a woven basket, in the win's soft turning light (currency.petal_pile).
            var art = new Box(body.CenterX - (220f * u), y, body.CenterX + (220f * u), y + (330f * u));
            float rays = art.Width * 0.62f;
            LightRaysView light = UiKit.LightRays("Light", card.Body);
            UiKit.PlaceBox((RectTransform)light.transform, Box.FromCenter(art.CenterX, art.CenterY, rays * 2f, rays * 2f), body);
            foreach ((float hx, float hy, float hs) in Heap)
            {
                float size = art.Width * hs;
                Image lotus = UiKit.PetalIcon("Petal", card.Body);
                UiKit.PlaceBox(lotus.rectTransform, Box.FromCenter(art.CenterX + (hx * art.Width), art.CenterY + (hy * art.Height), size, size), body);
            }

            Image basket = Basket("Basket", card.Body);
            UiKit.PlaceBox(basket.rectTransform, Box.FromCenter(art.CenterX, art.CenterY + (art.Height * 0.2f), art.Width * 0.8f, art.Width * 0.6f), body);
            y = art.Bottom + (10f * u);

            popup._amount = RewardPill(card.Body, body, Box.FromCenter(body.CenterX, y + (50f * u), 250f * u, 92f * u));
            y += 110f * u;

            // Claim: narrower, decorated and breathing while it waits (spec 003 FR-011, FR-011a, FR-019).
            Box claim = ScreenLayout.CardButton(body, y + (10f * u), true, u);
            popup._claim = UiKit.PrimaryButton("Claim", card.Body, Loc.T("daily_reward.claim"), () => { }, decorate: true, breathe: true);
            UiKit.PlaceBox((RectTransform)popup._claim.transform, claim, body);
            Box bonus = ScreenLayout.CardButton(body, claim.Bottom + (24f * u), false, u).Inset(40f * u, 0f);
            popup._bonus = UiKit.SecondaryButton("Bonus", card.Body, Loc.T("daily_reward.watch"), () => { }, "ui.ad");
            UiKit.PlaceBox((RectTransform)popup._bonus.transform, bonus, body);
            card.Root.SetActive(false);
            return popup;
        }

        /// <param name="bonusAvailable">A rewarded ad is ready and today's bonus is unused.</param>
        /// <param name="watchBonus">Shows the ad; reports the Petals paid (claim and bonus), 0 when nothing was earned.</param>
        /// <param name="bonusPetals">The bonus amount for "Get +N"; 0 shows "Watch for more".</param>
        /// <param name="claimable">
        /// Today's reward can still be claimed. Home's Daily scene opens the card after the claim too (spec 005 FR-032):
        /// then it shows the day claimed with Claim greyed and no bonus.
        /// </param>
        public void Show(int petals, int streak, bool bonusAvailable, Func<int> claim, Action<Action<int>> watchBonus, int bonusPetals = 0, bool claimable = true)
        {
            _claim.interactable = claimable;
            bonusAvailable &= claimable;
            _day.text = Loc.F("daily_reward.day", streak);
            _amount.text = NumberText.Plus(petals);
            _claim.onClick.RemoveAllListeners();
            _claim.onClick.AddListener(() =>
            {
                claim();

                // A short sparkle burst where the reward was claimed (spec 003 FR-020); the Petals pill counts up.
                if (_card.Root.transform.parent is RectTransform overlay)
                {
                    Bloomlings.Client.Gameplay.Effects.UiFx.Puff(overlay, _claim.transform.position, UiTheme.Of(DesignTokens.Colors.PetalCenter), 8, UiKit.Units(140f), UiKit.Units(36f), 0.8f, ProceduralSprites.Shape("fx.sparkle"));
                }

                Hide();
            });
            _bonus.gameObject.SetActive(bonusAvailable);
            _bonus.GetComponentInChildren<TextMeshProUGUI>().text = bonusPetals > 0 ? Loc.F("daily_reward.bonus", bonusPetals) : Loc.T("daily_reward.watch");
            _bonus.onClick.RemoveAllListeners();
            _bonus.onClick.AddListener(() => watchBonus(total =>
            {
                if (total > 0)
                {
                    _bonus.gameObject.SetActive(false);
                    _amount.text = NumberText.Plus(total);
                    _claim.GetComponentInChildren<TextMeshProUGUI>().text = Loc.T("common.close");
                }
            }));
            _claim.GetComponentInChildren<TextMeshProUGUI>().text = Loc.T("daily_reward.claim");
            _card.Root.SetActive(true);
        }

        public void Hide() => _card.Root.SetActive(false);

        /// <summary>
        /// The reward basket (the playtest's <c>MetaCards.Basket</c>, <c>currency.reward_basket</c>): woven wood with
        /// darker weave lines and a lighter upper half, outlined in <c>wood.dark_line</c>, baked into one sprite and
        /// stretched over the rect like the playtest's shapes.
        /// </summary>
        private static Image Basket(string name, Transform parent)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get("currency.reward_basket");
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>
            {
                ((x, y) => sdf(x, y) - 0.05f, C.WoodDarkLine),
                (sdf, C.RewardBasket),
                ((x, y) => Math.Max(sdf(x, y) + 0.05f, Math.Min(Weave(y, 7f), Weave(x + (0.07f * (float)Math.Floor(y * 7f)), 9f))), C.WoodDarkLine.WithAlpha(0.35f)),
                // The light over the basket's upper half (shape units have y up).
                ((x, y) => Math.Max(sdf(x, y) + 0.04f, -y), C.RewardBasket.Lighten(0.25f)),
            };
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            PictureFit.On(image, (w, h) => ProceduralSprites.Baked("currency.reward_basket/woven", Mathf.Max(8, Mathf.Max(w, h)), layers), sliced: false, shape: PictureShape.Rect);
            return image;
        }

        /// <summary>Thin lines across <paramref name="v"/> every 1/<paramref name="count"/> (negative on a line).</summary>
        private static float Weave(float v, float count)
        {
            float t = (v * count) - (float)Math.Floor(v * count);
            return (0.42f - Math.Abs(t - 0.5f)) / count;
        }

        /// <summary>
        /// The reward "+N" on a cream pill with the lotus before it (the playtest's <c>MetaCards.RewardPill</c>, the cost
        /// pill's look, larger): a soft shadow, the <c>cream.lip</c> below, the cream face, a <c>cream.line</c> outline, and
        /// the lotus and the amount centered as a group. Returns the amount's label.
        /// </summary>
        private static TextMeshProUGUI RewardPill(RectTransform parent, Box parentBox, Box box)
        {
            RectTransform root = UiKit.PlaceBox(UiFactory.CreateRect("Reward", parent), box, parentBox);
            BoxLayout layout = BoxLayout.On(root);
            UiKit.SoftShadow(layout, b => b, b => b.Height / 2f, 0.2f, 0.1f);
            Image lip = UiKit.RoundRect("Lip", root, UiTheme.Of(C.CreamLip));
            Image face = UiKit.RoundGradient("Face", root, C.CreamTop, C.ParchmentBottom);
            Image line = UiKit.RoundRing("Line", root, UiTheme.Of(C.CreamLine), null, b => Mathf.Max(UiKit.Units(2f), b.Height * 0.04f));
            TextMeshProUGUI amount = UiKit.KitLabel("Amount", root, string.Empty, T.Reward, TextLook.Plain(C.InkBrown));
            Image lotus = UiKit.PetalIcon("Lotus", root);
            layout.Add(lip.rectTransform, b => b.Offset(0f, b.Height * 0.07f));
            layout.Add(face.rectTransform, b => b);
            layout.Add(line.rectTransform, b => b);
            layout.Watch(amount).Then(b =>
            {
                float h = b.Height;
                float icon = h * 0.92f;
                float gap = h * 0.12f;
                float size = h * 0.64f;
                float measured = KitText.Measure(amount, size);
                float room = Mathf.Max(1f, b.Width - icon - gap - (h * 0.5f));
                float textWidth = Mathf.Min(measured > 0f ? measured : room, room);
                float start = b.CenterX - ((icon + gap + textWidth) / 2f);
                // The lotus first, then the amount, as on the win's reward pill and every cost pill.
                BoxLayout.Place(lotus.rectTransform, Box.FromCenter(start + (icon / 2f), b.CenterY - (h * 0.02f), icon, icon));
                KitText.Place(amount, T.Reward, start + icon + gap + (textWidth / 2f), b.CenterY, size, textWidth + 1f);
            });
            return amount;
        }
    }
}
