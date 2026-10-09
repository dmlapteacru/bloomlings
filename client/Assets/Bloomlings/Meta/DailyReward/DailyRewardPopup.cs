using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// The Daily Reward popup of the design board's frame 4 (spec 002 FR-022; spec 001 FR-055 as amended on 2026-10-09,
    /// T134; spec 005 FR-050, <see cref="DailyRewardCard"/>) in the popups' look (contracts/look.md §6.23; the playtest's
    /// <c>MetaCards.DailyReward</c>):
    /// <list type="bullet">
    /// <item><description>"Daily rewards" on the wooden sign, the reward basket heaped with lotuses in the win's soft turning
    /// light;</description></item>
    /// <item><description>the day's five steps as raised rows (<see cref="UiKit.DailyStep"/>): the next step's breathing
    /// green Claim (an ad step's with the ad mark), a claimed step's check, a later step's padlock, faded;</description></item>
    /// <item><description>under them, when the steps come again (midnight UTC).</description></item>
    /// </list>
    /// A claim keeps the card open: the row's check pops in and "+N" rises from it. Home's Daily scene opens it at any time
    /// once unlocked; it no longer opens by itself (spec 005 FR-050: the scene's "!" calls for it instead).
    /// </summary>
    public sealed class DailyRewardPopup : MonoBehaviour
    {
        private readonly DailyStepView[] _steps = new DailyStepView[DailyRewardService.StepCount];
        private CardView _card = null!;
        private TextMeshProUGUI _caption = null!;
        private TextMeshProUGUI _rise = null!;
        private Box _body;
        private DailyRewardRegions _regions = null!;
        private DailyRewardService? _daily;
        private Action<DailyRewardStep, Action<bool>>? _claim;
        private Func<bool>? _adReady;
        private int _claimedStep;
        private float _claimedAt = -10f;
        private float _nextCaption;

        public static DailyRewardPopup Create(Transform parent)
        {
            DailyRewardPopup popup = null!;
            CardView card = UiKit.Card("DailyReward", parent, Loc.T("daily_reward.title"), DailyRewardCard.ContentUnits(DailyRewardService.StepCount), () => popup.Hide(), sign: SignDecor.None);
            popup = card.Root.AddComponent<DailyRewardPopup>();
            popup._card = card;

            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            DailyRewardRegions d = DailyRewardCard.Layout(body, u, DailyRewardService.StepCount);
            popup._body = body;
            popup._regions = d;

            // The reward: a heap of lotuses over a woven basket, in the win's soft turning light (currency.petal_pile).
            Box art = d.Art;
            float rays = art.Width * 0.62f;
            LightRaysView light = UiKit.LightRays("Light", card.Body);
            UiKit.PlaceBox((RectTransform)light.transform, Box.FromCenter(art.CenterX, art.CenterY, rays * 2f, rays * 2f), body);
            foreach ((float hx, float hy, float hs) in DailyRewardCard.Heap)
            {
                float size = art.Width * hs;
                Image lotus = UiKit.PetalIcon("Petal", card.Body);
                UiKit.PlaceBox(lotus.rectTransform, Box.FromCenter(art.CenterX + (hx * art.Width), art.CenterY + (hy * art.Height), size, size), body);
            }

            Image basket = Basket("Basket", card.Body);
            UiKit.PlaceBox(basket.rectTransform, d.Basket, body);

            for (int i = 0; i < DailyRewardService.StepCount; i++)
            {
                int index = i;
                DailyStepView step = UiKit.DailyStep("Step" + (i + 1).ToString(CultureInfo.InvariantCulture), card.Body, () => popup.ClaimAt(index));
                UiKit.PlaceBox(step.Root, d.Rows[i], body);
                popup._steps[i] = step;
            }

            popup._caption = UiKit.Label("Caption", card.Body, string.Empty, T.Caption, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(popup._caption.rectTransform, d.Caption, body);

            // The claimed "+N" rising from where it was claimed, over the rows.
            popup._rise = UiKit.KitLabel("Rise", card.Body, string.Empty, T.Reward, TextLook.OnGloss(GardenLook.Green));
            popup._rise.raycastTarget = false;
            popup._rise.gameObject.SetActive(false);
            card.Root.SetActive(false);
            return popup;
        }

        /// <param name="daily">The steps and their states.</param>
        /// <param name="adReady">Whether a rewarded ad can show now; without one and without the ad's stand-in
        /// (<see cref="DailyRewardService.AdStub"/>) an ad step's Claim is greyed.</param>
        /// <param name="claim">Claims a step (an ad step after its rewarded ad, or its stand-in) and calls back with whether it paid.</param>
        public void Show(DailyRewardService daily, Func<bool> adReady, Action<DailyRewardStep, Action<bool>> claim)
        {
            _daily = daily;
            _adReady = adReady;
            _claim = claim;
            _claimedStep = 0;
            _rise.gameObject.SetActive(false);
            Refresh();
            _card.Root.SetActive(true);
        }

        public void Hide() => _card.Root.SetActive(false);

        private void Refresh()
        {
            if (_daily == null)
            {
                return;
            }

            IReadOnlyList<DailyRewardStep> steps = _daily.Steps;
            for (int i = 0; i < _steps.Length && i < steps.Count; i++)
            {
                DailyRewardStep step = steps[i];
                DailyStepState state = _daily.StateOf(step.Number);
                _steps[i].Show(step.Number, step.Petals, step.Ad, state == DailyStepState.Claimed, state == DailyStepState.Ready);
                _steps[i].Claim.interactable = !step.Ad || DailyRewardService.AdStub || (_adReady?.Invoke() ?? false);
            }

            UpdateCaption();
        }

        private void ClaimAt(int index)
        {
            if (_daily == null || _claim == null)
            {
                return;
            }

            DailyRewardStep step = _daily.Steps[index];
            _claim(step, paid =>
            {
                if (!paid)
                {
                    return;
                }

                // A short sparkle burst where the step was claimed (spec 003 FR-020); the row's check pops in, "+N" rises.
                if (_card.Root.transform.parent is RectTransform overlay)
                {
                    Bloomlings.Client.Gameplay.Effects.UiFx.Puff(overlay, _steps[index].Claim.transform.position, UiTheme.Of(DesignTokens.Colors.PetalCenter), 8, UiKit.Units(140f), UiKit.Units(36f), 0.8f, ProceduralSprites.Shape("fx.sparkle"));
                }

                _claimedStep = step.Number;
                _claimedAt = Time.unscaledTime;
                _rise.text = NumberText.Plus(step.Petals);
                _rise.gameObject.SetActive(true);
                _rise.transform.SetAsLastSibling();
                Refresh();
            });
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextCaption)
            {
                UpdateCaption();
            }

            if (_claimedStep <= 0)
            {
                return;
            }

            float since = Time.unscaledTime - _claimedAt;
            DailyStepView row = _steps[_claimedStep - 1];
            row.Check.localScale = Vector3.one * DailyRewardCard.CheckPop(since);
            if (DailyRewardCard.Rise(since) is (float rise, float alpha))
            {
                Box claim = DailyRewardCard.Row(_regions.Rows[_claimedStep - 1]).Button;
                float h = _regions.Rows[_claimedStep - 1].Height;
                Box local = UiKit.ToLocal(Box.FromCenter(claim.CenterX, claim.CenterY - (rise * h), claim.Width * 1.4f, h), _body);
                KitText.Place(_rise, T.Reward, local.CenterX, local.CenterY, h * 0.5f, claim.Width * 1.4f);
                _rise.alpha = alpha;
            }
            else
            {
                _rise.gameObject.SetActive(false);
            }
        }

        /// <summary>The caption under the rows, once a second: when the steps come again.</summary>
        private void UpdateCaption()
        {
            _nextCaption = Time.unscaledTime + 1f;
            if (_daily == null)
            {
                return;
            }

            (string key, object[] args) = DailyRewardCard.Duration(_daily.MinutesToNextDay);
            _caption.text = Loc.F(DailyRewardCard.CaptionKey(_daily.CanClaim), Loc.F(key, args));
        }

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
    }
}
