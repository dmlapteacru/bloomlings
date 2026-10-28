using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// PLACEHOLDER (spec 006, until T026 runs the level screen on the round timeline): a round level shown still, so a
    /// build with round content only opens its levels without the old rules. It draws the gameplay lawn, the top bar (Pause,
    /// the level's sign, the class badge), the round board in the game's frame and cubes (<see cref="Board"/>, through
    /// <see cref="RoundBoardLayout"/>) and the empty tray with a note that play is not wired yet. It holds no rules and
    /// takes no tap but Pause (whose Home and the system back return Home). <see cref="DesignApp"/> opens it from one place
    /// (<c>OpenRound</c>), which the round level screen replaces.
    /// </summary>
    public static class RoundLevelPlaceholder
    {
        /// <summary>The level <see cref="DesignApp.RoundLevelNumber"/> names, as the screen draws it.</summary>
        public static void Draw(IPainter p, DesignApp app) => Draw(p, app, app.RoundLevelNumber);

        /// <summary>Round level <paramref name="number"/> (also drawn ahead, for its pictures, by the splash and an idle Home).</summary>
        public static void Draw(IPainter p, DesignApp app, int number)
        {
            if (app.Rounds == null || !app.Rounds.Has(number))
            {
                return;
            }

            RoundLevel level = app.Rounds.GetLevel(number);
            DifficultyClass difficulty = level.Class switch
            {
                RoundClass.Hard => DifficultyClass.Hard,
                RoundClass.SuperHard => DifficultyClass.SuperHard,
                _ => DifficultyClass.Normal,
            };
            (string Text, Rgba Color, string Slot)? badge = Visuals.DifficultyBadge(difficulty);
            ReferenceGameplayRegions r = ScreenLayout.ReferenceGameplay(p.Width, p.Height, p.Insets, Math.Max(1, level.Tray.Count), level.Capacity, hasBoosters: false, hasBadge: badge.HasValue);
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, number);

            Box pause = r.Pause;
            Kit.RoundButton(p, pause.CenterX, pause.CenterY, pause.Height, "ui.pause", () => app.OpenOverlay(Overlay.Pause));
            p.Mark("ui.pause");
            Kit.LevelPill(p, r.Sign, PlaytestText.F("common.level", NumberText.Group(number)), difficulty == DifficultyClass.SuperHard);
            if (badge.HasValue)
            {
                Kit.Badge(p, r.Badge, badge.Value.Text, badge.Value.Color, badge.Value.Slot);
            }

            Board(p, RoundBoardLayout.In(r, level), app.Meta.Save.Settings.Symbols);
            LevelScreen.TrayPanel(p, r);
            Box note = GameplaySkin.SlotPanel(r);
            float width = note.Width * 0.86f;
            var lines = EndCards.Lines(p, PlaytestText.F("round.placeholder", number), T.Caption, width, 2);
            float line = Math.Min(p.U(50f), note.Height / (lines.Count + 0.8f));
            float y = note.CenterY - ((lines.Count - 1) * line / 2f);
            foreach (string text in lines)
            {
                p.Text(text, note.CenterX, y, T.Caption, C.InkBrown, width);
                y += line;
            }
        }

        /// <summary>
        /// A round board at the start of the level (spec 006 FR-040, FR-041): the game's stone border, grass on the path,
        /// water on the stream and its bridge sites, every material cell a plain cube in its round colour (its symbol only in
        /// the symbols mode), and an entry arch at each gate.
        /// </summary>
        public static void Board(IPainter p, RoundBoardLayout board, bool symbolsMode)
        {
            Kit.StoneBorder(p, board.Grid, board.Cell);
            for (int y = 1; y < board.Height - 1; y++)
            {
                for (int x = 1; x < board.Width - 1; x++)
                {
                    int cell = (y * board.Width) + x;
                    Box full = board.CellBox(cell);
                    switch (board.KindOf(cell))
                    {
                        case RoundCellKind.Stream:
                        case RoundCellKind.BridgeSite:
                            Kit.Water(p, full, x);
                            break;
                        case RoundCellKind.Material:
                            Kit.MaterialTile(p, board.CubeBox(cell), RoundMaterial.All[board.MaterialOf(cell)], RoundLook.CubeStyle, symbolsMode);
                            break;
                        default:
                            Kit.GrassCell(p, full, UiRaster.GrassSeed(x, y));
                            break;
                    }
                }
            }

            foreach (RoundGate gate in board.Gates)
            {
                Kit.EntryArch(p, gate.Arch);
            }
        }
    }
}
