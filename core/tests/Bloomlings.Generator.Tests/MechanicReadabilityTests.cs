using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Overlays;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    public class MechanicReadabilityTests
    {
        private static string Repo => FindRoot();
        private static string FindRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "content/launch.props"))) return dir.FullName;
            throw new DirectoryNotFoundException("Repository root");
        }

        [TestCase(14,16)] [TestCase(18,20)] [TestCase(21,26)]
        [TestCase(24,24)] [TestCase(30,32)] [TestCase(36,36)]
        [TestCase(22,28)] [TestCase(28,28)]
        public void EnvironmentObjectsHaveReadableFootprintsAndValidBoardGeometryAtEverySize(int width, int height)
        {
            LevelDefinition source = DefinitionJson.Read(File.ReadAllText(Path.Combine(Repo, "content/curated/dev/level-dev-mosaic-24x24.json")));
            BasePicture original = BasePictureJson.Read(File.ReadAllText(Path.Combine(Repo, "content/pictures/dev/lib", source.Picture.Id + ".json")));
            BasePicture picture = original with { Width = width, Height = height,
                Grid = Enumerable.Range(0,height).Select(y => (IReadOnlyList<int>)Enumerable.Repeat(y == height-1 ? 1 : 0,width).ToArray()).ToArray() };
            GenerationProfile profile = ProfileLoader.ReadFile(Path.Combine(Repo, "content/profiles/dev/band-mosaic-24x24.json")) with { ReadableMechanics = true };
            LevelDefinition plain = source with { Picture = source.Picture with { Mirror = Mirror.None },
                Entries = new[] { new EntryDef(new CellPos(width/2,0),EntrySide.Bottom) },
                Overlays = Array.Empty<CellOverlay>(), Specials = Array.Empty<SpecialDef>() };
            foreach (string mechanic in new[] { "gate", "fountain", "chest", "environment_2" })
            {
                var rng = new Xoshiro256StarStar(4);
                Bloomlings.Core.Boards.Board board = BoardBuilder.Build(plain, picture, VariantCatalog.Default);
                BoardPlan overlays = OverlayPlanner.BoardOverlays(board, Enumerable.Range(0,width*height).Select(i=>i/width<height-1).ToArray(),
                    new[] { mechanic }, profile, 5004, source.Mapping.Values.Distinct().ToArray(), ref rng, BoardLooks.For(width,height));
                LevelDefinition prepared = plain with { Overlays = overlays.Overlays };
                board = BoardBuilder.Build(prepared,picture,VariantCatalog.Default);
                var waves = SolutionPlanner.Plan(board,VariantCatalog.Default)!;
                LockPlan objects = OverlayPlanner.Locks(board,new[] { mechanic },waves,Array.Empty<PlannedPod>(),overlays,ref rng);
                Assert.That(objects.Dropped, Is.Empty, mechanic);
                Assert.That(objects.Specials, Has.Count.EqualTo(1), mechanic);
                int side = BoardMechanicReadability.ObjectSide(width,height);
                Assert.That(objects.Specials[0].Cells.Count, Is.EqualTo(side*side), mechanic);
                Assert.DoesNotThrow(()=>BoardBuilder.Build(prepared with { Specials=objects.Specials },picture,VariantCatalog.Default));
            }
            Assert.That(BoardMechanicReadability.Allows("key",width,height), Is.EqualTo(width<=20&&height<=24));
            Assert.That(BoardMechanicReadability.Allows("counted_stack",width,height), Is.EqualTo(width<=24&&height<=26));
            Assert.That(BoardMechanicReadability.Allows("connected_pair",width,height), Is.True);
        }

        [TestCase("key")]
        [TestCase("locked_pod")]
        [TestCase("locked_slot")]
        [TestCase("counted_stack")]
        public void ForcedTinyMarksAreRefusedOnDenseBoardsInsteadOfBeingSilentlyPlaced(string mechanic)
        {
            GenerationProfile profile=ProfileLoader.ReadFile(Path.Combine(Repo,"content/profiles/dev/band-mosaic-36x36.json"))
                with { ReadableMechanics=true, AllowedMechanics=new[]{mechanic}, MaxCandidatesPerLevel=1 };
            string thresholds=File.ReadAllText(Path.Combine(Repo,"content/profiles/difficulty-thresholds.json"));
            var pairs=ContentStore.LoadPairs(Path.Combine(Repo,"content/readability/approved-pairs.json"))!;
            var generator=new LevelGenerator(profile,new PicturePicker(ContentStore.LoadLibrary(Path.Combine(Repo,"content/pictures/dev/lib"))),
                ProfileLoader.ReadThresholds(thresholds,profile.BandId),pairs.IsApproved,new DifficultySchedule(0xB100B100UL))
            { ForcedClass=DifficultyClass.Normal, ForcedMechanics=new[]{mechanic}, BigBoardThresholds=ProfileLoader.ReadBigThresholds(thresholds,profile.BandId) };
            GenerationResult result=generator.Generate(5003,5003,2,new Dictionary<int,LevelDefinition>());
            Assert.That(result.Accepted,Is.Empty);
            Assert.That(result.Rejections.Any(r=>r.Reason.StartsWith("readability:"+mechanic,StringComparison.Ordinal)),Is.True);
        }

        [Test]
        public void GeneratedStackFixturePassesReleaseValidationAndJsonRoundTrip()
        {
            string json = File.ReadAllText(Path.Combine(Repo,"content/curated/dev/level-dev-counted-stack-24x24.json"));
            LevelDefinition level = DefinitionJson.Read(json);
            Assert.That(DefinitionJson.Write(level),Is.EqualTo(json));
            Assert.That(level.Overlays.Count(o=>o.StackCount>1),Is.GreaterThan(1));
            BasePicture picture = BasePictureJson.Read(File.ReadAllText(Path.Combine(Repo,"content/pictures/dev/lib",level.Picture.Id+".json")));
            var pairs=ContentStore.LoadPairs(Path.Combine(Repo,"content/readability/approved-pairs.json"))!;
            CatalogReport report = new CatalogValidator(new[]{picture},UnlockRoadmap.Default,pairs,new SolveOptions(200000)).Validate(new[]{level});
            Assert.That(report.Issues.Where(i=>i.IsError).Select(i=>i.Message),Is.Empty);
            Assert.That(report.Records[level.LevelNumber].JamWitness,Is.Not.Empty);
            Assert.That(report.Records[level.LevelNumber].PlayerInfoFair,Is.True,"visible stacks have no hidden variant allocation");
            Assert.That(FairnessChecker.Check(level,picture,new SessionOptions(1,20000),200000).Status,Is.EqualTo(FairnessStatus.Fair));
            ContentManifest manifest=ManifestWriter.Build(new PublishRequest(1,"1.0.0",1,20000,new[]{level},new[]{picture},Array.Empty<DailyPoolEntry>()),out IReadOnlyList<PackFile> files);
            var bytes=files.ToDictionary(f=>f.Entry.Id,f=>f.Bytes);
            ContentSet loaded=ContentLoader.FromPacks(ContentManifest.Read(manifest.Write()),entry=>bytes[entry.Id]);
            LevelDefinition restored=loaded.GetLevel(level.LevelNumber);
            Assert.That(DefinitionJson.Write(restored),Is.EqualTo(json));
            var session=LevelSession.Load(restored,loaded.GetPicture(restored.Picture),new SessionOptions(1,20000));
            foreach(CellOverlay stack in restored.Overlays.Where(o=>o.StackCount>1))
                Assert.That(session.View.Cell(stack.Cell).StackRemaining,Is.EqualTo(stack.StackCount));
        }
    }
}
