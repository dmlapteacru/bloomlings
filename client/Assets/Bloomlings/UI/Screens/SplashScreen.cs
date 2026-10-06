using UnityEngine;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The splash of the design board's frame 1 (spec 002 FR-016 as amended by spec 005 FR-039, contracts/look.md §6.13;
    /// the playtest's <c>SplashScreen</c>): the lotus loader. The logo and the lotus stand on the parchment from the first
    /// frame while services and content load; the ring of petals round the lotus fills with the loading
    /// (<see cref="Loaded"/>), "Loading..." under it. Once the first screen is up (<see cref="Ready"/>) and the ring is full,
    /// the lotus iris opens on it and the splash is gone. It takes no tap and never waits for one: the first launch still
    /// goes straight into Level 1 (spec 001 US2).
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour
    {
        private LotusIrisView _view = null!;
        private float _time;
        private float _loaded;
        private int _readyFrames = -1;
        private float _openSince = -1f;

        /// <summary>A top-most canvas under <paramref name="parent"/>, which should survive scene loads (the Boot object).</summary>
        public static SplashScreen Create(Transform parent)
        {
            LotusIrisView view = LotusIrisView.Create(parent, splash: true, Loc.T("splash.loading"));
            var screen = view.gameObject.AddComponent<SplashScreen>();
            screen._view = view;
            view.Show(LotusIris.Splash(0f, 0f));
            return screen;
        }

        /// <summary>How much of the start is done, 0–1 (the ring never runs ahead of it).</summary>
        public void Loaded(float share) => _loaded = Mathf.Max(_loaded, Mathf.Clamp01(share));

        /// <summary>The first screen is loading under the splash: once it is up and the ring is full, the iris opens.</summary>
        public void Ready()
        {
            _loaded = 1f;
            _readyFrames = 0;
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            if (_readyFrames >= 0)
            {
                _readyFrames++;
            }

            // The scene loads the frame after it is asked for: wait for it before opening on it.
            if (_openSince < 0f && _readyFrames >= 2 && LotusIris.SplashFull(_time, _loaded))
            {
                _openSince = _time;
            }

            _view.Show(LotusIris.Splash(_time, LotusIris.SplashProgress(_time, _loaded), _openSince));
            if (LotusIris.SplashDone(_time, _openSince))
            {
                Destroy(gameObject);
            }
        }
    }
}
