// Exact event-time wall-clock diagnostics of THIS pilot, not measured human time or Colony Flow physics.
import R from './rules.cjs';import T from './timeline.cjs';
const {Timeline,TAIL}=T;
const round=x=>Math.round(x*1000)/1000;
export function replayRhythm(level,actions,{schedule='patient',manualSpeed=1,thinkMs=1200,reserveSlots=1}={}){
 const t=new Timeline(R.create(level)),total=level.pods.reduce((n,p)=>n+p.count,0);
 let wall=0,lastAction=null,commands=0,events=0,maxNoChoice=0,noChoiceRun=0,maxLowRun=0,lowRun=0;
 const buckets={choice:0,noChoiceWorking:0,tail:0,deliberation:0},occupation=Array(6).fill(0),productive=Array(6).fill(0),commitAt=new Map(),residences=[],trace=[];
 let reserveGuardSeconds=0,maxReserveGuardSeconds=0;
 let zeroQuotaHeldSlotSeconds=0,completedLinkedHeldSlotSeconds=0,midLowProductivity=0,lowWithChoice=0,lowWithoutChoice=0,forcedLowRun=0,maxForcedLowRun=0;
 function profile(dt,deliberation=false){
  const s=t.state,held=Array.from(s.slots).filter(p=>p>=0),front=R.reach(s).targets,frontColors=new Set(front.filter(c=>t.reserved[c]<0).map(c=>s.board[c]));
  const outbound=new Set([...t.jobs.values()].filter(j=>j.clear>t.now).map(j=>j.pod));
  const working=held.filter(p=>s.remaining[p]>0&&frontColors.has(s.meta.colours[p])||outbound.has(p)).length;
  const freeChoice=R.legal(s).some(a=>!t.check(a)),tail=t.tail;
  if(deliberation)buckets.deliberation+=dt;else if(tail)buckets.tail+=dt;else if(freeChoice)buckets.choice+=dt;else buckets.noChoiceWorking+=dt;
  occupation[held.length]+=dt;productive[Math.min(5,working)]+=dt;
  zeroQuotaHeldSlotSeconds+=dt*held.filter(p=>s.remaining[p]===0).length;
  completedLinkedHeldSlotSeconds+=dt*held.filter(p=>level.pods[p].group&&s.remaining[p]===0&&!outbound.has(p)&&s.meta.groups[p].some(q=>s.remaining[q]>0)).length;
  if(!tail&&working>0&&working<=2){if(freeChoice)lowWithChoice+=dt;else lowWithoutChoice+=dt;}
  if(!tail&&!freeChoice&&working>0&&working<=2){forcedLowRun+=dt;maxForcedLowRun=Math.max(maxForcedLowRun,forcedLowRun);}else forcedLowRun=0;
  if(!tail&&!freeChoice){noChoiceRun+=dt;maxNoChoice=Math.max(maxNoChoice,noChoiceRun);}else noChoiceRun=0;
  if(!tail&&working>0&&working<=2){midLowProductivity+=dt;lowRun+=dt;maxLowRun=Math.max(maxLowRun,lowRun);}else lowRun=0;
 }
 function nextEvent(){
  if(!t.pending)return false;
  if(++events>100000)throw Error('Event budget L'+level.id);
  const model=Math.max(0,t.steps[0].at-t.now),dt=model/(t.tail?TAIL.speed:manualSpeed);profile(dt/1000);wall+=dt;
  t.advance(model);for(const e of t.lastEvents)if(e.type==='complete'&&commitAt.has(e.pod))residences.push({pod:e.pod,color:level.pods[e.pod].color,quota:level.pods[e.pod].count,group:level.pods[e.pod].group||null,seconds:round((wall-commitAt.get(e.pod))/1000)});
  return true;
 }
 function waitWall(ms){
  while(ms>1e-7&&t.pending){const rate=t.tail?TAIL.speed:manualSpeed,until=Math.max(0,t.steps[0].at-t.now)/rate;
   if(until<=ms+1e-7){ms-=until;nextEvent();}else{profile(ms/1000);wall+=ms;t.advance(ms*rate);ms=0;}}
  if(ms>0&&t.state.status==='Playing'){profile(ms/1000,true);wall+=ms;}
 }
 for(const action of actions){
  if(schedule==='patient')while(t.pending)nextEvent();
  waitWall(thinkMs);
  while(t.check(action)&&t.pending)nextEvent();
  if(schedule==='reserve'&&action.type==='tap'){const size=t.state.meta.groups[action.pod].length,before=wall;while(t.pending&&t.free-size<reserveSlots)nextEvent();const guard=(wall-before)/1000;reserveGuardSeconds+=guard;maxReserveGuardSeconds=Math.max(maxReserveGuardSeconds,guard);}
  const reason=t.check(action);if(reason)return {won:false,status:t.status,refusedAt:commands+1,reason,schedule,seconds:round(wall/1000),commands};
  const heldBefore=Array.from(t.state.slots);const result=t.commit(action);if(!result.accepted)throw Error('Invalid certificate');commands++;lastAction=wall;
  for(const e of result.events)if(e.type==='commit')commitAt.set(e.pod,wall);
  trace.push({command:commands,action,atSeconds:round(wall/1000),cleared:round(t.processedCells/total),heldBefore:heldBefore.filter(p=>p>=0).length,heldAfter:Array.from(t.state.slots).filter(p=>p>=0).length});
 }
 while(t.pending)nextEvent();
 return {won:t.status==='Won',status:t.status,schedule,manualSpeed,thinkMs,reserveSlots:schedule==='reserve'?reserveSlots:0,seconds:round(wall/1000),lastActionSeconds:round((lastAction??0)/1000),tailSeconds:round((wall-(lastAction??wall))/1000),
  // After-last-action and auto-tail differ if the certificate is incomplete; both are recorded explicitly.
  autoTailSeconds:round(buckets.tail),buckets:Object.fromEntries(Object.entries(buckets).map(([k,v])=>[k,round(v)])),
  reserveGuardSeconds:round(reserveGuardSeconds),maxReserveGuardSeconds:round(maxReserveGuardSeconds),maxNoChoiceSeconds:round(maxNoChoice),midOneOrTwoProductiveSeconds:round(midLowProductivity),maxOneOrTwoProductiveSeconds:round(maxLowRun),
  zeroQuotaHeldSlotSeconds:round(zeroQuotaHeldSlotSeconds),completedLinkedHeldSlotSeconds:round(completedLinkedHeldSlotSeconds),
  lowProductivityWithLegalChoiceSeconds:round(lowWithChoice),lowProductivityWithoutLegalChoiceSeconds:round(lowWithoutChoice),maxForcedLowProductivitySeconds:round(maxForcedLowRun),occupationSeconds:occupation.map(round),productiveSeconds:productive.map(round),
  longestResidences:residences.sort((a,b)=>b.seconds-a.seconds).slice(0,8),commands,events,trace};
}
export function compareRhythm(level,actions){return {patient1x:replayRhythm(level,actions),patient2x:replayRhythm(level,actions,{manualSpeed:2}),brisk1x:replayRhythm(level,actions,{schedule:'brisk'}),brisk2x:replayRhythm(level,actions,{schedule:'brisk',manualSpeed:2}),reserve1x:replayRhythm(level,actions,{schedule:'reserve'}),reserve2x:replayRhythm(level,actions,{schedule:'reserve',manualSpeed:2})};}
