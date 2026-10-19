/* Physical places and a resumable visual work queue. Never changes allocation
 * or outcomes: rules.cjs remains the sole owner of the logical game state. */
(function(root,factory){
  const api=factory(typeof module!=='undefined'&&module.exports?require('./rules.cjs'):root.BloomPilotRules);
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.BloomPilotTimeline=api;
})(typeof globalThis!=='undefined'?globalThis:this,function(R){
 'use strict';
 const WORK_MS=45;
 const fields=['board','remaining','heads','slots','ages','revealed'];
 function beats(events,meta){
  const rounds=new Map(),steps=[];
  for(const event of events)if(event.type==='clear'||event.type==='complete'){
   if(!rounds.has(event.round))rounds.set(event.round,[]);rounds.get(event.round).push(event);
  }
  for(const events of rounds.values()){
   const workers=new Map(),complete=[];
   for(const e of events)if(e.type==='clear'){if(!workers.has(e.pod))workers.set(e.pod,[]);workers.get(e.pod).push(e);}else complete.push(e);
   const count=Math.max(0,...Array.from(workers.values(),a=>a.length)),ending=new Map();
   for(const e of complete){const last=Math.max(...meta.groups[e.pod].map(p=>workers.get(p)?.length||0))-1;if(last<0)throw Error('Completion without work');if(!ending.has(last))ending.set(last,[]);ending.get(last).push(e);}
   for(let i=0;i<count;i++)steps.push([...Array.from(workers.values(),a=>a[i]).filter(Boolean),...(ending.get(i)||[])]);
  }
  return steps;
 }
 class Timeline{
  constructor(state,saved=null){
   this.state=state;this.shown=R.clone(state);this.steps=[];this.cursor=0;this.phase=0;this.processedCells=0;this.lastClears=[];
   if(saved){
    if(saved.logicalSignature!==R.signature(state))throw Error('Presentation checkpoint belongs to another logical state');
    for(const key of fields){const values=saved.shown[key];if(!Array.isArray(values)||values.length!==state[key].length)throw Error('Invalid presentation checkpoint');this.shown[key]=new state[key].constructor(values);}
    this.shown.serial=saved.shown.serial;this.shown.bridge=saved.shown.bridge;this.shown._reach=null;
    this.steps=saved.steps;this.phase=saved.phase;this.processedCells=saved.processedCells;
    if(!Array.isArray(this.steps)||!Number.isFinite(this.phase)||this.phase<0||this.phase>=WORK_MS)throw Error('Invalid work queue');
   }
   this.assert();
  }
  get pending(){return this.cursor<this.steps.length;}
  get status(){return this.pending?'Working':this.state.status;}
  get free(){return this.shown.slots.reduce((n,p)=>n+(p<0),0);}
  get pendingCells(){let n=0;for(let i=this.cursor;i<this.steps.length;i++)for(const e of this.steps[i])if(e.type==='clear')n++;return n;}
  get workers(){return new Set((this.steps[this.cursor]||[]).filter(e=>e.type==='clear').map(e=>e.pod));}
  bridgeChoices(){return R.bridgeChoices(this.state).filter(c=>R.reach(this.shown).distance[c.anchor]>0);}
  check(action){
   const reason=R.check(this.state,action);if(reason)return reason;
   if(action.type==='bridge')return this.bridgeChoices().some(c=>c.choice===action.choice)?null:'visible-bridge-not-ready';
   return this.free<this.state.meta.groups[action.pod].length?'visual-places-busy':null;
  }
  commit(action){
   const reason=this.check(action);if(reason)return {accepted:false,reason};
   const result=R.step(this.state,action,true);if(!result.accepted)throw Error('Timeline/rules admission mismatch');
   this.state=result.state;const v=this.shown;
   v.heads.set(this.state.heads);v.revealed.set(this.state.revealed);v.ages.set(this.state.ages);v.serial=this.state.serial;
   for(const e of result.events){
    if(e.type==='commit'){const slot=v.slots.indexOf(-1);if(slot<0)throw Error('Physical place reused while occupied');v.slots[slot]=e.pod;}
    if(e.type==='bridge'){v.board[e.cell]=R.OPEN;v.bridge=this.state.bridge;v._reach=null;}
   }
   const extra=beats(result.events,this.state.meta);this.steps.push(...extra);
   this.assert();return {accepted:true,events:result.events};
  }
  advance(milliseconds,speed=1){
   if(!Number.isFinite(milliseconds)||milliseconds<0||![1,2].includes(speed))throw Error('Invalid work time');
   this.lastClears=[];if(!this.pending)return false;
   this.phase+=milliseconds*speed;let changed=false;
   while(this.pending&&this.phase>=WORK_MS){
    this.phase-=WORK_MS;
    for(const e of this.steps[this.cursor++]){
     if(e.type==='clear'){
      if(this.shown.board[e.cell]<0||this.shown.remaining[e.pod]<=0||!this.shown.slots.includes(e.pod))throw Error('Invalid visual allocation');
      this.shown.board[e.cell]=R.OPEN;this.shown.remaining[e.pod]--;this.shown._reach=null;this.processedCells++;this.lastClears.push(e.cell);
     }else if(e.type==='complete'){
      const place=this.shown.slots.indexOf(e.pod);if(place<0||this.shown.remaining[e.pod]!==0)throw Error('Invalid physical release');this.shown.slots[place]=-1;
     }
    }
    changed=true;
   }
   if(!this.pending){this.steps=[];this.cursor=0;this.phase=0;}
   else if(this.cursor>=128){this.steps=this.steps.slice(this.cursor);this.cursor=0;}
   this.lastClears=this.lastClears.slice(-32);this.assert();return changed;
  }
  snapshot(){
   const shown={serial:this.shown.serial,bridge:this.shown.bridge};for(const key of fields)shown[key]=Array.from(this.shown[key]);
   return {logicalSignature:R.signature(this.state),shown,steps:this.steps.slice(this.cursor),phase:this.phase,processedCells:this.processedCells};
  }
  assert(){
   R.assertAccounting(this.state);R.assertAccounting(this.shown);
   for(const key of ['heads','revealed'])if(this.shown[key].some((n,i)=>n!==this.state[key][i]))throw Error('Tray presentation is stale');
   if(this.shown.slots.length!==this.state.slots.length)throw Error('Capacity changed');
   if(!this.pending){
    for(const key of ['board','remaining'])if(this.shown[key].some((n,i)=>n!==this.state[key][i]))throw Error('Idle visual state differs from logical work');
    const a=Array.from(this.shown.slots).filter(p=>p>=0).sort((a,b)=>a-b).join(','),b=Array.from(this.state.slots).filter(p=>p>=0).sort((a,b)=>a-b).join(',');if(a!==b)throw Error('Idle physical occupants differ');
   }
   return true;
  }
 }
 return {Timeline,WORK_MS};
});
