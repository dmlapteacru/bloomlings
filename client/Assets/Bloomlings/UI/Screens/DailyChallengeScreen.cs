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
    /// <summary>What the Daily Challenge card shows.</summary>
    public sealed record DailyChallengeModel(string UtcDate, bool CompletedToday, int RewardPetals);

    /// <summary>
    /// The Daily Challenge card (FR-064, T144): today's puzzle, the same for every player, with its own reward. Playing
    /// it never changes Level N; Home's Play button always continues the main sequence. In the reference look of spec 005
    /// (contracts/look.md §3.5, §4.5): parchment under a wooden sign, the sun on a cream disc as on Home's Daily Challenge
    /// card, the date in soft brown, the text in brown over at most two lines, the green Play in its wooden rim and the
    /// cream round close.
    /// </summary>
    public sealed class DailyChallengeScreen : MonoBehaviour
    {
        private const float SunUnits = 150f;
        private const float LineUnits = 56f;

        private GameObject _root = null!;
        private TextMeshProUGUI _date = null!;
        private TextMeshProUGUI _probe = null!;
        private TextMeshProUGUI[] _body = Array.Empty<TextMeshProUGUI>();
        private float _bodyWidth;
        private Button _play = null!;

        public bool IsOpen => _root.activeSelf;

        public static DailyChallengeScreen Create(Transform parent, Action onPlay)
        {
            float content = 10f + SunUnits + 16f + LineUnits + 12f + (2f * LineUnits) + 34f + DesignTokens.Size.CardPrimaryHeight + 40f;
            DailyChallengeScreen screen = null!;
            CardView card = UiKit.Card("DailyChallenge", parent, Loc.T("daily.title"), content, () => screen.Hide(), sign: SignDecor.None);
            screen = card.Root.AddComponent<DailyChallengeScreen>();
            screen._root = card.Root;
            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top + (10f * u);

            // The sun on a cream face raised on its wooden plate (ui.sun; spec 005 FR-047), as Home's Daily Challenge button.
            GardenButton disc = UiKit.RaisedButton("Sun", card.Body, GardenLook.White, GardenLook.IconRadiusShare, raycast: false, square: true);
            UiKit.PlaceBox((RectTransform)disc.transform, Box.FromCenter(body.CenterX, y + (SunUnits * u / 2f), SunUnits * u, SunUnits * u), body);
            Image sun = UiKit.OutlinedIcon("Glyph", disc.Content, "ui.sun", C.GardenFlowerCenter, C.GardenFlowerCenterLine);
            BoxLayout.On(disc.Content).Add(sun.rectTransform, f => f.Inset(-f.Width * 0.06f));
            y += (SunUnits + 16f) * u;

            screen._date = UiKit.Label("Date", card.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrownSoft));
            UiKit.PlaceBox(screen._date.rectTransform, new Box(body.Left, y, body.Right, y + (LineUnits * u)), body);
            y += (LineUnits + 12f) * u;

            screen._probe = UiKit.Label("Probe", card.Body, string.Empty, T.Body, Color.clear);
            screen._bodyWidth = body.Width * 0.94f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            screen._body = new TextMeshProUGUI[2];
            for (int i = 0; i < 2; i++)
            {
                TextMeshProUGUI line = UiKit.Label("Body" + i, card.Body, string.Empty, T.Body, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
                UiKit.PlaceBox(line.rectTransform, new Box(body.Left, y, body.Right, y + (LineUnits * u)), body);
                screen._body[i] = line;
                y += LineUnits * u;
            }

            y += 34f * u;
            screen._play = UiKit.PrimaryButton("Play", card.Body, Loc.T("common.play"), () =>
            {
                screen.Hide();
                onPlay();
            }, breathe: true);
            UiKit.PlaceBox((RectTransform)screen._play.transform, ScreenLayout.CardButton(body, y, true, u), body);
            card.Root.SetActive(false);
            return screen;
        }

        public void Show(DailyChallengeModel model)
        {
            _date.text = Loc.F("daily.date", model.UtcDate);
            string text = model.CompletedToday ? Loc.T("daily.done") : Loc.F("daily.body", model.RewardPetals);
            _root.SetActive(true);
            List<string> lines = UiKit.BalancedLines(_probe, text, UiKit.Units(T.Body.Size), _bodyWidth);
            _body[0].text = lines[0];
            _body[1].text = lines.Count > 1 ? lines[1] : string.Empty;
            _play.GetComponentInChildren<TextMeshProUGUI>().text = model.CompletedToday ? Loc.T("daily.play_again") : Loc.T("common.play");
        }

        public void Hide() => _root.SetActive(false);
    }
}
