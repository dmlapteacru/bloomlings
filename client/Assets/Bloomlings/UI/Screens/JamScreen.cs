using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The jam bottom sheet of the design board's frame 10 (spec 002 FR-019; spec 001 FR-027, T051, T121).
    /// <list type="bullet">
    /// <item><description>"NO MOVES LEFT" with "Use a booster to continue".</description></item>
    /// <item><description>One tile per usable recovery booster, with its icon, name and cost.</description></item>
    /// <item><description>The Free rescue (a rewarded ad, once per attempt) when offered.</description></item>
    /// <item><description>Restart.</description></item>
    /// </list>
    /// The sheet covers only the bottom of the screen, so the board stays visible. It never opens the Store.
    /// </summary>
    public sealed class JamScreen : MonoBehaviour
    {
        private SheetView _sheet = null!;
        private RectTransform _options = null!;
        private Button _rescue = null!;
        private Action<Recovery> _onRecovery = _ => { };
        private Action? _watch;

        public static JamScreen Create(Transform parent, Action onRestart, Action<Recovery> onRecovery)
        {
            float content = 250f + 30f + DesignTokens.Size.PrimaryHeightSmall + 24f + DesignTokens.Size.SecondaryHeight + 20f;
            SheetView sheet = UiKit.Sheet("JamScreen", parent, Loc.T("jam.title"), Loc.T("jam.subtitle"), content);
            var screen = sheet.Root.AddComponent<JamScreen>();
            screen._sheet = sheet;
            screen._onRecovery = onRecovery;
            Box body = sheet.Regions.Body;
            float u = DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            screen._options = UiKit.PlaceBox(UiFactory.CreateRect("Recoveries", sheet.Body), new Box(body.Left, body.Top, body.Right, body.Top + (250f * u)), body);
            float y = body.Top + (280f * u);
            screen._rescue = UiKit.PrimaryButton("Rescue", sheet.Body, Loc.T("jam.rescue"), () => screen._watch?.Invoke());
            UiKit.PlaceBox((RectTransform)screen._rescue.transform, new Box(body.Left + (20f * u), y, body.Right - (20f * u), y + (DesignTokens.Size.PrimaryHeightSmall * u)), body);
            y += (DesignTokens.Size.PrimaryHeightSmall + 24f) * u;
            Button restart = UiKit.SecondaryButton("Restart", sheet.Body, Loc.T("common.restart"), onRestart, "ui.restart");
            UiKit.PlaceBox((RectTransform)restart.transform, new Box(body.Left + (20f * u), y, body.Right - (20f * u), y + (DesignTokens.Size.SecondaryHeight * u)), body);
            sheet.Root.SetActive(false);
            return screen;
        }

        /// <param name="recoveries">Only the recoveries the player can use now: owned, or affordable with Petals (FR-027).</param>
        /// <param name="label">The tile text, e.g. "Extra Slot ×1" or "Shuffle 40 ✿".</param>
        /// <param name="rescue">The rewarded rescue (a free booster use, once per attempt), or null when not offered.</param>
        public void Show(bool stuck, IReadOnlyList<Recovery> recoveries, Func<Recovery, string>? label = null, (string Label, Action Watch)? rescue = null)
        {
            label ??= Label;
            _sheet.Title.text = stuck ? Loc.T("jam.stuck") : Loc.T("jam.title");
            for (int i = _options.childCount - 1; i >= 0; i--)
            {
                Destroy(_options.GetChild(i).gameObject);
            }

            float width = recoveries.Count == 0 ? 0f : 1f / recoveries.Count;
            for (int i = 0; i < recoveries.Count; i++)
            {
                Recovery recovery = recoveries[i];
                Image tile = UiKit.Raised(recovery.ToString(), _options, Color.white, UiTheme.PanelEdge, pill: false, radiusUnits: 36f, raycast: true);
                RectTransform root = (RectTransform)tile.transform.parent;
                UiFactory.Place(root, (i * width) + 0.02f, 0f, ((i + 1) * width) - 0.02f, 1f);
                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = tile;
                button.onClick.AddListener(() => _onRecovery(recovery));
                root.gameObject.AddComponent<PressMotion>();
                string id = Id(recovery);
                Image disc = UiFactory.CreateImage("Icon", tile.transform, ProceduralSprites.Circle, UiTheme.Of(DesignTokens.BoosterColor(id)));
                disc.preserveAspect = true;
                UiFactory.Place(disc.rectTransform, 0.25f, 0.5f, 0.75f, 0.94f);
                Image glyph = UiFactory.CreateImage("Glyph", disc.transform, ProceduralSprites.Shape("booster." + id), id == "bloom_burst" ? UiTheme.PetalCenter : Color.white);
                glyph.preserveAspect = true;
                UiFactory.Place(glyph.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);
                TextMeshProUGUI text = UiKit.Label("Label", tile.transform, label(recovery), DesignTokens.Type.Caption, UiTheme.Text);
                UiFactory.Place(text.rectTransform, 0.04f, 0.06f, 0.96f, 0.46f);
            }

            _watch = rescue?.Watch;
            _rescue.gameObject.SetActive(rescue.HasValue);
            _sheet.Root.SetActive(true);
        }

        public void Hide() => _sheet.Root.SetActive(false);

        public static string Label(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => Loc.T("booster.extra_slot"),
            Recovery.Shuffle => Loc.T("booster.shuffle"),
            Recovery.Return => Loc.T("booster.return"),
            _ => Loc.T("booster.bloom_burst"),
        };

        private static string Id(Recovery recovery) => recovery switch
        {
            Recovery.ExtraSlot => "extra_slot",
            Recovery.Shuffle => "shuffle",
            Recovery.Return => "return",
            _ => "bloom_burst",
        };
    }
}
