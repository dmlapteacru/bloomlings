using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Json;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Launch;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    [NonParallelizable]
    public class LaunchProductionTests
    {
        private string _previous = "";
        private string _temporary = "";
        [SetUp]
        public void EnterRepository()
        {
            _previous = Directory.GetCurrentDirectory();
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while(root != null && !File.Exists(Path.Combine(root.FullName,"content/launch.props"))) root = root.Parent;
            if(root == null)throw new DirectoryNotFoundException("Repository");
            Directory.SetCurrentDirectory(root.FullName);
            _temporary = Path.Combine("content/work", "launch-test-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporary);
        }
        [TearDown]
        public void LeaveRepository()
        {
            if(Directory.Exists(_temporary))Directory.Delete(_temporary,true);
            Directory.SetCurrentDirectory(_previous);
        }

        [Test]
        public void HeroMilestones_HaveFourUniqueMaximumBoards_AndKeepTheOtherPicturesUnique()
        {
            var doc = new LaunchArtDocument(LaunchArtDocument.DefaultRegistry, LaunchArtDocument.DefaultPlan);
            foreach (HeroMilestone hero in HeroMilestones.All)
            {
                var slot = doc.Plan.Slots[hero.Level - 1];
                var art = doc.Compositions[hero.Level];
                Assert.That((slot.Width, slot.Height, slot.SizeGroup), Is.EqualTo((36, 36, "xl")));
                Assert.That(art.PictureId, Is.EqualTo(hero.PictureId));
                Assert.That(doc.Plan.Slots[hero.Level - 2].SizeGroup, Is.Not.EqualTo("xl"));
                Assert.That(doc.Plan.Slots[hero.Level].SizeGroup, Is.Not.EqualTo("xl"));
                Assert.That(doc.CheckArt(new[] { hero.Level }), Is.Empty);
            }
            Assert.That(doc.Compositions.Values.Select(c => c.PictureId).Distinct().Count(), Is.EqualTo(2500));
        }

        [Test]
        public void EntryCapacityUsesBottomFirstRowsAndOnlyCountsHiddenColors()
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            BasePicture basis=BasePictureJson.Read(File.ReadAllText(Path.Combine(doc.LibraryDirectory,doc.Compositions[11].PictureId+".json")));
            int[][] grid=Enumerable.Range(0,8).Select(y=>Enumerable.Repeat(y==0?BasePicture.Empty:y==1?0:1,7).ToArray()).ToArray();
            var picture=basis with{Width=7,Height=8,Grid=grid};
            Assert.That(AuthoredTrayBuilder.InitialBlockedPodCapacity(picture),Is.EqualTo(8));
            grid[1][3]=BasePicture.Empty;
            Assert.That(AuthoredTrayBuilder.InitialBlockedPodCapacity(picture),Is.Zero);
        }

        [Test]
        public void FrozenGeometrySurvivesArtworkInventoryChangesAndPreservesRealNulls()
        {
            var original = new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            JObject inventory = JObject.Parse(File.ReadAllText(LaunchArtDocument.DefaultRegistry));
            ((JArray)inventory["compositions"]!).Last!.Remove();
            string registry=Path.Combine(_temporary,"registry.json");
            File.WriteAllText(registry,inventory.ToString());
            var reduced = new LaunchArtDocument(registry,LaunchArtDocument.DefaultPlan);
            Assert.That(reduced.Plan.Slots.Select(s=>(s.Level,s.Width,s.Height,s.Difficulty,s.RequiredMechanic)),
                Is.EqualTo(original.Plan.Slots.Select(s=>(s.Level,s.Width,s.Height,s.Difficulty,s.RequiredMechanic))));
            Assert.That(reduced.Write()["geometryHash"]!.Value<string>(),Is.EqualTo(original.Write()["geometryHash"]!.Value<string>()));
            Assert.That(reduced.Write()["slots"]!.Children().Any(s=>s["requiredMechanic"]!.Type==JTokenType.Null),Is.True);
            Assert.Throws<ArgumentException>(()=>reduced.RequireCurrent(LaunchArtDocument.DefaultPlan));
            string plan=Path.Combine(_temporary,"plan.json");
            File.WriteAllText(plan,CanonicalJson.Write(reduced.Write(),true));
            new LaunchArtDocument(registry,plan).RequireCurrent(plan);
        }

        [Test]
        public void DuplicateAssignmentsAndTamperedFrozenGeometryAreRejected()
        {
            JObject inventory=JObject.Parse(File.ReadAllText(LaunchArtDocument.DefaultRegistry));
            ((JArray)inventory["compositions"]!).Add(inventory["compositions"]![0]!.DeepClone());
            string registry=Path.Combine(_temporary,"registry.json");File.WriteAllText(registry,inventory.ToString());
            Assert.Throws<ArgumentException>(()=>new LaunchArtDocument(registry,LaunchArtDocument.DefaultPlan));
            JObject frozen=JObject.Parse(File.ReadAllText(LaunchArtDocument.DefaultPlan));
            frozen["geometryHash"]="tampered";
            string plan=Path.Combine(_temporary,"plan.json");File.WriteAllText(plan,frozen.ToString());
            Assert.Throws<ArgumentException>(()=>new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,plan));
        }

        [Test]
        public void DefinitionAuditDetectsChangedVersionAndPalette()
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            var tutorial=DefinitionJson.Read(File.ReadAllText(doc.Compositions[1].CuratedDefinition!));
            Assert.That(doc.CheckDefinitions(new[]{tutorial},false),Is.Empty);
            Assert.That(doc.CheckDefinitions(new[]{tutorial with{DefinitionVersion=tutorial.DefinitionVersion+1}},false),Is.Not.Empty);
            Assert.That(doc.CheckDefinitions(new[]{tutorial with{Mapping=new Dictionary<string,Bloomlings.Core.Variants.VariantId>()}},false),Is.Not.Empty);
            Assert.That(doc.CheckDefinitions(new[]{tutorial},true),Is.Not.Empty);
        }

        [Test]
        public void IncompleteBuildCannotBeInstalled()
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            File.WriteAllText(Path.Combine(_temporary,"launch-build.json"),new JObject {
                ["version"]=Bloomlings.Pipeline.Commands.LaunchArtCommand.BuildVersion,["complete"]=false,["registryHash"]=doc.RegistryHash }.ToString());
            Assert.Throws<ArgumentException>(()=>LaunchInstaller.Install(doc,_temporary));
            Assert.That(Directory.GetDirectories(_temporary),Is.Empty);
        }

        [Test]
        public void FailedPromotionRestoresBothPreviousDirectories()
        {
            string old1=Path.Combine(_temporary,"live1"),old2=Path.Combine(_temporary,"live2"),stage=Path.Combine(_temporary,"stage");
            Directory.CreateDirectory(old1);Directory.CreateDirectory(old2);Directory.CreateDirectory(stage);
            File.WriteAllText(Path.Combine(old1,"old"),"first");File.WriteAllText(Path.Combine(old2,"old"),"second");
            File.WriteAllText(Path.Combine(stage,"new"),"replacement");
            Assert.Throws<DirectoryNotFoundException>(()=>LaunchInstaller.Promote(new[] {
                (stage,old1),(Path.Combine(_temporary,"missing-stage"),old2) },Path.Combine(_temporary,"backup")));
            Assert.That(File.ReadAllText(Path.Combine(old1,"old")),Is.EqualTo("first"));
            Assert.That(File.ReadAllText(Path.Combine(old2,"old")),Is.EqualTo("second"));
            Assert.That(File.Exists(Path.Combine(old1,"new")),Is.False);
        }

        [Test]
        public void SuccessfulPromotionKeepsOldDataInBackup()
        {
            string live=Path.Combine(_temporary,"live"),stage=Path.Combine(_temporary,"stage"),backup=Path.Combine(_temporary,"backup");
            Directory.CreateDirectory(live);Directory.CreateDirectory(stage);
            File.WriteAllText(Path.Combine(live,"data"),"old");File.WriteAllText(Path.Combine(stage,"data"),"new");
            LaunchInstaller.Promote(new[]{(stage,live)},backup);
            Assert.That(File.ReadAllText(Path.Combine(live,"data")),Is.EqualTo("new"));
            Assert.That(File.ReadAllText(Path.Combine(backup,"directory-0","data")),Is.EqualTo("old"));
        }

        private static JObject RunCli(params string[] args)
        {
            using var output=new StringWriter();var previous=Console.Out;
            try
            {
                Console.SetOut(output);
                int code=Bloomlings.Pipeline.Program.Main(args);
                var report=JObject.Parse(output.ToString());
                Assert.That(report.Value<int>("exitCode"),Is.EqualTo(code));
                return report;
            }
            finally { Console.SetOut(previous); }
        }

        [Test]
        public void LocalBuildResumesMatchingCertificatesAndRegeneratesStaleRecipes()
        {
            string folder=Path.Combine(_temporary,"cache");
            string[] args={"launch","generate","--levels","11","--out",folder,"--jobs","1","--retries","0","--json"};
            var first=RunCli(args);
            Assert.That(first.Value<int>("exitCode"),Is.Zero);
            Assert.That(first.Value<int>("generated"),Is.EqualTo(1));
            string level=Path.Combine(folder,"levels",ContentStore.LevelFileName(11));
            string original=File.ReadAllText(level);
            var resumed=RunCli(args);
            Assert.That(resumed.Value<int>("exitCode"),Is.Zero);
            Assert.That(resumed.Value<int>("resumed"),Is.EqualTo(1));
            Assert.That(resumed.Value<int>("generated"),Is.Zero);
            Assert.That(File.ReadAllText(level),Is.EqualTo(original));
            File.WriteAllText(Path.Combine(folder,"recipes",ContentStore.LevelFileName(11)),"{\"recipeHash\":\"stale\"}");
            var rebuilt=RunCli(args);
            Assert.That(rebuilt.Value<int>("exitCode"),Is.Zero);
            Assert.That(rebuilt.Value<int>("generated"),Is.EqualTo(1));
            Assert.That(File.ReadAllText(level),Is.EqualTo(original));
            Assert.That(Directory.GetFiles(folder,"*.tmp-*",SearchOption.AllDirectories),Is.Empty);
        }

        [Test]
        public void Certified160CacheMigratesWithoutRegeneratingItsLevels()
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            string folder=Path.Combine(_temporary,"old-cache");
            string[] args={"launch","generate","--levels","11","--out",folder,"--jobs","1","--retries","0","--json"};
            Assert.That(RunCli(args).Value<int>("exitCode"),Is.Zero);
            string path=Path.Combine(folder,"levels",ContentStore.LevelFileName(11));
            var old=DefinitionJson.Read(File.ReadAllText(path)) with{GeneratorVersion="gen-1.6.0"};
            var record=ContentStore.LoadRecords(folder)[11] with{DefinitionHash=ValidationRecord.HashOf(old)};
            ContentStore.WriteLevelAtomic(folder,old,record);
            string recipe=Path.Combine(folder,"recipes",ContentStore.LevelFileName(11));
            File.WriteAllText(recipe,new JObject{["recipeHash"]=doc.RecipeHash(11,"gen-1.6.0")}.ToString());
            var resumed=RunCli(args);
            Assert.That(resumed.Value<int>("exitCode"),Is.Zero);
            Assert.That(resumed.Value<int>("generated"),Is.Zero);
            Assert.That(resumed.Value<int>("resumed"),Is.EqualTo(1));
            Assert.That(File.ReadAllText(path),Is.EqualTo(DefinitionJson.Write(old)));
            Assert.That(JObject.Parse(File.ReadAllText(recipe)).Value<string>("recipeHash"),Is.EqualTo(doc.RecipeHash(11)));
        }

        [Test]
        public void ResumeRepairsAnOptionalNeighbourOfABlockedCleanIntroduction()
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            var library=ContentStore.LoadLibrary(doc.LibraryDirectory);
            var pairs=ContentStore.LoadPairs("content/readability/approved-pairs.json")!;
            string calibration=File.ReadAllText(LaunchArtDocument.ThresholdsPath);
            var basis=doc.Profile(doc.Plan.Slots[42]);
            var prepare=new LevelGenerator(basis,new PicturePicker(library,plan:n=>doc.Compositions[n].PictureId),
                ProfileLoader.ReadThresholds(calibration,basis.BandId),pairs.IsApproved,new DifficultySchedule(doc.Plan.ScheduleSeed,UnlockRoadmap.Default)) {
                UseAuthoredArtTray=true,DefinitionVersionOf=doc.DefinitionVersion,ClassOf=n=>doc.Plan.Slots[n-1].Difficulty,
                MechanicsOf=_=>Array.Empty<string>(),RecipeOf=n=>{
                    var profile=doc.Profile(doc.Plan.Slots[n-1]);
                    return (profile,ProfileLoader.ReadThresholds(calibration,profile.BandId),ProfileLoader.ReadBigThresholds(calibration,profile.BandId));
                }
            };
            var initial=prepare.Generate(43,44,doc.Plan.Seed,new Dictionary<int,LevelDefinition>());
            Assert.That(initial.Failed,Is.Empty);
            string folder=Path.Combine(_temporary,"blocked-introduction");
            foreach(var generated in initial.Accepted)
            {
                var old=generated.Definition with{GeneratorVersion="gen-1.6.0"};int n=old.LevelNumber;
                ContentStore.WriteLevelAtomic(folder,old,generated.Record with{DefinitionHash=ValidationRecord.HashOf(old)});
                ContentStore.AtomicText(Path.Combine(folder,"recipes",ContentStore.LevelFileName(n)),new JObject{["recipeHash"]=doc.RecipeHash(n,"gen-1.6.0")}.ToString());
            }
            File.WriteAllText(Path.Combine(folder,"launch-build.json"),new JObject{["version"]=Bloomlings.Pipeline.Commands.LaunchArtCommand.BuildVersion,["complete"]=false}.ToString());
            var report=RunCli("launch","generate","--levels","43-45","--out",folder,"--jobs","1","--retries","0","--json");
            Assert.That(report.Value<int>("exitCode"),Is.Zero);
            Assert.That(report["failedLevels"]!.HasValues,Is.False);
            Assert.That(report.Value<int>("generated"),Is.EqualTo(2),"Only the blocked slot and one optional neighbour are rebuilt.");
            Assert.That(report.Value<int>("resumed"),Is.EqualTo(1));
            var levels=ContentStore.LoadLevels(folder).Select(l=>l.Level).ToArray();
            Assert.That(levels.Select(l=>l.LevelNumber),Is.EquivalentTo(new[]{43,44,45}));
            Assert.That(LevelMechanics.UnlocksUsed(levels.Single(l=>l.LevelNumber==45),library.Single(p=>p.Id==doc.Compositions[45].PictureId)),Is.Empty,
                "The new-color introduction keeps its required clean gameplay.");
        }

        [Test]
        public void CoarseXlArtGeneratesANonemptySourceWithCertifiedSuperHardDifficulty()
        {
            // L51 has few broad colors: almost all pods belong to the reserved route or jam sequence.
            // Spreading only the unreserved remainder used to leave Source columns empty for every seed.
            string folder=Path.Combine(_temporary,"source-columns");
            var report=RunCli("launch","generate","--levels","51","--out",folder,"--jobs","1","--retries","0","--json");
            Assert.That(report.Value<int>("exitCode"),Is.Zero);
            Assert.That(report["errors"]!.HasValues,Is.False);
            var level=DefinitionJson.Read(File.ReadAllText(Path.Combine(folder,"levels",ContentStore.LevelFileName(51))));
            Assert.That(level.Tray.Stacks.All(stack=>stack.Count>0),Is.True);
            Assert.That(level.Difficulty.Class,Is.EqualTo(DifficultyClass.SuperHard));
            Assert.That(ContentStore.LoadRecords(folder)[51].Result,Is.EqualTo(ValidationResult.Solvable));
        }

        [Test]
        public void MissingAssignedArtBlocksBeforeCreatingAnOutputCatalog()
        {
            JObject inventory=JObject.Parse(File.ReadAllText(LaunchArtDocument.DefaultRegistry));
            var entry=inventory["compositions"]!.Children().FirstOrDefault(e=>e.Value<int>("launchLevel")==2500);
            entry?.Remove();
            string registry=Path.Combine(_temporary,"registry.json"),plan=Path.Combine(_temporary,"plan.json"),folder=Path.Combine(_temporary,"blocked");
            File.WriteAllText(registry,inventory.ToString());
            var doc=new LaunchArtDocument(registry,LaunchArtDocument.DefaultPlan);
            File.WriteAllText(plan,CanonicalJson.Write(doc.Write(),true));
            var report=RunCli("launch","generate","--registry",registry,"--plan",plan,"--levels","2500","--out",folder,"--json");
            Assert.That(report.Value<int>("exitCode"),Is.EqualTo(1));
            Assert.That(report["missingArt"]!.Values<int>(),Does.Contain(2500));
            Assert.That(Directory.Exists(folder),Is.False);
        }

        [Test]
        public void SeparateDailyLibraryImportsOnlyReferencedPicturesAndRefusesConflicts()
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            var regular=BasePictureJson.Read(File.ReadAllText(Path.Combine(doc.LibraryDirectory,doc.Compositions[1].PictureId+".json")));
            var daily=ContentStore.LoadLevels("content/daily",1).Single().Level;
            var old=ContentStore.LoadLibrary("content/pictures/lib");
            var dailyPicture=old.Single(p=>p.Id==daily.Picture.Id&&p.Version==daily.Picture.Version);
            var merged=Bloomlings.Pipeline.Commands.CatalogCommands.MergeDailyPictures(new[]{regular},"content/pictures/lib",new[]{daily});
            Assert.That(merged.Select(p=>p.Id),Is.EquivalentTo(new[]{regular.Id,dailyPicture.Id}));
            var modified=JObject.Parse(BasePictureJson.Write(dailyPicture));modified["subject"]="modified Daily subject";
            var conflicting=BasePictureJson.Read(modified.ToString());
            Assert.Throws<IOException>(()=>Bloomlings.Pipeline.Commands.CatalogCommands.MergeDailyPictures(new[]{conflicting},"content/pictures/lib",new[]{daily}));
            Assert.Throws<IOException>(()=>Bloomlings.Pipeline.Commands.CatalogCommands.MergeDailyPictures(new[]{regular},"content/pictures/lib",new[]{daily with{Picture=new PictureRef(regular.Id,regular.Version,Mirror.None,"default")}}));
        }

        [TestCase(11)]
        [TestCase(15)]
        [TestCase(23)]
        [TestCase(26)]
        [TestCase(60)]
        [TestCase(102)]
        [TestCase(112)]
        [TestCase(351)]
        [TestCase(406)]
        [TestCase(1437)]
        [TestCase(2166)]
        public void AssignedAuthoredArtBuildsARealWinningAndLosingPuzzle(int number)
        {
            var doc=new LaunchArtDocument(LaunchArtDocument.DefaultRegistry,LaunchArtDocument.DefaultPlan);
            var slot=doc.Plan.Slots[number-1];
            var profile=doc.Profile(slot);
            string thresholds=File.ReadAllText(LaunchArtDocument.ThresholdsPath);
            var normal=ProfileLoader.ReadThresholds(thresholds,profile.BandId);
            var big=ProfileLoader.ReadBigThresholds(thresholds,profile.BandId);
            var library=ContentStore.LoadLibrary(doc.LibraryDirectory);
            var pairs=ContentStore.LoadPairs("content/readability/approved-pairs.json")!;
            var generator=new LevelGenerator(profile,new PicturePicker(library,plan:n=>doc.Compositions[n].PictureId),normal,pairs.IsApproved,
                new DifficultySchedule(doc.Plan.ScheduleSeed,UnlockRoadmap.Default)) {
                UseAuthoredArtTray=true,RecipeOf=n=>(profile,normal,big),ClassOf=n=>slot.Difficulty,
                DefinitionVersionOf=doc.DefinitionVersion,MechanicsOf=n=>slot.RequiredMechanic==null?null:new[]{slot.RequiredMechanic}
            };
            var result=generator.Generate(number,number,doc.Plan.Seed,new Dictionary<int,LevelDefinition>());
            Assert.That(result.Failed,Is.Empty,string.Join("; ",result.Rejections.Select(r=>r.Reason)));
            var generated=result.Accepted.Single();
            Assert.That(generated.Definition.GeneratorVersion,Is.EqualTo(LevelGenerator.AuthoredArtVersion));
            Assert.That(doc.CheckDefinitions(new[]{generated.Definition},false),Is.Empty);
            var report=new CatalogValidator(library,UnlockRoadmap.Default,pairs,new SolveOptions(600000)).Validate(new[]{generated.Definition});
            Assert.That(report.Issues.Where(i=>i.IsError).Select(i=>i.Message),Is.Empty);
            var certificate=report.Records[number];
            var calibrated=ProfileLoader.ReadThresholdsFor(thresholds,profile.BandId,slot.Width*slot.Height);
            Assert.That(generated.Definition.Difficulty.Overridden,Is.False);
            Assert.That(DifficultyScorer.Score(certificate.Metrics,calibrated),Is.EqualTo(generated.Definition.Difficulty.Score));
            Assert.That(DifficultyScorer.Classify(generated.Definition.Difficulty.Score,calibrated),Is.EqualTo(slot.Difficulty));
            Assert.That(certificate.SolutionTrace,Is.Not.Empty);
            Assert.That(certificate.JamWitness,Is.Not.Empty);
            var picture=library.Single(p=>p.Id==generated.Definition.Picture.Id);
            var session=LevelSession.Load(generated.Definition,picture,new SessionOptions(0,20000));
            var win=new Solver.Solver().Solve(session,new SolveOptions(600000));
            foreach(var move in win.Trace)Assert.That(session.Apply(move).Accepted,Is.True);
            Assert.That(session.Status,Is.EqualTo(LevelStatus.Won));
        }
    }
}
