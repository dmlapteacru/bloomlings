using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;
using Bloomlings.Solver;

namespace Bloomlings.Generator
{
    /// <summary>
    /// Coarse authored silhouettes need a deliberate Source puzzle: reserve reachable colors as a route and put a
    /// certified premature sequence in another column. Counts, art and gameplay rules never change. Every layout is
    /// solved and its actual winning pressure/score checked, including when a later connection is added by the caller.
    /// </summary>
    public static class AuthoredTrayBuilder
    {
        /// <summary>Static authoring prerequisite: enough hidden-color work to construct a five-slot mistake sequence.
        /// Exact wave, lock, pod-count, win and loss checks still run when generating each actual puzzle.</summary>
        public static int InitialBlockedPodCapacity(BasePicture picture)
        {
            var pending=new Queue<(int X,int Y)>();var visited=new HashSet<(int,int)>();var available=new HashSet<int>();
            // Canonical picture rows are bottom first; the authoring text atlas uses top-first rows.
            var start=(picture.Width/2,0);pending.Enqueue(start);visited.Add(start);
            while(pending.Count>0)
            {
                var (x,y)=pending.Dequeue();
                foreach(var (xx,yy) in new[]{(x-1,y),(x+1,y),(x,y-1),(x,y+1)})
                {
                    if(xx<0||xx>=picture.Width||yy<0||yy>=picture.Height)continue;
                    int value=picture.Grid[yy][xx];
                    if(value>=0)available.Add(value);
                    else if(value==BasePicture.Empty&&visited.Add((xx,yy)))pending.Enqueue((xx,yy));
                }
            }
            var counts=picture.Grid.SelectMany(r=>r).Where(c=>c>=0).GroupBy(c=>c);
            return counts.Where(g=>!available.Contains(g.Key)).Sum(g=>g.Count()/BandGuidelines.MinPodSize);
        }

        public static TrayOutcome? Tune(LevelDefinition skeleton,BasePicture picture,IReadOnlyList<PlannedPod> plan,int stacks,
            DifficultyClass target,int maxInjections,DifficultyThresholds thresholds,SolveOptions options,
            ref Xoshiro256StarStar rng,out string? rejection,IntRange? peakSlots,IntRange podCount,IntRange podSize)
        {
            var solver=new Solver.Solver();
            LevelDefinition original=TrayBuilder.Layout(skeleton,plan,stacks);
            var initial=LevelSession.Load(original,picture,new SessionOptions(0,20000));
            int capacity=Enumerable.Range(0,initial.View.SlotCapacity).Count(s=>initial.View.SlotStateOf(s)==Bloomlings.Core.Slots.SlotState.Free);
            HashSet<VariantId> Reachable(LevelSession session)=>session.View.ReachableTargets().Select(p=>session.View.Cell(p).Visible)
                .Where(v=>v.HasValue).Select(v=>v!.Value).ToHashSet();
            // A large single-color interior often gets only two or three pods from the random partitioner. Split
            // those pods within the band's actual size/count limits so premature taps can fill the buffer. No work,
            // color or game rule changes, and locks are never split or moved between waves.
            var partition=plan.ToList();
            var firstReachable=Reachable(initial);
            int BlockedCount()=>partition.Count(p=>!firstReachable.Contains(p.Variant)&&p.LockKeyId==null);
            while(BlockedCount()<capacity&&partition.Count<podCount.Max)
            {
                int at=Enumerable.Range(0,partition.Count)
                    .Where(i=>!firstReachable.Contains(partition[i].Variant)&&partition[i].LockKeyId==null&&partition[i].Count>=2*podSize.Min)
                    .OrderByDescending(i=>partition[i].Count).ThenBy(i=>i).DefaultIfEmpty(-1).First();
                if(at<0)break;
                var part=partition[at];int left=part.Count/2;
                partition[at]=part with{Count=left};partition.Insert(at+1,part with{Count=part.Count-left});
            }
            if(partition.Count!=plan.Count)
            {
                plan=partition;original=TrayBuilder.Layout(skeleton,plan,stacks);
                initial=LevelSession.Load(original,picture,new SessionOptions(0,20000));
            }
            int prefix=target==DifficultyClass.Normal?0:Math.Min(capacity-2,
                Math.Max(1,Math.Min(target==DifficultyClass.Hard?2:3,(peakSlots?.Max??4)-1)));
            string last="no-seed";
            for(int warmup=0;warmup<=3;warmup++)
            {
                var view=initial;
                var warm=new List<int>();
                if(warmup>0)
                {
                    var reachable=Reachable(initial);
                    warm=Enumerable.Range(0,plan.Count).Where(i=>reachable.Contains(plan[i].Variant)&&plan[i].LockKeyId==null)
                        .OrderBy(i=>plan[i].Count).ThenBy(i=>i).Take(warmup).ToList();
                    if(warm.Count!=warmup)continue;
                    var warmOrder=warm.Concat(Enumerable.Range(0,plan.Count).Where(i=>!warm.Contains(i))).Select(i=>plan[i]).ToArray();
                    view=LevelSession.Load(TrayBuilder.Layout(skeleton,warmOrder,stacks),picture,new SessionOptions(0,20000));
                    bool valid=true;
                    for(int i=0;i<warmup;i++)if(!view.Apply(new TapPod("p"+(i+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture))).Accepted){valid=false;break;}
                    if(!valid||view.Status!=LevelStatus.Playing)continue;
                }
                var available=Reachable(view);
                var blocked=Enumerable.Range(0,plan.Count).Where(i=>!warm.Contains(i)&&!available.Contains(plan[i].Variant)&&plan[i].LockKeyId==null).ToList();
                if(blocked.Count<capacity){last=$"blocked-{blocked.Count}-need-{capacity}-reachable-{available.Count}";continue;}
                var forced=blocked.Take(prefix).ToArray();
                var trap=blocked.Skip(prefix).Take(capacity-prefix).ToArray();
                var chosen=warm.Concat(forced).Concat(trap).ToHashSet();
                var route=Enumerable.Range(0,plan.Count).Where(i=>!chosen.Contains(i)&&available.Contains(plan[i].Variant)).ToArray();
                if(route.Length==0||route.Any(i=>plan[i].LockKeyId!=null)){last="route-unavailable";continue;}
                foreach(int i in route)chosen.Add(i);
                var columns=Enumerable.Range(0,stacks).Select(_=>new List<int>()).ToArray();
                columns[0].AddRange(forced);columns[1].AddRange(warm);columns[1].AddRange(trap);
                int[] safe=target==DifficultyClass.Normal?Enumerable.Range(0,stacks).Where(s=>s!=1).ToArray():new[]{0};
                for(int i=0;i<route.Length;i++)columns[safe[i%safe.Length]].Add(route[i]);
                // Fill unused Source columns before adding depth to the seeded route/trap. Coarse art can
                // consume nearly every pod in those two groups, leaving only a few unreserved pods.
                int cursor=0;
                foreach(int i in Enumerable.Range(0,plan.Count).Where(i=>!chosen.Contains(i)))
                {
                    int empty=Array.FindIndex(columns,c=>c.Count==0);
                    columns[empty>=0?empty:cursor++%stacks].Add(i);
                }
                // A reserved trap may also supply a column without exposing the safe route. Taking its tail
                // preserves the intended mistake order: each moved pod is still accessible when its turn comes.
                foreach(int empty in Enumerable.Range(0,stacks).Where(s=>columns[s].Count==0).ToArray())
                {
                    int from=Enumerable.Range(1,stacks-1).Where(s=>columns[s].Count>1)
                        .OrderByDescending(s=>columns[s].Count).DefaultIfEmpty(-1).First();
                    if(from<0)break;
                    int tail=columns[from].Count-1;
                    columns[empty].Add(columns[from][tail]);columns[from].RemoveAt(tail);
                }
                if(columns.Any(c=>c.Count==0)){last="empty-column";continue;}
                var trays=columns.Select(c=>(IReadOnlyList<string>)c.Select(i=>original.Pods[i].Id).ToArray()).ToArray();
                LevelDefinition definition=original with{Tray=new TrayDef(trays)};
                // Check the intended mistake sequence on the actual ids/layout, before doing any searches.
                var mistake=LevelSession.Load(definition,picture,new SessionOptions(0,20000));
                bool jam=true;
                foreach(int i in warm.Concat(forced).Concat(trap))if(!mistake.Apply(new TapPod(original.Pods[i].Id)).Accepted){jam=false;break;}
                if(!jam||mistake.Status!=LevelStatus.Jammed){last="trap-"+mistake.Status;continue;}
                var session=LevelSession.Load(definition,picture,new SessionOptions(0,20000));
                SolveResult win=solver.Solve(session,options);
                if(win.Status!=SolveStatus.Solvable){last="seed-win-"+win.Status;continue;}
                LevelAnalysis analysis=solver.Analyze(session,options,win);
                int score=DifficultyScorer.Score(analysis.Metrics,thresholds),injections=0;
                for(int attempt=0;attempt<=Math.Min(12,maxInjections*2);attempt++)
                {
                    DifficultyClass current=DifficultyScorer.Classify(score,thresholds);
                    if(current==target&&analysis.Jam.Status==SolveStatus.Solvable&&(peakSlots==null||peakSlots.Contains(analysis.Metrics.PeakBuffer)))
                    {rejection=null;return new TrayOutcome(definition,analysis,score,current,injections);}
                    last=$"seed-class-{current}-score-{score}-peak-{analysis.Metrics.PeakBuffer}";
                    if((int)current>(int)target||injections>=maxInjections)break;
                    // Preserve the seeded columns while moving one later pod ahead; unlike flat round-robin rebuilding,
                    // this retains a known loss path and avoids exhaustive no-jam searches for every attempted change.
                    var next=definition.Tray.Stacks.Select(c=>c.ToList()).ToArray();
                    var candidates=Enumerable.Range(0,stacks).Where(s=>next[s].Count>1).ToArray();
                    int from=candidates[rng.NextInt(candidates.Length)],to=rng.NextInt(stacks);
                    int at=rng.NextInt(next[from].Count),put=rng.NextInt(Math.Max(1,(next[to].Count+1)/2));
                    string id=next[from][at];next[from].RemoveAt(at);put=Math.Min(put,next[to].Count);next[to].Insert(put,id);
                    var candidate=definition with{Tray=new TrayDef(next.Select(c=>(IReadOnlyList<string>)c.ToArray()).ToArray())};
                    var test=LevelSession.Load(candidate,picture,new SessionOptions(0,20000));
                    var solved=solver.Solve(test,options);if(solved.Status!=SolveStatus.Solvable)continue;
                    var checkedAnalysis=solver.Analyze(test,options,solved);
                    if(checkedAnalysis.Jam.Status!=SolveStatus.Solvable)continue;
                    int checkedScore=DifficultyScorer.Score(checkedAnalysis.Metrics,thresholds);
                    var checkedClass=DifficultyScorer.Classify(checkedScore,thresholds);
                    if((int)checkedClass>(int)target)continue;
                    int Distance(int peak)=>peakSlots==null?0:Math.Max(0,peakSlots.Min-peak)+Math.Max(0,peak-peakSlots.Max);
                    if(checkedScore>score||(checkedClass==target&&Distance(checkedAnalysis.Metrics.PeakBuffer)<Distance(analysis.Metrics.PeakBuffer)))
                    {definition=candidate;analysis=checkedAnalysis;score=checkedScore;injections++;}
                }
            }
            rejection="authored-tray:"+last;
            return null;
        }
    }
}
