import R from './rules.cjs';
export const held=s=>s.slots.reduce((n,p)=>n+(p>=0),0);
export const work=s=>s.remaining.reduce((n,v)=>n+v,0);

// A pending completed pair member still reserves capacity. Include pod identity
// and group lifetime in search keys; co-reservation prevents the v1 quotient.
function key(s){
 const active=Array.from(s.slots).filter(p=>p>=0).sort((a,b)=>s.ages[a]-s.ages[b]);
 const counters=active.map(p=>p+':'+s.remaining[p]).join(',');
 return String.fromCharCode(...s.board)+'|'+s.heads.join(',')+'|'+counters+'|'+s.revealed.join('')+'|'+s.bridge;
}
export function solve(start,{budget=10000,heldLimit=5,requireBothBridges=false}={}){
 const memo=new Map(),choice=new Map();let nodes=0;
 function visit(s){
  if(held(s)>heldLimit)return false;
  if(s.status==='Won')return true;
  if(s.status!=='Playing')return false;
  const k=key(s);if(memo.has(k))return memo.get(k);
  if(++nodes>budget)return null;
  const moves=R.legal(s).filter(a=>!(requireBothBridges&&a.type==='bridge'&&s.level.hero.choices.length>1&&R.bridgeChoices(s).length<2)).map(action=>({action,state:R.step(s,action).state}));
  moves.sort((a,b)=>(b.state.status==='Won')-(a.state.status==='Won')||held(a.state)-held(b.state)||work(a.state)-work(b.state));
  let incomplete=false;
  for(const child of moves){const answer=visit(child.state);if(answer===true){memo.set(k,true);choice.set(k,child.action);return true;}if(answer===null)incomplete=true;}
  const answer=incomplete?null:false;memo.set(k,answer);return answer;
 }
 const winning=visit(start),actions=[];let peak=held(start),waitingTurns=0;
 if(winning===true){let s=start;while(s.status!=='Won'){const action=choice.get(key(s));if(!action)throw new Error('Missing witness');actions.push(action);s=R.step(s,action).state;peak=Math.max(peak,held(s));if(held(s))waitingTurns++;}}
 return {winning,actions,nodes,peak,waitingTurns,complete:winning!==null};
}

export function hiddenWorlds(level){
 const hidden=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
 if(!hidden.length)return [level];
 if(hidden.length!==2||level.pods[hidden[0]].count!==level.pods[hidden[1]].count)throw new Error('Unsupported uncertainty domain');
 const other={...level,pods:level.pods.map(p=>({...p}))};
 [other.pods[hidden[0]].color,other.pods[hidden[1]].color]=[other.pods[hidden[1]].color,other.pods[hidden[0]].color];
 return [level,other];
}

// AND/OR information policy. Before reveal it must choose the same action in
// every indistinguishable world. After reveal observations branch. Equal-count
// hidden pods plus exact public work totals give exactly these two assignments.
export function fairSolve(level,{budget=10000}={}){
 const worlds=hiddenWorlds(level),memo=new Map(),policy=new Map();let nodes=0;
 const initial=worlds.map((l,world)=>({world,state:R.create(l)}));
 function visit(belief){
  if(belief.every(b=>b.state.status==='Won'))return true;
  if(belief.some(b=>b.state.status!=='Playing'))return false;
  const k=belief.map(b=>b.world+':'+R.signature(b.state)).join('~');if(memo.has(k))return memo.get(k);
  if(++nodes>budget)return null;
  if(belief.length===1){
   const answer=solve(belief[0].state,{budget:Math.max(100,budget-nodes)});nodes+=answer.nodes;
   if(answer.winning){let s=belief[0].state;for(const action of answer.actions){policy.set(R.observation(s),action);s=R.step(s,action).state;}}
   memo.set(k,answer.winning);return answer.winning;
  }
  const base=belief[0].state,all=belief.map(b=>new Set(R.legal(b.state).map(a=>JSON.stringify(a))));
  const moves=R.legal(base).filter(a=>all.every(set=>set.has(JSON.stringify(a))));
  let incomplete=false;
  for(const action of moves){
   const branches=new Map();
   for(const b of belief){const child={world:b.world,state:R.step(b.state,action).state},obs=R.observation(child.state);if(!branches.has(obs))branches.set(obs,[]);branches.get(obs).push(child);}
   let valid=true,unknown=false;
   for(const branch of branches.values()){const answer=visit(branch);if(answer===false){valid=false;break;}if(answer===null)unknown=true;}
   if(valid&&!unknown){memo.set(k,true);policy.set(R.observation(base),action);return true;}
   if(unknown)incomplete=true;
  }
  const answer=incomplete?null:false;memo.set(k,answer);return answer;
 }
 // Top colours may already distinguish the worlds before the first action.
 const observations=new Map();for(const b of initial){const k=R.observation(b.state);if(!observations.has(k))observations.set(k,[]);observations.get(k).push(b);}
 let winning=true;for(const belief of observations.values()){const answer=visit(belief);if(answer===false){winning=false;break;}if(answer===null)winning=null;}
 const routes=[];
 if(winning===true)for(const l of worlds){let s=R.create(l);const actions=[];while(s.status!=='Won'){const action=policy.get(R.observation(s));if(!action)throw new Error('Incomplete information policy');actions.push(action);s=R.step(s,action).state;}routes.push(actions);}
 return {winning,routes,nodes,complete:winning!==null,worlds:worlds.length};
}

export function decisionProofs(level,actions,{wanted=2,gap=3,budget=12000}={}){
 const proofs=[];let s=R.create(level),last=-gap;
 for(let at=0;at<actions.length;at++){
  const correct=actions[at],good=R.step(s,correct).state,goodWork=work(s)-work(good);
  if(at-last>=gap&&goodWork>0){
   const alternatives=R.legal(s).filter(a=>JSON.stringify(a)!==JSON.stringify(correct));
   for(const wrong of alternatives){
    const bad=R.step(s,wrong).state,cleared=work(s)-work(bad);
    if(bad.status!=='Playing'||cleared<=0)continue; // exclude immediately obvious jams
    const proof=solve(bad,{budget});
    if(proof.winning===false){
     const colours=a=>a.type==='tap'?s.meta.groups[a.pod].filter(p=>s.revealed[p]).map(p=>level.pods[p].color):[];
     proofs.push({at,prefix:actions.slice(0,at),continuation:actions.slice(at),correct,wrong,goodClears:goodWork,badClears:cleared,sameColourChoice:colours(correct).some(c=>colours(wrong).includes(c)),proofNodes:proof.nodes});last=at;break;
    }
   }
  }
  s=good;if(proofs.length>=wanted)break;
 }
 return proofs;
}

export function bridgeProof(level,actions){
 if(!level.hero||level.hero.choices.length<2)return null;
 let s=R.create(level);
 for(let at=0;at<actions.length;at++){
  const action=actions[at];
  if(action.type==='bridge'){
   const ready=R.bridgeChoices(s),other=ready.find(b=>b.choice!==action.choice);
   if(!other)return null;
   const alternate={type:'bridge',choice:other.choice};let changed=R.step(s,alternate).state,refused=false;
   for(const move of actions.slice(at+1)){const next=R.step(changed,move);if(!next.accepted){refused=true;break;}changed=next.state;}
   const search=solve(R.step(s,alternate).state,{budget:16000});
   return {at,selected:action.choice,alternative:other.choice,bothReady:true,sameOrderWins:!refused&&changed.status==='Won',alternateWinning:search.winning,alternateActions:search.actions,proofNodes:search.nodes};
  }
  s=R.step(s,action).state;
 }
 return null;
}
