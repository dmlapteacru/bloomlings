using System.Collections;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The splash of the design board's frame 1 (spec 002 FR-016) in the reference look of spec 005 (contracts/look.md
    /// §4.5; the playtest's <c>SplashScreen</c>): the wooden logo (the owner's logo picture when it exists) over the
    /// garden (the owner's splash picture, else the Home garden, <see cref="OwnerPictures.Resolve"/>), fading in where
    /// Home shows it (<see cref="ScreenLayout.ReferenceHome"/>'s logo, §6.4). Over the owner's Home garden it shows Home's
    /// layered fountain from the first frame, its four animated heroes fading in on it (spec 005 FR-028,
    /// <see cref="HomeLayersView"/>) and the petals drifting, in the very motion Home then shows under it, so Home takes
    /// over without a jump; without the owner's picture, the drawn diorama's four families as 3D heroes around the lotus
    /// fountain (spec 004) rise in where Home shows them. It shows from the first frame while services and content load,
    /// and goes once the first screen is up. It takes no tap and never waits for one: the first launch still goes
    /// straight into Level 1 (spec 001 US2).
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour
    {
        private const float AppearSeconds = 0.5f;

        private Canvas _canvas = null!;
        private RectTransform _stage = null!;
        private HomeLayersView? _layers;

        /// <summary>A top-most canvas under <paramref name="parent"/>, which should survive scene loads (the Boot object).</summary>
        public static SplashScreen Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateCanvas("Splash", 1000);
            canvas.transform.SetParent(parent, false);
            var screen = canvas.gameObject.AddComponent<SplashScreen>();
            screen._canvas = canvas;
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("Root", canvas.transform));
            BackdropView.Create(root, BackdropScene.Splash);

            // The logo and the heroes stand where Home shows them (contracts/look.md §6.4), so Home takes over in place.
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            ReferenceHomeRegions r = ScreenLayout.ReferenceHome(w, h, insets);
            var screenBox = new Box(0f, 0f, w, h);

            // The four families as Home shows them ("brand.splash_art"): on the owner's layered fountain, which shows at
            // once as part of the garden while its heroes fade in on it, or around the drawn diorama's lotus fountain,
            // rising in with it. The splash's heroes take no taps (Home's, under it, do).
            screen._stage = UiFactory.Stretch(UiFactory.CreateRect("Heroes", root));
            HomeStageView stage = HeroPictures.Stage("Stage", screen._stage, tappable: false);
            stage.Place(r.Diorama, screenBox, BackdropScene.Splash);
            screen._layers = stage.Layers;
            if (screen._layers != null)
            {
                screen._layers.HeroAlpha = 0f;
            }
            else
            {
                UiKit.FadeInOnShow(screen._stage.gameObject, 1f, AppearSeconds);
            }

            // The logo across the top ("brand.wordmark"), where Home shows it.
            RectTransform logo = OwnerArt.Logo("Logo", root, Loc.T("home.logo"));
            UiKit.PlaceBox(logo, OwnerArt.LogoBox(r), screenBox);
            UiKit.FadeInOnShow(logo.gameObject, 1f, AppearSeconds);
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

        /// <summary>
        /// The drawn diorama's heroes rise 40 units into place as they fade in; the layered Home's heroes fade in where they
        /// stand, on the fountain.
        /// </summary>
        private IEnumerator Rise()
        {
            for (float t = 0f; t < AppearSeconds; t += Time.unscaledDeltaTime)
            {
                float k = FadeIn.Ease(t / AppearSeconds);
                if (_layers != null)
                {
                    _layers.HeroAlpha = k;
                }
                else
                {
                    _stage.anchoredPosition = new Vector2(0f, -(1f - k) * UiKit.Units(40f));
                }

                yield return null;
            }

            if (_layers != null)
            {
                _layers.HeroAlpha = 1f;
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
