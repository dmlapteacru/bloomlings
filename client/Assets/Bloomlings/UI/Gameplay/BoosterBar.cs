using System;
using System.Collections;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Gameplay
{
    /// <summary>The state of one booster button.</summary>
    public readonly struct BoosterButtonState
    {
        public BoosterButtonState(bool unlocked, bool applicable, int charges, int price, bool affordable)
        {
            Unlocked = unlocked;
            Applicable = applicable;
            Charges = charges;
            Price = price;
            Affordable = affordable;
        }

        public bool Unlocked { get; }

        /// <summary>The level accepts it now (<c>LevelSession.Check</c>); otherwise the button is disabled (FR-046).</summary>
        public bool Applicable { get; }

        public int Charges { get; }

        public int Price { get; }

        public bool Affordable { get; }
    }

    /// <summary>
    /// The four booster buttons under the slots (T120). Each shows its owned count, or its Petal price when none is
    /// owned; a booster the level cannot use now is disabled (FR-046), and a locked one shows a lock until its unlock
    /// level (FR-042). Return and Bloom Burst then ask for a target (a slot, a tile); the controller runs that step.
    /// </summary>
    public sealed class BoosterBar : MonoBehaviour
    {
        private static readonly BoosterKind[] Order = { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst };

        private readonly Button[] _buttons = new Button[4];
        private readonly Image[] _icons = new Image[4];
        private readonly TextMeshProUGUI[] _badges = new TextMeshProUGUI[4];
        private readonly Image[] _locks = new Image[4];

        public static BoosterBar Create(RectTransform area, Action<BoosterKind> onPress)
        {
            var bar = area.gameObject.AddComponent<BoosterBar>();
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterKind kind = Order[i];
                Button button = UiFactory.CreateButton(kind.ToString(), area, string.Empty, UiTheme.Panel, () => onPress(kind));
                UiFactory.Place((RectTransform)button.transform, (i * 0.25f) + 0.02f, 0.05f, ((i + 1) * 0.25f) - 0.02f, 0.95f);
                bar._buttons[i] = button;
                bar._icons[i] = UiFactory.CreateImage("Icon", button.transform, Icon(kind), UiTheme.Accent);
                bar._icons[i].preserveAspect = true;
                UiFactory.Place(bar._icons[i].rectTransform, 0.05f, 0.1f, 0.45f, 0.9f);
                bar._badges[i] = UiFactory.CreateText("Badge", button.transform, string.Empty, 36f, UiTheme.Text);
                UiFactory.Place(bar._badges[i].rectTransform, 0.45f, 0f, 1f, 1f);
                bar._locks[i] = UiFactory.CreateImage("Lock", button.transform, ProceduralSprites.Lock, UiTheme.Text);
                bar._locks[i].preserveAspect = true;
                UiFactory.Place(bar._locks[i].rectTransform, 0.3f, 0.15f, 0.7f, 0.85f);
            }

            return bar;
        }

        /// <summary>The button of a booster, for demo pointers.</summary>
        public RectTransform RectOf(BoosterKind kind) => (RectTransform)_buttons[Array.IndexOf(Order, kind)].transform;

        public void Refresh(Func<BoosterKind, BoosterButtonState> state)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterButtonState s = state(Order[i]);
                _locks[i].enabled = !s.Unlocked;
                _icons[i].enabled = s.Unlocked;
                _badges[i].text = !s.Unlocked ? string.Empty
                    : s.Charges > 0 ? "×" + s.Charges.ToString(CultureInfo.InvariantCulture)
                    : s.Price.ToString(CultureInfo.InvariantCulture) + " ✿";
                _buttons[i].interactable = s.Unlocked && s.Applicable && (s.Charges > 0 || s.Affordable);
            }
        }

        /// <summary>Highlights the button whose target the player is choosing (Return, Bloom Burst).</summary>
        public void SetTargeting(BoosterKind? kind)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                _icons[i].color = kind == Order[i] ? UiTheme.Warning : UiTheme.Accent;
            }
        }

        /// <summary>A short pulse when a booster is used.</summary>
        public void Pulse(BoosterKind kind)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(PulseRoutine(_buttons[Array.IndexOf(Order, kind)].transform));
            }
        }

        private static IEnumerator PulseRoutine(Transform target)
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * (1f + (0.1f * Mathf.Sin(t / 0.2f * Mathf.PI)));
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private static Sprite Icon(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => ProceduralSprites.PlusSlot,
            BoosterKind.Shuffle => ProceduralSprites.ShuffleArrows,
            BoosterKind.Return => ProceduralSprites.ReturnArrow,
            _ => ProceduralSprites.Burst,
        };
    }
}
