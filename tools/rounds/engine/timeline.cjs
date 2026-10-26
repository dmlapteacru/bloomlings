/* v8D/v9: the auto-tail. Once the queue is empty (and no bridge is still to choose) every command is given and the
 * outcome is fixed, so the page plays the rest at TAIL.speed (×2.5, couriers still leave one after another).
 * v8 authoritative live simulation. A slot stores UNSENT quota. Targets are
 * reserved only at dispatch; walking jobs outlive the slot and can overlap
 * commands. No predicted clear ownership, reusable-worker cap or hidden speed. */
(function(root,factory){const api=factory(typeof module!=='undefined'&&module.exports?require('./rules.cjs'):root.BloomPilotRules);if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.BloomPilotTimeline=api;})(globalThis,function(R){
 'use strict';
 const PROFILE=Object.freeze({outPerCell:100,returnPerCell:75,act:100,minOut:650,minReturn:400,departureGap:250,releaseLag:300,dispatch:'live-shared-frontier',release:'last-departure-plus-300ms',sameColour:'independent-cadence-oldest-tie'});
 // Keep current v8E tail: couriers still leave one after another; automatic ×2.5.
 const TAIL=Object.freeze({departureGap:250,speed:2.5,when:'queue-empty-and-no-bridge-to-choose'});
 const tailOf=s=>R.queueEmpty(s)&&(!s.level.hero||s.bridge>=0);
 const fields=['board','remaining','heads','slots','ages','revealed'];
 const order={clear:0,arrive:1,return:2,release:3,check:4};
 class Timeline{
  constructor(state,saved=null){
   this.state=R.clone(state);this.shown=this.state;this.steps=[];this.cursor=0;this.now=0;this.serial=0;this.processedCells=0;this.jobs=new Map();this.reserved=new Int32Array(state.board.length).fill(-1);this.inFlight=new Int16Array(state.level.pods.length);this.departReady=new Float64Array(state.level.pods.length);this.clearLogs();
   if(saved){
    if(saved.profile!==JSON.stringify({...PROFILE,tail:TAIL})||saved.definition!==JSON.stringify(state.level))throw Error('Stale checkpoint');
    for(const key of fields){const a=saved.state[key];if(!Array.isArray(a)||a.length!==this.state[key].length)throw Error('Invalid checkpoint');this.state[key]=new this.state[key].constructor(a);}
    Object.assign(this.state,{serial:saved.state.serial,bridge:saved.state.bridge,status:saved.state.status,_reach:null});
    this.steps=saved.steps.map(e=>({...e}));this.now=saved.now;this.serial=saved.serial;this.processedCells=saved.processedCells;this.jobs=new Map(saved.jobs.map(j=>[j.id,{...j,route:j.route.slice()}]));this.reserved=Int32Array.from(saved.reserved);this.inFlight=Int16Array.from(saved.inFlight);this.departReady=Float64Array.from(saved.departReady);
    if(!Number.isFinite(this.now)||this.now<0||this.steps.some(e=>e.at<this.now-1e-7))throw Error('Invalid event time');
   }
   this.updateStatus();this.assert();
  }
  clearLogs(){this.lastClears=[];this.lastEvents=[];this.lastPlans=[];this.lastAssignments=[];}
  get pending(){return this.steps.length>0;}
  get phase(){return this.now;}
  get status(){return this.pending?'Working':this.state.status;}
  get free(){return this.state.slots.reduce((n,p)=>n+(p<0),0);}
  get pendingCells(){return this.reserved.reduce((n,r)=>n+(r>=0),0);}
  get workers(){return new Set([...this.jobs.values()].map(j=>j.pod));}
  get tail(){return tailOf(this.state);}
  get hasChoice(){return R.legal(this.state).some(a=>!this.check(a));}
  get couriers(){return [...this.jobs.values()].map(j=>{
   const back=this.now>=j.clear,t=back?(j.done-this.now)/(j.done-j.clear):Math.min(1,(this.now-j.start)/(j.arrive-j.start)),v=Math.max(0,t)*Math.max(0,j.route.length-1),at=Math.min(j.route.length-1,Math.floor(v)),a=j.route[at],b=j.route[Math.min(at+1,j.route.length-1)],f=v-at,w=this.state.level.w;
   return {id:j.id,pod:j.pod,x:a%w+(b%w-a%w)*f,y:Math.floor(a/w)+(Math.floor(b/w)-Math.floor(a/w))*f,carrying:back,acting:this.now>=j.arrive&&!back};
  });}
  bridgeChoices(){return R.bridgeChoices(this.state);}
  check(action){return R.check(this.state,action);}
  sort(){this.steps.sort((a,b)=>a.at-b.at||order[a.type]-order[b.type]||(a.id??a.pod)-(b.id??b.pod));}
  updateStatus(){
   const s=this.state;
   if(this.pending){s.status='Playing';return;}
   if(!s.board.some(c=>c>=0)){s.status='Won';return;}
   if(R.bridgeChoices(s).length){s.status='Playing';return;}
   if(!s.slots.includes(-1)){s.status='Jammed';return;}
   s.status=R.topPods(s).some(p=>R.canTap(s,p)===null)?'Playing':'Stuck';
  }
  dispatch(){
   const s=this.state,frontier=R.reach(s).targets,active=Array.from(s.slots).filter(p=>p>=0&&s.remaining[p]>0).sort((a,b)=>s.ages[a]-s.ages[b]),gap=tailOf(s)?TAIL.departureGap:PROFILE.departureGap;
   for(const pod of active){
    const available=frontier.filter(c=>s.board[c]===s.meta.colours[pod]&&this.reserved[c]<0);if(!available.length)continue;
    while(s.remaining[pod]>0&&available.length&&this.departReady[pod]<=this.now+1e-7){
     const cell=available.shift(),route=R.routeTo(s,cell),id=++this.serial,distance=route.length-1,start=this.now,arrive=start+Math.max(PROFILE.minOut,distance*PROFILE.outPerCell),clear=arrive+PROFILE.act,done=clear+Math.max(PROFILE.minReturn,distance*PROFILE.returnPerCell);
     for(const c of route.slice(0,-1))if(s.board[c]!==R.OPEN)throw Error('Closed route at dispatch');
     const j={id,pod,cell,assigned:start,start,arrive,clear,done,route,frontier:available.length+1};
     this.jobs.set(id,j);this.reserved[cell]=id;this.inFlight[pod]++;s.remaining[pod]--;this.departReady[pod]=start+gap;
     const plan=[id,pod,cell,start,start,arrive,clear,done,route.length,j.frontier];this.lastPlans.push(plan);this.lastAssignments.push([id,pod,cell,start]);
     this.lastEvents.push({type:'assign',at:start,id},{type:'depart',at:start,id,plan});
     this.steps.push({type:'arrive',at:arrive,id},{type:'clear',at:clear,id},{type:'return',at:done,id});
    }
    if(s.remaining[pod]>0&&available.length&&!this.steps.some(e=>e.type==='check'&&e.pod===pod))this.steps.push({type:'check',at:this.departReady[pod],pod});
   }
   const seen=new Set();for(const pod of s.slots)if(pod>=0){const group=s.meta.groups[pod],first=group[0];if(seen.has(first))continue;seen.add(first);
    if(group.every(p=>s.remaining[p]===0)&&!this.steps.some(e=>e.type==='release'&&e.pod===first))this.steps.push({type:'release',at:this.now+PROFILE.releaseLag,pod:first});
   }
   this.sort();this.updateStatus();
  }
  commit(action){
   const reason=this.check(action);if(reason)return {accepted:false,reason,events:[]};this.clearLogs();const s=this.state,events=[];
   if(action.type==='tap'&&s.meta.groups[action.pod].length>1)R.compactSlots(s);
   if(action.type==='tap')for(const pod of s.meta.groups[action.pod]){
    const column=s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===pod),slot=s.slots.indexOf(-1);
    s.heads[column]++;s.slots[slot]=pod;s.ages[pod]=++s.serial;events.push({type:'commit',pod,slot});if(!s.revealed[pod]){s.revealed[pod]=1;events.push({type:'reveal',pod,mode:'sealed',color:s.level.pods[pod].color});}
   }else{const c=s.level.hero.choices[action.choice];s.board[c.cell]=R.OPEN;s.bridge=action.choice;s._reach=null;events.push({type:'bridge',cell:c.cell});}
   for(const p of R.topPods(s))if(!s.revealed[p]&&s.level.pods[p].mystery!=='sealed'){s.revealed[p]=1;events.push({type:'reveal',pod:p,mode:'depth',color:s.level.pods[p].color});}
   this.dispatch();this.assert();return {accepted:true,events};
  }
  advance(milliseconds,speed=1){
   if(!Number.isFinite(milliseconds)||milliseconds<0||![1,2,TAIL.speed].includes(speed))throw Error('Invalid time');this.clearLogs();if(!this.pending)return false;
   const end=this.now+milliseconds*speed;let changed=false;
   while(this.steps.length&&this.steps[0].at<=end+1e-7){
    this.now=this.steps[0].at;const due=[];while(this.steps.length&&this.steps[0].at<=this.now)due.push(this.steps.shift());
    for(const e of due){
     if(e.type==='check')continue;
     if(e.type==='release'){
      const group=this.state.meta.groups[e.pod];if(group.some(p=>this.state.remaining[p]!==0))throw Error('Early pair release');
      for(const pod of group){const slot=this.state.slots.indexOf(pod);if(slot<0)throw Error('Missing released occupant');this.state.slots[slot]=-1;this.lastEvents.push({type:'complete',at:this.now,pod});}changed=true;continue;
     }
     const j=this.jobs.get(e.id);if(!j)throw Error('Event without courier');this.lastEvents.push(e);
     if(e.type==='clear'){
      if(this.reserved[j.cell]!==j.id||this.state.board[j.cell]!==this.state.meta.colours[j.pod])throw Error('Invalid reservation');
      this.reserved[j.cell]=-1;this.state.board[j.cell]=R.OPEN;this.state._reach=null;this.processedCells++;this.lastClears.push(j.cell);changed=true;
     }
     if(e.type==='return'){this.inFlight[j.pod]--;this.jobs.delete(j.id);}
    }
    const serial=this.serial;this.dispatch();if(serial!==this.serial)changed=true;
   }
   if(this.pending)this.now=Math.max(end,this.now);this.lastClears=this.lastClears.slice(-32);this.updateStatus();this.assert();return changed;
  }
  snapshot(){const state={serial:this.state.serial,bridge:this.state.bridge,status:this.state.status};for(const k of fields)state[k]=Array.from(this.state[k]);return {profile:JSON.stringify({...PROFILE,tail:TAIL}),definition:JSON.stringify(this.state.level),state,steps:this.steps.map(e=>({...e})),now:this.now,serial:this.serial,processedCells:this.processedCells,jobs:[...this.jobs.values()].map(j=>({...j,route:j.route.slice()})),reserved:Array.from(this.reserved),inFlight:Array.from(this.inFlight),departReady:Array.from(this.departReady)};}
  assert(){
   const s=this.state,cells=new Int32Array(R.CHANNELS.length),work=cells.slice(),flights=new Int16Array(s.remaining.length),reserved=new Map();
   for(const c of s.board)if(c>=0)cells[c]++;s.remaining.forEach((n,p)=>{if(n<0)throw Error('Negative quota');work[s.meta.colours[p]]+=n;});
   for(const j of this.jobs.values()){
    flights[j.pod]++;if(j.clear>this.now){if(reserved.has(j.cell)||this.reserved[j.cell]!==j.id)throw Error('Duplicate/missing cell reservation');reserved.set(j.cell,j.id);work[s.meta.colours[j.pod]]++;}
   }
   if(cells.some((n,c)=>n!==work[c]))throw Error('Conservation: board = unsent + outbound');
   if(this.inFlight.some((n,p)=>n<0||n!==flights[p]))throw Error('Flight count mismatch');
   if(this.reserved.some((id,c)=>id>=0&&reserved.get(c)!==id))throw Error('Orphan reservation');
   const live=s.level.tray.flatMap((stack,c)=>stack.slice(s.heads[c])).concat(Array.from(s.slots).filter(p=>p>=0));
   if(new Set(live).size!==live.length||s.remaining.some((n,p)=>n>0&&!live.includes(p)))throw Error('Pod location mismatch');
   if(!this.pending&&(this.jobs.size||this.reserved.some(id=>id>=0)))throw Error('Lost scheduled work');
   return true;
  }
 }
 return {Timeline,PROFILE,TAIL};
});
