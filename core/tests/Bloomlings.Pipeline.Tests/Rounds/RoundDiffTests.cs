using System.Diagnostics;
using System.IO;
using Bloomlings.Pipeline.Commands;
using Bloomlings.Pipeline.Rounds;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests.Rounds
{
    /// <summary><c>rounds diff</c> (T051, FR-052): published levels and pictures change only by a new version.</summary>
    [TestFixture]
    public class RoundDiffTests
    {
        private static (RoundTestContent Content, string Published) Published()
        {
            var content = new RoundTestContent();
            string outDir = Path.Combine(content.Root, "published");
            RoundPublisher.Publish(RoundContent.Load(content.Folder), outDir, null);
            return (content, outDir);
        }

        [Test]
        public void NoChange_HasNoErrors()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                RoundDiffResult diff = RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(content.Folder, content.Folder));

                Assert.That(diff.Errors, Is.Empty);
                Assert.That(diff.Changed, Is.Empty);
                Assert.That(diff.Added, Is.Empty);
            }
        }

        [Test]
        public void AChangedLevel_WithoutABump_IsAnError()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                content.Edit("levels/L0002.json", j => j["tray"] = new JArray(new JArray(1), new JArray(0)));

                RoundDiffResult diff = RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(content.Folder, content.Folder));

                Assert.That(diff.Changed, Is.EqualTo(new[] { 2 }));
                Assert.That(diff.Errors, Is.EqualTo(new[] { "L0002 changed without a definitionVersion bump (1 → 1)" }));
            }
        }

        [Test]
        public void AChangedLevel_WithABump_IsDeliberate()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                content.Edit("levels/L0002.json", j =>
                {
                    j["tray"] = new JArray(new JArray(1), new JArray(0));
                    j["definitionVersion"] = 2;
                });

                RoundDiffResult diff = RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(content.Folder, content.Folder));

                Assert.That(diff.Changed, Is.EqualTo(new[] { 2 }));
                Assert.That(diff.Errors, Is.Empty);
            }
        }

        [Test]
        public void ARemovedLevel_IsAnError_AndANewOneIsAdded()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                File.Delete(content.PathOf("levels/L0002.json"));
                RoundTestContent.Write(content.Folder, 1, firstLevel: 3);

                RoundDiffResult diff = RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(content.Folder, content.Folder));

                Assert.That(diff.Errors, Is.EqualTo(new[] { "L0002 was removed" }));
                Assert.That(diff.Added, Is.EqualTo(new[] { 3 }));
            }
        }

        [Test]
        public void AChangedPicture_NeedsANewVersion_ButANewApprovalDateIsNoChange()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                content.Edit("pictures/P9001.json", j => j["approval"]!["decidedAt"] = "2026-11-01");
                Assert.That(RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(content.Folder, content.Folder)).Errors, Is.Empty);

                content.Edit("pictures/P9001.json", j => j["title"] = "Sprout, retitled");
                RoundDiffResult diff = RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(content.Folder, content.Folder));

                Assert.That(diff.ChangedPictures, Is.EqualTo(new[] { "P9001" }));
                Assert.That(diff.Errors, Is.EqualTo(new[] { "picture P9001 v1 changed without a new picture version" }));
            }
        }

        [Test]
        public void APublishedVersion_MustBeFollowedByAHigherOne()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                RoundDiffResult diff = RoundDiff.Compare(RoundSource.Load(published, content.Folder), RoundSource.Load(published, content.Folder));

                Assert.That(diff.Errors, Is.EqualTo(new[] { "the later content version (v1) must be higher than the earlier (v1)" }));
            }
        }

        [Test]
        public void AGitRef_IsReadFromTheRoundsFolder()
        {
            using var content = new RoundTestContent();
            Git(content.Root, "init", "-q");
            Git(content.Root, "add", "-A");
            Git(content.Root, "-c", "user.name=test", "-c", "user.email=test@example.invalid", "commit", "-q", "-m", "rounds");
            content.Edit("levels/L0001.json", j => j["tray"] = new JArray(new JArray(1), new JArray(0)));

            RoundSource before = RoundSource.Load("HEAD", content.Folder);
            RoundDiffResult diff = RoundDiff.Compare(before, RoundSource.Load(content.Folder, content.Folder));

            Assert.That(before.Levels.Keys, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(before.Pictures.Keys, Is.EqualTo(new[] { "P9001@1" }));
            Assert.That(diff.Errors, Is.EqualTo(new[] { "L0001 changed without a definitionVersion bump (1 → 1)" }));
        }

        [Test]
        public void Cli_Diff_ExitsOneOnAnUnbumpedChange()
        {
            (RoundTestContent content, string published) = Published();
            using (content)
            {
                content.Edit("levels/L0001.json", j => j["tray"] = new JArray(new JArray(1), new JArray(0)));

                int code = Program.Main(new[] { "rounds", "diff", "--from", published, "--to", content.Folder });

                Assert.That(code, Is.EqualTo(ExitCodes.ValidationFailed));
                Assert.That(Program.Main(new[] { "rounds", "diff", "--from", published, "--to", published + "-missing" }), Is.EqualTo(ExitCodes.UsageOrIo));
            }
        }

        private static void Git(string dir, params string[] args)
        {
            var info = new ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (string arg in args)
            {
                info.ArgumentList.Add(arg);
            }

            using Process process = Process.Start(info)!;
            string error = process.StandardError.ReadToEnd();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.EqualTo(0), "git " + string.Join(" ", args) + ": " + error);
        }
    }
}
