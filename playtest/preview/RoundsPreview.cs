using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--rounds [dir]</c> (spec 006 T025, T028, T029): the full playtest as the APKs run it, on a published round set
    /// (default <c>content/work/rounds-published</c>, from <c>playtest/check -- --publish-dev</c>, else
    /// <c>content/game/rounds/published</c>): Home without the Daily Challenge, Play opening round level 1 (the placeholder
    /// until the level screen plays round levels), a few round levels, the dev row's jump past the last level and the "new
    /// levels soon" card. Writes <c>rounds-*.png</c> and checks the navigation; a quick look, not part of the board.
    /// </summary>
    public static class RoundsPreview
    {
        public static int Write(string root, string outDir, string? folder)
        {
            folder ??= new[] { Path.Combine(root, "content/work/rounds-published"), Path.Combine(root, "content/game/rounds/published") }
                .FirstOrDefaultExisting();
            if (folder == null || !File.Exists(Path.Combine(folder, RoundContentManifest.FileName)))
            {
                Console.WriteLine("No published round set: run `dotnet run --project playtest/check -- --publish-dev content/work/rounds-published` first.");
                return 1;
            }

            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(folder, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            string data = Path.Combine(Path.GetTempPath(), "bloomlings-rounds-" + Guid.NewGuid().ToString("N"));
            var problems = new List<string>();
            void Expect(bool ok, string what)
            {
                if (!ok)
                {
                    problems.Add(what);
                }
            }

            Directory.CreateDirectory(outDir);
            try
            {
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                void Shot(string name)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        painter.Now += 0.5f;
                        painter.BeginFrame();
                        app.Draw(painter, 0.5f);
                    }

                    File.WriteAllBytes(Path.Combine(outDir, "rounds-" + name + ".png"), painter.Png());
                }

                Expect(!app.DailyChallengeAvailable && !HomeScreen.Look(app).DailyChallenge, "the Daily Challenge is off with round content");
                Shot("home");
                app.StartLevel();
                Expect(app.Screen == Screen.RoundLevel && app.RoundLevelNumber == 1, "Play opens round level 1");
                Shot("level-01");
                foreach (int n in new[] { 6, 11, 16, rounds.LevelCount })
                {
                    app.LoadLevel(n);
                    Expect(app.Screen == Screen.RoundLevel && app.RoundLevelNumber == n, "round level " + n + " opens");
                    Shot("level-" + n.ToString("00", System.Globalization.CultureInfo.InvariantCulture));
                }

                Expect(app.Back() && app.Screen == Screen.Home, "the system back leaves a round level for Home");
                app.OpenOverlay(Overlay.Settings);
                Shot("settings");
                Expect(app.JumpToLevel("50") && app.Meta.CurrentLevel == 50, "the dev row's jump sets the save's progress past the last level");
                Expect(!app.JumpToLevel("x") && app.Meta.CurrentLevel == 50, "a jump to no number changes nothing");
                app.GoHome();
                app.StartLevel();
                Expect(app.Screen == Screen.Home && app.IsOpen(Overlay.NewLevelsSoon), "Play past the last round level says \"new levels soon\"");
                Shot("new-levels-soon");
                app.CloseOverlay();
                app.LoadLevel(rounds.LevelCount + 1);
                Expect(app.Screen == Screen.Home && app.IsOpen(Overlay.NewLevelsSoon), "no level follows the last round level");
                app.Dispose();
            }
            finally
            {
                if (Directory.Exists(data))
                {
                    Directory.Delete(data, true);
                }
            }

            foreach (string problem in problems)
            {
                Console.WriteLine("FAIL rounds: " + problem);
            }

            Console.WriteLine($"rounds: {rounds.LevelCount} round levels from {folder}; frames in {outDir}" + (problems.Count == 0 ? "; all checks pass" : string.Empty));
            return problems.Count == 0 ? 0 : 1;
        }

        private static string? FirstOrDefaultExisting(this IEnumerable<string> folders)
        {
            foreach (string folder in folders)
            {
                if (File.Exists(Path.Combine(folder, RoundContentManifest.FileName)))
                {
                    return folder;
                }
            }

            return null;
        }
    }
}
