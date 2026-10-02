using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The win of the design board's frame 15 (spec 002 FR-020; FR-025, T050, T122) as the reference's full-screen
    /// celebration (spec 005 FR-023, contracts/look.md §4.4 and §6.3; the playtest's <c>EndCards.Win</c>), laid out by
    /// <see cref="ScreenLayout.WinScreen"/>:
    /// <list type="bullet">
    /// <item><description>The finished picture is revealed on the board first; then the celebration fades in over the
    /// whole screen, on the win's garden (<c>bg.win</c>: the owner's picture, else the level's lawn blurred and
    /// lightened). It covers the gameplay, top bar included, and takes every tap: Next goes on (FR-023: no top bar on
    /// it).</description></item>
    /// <item><description>Top to bottom: the wooden "Level / complete!" sign with white flower clusters, the finished
    /// picture large in its stone frame (a milestone level adds the "Milestone reached!" mark over its top edge), the
    /// celebrating hero of the level's main family (pictures.md A7; else the group) on a stone pedestal overlapping the
    /// picture's foot, with slowly turning light rays behind it, pink petals falling over the screen and, for the first
    /// seconds, a light sprinkle of confetti in the level's colors around the sign.</description></item>
    /// <item><description>On the pedestal's front, the Petals earned counting up on a cream plate with the lotus, a dropped
    /// booster on a cream tile at its left and the optional "×2" rewarded ad at its right; at the bottom, Next in its
    /// wooden rim, decorated and breathing.</description></item>
    /// <item><description>A milestone level shows its milestone screen after Next (<see cref="MilestoneCard"/>).</description></item>
    /// </list>
    /// </summary>
    public sealed class WinScreen : MonoBehaviour
    {
        private const float RevealSeconds = 1.2f;

        /// <summary>How long the celebration takes to fade in over the revealed board.</summary>
        private const float FadeSeconds = 0.35f;

        private GameObject _root = null!;
        private RectTransform _rect = null!;
        private CanvasGroup _fade = null!;
        private BackdropView? _backdrop;
        private BackgroundTheme? _theme;
        private CelebrationView _celebration = null!;
        private WinPictureView _picture = null!;
        private CelebrationSignView _sign = null!;
        private CostPillView _mark = null!;
        private RectTransform _medal = null!;
        private RectTransform _rise = null!;
        private CanvasGroup _riseFade = null!;
        private RewardPlateView _reward = null!;
        private CountUp _count = null!;
        private RectTransform _drop = null!;
        private Image _dropIcon = null!;
        private Button _next = null!;
        private Button _double = null!;
        private RectTransform _petals = null!;
        private RectTransform _confettiClip = null!;
        private ConfettiView _confetti = null!;

        private (long Petals, Func<long, string> Format)? _pendingCount;
        private LevelReward? _model;
        private long _petalsShown;
        private Vector3 _lotusAt;
        private Vector3 _pillAt;
        private float _pillWidth;
        private int _shows;

        public static WinScreen Create(Transform parent, Action onNext)
        {
            // The whole screen takes every tap while the celebration shows; its garden covers the gameplay below.
            Image root = UiFactory.CreateImage("WinScreen", parent, null, UiTheme.Of(C.LawnLight), raycast: true);
            UiFactory.Stretch(root.rectTransform);
            var screen = root.gameObject.AddComponent<WinScreen>();
            screen._root = root.gameObject;
            screen._rect = root.rectTransform;
            screen._fade = root.gameObject.AddComponent<CanvasGroup>();
            Transform t = root.transform;

            // Back to front: the rays, the finished picture, the pedestal and the hero over the picture's foot, the sign.
            screen._celebration = HeroPictures.Celebration(root.rectTransform);
            screen._picture = WinPictureView.Create("Picture", t);
            screen._celebration.BringHeroesForward();
            screen._sign = UiKit.CelebrationSign("Title", t, Loc.T("win.title"));

            // A milestone level: a cream pill with the gold medal over the picture's top edge.
            screen._mark = UiKit.TextPill("Milestone", t, CostKind.Charges, Loc.T("milestone.reached"));
            screen._medal = UiKit.GoldMedal("Medal", screen._mark.transform).rectTransform;

            // The reward rises in after the fade (motion.reward): the lotus plate counting up on the pedestal's front, a
            // dropped booster on its left.
            screen._rise = UiFactory.Stretch(UiFactory.CreateRect("Reward", t));
            screen._riseFade = screen._rise.gameObject.AddComponent<CanvasGroup>();
            screen._riseFade.blocksRaycasts = false;
            screen._reward = UiKit.RewardPlate("Plate", screen._rise);
            screen._count = CountUp.On(screen._reward.Label, NumberText.Plus);
            screen._dropIcon = UiKit.BoosterIcon("DropIcon", screen._rise, "extra_slot");
            screen._drop = UiKit.DropTile("Drop", screen._rise, screen._dropIcon, NumberText.Plus(1));

            // The ×2 rewarded ad at the plate's right, and Next in its wooden rim, decorated and breathing while it waits
            // (spec 003 FR-011a, FR-019).
            screen._double = UiKit.DoubleOffer("Double", t, () => { });
            screen._next = UiKit.PrimaryButton("Next", t, Loc.T("common.next"), onNext, T.ButtonLarge, decorate: true, breathe: true);

            // Petals drift down over the whole screen (fx.petals), and a light sprinkle of confetti in the level's colors
            // falls around the sign for the first seconds (fx.confetti).
            screen._petals = (RectTransform)UiKit.FallingPetals("Petals", t).transform;
            UiKit.FadeInOnShow(screen._petals.gameObject, 1f, 0.5f);
            screen._confettiClip = UiFactory.CreateRect("ConfettiClip", t);
            screen._confettiClip.gameObject.AddComponent<RectMask2D>();
            screen._confetti = UiKit.Confetti("Confetti", screen._confettiClip);
            root.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>
        /// The level's backdrop theme (the band's, <see cref="ThemeRotation"/>; null: the first theme): the win's garden is
        /// rendered now, at the level's start, so the celebration shows without a pause.
        /// </summary>
        public void SetTheme(BackgroundTheme? theme)
        {
            _theme = theme;
            if (_backdrop == null)
            {
                _backdrop = BackdropView.Create(_rect, BackdropScene.Win);
            }

            _backdrop.Show(theme);
        }

        /// <summary>Waits for the picture reveal, then shows the celebration.</summary>
        /// <param name="rewardText">The reward as text, shown on the plate when no <paramref name="reward"/> is given.</param>
        /// <param name="doubleReward">The optional rewarded ad that doubles the Petals (FR-052); null hides it.</param>
        /// <param name="milestone">A milestone was granted: a small mark says so, and its screen follows Next.</param>
        /// <param name="countUp">Counts the earned Petals up from 0 (spec 003 FR-020); null shows the amount at once.</param>
        /// <param name="reward">The reward: the Petals on the lotus plate, a dropped booster beside it.</param>
        /// <param name="session">The won level, whose finished picture shows and whose main family celebrates.</param>
        public void Show(MonoBehaviour host, string rewardText, Action<Action<string>>? doubleReward = null, bool milestone = false, (long Petals, Func<long, string> Format)? countUp = null, LevelReward? reward = null, LevelSession? session = null)
        {
            if (_backdrop == null)
            {
                SetTheme(_theme);
            }

            _model = reward;
            _pendingCount = countUp;
            _mark.gameObject.SetActive(milestone);
            _double.gameObject.SetActive(doubleReward != null);
            _double.onClick.RemoveAllListeners();
            if (doubleReward != null)
            {
                _double.onClick.AddListener(() =>
                {
                    GameFeedback.Current?.Play(SoundCue.Click);
                    doubleReward(text =>
                    {
                        // The ad doubled the Petals: count on from what shows to the doubled amount.
                        if (_model != null)
                        {
                            _count.Run(_petalsShown, _model.Petals * 2L);
                            _petalsShown = _model.Petals * 2L;
                        }
                        else
                        {
                            _reward.Label.text = text;
                        }

                        _double.gameObject.SetActive(false);
                    });
                });
            }

            // The reward plate: "+N" with the lotus, or the given text alone.
            bool hasReward = reward != null || !string.IsNullOrEmpty(rewardText);
            _reward.gameObject.SetActive(hasReward);
            long petals = reward?.Petals ?? countUp?.Petals ?? 0L;
            bool lotus = reward != null || countUp.HasValue;
            _reward.ShowLotus(lotus);
            _reward.Label.text = lotus ? NumberText.Plus(petals) : rewardText;
            _petalsShown = petals;

            BoosterKind? drop = reward?.DroppedBooster;
            _drop.gameObject.SetActive(drop.HasValue);
            if (drop.HasValue)
            {
                UiKit.SetBoosterIcon(_dropIcon, BoosterId(drop.Value), false);
            }

            _celebration.ShowHero(session != null ? HeroPictures.MainFamily(session.Definition.Pods) : (Family?)null);
            _confettiClip.gameObject.SetActive(session != null);
            if (session != null)
            {
                _confetti.SetColors(ConfettiView.ColorsOf(session.Definition.Pods));
            }

            Layout(session);
            host.StartCoroutine(ShowAfterReveal(++_shows));
        }

        /// <summary>Hides the celebration, and keeps a reveal still waiting (a restart from Pause) from showing it.</summary>
        public void Hide()
        {
            _shows++;
            _root.SetActive(false);
        }

        /// <summary>The celebration's regions (<see cref="ScreenLayout.WinScreen"/>), in screen pixels.</summary>
        private void Layout(LevelSession? session)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            var screen = new Box(0f, 0f, w, h);
            WinRegions r = ScreenLayout.WinScreen(w, h, insets);

            UiKit.PlaceBox((RectTransform)_sign.transform, r.Sign, screen);

            // The owner's win garden paints its own round stone stage: anchored at the top and zoomed so that stage lies
            // under the hero's feet, it replaces the drawn pedestal (one stage, as on the reference).
            bool painted = _backdrop != null && _backdrop.StandOnPicture(screen, r.Hero.Top + (r.Hero.Height * HomeStage.FeetShare));
            _celebration.Place(r, screen, pedestal: !painted);

            // The finished picture in full color, its frame on the region's top edge.
            _picture.Rect.gameObject.SetActive(session != null);
            Box frame = new Box(r.Picture.Left, r.Picture.Top, r.Picture.Right, r.Picture.Top);
            if (session != null)
            {
                _picture.Show(session.Definition, session.Picture, r.Picture, screen, u, alignTop: true);
                frame = _picture.Frame;
            }

            float mh = 70f * u;
            float markWidth = MeasurePx(UiKit.PillText(_mark), mh * 0.56f) + (mh * 1.9f);
            Box mark = Box.FromCenter(frame.CenterX, frame.Top + (mh * 0.2f), markWidth, mh);
            UiKit.PlaceBox((RectTransform)_mark.transform, mark, screen);
            UiKit.PlaceBox(_medal, Box.FromCenter(mark.Left + (mh * 0.62f), mark.CenterY, mh * 0.7f, mh * 0.7f), mark);

            // The plate on the pedestal's front; the sparkle and the petal burst start at its lotus.
            Box plate = r.Reward;
            UiKit.PlaceBox((RectTransform)_reward.transform, plate, screen);
            float ph = plate.Height;
            float icon = _reward.Lotus.gameObject.activeSelf ? ph * 0.62f : 0f;
            float gap = icon > 0f ? ph * 0.1f : 0f;
            float textWidth = Mathf.Min(MeasurePx(_reward.Label, ph * 0.5f), plate.Width - icon - gap);
            _pillWidth = plate.Width;
            _lotusAt = new Vector3(plate.CenterX - ((icon + gap + textWidth) / 2f) + (icon / 2f), h - plate.CenterY, 0f);
            _pillAt = new Vector3(plate.CenterX, h - plate.CenterY, 0f);

            UiKit.PlaceBox(_drop, r.Drop, screen);
            UiKit.PlaceBox((RectTransform)_double.transform, r.Double, screen);
            UiKit.PlaceBox((RectTransform)_next.transform, r.Next, screen);
            UiKit.PlaceBox(_petals, r.Safe, screen);
            UiKit.PlaceBox(_confettiClip, new Box(0f, 0f, w, frame.Top), screen);
        }

        private IEnumerator ShowAfterReveal(int show)
        {
            yield return new WaitForSecondsRealtime(RevealSeconds);
            if (show != _shows)
            {
                yield break;
            }

            _root.SetActive(true);
            _fade.alpha = 0f;
            _riseFade.alpha = 0f;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                _fade.alpha = FadeIn.Ease(t / FadeSeconds);
                yield return null;
            }

            _fade.alpha = 1f;
            if (_pendingCount.HasValue && _model != null && _reward.gameObject.activeSelf)
            {
                // The Petals count up while a sparkle bursts at the lotus and petals burst around the plate (spec 003
                // FR-020); Next works at once.
                _count.Run(0, _pendingCount.Value.Petals);
                UiFx.Puff(_rect, _lotusAt, UiTheme.Of(C.PetalCenter), 8, UiKit.Units(110f), UiKit.Units(34f), 0.8f, ProceduralSprites.Shape("fx.sparkle"));
                UiFx.Puff(_rect, _pillAt, UiTheme.Of(C.LotusFill), 6, (_pillWidth / Mathf.Max(0.0001f, UiKit.PixelsPerUnit) * 0.42f) + UiKit.Units(70f), UiKit.Units(34f), 1.2f, ProceduralSprites.Shape("fx.petal_burst"));
            }

            // The reward rises in a moment after the celebration (motion.reward).
            yield return new WaitForSecondsRealtime(0.15f);
            float seconds = DesignTokens.Motion.Reward.Seconds;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = FadeIn.Ease(t / seconds);
                _riseFade.alpha = k;
                _rise.anchoredPosition = new Vector2(0f, -(1f - k) * UiKit.Units(30f));
                yield return null;
            }

            _riseFade.alpha = 1f;
            _rise.anchoredPosition = Vector2.zero;
        }

        /// <summary>A label's natural width in screen pixels at a font size in screen pixels (an estimate before the text engine can tell).</summary>
        private static float MeasurePx(TextMeshProUGUI label, float fontPx)
        {
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float width = KitText.Measure(label, fontPx / ppu) * ppu;
            return width > 0f ? width : fontPx * 0.56f * label.text.Length;
        }

        private static string BoosterId(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => "extra_slot",
            BoosterKind.Shuffle => "shuffle",
            BoosterKind.Return => "return",
            _ => "bloom_burst",
        };
    }
}
