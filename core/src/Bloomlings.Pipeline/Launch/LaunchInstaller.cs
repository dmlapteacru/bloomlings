using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Validation;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Bloomlings.Pipeline.Validation;
using Bloomlings.Solver;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Launch
{
    public static class LaunchInstaller
    {
        public static string Fingerprint(LaunchArtDocument doc,IEnumerable<LevelDefinition> levels,IReadOnlyDictionary<int,ValidationRecord> records)
        {
            var text=new StringBuilder(doc.RegistryHash).Append('\n');
            foreach(var level in levels.OrderBy(l=>l.LevelNumber))
            {
                int n=level.LevelNumber;
                if(!records.TryGetValue(n,out var certificate))throw new ArgumentException("Missing certificate L"+n);
                text.Append(n).Append(':').Append(ValidationRecord.HashOf(level)).Append(':')
                    .Append(PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(certificate.Write()))).Append(':').Append(doc.RecipeHash(n)).Append('\n');
            }
            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text.ToString()));
        }

        /// <summary>Install only a complete checked local build. Old levels/pictures are retained together in a backup.</summary>
        public static string Install(LaunchArtDocument doc,string source)
        {
            string work=Path.GetFullPath("content/work")+Path.DirectorySeparatorChar;
            source=Path.GetFullPath(source);
            if(!source.StartsWith(work,StringComparison.Ordinal))throw new ArgumentException("Install source must be inside content/work");
            using var buildLock=new FileStream(Path.Combine(source,".launch-build.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            JObject marker=JObject.Parse(File.ReadAllText(Path.Combine(source,"launch-build.json")));
            if(marker.Value<string>("version")!=Commands.LaunchArtCommand.BuildVersion||marker.Value<bool>("complete")!=true||marker.Value<string>("registryHash")!=doc.RegistryHash)
                throw new ArgumentException("Only a complete L1–2500 build matching the current registry can be installed");
            int[] numbers=Enumerable.Range(1,2500).ToArray();
            var artErrors=doc.CheckArt(numbers);
            if(artErrors.Count>0)throw new ArgumentException(string.Join("; ",artErrors.Take(10)));
            var levels=ContentStore.LoadLevels(source).Select(l=>l.Level).ToArray();
            var errors=doc.CheckDefinitions(levels,true);
            if(errors.Count>0)throw new ArgumentException(string.Join("; ",errors.Take(10)));
            var records=ContentStore.LoadRecords(source);
            if(marker.Value<string>("artifactHash")!=Fingerprint(doc,levels,records))throw new ArgumentException("Generated artifact hash mismatch; rerun launch generate");
            var pictures=ContentStore.LoadLibrary(doc.LibraryDirectory).ToDictionary(p=>p.Id,StringComparer.Ordinal);
            string thresholds=File.ReadAllText(LaunchArtDocument.ThresholdsPath);
            foreach(var level in levels)
            {
                int n=level.LevelNumber;var record=records[n];
                string recipe=Path.Combine(source,"recipes",ContentStore.LevelFileName(n));
                if(!File.Exists(recipe)||JObject.Parse(File.ReadAllText(recipe)).Value<string>("recipeHash")!=doc.RecipeHash(n))
                    throw new ArgumentException("Stale recipe L"+n);
                if(record.DefinitionHash!=ValidationRecord.HashOf(level)||record.Result!=ValidationResult.Solvable||record.DefinitionVersion!=level.DefinitionVersion
                    ||!record.Checks.Contains("solvable")||(n>10&&!record.Checks.Contains("losable")))throw new ArgumentException("Invalid certificate L"+n);
                var picture=pictures[level.Picture.Id];
                void Replay(IReadOnlyList<Command> trace,LevelStatus expected)
                {
                    var session=LevelSession.Load(level,picture,new SessionOptions(1,20000));
                    foreach(var command in trace)if(!session.Apply(command).Accepted)throw new ArgumentException("Invalid replay L"+n);
                    if(session.Status!=expected)throw new ArgumentException("Replay has wrong outcome L"+n);
                }
                Replay(record.SolutionTrace,LevelStatus.Won);
                if(n>10)Replay(record.JamWitness??throw new ArgumentException("Missing jam witness L"+n),LevelStatus.Jammed);
                if(n<=10)
                {
                    if(DefinitionJson.Write(level)!=DefinitionJson.Write(DefinitionJson.Read(File.ReadAllText(doc.Compositions[n].CuratedDefinition!))))
                        throw new ArgumentException("Tutorial changed L"+n);
                }
                else
                {
                    var calibration=ProfileLoader.ReadThresholdsFor(thresholds,doc.Profile(doc.Plan.Slots[n-1]).BandId,picture.Width*picture.Height);
                    int score=DifficultyScorer.Score(record.Metrics,calibration);
                    if(level.Difficulty.Overridden||score!=level.Difficulty.Score||DifficultyScorer.Classify(score,calibration)!=level.Difficulty.Class)
                        throw new ArgumentException("Invalid computed class L"+n);
                }
            }
            var pairs=ContentStore.LoadPairs("content/readability/approved-pairs.json");
            var sequences=new CatalogValidator(pictures.Values,UnlockRoadmap.Default,pairs,new SolveOptions(600000)).Validate(levels,new HashSet<int>());
            if(sequences.HasErrors)throw new ArgumentException(string.Join("; ",sequences.Issues.Where(i=>i.IsError).Take(10).Select(i=>i.Message)));
            Directory.CreateDirectory(work);
            using var installLock=new FileStream(Path.Combine(work,".launch-install.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            string id=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");
            string stage=Path.Combine(work,"launch-install-"+id),backup=Path.Combine(work,"launch-backups",id);
            Directory.CreateDirectory(stage);Directory.CreateDirectory(backup);
            string catalog=Path.Combine(stage,"catalog"),library=Path.Combine(stage,"pictures");Directory.CreateDirectory(library);
            foreach(var level in levels.Where(l=>l.LevelNumber>10))ContentStore.WriteLevelAtomic(catalog,level,records[level.LevelNumber]);
            var shipping=new SortedSet<string>(StringComparer.Ordinal);
            foreach(var art in doc.Compositions.Values)
            {
                string file=art.PictureId+".json";
                File.Copy(Path.Combine(doc.LibraryDirectory,file),Path.Combine(library,file),true);shipping.Add(file);
            }
            // Daily remains a separate, unchanged catalog. Keep exactly the pictures it actually references.
            foreach(var daily in ContentStore.LoadLevels("content/daily").Select(l=>l.Level))
            {
                string file=daily.Picture.Id+".json",old=Path.Combine("content/pictures/lib",file),target=Path.Combine(library,file);
                var picture=BasePictureJson.Read(File.ReadAllText(old));
                if(picture.Version!=daily.Picture.Version)throw new ArgumentException("Daily picture version mismatch: "+file);
                if(File.Exists(target)&&BasePictureJson.Write(BasePictureJson.Read(File.ReadAllText(target)))!=BasePictureJson.Write(picture))
                    throw new ArgumentException("Daily picture conflicts with launch art: "+file);
                File.Copy(old,target,true);shipping.Add(file);
            }
            string props="content/launch-art/installed-assets.props";
            string? oldProps=File.Exists(props)?File.ReadAllText(props):null;
            string xml="<Project>\n  <PropertyGroup><LaunchArtInstalled>true</LaunchArtInstalled><LaunchArtRegistryHash>"+doc.RegistryHash+"</LaunchArtRegistryHash></PropertyGroup>\n  <ItemGroup>\n"
                +string.Join("\n",shipping.Select(f=>"    <LaunchInstalledPicture Include=\"$(MSBuildThisFileDirectory)../pictures/lib/"+SecurityElement.Escape(f)+"\" />"))+"\n  </ItemGroup>\n</Project>\n";
            if(oldProps!=null)File.WriteAllText(Path.Combine(backup,"installed-assets.props"),oldProps);
            var promotions=new[]{(catalog,Path.GetFullPath("content/catalog")),(library,Path.GetFullPath("content/pictures/lib"))};
            Promote(promotions,backup,()=>ContentStore.AtomicText(props,xml));
            ContentStore.AtomicText(Path.Combine(backup,"installation.json"),CanonicalJson.Write(new JObject{["source"]=source,["registryHash"]=doc.RegistryHash,["levels"]=2500,["pictures"]=shipping.Count},true));
            return backup;
        }

        /// <summary>Promote staged directories together; rollback every directory when any step fails.</summary>
        public static void Promote(IReadOnlyList<(string Stage,string Target)> items,string backup,Action? finish=null)
        {
            Directory.CreateDirectory(backup);
            var moved=new List<(string Target,string Saved,bool HadOld)>();
            try
            {
                foreach(var item in items)
                {
                    string saved=Path.Combine(backup,"directory-"+moved.Count);
                    bool old=Directory.Exists(item.Target);
                    if(old)Directory.Move(item.Target,saved);
                    moved.Add((item.Target,saved,old));
                    Directory.Move(item.Stage,item.Target);
                }
                finish?.Invoke();
            }
            catch
            {
                foreach(var item in moved.AsEnumerable().Reverse())
                {
                    if(Directory.Exists(item.Target))Directory.Delete(item.Target,true);
                    if(item.HadOld)Directory.Move(item.Saved,item.Target);
                }
                throw;
            }
        }
    }
}
