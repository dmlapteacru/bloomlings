/* Deterministic trips. Rules owns allocation; places remain occupied until
 * every courier of the pod/pair has returned. No backlog acceleration. */
(function(root,factory){const api=factory(typeof module!=='undefined'&&module.exports?require('./rules.cjs'):root.BloomPilotRules);if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.BloomPilotTimeline=api;})(globalThis,function(R){
 'use strict';
 const PROFILE=Object.freeze({outPerCell:10,returnPerCell:8,act:40,minOut:60,minReturn:50,departureGap:35,maxWorkers:2});
 const fields=['board','remaining','heads','slots','ages','revealed'];
 class Timeline{
  constructor(state,saved=null){
   this.state=state;this.shown=R.clone(state);this.steps=[];this.cursor=0;this.now=0;this.serial=0;this.processedCells=0;this.lastClears=[];this.lastEvents=[];this.lastPlans=[];this.jobs=new Map();this.ready=new Float64Array(state.board.length);this.workerReady=state.level.pods.map(()=>[0,0]);this.departReady=new Float64Array(state.level.pods.length);this.lastReturn=new Float64Array(state.level.pods.length);
   if(saved){
    if(saved.logicalSignature!==R.signature(state)||saved.profile!==JSON.stringify(PROFILE))throw Error('Stale presentation checkpoint');
    for(const key of fields){const a=saved.shown[key];if(!Array.isArray(a)||a.length!==state[key].length)throw Error('Invalid checkpoint');this.shown[key]=new state[key].constructor(a);}
    this.shown.serial=saved.shown.serial;this.shown.bridge=saved.shown.bridge;this.shown._reach=null;
    this.now=saved.now;this.serial=saved.serial;this.processedCells=saved.processedCells;this.steps=saved.steps;this.jobs=new Map(saved.jobs.map(j=>[j.id,j]));this.ready=Float64Array.from(saved.ready);this.workerReady=saved.workerReady;this.departReady=Float64Array.from(saved.departReady);this.lastReturn=Float64Array.from(saved.lastReturn);
    if(!Number.isFinite(this.now)||this.now<0||this.ready.length!==state.board.length||this.workerReady.length!==state.level.pods.length||this.steps.some(e=>e.at<this.now-1e-5)||[...this.jobs.values()].some(j=>j.done<=this.now-1e-5))throw Error('Invalid work queue');
   }
   this.assert();
  }
  get pending(){return this.cursor<this.steps.length;}
  get phase(){return this.now;}
  get status(){return this.pending?'Working':this.state.status;}
  get free(){return this.shown.slots.reduce((n,p)=>n+(p<0),0);}
  get pendingCells(){let n=0;for(let i=this.cursor;i<this.steps.length;i++)if(this.steps[i].type==='clear')n++;return n;}
  get workers(){return new Set([...this.jobs.values()].filter(j=>j.start<=this.now&&j.done>this.now).map(j=>j.pod));}
  get hasChoice(){return R.legal(this.state).some(a=>!this.check(a));}
  get couriers(){return [...this.jobs.values()].filter(j=>j.start<=this.now&&j.done>this.now).map(j=>{
   const back=this.now>=j.clear,t=back?(j.done-this.now)/(j.done-j.clear):Math.min(1,(this.now-j.start)/(j.arrive-j.start)),v=t*Math.max(0,j.route.length-1),at=Math.min(j.route.length-1,Math.floor(v)),a=j.route[at],b=j.route[Math.min(at+1,j.route.length-1)],f=v-at,w=this.state.level.w;
   return {id:j.id,pod:j.pod,worker:j.worker,x:a%w+(b%w-a%w)*f,y:Math.floor(a/w)+(Math.floor(b/w)-Math.floor(a/w))*f,carrying:back,acting:this.now>=j.arrive&&!back};
  });}
  bridgeChoices(){return R.bridgeChoices(this.state).filter(c=>R.reach(this.shown).distance[c.anchor]>0);}
  check(action){const reason=R.check(this.state,action);if(reason)return reason;if(action.type==='bridge')return this.bridgeChoices().some(c=>c.choice===action.choice)?null:'visible-bridge-not-ready';return this.free<this.state.meta.groups[action.pod].length?'visual-places-busy':null;}
  commit(action){
   const reason=this.check(action);if(reason)return {accepted:false,reason};const result=R.step(this.state,action,true);this.state=result.state;const v=this.shown;
   v.heads.set(this.state.heads);v.revealed.set(this.state.revealed);v.ages.set(this.state.ages);v.serial=this.state.serial;this.lastPlans=[];
   const widths=new Map();for(const e of result.events)if(e.type==='clear'){const key=e.round+':'+e.pod;widths.set(key,(widths.get(key)||0)+1);}
   const extra=[];
   for(const e of result.events){
    if(e.type==='commit'){const slot=v.slots.indexOf(-1);if(slot<0)throw Error('Physical place reused');v.slots[slot]=e.pod;}
    if(e.type==='bridge'){v.board[e.cell]=R.OPEN;v.bridge=this.state.bridge;v._reach=null;this.ready[e.cell]=this.now;}
    if(e.type==='clear'){
     let dependency=this.now;for(const cell of e.route.slice(0,-1))dependency=Math.max(dependency,this.ready[cell]);
     const workers=widths.get(e.round+':'+e.pod)>1?2:1,slots=this.workerReady[e.pod],worker=workers===2&&slots[1]<slots[0]?1:0;
     const start=Math.max(dependency,slots[worker],this.departReady[e.pod]),distance=e.route.length-1,out=Math.max(PROFILE.minOut,distance*PROFILE.outPerCell),back=Math.max(PROFILE.minReturn,distance*PROFILE.returnPerCell),arrive=start+out,clear=arrive+PROFILE.act,done=clear+back,id=++this.serial;
     const job={id,pod:e.pod,cell:e.cell,route:e.route,worker,start,arrive,clear,done};this.jobs.set(id,job);slots[worker]=done;this.departReady[e.pod]=start+PROFILE.departureGap;this.lastReturn[e.pod]=Math.max(this.lastReturn[e.pod],done);this.ready[e.cell]=clear;
     extra.push({type:'depart',at:start,id},{type:'clear',at:clear,id,pod:e.pod,cell:e.cell},{type:'return',at:done,id});this.lastPlans.push([e.pod,e.cell,start,clear,done,e.route.length,worker,widths.get(e.round+':'+e.pod)]);
    }
    if(e.type==='complete'){const at=Math.max(this.now,...this.state.meta.groups[e.pod].map(p=>this.lastReturn[p]));extra.push({type:'complete',at,pod:e.pod});}
   }
   const order={depart:0,clear:1,return:2,complete:3};this.steps=[...this.steps.slice(this.cursor),...extra].sort((a,b)=>a.at-b.at||order[a.type]-order[b.type]||(a.id||a.pod)-(b.id||b.pod));this.cursor=0;this.assert();return {accepted:true,events:result.events};
  }
  advance(milliseconds,speed=1){
   if(!Number.isFinite(milliseconds)||milliseconds<0||![1,2].includes(speed))throw Error('Invalid time');this.lastClears=[];this.lastEvents=[];if(!this.pending)return false;
   const end=this.now+milliseconds*speed;let changed=false;
   while(this.pending&&this.steps[this.cursor].at<=end+1e-7){const e=this.steps[this.cursor++];this.now=e.at;this.lastEvents.push(e);
    if(e.type==='clear'){if(this.shown.board[e.cell]<0||this.shown.remaining[e.pod]<=0||!this.shown.slots.includes(e.pod))throw Error('Invalid allocation');this.shown.board[e.cell]=R.OPEN;this.shown.remaining[e.pod]--;this.shown._reach=null;this.processedCells++;this.lastClears.push(e.cell);changed=true;}
    if(e.type==='return')this.jobs.delete(e.id);
    if(e.type==='complete'){const slot=this.shown.slots.indexOf(e.pod);if(slot<0||this.shown.remaining[e.pod])throw Error('Premature release');this.shown.slots[slot]=-1;changed=true;}
   }
   this.now=this.pending?end:Math.min(end,this.now);if(!this.pending){this.steps=[];this.cursor=0;}else if(this.cursor>=128){this.steps=this.steps.slice(this.cursor);this.cursor=0;}
   this.lastClears=this.lastClears.slice(-32);this.assert();return changed;
  }
  snapshot(){const shown={serial:this.shown.serial,bridge:this.shown.bridge};for(const key of fields)shown[key]=Array.from(this.shown[key]);return {profile:JSON.stringify(PROFILE),logicalSignature:R.signature(this.state),shown,steps:this.steps.slice(this.cursor),now:this.now,serial:this.serial,processedCells:this.processedCells,jobs:[...this.jobs.values()],ready:Array.from(this.ready),workerReady:this.workerReady.map(a=>a.slice()),departReady:Array.from(this.departReady),lastReturn:Array.from(this.lastReturn)};}
  assert(){R.assertAccounting(this.state);R.assertAccounting(this.shown,true);for(const key of ['heads','revealed'])if(this.shown[key].some((n,i)=>n!==this.state[key][i]))throw Error('Stale tray');if(!this.pending){for(const key of ['board','remaining'])if(this.shown[key].some((n,i)=>n!==this.state[key][i]))throw Error('Idle display mismatch');if(Array.from(this.shown.slots).filter(p=>p>=0).sort().join()!==Array.from(this.state.slots).filter(p=>p>=0).sort().join())throw Error('Idle occupants mismatch');}return true;}
 }
 return {Timeline,PROFILE};
});
