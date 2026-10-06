using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI.Tutorial
{
    /// <summary>
    /// Plays a <see cref="DemoScript"/> (T064): a pointer hand, at most one short message, and a Skip button on every
    /// step. A step that waits for an action lets taps through to the game (the Level 1 guided tap); other steps
    /// continue on a tap anywhere. In the reference look of spec 005 (contracts/look.md §4.3, the demo cards): the message
    /// on parchment in brown, at most two balanced lines; variants as sticker candy tiles; Skip a cream button.
    /// </summary>
    public sealed class DemoOverlay : MonoBehaviour
    {
        /// <summary>The shade over the game: light while a step waits for the player's tap, darker otherwise.</summary>
        private const float ShadeAlpha = 0.25f;
        private const float WaitingShadeAlpha = 0.08f;

        private readonly List<RectTransform> _icons = new List<RectTransform>();
        private GameObject _root = null!;
        private Image _shade = null!;
        private Image _hand = null!;
        private Image _cross = null!;
        private TextMeshProUGUI _message = null!;
        private TextMeshProUGUI _probe = null!;
        private float _messageWidth;
        private RectTransform _iconRow = null!;
        private DemoScript? _script;
        private int _step;
        private Action<DemoScript> _onDone = _ => { };
        private RectTransform? _target;
        private float _time;
        private bool _ignoring;

        public bool IsShowing => _root.activeSelf;

        public static DemoOverlay Create(Transform parent)
        {
            Image shade = UiFactory.CreateImage("DemoOverlay", parent, null, UiTheme.Of(C.SurfaceScrim.WithAlpha(ShadeAlpha)), raycast: true);
            UiFactory.Stretch(shade.rectTransform);
            var overlay = shade.gameObject.AddComponent<DemoOverlay>();
            overlay._root = shade.gameObject;
            overlay._shade = shade;
            Button next = shade.gameObject.AddComponent<Button>();
            next.onClick.AddListener(overlay.Advance);

            // The message on parchment, never a touch target (taps go to the shade or, while waiting, to the game).
            Image bubble = UiKit.Paper("Bubble", shade.transform, 48f, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard, raycast: false);
            UiFactory.Place(bubble.rectTransform, 0.08f, 0.8f, 0.92f, 0.9f);
            overlay._message = UiKit.Label("Message", bubble.transform, string.Empty, T.ButtonSecondary, UiTheme.Of(C.InkBrown), look: TextLook.Plain(C.InkBrown));
            UiFactory.Place(overlay._message.rectTransform, 0.05f, 0.08f, 0.95f, 0.92f);
            overlay._probe = UiKit.Label("Probe", bubble.transform, string.Empty, T.ButtonSecondary, Color.clear);
            overlay._messageWidth = UiKit.ScreenBox().Width * 0.84f * 0.9f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);

            overlay._iconRow = UiFactory.Place(UiFactory.CreateRect("Icons", shade.transform), 0.15f, 0.58f, 0.85f, 0.76f);
            overlay._cross = UiFactory.CreateImage("Ignore", overlay._iconRow, ProceduralSprites.Cross, UiTheme.Of(C.StateDanger));
            overlay._cross.preserveAspect = true;
            UiFactory.Place(overlay._cross.rectTransform, 0.4f, 0.2f, 0.6f, 0.8f);

            overlay._hand = UiFactory.CreateImage("Hand", shade.transform, ProceduralSprites.Pointer, Color.white);
            overlay._hand.preserveAspect = true;
            overlay._hand.rectTransform.sizeDelta = new Vector2(UiKit.Units(140f), UiKit.Units(140f));

            Button skip = UiKit.SecondaryButton("Skip", shade.transform, Loc.T("demo.skip"), overlay.Finish);
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
            _message.text = string.Join("\n", UiKit.BalancedLines(_probe, step.Message, UiKit.Units(T.ButtonSecondary.Size), _messageWidth));

            // A step that waits for a game action lets taps through, except on the Skip button.
            _shade.raycastTarget = !step.WaitForAction;
            _shade.color = UiTheme.Of(C.SurfaceScrim.WithAlpha(step.WaitForAction ? WaitingShadeAlpha : ShadeAlpha));

            foreach (RectTransform icon in _icons)
            {
                Destroy(icon.gameObject);
            }

            _icons.Clear();
            IReadOnlyList<VariantVisual>? visuals = step.SideBySide;
            if (visuals != null)
            {
                // Each variant as its sticker candy tile (spec 005 §3.1), as on the playtest's demo cards.
                float width = visuals.Count <= 2 ? 0.3f : 0.9f / visuals.Count;
                for (int i = 0; i < visuals.Count; i++)
                {
                    float x = visuals.Count == 1 ? 0.35f
                        : visuals.Count == 2 ? (i == 0 ? 0.05f : 0.65f)
                        : 0.05f + (i * 0.9f / visuals.Count);
                    CandyTileView tile = UiKit.CandyTile("Variant", _iconRow, visuals[i].Id, TileStyle.Sticker);
                    RectTransform rect = UiFactory.Place((RectTransform)tile.transform, x, 0f, x + width - 0.02f, 1f);
                    _icons.Add(rect);
                }
            }

            _ignoring = step.ShowIgnore && _icons.Count >= 2;

            _cross.gameObject.SetActive(step.ShowIgnore);
            _cross.transform.SetAsLastSibling();
            _target = step.PointAt?.Invoke();
            _hand.gameObject.SetActive(_target != null);
            _time = 0f;
        }

        private void Update()
        {
            if (!IsShowing)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            if (_ignoring)
            {
                // FR-071: the first variant's pod walks toward the sibling's tile, meets the cross and turns back.
                float gap = _iconRow.rect.width * 0.3f;
                float k = Mathf.PingPong(_time * 0.9f, 1f);
                _icons[0].anchoredPosition = new Vector2(gap * Mathf.SmoothStep(0f, 1f, k), 0f);
                _cross.transform.localScale = Vector3.one * (k > 0.85f ? 1.2f : 1f);
            }

            if (_target == null)
            {
                return;
            }

            _hand.transform.position = _target.position;
            _hand.rectTransform.anchoredPosition += new Vector2(UiKit.Units(40f), -UiKit.Units(60f) - (UiKit.Units(18f) * Mathf.Sin(_time * 6f)));
        }
    }
}
