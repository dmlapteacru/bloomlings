using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// One board cell. A target shows a framed tile in its variant color with the variant icon (never a character
    /// face); open cells hide the tile so the finished picture shows through; stones show a gray block. A small corner
    /// marker previews the next hidden layer (FR-036).
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        private Image _frame = null!;
        private Image _fill = null!;
        private Image _icon = null!;
        private Image _peek = null!;
        private Image _keyMark = null!;
        private VariantVisualCatalog? _visuals;

        public CellPos Cell { get; private set; }

        /// <summary>The variant currently shown, or null when the tile is hidden.</summary>
        public VariantId? Shown { get; private set; }

        public static TileView Create(Transform parent, CellPos cell, VariantVisualCatalog? visuals)
        {
            Image frame = UiFactory.CreateImage($"Tile {cell.X},{cell.Y}", parent, ProceduralSprites.RoundedSquare, UiTheme.TileFrame);
            var view = frame.gameObject.AddComponent<TileView>();
            view._frame = frame;
            view._visuals = visuals;
            view.Cell = cell;
            view._fill = UiFactory.CreateImage("Fill", frame.transform, ProceduralSprites.RoundedSquare, Color.white);
            UiFactory.Place(view._fill.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
            view._icon = UiFactory.CreateImage("Icon", frame.transform, null, new Color(1f, 1f, 1f, 0.92f));
            UiFactory.Place(view._icon.rectTransform, 0.2f, 0.2f, 0.8f, 0.8f);
            view._icon.preserveAspect = true;
            view._peek = UiFactory.CreateImage("NextLayer", frame.transform, ProceduralSprites.Circle, Color.white);
            UiFactory.Place(view._peek.rectTransform, 0.64f, 0.64f, 0.98f, 0.98f);
            view._keyMark = UiFactory.CreateImage("Key", frame.transform, ProceduralSprites.Key, UiTheme.EntryMarker);
            UiFactory.Place(view._keyMark.rectTransform, 0.02f, 0.62f, 0.4f, 0.98f);
            return view;
        }

        public void ShowTarget(VariantId? visible, VariantId? next, string? keyId)
        {
            gameObject.SetActive(true);
            _frame.enabled = true;
            if (visible.HasValue)
            {
                VariantVisual visual = Visual(visible.Value);
                _fill.color = visual.Color;
                _icon.sprite = visual.Icon;
                _icon.enabled = true;
                _frame.color = UiTheme.Dark(visual.Color);
            }
            else
            {
                // A hidden mystery tile (FR-039): neutral tile with a question mark.
                _fill.color = UiTheme.SlotLocked;
                _icon.sprite = ProceduralSprites.Question;
                _icon.enabled = true;
                _frame.color = UiTheme.TileFrame;
            }

            _peek.enabled = next.HasValue;
            if (next.HasValue)
            {
                _peek.color = Visual(next.Value).Color;
            }

            _keyMark.enabled = keyId != null;
            Shown = visible;
        }

        public void ShowStone()
        {
            gameObject.SetActive(true);
            _frame.color = UiTheme.Dark(UiTheme.StoneColor);
            _fill.color = UiTheme.StoneColor;
            _icon.enabled = false;
            _peek.enabled = false;
            _keyMark.enabled = false;
            Shown = null;
        }

        /// <summary>Open ground: the tile disappears and the finished picture shows through.</summary>
        public void ShowOpen()
        {
            gameObject.SetActive(false);
            Shown = null;
        }

        private VariantVisual Visual(VariantId id) => _visuals != null ? _visuals.Get(id) : VariantVisualCatalog.Default(id);
    }
}
