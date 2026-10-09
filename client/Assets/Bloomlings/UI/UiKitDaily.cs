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
    /// step's Claim (<see cref="Claim"/>), its check (<see cref="Check"/>) or its padlock (<see cref="Lock"/>). Show one
    /// of the three and fade a later step with <see cref="Fade"/>.
    /// </summary>
    public sealed class DailyStepView
    {
        internal DailyStepView(RectTransform root, CanvasGroup fade, TextMeshProUGUI number, TextMeshProUGUI amount, Button claim, Image adGlyph, BoxLayout claimFace, RectTransform check, RectTransform lockBadge)
        {
            _claimFace = claimFace;
            Root = root;
            Fade = fade;
            Number = number;
            Amount = amount;
            Claim = claim;
            AdGlyph = adGlyph;
            Check = check;
            Lock = lockBadge;
        }

        private readonly BoxLayout _claimFace;

        public RectTransform Root { get; }

        /// <summary>The row's alpha (<see cref="DailyRewardCard.LockedAlpha"/> for a later step).</summary>
        public CanvasGroup Fade { get; }

        public TextMeshProUGUI Number { get; }

        public TextMeshProUGUI Amount { get; }

        /// <summary>The next step's green Claim, breathing; greyed when not interactable.</summary>
        public Button Claim { get; }

        /// <summary>The ad mark before an ad step's "Claim".</summary>
        public Image AdGlyph { get; }

        /// <summary>A claimed step's green check, scaled for its pop.</summary>
        public RectTransform Check { get; }

        /// <summary>A later step's padlock.</summary>
        public RectTransform Lock { get; }

        /// <summary>Whether the step's Claim shows the ad mark (an ad step); lay the row out again after a change.</summary>
        public bool Ad { get; set; }

        /// <summary>Shows the step: its number and Petals, one of its claim, check or padlock, faded when later.</summary>
        public void Show(int number, int petals, bool ad, bool claimed, bool ready)
        {
            Number.text = number.ToString(CultureInfo.InvariantCulture);
            Amount.text = NumberText.Plus(petals);
            Ad = ad;
            AdGlyph.gameObject.SetActive(ad);
            Claim.gameObject.SetActive(ready);
            Check.gameObject.SetActive(claimed);
            Lock.gameObject.SetActive(!claimed && !ready);
            Fade.alpha = !claimed && !ready ? DailyRewardCard.LockedAlpha : 1f;
            BoxLayout.On(Root).Apply();
            _claimFace.Apply();
        }
    }

    public static partial class UiKit
    {
        /// <summary>
        /// A Daily Reward step's row (spec 005 FR-050; <see cref="DailyStepView"/>): the raised slab (<see cref="RaisedRow"/>,
        /// corners 0.3 of its height), the number in <c>ink.brown_soft</c>, the lotus, "+N" in <c>type.reward</c>, and the
        /// claim box's Claim, check and padlock, each lifted off the slab's front side (<see cref="RaisedRowSide"/>).
        /// <paramref name="onClaim"/> runs on the Claim.
        /// </summary>
        public static DailyStepView DailyStep(string name, Transform parent, Action onClaim)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            CanvasGroup fade = root.gameObject.AddComponent<CanvasGroup>();
            RaisedRow(layout, root, 0.3f);
            TextMeshProUGUI number = KitLabel("Number", root, string.Empty, T.Count, TextLook.Plain(C.InkBrownSoft));
            Image lotus = PetalIcon("Lotus", root);
            TextMeshProUGUI amount = KitLabel("Amount", root, string.Empty, T.Reward, TextLook.Plain(C.InkBrown));
            DailyStepView? view = null;
            Button claim = DailyClaim("Claim", root, onClaim, () => view?.Ad ?? false, out Image adGlyph, out BoxLayout claimFace);

            (RectTransform check, BoxLayout checkLayout) = Element("Check", root);
            CheckBadge(checkLayout, b => Box.FromCenter(b.CenterX, b.CenterY, b.Width, b.Width));
            RectTransform lockBadge = LockBadge("Lock", root);

            float Lift(Box b) => b.Height * RaisedRowSide / 2f;
            layout.Add(lotus.rectTransform, b => DailyRewardCard.Row(b).Lotus.Offset(0f, -Lift(b)));
            layout.Add((RectTransform)claim.transform, b => DailyRewardCard.Row(b).Button);
            layout.Add(check, b => DailyRewardCard.Badge(DailyRewardCard.Row(b).Button.Offset(0f, -Lift(b))));
            layout.Add(lockBadge, b =>
            {
                Box badge = DailyRewardCard.Badge(DailyRewardCard.Row(b).Button.Offset(0f, -Lift(b)));
                return badge.Inset(-badge.Width * 0.08f);
            });
            layout.Watch(number).Watch(amount).Then(b =>
            {
                DailyRowParts parts = DailyRewardCard.Row(b);
                float lift = Lift(b);
                KitText.Place(number, T.Count, parts.Number.CenterX, parts.Number.CenterY - lift, b.Height * 0.42f, parts.Number.Width);
                float size = b.Height * 0.46f;
                float width = Mathf.Min(Mathf.Max(1f, KitText.Measure(amount, size)), parts.Amount.Width);
                KitText.Place(amount, T.Reward, parts.Amount.Left + (width / 2f), parts.Amount.CenterY - lift, size, width + 1f);
            });
            view = new DailyStepView(root, fade, number, amount, claim, adGlyph, claimFace, check, lockBadge);
            return view;
        }

        /// <summary>
        /// A Daily Reward step's Claim (the playtest's <c>MetaCards.StepButton</c>): the green face raised on its wooden plate
        /// (<see cref="RaisedButton"/>, glossy, as every primary button since spec 005 FR-045), breathing while it waits,
        /// "Claim" in the buttons' white letters at 0.62 of the face's height, after the ad mark while <paramref name="ad"/>
        /// (<paramref name="adGlyph"/>; lay <paramref name="face"/> out again when it changes); greyed when not interactable.
        /// </summary>
        public static Button DailyClaim(string name, Transform parent, Action onClick, Func<bool> ad, out Image adGlyph, out BoxLayout face)
        {
            TypeStyle s = T.Button;
            GardenButton view = RaisedButton(name, parent, GardenLook.Green, 0.5f, gloss: true);
            TextMeshProUGUI text = KitLabel("Label", view.Content, Loc.T("daily_reward.claim"), s, TextLook.OnGloss(GardenLook.Green));
            view.Track(text, s, TextLook.OnGloss);
            Image glyph = GardenGlyph(view, view.Content, "ui.ad");
            glyph.raycastTarget = false;
            face = BoxLayout.On(view.Content).Watch(text).Then(f =>
            {
                float size = f.Height * 0.62f;
                if (!ad())
                {
                    KitText.Place(text, s, f.CenterX, f.CenterY, size, f.Width - (f.Height * 0.6f));
                    return;
                }

                float icon = f.Height * 0.62f;
                float gap = f.Height * 0.12f;
                float room = Mathf.Max(1f, f.Width - icon - gap - (f.Height * 0.5f));
                float measured = KitText.Measure(text, size);
                float textWidth = Mathf.Min(measured > 0f ? measured : room, room);
                float start = f.CenterX - ((icon + gap + textWidth) / 2f);
                BoxLayout.Place(glyph.rectTransform, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon));
                KitText.Place(text, s, start + icon + gap + (textWidth / 2f), f.CenterY, size, textWidth + 1f);
            });
            view.Breathe = true;
            adGlyph = glyph;
            return Clickable(view, onClick);
        }
    }
}
