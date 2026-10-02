using System.Collections;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using Bloomlings.Client.UI.Localization;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The splash of the design board's frame 1 (spec 002 FR-016) in the reference look of spec 005 (contracts/look.md
    /// §4.5; the playtest's <c>SplashScreen</c>): the wooden logo (the owner's logo picture when it exists) over the warm
    /// garden, with the four families as 3D heroes around the lotus fountain on the stone pedestal (spec 004), fading and
    /// rising in. It shows from the first frame while services and content load, and goes once the first screen is up.
    /// It never waits for a tap: the first launch still goes straight into Level 1 (spec 001 US2).
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour
    {
        private const float AppearSeconds = 0.5f;

        private Canvas _canvas = null!;
        private RectTransform _stage = null!;

        /// <summary>A top-most canvas under <paramref name="parent"/>, which should survive scene loads (the Boot object).</summary>
        public static SplashScreen Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateCanvas("Splash", 1000);
            canvas.transform.SetParent(parent, false);
            var screen = canvas.gameObject.AddComponent<SplashScreen>();
            screen._canvas = canvas;
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("Root", canvas.transform));
            BackdropView.Create(root, BackdropScene.Splash);

            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            float u = DesignTokens.ScaleFor(w, h);
            Box safe = ScreenLayout.SafeArea(w, h, insets);
            var screenBox = new Box(0f, 0f, w, h);

            // The logo at a third of the safe area, at most 900 units wide ("brand.wordmark").
            float logoWidth = Mathf.Min(safe.Width * 0.84f, 900f * u);
            RectTransform logo = OwnerArt.Logo("Logo", root, Loc.T("home.logo"));
            UiKit.PlaceBox(logo, Box.FromCenter(safe.CenterX, safe.Top + (safe.Height * 0.3f), logoWidth, Mathf.Min(logoWidth * 0.37f, T.Wordmark.Size * u * 1.5f)), screenBox);
            UiKit.FadeInOnShow(logo.gameObject, 1f, AppearSeconds);

            // The four families on their stone around the lotus fountain ("brand.splash_art"), without the guest.
            screen._stage = UiFactory.Stretch(UiFactory.CreateRect("Heroes", root));
            UiKit.FadeInOnShow(screen._stage.gameObject, 1f, AppearSeconds);
            float width = Mathf.Min(safe.Width, 1000f * u);
            HeroPictures.Stage("Stage", screen._stage).Place(Box.FromCenter(safe.CenterX, safe.Top + (safe.Height * 0.62f), width, width * 0.7f), screenBox, BackdropScene.Splash, guest: false);
            return screen;
        }

        private void Start() => StartCoroutine(Rise());

        /// <summary>Fades the splash out after <paramref name="delay"/> seconds and removes it.</summary>
        public void FadeOut(float delay)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(FadeRoutine(delay));
            }
        }

        /// <summary>The heroes rise 40 units into place as they fade in.</summary>
        private IEnumerator Rise()
        {
            for (float t = 0f; t < AppearSeconds; t += Time.unscaledDeltaTime)
            {
                _stage.anchoredPosition = new Vector2(0f, -(1f - FadeIn.Ease(t / AppearSeconds)) * UiKit.Units(40f));
                yield return null;
            }

            _stage.anchoredPosition = Vector2.zero;
        }

        private IEnumerator FadeRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Destroy(_canvas.gameObject);
        }
    }
}
