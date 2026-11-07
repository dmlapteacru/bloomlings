using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <c>--island-stills &lt;levels dir&gt; &lt;out dir&gt;</c>: the water channel's island behind water (rules 17 §8
    /// «Остров за водой», the owner, 2026-11-07) on rebuilt round levels before they are transferred, for the owner's review.
    /// The levels dir holds <c>L####.json</c> (round-level.v1, written by <c>tools/rounds/island-stills.mjs</c> from
    /// <c>content/rounds/levels/</c>, so the game's <c>content/game/rounds</c> is never touched) and <c>L####.commands.json</c>
    /// (the level's saved solution: <c>[{"type": "tap", "index": k} | {"type": "bridge", "index": k} | {"type": "channel",
    /// "exit": i}]</c>). Each level is the game's own screen played by its rules: the start (the island ringed by water, both
    /// routes dotted), the saved solution played patiently until the spring is reached (both exits calling), then the
    /// solution's exit chosen by a tap on it and played on until a walker rides the stream. Review material only.
    /// </summary>
    public static class IslandStills
    {
        public static int Write(string levelsDir, string outDir)
        {
            string published = Path.Combine(FindRoot(), "content", "game", "rounds", "published");
            RoundContentSet rounds = RoundContentSet.Read(path =>
            {
                string file = Path.Combine(published, path);
                return File.Exists(file) ? File.ReadAllBytes(file) : null;
            });
            var empty = new ContentSet(0, 0, Array.Empty<Bloomlings.Core.Definitions.LevelDefinition>(), Array.Empty<Bloomlings.Core.Definitions.BasePicture>());
            Directory.CreateDirectory(outDir);
            int written = 0;
            foreach (string file in Directory.GetFiles(levelsDir, "L????.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                RoundLevel level = RoundJson.ReadLevel(File.ReadAllText(file));
                string tag = Path.GetFileNameWithoutExtension(file);
                var commands = JArray.Parse(File.ReadAllText(Path.Combine(levelsDir, tag + ".commands.json")));
                string data = Path.Combine(Path.GetTempPath(), "bloomlings-island-stills-" + Guid.NewGuid().ToString("N"));
                using var painter = new SkiaPainter(1080, 2340, new Insets(110, 63));
                var app = new DesignApp(data, empty, new Silence(), false, rounds: rounds);
                app.Meta.Save.Progression.HighestCompletedLevel = level.Number - 1;
                var s = new RoundLevelScreen(app, level.Number, level, null);
                app.ShowRound(s);
                s.SkipGuide();
                if (s.Intro != null)
                {
                    s.CloseIntro();
                }

                void Frame(float dt)
                {
                    painter.Now += dt;
                    painter.BeginFrame();
                    app.Draw(painter, dt);
                }

                void Run(float seconds)
                {
                    for (float t = 0f; t < seconds; t += 1f / 30f)
                    {
                        Frame(1f / 30f);
                    }
                }

                void Save(string name) => File.WriteAllBytes(Path.Combine(outDir, tag + "-" + name + ".png"), painter.Png());

                Run(1.0f);
                Save("1-start");

                // the saved solution, each command once the board has settled, until the spring is reached
                int c = 0;
                int exit = -1;
                bool Until(Func<bool> done, float limit = 60f)
                {
                    for (float t = 0f; t < limit; t += 1f / 30f)
                    {
                        Frame(1f / 30f);
                        if (done())
                        {
                            return true;
                        }

                        if (!s.Play.Timeline.Pending)
                        {
                            return false;
                        }
                    }

                    return false;
                }

                bool ready = false;
                for (; c < commands.Count && !ready; c++)
                {
                    ready = Until(() => s.Animator.ChannelReady);
                    if (ready)
                    {
                        break;
                    }

                    JObject command = (JObject)commands[c];
                    string type = (string)command["type"]!;
                    if (type == "channel")
                    {
                        exit = (int)command["exit"]!;
                        break;
                    }

                    if (type == "tap")
                    {
                        s.Tap((int)command["index"]!);
                    }
                    else
                    {
                        s.BuildBridge((int)command["index"]!);
                    }
                }

                if (!ready)
                {
                    ready = Until(() => s.Animator.ChannelReady);
                }

                if (!ready)
                {
                    Console.WriteLine(tag + ": the spring was not reached on the saved solution");
                    continue;
                }

                Run(1.2f);
                Save("2-ready");
                for (int k = c; k < commands.Count && exit < 0; k++)
                {
                    if ((string)commands[k]["type"]! == "channel")
                    {
                        exit = (int)commands[k]["exit"]!;
                    }
                }

                // the solution's exit chosen as a player does it, by a tap on the exit
                RoundPainter.LayoutCache cache = RoundPainter.Layout(painter, s, RoundPainter.HasBoosters(s), RoundPainter.Badge(s).HasValue);
                float min = painter.U(DesignTokens.Size.TouchMin);
                Box touch = cache.Channel!.Touch(Math.Max(0, exit), min);
                painter.Dispatch(touch.CenterX, touch.CenterY);
                if (s.Play.Timeline.State.Channel < 0)
                {
                    s.ChooseExit(Math.Max(0, exit));
                }

                // on with the solution after its exit until a walker rides the stream from the chosen exit
                int next = 0;
                for (int k = 0; k < commands.Count; k++)
                {
                    if ((string)commands[k]["type"]! == "channel")
                    {
                        next = k + 1;
                        break;
                    }
                }

                bool riding = false;
                for (int k = next; k <= commands.Count && !riding; k++)
                {
                    riding = Until(() => s.Animator.ShownWalkers.Any(w => w.RideUs > 0 && s.Animator.Now > w.Start - (w.RideUs * 6 / 10) && s.Animator.Now < w.Start - (w.RideUs * 3 / 10)), 30f);
                    if (riding || k == commands.Count)
                    {
                        break;
                    }

                    JObject command = (JObject)commands[k];
                    if ((string)command["type"]! == "tap")
                    {
                        s.Tap((int)command["index"]!);
                    }
                }

                Save("3-chosen");
                Console.WriteLine($"{tag}: exit {s.Play.Timeline.State.Channel}, riding {riding}");
                written++;
            }

            return written > 0 ? 0 : 1;
        }

        private static string FindRoot()
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "CLAUDE.md")))
            {
                dir = Path.GetDirectoryName(dir);
            }

            return dir ?? Directory.GetCurrentDirectory();
        }
    }
}
