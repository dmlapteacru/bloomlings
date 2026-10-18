using Bloomlings.Core.Simulation;
public sealed record ExactResult(double WinProbability,long WinOrders,long LoseOrders,int UniqueStates);
public static class ExactAudit
{
 public static ExactResult Calculate(LevelSession root)
 {
  var memo=new Dictionary<ulong,(double P,long W,long L)>();
  (double P,long W,long L) Walk(LevelSession s)
  {
   if(s.Status==LevelStatus.Won)return(1,1,0);
   if(s.Status!=LevelStatus.Playing)return(0,0,1);
   if(memo.TryGetValue(s.StateHash,out var known))return known;
   var ids=Enumerable.Range(0,s.View.StackCount).Select(i=>s.View.Stack(i).FirstOrDefault()).Where(id=>id!=null&&s.Check(new TapPod(id)).IsAllowed).ToArray();
   double p=0;long w=0,l=0;
   foreach(string? id in ids){var r=Walk(s.SearchChild(new TapPod(id!))!);p+=r.P;w+=r.W;l+=r.L;}
   var result=(ids.Length>0?p/ids.Length:0,w,l);memo[s.StateHash]=result;return result;
  }
  var r=Walk(root);return new(r.P,r.W,r.L,memo.Count);
 }
}
