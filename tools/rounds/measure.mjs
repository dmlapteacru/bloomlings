// Checks of a candidate level on the round engine (product/17_LEVEL_GENERATION_RULES.md §1, §5, §8, §10, §11):
// the saved solution (search, then replay on the live timeline), the extended random test with waiting and the
// Wilson interval, the simple strategies, the fairness of hidden pods, the rhythm by fifths of done work.
import {engine} from './engine.mjs';
import {solver} from '../level-stats/solve.mjs';
import {run} from '../level-stats/trace.mjs';
import {STRATEGIES,rng} from '../level-stats/strategies.mjs';

const isStateEvent=e=>e.type!=='return'&&e.type!=='arrive';
// one game of the extended random policy (§5): before each choice the events of the moment are processed; the bot
// picks uniformly among all legal actions (a pod, a whole group, each bridge place) and, when a state-changing event
// is scheduled, one extra option: wait until that event. With no action and a scheduled event, time moves on by itself.
export function randomGame(E,level,seed,keep=null){const {R,T}=E,t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed);let guard=0;
 const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)t.advance(0);};
 const waitEvent=()=>{while(t.pending){const e=t.steps[0];t.advance(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
 while(guard++<200000){settleNow();if(t.state.status==='Won')return true;
  const moves=R.legal(t.state).filter(a=>!t.check(a)&&(!keep||keep(a))),canWait=t.steps.some(isStateEvent);
  if(!moves.length){if(t.pending){waitEvent();continue;}return false;}
  const k=Math.floor(r()*(moves.length+(canWait?1:0)));
  if(k===moves.length){waitEvent();continue;}
  t.commit(moves[k]);}
 return false;}
export function wilson(w,n,z=1.96){if(!n)return [0,1];const p=w/n,d=1+z*z/n,c=p+z*z/(2*n),h=z*Math.sqrt(p*(1-p)/n+z*z/(4*n*n));return [Math.max(0,(c-h)/d),Math.min(1,(c+h)/d)];}
export const classOfShare=p=>p>=.5?'Normal':p>=.2?'Hard':p>=.05?'Super Hard':'too hard';
// 100 games; widened to 400 in total when the 95 % Wilson interval crosses a class border (§5)
export async function randomTest(level,{seed=1,first=100,max=400}={}){const E=await engine();let wins=0,n=0;
 const play=k=>{for(let i=0;i<k;i++){if(randomGame(E,level,seed*7919+n*104729+1))wins++;n++;}};
 play(first);let ci=wilson(wins,n);const crosses=([a,b])=>[.05,.2,.5].some(x=>a<x&&b>=x);
 if(crosses(ci)&&max>first){play(max-first);ci=wilson(wins,n);}
 return {runs:n,wins,share:Math.round(wins/n*1000)/1000,wilson95:ci.map(x=>Math.round(x*1000)/1000),cls:classOfShare(wins/n),borderline:crosses(ci),protocol:'§5 extended: uniform over legal actions + wait before a scheduled event, ×1',seedBase:seed};}
// a quick estimate for screening candidates before the full test
export async function quickShare(level,games=40,seed=11){const E=await engine();let w=0;for(let i=0;i<games;i++)if(randomGame(E,level,seed*31+i*977))w++;return w/games;}

// the saved winning solution: full-information search on the patient model, replayed on the live timeline (§1, §13.7)
export async function solve(level,budget=200000){const E=await engine(),S=solver(E,budget),cap=level.capacity||5;
 const r=S.route(level,cap);if(!r.route)return {win:r.win,route:null,nodes:r.nodes};
 const tr=run(E,level,{commands:r.route.map(a=>({t:0,action:a})),patient:true});
 const dec=S.decisions(level,cap,r.route);
 // the same actions tapped the moment each is accepted (no waiting for work to settle): a lower bound of the duration;
 // it may lose, which is allowed (§6: the solution may need a reserve of places or a pause)
 const fast=run(E,level,{commands:r.route.map(a=>({t:0,action:a}))});
 return {win:true,route:r.route,nodes:r.nodes,replay:tr.status,trace:tr,fast:{status:fast.status,seconds:Math.round(fast.end/100)/10},decisions:{forks:dec.risky,single:dec.tight,unknown:dec.unknown}};}

// fairness of hidden pods (§8): in every world where the hidden pods carry each other's material and count, following
// the saved solution until the first differing pod shows must still leave a winning continuation
export async function fairness(level,route,limit=24){const E=await engine(),{idle:R}=E,S=solver(E,60000),hid=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
 if(hid.length<2||!route)return {checked:0,fair:true,note:hid.length<2?'одна скрытая — нечего переставлять':'нет решения'};
 const vals=hid.map(i=>[level.pods[i].color,level.pods[i].count]),worlds=[];
 const perm=(a,k=0)=>{if(worlds.length>=limit)return;if(k===a.length){if(a.some((v,i)=>v!==i))worlds.push(a.slice());return;}for(let i=k;i<a.length;i++){[a[k],a[i]]=[a[i],a[k]];perm(a,k+1);[a[k],a[i]]=[a[i],a[k]];}};
 perm(hid.map((_,i)=>i));let bad=0,unknown=0;
 for(const w of worlds){const pods=level.pods.map(p=>({...p}));w.forEach((src,j)=>{pods[hid[j]].color=vals[src][0];pods[hid[j]].count=vals[src][1];});
  const lv={...level,pods},diff=hid.filter((_,j)=>w[j]!==j&&(vals[w[j]][0]!==vals[j][0]||vals[w[j]][1]!==vals[j][1]));if(!diff.length)continue;
  // the world must keep the materials' balance; a swap between different colours changes it, so such worlds are not consistent
  const bal=c=>pods.filter(p=>p.color===c).reduce((n,p)=>n+p.count,0),orig=c=>level.pods.filter(p=>p.color===c).reduce((n,p)=>n+p.count,0);
  if([...new Set(vals.map(v=>v[0]))].some(c=>bal(c)!==orig(c)))continue;
  let s=R.create(lv,lv.capacity||5),k=0;
  while(k<route.length&&!diff.some(p=>s.revealed[p])){const st=R.step(s,route[k]);if(!st.accepted){s=null;break;}s=st.state;k++;}
  if(!s){bad++;continue;}const ok=S.winnable(s);if(ok===false)bad++;else if(ok===null)unknown++;}
 return {checked:worlds.length,unfair:bad,unknown,fair:bad===0&&unknown===0,note:'перестановки материала и числа между скрытыми подами с тем же балансом'};}

// the simple strategies of §11, each with the leftmost and the rightmost hero choice, tapping as soon as possible
export async function strategies(level){const E=await engine(),{R,T}=E,out={};
 for(const [k,st] of Object.entries(STRATEGIES)){if(st.random||k==='planner'||k==='big-first')continue;
  for(const side of ['left','right']){const pol=st.make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));let g=0;
   while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));
    if(m.length){const br=m.filter(a=>a.type==='bridge');const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
    if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
   out[`${k}|${side}`]=t.state.status==='Won';}}
 return out;}

// the rhythm on the saved solution (§10): per fifth of done work W, mean and minimum free places (time-weighted),
// share of time with a full buffer, most pods waiting without work; launches left after 80 % W and the last launch's W
export function rhythm(tr,W){const S=tr.samples,end=tr.end,seg=[0,1,2,3,4].map(()=>({t:0,free:0,min:Infinity,full:0,stalled:0}));
 for(let i=0;i<S.length;i++){const [t,free,,stalled,,done]=S[i],dt=(i+1<S.length?S[i+1][0]:end)-t,k=Math.min(4,Math.floor(5*done/W));const s=seg[k];
  s.t+=dt;s.free+=free*dt;s.min=Math.min(s.min,free);if(free===0)s.full+=dt;s.stalled=Math.max(s.stalled,stalled);}
 const doneAt=t=>{let d=0;for(const s of S){if(s[0]<=t)d=s[5];else break;}return d;};
 const after80=tr.taps.filter(x=>doneAt(x.t)>=.8*W).length,lastShare=tr.taps.length?Math.round(100*doneAt(tr.taps.at(-1).t)/W):null;
 return {segments:seg.map((s,i)=>({from:i*20,to:i*20+20,meanFree:s.t?Math.round(s.free/s.t*100)/100:null,minFree:s.min===Infinity?null:s.min,fullShare:s.t?Math.round(s.full/s.t*100)/100:null,maxWaiting:s.stalled})),launchesAfter80:after80,lastLaunchAtW:lastShare,seconds:Math.round(end/100)/10};}

// §9: on a bridge introduction every shown bridge keeps a winning continuation: the level is solved with only that bridge
// (a bridge needs its approach cleared first, §8, so it cannot simply be taken at the start)
export async function bridgeChoicesWin(level){const E=await engine(),S=solver(E,200000),out=[];
 for(let i=0;i<(level.hero?level.hero.choices.length:0);i++){const lv={...level,hero:{...level.hero,choices:[level.hero.choices[i]]}};out.push(S.winnable(E.idle.create(lv,level.capacity||5)));}
 return {each:out,ok:out.length>0&&out.every(x=>x===true)};}

// the bridge as a choice (the owner, 2026-10-27: "надо выбрать, какой мост открывать"): for each bridge, whether it keeps a
// winning continuation and the random test's share when only that bridge may be built; gap = best share − worst share
export async function bridgeChoices(level,games=60,seed=5){const E=await engine(),win=(await bridgeChoicesWin(level)).each,each=[];
 for(let i=0;i<win.length;i++){let w=0;for(let g=0;g<games;g++)if(randomGame(E,level,seed*7919+g*104729+i,a=>a.type!=='bridge'||a.choice===i))w++;each.push({label:level.hero.choices[i].label,win:win[i],share:Math.round(w/games*100)/100});}
 const sh=each.map(x=>x.share);return {each,gap:Math.round((Math.max(...sh)-Math.min(...sh))*100)/100,better:sh.indexOf(Math.max(...sh))};}
