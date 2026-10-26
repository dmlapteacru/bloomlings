// One attempt on the live timeline of our engine, recorded event by event: the buffer (free places, capsules working,
// stopped with nothing to reach, finished but held by a partner), every tap and release, every cell's clear time and
// every capsule's stops and resumes. Commands come either from a recording (times in model ms) or from a player
// (strategies.mjs); a recorded padlock opening ({type:'unlock', column}) is applied at its time; `patient` waits until all work has settled before each recorded command. Times are model
// milliseconds at ×1, on one clock: the engine's own clock stands still while nothing is pending, so the time that passes
// then (the player thinking with no work left) is kept apart in `idle` and added to every time recorded here. Commands
// timed from a recording therefore land at their own moment, not at the end of the work before them. A player with
// `reactionMs` takes that long to tap once it sees a move (the clock runs on, with or without work); `reactionMs` of
// 1e8 or more keeps the old meaning: tap only when all work has settled.
export function run(E,level,{commands=null,player=null,capacity=null,limitMs=3.6e6,reactionMs=0,patient=false}={}){
 const {R,T}=E,state=R.create(level,capacity??level.capacity??5),t=new T.Timeline(state),s0=state;
 const n=level.pods.length,cap=t.state.slots.length,cells=[];for(let i=0;i<s0.board.length;i++)if(s0.board[i]>=0)cells.push(i);
 const clearAt=new Float64Array(s0.board.length).fill(-1),samples=[],taps=[],releases=[],refused=[];
 const pod=Array.from({length:n},()=>({tap:null,firstDepart:null,release:null,stops:[],resumes:0,stopped:null,count:0}));
 level.pods.forEach((p,i)=>pod[i].count=p.count);
 let lastSample=null,idle=0;const clock=()=>t.now+idle;
 const colourOf=p=>t.state.meta.colours[p];
 const classify=()=>{const s=t.state,targets=R.reach(s).targets,free=new Set();for(const c of targets)if(t.reserved[c]<0)free.add(s.board[c]);
  const flying=new Int16Array(n);for(const j of t.jobs.values())flying[j.pod]++;
  let working=0,stalled=0,waiting=0;const stalledPods=[];
  for(const p of s.slots){if(p<0)continue;
   if(s.remaining[p]===0){if(flying[p])working++;else waiting++;continue;}
   if(flying[p]||free.has(colourOf(p))){working++;continue;}
   stalled++;stalledPods.push(p);}
  return {working,stalled,waiting,stalledPods,flying};};
 const record=()=>{const s=t.state,c=classify(),free=s.slots.reduce((a,p)=>a+(p<0),0),cleared=t.processedCells;
  for(const p of s.slots)if(p>=0&&s.remaining[p]>0){const st=pod[p];const isStalled=c.stalledPods.includes(p);
   if(isStalled&&st.stopped==null){st.stopped={from:clock(),left:s.remaining[p]};}
   else if(!isStalled&&st.stopped!=null){st.stops.push({from:st.stopped.from,to:clock(),left:st.stopped.left});st.stopped=null;st.resumes++;}}
  const row=[clock(),free,c.working,c.stalled,c.waiting,cleared];
  if(!lastSample||row.slice(1).some((v,i)=>v!==lastSample[i+1]))samples.push(row);lastSample=row;};
 const watch=()=>{for(const e of t.lastEvents){
   if(e.type==='depart'){const j=t.jobs.get(e.id);if(j&&pod[j.pod].firstDepart==null)pod[j.pod].firstDepart=e.at+idle;}
   if(e.type==='complete'){const at=e.at+idle;pod[e.pod].release=at;const last=releases.at(-1);if(last&&last.t===at)last.pods.push(e.pod);else releases.push({t:at,pods:[e.pod]});}}
  for(const c of t.lastClears)if(clearAt[c]<0)clearAt[c]=clock();};
 // target on the trace's clock; the engine advances while work is pending, the rest is idle time
 const advanceTo=target=>{while(t.pending&&t.steps[0].at+idle<=target+1e-7){t.advance(Math.max(0,t.steps[0].at-t.now));watch();record();}if(target>clock()){if(t.pending)t.advance(target-clock());else idle+=target-clock();}};
 const nextAt=()=>t.steps[0].at+idle;
 const doTap=(action,source)=>{const s=t.state,free=s.slots.reduce((a,p)=>a+(p<0),0),r=t.commit(action);
  if(!r.accepted){refused.push({t:clock(),action,reason:r.reason,source});return false;}
  const pods=action.type==='tap'?s.meta.groups[action.pod]:[];for(const p of pods)pod[p].tap=clock();
  taps.push({t:clock(),action,pods,freeBefore:free,source});watch();record();return true;};
 record();
 if(commands){for(const c of commands){if(c.t>clock())advanceTo(c.t);if(c.action.type==='unlock'){if(t.forceUnlock)t.forceUnlock(c.action.column);watch();record();continue;}if(patient)while(t.pending)advanceTo(nextAt());let guard=0;while(t.check(c.action)&&t.pending&&guard++<2000)advanceTo(Math.min(nextAt(),clock()+100));doTap(c.action,'recorded');}
  while(t.pending&&clock()<limitMs)advanceTo(nextAt());}
 else{let guard=0;while(guard++<5000){const st=t.status;if(st==='Won'||st==='Jammed'||st==='Stuck')break;
   const moves=R.legal(t.state).filter(a=>!t.check(a));const a=moves.length?player(t,moves):null;
   if(a){if(reactionMs>=1e8&&t.pending){advanceTo(clock()+reactionMs);continue;}
    if(reactionMs&&reactionMs<1e8){advanceTo(clock()+reactionMs);if(t.check(a)){const again=R.legal(t.state).filter(x=>!t.check(x));if(!again.length){if(!t.pending)break;advanceTo(nextAt());continue;}doTap(player(t,again),'player');continue;}}
    doTap(a,'player');continue;}
   if(!t.pending)break;advanceTo(nextAt());if(clock()>limitMs)break;}}
 for(const st of pod)if(st.stopped!=null){st.stops.push({from:st.stopped.from,to:clock(),left:st.stopped.left,open:true});st.stopped=null;}
 return {status:t.state.status==='Won'?'won':t.state.status==='Playing'&&t.pending?'unfinished':'lost',rawStatus:t.state.status,end:clock(),idle,capacity:cap,
  cells:cells.length,colours:cells.map(i=>s0.board[i]),cellIndex:cells,clearAt:cells.map(i=>clearAt[i]),samples,taps,releases,refused,pods:pod,w:level.w,h:level.h,
  left:t.state.level.tray.reduce((a,stack,c)=>a+stack.length-t.state.heads[c],0)};}
