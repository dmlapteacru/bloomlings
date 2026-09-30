using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// A special object on the board (T108): the Garden Gate / hedge seal, the Fountain, the Chest (L150), and the Statue
    /// or Bridge (L250), drawn over its cells. Its condition is always visible (FR-037, FR-038): a key for a key door,
    /// the exact variant's icon for "restore N &lt;variant&gt; around it", a dashed square for "restore this region",
    /// and the counter. When it triggers: a gate swings open and fades; a chest pops open with a sparkle and fades; a
    /// statue glows and fades; a Fountain sprays droplets and stays as a landmark; a bridge is repaired and stays.
    /// </summary>
    public sealed class SpecialView : MonoBehaviour
    {
        private Image _body = null!;
        private Image _condition = null!;
        private TextMeshProUGUI _counter = null!;
        private Coroutine? _animation;

        public string SpecialId { get; private set; } = string.Empty;

        public SpecialType Type { get; private set; }

        public IReadOnlyList<CellPos> Cells { get; private set; } = new CellPos[0];

        public static SpecialView Create(Transform parent, SpecialInfo special, VariantVisualCatalog? visuals)
        {
            Image body = UiFactory.CreateImage("Special " + special.Id, parent, SpriteOf(special.Type, triggered: false), ColorOf(special.Type));
            body.preserveAspect = true;
            var view = body.gameObject.AddComponent<SpecialView>();
            view._body = body;
            view.SpecialId = special.Id;
            view.Type = special.Type;
            view.Cells = special.Cells;

            SpecialCondition condition = special.Condition;
            Sprite? conditionSprite = condition.Kind == SpecialConditionKind.Key
                ? ProceduralSprites.Key
                : condition.Variant.HasValue ? Visual(visuals, condition.Variant.Value).Icon
                : condition.Kind == SpecialConditionKind.ClearRegion ? ProceduralSprites.Region : null;
            Color conditionColor = condition.Variant.HasValue ? Visual(visuals, condition.Variant.Value).Color
                : condition.Kind == SpecialConditionKind.ClearRegion ? UiTheme.Dark(ColorOf(special.Type)) : UiTheme.EntryMarker;
            view._condition = UiFactory.CreateImage("Condition", body.transform, conditionSprite, conditionColor);
            view._condition.preserveAspect = true;
            view._condition.enabled = conditionSprite != null;
            UiFactory.Place(view._condition.rectTransform, 0.02f, 0.55f, 0.45f, 0.98f);
            view._counter = UiFactory.CreateText("Counter", body.transform, string.Empty, 40f, UiTheme.Text);
            view._counter.fontStyle = FontStyles.Bold;
            UiFactory.Place(view._counter.rectTransform, 0.4f, 0f, 1f, 0.4f);
            view.SetProgress(special.Progress, special.Total, special.Condition.Kind);
            if (special.Triggered)
            {
                view.ShowTriggered();
            }

            return view;
        }

        /// <summary>The counter: "3/6" for counts and regions; a key door shows only its key.</summary>
        public void SetProgress(int progress, int total, SpecialConditionKind kind)
        {
            _counter.text = kind == SpecialConditionKind.Key ? string.Empty : progress.ToString(CultureInfo.InvariantCulture) + "/" + total.ToString(CultureInfo.InvariantCulture);
            if (isActiveAndEnabled && kind != SpecialConditionKind.Key)
            {
                Play(Bump());
            }
        }

        /// <summary>The color a special is drawn in; its counted cells are outlined in it too.</summary>
        public static Color ColorOf(SpecialType type) => type switch
        {
            SpecialType.Fountain => UiTheme.Of(UI.Design.DesignTokens.Colors.SpecialFountain),
            SpecialType.Chest => UiTheme.Of(UI.Design.DesignTokens.Colors.SpecialChest),
            SpecialType.Statue => UiTheme.Of(UI.Design.DesignTokens.Colors.SpecialStatue),
            SpecialType.Bridge => UiTheme.Of(UI.Design.DesignTokens.Colors.SpecialBridge),
            _ => UiTheme.Of(UI.Design.DesignTokens.Colors.SpecialGate),
        };

        /// <summary>Whether the object stays on the board after it triggers (a landmark) or its cells turn to open ground.</summary>
        public static bool StaysAfterTrigger(SpecialType type) => type == SpecialType.Fountain || type == SpecialType.Bridge;

        private static Sprite SpriteOf(SpecialType type, bool triggered) => type switch
        {
            SpecialType.Fountain => ProceduralSprites.Fountain,
            SpecialType.Chest => ProceduralSprites.Chest,
            SpecialType.Statue => ProceduralSprites.Statue,
            SpecialType.Bridge => triggered ? ProceduralSprites.Bridge : ProceduralSprites.BridgeBroken,
            _ => ProceduralSprites.Gate,
        };

        /// <summary>The trigger animation; a gate, chest or statue then disappears (its cells are open ground now).</summary>
        /// <param name="overlay">Where the droplets and sparkles are drawn.</param>
        public void Trigger(RectTransform overlay, float cellSize)
        {
            _counter.text = string.Empty;
            _condition.enabled = false;
            if (!isActiveAndEnabled)
            {
                ShowTriggered();
                return;
            }

            switch (Type)
            {
                case SpecialType.Fountain:
                    Effects.UiFx.Puff(overlay, transform.position, new Color(0.62f, 0.82f, 0.95f, 0.9f), 10, cellSize * 1.8f, cellSize * 0.25f, 0.7f);
                    Play(Spray());
                    break;
                case SpecialType.Chest:
                    Effects.UiFx.Puff(overlay, transform.position, UiTheme.EntryMarker, 8, cellSize * 1.4f, cellSize * 0.3f, 0.5f, ProceduralSprites.Star);
                    Play(PopOpen());
                    break;
                case SpecialType.Statue:
                    Effects.UiFx.Puff(overlay, transform.position, Color.white, 8, cellSize * 1.2f, cellSize * 0.25f, 0.5f, ProceduralSprites.Star);
                    Play(Glow());
                    break;
                case SpecialType.Bridge:
                    _body.sprite = SpriteOf(Type, triggered: true);
                    Play(Bump());
                    break;
                default:
                    Play(SwingOpen());
                    break;
            }
        }

        private void ShowTriggered()
        {
            _counter.text = string.Empty;
            _condition.enabled = false;
            _body.sprite = SpriteOf(Type, triggered: true);
            if (!StaysAfterTrigger(Type))
            {
                gameObject.SetActive(false);
            }
        }

        private void Play(IEnumerator routine)
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                transform.localScale = Vector3.one;
            }

            _animation = StartCoroutine(routine);
        }

        private IEnumerator Bump()
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * (1f + (0.12f * Mathf.Sin(t / 0.2f * Mathf.PI)));
                yield return null;
            }

            transform.localScale = Vector3.one;
            _animation = null;
        }

        private IEnumerator SwingOpen()
        {
            Color start = _body.color;
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                transform.localScale = new Vector3(1f - (0.9f * k), 1f + (0.1f * k), 1f);
                _body.color = new Color(start.r, start.g, start.b, 1f - k);
                yield return null;
            }

            gameObject.SetActive(false);
            _animation = null;
        }

        private IEnumerator PopOpen()
        {
            Color start = _body.color;
            for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.45f;
                transform.localScale = new Vector3(1f + (0.15f * Mathf.Sin(k * Mathf.PI)), 1f + (0.35f * k), 1f);
                _body.color = new Color(start.r, start.g, start.b, 1f - (k * k));
                yield return null;
            }

            gameObject.SetActive(false);
            _animation = null;
        }

        private IEnumerator Glow()
        {
            Color start = _body.color;
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                _body.color = Color.Lerp(start, new Color(1f, 1f, 1f, 0f), k);
                transform.localScale = Vector3.one * (1f + (0.1f * k));
                yield return null;
            }

            gameObject.SetActive(false);
            _animation = null;
        }

        private IEnumerator Spray()
        {
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * (1f + (0.2f * Mathf.Abs(Mathf.Sin(t * 12f)) * (1f - (t / 0.8f))));
                yield return null;
            }

            transform.localScale = Vector3.one;
            _animation = null;
        }

        private static VariantVisual Visual(VariantVisualCatalog? visuals, Core.Variants.VariantId id) =>
            visuals != null ? visuals.Get(id) : VariantVisualCatalog.Default(id);
    }
}
