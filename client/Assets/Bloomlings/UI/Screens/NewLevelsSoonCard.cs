using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// "New levels soon" (spec 006 FR-060, T060), the playtest's <c>EndCards.NewLevelsSoon</c>: after the last round level
    /// no level follows yet, so Home shows this card over it when Play (or the last level's Next) finds none. A popup card
    /// with the title on its wooden sign, the text in brown over at most three lines ("You have played all N levels …"), and
    /// OK, which closes it, as the close does.
    /// </summary>
    public sealed class NewLevelsSoonCard : MonoBehaviour
    {
        private const float TopUnits = 24f;
        private const float LineUnits = 58f;
        private const float GapUnits = 30f;
        private const float BottomUnits = 36f;
        private const int RoomLines = 3;

        private GameObject _root = null!;
        private TextMeshProUGUI _probe = null!;
        private TextMeshProUGUI[] _body = Array.Empty<TextMeshProUGUI>();
        private TextMeshProUGUI _single = null!;
        private float _bodyWidth;

        public bool IsOpen => _root.activeSelf;

        public static NewLevelsSoonCard Create(Transform parent)
        {
            float content = TopUnits + (RoomLines * LineUnits) + GapUnits + DesignTokens.Size.CardPrimaryHeight + BottomUnits;
            NewLevelsSoonCard screen = null!;
            CardView card = UiKit.Card("NewLevelsSoon", parent, Loc.T("round.soon.title"), content, () => screen.Hide(), sign: SignDecor.None);
            screen = card.Root.AddComponent<NewLevelsSoonCard>();
            screen._root = card.Root;
            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top + (TopUnits * u);

            screen._probe = UiKit.Label("Probe", card.Body, string.Empty, T.Body, Color.clear);
            screen._bodyWidth = body.Width * 0.94f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            // The text takes one or two balanced lines in the middle of a room of three (UiKit.BalancedLines).
            TextMeshProUGUI Line(string name, float top)
            {
                TextMeshProUGUI line = UiKit.Label(name, card.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                UiKit.PlaceBox(line.rectTransform, new Box(body.Left, top, body.Right, top + (LineUnits * u)), body);
                return line;
            }

            screen._body = new[] { Line("Body0", y + (LineUnits * u / 2f)), Line("Body1", y + (LineUnits * u * 1.5f)) };
            screen._single = Line("Single", y + (LineUnits * u));
            y += ((RoomLines * LineUnits) + GapUnits) * u;
            Button ok = UiKit.PrimaryButton("Ok", card.Body, Loc.T("common.ok"), () => screen.Hide());
            UiKit.PlaceBox((RectTransform)ok.transform, ScreenLayout.CardButton(body, y, true, u), body);
            card.Root.SetActive(false);
            return screen;
        }

        /// <summary>Shows the card for a game of <paramref name="levelCount"/> round levels, all played.</summary>
        public void Show(int levelCount)
        {
            _root.SetActive(true);
            List<string> lines = UiKit.BalancedLines(_probe, Loc.F("round.soon.body", levelCount), UiKit.Units(T.Body.Size), _bodyWidth);
            bool one = lines.Count < 2;
            _single.text = one && lines.Count > 0 ? lines[0] : string.Empty;
            _body[0].text = one ? string.Empty : lines[0];
            _body[1].text = one ? string.Empty : lines[1];
        }

        public void Hide() => _root.SetActive(false);
    }
}
