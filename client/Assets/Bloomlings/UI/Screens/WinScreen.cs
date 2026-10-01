using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The win card of the design board's frame 15 (spec 002 FR-020; FR-025, T050, T122).
    /// <list type="bullet">
    /// <item><description>The finished picture is revealed on the board first.</description></item>
    /// <item><description>Then this card rises below it: the Petals earned with the Petal symbol (and any booster
    /// drop), NEXT as the primary button, and the optional "×2 reward" rewarded ad.</description></item>
    /// <item><description>A milestone level shows its milestone card after NEXT (<see cref="MilestoneCard"/>).</description></item>
    /// </list>
    /// </summary>
    public sealed class WinScreen : MonoBehaviour
    {
        private const float RevealSeconds = 1.2f;

        private GameObject _root = null!;
        private TextMeshProUGUI _reward = null!;
        private Button _double = null!;
        private GameObject _milestoneMark = null!;
        private CountUp _count = null!;
        private RectTransform _petal = null!;

        public static WinScreen Create(Transform parent, Action onNext)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            Box safe = ScreenLayout.SafeArea(w, h, insets);
            var box = new Box(safe.CenterX - (Mathf.Min(safe.Width * 0.86f, 920f * u) / 2f), safe.Bottom - (40f * u) - (620f * u), safe.CenterX + (Mathf.Min(safe.Width * 0.86f, 920f * u) / 2f), safe.Bottom - (40f * u));
            // Paper in a wooden frame with a green header band (spec 003 FR-015).
            Image panel = UiKit.Paper("WinScreen", parent, 56f, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            UiKit.PlaceScreen(panel.rectTransform, box);
            var screen = panel.gameObject.AddComponent<WinScreen>();
            screen._root = panel.gameObject;
            GardenButton band = UiKit.Garden("Header", panel.transform, GardenLook.Green, 108f, radiusUnits: 40f, raycast: false, plate: false);
            UiFactory.Place((RectTransform)band.transform, 0.02f, 0.8f, 0.98f, 0.97f);
            TextMeshProUGUI title = UiKit.Label("Title", band.Content, Loc.T("win.title"), DesignTokens.Type.Title, UiTheme.TextOnColor, look: TextLook.OnColor(GardenLook.Green));
            UiFactory.Place(title.rectTransform, 0.06f, 0f, 0.94f, 1f);

            screen._reward = UiKit.Label("Reward", panel.transform, string.Empty, DesignTokens.Type.Reward, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain), look: TextLook.Plain(DesignTokens.Colors.GardenLabelPlain));
            UiFactory.Place(screen._reward.rectTransform, 0.06f, 0.56f, 0.8f, 0.78f);
            screen._count = CountUp.On(screen._reward, n => n.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Image petal = UiKit.PetalIcon("Petal", panel.transform);
            UiFactory.Place(petal.rectTransform, 0.8f, 0.58f, 0.92f, 0.76f);
            screen._petal = petal.rectTransform;

            // NEXT is narrower and carries the leaves and flowers (spec 003 FR-011, FR-011a).
            Button next = UiKit.PrimaryButton("Next", panel.transform, Loc.T("common.next"), onNext, decorate: true);
            UiFactory.Place((RectTransform)next.transform, 0.16f, 0.28f, 0.84f, 0.5f);
            screen._double = UiKit.SecondaryButton("Double", panel.transform, Loc.T("win.double"), () => { }, "ui.ad");
            UiFactory.Place((RectTransform)screen._double.transform, 0.22f, 0.06f, 0.78f, 0.22f);

            Image mark = UiKit.Pill("Milestone", panel.transform, UiTheme.Of(DesignTokens.Colors.MedalGold));
            UiFactory.Place(mark.rectTransform, 0.3f, 0.96f, 0.7f, 1.06f);
            TextMeshProUGUI markText = UiKit.Label("Text", mark.transform, Loc.T("milestone.reached"), DesignTokens.Type.Badge, UiTheme.Text);
            UiFactory.Place(markText.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
            screen._milestoneMark = mark.gameObject;
            panel.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>Waits for the picture reveal, then shows the card.</summary>
        /// <param name="doubleReward">The optional rewarded ad that doubles the Petals (FR-052); null hides it.</param>
        /// <param name="milestone">A milestone was granted: a small mark says so, and its card follows NEXT.</param>
        /// <param name="countUp">Counts the earned Petals up from 0 with this text (spec 003 FR-020); null shows the text.</param>
        public void Show(MonoBehaviour host, string rewardText, Action<Action<string>>? doubleReward = null, bool milestone = false, (long Petals, Func<long, string> Format)? countUp = null)
        {
            _reward.text = rewardText;
            _pendingCount = countUp;
            _milestoneMark.SetActive(milestone);
            _double.gameObject.SetActive(doubleReward != null);
            _double.onClick.RemoveAllListeners();
            if (doubleReward != null)
            {
                _double.onClick.AddListener(() => doubleReward(text =>
                {
                    _reward.text = text;
                    _double.gameObject.SetActive(false);
                }));
            }

            host.StartCoroutine(ShowAfterReveal());
        }

        public void Hide() => _root.SetActive(false);

        private (long Petals, Func<long, string> Format)? _pendingCount;

        private IEnumerator ShowAfterReveal()
        {
            yield return new WaitForSecondsRealtime(RevealSeconds);
            _root.SetActive(true);
            if (_pendingCount.HasValue)
            {
                // The Petals count up while a short sparkle burst plays; NEXT works at once (spec 003 FR-020).
                _count.Format = _pendingCount.Value.Format;
                _count.Run(0, _pendingCount.Value.Petals);
                Bloomlings.Client.Gameplay.Effects.UiFx.Puff((RectTransform)_root.transform, _petal.position, UiTheme.Of(DesignTokens.Colors.PetalCenter), 8, UiKit.Units(110f), UiKit.Units(34f), 0.8f, ProceduralSprites.Shape("fx.sparkle"));
            }

            yield return Bloomlings.Client.Gameplay.Effects.UiFx.Pop(_root.transform, 1.06f, 0.25f);
        }
    }
}
