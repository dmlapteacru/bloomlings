using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Playtest.Check
{
    /// <summary>
    /// The round content the playtest APKs carry (spec 006 T025, T053): a published set made from the round levels in the
    /// repository (<see cref="Publish"/>) is read back as the APKs read their assets (<see cref="PlaytestContent.LoadRounds"/>
    /// over <see cref="FolderFiles"/>), and so is <c>content/game/rounds/published</c> once <c>rounds publish</c> has
    /// written it. <c>--publish-dev &lt;dir&gt;</c> writes the same set for a local APK build before the first publish.
    /// </summary>
    internal static class RoundContentChecks
    {
        /// <summary>Where <c>rounds publish</c> writes the set the APKs pack (playtest/Catalog.props).</summary>
        public const string PublishedFolder = "content/game/rounds/published";

        public static IEnumerable<string> Run(string root)
        {
            var failures = new List<string>();
            void Expect(bool condition, string message)
            {
                if (!condition)
                {
                    failures.Add(message);
                }
            }

            string temp = Path.Combine(Path.GetTempPath(), "pt-rounds-" + Guid.NewGuid().ToString("N"));
            IBundledFiles before = PlaytestFiles.Source;
            try
            {
                int written = Publish(root, temp, out string source);
                PlaytestFiles.Source = new FolderFiles(PlaytestContent.RoundPrefix, temp);
                Expect(PlaytestContent.HasRounds, "the build's files carry rounds/manifest.json");
                RoundContentSet rounds = PlaytestContent.LoadRounds();
                Expect(rounds.LevelCount == written, $"the published set has its {written} levels from {source}");
                Expect(rounds.ReadPacks.Count == 0, "nothing but the manifest is read at start");
                Expect(PlaytestContent.Load().LevelCount == 0, "a build with round content carries no legacy level");
                CheckLevels(rounds, "dev set", Expect);
                Expect(!rounds.Has(rounds.LevelCount + 1) && !rounds.TryGetLevel(rounds.LevelCount + 1, out _), "no level follows the last round level");
                Console.WriteLine($"round content: {rounds.LevelCount} levels from {source}, published and read back as the APKs read them ({string.Join(", ", rounds.ReadPacks.OrderBy(p => p, StringComparer.Ordinal))})");

                string published = Path.Combine(root, PublishedFolder);
                if (File.Exists(Path.Combine(published, RoundContentManifest.FileName)))
                {
                    PlaytestFiles.Source = new FolderFiles(PlaytestContent.RoundPrefix, published);
                    CheckLevels(PlaytestContent.LoadRounds(), PublishedFolder, Expect);
                }

                PlaytestFiles.Source = new FolderFiles(PlaytestContent.RoundPrefix, Path.Combine(temp, "missing"));
                bool refused = false;
                try
                {
                    PlaytestContent.LoadRounds();
                }
                catch (ContentIntegrityException error)
                {
                    refused = error.Message.Contains("no published round content", StringComparison.Ordinal);
                }

                Expect(refused && !PlaytestContent.HasRounds, "a build without published round content says so");
            }
            catch (Exception error)
            {
                failures.Add("round content: " + error.GetType().Name + ": " + error.Message);
            }
            finally
            {
                PlaytestFiles.Source = before;
                if (Directory.Exists(temp))
                {
                    Directory.Delete(temp, true);
                }
            }

            return failures;
        }

        /// <summary>
        /// Writes a published round set (manifest and packs, <see cref="RoundPacks.Build"/>) into <paramref name="folder"/>
        /// from the round levels in the repository: <c>content/game/rounds/levels</c> once a wave is imported, else the
        /// exported waves (<c>content/game/rounds/export/L####-L####</c>). For checks and local APK builds only: the
        /// game's set comes from <c>rounds publish</c>, which validates and proves the levels first.
        /// </summary>
        public static int Publish(string root, string folder, out string source)
        {
            string rounds = Path.Combine(root, "content/game/rounds");
            var sources = new List<string>();
            if (Directory.Exists(Path.Combine(rounds, "levels")) && Directory.GetFiles(Path.Combine(rounds, "levels"), "L*.json").Length > 0)
            {
                sources.Add(rounds);
            }
            else if (Directory.Exists(Path.Combine(rounds, "export")))
            {
                sources.AddRange(Directory.GetDirectories(Path.Combine(rounds, "export"), "L*").OrderBy(d => d, StringComparer.Ordinal));
            }

            var levels = new Dictionary<int, RoundLevel>();
            var pictures = new Dictionary<string, RoundPicture>(StringComparer.Ordinal);
            foreach (string from in sources)
            {
                foreach (string file in Directory.GetFiles(Path.Combine(from, "levels"), "L*.json"))
                {
                    RoundLevel level = RoundJson.ReadLevel(File.ReadAllText(file));
                    levels[level.Number] = level;
                }

                foreach (string file in Directory.GetFiles(Path.Combine(from, "pictures"), "P*.json"))
                {
                    RoundPicture picture = RoundJson.ReadPicture(File.ReadAllText(file));
                    pictures[picture.Key] = picture;
                }
            }

            if (levels.Count == 0)
            {
                throw new InvalidOperationException("No round levels in content/game/rounds/levels or content/game/rounds/export.");
            }

            var used = new HashSet<string>(levels.Values.Select(l => RoundPicture.KeyOf(l.PictureId!, l.PictureVersion)), StringComparer.Ordinal);
            RoundContentManifest manifest = RoundPacks.Build(1, levels.Values.ToList(), pictures.Values.Where(p => used.Contains(p.Key)).ToList(), out IReadOnlyList<RoundPackFile> packs);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            foreach (RoundPackFile pack in packs)
            {
                string path = Path.Combine(folder, pack.Entry.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, pack.Bytes);
            }

            File.WriteAllText(Path.Combine(folder, RoundContentManifest.FileName), manifest.Write(), new UTF8Encoding(false));
            source = string.Join(", ", sources.Select(s => Path.GetRelativePath(root, s)));
            return levels.Count;
        }

        /// <summary>Every level reads with its picture, and its board is its picture's board (the level screen draws both).</summary>
        private static void CheckLevels(RoundContentSet rounds, string name, Action<bool, string> expect)
        {
            for (int n = 1; n <= rounds.LevelCount; n++)
            {
                RoundLevel level = rounds.GetLevel(n);
                RoundPicture picture = rounds.GetPicture(level);
                RoundManifestLevel listed = rounds.Listed(n);
                expect(level.Number == n && listed.Class == level.Class && picture.Id == level.PictureId, $"{name}: L{n} reads with its picture");
                RoundBoardShape board = RoundPictureBoard.Of(picture);
                expect(board.Width == level.Width && board.Height == level.Height && board.RowsBottomFirst.SequenceEqual(level.RowsBottomFirst),
                    $"{name}: L{n}'s board is its picture {picture.Key}'s board");
            }
        }
    }
}
