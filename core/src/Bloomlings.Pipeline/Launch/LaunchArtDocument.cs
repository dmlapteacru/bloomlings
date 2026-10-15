using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bloomlings.Content.Json;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Variants;
using Bloomlings.Generator;
using Bloomlings.Generator.Profiles;
using Bloomlings.Pipeline.Catalog;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Launch
{
    public sealed record LaunchComposition(string Id,string PictureId,int Version,int Level,int Width,int Height,
        string Status,IReadOnlyDictionary<string,VariantId> Mapping,string? CuratedDefinition);

    /// <summary>Checked production registry and a frozen geometry plan. Adding art cannot move another level's field.</summary>
    public sealed class LaunchArtDocument
    {
        public const string DefaultRegistry="content/launch-art/compositions.json";
        public const string DefaultPlan="content/launch-art/plan.json";
        public const string ThresholdsPath="content/profiles/difficulty-thresholds-launch.json";
        public const string RecipeVersion="launch-recipe-1";
        public LaunchArtPlan Plan { get; }
        public string RegistryHash { get; }
        public string LibraryDirectory { get; }
        public IReadOnlyDictionary<int,LaunchComposition> Compositions { get; }
        private readonly IReadOnlyDictionary<int,(int Width,int Height)> _pins;
        private readonly IReadOnlyDictionary<int,int> _definitionVersions;

        public LaunchArtDocument(string registry,string? existingPlan=null,ulong seed=1)
        {
            JObject data=JsonDoc.ParseObject(File.ReadAllText(registry),registry);
            if(data.Value<int>("schemaVersion")!=1||data.Value<int>("launchCompositionTarget")!=2500)
                throw new ArgumentException("The production registry must target exactly 2500 levels (schema 1)");
            if(data["policy"]?.Value<bool>("oneCompositionPerLaunchLevel")!=true||data["policy"]?.Value<bool>("resizeMirrorRecolorAreNewComposition")!=false
                ||data["policy"]?.Value<bool>("dailyIsSeparate")!=true) throw new ArgumentException("Invalid launch composition policy");
            RegistryHash=PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(CanonicalJson.Write(data,false)));
            LibraryDirectory=data.Value<string>("libraryDirectory")??throw new ArgumentException("Missing libraryDirectory");
            var compositions=new SortedDictionary<int,LaunchComposition>();
            var ids=new HashSet<string>(StringComparer.Ordinal);var pictures=new HashSet<string>(StringComparer.Ordinal);
            foreach(JObject e in (JArray)(data["compositions"]??throw new ArgumentException("Missing compositions")))
            {
                string Id(string key)
                {
                    string value=e.Value<string>(key)??"";
                    if(!Regex.IsMatch(value,"^[a-z][a-z0-9_]*$")) throw new ArgumentException("Invalid "+key+": "+value);
                    return value;
                }
                string id=Id("compositionId"),picture=Id("pictureId");int n=e.Value<int>("launchLevel");
                if(n<1||n>2500||compositions.ContainsKey(n)||!ids.Add(id)||!pictures.Add(picture)) throw new ArgumentException("Duplicate/out-of-range production assignment: "+id);
                var mapping=new SortedDictionary<string,VariantId>(StringComparer.Ordinal);
                foreach(var role in ((JObject)(e["previewMapping"]??throw new ArgumentException("Missing previewMapping"))).Properties())
                {
                    var variant=new VariantId(role.Value.Value<string>()!);VariantCatalog.Default.Get(variant);mapping.Add(role.Name,variant);
                }
                compositions.Add(n,new LaunchComposition(id,picture,e.Value<int>("version"),n,e.Value<int>("width"),e.Value<int>("height"),
                    e.Value<string>("artStatus")??"missing",mapping,e.Value<string>("curatedDefinition")));
            }
            Compositions=compositions;
            if(existingPlan!=null&&File.Exists(existingPlan))
            {
                JObject old=JObject.Parse(File.ReadAllText(existingPlan));
                if(old.Value<string>("version")!=LaunchArtPlan.Version||old.Value<int>("levelCount")!=2500) throw new ArgumentException("Unsupported launch plan");
                var pins=new SortedDictionary<int,(int,int)>();
                foreach(JObject p in (JArray)old["sizePins"]!) pins.Add(p.Value<int>("level"),(p.Value<int>("width"),p.Value<int>("height")));
                _definitionVersions=((JArray)old["slots"]!).ToDictionary(s=>s.Value<int>("level"),s=>s.Value<int>("definitionVersion"));
                if(_definitionVersions.Count!=2500||_definitionVersions.Values.Any(v=>v<1))throw new ArgumentException("Launch plan requires a positive definitionVersion for every slot");
                _pins=pins;
                var slots=((JArray)old["slots"]!).Cast<JObject>().Select(s=>new LaunchArtSlot(s.Value<int>("level"),s.Value<int>("width"),s.Value<int>("height"),
                    s.Value<string>("sizeGroup")!,s.Value<string>("difficulty") switch { "normal"=>DifficultyClass.Normal,"hard"=>DifficultyClass.Hard,"super_hard"=>DifficultyClass.SuperHard,_=>throw new ArgumentException("Invalid class") },
                    s.Value<string>("pace")!,s.Value<string>("artKind")!,string.IsNullOrEmpty(s.Value<string>("requiredMechanic"))?null:s.Value<string>("requiredMechanic"),
                    ((JArray)s["allowedMechanics"]!).Values<string>().Select(v=>v!).ToArray(),
                    new IntRange(s["variantCount"]![0]!.Value<int>(),s["variantCount"]![1]!.Value<int>()))).ToArray();
                Plan=LaunchArtPlan.Restore(old.Value<ulong>("seed"),old.Value<ulong>("scheduleSeed"),slots);
                if(old.Value<string>("geometryHash") is string hash && hash!=GeometryHash())throw new ArgumentException("Frozen geometry hash mismatch");
            }
            else
            {
                _pins=compositions.ToDictionary(e=>e.Key,e=>(e.Value.Width,e.Value.Height));
                var baseline=ContentStore.LoadLevels("content/catalog",2500).Concat(ContentStore.LoadLevels("content/curated",10))
                    .GroupBy(d=>d.Level.LevelNumber).ToDictionary(g=>g.Key,g=>g.Max(d=>d.Level.DefinitionVersion));
                _definitionVersions=Enumerable.Range(1,2500).ToDictionary(n=>n,n=>n<=10?baseline.GetValueOrDefault(n,1):baseline.GetValueOrDefault(n,0)+1);
                Plan=LaunchArtPlan.Build(seed,_pins);
            }
            foreach(var e in compositions.Values)
            {
                var slot=Plan.Slots[e.Level-1];
                if(slot.Width!=e.Width||slot.Height!=e.Height) throw new ArgumentException($"{e.Id} does not fit frozen L{e.Level} ({slot.Width}x{slot.Height})");
            }
        }

        public JObject Write()
        {
            return new JObject {
                ["version"]=LaunchArtPlan.Version,["levelCount"]=2500,["seed"]=Plan.Seed,["scheduleSeed"]=Plan.ScheduleSeed,
                ["registryHash"]=RegistryHash,["geometryHash"]=GeometryHash(),["registeredCompositions"]=Compositions.Count,
                ["sizePins"]=new JArray(_pins.OrderBy(p=>p.Key).Select(p=>new JObject{["level"]=p.Key,["width"]=p.Value.Width,["height"]=p.Value.Height})),
                ["bands"]=new JArray(LaunchArtPlan.Bands.Select(b=>new JObject{["first"]=b.First,["last"]=b.Last,["quotas"]=new JArray(LaunchArtPlan.Quotas(b.Last-b.First+1,b.Shares))})),
                ["slots"]=new JArray(Plan.Slots.Select(SlotJson)),
            };
        }
        private string GeometryHash()
        {
            var slots=new JArray(Plan.Slots.Select(s=>
            {
                JObject item=SlotJson(s);item.Remove("compositionId");item.Remove("pictureId");item.Remove("artStatus");return item;
            }));
            var root=new JObject{["version"]=LaunchArtPlan.Version,["seed"]=Plan.Seed,["scheduleSeed"]=Plan.ScheduleSeed,["slots"]=slots};
            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(CanonicalJson.Write(root,false)));
        }
        public JObject SlotJson(LaunchArtSlot slot)
        {
            Compositions.TryGetValue(slot.Level,out var art);
            return new JObject { ["level"]=slot.Level,["definitionVersion"]=DefinitionVersion(slot.Level),["width"]=slot.Width,["height"]=slot.Height,["sizeGroup"]=slot.SizeGroup,
                ["difficulty"]=ClassName(slot.Difficulty),["pace"]=slot.Pace,["artKind"]=slot.ArtKind,
                ["requiredMechanic"]=slot.RequiredMechanic==null?JValue.CreateNull():new JValue(slot.RequiredMechanic),["allowedMechanics"]=new JArray(slot.AllowedMechanics),
                ["variantCount"]=new JArray(slot.VariantCount.Min,slot.VariantCount.Max),
                ["compositionId"]=art==null?JValue.CreateNull():new JValue(art.Id),["pictureId"]=art==null?JValue.CreateNull():new JValue(art.PictureId),["artStatus"]=art?.Status??"missing" };
        }
        public static string ClassName(DifficultyClass c)=>c==DifficultyClass.SuperHard?"super_hard":c.ToString().ToLowerInvariant();
        public int DefinitionVersion(int level)=>_definitionVersions[level];
        public string RecipeHash(int n, string? generatorVersion = null)
        {
            var art=Compositions[n];
            string profile=File.ReadAllText("content/profiles/"+ProfileName(Math.Max(11,n))+".json");
            string picture=BasePictureJson.Write(BasePictureJson.Read(File.ReadAllText(Path.Combine(LibraryDirectory,art.PictureId+".json"))));
            string curated=art.CuratedDefinition==null?"":File.ReadAllText(art.CuratedDefinition);
            string text=RecipeVersion+"\n"+(generatorVersion??LevelGenerator.AuthoredArtVersion)+"\n"+Plan.Seed+"\n"+Plan.ScheduleSeed+"\n"+SlotJson(Plan.Slots[n-1])+"\n"+profile+"\n"+picture+"\n"+curated+"\n"
                +File.ReadAllText(ThresholdsPath)+"\n"+File.ReadAllText("content/readability/approved-pairs.json");
            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(text));
        }
        public static string ProfileName(int n)=>n<=25?"band-0011-0025":n<=50?"band-0026-0050":n<=100?"band-0051-0100":n<=250?"band-0101-0250":n<=500?"band-0251-0500":n<=1000?"band-0501-1000":n<=2000?"band-1001-2000":"band-2001-5000";

        public void RequireCurrent(string planPath)
        {
            if(!JToken.DeepEquals(JObject.Parse(File.ReadAllText(planPath)),JObject.Parse(CanonicalJson.Write(Write(),false))))
                throw new ArgumentException("Launch plan inventory is stale or modified; run launch plan to refresh it");
        }
        public IReadOnlyList<string> CheckDefinitions(IEnumerable<LevelDefinition> definitions, bool complete)
        {
            var levels=definitions.OrderBy(d=>d.LevelNumber).ToArray();var errors=new List<string>();
            if(complete&&!levels.Select(d=>d.LevelNumber).SequenceEqual(Enumerable.Range(1,2500))) errors.Add("Launch requires every level L1–2500 exactly once");
            var used=new HashSet<string>(StringComparer.Ordinal);
            foreach(var level in levels)
            {
                if(level.LevelNumber<1||level.LevelNumber>2500||!Compositions.TryGetValue(level.LevelNumber,out var art))
                { errors.Add("Unassigned level "+level.LevelNumber);continue; }
                var slot=Plan.Slots[level.LevelNumber-1];
                if(!used.Add(art.Id))errors.Add("Repeated composition "+art.Id);
                if(level.DefinitionVersion!=DefinitionVersion(level.LevelNumber)||level.Picture.Id!=art.PictureId||level.Picture.Version!=art.Version||level.Picture.Mirror!=Mirror.None
                    ||!LevelGenerator.SameMapping(level.Mapping,art.Mapping)||level.Difficulty.Class!=slot.Difficulty)
                    errors.Add($"L{level.LevelNumber}: picture, palette, mirror or difficulty differs from its production assignment");
                if(slot.RequiredMechanic!=null&&!level.Mechanics.Contains(slot.RequiredMechanic)) errors.Add($"L{level.LevelNumber}: missing {slot.RequiredMechanic}");
            }
            return errors;
        }

        public GenerationProfile Profile(LaunchArtSlot slot)
        {
            var art=Compositions[slot.Level];
            var original=ProfileLoader.ReadFile("content/profiles/"+ProfileName(slot.Level)+".json");
            int count=art.Mapping.Values.Distinct().Count();
            return original with { BoardWidth=new IntRange(slot.Width,slot.Width),BoardHeight=new IntRange(slot.Height,slot.Height),
                VariantCount=new IntRange(count,count),FixedMapping=art.Mapping,PictureMirror=Mirror.None,AllowRoleMerge=slot.Level<=10,
                AllowedVariants=art.Mapping.Values.Distinct().OrderBy(v=>v.Key,StringComparer.Ordinal).ToArray(),
                AllowedMechanics=slot.AllowedMechanics,ReadableMechanics=true };
        }
        public IReadOnlyList<string> CheckArt(IEnumerable<int> levels)
        {
            var errors=new List<string>();var signatures=new Dictionary<string,int>(StringComparer.Ordinal);
            var pairs=ContentStore.LoadPairs("content/readability/approved-pairs.json")??throw new IOException("Missing readable pairs");
            foreach(int n in levels)
            {
                if(!Compositions.TryGetValue(n,out var art)) { errors.Add($"L{n}: missing composition");continue; }
                if(art.Status!="approved") { errors.Add($"L{n}: art status {art.Status}");continue; }
                string path=Path.Combine(LibraryDirectory,art.PictureId+".json");
                if(!File.Exists(path)) { errors.Add($"L{n}: missing picture {art.PictureId}");continue; }
                BasePicture p=BasePictureJson.Read(File.ReadAllText(path));var slot=Plan.Slots[n-1];
                if(n>10)foreach(string problem in Bloomlings.Pipeline.Pictures.PictureChecks.Problems(p))errors.Add($"L{n}: {problem}");
                if(n>10&&AuthoredTrayBuilder.InitialBlockedPodCapacity(p)<5)
                    errors.Add($"L{n}: insufficient hidden-color work at entry; add a clear background margin");
                if(p.Id!=art.PictureId||p.Version!=art.Version||p.Width!=slot.Width||p.Height!=slot.Height||p.Review.Status!=ReviewStatus.Approved)
                { errors.Add($"L{n}: picture identity, size or automated review mismatch");continue; }
                if(n>10&&RoleMapper.Mappings(p,Profile(slot),pairs.IsApproved,extraVariants:VariantPool.Default.ExpansionsAt(n,UnlockRoadmap.Default)).Count!=1)
                    errors.Add($"L{n}: palette violates roles, readable pairs, allowed variants or count");
                if(n>10&&p.Structure!=null)
                {
                    var targets=Profile(slot).StructureTargets;
                    if(!targets.BackgroundSharePermille.Contains(p.Structure.BackgroundSharePermille))
                        errors.Add($"L{n}: background {p.Structure.BackgroundSharePermille} outside {targets.BackgroundSharePermille}");
                    if(!targets.NestingDepth.Contains(p.Structure.NestingDepth))
                        errors.Add($"L{n}: nesting {p.Structure.NestingDepth} outside {targets.NestingDepth}");
                }
                if(!slot.VariantCount.Contains(art.Mapping.Values.Distinct().Count())) errors.Add($"L{n}: palette count outside progression limits");
                var joining=VariantPool.Default.JoiningAt(n,UnlockRoadmap.Default).Concat(VariantPool.Default.JoiningAt(n-1,UnlockRoadmap.Default));
                foreach(var variant in joining)
                    if(!art.Mapping.Values.Contains(variant))errors.Add($"L{n}: palette must introduce/practice {variant.Key}");
                string signature=GridSignature(p);
                if(signatures.TryGetValue(signature,out int previous)) errors.Add($"L{n}: structural copy of L{previous}");
                else signatures.Add(signature,n);
            }
            foreach(int n in levels.Where(n=>n>=11).Distinct())
                if(Compositions.TryGetValue(n,out var current)&&Compositions.TryGetValue(n-1,out var previous)&&Compositions.TryGetValue(n-2,out var earlier))
                {
                    string Palette(LaunchComposition c)=>string.Join(",",c.Mapping.Values.Distinct().Select(v=>v.Key).OrderBy(v=>v,StringComparer.Ordinal));
                    if(Palette(current)==Palette(previous)&&Palette(current)==Palette(earlier))
                        errors.Add($"L{n}: fixed palettes repeat the same variant set three levels in a row");
                }
            return errors;
        }

        /// <summary>Canonical grid partitions: colors, rotations, mirrors, empty margins and simple nearest scaling are ignored.</summary>
        public static string GridSignature(BasePicture picture)
        {
            var rows=picture.Grid.Select(r=>r.ToArray()).ToList();
            while(rows.Count>0&&rows[0].All(v=>v==-1))rows.RemoveAt(0);
            while(rows.Count>0&&rows[^1].All(v=>v==-1))rows.RemoveAt(rows.Count-1);
            if(rows.Count==0)throw new ArgumentException("Empty picture");
            while(rows.All(r=>r[0]==-1))rows=rows.Select(r=>r.Skip(1).ToArray()).ToList();
            while(rows.All(r=>r[^1]==-1))rows=rows.Select(r=>r.Take(r.Length-1).ToArray()).ToList();
            rows=rows.Where((r,i)=>i==0||!r.SequenceEqual(rows[i-1])).ToList();
            int[] cols=Enumerable.Range(0,rows[0].Length).Where(x=>x==0||!rows.All(r=>r[x]==r[x-1])).ToArray();
            rows=rows.Select(r=>cols.Select(x=>r[x]).ToArray()).ToList();
            var signatures=new List<string>();
            for(int transpose=0;transpose<2;transpose++)
                for(int fy=0;fy<2;fy++) for(int fx=0;fx<2;fx++)
                {
                    int h=transpose==0?rows.Count:rows[0].Length,w=transpose==0?rows[0].Length:rows.Count;
                    var labels=new Dictionary<int,int>();var text=new StringBuilder().Append(w).Append('x').Append(h).Append(':');
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                    {
                        int sx=fx==0?x:w-1-x,sy=fy==0?y:h-1-y;
                        int c=transpose==0?rows[sy][sx]:rows[sx][sy];
                        if(c>=0) { if(!labels.TryGetValue(c,out int label)){label=labels.Count;labels.Add(c,label);} c=label; }
                        text.Append(c).Append(',');
                    }
                    signatures.Add(text.ToString());
                }
            return signatures.OrderBy(s=>s,StringComparer.Ordinal).First();
        }
    }
}
