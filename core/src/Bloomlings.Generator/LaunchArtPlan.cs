using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Random;
using Bloomlings.Generator.Overlays;
using Bloomlings.Generator.Profiles;

namespace Bloomlings.Generator
{
    public sealed record LaunchArtSlot(int Level, int Width, int Height, string SizeGroup,
        DifficultyClass Difficulty, string Pace, string ArtKind, string? RequiredMechanic,
        IReadOnlyList<string> AllowedMechanics, IntRange VariantCount);

    /// <summary>The owner's launch quotas and pacing, independent of the number of drawings currently available.</summary>
    public sealed class LaunchArtPlan
    {
        public const int LevelCount = 2500;
        public const string Version = "launch-art-1";
        public ulong Seed { get; }
        public ulong ScheduleSeed { get; }
        public IReadOnlyList<LaunchArtSlot> Slots { get; }
        public static readonly string[] Groups = { "compact", "medium", "large", "xl" };
        public static readonly (int First, int Last, int[] Shares)[] Bands = {
            (11,25,new[]{45,35,20,0}), (26,50,new[]{25,30,40,5}), (51,100,new[]{15,20,55,10}),
            (101,300,new[]{15,15,55,15}), (301,2500,new[]{0,20,60,20}),
        };
        private LaunchArtPlan(ulong seed, ulong scheduleSeed, IReadOnlyList<LaunchArtSlot> slots)
        { Seed=seed; ScheduleSeed=scheduleSeed; Slots=slots; }

        /// <summary>Read frozen authoring geometry without rerolling it when new compositions are registered.</summary>
        public static LaunchArtPlan Restore(ulong seed,ulong scheduleSeed,IReadOnlyList<LaunchArtSlot> slots)
        {
            if(!slots.Select(s=>s.Level).SequenceEqual(Enumerable.Range(1,2500)))throw new ArgumentException("Plan must contain L1–2500 in order");
            var schedule=new DifficultySchedule(scheduleSeed,UnlockRoadmap.Default);schedule.ClassFor(2500);
            var required=new Dictionary<int,string>();
            foreach(string m in MechanicNames.All)
            {
                int? at=UnlockRoadmap.Default.LevelOf(MechanicNames.UnlockId(m));if(at==null)continue;
                required[at.Value]=m;
                if(MechanicNames.PracticeLevel(m,UnlockRoadmap.Default,schedule.ClassFor) is int p)required[p]=m;
            }
            foreach(var s in slots)
            {
                if(GroupOf(s.Width,s.Height,s.Level)!=s.SizeGroup||s.Difficulty!=schedule.ClassFor(s.Level))throw new ArgumentException("Invalid plan size/class at L"+s.Level);
                if(s.RequiredMechanic!=required.GetValueOrDefault(s.Level))throw new ArgumentException("Invalid mechanic introduction/practice at L"+s.Level);
                if(s.RequiredMechanic!=null&&!BoardMechanicReadability.Allows(s.RequiredMechanic,s.Width,s.Height))throw new ArgumentException("Unreadable mandatory mechanic at L"+s.Level);
                var variants=BandGuidelines.Variants(s.Level,s.Difficulty);
                if(s.VariantCount!=variants)throw new ArgumentException("Invalid variant budget at L"+s.Level);
                if(s.ArtKind!="simple"&&s.ArtKind!="story")throw new ArgumentException("Invalid art kind");
                foreach(string m in s.AllowedMechanics)
                    if(!UnlockRoadmap.Default.IsUnlockedAt(MechanicNames.UnlockId(m),s.Level)||!BoardMechanicReadability.Allows(m,s.Width,s.Height)
                        ||(m==MechanicNames.ConnectedTriple&&s.Difficulty==DifficultyClass.Normal))throw new ArgumentException("Invalid available mechanic at L"+s.Level);
                bool relief=s.Level>1&&slots[s.Level-2].Difficulty==DifficultyClass.SuperHard;
                if(relief&&(s.Difficulty!=DifficultyClass.Normal||s.Pace!="relief"||s.Width*s.Height>576||s.ArtKind!="simple"))throw new ArgumentException("Missing relief at L"+s.Level);
                if(!relief&&s.Pace!=(s.SizeGroup=="xl"?"long":"standard"))throw new ArgumentException("Invalid pace at L"+s.Level);
                if(s.SizeGroup=="xl"&&s.Level>1&&slots[s.Level-2].SizeGroup=="xl")throw new ArgumentException("Consecutive XL boards");
            }
            foreach(var b in Bands)
            {
                int[] quota=Quotas(b.Last-b.First+1,b.Shares);
                if(!Groups.Select(g=>slots.Count(s=>s.Level>=b.First&&s.Level<=b.Last&&s.SizeGroup==g)).SequenceEqual(quota))throw new ArgumentException("Invalid quotas in band "+b.First);
            }
            int firstMax=slots.Where(s=>s.Width==36).Select(s=>s.Level).DefaultIfEmpty(0).First();
            if(firstMax<75||firstMax>100||slots.Count(s=>s.Width==36)>=100)throw new ArgumentException("36x36 must remain a rare event, first at L75–100");
            return new LaunchArtPlan(seed,scheduleSeed,slots.ToArray());
        }

        public static int[] Quotas(int count, IReadOnlyList<int> shares)
        {
            if(count<0||shares.Sum()!=100||shares.Any(s=>s<0))throw new ArgumentException("Quotas require nonnegative shares totaling 100%");
            int[] q=shares.Select(s=>s*count/100).ToArray();
            foreach(int i in Enumerable.Range(0,shares.Count).OrderByDescending(i=>shares[i]*count%100).ThenBy(i=>i).Take(count-q.Sum())) q[i]++;
            return q;
        }

        public static IReadOnlyList<(int Width,int Height)> Sizes(string group, int level) => group switch
        {
            "tutorial" => new[]{(11,12),(12,12)},
            "compact" => level<=300 ? new[]{(14,16),(16,18)} : Array.Empty<(int,int)>(),
            "medium" => level<=300 ? new[]{(18,20),(20,24),(21,26)} : new[]{(21,24),(21,26),(22,26)},
            "large" => new[]{(24,24),(22,28),(24,28),(28,28)},
            "xl" => new[]{(30,32),(32,32),(36,36)},
            _=>throw new ArgumentException("Unknown size group "+group),
        };
        public static string GroupOf(int width,int height,int level) =>
            (level<=10?new[]{"tutorial"}:Groups).FirstOrDefault(g=>Sizes(g,level).Contains((width,height)))
                ?? throw new ArgumentException($"{width}x{height} is not a launch size at L{level}");

        public static LaunchArtPlan Build(ulong seed=1, IReadOnlyDictionary<int,(int Width,int Height)>? sizePins=null)
        {
            var roadmap=UnlockRoadmap.Default;
            ulong scheduleSeed=FindScheduleSeed(seed,roadmap);
            var schedule=new DifficultySchedule(scheduleSeed,roadmap);
            schedule.ClassFor(LevelCount);
            var required=new Dictionary<int,string>();
            foreach(string m in MechanicNames.All)
            {
                int? at=roadmap.LevelOf(MechanicNames.UnlockId(m));
                if(at==null) continue;
                required[at.Value]=m;
                if(MechanicNames.PracticeLevel(m,roadmap,schedule.ClassFor) is int practice) required[practice]=m;
            }
            var pins=sizePins==null?new Dictionary<int,(int Width,int Height)>():new Dictionary<int,(int Width,int Height)>(sizePins);
            foreach(var pin in pins)
            {
                if(pin.Key<1||pin.Key>LevelCount) throw new ArgumentException("Size pin outside L1–2500");
                GroupOf(pin.Value.Width,pin.Value.Height,pin.Key);
                if(required.TryGetValue(pin.Key,out string? m)&&!BoardMechanicReadability.Allows(m,pin.Value.Width,pin.Value.Height))
                    throw new ArgumentException($"Size pin at L{pin.Key} cannot show {m}");
            }
            bool Relief(int n)=>n>1&&schedule.ClassFor(n-1)==DifficultyClass.SuperHard;
            int firstMax=pins.Where(p=>p.Key>=75&&p.Key<=100&&p.Value==(36,36)&&!Relief(p.Key)&&!required.ContainsKey(p.Key)).Select(p=>p.Key).OrderBy(n=>n).FirstOrDefault();
            if(firstMax==0)firstMax=Enumerable.Range(85,16).FirstOrDefault(n=>!Relief(n)&&!required.ContainsKey(n)&&!pins.ContainsKey(n));
            if(firstMax==0)throw new ArgumentException("The first 36x36 board requires an eligible position in L75–100");
            pins[firstMax]=(36,36);
            var slots=new List<LaunchArtSlot>();
            var rng=new Xoshiro256StarStar(seed);
            LaunchArtSlot Make(int n,string group,(int Width,int Height) size)
            {
                string? mechanic=required.TryGetValue(n,out string? m)?m:null;
                string pace=Relief(n)?"relief":group=="xl"?"long":"standard";
                var available=MechanicNames.All.Where(m=>roadmap.IsUnlockedAt(MechanicNames.UnlockId(m),n)
                    &&BoardMechanicReadability.Allows(m,size.Width,size.Height)
                    &&(m!=MechanicNames.ConnectedTriple||schedule.ClassFor(n)!=DifficultyClass.Normal)).ToArray();
                return new LaunchArtSlot(n,size.Width,size.Height,group,schedule.ClassFor(n),pace,
                    n<50||pace=="relief"||group=="compact"||group=="medium"||group=="tutorial"?"simple":"story",
                    mechanic,available,BandGuidelines.Variants(n,schedule.ClassFor(n)));
            }
            for(int n=1;n<=10;n++) slots.Add(Make(n,"tutorial",pins.TryGetValue(n,out var s)?s:(n is 1 or 2 or 6 or 8?(11,12):(12,12))));
            foreach(var band in Bands)
            {
                int[] quotas=Quotas(band.Last-band.First+1,band.Shares);
                bool done=false;
                for(int attempt=0;attempt<128&&!done;attempt++)
                {
                    int[] remaining=(int[])quotas.Clone();
                    var assigned=new Dictionary<int,int>();
                    List<(int Width,int Height)> Options(int n,int group)
                    {
                        return Sizes(Groups[group],n).Where(s=>(!pins.TryGetValue(n,out var pin)||pin==s)
                            &&(!Relief(n)||s.Width*s.Height<=576)
                            &&(s!=(36,36)||n>=firstMax)
                            &&(!required.TryGetValue(n,out string? m)||BoardMechanicReadability.Allows(m,s.Width,s.Height))).ToList();
                    }
                    int[] Allowed(int n)=>Enumerable.Range(0,4).Where(g=>Options(n,g).Count>0).ToArray();
                    // Reserve every constrained slot before distributing the remaining quotas.
                    bool valid=true;
                    foreach(int n in Enumerable.Range(band.First,band.Last-band.First+1).OrderBy(n=>Allowed(n).Length).ThenBy(n=>n))
                    {
                        int[] groups=Allowed(n);
                        if(groups.Length==4) continue;
                        int[] choices=groups.Where(g=>remaining[g]>0&&!(g==3&&(assigned.GetValueOrDefault(n-1)==3||assigned.GetValueOrDefault(n+1)==3))).ToArray();
                        if(choices.Length==0) { valid=false; break; }
                        int pick=choices.OrderByDescending(g=>(double)remaining[g]/Math.Max(1,quotas[g])).ThenBy(g=>g).First();
                        if(groups.Length>1&&attempt>0) pick=choices[rng.NextInt(choices.Length)];
                        assigned[n]=pick; remaining[pick]--;
                    }
                    if(!valid) continue;
                    for(int n=band.First;n<=band.Last&&valid;n++)
                    {
                        if(assigned.ContainsKey(n)) continue;
                        int previous=n==band.First?(slots[^1].SizeGroup=="xl"?3:-1):assigned.GetValueOrDefault(n-1,-1);
                        int[] choices=Allowed(n).Where(g=>remaining[g]>0&&!(g==3&&(previous==3||assigned.GetValueOrDefault(n+1,-1)==3))).ToArray();
                        if(choices.Length==0) { valid=false; break; }
                        int pick=choices.OrderByDescending(g=>((double)(n-band.First+1)*quotas[g]/(band.Last-band.First+1))-(quotas[g]-remaining[g])).ThenBy(g=>g).First();
                        assigned[n]=pick; remaining[pick]--;
                    }
                    if(!valid||remaining.Any(x=>x!=0)) continue;
                    var batch=new List<LaunchArtSlot>();
                    for(int n=band.First;n<=band.Last;n++)
                    {
                        int g=assigned[n]; var options=Options(n,g);
                        // 36×36 is an event, roughly one in ten XL boards; relief uses the smallest available field.
                        (int Width,int Height) size;
                        if(Relief(n)) size=options.OrderBy(s=>s.Width*s.Height).First();
                        else if(g==3&&options.Count>1) { int roll=rng.NextInt(10); size=options[Math.Min(roll<6?0:roll<9?1:2,options.Count-1)]; }
                        else size=options[rng.NextInt(options.Count)];
                        batch.Add(Make(n,Groups[g],size));
                    }
                    if(slots[^1].SizeGroup=="xl"&&batch[0].SizeGroup=="xl") continue;
                    slots.AddRange(batch); done=true;
                }
                if(!done) throw new ArgumentException($"Unable to satisfy launch quotas and pins in L{band.First}–{band.Last}");
            }
            return new LaunchArtPlan(seed,scheduleSeed,slots);
        }

        private static ulong FindScheduleSeed(ulong seed,UnlockRoadmap roadmap)
        {
            var introductions=MechanicNames.All.Select(m=>(Mechanic:m,Level:roadmap.LevelOf(MechanicNames.UnlockId(m))))
                .Where(e=>e.Level>=11).ToArray();
            for(ulong offset=0;offset<100000;offset++)
            {
                ulong candidate=0xB100B100UL+seed+offset;
                var schedule=new DifficultySchedule(candidate,roadmap); schedule.ClassFor(LevelCount);
                if(introductions.Any(e=>schedule.ClassFor(e.Level!.Value)!=(e.Mechanic==MechanicNames.ConnectedTriple?DifficultyClass.Hard:DifficultyClass.Normal))) continue;
                bool good=true;
                for(int first=11;first+99<=LevelCount;first+=100)
                {
                    var classes=Enumerable.Range(first,100).Select(schedule.ClassFor).ToArray();
                    int hard=classes.Count(c=>c==DifficultyClass.Hard),super=classes.Count(c=>c==DifficultyClass.SuperHard);
                    if(hard<15||hard>25||super<6||super>10) { good=false;break; }
                }
                if(good) return candidate;
            }
            throw new ArgumentException("No launch difficulty schedule found for this seed");
        }
    }
}
