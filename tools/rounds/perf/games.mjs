// The single games of the measures of measure.mjs (tools/rounds/perf/README.md), moved here verbatim so that the game
// workers (worker.mjs) play exactly what measure.mjs plays in its own thread. Each game depends only on the engine, the
// level, its parameters and its own index (its seed), never on the games before it, so the games of one test may be
// played in any order and on any thread: the test assembles them by index.
import {STRATEGIES,rng} from '../../level-stats/strategies.mjs';

export const isStateEvent=e=>e.type!=='return'&&e.type!=='arrive';
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
// the keep filter of a test restricted to one bridge (null: every action)
export const keepBridge=bridge=>bridge===null||bridge===undefined?null:a=>a.type!=='bridge'||a.choice===bridge;
// the keep filter of a test restricted to one exit of a water channel (measure.mjs channelExits)
export const keepExit=exit=>a=>a.type!=='channel'||a.exit===exit;
// a hero's choice the strategies take at once by side: a bridge place or a water channel's exit (left the first offered,
// right the last)
export const heroChoices=m=>m.filter(a=>a.type==='bridge'||a.type==='channel');

// a calm game (calmTest): before every choice the board plays until nothing more changes (the idle engine)
export function calmGame(E,level,seed,bridge){const {R,idle:I}=E,r=rng(seed);let s=R.create(level,level.capacity||5),g=0;
 while(s.status==='Playing'&&g++<5000){const mv=R.legal(s).filter(a=>bridge===null||a.type!=='bridge'||a.choice===bridge);if(!mv.length)break;const x=I.step(s,mv[Math.floor(r()*mv.length)]);if(!x.accepted)break;s=x.state;}
 return s.status==='Won';}

// bridgeSingle's game g: the bridge choices buildable at the first moment any bridge is (policy 'random' or 'tap')
export function singleGame(E,level,seed,g,policy){const {R,T}=E,t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed*977+g),seen=[];let k=0;
 if(policy==='tap'){while(k++<100000&&t.state.status!=='Won'){const m=R.legal(t.state).filter(a=>!t.check(a));const br=m.filter(a=>a.type==='bridge');br.forEach(a=>seen.push(a.choice));
   if(br.length||t.state.bridge>=0)break;if(m.length){t.commit(m[Math.floor(r()*m.length)]);continue;}if(!t.pending)break;t.advance(t.steps[0].at-t.now);}return seen;}
 const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)t.advance(0);};
 const waitEvent=()=>{while(t.pending){const e=t.steps[0];t.advance(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
 while(k++<200000){settleNow();if(t.state.status==='Won')break;
  const moves=R.legal(t.state).filter(a=>!t.check(a)),br=moves.filter(a=>a.type==='bridge');br.forEach(a=>seen.push(a.choice));
  if(br.length||t.state.bridge>=0)break;
  const canWait=t.steps.some(isStateEvent);if(!moves.length){if(t.pending){waitEvent();continue;}break;}
  const j=Math.floor(r()*(moves.length+(canWait?1:0)));if(j===moves.length){waitEvent();continue;}t.commit(moves[j]);}
 return seen;}

// bridgeLoss's game g: [built, free (another move was open at the choice), launches after the choice, won with it]
export function lossGame(E,level,seed,g,choice,restricted){const {R,T}=E,t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed*7919+g*104729+1);let k=0,built=false,free=false,n=0,won=false;
 const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)t.advance(0);};
 const waitEvent=()=>{while(t.pending){const e=t.steps[0];t.advance(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
 while(k++<200000){settleNow();if(t.state.status==='Won'){if(built)won=true;break;}
  const moves=R.legal(t.state).filter(a=>!t.check(a)&&(!restricted||a.type!=='bridge'||a.choice===choice)),canWait=t.steps.some(isStateEvent);
  if(!moves.length){if(t.pending){waitEvent();continue;}break;}
  const j=Math.floor(r()*(moves.length+(canWait?1:0)));if(j===moves.length){waitEvent();continue;}
  const a=moves[j];if(a.type==='bridge'){if(a.choice===choice){built=true;free=moves.length>1;}}else if(built)n++;t.commit(a);}
 return [built,free,n,won];}

// the opening test's pieces: the moves of the first moment a move is possible (asap), and a greedy strategy played after an
// opening (a list of JSON actions, each taken as soon as one is possible)
export const asap=(E,t)=>{let g=0;while(g++<100000){if(t.state.status==='Won')return [];const m=E.R.legal(t.state).filter(a=>!t.check(a));if(m.length)return m;if(!t.pending)return [];t.advance(t.steps[0].at-t.now);}return [];};
export function greedyFrom(E,level,opening,k,side){const {R,T}=E,pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));
 for(const a of opening){const m=asap(E,t),x=m.find(y=>JSON.stringify(y)===a);if(!x)return false;t.commit(x);}
 let g=0;while(g++<100000){if(t.state.status==='Won')return true;const m=R.legal(t.state).filter(a=>!t.check(a));
  if(m.length){const br=heroChoices(m);const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
  if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
 return t.state.status==='Won';}
// whether some strategy at some hero side wins after the opening
export const openingWins=(E,level,seq,strats,sides)=>strats.some(k=>sides.some(s=>greedyFrom(E,level,seq,k,s)));

// game i of a test kind, the unit a worker plays (pool.mjs): the seeds are the measures' own
export function playOne(E,level,kind,p,i){
 switch(kind){
  case 'random':return randomGame(E,level,p.seed*7919+i*104729+1,keepBridge(p.bridge));
  case 'quick':return randomGame(E,level,p.seed*31+i*977,keepBridge(p.bridge));
  case 'choice':return randomGame(E,level,p.seed*7919+i*104729+p.choice,keepBridge(p.choice));
  case 'exit':return randomGame(E,level,p.seed*7919+i*104729+p.exit+3,keepExit(p.exit));
  case 'calm':return calmGame(E,level,p.seed*7919+i*104729+5,p.bridge);
  case 'single':return singleGame(E,level,p.seed,i,p.policy);
  case 'loss':return lossGame(E,level,p.seed,i,p.choice,p.restricted);
  case 'opening':return openingWins(E,level,p.openings[i],p.strats,p.sides);
  default:throw Error('unknown game kind '+kind);}}
