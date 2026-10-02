using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Effects;
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
    /// The win card of the design board's frame 15 (spec 002 FR-020; FR-025, T050, T122) in the reference look of spec
    /// 005 (contracts/look.md §4.4; the playtest's <c>EndCards.Win</c>).
    /// <list type="bullet">
    /// <item><description>The finished picture is revealed on the board first; then the card pops up over a scrim.</description></item>
    /// <item><description>A wooden "Level complete!" sign with white flower clusters lies across the card's top edge,
    /// and above it the heroes celebrate on a stone pedestal in slowly turning light rays, with pink petals falling (the
    /// owner's celebrating hero of the level's main family when that picture exists, pictures.md A7).</description></item>
    /// <item><description>The card shows the finished picture in full color in a thin stone frame (a milestone level
    /// adds the "Milestone reached!" mark over its top edge), the Petals earned counting up in a cream pill with the lotus
    /// (and a dropped booster), Next in its wooden rim, decorated and breathing, and the optional "×2 reward" rewarded ad
    /// as a cream secondary button.</description></item>
    /// <item><description>A milestone level shows its milestone card after Next (<see cref="MilestoneCard"/>).</description></item>
    /// </list>
    /// </summary>
    public sealed class WinScreen : MonoBehaviour
    {
        private const float RevealSeconds = 1.2f;

        /// <summary>The finished picture's height on the card (units), on a phone of 19.5:9 or taller.</summary>
        private const float PictureUnits = 520f;

        /// <summary>The reward row's height (units).</summary>
        private const float RewardUnits = 118f;

        /// <summary>The reward pill's height (units).</summary>
        private const float RewardPillUnits = 104f;

        /// <summary>The wooden sign's height (units).</summary>
        private const float SignUnits = 146f;

        private GameObject _root = null!;
        private RectTransform _card = null!;
        private CelebrationView _celebration = null!;
        private WoodSignView _sign = null!;
        private WinPictureView _picture = null!;
        private CostPillView _mark = null!;
        private RectTransform _medal = null!;
        private RectTransform _rise = null!;
        private CanvasGroup _riseFade = null!;
        private CostPillView _reward = null!;
        private TextMeshProUGUI _rewardText = null!;
        private CountUp _count = null!;
        private Image _dropIcon = null!;
        private TextMeshProUGUI _dropText = null!;
        private Button _next = null!;
        private Button _double = null!;
        private RectTransform _petals = null!;

        private (long Petals, Func<long, string> Format)? _pendingCount;
        private LevelReward? _model;
        private long _petalsShown;
        private Vector3 _lotusAt;
        private Vector3 _pillAt;
        private float _pillWidth;

        public static WinScreen Create(Transform parent, Action onNext)
        {
            Image shade = UiFactory.CreateImage("WinScreen", parent, null, UiTheme.PanelShade, raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var screen = shade.gameObject.AddComponent<WinScreen>();
            screen._root = shade.gameObject;

            // Parchment (spec 005 §3.5) that pops in; everything on it pops with it.
            Image card = UiKit.Paper("Card", shade.transform, b => Mathf.Max(UiKit.Units(DesignTokens.Radius.CardMin), b.Width * DesignTokens.Radius.Card), DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
            card.gameObject.AddComponent<PopMotion>();
            screen._card = card.rectTransform;

            // The celebration first, so the sign lies over the pedestal's foot.
            screen._celebration = HeroPictures.Celebration(card.rectTransform);
            screen._sign = UiKit.WoodSign("Title", card.transform, Loc.T("win.title"), T.LevelHome, SignDecor.Flowers);
            screen._picture = WinPictureView.Create("Picture", card.transform);

            // A milestone level: a cream pill with the gold medal over the picture's top edge.
            screen._mark = UiKit.TextPill("Milestone", card.transform, CostKind.Charges, Loc.T("milestone.reached"));
            screen._medal = UiKit.ShapeImage("Medal", screen._mark.transform, "ui.medal", C.MedalGold).rectTransform;

            // The reward rises in after the reveal (motion.reward): the lotus pill counting up, a dropped booster below it.
            screen._rise = UiFactory.Stretch(UiFactory.CreateRect("Reward", card.transform));
            screen._riseFade = screen._rise.gameObject.AddComponent<CanvasGroup>();
            screen._riseFade.blocksRaycasts = false;
            screen._reward = UiKit.TextPill("Pill", screen._rise, CostKind.Petals, NumberText.Plus(0));
            screen._rewardText = UiKit.PillText(screen._reward);
            screen._count = CountUp.On(screen._rewardText, NumberText.Plus);
            screen._dropIcon = UiKit.BoosterIcon("DropIcon", screen._rise, "extra_slot");
            screen._dropText = UiKit.Label("DropText", screen._rise, string.Empty, T.ButtonSecondary, UiTheme.Of(C.InkBrownSoft), look: TextLook.Plain(C.InkBrownSoft));

            // Next in its wooden rim, decorated and breathing while it waits (spec 003 FR-011a, FR-019).
            screen._next = UiKit.PrimaryButton("Next", card.transform, Loc.T("common.next"), onNext, decorate: true, breathe: true);
            screen._double = UiKit.SecondaryButton("Double", card.transform, Loc.T("win.double"), () => { }, "ui.ad");

            // Petals drift down around the heroes and over the card's top (fx.petals), above everything.
            screen._petals = (RectTransform)UiKit.FallingPetals("Petals", shade.transform).transform;
            UiKit.FadeInOnShow(screen._petals.gameObject, 1f, 0.5f);
            shade.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>Waits for the picture reveal, then shows the card.</summary>
        /// <param name="rewardText">The reward as text, shown in the pill when no <paramref name="reward"/> is given.</param>
        /// <param name="doubleReward">The optional rewarded ad that doubles the Petals (FR-052); null hides it.</param>
        /// <param name="milestone">A milestone was granted: a small mark says so, and its card follows Next.</param>
        /// <param name="countUp">Counts the earned Petals up from 0 (spec 003 FR-020); null shows the amount at once.</param>
        /// <param name="reward">The reward: the Petals in the lotus pill, a dropped booster below it.</param>
        /// <param name="session">The won level, whose finished picture the card shows and whose main family celebrates.</param>
        public void Show(MonoBehaviour host, string rewardText, Action<Action<string>>? doubleReward = null, bool milestone = false, (long Petals, Func<long, string> Format)? countUp = null, LevelReward? reward = null, LevelSession? session = null)
        {
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
                            _rewardText.text = text;
                        }

                        _double.gameObject.SetActive(false);
                    });
                });
            }

            // The reward pill: "+N" with the lotus, or the given text alone.
            bool hasReward = reward != null || !string.IsNullOrEmpty(rewardText);
            _reward.gameObject.SetActive(hasReward);
            long petals = reward?.Petals ?? countUp?.Petals ?? 0L;
            if (reward != null || countUp.HasValue)
            {
                _reward.SetCost(Cost.Petals(0));
                _rewardText.text = NumberText.Plus(petals);
            }
            else
            {
                _reward.SetCost(Cost.Charges(0));
                _rewardText.text = rewardText;
            }

            _petalsShown = petals;
            BoosterKind? drop = reward?.DroppedBooster;
            _dropIcon.gameObject.SetActive(drop.HasValue);
            _dropText.gameObject.SetActive(drop.HasValue);
            if (drop.HasValue)
            {
                string id = BoosterId(drop.Value);
                UiKit.SetBoosterIcon(_dropIcon, id, false);
                _dropText.text = Loc.F("win.drop", Loc.T("booster." + id));
            }

            _celebration.ShowHero(session != null ? HeroPictures.MainFamily(session.Definition.Pods) : (Family?)null);
            Layout(hasReward, drop.HasValue, doubleReward != null, session);
            host.StartCoroutine(ShowAfterReveal());
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>The card's regions as the playtest's <c>EndCards.Win</c> computes them, in screen pixels.</summary>
        private void Layout(bool hasReward, bool drop, bool offerDouble, LevelSession? session)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            Box safe = ScreenLayout.SafeArea(w, h, insets);

            // The finished picture is a little smaller on a short phone, so the heroes keep their room above the card.
            float pictureUnits = Mathf.Max(400f, Mathf.Min(PictureUnits, safe.Height / u * 0.24f));
            float content = pictureUnits + 26f + (hasReward ? RewardUnits : 0f) + (drop ? 64f : 0f) + 22f + DesignTokens.Size.CardPrimaryHeight + (offerDouble ? 26f + DesignTokens.Size.SecondaryHeight : 0f) + 34f;
            CardRegions r = ScreenLayout.Card(w, h, insets, content);
            Box card = r.Card;
            UiKit.PlaceScreen(_card, card);

            // The sign across the card's top edge, as wide as its letters need; the heroes above it.
            float sh = SignUnits * u;
            float signWidth = Mathf.Min(card.Width * 0.8f, MeasurePx(_sign.Label, T.LevelHome.Size * u) + (sh * 1.5f));
            Box sign = Box.FromCenter(card.CenterX, card.Top + (30f * u), signWidth, sh);
            UiKit.PlaceBox((RectTransform)_sign.transform, sign, card);
            _celebration.Place(new Box(card.Left, safe.Top + (12f * u), card.Right, sign.Top + (sh * 0.3f)), card, w, u);

            // The finished picture in full color.
            float y = r.Body.Top + (10f * u);
            var picture = new Box(r.Body.Left + (16f * u), y, r.Body.Right - (16f * u), y + (pictureUnits * u));
            _picture.Rect.gameObject.SetActive(session != null);
            if (session != null)
            {
                _picture.Show(session.Definition, session.Picture, picture, card, u);
            }

            float mh = 70f * u;
            float markWidth = MeasurePx(UiKit.PillText(_mark), mh * 0.56f) + (mh * 1.9f);
            Box mark = Box.FromCenter(picture.CenterX, picture.Top, markWidth, mh);
            UiKit.PlaceBox((RectTransform)_mark.transform, mark, card);
            UiKit.PlaceBox(_medal, Box.FromCenter(mark.Left + (mh * 0.62f), mark.CenterY, mh * 0.8f, mh * 0.8f), mark);
            y = picture.Bottom + (26f * u);

            if (hasReward)
            {
                // The pill is as wide as its final amount needs: the lotus, a gap, the digits and its rounded ends.
                float ph = RewardPillUnits * u;
                float textWidth = MeasurePx(_rewardText, ph * 0.56f);
                bool lotus = _reward.Cost.Kind == CostKind.Petals;
                float icon = lotus ? ph * 0.86f : 0f;
                float gap = lotus ? ph * 0.16f : 0f;
                _pillWidth = Mathf.Max(320f * u, textWidth + icon + gap + (ph * 1.1f));
                Box pill = Box.FromCenter(r.Body.CenterX, y + (RewardUnits * u / 2f), _pillWidth, ph);
                UiKit.PlaceBox((RectTransform)_reward.transform, pill, card);
                float lotusX = pill.CenterX - ((icon + gap + Mathf.Min(textWidth, _pillWidth - icon - gap - (ph * 0.5f))) / 2f) + (icon / 2f);
                _lotusAt = new Vector3(lotusX, h - pill.CenterY, 0f);
                _pillAt = new Vector3(pill.CenterX, h - pill.CenterY, 0f);
                y += RewardUnits * u;
                if (drop)
                {
                    // "+1 Extra Slot" in brown after the booster's icon, centered as a group.
                    float iconSize = 56f * u;
                    float iconGap = 12f * u;
                    float font = T.ButtonSecondary.Size * u * 0.8f;
                    float dropWidth = Mathf.Min(MeasurePx(_dropText, font), r.Body.Width - iconSize - iconGap);
                    float start = r.Body.CenterX - ((iconSize + iconGap + dropWidth) / 2f);
                    float cy = y + (32f * u);
                    UiKit.PlaceBox(_dropIcon.rectTransform, Box.FromCenter(start + (iconSize / 2f), cy, iconSize, iconSize), card);
                    UiKit.PlaceBox(_dropText.rectTransform, Box.FromCenter(start + iconSize + iconGap + (dropWidth / 2f), cy, dropWidth + (4f * u), 64f * u), card);
                    SetFont(_dropText, font);
                    y += 64f * u;
                }
            }

            Box next = ScreenLayout.CardButton(r.Body, y + (22f * u), true, u);
            UiKit.PlaceBox((RectTransform)_next.transform, next, card);
            Box twice = ScreenLayout.CardButton(r.Body, next.Bottom + (26f * u), false, u).Inset(50f * u, 0f);
            UiKit.PlaceBox((RectTransform)_double.transform, twice, card);
            UiKit.PlaceScreen(_petals, new Box(safe.Left, safe.Top, safe.Right, Mathf.Min(card.Bottom, sign.Bottom + (260f * u))));
        }

        private IEnumerator ShowAfterReveal()
        {
            yield return new WaitForSecondsRealtime(RevealSeconds);
            _root.SetActive(true);
            _riseFade.alpha = 0f;
            if (_pendingCount.HasValue && _model != null && _reward.gameObject.activeSelf)
            {
                // The Petals count up while a sparkle bursts at the lotus and petals burst around the pill (spec 003
                // FR-020); Next works at once.
                _count.Run(0, _pendingCount.Value.Petals);
                UiFx.Puff((RectTransform)_root.transform, _lotusAt, UiTheme.Of(C.PetalCenter), 8, UiKit.Units(110f), UiKit.Units(34f), 0.8f, ProceduralSprites.Shape("fx.sparkle"));
                UiFx.Puff((RectTransform)_root.transform, _pillAt, UiTheme.Of(C.LotusFill), 6, (_pillWidth / Mathf.Max(0.0001f, UiKit.PixelsPerUnit) * 0.42f) + UiKit.Units(70f), UiKit.Units(34f), 1.2f, ProceduralSprites.Shape("fx.petal_burst"));
            }

            // The reward rises in a moment after the card (motion.reward).
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

        /// <summary>Sets a placed label's font size (screen pixels), shrinking to its style's minimum when it does not fit.</summary>
        private static void SetFont(TextMeshProUGUI label, float fontPx)
        {
            float size = fontPx / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            label.fontSizeMax = size;
            label.fontSize = size;
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
