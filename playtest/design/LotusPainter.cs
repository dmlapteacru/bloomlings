using Bloomlings.Client.UI.Design;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The lotus loader and the lotus iris (spec 005 FR-039, contracts/look.md §6.13; Unity's <c>LotusIrisView</c> is its
    /// twin): a <see cref="LotusPose"/> of the kit's <see cref="LotusIris"/> drawn over the screen, back to front: the
    /// parchment cover with its round hole and pink rim, the glow, the ring of petals, the lotus, the splash's logo and the
    /// text. While anything of the cover shows, it takes every tap.
    /// </summary>
    public static class LotusPainter
    {
        /// <summary>
        /// Draws <paramref name="pose"/>; <paramref name="text"/> is "Level N" (the transition) or "Loading..." (the
        /// splash, <paramref name="splash"/>, which also shows the logo).
        /// </summary>
        public static void Draw(IPainter p, LotusPose pose, string text, bool splash)
        {
            if (!pose.Covers)
            {
                return;
            }

            LotusIrisLayout l = LotusIris.Layout(p.Width, p.Height, p.Insets);
            var screen = new Box(0f, 0f, p.Width, p.Height);
            Cover(p, l, pose);
            if (splash)
            {
                p.Mark("brand.splash_art");
                p.Mark("bg.splash");
            }

            if (pose.GlowAlpha > 0f)
            {
                // A small picture scaled up: the light is soft all through.
                p.PushAlpha(pose.GlowAlpha);
                p.PushTransform(l.Glow.Left, l.Glow.Top, l.Glow.Width / LotusIris.GlowRaster, 0f, 0f);
                p.Picture(LotusIris.GlowKey, new Box(0f, 0f, LotusIris.GlowRaster, LotusIris.GlowRaster), (w, h) => UiRaster.IrisGlow(w, h));
                p.PopTransform();
                p.PopAlpha();
            }

            if (pose.RingAlpha > 0f)
            {
                Ring(p, l, pose);
            }

            if (pose.LotusAlpha > 0f)
            {
                p.PushAlpha(pose.LotusAlpha);
                p.PushRotate(pose.LotusTurn, l.CenterX, l.CenterY);
                p.PushTransform(0f, 0f, pose.LotusScale, l.CenterX, l.CenterY);
                Kit.Petal(p, l.Lotus);
                p.PopTransform();
                p.PopTransform();
                p.PopAlpha();
            }

            if (splash && pose.LogoAlpha > 0f)
            {
                ReferenceHomeRegions r = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p));
                Box logo = HomeScreen.LogoBox(p, r);
                p.PushAlpha(pose.LogoAlpha);
                p.PushTransform(0f, 0f, pose.LogoScale, logo.CenterX, logo.CenterY);
                HomeScreen.Wordmark(p, r);
                p.PopTransform();
                p.PopAlpha();
            }

            if (pose.TextAlpha > 0f)
            {
                p.PushAlpha(pose.TextAlpha);
                float y = l.TextY + p.U(pose.TextRise);
                if (splash)
                {
                    p.Text(text, l.CenterX, y, T.ButtonSecondary, LotusIris.TextColor(true), p.Width * 0.8f);
                }
                else
                {
                    p.Text(text, l.CenterX, y, T.LevelHome, LotusIris.TextColor(false), p.Width * 0.8f, LotusIris.LevelTextScale);
                }

                p.PopAlpha();
            }

            // The cover takes every tap while it shows (a second Next, a tap on the screen coming up under it).
            p.Hit(screen, () => { });
        }

        /// <summary>The cover: plain where the hole picture does not reach, the hole picture round the hole, the pink rim.</summary>
        private static void Cover(IPainter p, LotusIrisLayout l, LotusPose pose)
        {
            p.Mark("ui.lotus_iris");
            float radius = l.HoleRadius(pose);
            if (radius < 0.5f)
            {
                p.FillRect(new Box(0f, 0f, p.Width, p.Height), LotusIris.Cover);
                return;
            }

            Box hole = LotusIris.HoleBox(l.CenterX, l.CenterY, radius);
            foreach (Box panel in LotusIris.CoverPanels(p.Width, p.Height, hole))
            {
                p.FillRect(panel, LotusIris.Cover);
            }

            // The picture is rendered once at its own size and scaled to the hole.
            float k = hole.Width / LotusIris.HoleRaster;
            p.PushTransform(hole.Left, hole.Top, k, 0f, 0f);
            p.Picture(LotusIris.HoleKey, new Box(0f, 0f, LotusIris.HoleRaster, LotusIris.HoleRaster), (w, h) => UiRaster.IrisHole(w, h));
            p.PopTransform();

            p.StrokeCircle(l.CenterX, l.CenterY, radius + p.U(LotusIris.RimUnits / 2f), p.U(LotusIris.RimUnits), LotusIris.Rim);
            p.StrokeCircle(l.CenterX, l.CenterY, radius + p.U(LotusIris.RimUnits + (LotusIris.RimLineUnits / 2f)), p.U(LotusIris.RimLineUnits), LotusIris.RimLine);
        }

        /// <summary>The ring: twelve petals pointing outward, each over a larger copy in its outline color.</summary>
        private static void Ring(IPainter p, LotusIrisLayout l, LotusPose pose)
        {
            p.Mark(LotusIris.PetalShape);
            p.PushAlpha(pose.RingAlpha);
            for (int i = 0; i < LotusIris.PetalCount; i++)
            {
                (float x, float y, float turn) = l.PetalAt(i, pose.RingSpin);
                (float lit, float scale) = LotusIris.PetalState(i, pose.RingLit);
                float side = l.Petal * scale;
                p.PushRotate(turn, x, y);
                p.Shape(LotusIris.PetalShape, Box.FromCenter(x, y, side * LotusIris.PetalLineScale, side * LotusIris.PetalLineScale), LotusIris.PetalLine(lit));
                p.Shape(LotusIris.PetalShape, Box.FromCenter(x, y, side, side), LotusIris.PetalFill(lit));
                p.PopTransform();
            }

            p.PopAlpha();
        }
    }
}
