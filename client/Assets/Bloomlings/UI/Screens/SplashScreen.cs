using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The splash of the design board's frame 1 (spec 002 FR-016): the Bloomlings wordmark over the garden, with the
    /// four families as 3D heroes on their stone (spec 004). It shows from the first frame while services and content load, and
    /// fades once the first screen is up. It never waits for a tap: the first launch still goes straight into
    /// Level 1 (spec 001 US2).
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour
    {
        private Canvas _canvas = null!;

        /// <summary>A top-most canvas under <paramref name="parent"/>, which should survive scene loads (the Boot object).</summary>
        public static SplashScreen Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateCanvas("Splash", 1000);
            canvas.transform.SetParent(parent, false);
            var screen = canvas.gameObject.AddComponent<SplashScreen>();
            screen._canvas = canvas;
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect("Root", canvas.transform));
            BackdropView.Create(root, BackdropScene.Splash);
            TextMeshProUGUI shadow = UiKit.Label("WordmarkShadow", root, Loc.T("home.logo"), DesignTokens.Type.Wordmark, UiTheme.Of(DesignTokens.Colors.WordmarkOutline));
            UiFactory.Place(shadow.rectTransform, 0.06f, 0.6f, 0.94f, 0.74f);
            TextMeshProUGUI wordmark = UiKit.Label("Wordmark", root, Loc.T("home.logo"), DesignTokens.Type.Wordmark, UiTheme.Of(DesignTokens.Colors.WordmarkFill));
            wordmark.outlineColor = UiTheme.Of(DesignTokens.Colors.WordmarkOutline);
            UiFactory.Place(wordmark.rectTransform, 0.06f, 0.605f, 0.94f, 0.745f);
            Image petal = UiKit.PetalIcon("Petal", root);
            UiFactory.Place(petal.rectTransform, 0.5f, 0.73f, 0.58f, 0.775f);

            // The four families as 3D heroes on their stone (spec 004 FR-017; "brand.splash_art").
            UiFactory.Place(HeroPictures.Group("Heroes", root), 0.02f, 0.22f, 0.98f, 0.52f);

            return screen;
        }

        /// <summary>Fades the splash out after <paramref name="delay"/> seconds and removes it.</summary>
        public void FadeOut(float delay)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(FadeRoutine(delay));
            }
        }

        private IEnumerator FadeRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Destroy(_canvas.gameObject);
        }
    }
}
