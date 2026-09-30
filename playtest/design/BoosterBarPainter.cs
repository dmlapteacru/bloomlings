using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The booster bar of frame 14 (spec 002 FR-014).
    /// <list type="bullet">
    /// <item><description>It is hidden before the first booster unlocks.</description></item>
    /// <item><description>Each booster appears at its unlock level as a round button in its color.</description></item>
    /// <item><description>A dark badge shows the charges. With none left, a Petal price shows instead, so buying stays
    /// clear.</description></item>
    /// <item><description>A booster the level cannot use now is greyed (spec 001 FR-046).</description></item>
    /// <item><description>The booster whose target is being chosen is ringed.</description></item>
    /// </list>
    /// </summary>
    public static class BoosterBarPainter
    {
        public static void Draw(IPainter p, Box area, LevelScreen s)
        {
            var shown = new List<(BoosterKind Kind, Recovery Recovery, string Id)>();
            foreach ((BoosterKind kind, Recovery recovery, string id) in LevelScreen.Boosters)
            {
                if (s.Meta.Economy.IsUnlocked(kind))
                {
                    shown.Add((kind, recovery, id));
                }
            }

            if (shown.Count == 0)
            {
                return;
            }

            IReadOnlyList<Recovery> eligible = s.Session.EligibleRecoveries();
            float size = Math.Min(area.Height, p.U(DesignTokens.Size.BoosterButton));
            // Four places across the bar; unlocked boosters take theirs in order (frame 14).
            Box[] places = ScreenLayout.Row(area, 4, p.U(24f), p.U(220f), square: false);
            for (int i = 0; i < shown.Count; i++)
            {
                (BoosterKind kind, Recovery recovery, string id) = shown[i];
                Box place = places[i];
                float cx = place.CenterX;
                float cy = area.CenterY;
                bool applicable = Applicable(s, recovery, eligible);
                int charges = s.Meta.Economy.Charges(kind);
                bool affordable = charges > 0 || s.Meta.Economy.CanAfford(kind);
                bool enabled = applicable && affordable && s.Session.Status != LevelStatus.Won;
                Button(p, cx, cy, size, id, enabled, s.Targeting == recovery, charges, s.Meta.Economy.Price(kind), enabled ? () => s.PressBooster(kind, recovery) : (Action?)null);
            }
        }

        /// <summary>One round booster button with its count or price (also used by the Store and the jam sheet).</summary>
        public static void Button(IPainter p, float cx, float cy, float size, string id, bool enabled, bool targeting, int charges, int price, Action? action)
        {
            Rgba color = DesignTokens.BoosterColor(id);
            p.PushAlpha(enabled ? 1f : 0.45f);
            Box box = Box.FromCenter(cx, cy, size, size);
            bool pressed = action != null && p.Pressed(box);
            float lift = p.U(7f);
            if (targeting)
            {
                p.FillCircle(cx, cy, (size / 2f) + p.U(10f), color.WithAlpha(0.3f));
            }

            if (!pressed)
            {
                p.FillCircle(cx, cy + (lift / 2f), size / 2f, color.Darken(0.3f));
            }

            float faceY = pressed ? cy + (lift / 2f) : cy - (lift / 2f);
            p.FillCircle(cx, faceY, (size / 2f) - p.U(2f), color);
            p.FillCircle(cx, faceY, (size / 2f) - p.U(8f), color.Lighten(0.08f));
            p.Shape("booster." + id, Box.FromCenter(cx, faceY, size * 0.54f, size * 0.54f), id == "bloom_burst" ? C.PetalCenter : Rgba.White);
            p.PopAlpha();

            float badge = size * 0.36f;
            if (charges > 0)
            {
                Kit.CountBadge(p, cx + (size * 0.36f), cy + (size * 0.34f), badge, charges.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                string text = NumberText.Group(price);
                float w = p.MeasureText(text, T.Badge, badge / p.U(48f)) + (badge * 1.3f);
                Box pill = Box.FromCenter(cx + (size * 0.3f), cy + (size * 0.36f), w, badge);
                p.FillRound(pill.Inset(-p.U(3f)), (badge / 2f) + p.U(3f), Rgba.White);
                p.FillRound(pill, badge / 2f, C.BadgeCount);
                Kit.Petal(p, Box.FromCenter(pill.Left + (badge * 0.5f), pill.CenterY, badge * 0.8f, badge * 0.8f));
                p.Text(text, pill.Left + (badge * 0.95f) + ((pill.Width - (badge * 1.1f)) / 2f), pill.CenterY, T.Badge, C.TextOnColor, pill.Width - badge, sizeScale: badge / p.U(48f));
            }

            if (action != null)
            {
                p.Hit(Kit.Touch(p, box), action);
            }
        }

        private static bool Applicable(LevelScreen s, Recovery recovery, IReadOnlyList<Recovery> eligible)
        {
            if (s.Session.Status == LevelStatus.Jammed || s.Session.Status == LevelStatus.Stuck)
            {
                return eligible.Contains(recovery);
            }

            return recovery switch
            {
                Recovery.ExtraSlot => s.Session.Check(new UseExtraSlot()).IsAllowed,
                Recovery.Shuffle => s.Session.Check(new UseShuffle()).IsAllowed,
                _ => true,
            };
        }
    }
}
