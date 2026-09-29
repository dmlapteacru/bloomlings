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
    /// A special object on the board (T108): the Garden Gate / hedge seal or the Fountain, drawn over its cells. Its
    /// condition is always visible (FR-037, FR-038): a key for a key door, the exact variant's icon for "restore N
    /// &lt;variant&gt; around it", and the counter. When it triggers, a gate swings open and fades; a Fountain sprays and
    /// stays as a landmark.
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
            bool fountain = special.Type == SpecialType.Fountain;
            Image body = UiFactory.CreateImage("Special " + special.Id, parent, fountain ? ProceduralSprites.Fountain : ProceduralSprites.Gate, fountain ? UiTheme.FountainColor : UiTheme.GateColor);
            body.preserveAspect = true;
            var view = body.gameObject.AddComponent<SpecialView>();
            view._body = body;
            view.SpecialId = special.Id;
            view.Type = special.Type;
            view.Cells = special.Cells;

            SpecialCondition condition = special.Condition;
            Sprite? conditionSprite = condition.Kind == SpecialConditionKind.Key
                ? ProceduralSprites.Key
                : condition.Variant.HasValue ? Visual(visuals, condition.Variant.Value).Icon : null;
            Color conditionColor = condition.Variant.HasValue ? Visual(visuals, condition.Variant.Value).Color : UiTheme.EntryMarker;
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

        /// <summary>The trigger animation; a gate then disappears (its cells are open ground now).</summary>
        public void Trigger()
        {
            _counter.text = string.Empty;
            _condition.enabled = false;
            if (isActiveAndEnabled)
            {
                Play(Type == SpecialType.Fountain ? Spray() : SwingOpen());
            }
            else
            {
                ShowTriggered();
            }
        }

        private void ShowTriggered()
        {
            _counter.text = string.Empty;
            _condition.enabled = false;
            if (Type != SpecialType.Fountain)
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
