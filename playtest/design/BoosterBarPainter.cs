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
    /// The booster bar of frame 14 (spec 002 FR-014) as booster tiles (spec 003 FR-031, contracts/booster-tile.md).
    /// <list type="bullet">
    /// <item><description>It is hidden before the first booster unlocks.</description></item>
    /// <item><description>Each booster appears at its unlock level as a tile: a cream plate, a raised tile in its color
    /// and a large light icon.</description></item>
    /// <item><description>A "×N" badge shows the charges. With none left, a price tag and a green "+" show instead, so
    /// buying stays clear.</description></item>
    /// <item><description>A booster the level cannot use now is greyed (spec 001 FR-046).</description></item>
    /// <item><description>The booster whose target is being chosen is raised, with a pulsing golden ring.</description></item>
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
            // Four places across the bar; unlocked boosters take theirs in order (frame 14).
            Box[] places = ScreenLayout.Row(area, 4, p.U(24f), p.U(220f), square: false);
            for (int i = 0; i < shown.Count; i++)
            {
                (BoosterKind kind, Recovery recovery, string id) = shown[i];
                int charges = s.Meta.Economy.Charges(kind);
                var state = new BoosterTileState(
                    charges,
                    s.Meta.Economy.Price(kind),
                    selected: s.Targeting == recovery,
                    usable: Applicable(s, recovery, eligible) && s.Session.Status != LevelStatus.Won,
                    affordable: charges > 0 || s.Meta.Economy.CanAfford(kind));
                Tile(p, Fit(p, places[i], area), id, state, state.Disabled ? (Action?)null : () => s.PressBooster(kind, recovery));
            }
        }

        /// <summary>The tile box at a bar place: <c>size.booster_tile</c>, shrunk to fit the bar.</summary>
        public static Box Fit(IPainter p, Box place, Box area)
        {
            float k = Math.Min(1f, Math.Min(place.Width / p.U(DesignTokens.Size.BoosterTileWidth + 24f), area.Height / p.U(DesignTokens.Size.BoosterTileHeight + 16f)));
            return Box.FromCenter(place.CenterX, area.CenterY - p.U(6f), p.U(DesignTokens.Size.BoosterTileWidth) * k, p.U(DesignTokens.Size.BoosterTileHeight) * k);
        }

        /// <summary>One booster tile in its state (contracts/booster-tile.md "Layers").</summary>
        public static void Tile(IPainter p, Box box, string id, BoosterTileState state, Action? action)
        {
            p.Mark("booster." + id);
            float k = box.Width / p.U(DesignTokens.Size.BoosterTileWidth);
            ColorSet set = GardenLook.Booster(id);
            if (state.Disabled)
            {
                set = set.Disabled();
            }

            float depth = Kit.Press(p, box, action != null);
            Box tile = state.Selected ? box.Offset(0f, -p.U(12f) * k) : box;
            p.PushAlpha(state.Disabled ? 0.45f : 1f);
            if (state.Selected)
            {
                // The selected glow: a golden halo pulsing every motion.glow, and a ring around the plate.
                float glow = GardenLook.Glow(p.Now);
                float radius = p.U(40f) * k;
                for (int ring = 3; ring >= 1; ring--)
                {
                    float grow = p.U(28f) * k * ring / 3f;
                    p.FillRound(tile.Inset(-grow), radius + grow, C.GardenGlow.WithAlpha(0.22f * glow));
                }

                p.StrokeRound(tile.Inset(-p.U(5f) * k), radius + (p.U(5f) * k), p.U(5f) * k, C.GardenGlow.WithAlpha(glow));
            }

            Kit.Squash(p, tile, depth, tile: true);
            Box plate = Kit.Plate(p, tile, p.U(40f) * k);
            Box face = Kit.Face(p, plate.Inset(p.U(10f) * k), set, p.U(30f) * k, depth, highlight: !state.Disabled, lipUnits: 14f * k);
            float icon = Math.Min(face.Width, face.Height) * 0.72f;
            Box iconBox = Box.FromCenter(face.CenterX, face.CenterY, icon, icon);
            if (!state.Disabled)
            {
                p.Shape("booster." + id, iconBox.Offset(0f, icon * 0.06f), set.Line);
            }

            p.Shape("booster." + id, iconBox, id == "bloom_burst" && !state.Disabled ? C.PetalCenter : Rgba.FromHex("#FFFBEF"));
            p.PopTransform();

            if (state.ShowsCharges)
            {
                float badge = p.U(54f) * k;
                Kit.CountBadge(p, tile.Right - (badge * 0.5f) + (p.U(12f) * k), tile.Top + (badge * 0.5f) - (p.U(12f) * k), badge, "×" + state.Charges.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                var tag = Box.FromCenter(tile.CenterX, tile.Bottom + (p.U(10f) * k), p.U(96f) * k, p.U(46f) * k);
                Kit.PriceTag(p, tag, state.Price);
                float plus = p.U(46f) * k;
                Box plusBox = Box.FromCenter(tile.Right - (plus * 0.5f) + (p.U(10f) * k), tile.Top + (plus * 0.5f) - (p.U(10f) * k), plus, plus);
                p.FillCircle(plusBox.CenterX, plusBox.CenterY + (plus * 0.06f), (plus / 2f) + p.U(2f), GardenLook.Green.Line);
                p.FillCircle(plusBox.CenterX, plusBox.CenterY, plus / 2f, GardenLook.Green.Face);
                p.Shape("ui.plus", plusBox.Inset(plus * 0.22f), Rgba.White);
            }

            p.PopAlpha();
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
