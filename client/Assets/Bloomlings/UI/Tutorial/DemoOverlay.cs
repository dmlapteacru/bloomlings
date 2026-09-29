using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Tutorial
{
    /// <summary>
    /// Plays a <see cref="DemoScript"/> (T064): a pointer hand, at most one short message, and a Skip button on every
    /// step. A step that waits for an action lets taps through to the game (the Level 1 guided tap); other steps
    /// continue on a tap anywhere.
    /// </summary>
    public sealed class DemoOverlay : MonoBehaviour
    {
        private readonly List<Image> _icons = new List<Image>();
        private GameObject _root = null!;
        private Image _shade = null!;
        private Image _hand = null!;
        private Image _cross = null!;
        private TextMeshProUGUI _message = null!;
        private RectTransform _iconRow = null!;
        private DemoScript? _script;
        private int _step;
        private Action<DemoScript> _onDone = _ => { };
        private RectTransform? _target;
        private float _time;

        public bool IsShowing => _root.activeSelf;

        public static DemoOverlay Create(Transform parent)
        {
            Image shade = UiFactory.CreateImage("DemoOverlay", parent, null, new Color(0f, 0f, 0f, 0.25f), raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var overlay = shade.gameObject.AddComponent<DemoOverlay>();
            overlay._root = shade.gameObject;
            overlay._shade = shade;
            Button next = shade.gameObject.AddComponent<Button>();
            next.onClick.AddListener(overlay.Advance);

            Image bubble = UiFactory.CreateImage("Bubble", shade.transform, ProceduralSprites.RoundedSquare, UiTheme.Panel);
            UiFactory.Place(bubble.rectTransform, 0.08f, 0.8f, 0.92f, 0.9f);
            overlay._message = UiFactory.CreateText("Message", bubble.transform, string.Empty, 56f, UiTheme.Text);
            UiFactory.Stretch(overlay._message.rectTransform);

            overlay._iconRow = UiFactory.Place(UiFactory.CreateRect("Icons", shade.transform), 0.15f, 0.58f, 0.85f, 0.76f);
            overlay._cross = UiFactory.CreateImage("Ignore", overlay._iconRow, ProceduralSprites.Cross, UiTheme.Warning);
            overlay._cross.preserveAspect = true;
            UiFactory.Place(overlay._cross.rectTransform, 0.4f, 0.2f, 0.6f, 0.8f);

            overlay._hand = UiFactory.CreateImage("Hand", shade.transform, ProceduralSprites.Pointer, Color.white);
            overlay._hand.preserveAspect = true;
            overlay._hand.rectTransform.sizeDelta = new Vector2(140f, 140f);

            Button skip = UiFactory.CreateButton("Skip", shade.transform, Loc.T("demo.skip"), UiTheme.Text, overlay.Finish, 40f);
            UiFactory.Place((RectTransform)skip.transform, 0.78f, 0.92f, 0.97f, 0.97f);
            shade.gameObject.SetActive(false);
            return overlay;
        }

        public void Show(DemoScript script, Action<DemoScript> onDone)
        {
            _script = script;
            _onDone = onDone;
            _step = 0;
            _root.SetActive(true);
            ShowStep();
        }

        /// <summary>Called by the game when the player performed the awaited action.</summary>
        public void NotifyAction()
        {
            if (IsShowing && _script != null && _script.Steps[_step].WaitForAction)
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (_script == null)
            {
                return;
            }

            _step++;
            if (_step >= _script.Steps.Count)
            {
                Finish();
            }
            else
            {
                ShowStep();
            }
        }

        private void Finish()
        {
            DemoScript? script = _script;
            _script = null;
            _root.SetActive(false);
            if (script != null)
            {
                _onDone(script);
            }
        }

        private void ShowStep()
        {
            DemoStep step = _script!.Steps[_step];
            _message.text = step.Message;

            // A step that waits for a game action lets taps through, except on the Skip button.
            _shade.raycastTarget = !step.WaitForAction;
            _shade.color = new Color(0f, 0f, 0f, step.WaitForAction ? 0.08f : 0.25f);

            foreach (Image icon in _icons)
            {
                Destroy(icon.gameObject);
            }

            _icons.Clear();
            IReadOnlyList<VariantVisual>? visuals = step.SideBySide;
            if (visuals != null)
            {
                for (int i = 0; i < visuals.Count; i++)
                {
                    float x = visuals.Count == 1 ? 0.35f : i == 0 ? 0.05f : 0.65f;
                    Image tile = UiFactory.CreateImage("Variant", _iconRow, ProceduralSprites.RoundedSquare, visuals[i].Color);
                    UiFactory.Place(tile.rectTransform, x, 0f, x + 0.3f, 1f);
                    Image icon = UiFactory.CreateImage("Icon", tile.transform, visuals[i].Icon, Color.white);
                    icon.preserveAspect = true;
                    UiFactory.Place(icon.rectTransform, 0.15f, 0.15f, 0.85f, 0.85f);
                    _icons.Add(tile);
                }
            }

            _cross.gameObject.SetActive(step.ShowIgnore);
            _cross.transform.SetAsLastSibling();
            _target = step.PointAt?.Invoke();
            _hand.gameObject.SetActive(_target != null);
            _time = 0f;
        }

        private void Update()
        {
            if (!IsShowing || _target == null)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            _hand.transform.position = _target.position;
            _hand.rectTransform.anchoredPosition += new Vector2(40f, -60f - (18f * Mathf.Sin(_time * 6f)));
        }
    }
}
