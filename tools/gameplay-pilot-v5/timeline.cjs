/* Native logical allocation, physical target reservations and a live stream.
 * Dispatch uses the CURRENT open frontier. Couriers never wait for a reusable
 * worker or a predicted route; quota/cell ownership remain deterministic. */
(function(root,factory){const api=factory(typeof module!=='undefined'&&module.exports?require('./rules.cjs'):root.BloomPilotRules);if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.BloomPilotTimeline=api;})(globalThis,function(R){
 'use strict';
 const PROFILE=Object.freeze({outPerCell:28,returnPerCell:22,act:90,minOut:180,minReturn:140,departureGap:85,dispatch:'reachable-target-stream'});
 const fields=['board','remaining','heads','slots','ages','revealed'];
 const order={clear:0,arrive:1,return:2,check:3};
 class Timeline{
  constructor(state,saved=null){
   this.state=state;this.shown=R.clone(state);this.steps=[];this.cursor=0;this.now=0;this.serial=0;this.processedCells=0;this.lastClears=[];this.lastEvents=[];this.lastPlans=[];this.lastAssignments=[];this.jobs=new Map();this.byCell=new Map();
   this.ready=new Float64Array(state.board.length);this.reserved=new Int32Array(state.board.length).fill(-1);this.inFlight=new Int16Array(state.level.pods.length);this.departReady=new Float64Array(state.level.pods.length);
   if(saved){
    if(saved.logicalSignature!==R.signature(state)||saved.profile!==JSON.stringify(PROFILE))throw Error('Stale presentation checkpoint');
    for(const key of fields){const a=saved.shown[key];if(!Array.isArray(a)||a.length!==state[key].length)throw Error('Invalid checkpoint');this.shown[key]=new state[key].constructor(a);}
    this.shown.serial=saved.shown.serial;this.shown.bridge=saved.shown.bridge;this.shown._reach=null;this.now=saved.now;this.serial=saved.serial;this.processedCells=saved.processedCells;this.steps=saved.steps;
    this.jobs=new Map(saved.jobs.map(j=>[j.id,j]));this.ready=Float64Array.from(saved.ready);this.reserved=Int32Array.from(saved.reserved);this.inFlight=Int16Array.from(saved.inFlight);this.departReady=Float64Array.from(saved.departReady);
    if(!Number.isFinite(this.now)||this.now<0||this.ready.length!==state.board.length||this.inFlight.length!==state.level.pods.length||this.reserved.length!==state.board.length||!Array.isArray(this.steps)||this.steps.some(e=>e.at<this.now-1e-5))throw Error('Invalid work queue');
    for(const j of this.jobs.values())if(j.start===null||j.clear>this.now)this.byCell.set(j.cell,j.id);
   }
   this.assert();
  }
  get pending(){return this.steps.length>0;}
  get phase(){return this.now;}
  get status(){return this.pending?'Working':this.state.status;}
  get free(){return this.shown.slots.reduce((n,p)=>n+(p<0),0);}
  get pendingCells(){return this.byCell.size;}
  get workers(){return new Set([...this.jobs.values()].filter(j=>j.start!==null).map(j=>j.pod));}
  get hasChoice(){return R.legal(this.state).some(a=>!this.check(a));}
  get couriers(){return [...this.jobs.values()].filter(j=>j.start!==null).map(j=>{
   const back=this.now>=j.clear,t=back?(j.done-this.now)/(j.done-j.clear):Math.min(1,(this.now-j.start)/(j.arrive-j.start)),v=Math.max(0,t)*Math.max(0,j.route.length-1),at=Math.min(j.route.length-1,Math.floor(v)),a=j.route[at],b=j.route[Math.min(at+1,j.route.length-1)],f=v-at,w=this.state.level.w;
   return {id:j.id,pod:j.pod,x:a%w+(b%w-a%w)*f,y:Math.floor(a/w)+(Math.floor(b/w)-Math.floor(a/w))*f,carrying:back,acting:this.now>=j.arrive&&!back};
  });}
  bridgeChoices(){return R.bridgeChoices(this.state).filter(c=>R.reach(this.shown).distance[c.anchor]>0);}
  check(action){const reason=R.check(this.state,action);if(reason)return reason;if(action.type==='bridge')return this.bridgeChoices().some(c=>c.choice===action.choice)?null:'visible-bridge-not-ready';return this.free<this.state.meta.groups[action.pod].length?'visual-places-busy':null;}
  sort(){this.steps.sort((a,b)=>a.at-b.at||order[a.type]-order[b.type]||(a.id??a.pod)-(b.id??b.pod));}
  dispatch(){
   const byPod=new Map();
   for(const cell of R.reach(this.shown).targets){const id=this.byCell.get(cell);if(id===undefined||this.reserved[cell]>=0)continue;const j=this.jobs.get(id);if(j.start!==null)continue;if(!byPod.has(j.pod))byPod.set(j.pod,[]);byPod.get(j.pod).push(j);}
   for(const [pod,available] of [...byPod].sort((a,b)=>this.shown.ages[a[0]]-this.shown.ages[b[0]])){
    if(this.departReady[pod]<=this.now+1e-7){
     const j=available.shift();if(!this.shown.slots.includes(pod)||this.shown.board[j.cell]!==this.state.meta.colours[pod])throw Error('Invalid destination');
     j.route=R.routeTo(this.shown,j.cell);j.start=this.now;const distance=j.route.length-1;j.arrive=this.now+Math.max(PROFILE.minOut,distance*PROFILE.outPerCell);j.clear=j.arrive+PROFILE.act;j.done=j.clear+Math.max(PROFILE.minReturn,distance*PROFILE.returnPerCell);j.frontier=available.length+1;
     for(const c of j.route.slice(0,-1))if(this.shown.board[c]!==R.OPEN)throw Error('Route is not physically open');
     this.reserved[j.cell]=j.id;this.inFlight[pod]++;this.departReady[pod]=this.now+PROFILE.departureGap;
     const plan=[j.id,pod,j.cell,j.assigned,j.start,j.arrive,j.clear,j.done,j.route.length,j.frontier];this.lastPlans.push(plan);this.lastEvents.push({type:'depart',at:this.now,id:j.id,plan});
     this.steps.push({type:'arrive',at:j.arrive,id:j.id},{type:'clear',at:j.clear,id:j.id},{type:'return',at:j.done,id:j.id});
    }
    if(available.length&&!this.steps.some(e=>e.type==='check'&&e.pod===pod))this.steps.push({type:'check',at:this.departReady[pod],pod});
   }
   this.sort();
  }
  release(){
   for(const p of [...this.shown.slots])if(p>=0){const group=this.state.meta.groups[p];if(group.some(q=>this.state.remaining[q]||this.shown.remaining[q]||this.inFlight[q]))continue;
    for(const q of group){const at=this.shown.slots.indexOf(q);if(at>=0){this.shown.slots[at]=-1;this.lastEvents.push({type:'complete',at:this.now,pod:q});}}
   }
  }
  commit(action){
   const reason=this.check(action);if(reason)return {accepted:false,reason};const result=R.step(this.state,action,true);this.state=result.state;const v=this.shown;
   v.heads.set(this.state.heads);v.revealed.set(this.state.revealed);v.ages.set(this.state.ages);v.serial=this.state.serial;this.lastEvents=[];this.lastPlans=[];this.lastAssignments=[];
   for(const e of result.events){
    if(e.type==='commit'){const slot=v.slots.indexOf(-1);if(slot<0)throw Error('Physical place reused');v.slots[slot]=e.pod;}
    if(e.type==='bridge'){v.board[e.cell]=R.OPEN;v.bridge=this.state.bridge;v._reach=null;this.ready[e.cell]=this.now;}
    if(e.type==='clear'){
     if(this.byCell.has(e.cell))throw Error('Duplicate assignment');const id=++this.serial,j={id,pod:e.pod,cell:e.cell,assigned:this.now,start:null,route:null,arrive:null,clear:null,done:null};this.jobs.set(id,j);this.byCell.set(e.cell,id);
     this.lastAssignments.push([id,e.pod,e.cell,this.now]);this.lastEvents.push({type:'assign',at:this.now,id});
    }
   }
   this.dispatch();this.release();this.assert();return {accepted:true,events:result.events};
  }
  advance(milliseconds,speed=1){
   if(!Number.isFinite(milliseconds)||milliseconds<0||![1,2].includes(speed))throw Error('Invalid time');this.lastClears=[];this.lastEvents=[];this.lastPlans=[];if(!this.pending)return false;
   const end=this.now+milliseconds*speed;let changed=false;
   while(this.steps.length&&this.steps[0].at<=end+1e-7){
    this.now=this.steps[0].at;const due=[];while(this.steps.length&&this.steps[0].at<=this.now)due.push(this.steps.shift());
    for(const e of due){if(e.type==='check')continue;const j=this.jobs.get(e.id);if(!j)throw Error('Event without courier');
     this.lastEvents.push(e);
     if(e.type==='clear'){if(this.reserved[j.cell]!==j.id||this.shown.board[j.cell]<0||this.shown.remaining[j.pod]<=0)throw Error('Invalid allocation');this.reserved[j.cell]=-1;this.byCell.delete(j.cell);this.shown.board[j.cell]=R.OPEN;this.shown.remaining[j.pod]--;this.shown._reach=null;this.ready[j.cell]=this.now;this.processedCells++;this.lastClears.push(j.cell);changed=true;}
     if(e.type==='return'){this.inFlight[j.pod]--;this.jobs.delete(j.id);}
    }
    this.dispatch();const free=this.free;this.release();if(this.free!==free)changed=true;
   }
   if(this.pending)this.now=Math.max(end,this.now);this.lastClears=this.lastClears.slice(-32);this.assert();return changed;
  }
  snapshot(){const shown={serial:this.shown.serial,bridge:this.shown.bridge};for(const key of fields)shown[key]=Array.from(this.shown[key]);return {profile:JSON.stringify(PROFILE),logicalSignature:R.signature(this.state),shown,steps:this.steps.map(e=>({...e})),now:this.now,serial:this.serial,processedCells:this.processedCells,jobs:[...this.jobs.values()].map(j=>({...j})),ready:Array.from(this.ready),reserved:Array.from(this.reserved),inFlight:Array.from(this.inFlight),departReady:Array.from(this.departReady)};}
  assert(){
   R.assertAccounting(this.state);R.assertAccounting(this.shown,true);if(this.inFlight.some(n=>n<0))throw Error('Negative in-flight count');const targets=new Set(),quota=new Int16Array(this.state.level.pods.length);
   for(const j of this.jobs.values())if(j.start===null||j.clear>this.now){if(targets.has(j.cell))throw Error('Two assignments for one cell');targets.add(j.cell);quota[j.pod]++;}if(quota.some((n,p)=>n>this.shown.remaining[p]))throw Error('Assigned work exceeds quota');
   for(const key of ['heads','revealed'])if(this.shown[key].some((n,i)=>n!==this.state[key][i]))throw Error('Stale tray');
   if(!this.pending){if(this.jobs.size)throw Error('Assigned jobs deadlocked');for(const key of ['board','remaining'])if(this.shown[key].some((n,i)=>n!==this.state[key][i]))throw Error('Idle display mismatch');if(Array.from(this.shown.slots).filter(p=>p>=0).sort().join()!==Array.from(this.state.slots).filter(p=>p>=0).sort().join())throw Error('Idle occupants mismatch');}
   return true;
  }
 }
 return {Timeline,PROFILE};
});
