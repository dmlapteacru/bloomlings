using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Launch;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Commands
{
    public static class LaunchArtCommand
    {
        public const string BuildVersion="launch-build-1";
        public static Command Create()
        {
            var root=new Command("launch","Production art plan and exact one-composition-per-level launch generation.");
            root.Subcommands.Add(Plan());root.Subcommands.Add(Status());root.Subcommands.Add(Generate());root.Subcommands.Add(Install());
            return root;
        }
        private static Option<string> Registry()=>Cli.Path("--registry",LaunchArtDocument.DefaultRegistry,"Production composition registry.");
        private static Option<string> PlanPath()=>Cli.Path("--plan",LaunchArtDocument.DefaultPlan,"Frozen 2500-slot production plan.");
        public static Command Install()
        {
            var cmd=new Command("install","Install a complete matching local launch build, keeping a rollback backup and Daily assets.");
            var registry=Registry();var plan=PlanPath();var source=Cli.Path("--from","content/work/launch-catalog","Completed local build.");
            cmd.Options.Add(registry);cmd.Options.Add(plan);cmd.Options.Add(source);
            cmd.SetAction(p=>Cli.Run(p,report=>
            {
                var doc=new LaunchArtDocument(p.GetValue(registry)!,p.GetValue(plan));doc.RequireCurrent(p.GetValue(plan)!);
                string backup=LaunchInstaller.Install(doc,p.GetValue(source)!);
                report["installedLevels"]=2500;report["backup"]=backup;
                Cli.Say(p,"Installed L1–2500 source data. Previous catalog and picture library: "+backup);
                return 0;
            }));return cmd;
        }
        public static Command Plan()
        {
            var cmd=new Command("plan","Create the full production plan, or refresh artwork without moving existing fields.");
            var registry=Registry();var path=PlanPath();var seed=Cli.Int("--seed",1,"Initial planning seed; an existing plan retains its own seed.");
            cmd.Options.Add(registry);cmd.Options.Add(path);cmd.Options.Add(seed);
            cmd.SetAction(p=>Cli.Run(p,report=>
            {
                string output=p.GetValue(path)!;
                if(p.GetValue(seed)<0)throw new ArgumentException("Planning seed must be nonnegative");
                var doc=new LaunchArtDocument(p.GetValue(registry)!,File.Exists(output)?output:null,(ulong)p.GetValue(seed));
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                File.WriteAllText(output,CanonicalJson.Write(doc.Write(),true));
                string csv=Path.ChangeExtension(output,"tsv");
                var lines=new List<string>{"level\tdefinitionVersion\twidth\theight\tsizeGroup\tdifficulty\tpace\tartKind\tvariantMin\tvariantMax\trequiredMechanic\tcompositionId\tartStatus"};
                foreach(var s in doc.Plan.Slots)
                {
                    doc.Compositions.TryGetValue(s.Level,out var art);
                    lines.Add($"{s.Level}\t{doc.DefinitionVersion(s.Level)}\t{s.Width}\t{s.Height}\t{s.SizeGroup}\t{LaunchArtDocument.ClassName(s.Difficulty)}\t{s.Pace}\t{s.ArtKind}\t{s.VariantCount.Min}\t{s.VariantCount.Max}\t{s.RequiredMechanic}\t{art?.Id}\t{art?.Status??"missing"}");
                }
                File.WriteAllText(csv,string.Join("\n",lines)+"\n");
                report["plannedLevels"]=2500;report["registeredCompositions"]=doc.Compositions.Count;report["scheduleSeed"]=doc.Plan.ScheduleSeed;
                report["bands"]=doc.Write()["bands"]!.DeepClone();
                Cli.Say(p,$"launch plan: 2500 slots; {doc.Compositions.Count} registered compositions; geometry, classes and palettes are separate from generated levels.\n{output}\n{csv}");
                return 0;
            }));return cmd;
        }
        public static Command Status()
        {
            var cmd=new Command("status","Report real artwork coverage and blockers without generating levels.");var registry=Registry();var path=PlanPath();
            cmd.Options.Add(registry);cmd.Options.Add(path);
            cmd.SetAction(p=>Cli.Run(p,report=>
            {
                var doc=new LaunchArtDocument(p.GetValue(registry)!,p.GetValue(path));
                var missing=Enumerable.Range(1,2500).Where(n=>!doc.Compositions.ContainsKey(n)).ToArray();
                var errors=doc.CheckArt(doc.Compositions.Keys);
                report["plannedLevels"]=2500;report["registeredCompositions"]=doc.Compositions.Count;report["missingArt"]=new JArray(missing);
                report["artErrors"]=new JArray(errors);report["complete"]=missing.Length==0&&errors.Count==0;
                Cli.Say(p,$"launch: {doc.Compositions.Count}/2500 registered; {missing.Length} missing; {errors.Count} art errors.");
                foreach(string error in errors.Take(20))Cli.Say(p,error);
                return errors.Count==0?0:1;
            }));return cmd;
        }
        public static Command Generate()
        {
            var cmd=new Command("generate","Generate the assigned launch art locally; refuse missing art and resume only matching recipes.");
            var registry=Registry();var path=PlanPath();var output=Cli.Path("--out","content/work/launch-catalog","Isolated generated catalog, inside content/work.");
            var range=Cli.Path("--levels","1-2500","Explicit subset for engineering checks; default is the full launch.");
            var jobs=Cli.Int("--jobs",2,"Parallel workers; fixed segments keep definitions independent of worker count.");
            var retries=Cli.Int("--retries",2,"Additional seeds for failed assigned puzzles; never changes their artwork.");
            foreach(Option option in new Option[]{registry,path,output,range,jobs,retries})cmd.Options.Add(option);
            cmd.SetAction(p=>Cli.Run(p,report=>
            {
                var doc=new LaunchArtDocument(p.GetValue(registry)!,p.GetValue(path));doc.RequireCurrent(p.GetValue(path)!);
                (int first,int last)=Cli.Range(p.GetValue(range)!);
                if(last>2500||p.GetValue(jobs)<1||p.GetValue(retries)<0)throw new ArgumentException("Use L1–2500, positive jobs and nonnegative retries");
                int[] requested=Enumerable.Range(first,last-first+1).ToArray();
                var missing=requested.Where(n=>!doc.Compositions.ContainsKey(n)).ToArray();
                var artErrors=doc.CheckArt(doc.Compositions.Keys);
                report["missingArt"]=new JArray(missing);report["artErrors"]=new JArray(artErrors);
                if(missing.Length>0||artErrors.Count>0)
                {
                    Cli.Say(p,$"launch generation blocked: {missing.Length} requested levels lack compositions; {artErrors.Count} artwork errors. No catalog was written.");
                    foreach(string error in artErrors.Take(20))Cli.Say(p,error);
                    return 1;
                }
                string folder=Path.GetFullPath(p.GetValue(output)!);
                string work=Path.GetFullPath("content/work")+Path.DirectorySeparatorChar;
                if(!folder.StartsWith(work,StringComparison.Ordinal))throw new ArgumentException("Launch generation output must be inside content/work; publication is a separate checked step");
                string marker=Path.Combine(folder,"launch-build.json");
                if(Directory.Exists(folder)&&Directory.EnumerateFileSystemEntries(folder).Any()&&!File.Exists(marker))
                    throw new ArgumentException("Output is not a recognized launch build; choose an empty content/work folder");
                if(File.Exists(marker)&&JObject.Parse(File.ReadAllText(marker)).Value<string>("version")!=BuildVersion)
                    throw new ArgumentException("Output belongs to a different build format");
                Directory.CreateDirectory(folder);
                using var buildLock=new FileStream(Path.Combine(folder,".launch-build.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                Directory.CreateDirectory(Path.Combine(folder,"recipes"));
                ContentStore.AtomicText(marker,CanonicalJson.Write(new JObject{["version"]=BuildVersion,["complete"]=false,["registryHash"]=doc.RegistryHash},true));
                var library=ContentStore.LoadLibrary(doc.LibraryDirectory);var byId=library.ToDictionary(b=>b.Id,StringComparer.Ordinal);
                var pairs=ContentStore.LoadPairs("content/readability/approved-pairs.json")!;
                string thresholdsJson=File.ReadAllText(LaunchArtDocument.ThresholdsPath);
                var recipes=doc.Plan.Slots.Where(s=>s.Level>10&&doc.Compositions.ContainsKey(s.Level)).ToDictionary(s=>s.Level,s=>
                {
                    GenerationProfile profile=doc.Profile(s);
                    return (Profile:profile,Thresholds:ProfileLoader.ReadThresholds(thresholdsJson,profile.BandId),Big:ProfileLoader.ReadBigThresholds(thresholdsJson,profile.BandId));
                });
                string RecipeHash(int n)=>doc.RecipeHash(n);
                var history=new SortedDictionary<int,LevelDefinition>();var kept=new HashSet<int>();
                var records=ContentStore.LoadRecords(folder);var hashes=new Dictionary<int,string>();
                foreach(var item in ContentStore.LoadLevels(folder))
                {
                    int n=item.Level.LevelNumber;
                    if(n<1||n>2500||!doc.Compositions.ContainsKey(n))throw new ArgumentException("Cached level has no current launch assignment: "+n);
                    string recipePath=Path.Combine(folder,"recipes",ContentStore.LevelFileName(n));
                    string expected=RecipeHash(n);hashes[n]=expected;
                    if(File.Exists(recipePath)&&JObject.Parse(File.ReadAllText(recipePath)).Value<string>("recipeHash")==expected
                        &&records.TryGetValue(n,out var record)&&record.DefinitionHash==ValidationRecord.HashOf(item.Level)
                        &&record.Result==ValidationResult.Solvable&&doc.CheckDefinitions(new[]{item.Level},false).Count==0)
                    {history[n]=item.Level;kept.Add(n);}
                    else
                    {
                        File.Delete(item.File);
                        string certificate=Path.Combine(folder,"validation",ContentStore.LevelFileName(n));if(File.Exists(certificate))File.Delete(certificate);
                    }
                }
                // Curated teaching layouts and commands remain fixed, but are part of the unique composition audit.
                foreach(int n in requested.Where(n=>n<=10))
                {
                    var art=doc.Compositions[n];
                    if(art.CuratedDefinition==null)throw new ArgumentException("Tutorial must retain its curated teaching definition: L"+n);
                    var definition=DefinitionJson.Read(File.ReadAllText(art.CuratedDefinition));
                    if(doc.CheckDefinitions(new[]{definition},false).Count>0)throw new ArgumentException("Curated tutorial differs from its production assignment: L"+n);
                    history[n]=definition;kept.Add(n);
                }
                var progressLock=new object();
                var rejections=new List<Rejection>();
                LevelGenerator NewGenerator()
                {
                    var baseProfile=ProfileLoader.ReadFile("content/profiles/band-0011-0025.json");
                    return new LevelGenerator(baseProfile,new PicturePicker(library,plan:n=>doc.Compositions.TryGetValue(n,out var a)?a.PictureId:null),
                        ProfileLoader.ReadThresholds(thresholdsJson,baseProfile.BandId),pairs.IsApproved,new DifficultySchedule(doc.Plan.ScheduleSeed,UnlockRoadmap.Default))
                    {
                        RecipeOf=n=>recipes[n],ClassOf=n=>doc.Plan.Slots[n-1].Difficulty,UseAuthoredArtTray=true,
                        DefinitionVersionOf=doc.DefinitionVersion,
                        Rejected=r=>{lock(progressLock)rejections.Add(r);},
                        MechanicsOf=n=>doc.Plan.Slots[n-1].RequiredMechanic is string m?new[]{m}:null,
                        Progress=(n,accepted,candidates)=>{lock(progressLock) {
                            if(accepted!=null) {
                                ContentStore.WriteLevelAtomic(folder,accepted.Definition,accepted.Record);
                                ContentStore.AtomicText(Path.Combine(folder,"recipes",ContentStore.LevelFileName(n)),CanonicalJson.Write(new JObject{["recipeHash"]=RecipeHash(n)},true));
                            }
                            Console.Error.WriteLine(accepted==null?$"  L{n}: failed after {candidates} candidates":$"  L{n}: {accepted.Definition.Difficulty.Class}, {accepted.Definition.Picture.Id}, {candidates} candidates");
                            if(accepted==null)foreach(var reason in rejections.Where(r=>r.Level==n).GroupBy(r=>r.Reason).OrderByDescending(g=>g.Count()).Take(3))
                                Console.Error.WriteLine($"    {reason.Count()} × {reason.Key}");
                        }},
                    };
                }
                var (result,seams)=GenerateCommand.GenerateRange(NewGenerator,first,last,doc.Plan.Seed,history,kept,50,p.GetValue(jobs));
                var all=new SortedDictionary<int,GeneratedLevel>(result.Accepted.ToDictionary(l=>l.Definition.LevelNumber));
                var failed=new HashSet<int>(result.Failed);
                for(int retry=1;retry<=p.GetValue(retries)&&failed.Count>0;retry++)
                    foreach(int n in failed.OrderBy(n=>n).ToArray())
                    {
                        var attempt=NewGenerator().Regenerate(new[]{n},doc.Plan.Seed+(ulong)retry,history);
                        if(attempt.Accepted.Count==1) {all[n]=attempt.Accepted[0];failed.Remove(n);}
                    }
                foreach(var generated in all.Values) ContentStore.WriteLevelAtomic(folder,generated.Definition,generated.Record);
                Directory.CreateDirectory(Path.Combine(folder,"rejections"));
                foreach(var group in rejections.GroupBy(r=>r.Level))
                    File.WriteAllText(Path.Combine(folder,"rejections",ContentStore.LevelFileName(group.Key)),CanonicalJson.Write(new JObject {
                        ["level"]=group.Key,["reasons"]=new JArray(group.GroupBy(r=>r.Reason).OrderByDescending(g=>g.Count())
                            .ThenBy(g=>g.Key,StringComparer.Ordinal).Select(g=>new JObject{["reason"]=g.Key,["count"]=g.Count()})) },true));
                var selected=history.Values.Where(d=>requested.Contains(d.LevelNumber)).ToArray();
                var audit=doc.CheckDefinitions(selected,first==1&&last==2500).ToList();
                var validation=new CatalogValidator(library,UnlockRoadmap.Default,pairs,new SolveOptions(600000)).Validate(selected);
                audit.AddRange(validation.Issues.Where(i=>i.IsError).Select(i=>$"L{i.Level} {i.Check}: {i.Message}"));
                foreach(var definition in selected)
                {
                    int n=definition.LevelNumber;
                    if(validation.Records.TryGetValue(n,out var certificate))ContentStore.WriteLevelAtomic(folder,definition,certificate);
                    string hash=RecipeHash(n);
                    ContentStore.AtomicText(Path.Combine(folder,"recipes",ContentStore.LevelFileName(n)),CanonicalJson.Write(new JObject{["recipeHash"]=hash},true));
                }
                bool complete=first==1&&last==2500&&failed.Count==0&&audit.Count==0&&selected.Length==2500;
                report["generated"]=all.Count;report["resumed"]=kept.Count(n=>n>=first&&n<=last);report["failedLevels"]=new JArray(failed.OrderBy(n=>n));
                report["seamRepairs"]=seams;report["errors"]=new JArray(audit);report["complete"]=complete;
                ContentStore.AtomicText(marker,CanonicalJson.Write(new JObject{["version"]=BuildVersion,["complete"]=complete,["registryHash"]=doc.RegistryHash,
                    ["artifactHash"]=complete?new JValue(LaunchInstaller.Fingerprint(doc,selected,validation.Records)):JValue.CreateNull(),
                    ["generated"]=all.Count,["failedLevels"]=new JArray(failed.OrderBy(n=>n)),["errors"]=new JArray(audit)},true));
                foreach(string error in audit.Take(30))Cli.Say(p,error);
                Cli.Say(p,$"launch L{first}–{last}: {all.Count} generated, {kept.Count} fixed/resumed, {failed.Count} failed, {audit.Count} errors; complete={complete}.");
                return failed.Count==0&&audit.Count==0?0:1;
            }));return cmd;
        }
    }
}
