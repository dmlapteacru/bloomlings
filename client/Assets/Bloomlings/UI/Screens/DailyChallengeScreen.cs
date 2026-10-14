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
        private const float SunUnits = 260f;
        private const float LineUnits = 56f;

        private GameObject _root = null!;
        private TextMeshProUGUI _probe = null!;
        private TextMeshProUGUI[] _body = Array.Empty<TextMeshProUGUI>();
        private float _bodyWidth;
        private TextMeshProUGUI _reward = null!;
        private GameObject _done = null!;
        private Button _play = null!;

        public bool IsOpen => _root.activeSelf;

        public static DailyChallengeScreen Create(Transform parent, Action onPlay)
        {
            float content = 10f + SunUnits + 24f + (2f * LineUnits) + 22f + 72f + 26f + DesignTokens.Size.CardPrimaryHeight + 40f;
            DailyChallengeScreen screen = null!;
            CardView card = UiKit.Card("DailyChallenge", parent, Loc.T("daily.title"), content, () => screen.Hide(), sign: SignDecor.None);
            screen = card.Root.AddComponent<DailyChallengeScreen>();
            screen._root = card.Root;
            Box body = card.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            float y = body.Top + (10f * u);

            Image puzzle = UiKit.ChallengeObject("PuzzleBoard", card.Body);
            UiKit.PlaceBox(puzzle.rectTransform, Box.FromCenter(body.CenterX, y + SunUnits * u / 2f, SunUnits * u, SunUnits * u), body);
            screen._done = UiKit.CornerCheck("Completed", puzzle.transform);
            y += (SunUnits + 24f) * u;

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

            y += 22f * u;
            Box reward = Box.FromCenter(body.CenterX, y + 36f * u, 340f * u, 72f * u);
            Image lotus = UiKit.PetalIcon("RewardLotus", card.Body);
            UiKit.PlaceBox(lotus.rectTransform, new Box(reward.Left, reward.Top, reward.Left + 72f * u, reward.Bottom), body);
            screen._reward = UiKit.Label("Reward", card.Body, string.Empty, PopupDesign.Label, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            UiKit.PlaceBox(screen._reward.rectTransform, new Box(reward.Left + 80f * u, reward.Top, reward.Right, reward.Bottom), body);
            y += (72f + 26f) * u;
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
            _done.SetActive(model.CompletedToday);
            _reward.text = model.CompletedToday ? Loc.T("daily.reward_collected") : Loc.F("daily.reward", model.RewardPetals);
            string text = model.CompletedToday ? Loc.T("daily.done") : Loc.T("daily.description");
            _root.SetActive(true);
            List<string> lines = UiKit.BalancedLines(_probe, text, UiKit.Units(T.Body.Size), _bodyWidth);
            _body[0].text = lines[0];
            _body[1].text = lines.Count > 1 ? lines[1] : string.Empty;
            _play.GetComponentInChildren<TextMeshProUGUI>().text = model.CompletedToday ? Loc.T("daily.play_again") : Loc.T("common.play");
        }

        public void Hide() => _root.SetActive(false);
    }
}
