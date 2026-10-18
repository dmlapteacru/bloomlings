using System.Diagnostics;
using System.Text.Json;
using Bloomlings.Content.Json;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;

string root=FindRoot();
string outDir=Path.Combine(root,"content/work/gameplay-audit");
Directory.CreateDirectory(outDir);
int first=int.Parse(args.ElementAtOrDefault(0)??"1"),last=int.Parse(args.ElementAtOrDefault(1)??"100"),trials=int.Parse(args.ElementAtOrDefault(2)??"64");
if(first<1||last<first||last>2500||trials<1||trials>10000)throw new ArgumentException("Use 1 <= first <= last <= 2500 and 1 <= trials <= 10000.");
var watch=Stopwatch.StartNew();
string mode=args.ElementAtOrDefault(3)??"full";
if(mode is not ("full" or "sequential" or "doors"))throw new ArgumentException("Mode: full, sequential or doors.");
if(mode=="doors"){DoorAudit.Run(root,first,last);return;}
using var writer=new StreamWriter(Path.Combine(outDir,$"{mode}-{first}-{last}-{trials}.jsonl"));writer.AutoFlush=true;
for(int n=first;n<=last;n++)
{
 string file=n<=10?$"content/curated/level-{n:0000}.json":$"content/catalog/levels/level-{n:00000}.json";
 var def=DefinitionJson.Read(File.ReadAllText(Path.Combine(root,file)));
 string pictureFile=Path.Combine(root,$"content/pictures/lib/{def.Picture.Id}.json");
 var pic=BasePictureJson.Read(File.ReadAllText(pictureFile));
 var initial=LevelSession.Load(def,pic,new SessionOptions(0,20000));
 var exact=n<=10?ExactAudit.Calculate(initial):null;
 var policies=new List<object>();
 foreach(string policy in mode=="sequential"?new[]{"leftmost","rightmost","cycle","visible-largest"}:new[]{"random","leftmost","visible-frontier"})
 {
  int runs=policy=="random"||policy=="visible-frontier"?trials:1,wins=0,losses=0,stalled=0;long steps=0,productive=0,instantFinished=0,workDone=0;
  var peaks=new List<int>();
  for(int t=0;t<runs;t++)
  {
   var rng=new Random(unchecked(n*1000003+t*101+17));var state=initial.Clone();int peak=0,cursor=0;
   for(int step=0;state.Status==LevelStatus.Playing&&step<def.Pods.Count*3+10;step++)
   {
    var choices=Enumerable.Range(0,state.View.StackCount).Select(s=>state.View.Stack(s).FirstOrDefault())
      .Where(id=>id!=null&&state.Check(new TapPod(id)).IsAllowed).Select(id=>id!).ToList();
    if(choices.Count==0)break;
    string id;
    if(policy=="leftmost")id=choices[0];
    else if(policy=="rightmost")id=choices[^1];
    else if(policy=="cycle")
    {
     id=choices[0];
     for(int j=0;j<state.View.StackCount;j++)
     {
      int col=(cursor+j)%state.View.StackCount;var top=state.View.Stack(col).FirstOrDefault();
      if(top!=null&&choices.Contains(top)){id=top;cursor=(col+1)%state.View.StackCount;break;}
     }
    }
    else if(policy=="random")id=choices[rng.Next(choices.Count)];
    else if(policy=="visible-frontier"||policy=="visible-largest")
    {
     var frontier=state.View.ReachableTargets().Select(p=>state.View.Cell(p).Visible).Where(v=>v.HasValue).Select(v=>v!.Value).ToHashSet();
     var matches=choices.Where(p=>state.View.Pod(p).Variant is var v&&v.HasValue&&frontier.Contains(v.Value)).ToList();
     var pool=matches.Count>0?matches:choices;id=policy=="visible-largest"?pool.OrderByDescending(p=>state.View.Pod(p).Remaining).First():pool[rng.Next(pool.Count)];
    }
    else throw new InvalidOperationException("Unknown policy: "+policy);

    // Settled logical rules: no animation timing, boosters or rescue actions.
    int before=state.View.RemainingWork;
    state=state.SearchChild(new TapPod(id))!;
    int delta=before-state.View.RemainingWork;
    steps++;workDone+=delta;if(delta>0)productive++;
    if(state.View.Pod(id).Location==Bloomlings.Core.Tray.PodLocation.Done)instantFinished++;
    peak=Math.Max(peak,Enumerable.Range(0,state.View.SlotCapacity).Count(s=>state.View.PodInSlot(s)!=null));
   }
   if(state.Status==LevelStatus.Won)wins++;else if(state.Status==LevelStatus.Jammed||state.Status==LevelStatus.Stuck)losses++;else stalled++;
   peaks.Add(peak);
  }
  policies.Add(new{policy,runs,wins,losses,stalled,steps,productive,instantFinished,workDone,meanPeak=peaks.Average(),maxPeak=peaks.Max()});
 }
 string definitionHash=Bloomlings.Content.Validation.ValidationRecord.HashOf(def);
 string pictureHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(pictureFile))).ToLowerInvariant();
 var row=new{definitionHash,pictureHash,level=n,difficulty=def.Difficulty.Class.ToString(),score=def.Difficulty.Score,def.Picture.Id,pic.Width,pic.Height,pods=def.Pods.Count,work=initial.View.RemainingWork,variants=def.Pods.Select(p=>p.Variant.Key).Distinct().Count(),mechanics=def.Mechanics,exact,policies};
 writer.WriteLine(JsonSerializer.Serialize(row));
 if(n<=10||n%10==0)Console.WriteLine($"L{n} {def.Difficulty.Class}, {def.Pods.Count} pods, {watch.Elapsed.TotalSeconds:F1}s");
}
Console.WriteLine($"Done {first}–{last}, {trials} trials, {watch.Elapsed}");

static string FindRoot()
{
 foreach(string start in new[]{Directory.GetCurrentDirectory(),AppContext.BaseDirectory})
  for(string? p=start;p!=null;p=Path.GetDirectoryName(p))
   if(File.Exists(Path.Combine(p,"content/launch.props")))return p;
 throw new DirectoryNotFoundException("Run from the Bloomlings checkout.");
}
