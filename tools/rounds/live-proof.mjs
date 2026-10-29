// Sound certificates for a bridge on the live timeline. A failed patient search is
// not an impossibility proof: overlapping dispatches can admit other tap orders.
import {engine} from './engine.mjs';
import {solver} from '../level-stats/solve.mjs';

const COLORS='GPBACDVYOLMRKNSE';

// Optimistic reach before any pod of `gate` is admitted: the chosen bridge is
// already open, every non-gate material can disappear for free, and time costs
// nothing. Every cell the real game could process before the first gate pod is
// in this flood. A failure even in this relaxation is a live impossibility.
function beforeGate(level,choice,gate){
 const cells=[...level.rowsBottomFirst.join('')];cells[level.hero.choices[choice].cell]='.';
 const seen=new Set(),pending=[];
 for(const i of level.entries||[Math.floor(level.w/2)])if(cells[i]!=='#'&&cells[i]!==gate){seen.add(i);pending.push(i);}
 while(pending.length){const i=pending.pop(),x=i%level.w,y=Math.floor(i/level.w);
  for(const j of [y?i-level.w:-1,x?i-1:-1,x+1<level.w?i+1:-1,y+1<level.h?i+level.w:-1])
   if(j>=0&&!seen.has(j)&&cells[j]!=='#'&&cells[j]!==gate){seen.add(j);pending.push(j);}}
 const work={};for(const i of seen)if(COLORS.includes(cells[i]))work[cells[i]]=(work[cells[i]]||0)+1;
 return {cells,seen,work};
}

// Before a group is admitted, all earlier pods in each member's column must
// have been admitted. Include linked partners and their predecessors to a fixpoint.
function prerequisites(level,members,groups){
 const required=new Set(),target=new Set(members),pending=[...members];
 while(pending.length){const p=pending.pop();
  for(const column of level.tray){const at=column.indexOf(p);if(at<0)continue;
   for(const q of column.slice(0,at))if(!target.has(q)&&!required.has(q)){
    for(const g of groups[q])if(!target.has(g)&&!required.has(g)){required.add(g);pending.push(g);}
   }}}
 return [...required].sort((a,b)=>a-b);
}

// Keep links whose total quota cannot fit in the available work; for the rest,
// ignore shared release and give every color all its reachable work. Finishing
// its smallest pods first releases the most possible places. The remaining pod
// count is a lower bound even with arbitrary timing, target allocation, other
// launches, and partial dispatches.
function minimumHeld(level,pods,work,groups){
 // A link cannot release either participant if the whole group needs more
 // than the total available amount of even one color. Keep all those places;
 // give the other pods the entire work budget again (still optimistic).
 const blocked=new Set(),seen=new Set();
 for(const p of pods){const group=groups[p],key=group.join(',');if(seen.has(key))continue;seen.add(key);
  const cost={};for(const q of group)cost[level.pods[q].color]=(cost[level.pods[q].color]||0)+level.pods[q].count;
  if(Object.entries(cost).some(([c,n])=>n>(work[c]||0)))for(const q of group)blocked.add(q);
 }
 const byColor={};for(const p of pods)if(!blocked.has(p))(byColor[level.pods[p].color]??=[]).push(level.pods[p].count);
 let held=blocked.size;const detail={};
 for(const [color,counts] of Object.entries(byColor)){
  counts.sort((a,b)=>a-b);let left=work[color]||0,finished=0;
  for(const count of counts){if(count>left)break;left-=count;finished++;}
  detail[color]={pods:counts.length,available:work[color]||0,atMostFinished:finished};held+=counts.length-finished;
 }
 return {held,detail,blockedGroups:[...blocked].sort((a,b)=>a-b)};
}

export function gateCutCertificate(level,choice){
 if(!level.hero?.choices?.[choice])return null;
 const groups=level.pods.map((p,i)=>p.group?level.pods.flatMap((q,j)=>q.group===p.group?[j]:[]):[i]);
 const capacity=level.capacity||5;
 const unrestricted=beforeGate(level,choice,null);
 const disconnected=unrestricted.cells.flatMap((c,i)=>COLORS.includes(c)&&!unrestricted.seen.has(i)?[i]:[]);
 if(disconnected.length)return {method:'disconnected-work',choice,cells:disconnected};
 for(const gate of new Set(level.pods.map(p=>p.color))){
  const {work}=beforeGate(level,choice,gate),seen=new Set(),attempts=[];
  for(let p=0;p<level.pods.length;p++)if(level.pods[p].color===gate){
   const members=groups[p],key=members.join(',');if(seen.has(key))continue;seen.add(key);
   const prior=prerequisites(level,members,groups);
   // Such a group cannot be the first group containing the gate material.
   if(prior.some(q=>level.pods[q].color===gate))continue;
   const bound=minimumHeld(level,prior,work,groups);
   attempts.push({group:members,prerequisites:prior,heldAtLeast:bound.held,placesNeeded:members.length,blockedGroupPods:bound.blockedGroups,byColor:bound.detail});
  }
  if(attempts.length&&attempts.every(a=>a.heldAtLeast+a.placesNeeded>capacity))
   return {method:'gate-slot-cut',choice,gate,capacity,availableBeforeGate:work,attempts};
 }
 return null;
}

export async function bridgeLiveVerdict(level,choice,budget=200000){
 if(!Number.isInteger(choice)||!level.hero?.choices?.[choice])return {win:null,method:'invalid-choice'};
 const E=await engine(),errors=E.R.validate(level);
 if(errors.length)return {win:null,method:'invalid-level',errors};
 const certificate=gateCutCertificate(level,choice);
 if(certificate)return {win:false,method:certificate.method,certificate};
 const restricted={...level,hero:{...level.hero,choices:[level.hero.choices[choice]]}};
 const result=solver(E,budget).route(restricted,level.capacity||5);
 if(!result.route)return {win:null,method:result.win===null?'patient-budget-exhausted':'patient-search-only',nodes:result.nodes};
 const t=new E.T.Timeline(E.R.create(restricted,level.capacity||5));
 for(const action of result.route){t.advance(1e9);if(!t.commit(action).accepted)return {win:null,method:'witness-rejected',nodes:result.nodes};}
 t.advance(1e9);
 if(t.state.status!=='Won')return {win:null,method:'witness-did-not-win',nodes:result.nodes};
 return {win:true,method:'live-replayed-witness',nodes:result.nodes,
  route:result.route.map(a=>a.type==='bridge'?{...a,choice}:a)};
}
