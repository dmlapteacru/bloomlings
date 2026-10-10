using System;
using System.Globalization;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// A Daily Reward step's row (<c>ui.daily.step</c>, spec 005 FR-050; the playtest's <c>MetaCards.Step</c>), laid out from its
    /// own box by <see cref="DailyRewardCard.Row"/>: the raised slab, the number, the lotus and "+N", and at its right the
    /// step's Claim (<see cref="Claim"/>: green and breathing on the next step, greyed and not active on a later one) or,
    /// once claimed, its check (<see cref="Check"/>).
    /// </summary>
    public sealed class DailyStepView
    {
        internal DailyStepView(RectTransform root, TextMeshProUGUI number, TextMeshProUGUI amount, Button claim, GardenButton claimView, RectTransform adGlyph, BoxLayout claimFace, RectTransform check)
        {
            _claimFace = claimFace;
            _claimView = claimView;
            Root = root;
            Number = number;
            Amount = amount;
            Claim = claim;
            AdGlyph = adGlyph;
            Check = check;
        }

        private readonly BoxLayout _claimFace;
        private readonly GardenButton _claimView;

        public RectTransform Root { get; }

        public TextMeshProUGUI Number { get; }

        public TextMeshProUGUI Amount { get; }

        /// <summary>The step's Claim: green and breathing on the next step, greyed when not interactable.</summary>
        public Button Claim { get; }

        /// <summary>The clapperboard before an ad step's "Claim" (<see cref="GardenLook.AdMark"/>).</summary>
        public RectTransform AdGlyph { get; }

        /// <summary>A claimed step's green check, scaled for its pop.</summary>
        public RectTransform Check { get; }

        /// <summary>Whether the step's Claim shows the clapperboard (an ad step); lay the row out again after a change.</summary>
        public bool Ad { get; set; }

        /// <summary>
        /// Shows the step: its number and Petals, and its Claim (active only when <paramref name="payable"/>, breathing when
        /// <paramref name="ready"/>) or, once claimed, its check.
        /// </summary>
        public void Show(int number, int petals, bool ad, bool claimed, bool ready, bool payable)
        {
            Number.text = number.ToString(CultureInfo.InvariantCulture);
            Amount.text = NumberText.Plus(petals);
            Ad = ad;
            AdGlyph.gameObject.SetActive(ad);
            AdGlyph.GetComponent<CanvasGroup>().alpha = ready && payable ? 1f : GardenLook.AdMarkDisabledAlpha;
            Claim.gameObject.SetActive(!claimed);
            Claim.interactable = ready && payable;
            _claimView.Breathe = ready && payable;
            Check.gameObject.SetActive(claimed);
            BoxLayout.On(Root).Apply();
            _claimFace.Apply();
        }
    }

    public static partial class UiKit
    {
        /// <summary>
        /// A Daily Reward step's row (spec 005 FR-050; <see cref="DailyStepView"/>): the raised slab (<see cref="RaisedRow"/>,
        /// corners 0.3 of its height), the number in <c>ink.brown_soft</c>, the lotus, "+N" in <c>type.reward</c>, and the
        /// claim box's Claim and check, lifted off the slab's front side (<see cref="RaisedRowSide"/>).
        /// <paramref name="onClaim"/> runs on the Claim.
        /// </summary>
        public static DailyStepView DailyStep(string name, Transform parent, Action onClaim)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            RaisedRow(layout, root, 0.3f);
            TextMeshProUGUI number = KitLabel("Number", root, string.Empty, T.Count, TextLook.Plain(C.InkBrownSoft));
            Image lotus = PetalIcon("Lotus", root);
            TextMeshProUGUI amount = KitLabel("Amount", root, string.Empty, T.Reward, TextLook.Plain(C.InkBrown));
            DailyStepView? view = null;
            Button claim = DailyClaim("Claim", root, onClaim, () => view?.Ad ?? false, out RectTransform adGlyph, out BoxLayout claimFace);
            GardenButton claimView = claim.GetComponent<GardenButton>();

            (RectTransform check, BoxLayout checkLayout) = Element("Check", root);
            CheckBadge(checkLayout, b => Box.FromCenter(b.CenterX, b.CenterY, b.Width, b.Width));

            float Lift(Box b) => b.Height * RaisedRowSide / 2f;
            layout.Add(lotus.rectTransform, b => DailyRewardCard.Row(b).Lotus.Offset(0f, -Lift(b)));
            layout.Add((RectTransform)claim.transform, b => DailyRewardCard.Row(b).Button);
            layout.Add(check, b => DailyRewardCard.Badge(DailyRewardCard.Row(b).Button.Offset(0f, -Lift(b))));
            layout.Watch(number).Watch(amount).Then(b =>
            {
                DailyRowParts parts = DailyRewardCard.Row(b);
                float lift = Lift(b);
                KitText.Place(number, T.Count, parts.Number.CenterX, parts.Number.CenterY - lift, b.Height * 0.42f, parts.Number.Width);
                float size = b.Height * 0.46f;
                float width = Mathf.Min(Mathf.Max(1f, KitText.Measure(amount, size)), parts.Amount.Width);
                KitText.Place(amount, T.Reward, parts.Amount.Left + (width / 2f), parts.Amount.CenterY - lift, size, width + 1f);
            });
            view = new DailyStepView(root, number, amount, claim, claimView, adGlyph, claimFace, check);
            return view;
        }

        /// <summary>
        /// A Daily Reward step's Claim (the playtest's <c>MetaCards.StepButton</c>): the green face raised on its wooden plate
        /// (<see cref="RaisedButton"/>, glossy, as every primary button since spec 005 FR-045), breathing while it waits,
        /// "Claim" in the buttons' white letters (<see cref="DailyRewardCard.ClaimTextShare"/> of the face's height), after the
        /// clapperboard's blue sticker (<see cref="AdMark"/>, <see cref="DailyRewardCard.ClaimIconShare"/>) while <paramref name="ad"/>
        /// (<paramref name="adGlyph"/>; lay <paramref name="face"/> out again when it changes); greyed when not interactable.
        /// </summary>
        public static Button DailyClaim(string name, Transform parent, Action onClick, Func<bool> ad, out RectTransform adGlyph, out BoxLayout face)
        {
            TypeStyle s = T.Button;
            GardenButton view = RaisedButton(name, parent, GardenLook.Green, 0.5f, gloss: true);
            TextMeshProUGUI text = KitLabel("Label", view.Content, Loc.T("daily_reward.claim"), s, TextLook.OnGloss(GardenLook.Green));
            view.Track(text, s, TextLook.OnGloss);
            RectTransform glyph = AdMark("Ad", view.Content);
            face = BoxLayout.On(view.Content).Watch(text).Then(f =>
            {
                float size = f.Height * DailyRewardCard.ClaimTextShare;
                if (!ad())
                {
                    KitText.Place(text, s, f.CenterX, f.CenterY, size, f.Width - (f.Height * 0.5f));
                    return;
                }

                float icon = f.Height * DailyRewardCard.ClaimIconShare;
                float gap = f.Height * DailyRewardCard.ClaimGapShare;
                float room = Mathf.Max(1f, f.Width - icon - gap - (f.Height * 0.4f));
                float measured = KitText.Measure(text, size);
                float textWidth = Mathf.Min(measured > 0f ? measured : room, room);
                float start = f.CenterX - ((icon + gap + textWidth) / 2f);
                BoxLayout.Place(glyph, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon));
                KitText.Place(text, s, start + icon + gap + (textWidth / 2f), f.CenterY, size, textWidth + 1f);
            });
            view.Breathe = true;
            adGlyph = glyph;
            return Clickable(view, onClick);
        }
    }
}
