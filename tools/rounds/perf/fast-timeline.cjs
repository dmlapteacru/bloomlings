/* THE FAST TWIN of tools/rounds/engine/timeline.cjs (tools/rounds/perf/README.md): the same live simulation on
 * fast-rules.cjs with faster bookkeeping only. A new step goes straight to its place in the sorted step list (the step
 * order is a total order, so the list is the one the reference's sort gives); whether a check or a release is pending is a
 * flag per pod instead of a scan of the steps; dispatch filters the frontier once per colour instead of once per pod (a
 * later pod of the same colour takes what the earlier left, as the reference's filter gives); the invariant check
 * assert() runs on a restored checkpoint and when ROUNDS_FAST_ASSERT is set (the reference runs it after every commit and
 * advance; it only throws, never changes anything). Never edit the rules here: change the reference, then port the
 * change and verify again (tools/rounds/perf/verify.mjs).
 */
/* Water channel and wooden shutters (17 §8, 2026-11-05; tools/rounds/README.md «Channel», «Shutters»): the channel
 * command commits like a bridge (free, no place), then channelOpen and the cell reveals and latches it causes at the commit
 * time; a walker whose route starts at the chosen exit rides rideUs before it walks and rideUs after it walks back (its
 * job carries `ride`, in ms); a clear (or a bridge) that reaches the spring emits channelReady after its cell reveals and
 * latches; a clear opens the shutters whose count it reached first of all (shutterOpen at its time, before its cell
 * reveals). A restored checkpoint takes the chosen exit from the saved state and recomputes the rest (rules.cjs
 * syncShutters, syncChannel). Fast twin: on a channel level a courier's route is made at its departure (its source and
 * its ride depend on the choice of that moment).
 * Latch + acorn key (17 §8, 2026-11-01): a latch opens (rules.cjs openLatches) at the model time of the change that makes
 * it true: a tap's commit (in the commit's events, `at` = the commit time, before the depth reveals it exposes), a clear
 * (right after that clear's cell reveals, at its time, then the depth reveals of the pods it exposes, also at that time)
 * or a bridge (after its cell reveals); a restored checkpoint recomputes the latches from heads and board (syncLatches).
 * Hidden cells (17 §8, 2026-11-01): a clear or a bridge reveals the hidden cells it opens (rules.cjs revealCells), as
 * cellReveal events at that change's time; a restored checkpoint recomputes them from the board (rules.cjs syncCells).
 * v8D/v9: the auto-tail. Once the queue is empty (and no bridge is still to choose) every command is given and the
 * outcome is fixed, so the page plays the rest at TAIL.speed (×2.5, couriers still leave one after another).
 * v8 authoritative live simulation. A slot stores UNSENT quota. Targets are
 * reserved only at dispatch; walking jobs outlive the slot and can overlap
 * commands. No predicted clear ownership, reusable-worker cap or hidden speed. */
(function(root,factory){module.exports=factory(require('./fast-rules.cjs'));})(globalThis,function(R){
 'use strict';
 const PROFILE=Object.freeze({outPerCell:100,returnPerCell:75,act:100,minOut:650,minReturn:400,departureGap:250,releaseLag:300,dispatch:'live-shared-frontier',release:'last-departure-plus-300ms',sameColour:'independent-cadence-oldest-tie'});
 // Keep current v8E tail: couriers still leave one after another; automatic ×2.5.
 const TAIL=Object.freeze({departureGap:250,speed:2.5,when:'queue-empty-and-no-bridge-to-choose'});
 const tailOf=s=>R.queueEmpty(s)&&(!s.level.hero||s.bridge>=0)&&(!s.level.channel||s.channel>=0);
 const fields=['board','remaining','heads','slots','ages','revealed'];
 const order={clear:0,arrive:1,return:2,release:3,check:4};
 const ASSERT=!!process.env.ROUNDS_FAST_ASSERT;
 // the reference's step order (sort's comparator), a total order on the steps that can be pending together
 const before=(a,b)=>a.at-b.at||order[a.type]-order[b.type]||(a.id??a.pod)-(b.id??b.pod);
 // a courier (the reference's plain object {id,pod,cell,assigned,start,arrive,clear,done,route,frontier}); its route is made
 // when read (Timeline.routeAt), and snapshot() writes the keys in the reference's order
 class Job{
  constructor(id,pod,cell,start,arrive,clear,done,tl,k,frontier,route=null,ride=0){this.id=id;this.pod=pod;this.cell=cell;this.assigned=start;this.start=start;this.arrive=arrive;this.clear=clear;this.done=done;this.frontier=frontier;this._tl=tl;this._k=k;this._route=route;if(ride)this.ride=ride;}
  get route(){return this._route||(this._route=this._tl.routeAt(this._k,this.cell));}
 }
 const jobRecord=j=>({id:j.id,pod:j.pod,cell:j.cell,assigned:j.assigned,start:j.start,arrive:j.arrive,clear:j.clear,done:j.done,route:j.route.slice(),frontier:j.frontier,...(j.ride?{ride:j.ride}:{})});
 class Timeline{
  constructor(state,saved=null){
   this.state=R.clone(state);this.shown=this.state;this.steps=[];this.cursor=0;this.now=0;this.serial=0;this.processedCells=0;this.jobs=new Map();this.reserved=new Int32Array(state.board.length).fill(-1);this.inFlight=new Int16Array(state.level.pods.length);this.departReady=new Float64Array(state.level.pods.length);this.checking=new Uint8Array(state.level.pods.length);this.opened=[];this.releasing=new Uint8Array(state.level.pods.length);this.clearLogs();
   if(saved){
    if(saved.profile!==JSON.stringify({...PROFILE,tail:TAIL})||saved.definition!==JSON.stringify(state.level))throw Error('Stale checkpoint');
    for(const key of fields){const a=saved.state[key];if(!Array.isArray(a)||a.length!==this.state[key].length)throw Error('Invalid checkpoint');this.state[key]=new this.state[key].constructor(a);}
    Object.assign(this.state,{serial:saved.state.serial,bridge:saved.state.bridge,status:saved.state.status,_reach:null});
    if(this.state.meta.channel){const c=saved.state.channel;if(!Number.isInteger(c)||c<-1||c>=this.state.meta.channel.exits.length)throw Error('Invalid checkpoint');this.state.channel=c;}
    R.syncShutters(this.state);R.syncCells(this.state);R.syncLatches(this.state);R.syncChannel(this.state);
    this.steps=saved.steps.map(e=>({...e}));this.now=saved.now;this.serial=saved.serial;this.processedCells=saved.processedCells;this.jobs=new Map(saved.jobs.map(j=>[j.id,{...j,route:j.route.slice()}]));this.reserved=Int32Array.from(saved.reserved);this.inFlight=Int16Array.from(saved.inFlight);this.departReady=Float64Array.from(saved.departReady);
    if(!Number.isFinite(this.now)||this.now<0||this.steps.some(e=>e.at<this.now-1e-7))throw Error('Invalid event time');
    for(const e of this.steps){if(e.type==='check')this.checking[e.pod]=1;else if(e.type==='release')this.releasing[e.pod]=1;}
   }
   this.updateStatus();if(saved||ASSERT)this.assert();
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
   // a channel rider (j.ride, ms) is in the water for ride ms from its start and for the last ride ms before it is home:
   // drawn at the exit, riding
   const r=j.ride||0,back=this.now>=j.clear,t=back?(j.done-r-this.now)/(j.done-r-j.clear):Math.min(1,(this.now-j.start-r)/(j.arrive-j.start-r)),v=Math.max(0,t)*Math.max(0,j.route.length-1),at=Math.min(j.route.length-1,Math.floor(v)),a=j.route[at],b=j.route[Math.min(at+1,j.route.length-1)],f=v-at,w=this.state.level.w;
   return {id:j.id,pod:j.pod,x:a%w+(b%w-a%w)*f,y:Math.floor(a/w)+(Math.floor(b/w)-Math.floor(a/w))*f,carrying:back,acting:this.now>=j.arrive&&!back,...(r?{riding:back?this.now>j.done-r:this.now<j.start+r}:{})};
  });}
  bridgeChoices(){return R.bridgeChoices(this.state);}
  check(action){return R.check(this.state,action);}
  sort(){this.steps.sort(before);}
  // the route a courier took when it left after the first k cells of this.opened had opened: routeTo on that board (the
  // board now with the cells opened since closed again, as they were: a closed cell never changes)
  routeAt(k,cell){const s=this.state,b=s.board.slice();for(let i=k;i<this.opened.length;i++)b[this.opened[i]]=s.meta.board[this.opened[i]];return R.routeTo({board:b,meta:s.meta,_reach:null},cell);}
  // a new step at its place in the sorted list (after every step that comes before it)
  insert(e){const st=this.steps;let lo=0,hi=st.length;if(hi&&before(st[hi-1],e)<0){st.push(e);return;}while(lo<hi){const m=(lo+hi)>>1;if(before(st[m],e)<0)lo=m+1;else hi=m;}st.splice(lo,0,e);}
  updateStatus(){
   const s=this.state;
   if(this.pending){s.status='Playing';return;}
   if(!s.board.some(c=>c>=0)){s.status='Won';return;}
   if(R.bridgeChoices(s).length){s.status='Playing';return;}
   if(R.channelChoices(s).length){s.status='Playing';return;}
   if(!s.slots.includes(-1)){s.status='Jammed';return;}
   s.status=R.topPods(s).some(p=>R.canTap(s,p)===null)?'Playing':'Stuck';
  }
  dispatch(){
   const s=this.state,sl=s.slots,rem=s.remaining,active=[];
   for(let i=0;i<sl.length;i++){const p=sl[i];if(p>=0&&rem[p]>0)active.push(p);}
   if(active.length>1)active.sort((a,b)=>s.ages[a]-s.ages[b]);
   if(active.length){
    const gap=tailOf(s)?TAIL.departureGap:PROFILE.departureGap,colours=s.meta.colours;let byColour=null,lists=null;
    for(const pod of active){
     // a pod that cannot leave yet and already has its check step pending changes nothing (the reference's filter runs
     // for nothing there)
     if(this.departReady[pod]>this.now+1e-7&&this.checking[pod])continue;
     // the frontier's free cells of the pod's colour; a later pod of that colour gets what the earlier ones left
     if(!lists){lists=new Map();byColour=R.targetsByColour(s);}
     const col=colours[pod];let li=lists.get(col);
     if(!li){const cells=[],all=byColour[col];if(all)for(let k=0;k<all.length;k++){const c=all[k];if(this.reserved[c]<0)cells.push(c);}li={cells,k:0};lists.set(col,li);}
     if(li.k>=li.cells.length)continue;
     while(rem[pod]>0&&li.k<li.cells.length&&this.departReady[pod]<=this.now+1e-7){
      // the route's length now; the route itself (routeTo on this board) only when someone reads it (couriers, snapshot)
      // on a channel level the route now (its source and its ride, rules.cjs rideOf, depend on the choice of this moment)
      const cell=li.cells[li.k++],route=s.meta.channel?R.routeTo(s,cell):null,ride=route?R.rideOf(s,route):0,length=route?route.length:R.routeLength(s,cell),id=++this.serial,distance=length-1,start=this.now,arrive=start+ride+Math.max(PROFILE.minOut,distance*PROFILE.outPerCell),clear=arrive+PROFILE.act,done=clear+Math.max(PROFILE.minReturn,distance*PROFILE.returnPerCell)+ride;
      const j=new Job(id,pod,cell,start,arrive,clear,done,this,this.opened.length,li.cells.length-li.k+1,route,ride);
      this.jobs.set(id,j);this.reserved[cell]=id;this.inFlight[pod]++;rem[pod]--;this.departReady[pod]=start+gap;
      const plan=[id,pod,cell,start,start,arrive,clear,done,length,j.frontier];this.lastPlans.push(plan);this.lastAssignments.push([id,pod,cell,start]);
      this.lastEvents.push({type:'assign',at:start,id},{type:'depart',at:start,id,plan});
      this.insert({type:'arrive',at:arrive,id});this.insert({type:'clear',at:clear,id});this.insert({type:'return',at:done,id});
     }
     if(rem[pod]>0&&li.k<li.cells.length&&!this.checking[pod]){this.checking[pod]=1;this.insert({type:'check',at:this.departReady[pod],pod});}
    }
   }
   let seen=null;for(let i=0;i<sl.length;i++){const pod=sl[i];if(pod<0)continue;const group=s.meta.groups[pod],first=group[0];
    if(group.length>1){if(!seen)seen=new Set();if(seen.has(first))continue;seen.add(first);}
    if(!this.releasing[first]&&(group.length===1?rem[pod]===0:group.every(p=>rem[p]===0))){this.releasing[first]=1;this.insert({type:'release',at:this.now+PROFILE.releaseLag,pod:first});}
   }
   this.updateStatus();
  }
  commit(action){
   const reason=this.check(action);if(reason)return {accepted:false,reason,events:[]};this.clearLogs();const s=this.state,events=[];
   if(action.type==='tap'&&s.meta.groups[action.pod].length>1)R.compactSlots(s);
   if(action.type==='tap')for(const pod of s.meta.groups[action.pod]){
    const column=s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===pod),slot=s.slots.indexOf(-1);
    s.heads[column]++;s.slots[slot]=pod;s.ages[pod]=++s.serial;events.push({type:'commit',pod,slot});if(!s.revealed[pod]){s.revealed[pod]=1;events.push({type:'reveal',pod,mode:'sealed',color:s.level.pods[pod].color});}
   }else if(action.type==='channel'){s.channel=action.exit;s._reach=null;events.push({type:'channelOpen',exit:action.exit});R.revealCells(s,events,this.now);}
   else{const c=s.level.hero.choices[action.choice];R.openCell(s,c.cell);this.opened.push(c.cell);s.bridge=action.choice;events.push({type:'bridge',cell:c.cell});R.revealCells(s,events,this.now);}
   // latch + key (17 §8): a latch the tap brought to the top, or whose key the bridge or the channel reached, opens now
   R.openLatches(s,events,this.now);
   // water channel (17 §8): a bridge that reached the spring makes the channel ready now
   R.readyChannel(s,events,this.now);
   for(const p of R.topPods(s))if(!s.revealed[p]&&s.level.pods[p].mystery!=='sealed'){s.revealed[p]=1;events.push({type:'reveal',pod:p,mode:'depth',color:s.level.pods[p].color});}
   this.dispatch();if(ASSERT)this.assert();return {accepted:true,events};
  }
  advance(milliseconds,speed=1){
   if(!Number.isFinite(milliseconds)||milliseconds<0||![1,2,TAIL.speed].includes(speed))throw Error('Invalid time');this.clearLogs();if(!this.pending)return false;
   const end=this.now+milliseconds*speed;let changed=false;
   while(this.steps.length&&this.steps[0].at<=end+1e-7){
    this.now=this.steps[0].at;const due=[];while(this.steps.length&&this.steps[0].at<=this.now)due.push(this.steps.shift());
    for(const e of due){
     if(e.type==='check'){this.checking[e.pod]=0;continue;}
     if(e.type==='release'){this.releasing[e.pod]=0;
      const group=this.state.meta.groups[e.pod];if(group.some(p=>this.state.remaining[p]!==0))throw Error('Early pair release');
      for(const pod of group){const slot=this.state.slots.indexOf(pod);if(slot<0)throw Error('Missing released occupant');this.state.slots[slot]=-1;this.lastEvents.push({type:'complete',at:this.now,pod});}changed=true;continue;
     }
     const j=this.jobs.get(e.id);if(!j)throw Error('Event without courier');this.lastEvents.push(e);
     if(e.type==='clear'){
      if(this.reserved[j.cell]!==j.id||this.state.board[j.cell]!==this.state.meta.colours[j.pod])throw Error('Invalid reservation');
      this.reserved[j.cell]=-1;R.openCell(this.state,j.cell);this.opened.push(j.cell);this.processedCells++;this.lastClears.push(j.cell);changed=true;
      // shutters (17 §8): the ones whose count this clear reached open at its time, before its cell reveals
      R.openShutters(this.state,this.lastEvents,this.now);
      // hidden cells (17 §8): the cells this clear reveals, right after it, at its time (rules.cjs revealCells)
      R.revealCells(this.state,this.lastEvents,this.now);
      // latch + key (17 §8): a latch on top whose key this clear reached opens at its time; the pods it exposes reveal by
      // the depth rule at the same time
      if(R.openLatches(this.state,this.lastEvents,this.now))for(const p of R.topPods(this.state))if(!this.state.revealed[p]&&this.state.level.pods[p].mystery!=='sealed'){this.state.revealed[p]=1;this.lastEvents.push({type:'reveal',at:this.now,pod:p,mode:'depth',color:this.state.level.pods[p].color});}
      // water channel (17 §8): the clear that reached the spring makes the channel ready at its time
      R.readyChannel(this.state,this.lastEvents,this.now);
     }
     if(e.type==='return'){this.inFlight[j.pod]--;this.jobs.delete(j.id);}
    }
    const serial=this.serial;this.dispatch();if(serial!==this.serial)changed=true;
   }
   if(this.pending)this.now=Math.max(end,this.now);if(this.lastClears.length>32)this.lastClears=this.lastClears.slice(-32);this.updateStatus();if(ASSERT)this.assert();return changed;
  }
  snapshot(){const state={serial:this.state.serial,bridge:this.state.bridge,status:this.state.status,...(this.state.meta.channel?{channel:this.state.channel}:{})};for(const k of fields)state[k]=Array.from(this.state[k]);return {profile:JSON.stringify({...PROFILE,tail:TAIL}),definition:JSON.stringify(this.state.level),state,steps:this.steps.map(e=>({...e})),now:this.now,serial:this.serial,processedCells:this.processedCells,jobs:[...this.jobs.values()].map(j=>j instanceof Job?jobRecord(j):{...j,route:j.route.slice()}),reserved:Array.from(this.reserved),inFlight:Array.from(this.inFlight),departReady:Array.from(this.departReady)};}
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
