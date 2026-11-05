using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Rounds;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// The water channel and the wooden shutters in the game (spec 006 FR-069, FR-070; the owner's looks A and B of
    /// 2026-11-05; frames 277–281). There are no published levels with them yet (the wave L101–L150 is still being built), so
    /// the frames play two dev levels made here from published pictures, as the look mock did: Level 76's airship with a
    /// spring behind three green cubes of its top edge, an exit in a pocket in its cream middle and one by its nose (shown as
    /// "Level 101"), and Level 91's picture with a shutter over a block of 8 × 5 cubes counting 30 green ones (shown as
    /// "Level 121"). Every frame is the game's own screen, played by the rules: the channel waiting (both routes dotted), ready
    /// (the exits calling), chosen by a tap on its exit with a walker riding the stream in its bubble; the shutter counting
    /// with a spark flying to its tag, and opening.
    /// </summary>
    public static partial class Fixtures
    {
        public static IEnumerable<Fixture> ChannelShutters(string root)
        {
            RoundContentSet? rounds = null;
            RoundContentSet Rounds()
            {
                string folder = Path.Combine(root, "content", "game", "rounds", "published");
                return rounds ??= RoundContentSet.Read(path =>
                {
                    string file = Path.Combine(folder, path);
                    return File.Exists(file) ? File.ReadAllBytes(file) : null;
                });
            }

            (DesignApp App, RoundLevelScreen Screen) Open(SkiaPainter p, string data, RoundLevel level, bool intro = false)
            {
                var empty = new ContentSet(0, 0, Array.Empty<LevelDefinition>(), Array.Empty<BasePicture>());
                var app = new DesignApp(data, empty, new Silence(), false, rounds: Rounds());
                app.Meta.Save.Progression.HighestCompletedLevel = level.Number - 1;
                var s = new RoundLevelScreen(app, level.Number, level, null);
                app.ShowRound(s);
                s.SkipGuide();
                if (s.Intro != null && !intro)
                {
                    s.CloseIntro();
                }

                Run(app, p, 0.1f);
                return (app, s);
            }

            ChannelLook Look(SkiaPainter p, RoundLevelScreen s) =>
                RoundPainter.Layout(p, s, RoundPainter.HasBoosters(s), RoundPainter.Badge(s).HasValue).Channel!;

            // Plays until the animator shows the spring reached, then a little more.
            void UntilReady(DesignApp app, SkiaPainter p, RoundLevelScreen s)
            {
                TapTop(s, 0);
                for (int i = 0; i < 600 && !s.Animator.ChannelReady; i++)
                {
                    Run(app, p, Frame);
                }

                Expect(s.Animator.ChannelReady, "the walkers reach the spring and the channel is ready");
                Run(app, p, 1.2f);
            }

            yield return new Fixture(277, "channel-waiting", "Water channel: waiting, both routes dotted, the spring still, the exits faint (spec 006 FR-069)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ChannelLevel(Rounds()));
                Run(app, p, 1.0f);
                Expect(p.Slots.Contains(ChannelLook.Slot), "the channel is drawn");
                Expect(!s.Animator.ChannelReady && s.ChannelChoices().Count == 0, "nothing to choose before the spring is reached");
            });

            yield return new Fixture(278, "channel-ready", "Water channel: the spring reached, both exits calling (spec 006 FR-069)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ChannelLevel(Rounds()));
                UntilReady(app, p, s);
                Expect(s.ChannelChoices().Count == 2, "both exits can be chosen");
                Expect(!s.Paused, "the game keeps running while the exits call");
            });

            yield return new Fixture(279, "channel-ride", "Water channel: an exit chosen by a tap, the stream, a walker riding it in a bubble (spec 006 FR-069)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ChannelLevel(Rounds()));
                UntilReady(app, p, s);
                ChannelLook look = Look(p, s);
                float min = p.U(DesignTokens.Size.TouchMin);
                Box touch = look.Touch(0, min);
                Expect(touch.Width >= min && touch.Height >= min, "an exit's touch target is at least the touch minimum");
                Tap(p, touch);
                Expect(s.Play.Timeline.State.Channel == 0, "a tap on the inner exit chooses it (channel exit=0)");
                Run(app, p, 1.2f);
                Expect(s.Animator.ChannelExit == 0 && s.ChannelChoices().Count == 0, "the choice is final");
                TapTop(s, 1);
                bool riding = false;
                for (int i = 0; i < 300 && !riding; i++)
                {
                    Run(app, p, Frame);
                    foreach (RoundWalk walk in s.Animator.ShownWalkers)
                    {
                        long t = s.Animator.Now;
                        riding |= walk.RideUs > 0 && t > walk.Start - (walk.RideUs * 6 / 10) && t < walk.Start - (walk.RideUs * 3 / 10);
                    }
                }

                Expect(riding, "a walker from the chosen exit rides the stream");
            });

            yield return new Fixture(280, "shutters-counting", "Wooden shutters: the crate lid, its tag counting the green cubes, a spark flying to it (spec 006 FR-070)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ShutterLevel(Rounds()));
                Expect(p.Slots.Contains(ShutterLook.Slot), "the shutter is drawn");
                Expect(s.Animator.ShutterLeft(0) == 30, "the tag starts at its count");
                TapTop(s, 0);
                Run(app, p, 0.8f);
                TapTop(s, 1);
                Run(app, p, 0.8f);
                TapTop(s, 2);
                bool counting = false;
                for (int i = 0; i < 900 && !counting; i++)
                {
                    Run(app, p, Frame);
                    long at = s.Animator.ShutterCountedAt(0);
                    int left = s.Animator.ShutterLeft(0);
                    counting = left <= 22 && left >= 16 && at != RoundAnimator.Before && s.Animator.Now - at > 150_000 && s.Animator.Now - at < 280_000;
                }

                Expect(counting, "the tag counts the green cubes cleared anywhere");
                Expect(s.Animator.ShutterClosed(0) && s.Play.Timeline.State.ShutterLeft(0) == s.Animator.ShutterLeft(0), "the tag shows the rules' count");
            });

            yield return new Fixture(282, "channel-intro", "Water channel: its introduction card on its first level (L101; spec 006 FR-069)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ChannelLevel(Rounds(), new RoundIntroduction(RoundIntroCards.Canal, false)), intro: true);
                Expect(s.Intro != null && s.Intro.Picture == RoundIntroPicture.Channel, "the channel's card opens on its first level");
                Run(app, p, 2.2f);
                Expect(Shows(p, "The water channel"), "the card shows its title");
            });

            yield return new Fixture(283, "shutters-intro", "Wooden shutters: their introduction card on their first level (L121; spec 006 FR-070)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ShutterLevel(Rounds(), new RoundIntroduction(RoundIntroCards.Shutters, false)), intro: true);
                Expect(s.Intro != null && s.Intro.Picture == RoundIntroPicture.Shutters, "the shutters' card opens on their first level");
                Run(app, p, 1.5f);
                Expect(Shows(p, "The crate"), "the card shows its title");
            });

            yield return new Fixture(281, "shutters-opening", "Wooden shutters: the count reached, the slats popping off (spec 006 FR-070)", (p, data) =>
            {
                (DesignApp app, RoundLevelScreen s) = Open(p, data, ShutterLevel(Rounds()));
                TapTop(s, 0);
                Run(app, p, 0.8f);
                TapTop(s, 1);
                Run(app, p, 0.8f);
                TapTop(s, 2);
                for (int i = 0; i < 1800 && s.Animator.ShutterOpenSince(0) < ShutterLook.OpenSeconds * 0.42f; i++)
                {
                    Run(app, p, Frame);
                }

                Expect(s.Animator.ShutterOpenSince(0) >= 0f, "the shutter opens when its count is reached");
            });
        }

        // Taps the pod on top of a column that the rules take now.
        private static void TapTop(RoundLevelScreen s, int column)
        {
            foreach (int pod in s.Round.Tray[column])
            {
                if (s.Play.Check(RoundAction.Tap(pod)) == RoundRefusal.None)
                {
                    s.Tap(pod);
                    return;
                }
            }
        }

        private static char[][] RowsTop(RoundLevel src)
        {
            var rows = new char[src.Height][];
            for (int y = 0; y < src.Height; y++)
            {
                rows[y] = src.RowsBottomFirst[src.Height - 1 - y].ToCharArray();
            }

            return rows;
        }

        private static int CellTop(RoundLevel level, int x, int yTop) => ((level.Height - 1 - yTop) * level.Width) + x;

        /// <summary>
        /// A dev level from <paramref name="src"/>'s picture (rows top first, changed by the caller): the given pods lead their
        /// columns and every material's rest follows in pods of at most 50 (the rules' exact balance).
        /// </summary>
        private static RoundLevel DevLevel(RoundLevel src, int number, char[][] rowsTop, (char Code, int Count)[][] columns, RoundChannel? channel = null, IReadOnlyList<RoundShutter>? shutters = null, RoundIntroduction? introduction = null)
        {
            var bottom = new List<string>();
            for (int y = rowsTop.Length - 1; y >= 0; y--)
            {
                bottom.Add(new string(rowsTop[y]));
            }

            var left = new Dictionary<char, int>();
            foreach (char[] row in rowsTop)
            {
                foreach (char ch in row)
                {
                    if (ch != '#' && ch != '.')
                    {
                        left[ch] = left.TryGetValue(ch, out int n) ? n + 1 : 1;
                    }
                }
            }

            var pods = new List<RoundPod>();
            var lists = new List<List<int>>();
            foreach ((char Code, int Count)[] column in columns)
            {
                var ids = new List<int>();
                foreach ((char code, int count) in column)
                {
                    int have = left.TryGetValue(code, out int n) ? n : 0;
                    int take = Math.Min(have, count);
                    if (take > 0)
                    {
                        ids.Add(pods.Count);
                        pods.Add(new RoundPod(code, take));
                        left[code] = have - take;
                    }
                }

                lists.Add(ids);
            }

            int next = 0;
            foreach (char code in left.Keys.OrderBy(c => c))
            {
                for (int n = left[code]; n > 0; n -= 50)
                {
                    lists[next % lists.Count].Add(pods.Count);
                    pods.Add(new RoundPod(code, Math.Min(50, n)));
                    next++;
                }
            }

            var tray = new List<IReadOnlyList<int>>(lists);
            return new RoundLevel(number, src.Width, src.Height, bottom, pods, tray, src.Entries, introduction: introduction, pictureId: src.PictureId, pictureVersion: src.PictureVersion, channel: channel, shutters: shutters);
        }

        /// <summary>
        /// The channel's dev level (frames 277–279): Level 76's airship, its spring behind three green cubes of the envelope's top
        /// edge, the inner exit in a carved pocket in the cream middle (a straight route down) and the outer one in the walkway by
        /// the nose (along the top, then down), shown as Level 101.
        /// </summary>
        private static RoundLevel ChannelLevel(RoundContentSet rounds, RoundIntroduction? introduction = null)
        {
            RoundLevel src = rounds.GetLevel(76);
            char[][] rows = RowsTop(src);
            for (int y = 3; y <= 4; y++)
            {
                for (int x = 22; x <= 24; x++)
                {
                    rows[y][x] = '.';
                }
            }

            for (int y = 12; y <= 14; y++)
            {
                for (int x = 22; x <= 24; x++)
                {
                    rows[y][x] = '.';
                }
            }

            for (int x = 22; x <= 24; x++)
            {
                rows[2][x] = 'G';
            }

            var inner = new List<int>();
            for (int y = 3; y <= 13; y++)
            {
                inner.Add(CellTop(src, 23, y));
            }

            var outer = new List<int>();
            for (int x = 23; x <= 36; x++)
            {
                outer.Add(CellTop(src, x, 3));
            }

            for (int y = 4; y <= 13; y++)
            {
                outer.Add(CellTop(src, 36, y));
            }

            outer.Add(CellTop(src, 37, 13));
            var channel = new RoundChannel(CellTop(src, 23, 3), new[]
            {
                new RoundChannelExit(CellTop(src, 23, 13), inner, 900_000),
                new RoundChannelExit(CellTop(src, 37, 13), outer, 1_800_000),
            });
            (char, int)[][] tray =
            {
                new[] { ('G', 3), ('B', 48), ('R', 32), ('A', 22) },
                new[] { ('C', 40), ('N', 30), ('B', 48), ('D', 15) },
                new[] { ('B', 48), ('R', 31), ('C', 50), ('Y', 11) },
            };
            return DevLevel(src, 101, rows, tray, channel: channel, introduction: introduction);
        }

        /// <summary>The shutters' dev level (frames 280–281): Level 91's picture with a shutter over 8 × 5 cubes counting 30 green ones, shown as Level 121.</summary>
        private static RoundLevel ShutterLevel(RoundContentSet rounds, RoundIntroduction? introduction = null)
        {
            RoundLevel src = rounds.GetLevel(91);
            var cells = new List<int>();
            for (int y = 5; y <= 9; y++)
            {
                for (int x = 13; x <= 20; x++)
                {
                    cells.Add(CellTop(src, x, y));
                }
            }

            cells.Sort();
            (char, int)[][] tray =
            {
                new[] { ('G', 36), ('M', 23), ('Y', 51), ('C', 52) },
                new[] { ('M', 23), ('G', 36), ('C', 52), ('Y', 51) },
                new[] { ('D', 25), ('O', 27), ('G', 36), ('Y', 50) },
            };
            return DevLevel(src, 121, RowsTop(src), tray, shutters: new[] { new RoundShutter(cells, 'G', 30) }, introduction: introduction);
        }
    }
}
