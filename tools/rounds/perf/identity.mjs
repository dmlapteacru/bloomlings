// The identity check of the fast twin (verify.mjs, test/perf.test.mjs; tools/rounds/perf/README.md): the reference engine
// (tools/rounds/engine/) and the fast twin (fast-rules.cjs, fast-timeline.cjs, fast-idle.cjs) play a game each and every
// event, state and step list goes into one hash per game; the two hashes must be equal.
import crypto from 'node:crypto';import {createRequire} from 'node:module';
import {rng} from '../../level-stats/strategies.mjs';
const req=createRequire(import.meta.url);
export const REF={R:req('../engine/rules.cjs'),T:req('../engine/timeline.cjs'),idle:req('../engine/idle-rules.cjs')};
export const FAST={R:req('./fast-rules.cjs'),T:req('./fast-timeline.cjs'),idle:req('./fast-idle.cjs')};
const isStateEvent=e=>e.type!=='return'&&e.type!=='arrive';

// everything a game shows after each move, into one hash
export class Log{constructor(){this.h=crypto.createHash('sha256');this.n=0;}
 add(x){this.h.update(typeof x==='string'?x:JSON.stringify(x));this.h.update('\n');this.n++;}
 // after every advance: the events, the clears and the signature; after every commit (full) also the step list, every
 // courier's route, the reservations and the veiled cells and latches
 timeline(E,t,full=false){this.add(t.lastEvents);this.add(t.lastClears);this.add(E.R.signature(t.state)+'|'+t.now+'|'+t.status+'|'+t.processedCells);
  if(full){this.add(t.steps);this.add([...t.jobs.values()].map(j=>[j.id,j.route]));this.add(Array.from(t.reserved).join(',')+'|'+Array.from(t.inFlight).join(',')+'|'+Array.from(t.departReady).join(','));
   this.add([t.state.veiled?Array.from(t.state.veiled):null,t.state.latched?Array.from(t.state.latched):null,
    // the shutters and the channel (17 §8, 2026-11-05), only on a level with them
    ...(t.state.shut?[Array.from(t.state.shut)]:[]),...(t.state.meta.channel?[t.state.channel,t.state.springReached]:[])]);}}
 digest(){return this.h.digest('hex');}}

// the saved route on the timeline: patient (all work settled before each command) or tapped as soon as accepted
export function replay(E,level,route,patient){const {R,T}=E,t=new T.Timeline(R.create(level,level.capacity||5)),log=new Log();log.timeline(E,t,true);
 const step=()=>{t.advance(Math.max(0,t.steps[0].at-t.now));log.timeline(E,t);};
 for(const a of route){if(patient)while(t.pending)step();let g=0;while(t.check(a)&&t.pending&&g++<100000)step();
  const r=t.commit(a);log.add([a,r.accepted,r.reason||null,r.events]);log.timeline(E,t,true);log.add(t.snapshot());log.add(t.couriers);}
 while(t.pending)step();return {hash:log.digest(),status:t.state.status,end:t.now,lines:log.n};}

// measure.mjs randomGame with every move logged
export function randomGame(E,level,seed,keep){const {R,T}=E,t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed),log=new Log();let guard=0;
 const adv=ms=>{t.advance(ms);log.timeline(E,t);};
 const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)adv(0);};
 const waitEvent=()=>{while(t.pending){const e=t.steps[0];adv(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
 while(guard++<200000){settleNow();if(t.state.status==='Won')return {won:true,hash:log.digest()};
  const moves=R.legal(t.state).filter(a=>!t.check(a)&&(!keep||keep(a))),canWait=t.steps.some(isStateEvent);log.add(moves);
  if(!moves.length){if(t.pending){waitEvent();continue;}return {won:false,hash:log.digest()};}
  const k=Math.floor(r()*(moves.length+(canWait?1:0)));
  if(k===moves.length){waitEvent();continue;}
  const res=t.commit(moves[k]);log.add(res.events);log.timeline(E,t,true);}
 return {won:false,hash:log.digest()};}

// measure.mjs calmTest's game with the signature after every step
export function calmGame(E,level,seed,bridge,exit=null){const {R,idle:I}=E,r=rng(seed),log=new Log();let s=R.create(level,level.capacity||5),g=0;log.add(R.signature(s));
 while(s.status==='Playing'&&g++<5000){const mv=R.legal(s).filter(a=>(bridge===null||a.type!=='bridge'||a.choice===bridge)&&(exit===null||a.type!=='channel'||a.exit===exit));log.add(mv);if(!mv.length)break;const x=I.step(s,mv[Math.floor(r()*mv.length)]);log.add(x.events);if(!x.accepted)break;s=x.state;log.add(R.signature(s));}
 return {won:s.status==='Won',hash:log.digest()};}

