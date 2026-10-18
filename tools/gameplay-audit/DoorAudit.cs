using System.Text.Json;
using Bloomlings.Content.Json;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
public static class DoorAudit
{
 public static void Run(string root,int first,int last)
 {
  using var writer=new StreamWriter(Path.Combine(root,$"content/work/gameplay-audit/doors-{first}-{last}.jsonl")){AutoFlush=true};
  int count=0,unchanged=0;
  for(int n=first;n<=last;n++)
  {
   var file=Path.Combine(root,n<=10?$"content/curated/level-{n:0000}.json":$"content/catalog/levels/level-{n:00000}.json");
   var def=DefinitionJson.Read(File.ReadAllText(file));var gates=def.Specials.Where(s=>s.Type==SpecialType.Gate&&s.Effect.Kind==SpecialEffectKind.OpenCells&&s.Effect.Cells.Count==0).ToArray();if(gates.Length==0)continue;
   var pic=BasePictureJson.Read(File.ReadAllText(Path.Combine(root,$"content/pictures/lib/{def.Picture.Id}.json")));
   string definitionHash=Bloomlings.Content.Validation.ValidationRecord.HashOf(def);
   string validation=Path.Combine(root,$"content/catalog/validation/level-{n:00000}.json");List<string> trace=new();
   if(File.Exists(validation))using(var doc=JsonDocument.Parse(File.ReadAllText(validation)))
   {
    if(doc.RootElement.GetProperty("definitionHash").GetString()!=definitionHash)throw new InvalidDataException("Stale validation at L"+n);
    trace=doc.RootElement.GetProperty("solutionTrace").EnumerateArray().Select(a=>a.GetString()!).ToList();
   }
   else
   {
    var s=LevelSession.Load(def,pic,new SessionOptions(0,20000));
    while(s.Status==LevelStatus.Playing)
    {
     var id=Enumerable.Range(0,s.View.StackCount).Select(i=>s.View.Stack(i).FirstOrDefault()).FirstOrDefault(id=>id!=null&&s.Check(new TapPod(id)).IsAllowed);
     if(id==null)break;trace.Add("tap:"+id);s=s.SearchChild(new TapPod(id))!;
    }
   }
   foreach(var gate in gates)
   {
    var gateCells=gate.Cells.ToHashSet();var keys=def.Locks.Where(l=>l.TargetKind==LockTargetKind.Special&&l.TargetId==gate.Id).Select(l=>l.KeyId).ToHashSet();
    var overlays=def.Overlays.Where(o=>!gateCells.Contains(o.Cell)).Select(o=>keys.Contains(o.KeyId??"")?o with{KeyId=null}:o).ToList();
    overlays.AddRange(gateCells.Select(c=>new CellOverlay(c,Array.Empty<Bloomlings.Core.Variants.VariantId>(),false,true,false,null)));
    // Counterfactual geometry test: keep the closed footprint as permanent stone.
    // Remove only this gate's administrative trigger goal and its own key/lock pair.
    // Target work, charges, tray, other mechanics and input files stay unchanged.
    var frozen=def with{Specials=def.Specials.Where(s=>s.Id!=gate.Id).ToArray(),Locks=def.Locks.Where(l=>!(l.TargetKind==LockTargetKind.Special&&l.TargetId==gate.Id)).ToArray(),Overlays=overlays};
    string? error=null;bool originalWon=false,won=false,stillClosed=false,accountingSame=false;int rejected=0,remaining=-1;
    try
    {
     var original=LevelSession.Load(def,pic,new SessionOptions(0,20000));var s=LevelSession.Load(frozen,pic,new SessionOptions(0,20000));accountingSame=s.View.RemainingWork==original.View.RemainingWork;
     var baseline=original;
     foreach(var command in trace)
     {
      if(baseline.Status!=LevelStatus.Playing)break;
      if(!command.StartsWith("tap:",StringComparison.Ordinal))break;
      var next=baseline.SearchChild(new TapPod(command[4..]));if(next==null)break;baseline=next;
     }
     originalWon=baseline.Status==LevelStatus.Won;
     foreach(var command in trace)
     {
      if(s.Status!=LevelStatus.Playing)break;
      if(!command.StartsWith("tap:",StringComparison.Ordinal)){error="non-tap trace";break;}
      var next=s.SearchChild(new TapPod(command[4..]));if(next==null){rejected++;break;}s=next;
     }
     won=s.Status==LevelStatus.Won;remaining=s.View.RemainingWork;stillClosed=gateCells.All(c=>s.View.Cell(c).Kind==CellKind.Stone);
    }
    catch(Exception ex){error=ex.GetType().Name+": "+ex.Message;}
    bool provedUnneeded=originalWon&&won&&stillClosed&&accountingSame&&error==null;if(provedUnneeded)unchanged++;count++;
    writer.WriteLine(JsonSerializer.Serialize(new{definitionHash,level=n,gate=gate.Id,keyDoor=gate.Condition.Kind==SpecialConditionKind.Key,cells=gateCells.Count,originalWon,won,stillClosed,accountingSame,rejected,remaining,error,provedUnneeded}));
   }
   if(n%100==0)Console.WriteLine($"Door audit L{n}: {unchanged}/{count} unchanged");
  }
  Console.WriteLine($"Door audit done: {unchanged}/{count} same solution while door permanently closed");
 }
}
