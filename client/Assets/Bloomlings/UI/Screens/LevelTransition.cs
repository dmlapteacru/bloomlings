using System;
using UnityEngine;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The lotus iris between two levels (spec 005 FR-039, contracts/look.md §6.13; the playtest's <c>DesignApp.NextLevel</c>):
    /// the win's Next closes the iris over the win, the next level is asked for under the closed cover with "Level N" on it
    /// (after the interstitial when one is due, spec 001 FR-053), and once its scene is up the iris opens on it. It lives on
    /// the Boot object, so it survives the scene load.
    /// </summary>
    public sealed class LevelTransition : MonoBehaviour
    {
        private LotusIrisView? _view;
        private Action<Action>? _switch;
        private float _time = -1f;
        private bool _asked;
        private bool _switched;
        private int _framesSinceSwitch;

        /// <summary>Whether the iris shows.</summary>
        public bool Playing => _time >= 0f;

        /// <summary>
        /// Plays the transition to <paramref name="level"/>: at <see cref="LotusIris.SwitchAt"/> it calls
        /// <paramref name="switchLevel"/>, which starts the next level and then calls its argument (at once, or after an
        /// interstitial); the cover stays closed until then and until the level's scene is up.
        /// </summary>
        public void Play(int level, Action<Action> switchLevel)
        {
            if (Playing)
            {
                return;
            }

            if (_view == null)
            {
                _view = LotusIrisView.Create(transform, splash: false, string.Empty);
            }

            _view.SetText(Loc.F("common.level", level));
            _switch = switchLevel;
            _time = 0f;
            _asked = false;
            _switched = false;
            _framesSinceSwitch = 0;
            _view.Show(LotusIris.Transition(0f), block: true);
        }

        private void Update()
        {
            if (!Playing || _view == null)
            {
                return;
            }

            float next = _time + Time.unscaledDeltaTime;
            if (!_asked && next >= LotusIris.SwitchAt)
            {
                _asked = true;
                _time = LotusIris.SwitchAt;
                _view.Show(LotusIris.Transition(_time), block: true);
                _switch?.Invoke(() => _switched = true);
                return;
            }

            if (_switched)
            {
                _framesSinceSwitch++;
            }

            // The cover holds closed while the next level is not up yet (an interstitial, the scene load).
            bool up = _switched && _framesSinceSwitch >= 2;
            _time = _asked && !up ? Mathf.Min(next, LotusIris.OpenAt) : next;
            if (_time >= LotusIris.TransitionSeconds)
            {
                _time = -1f;
                _switch = null;
                _view.Show(LotusIris.Transition(LotusIris.TransitionSeconds), block: false);
                return;
            }

            _view.Show(LotusIris.Transition(_time), block: true);
        }
    }
}
