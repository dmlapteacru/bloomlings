import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import Rules from './rules.cjs';
import { makeLevelArt } from './art.mjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const OUT=resolve(ROOT,process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'content/pilot-v1');
const FROM=Number(process.argv.find(a=>a.startsWith('--from='))?.slice(7)||1);
const TO=Number(process.argv.find(a=>a.startsWith('--to='))?.slice(5)||30);
const HARD=new Set([16,23,29]);
const VERSION='pilot-30-v1';
const rng=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const pick=(a,r)=>a[Math.floor(r()*a.length)];
const sha=x=>createHash('sha256').update(x).digest('hex');

function closure(state,color){
  const seen=new Uint8Array(state.board.length),q=new Int16Array(state.board.length),cells=[];
  let head=0,tail=0;
  for(const i of state.meta.entries)if(state.board[i]===Rules.OPEN||state.board[i]===color){q[tail++]=i;seen[i]=1;}
  while(head<tail){const i=q[head++];if(state.board[i]===color)cells.push(i);for(let d=0;d<4;d++){const j=state.meta.neighbours[i*4+d];if(j>=0&&!seen[j]&&(state.board[j]===Rules.OPEN||state.board[j]===color)){seen[j]=1;q[tail++]=j;}}}
  return cells;
}

function painting(art,r){
  const state=Rules.create({...art,pods:[],tray:[[],[]]}),sequence=[];
  for(let guard=0;state.board.some(c=>c>=0);guard++){
    if(guard>150)throw new Error('Painting did not finish.');
    const options=Array.from({length:6},(_,c)=>({c,cells:closure(state,c)})).filter(x=>x.cells.length);
    const bridges=Rules.bridgeChoices(state);
    if(bridges.length&&(!options.length||r()<.35)){
      const b=pick(bridges,r);state.board[b.cell]=Rules.OPEN;state.bridge=b.choice;state._reach=null;sequence.push({bridge:b.choice});continue;
    }
    if(!options.length)throw new Error(`L${art.id}: unreachable picture`);
    const chosen=r()<.55?options.sort((a,b)=>b.cells.length-a.cells.length)[0]:pick(options,r);
    sequence.push({color:Rules.CHANNELS[chosen.c],count:chosen.cells.length});
    for(const cell of chosen.cells)state.board[cell]=Rules.OPEN;state._reach=null;
  }
  return sequence;
}

function definition(art,sequence,r){
  const pods=[],actions=[];
  for(const item of sequence){if(item.bridge!==undefined)actions.push({type:'bridge',choice:item.bridge});else{actions.push({type:'tap',pod:pods.length});pods.push({color:item.color,count:item.count});}}
  const columns=art.id<4?2:art.id<7?3:4, tray=Array.from({length:columns},()=>[]);
  const depth=Math.ceil(pods.length/columns)+1;
  for(let p=0;p<pods.length;p++){
    const eligible=tray.map((s,c)=>({s,c})).filter(x=>x.s.length<depth);
    const chosen=pick(eligible,r);chosen.s.push(p);
  }
  return {level:{...art,pods,tray,difficulty:HARD.has(art.id)?'Hard':'Normal'},actions};
}

function replay(level,actions){
  let state=Rules.create(level),peak=0,waiting=0;
  for(const action of actions){const result=Rules.step(state,action);if(!result.accepted)return null;state=result.state;const occupied=state.slots.filter(p=>p>=0).length;peak=Math.max(peak,occupied);if(occupied)waiting++;}
  return state.status==='Won'?{state,peak,waiting}:null;
}

function sculpt(art,base,r){
  let sequence=base.map(x=>({...x}));
  const desired=art.id<4?art.id+2:art.id<7?7:art.id<11?10:art.id<16?14:art.id<20?17:art.id<25?19:20;
  // Split a few big work batches, rather than force every batch into ten clears.
  while(sequence.filter(s=>s.color).length<desired){
    const options=sequence.map((s,i)=>({s,i})).filter(x=>x.s.count>=12);
    if(!options.length)break;
    const chosen=options.sort((a,b)=>b.s.count-a.s.count)[0];
    const first=Math.max(3,Math.floor(chosen.s.count*(.32+r()*.36)));
    sequence.splice(chosen.i,1,{...chosen.s,count:first},{...chosen.s,count:chosen.s.count-first});
  }
  // Move later work into an earlier capsule while preserving every colour total.
  // The reference route must still win; overcommitted capsules now have a slot cost.
  const mutations=art.id<4?0:art.id<7?2:art.id<11?4:art.id<16?7:10;
  for(let k=0;k<mutations;k++){
    const candidates=[];
    for(let i=0;i<sequence.length;i++)for(let j=i+2;j<sequence.length;j++)if(sequence[i].color&&sequence[i].color===sequence[j].color&&sequence[j].count>3)candidates.push([i,j]);
    if(!candidates.length)break;
    const [i,j]=pick(candidates,r),next=sequence.map(x=>({...x}));
    const amount=Math.max(1,Math.floor(next[j].count*(.2+r()*.65)));
    next[i].count+=amount;next[j].count-=amount;
    const test=definition(art,next,r);
    if(replay(test.level,test.actions))sequence=next;
  }
  // Counts belong to an entire colour's work, not to one locally accessible
  // patch. Repartition same-colour capsules across those patches, retaining a
  // winning route. This creates multiple genuine reservations instead of just
  // splitting an always-reachable region into tiny, immediately-finished pods.
  if(art.id>=7)for(let trial=0;trial<5;trial++){
    const next=sequence.map(x=>({...x}));
    for(const color of Rules.CHANNELS){
      const indices=next.map((s,i)=>s.color===color?i:-1).filter(i=>i>=0);
      if(indices.length<2)continue;
      const total=indices.reduce((n,i)=>n+next[i].count,0),weights=indices.map(()=>HARD.has(art.id)?.12+r()**2*4:.5+r());
      let remaining=total;const sum=weights.reduce((a,b)=>a+b,0);
      indices.forEach((i,k)=>{const count=k===indices.length-1?remaining:Math.max(1,Math.min(remaining-(indices.length-k-1),Math.floor(total*weights[k]/sum)));next[i].count=count;remaining-=count;});
    }
    const test=definition(art,next,r),candidate=replay(test.level,test.actions);
    const before=definition(art,sequence,r),baseline=replay(before.level,before.actions);
    if(candidate&&candidate.waiting>=baseline.waiting)sequence=next;
  }
  return sequence;
}

function selectMove(state,mode,r,turn){
  const moves=Rules.legal(state);if(!moves.length)return null;
  const bridge=moves.filter(m=>m.type==='bridge');
  if(bridge.length&&mode!=='random')return pick(bridge,r);
  if(mode==='random')return pick(moves,r);
  const tops=moves.filter(m=>m.type==='tap');
  if(!tops.length)return pick(moves,r);
  if(mode==='left')return tops[0];
  if(mode==='right')return tops.at(-1);
  if(mode==='cycle')return tops[turn%tops.length];
  const counts=new Int16Array(6);for(const cell of Rules.reach(state).targets)counts[state.board[cell]]++;
  const scored=tops.map(m=>({m,score:Math.min(counts[state.meta.colours[m.pod]],state.remaining[m.pod])}));
  const best=Math.max(...scored.map(s=>s.score));
  return pick(scored.filter(s=>s.score===best),r).m;
}

function sample(level,mode,n,seed){
  let wins=0;const r=rng(seed);let losing=null;const winning=[];
  for(let trial=0;trial<n;trial++){
    let state=Rules.create(level);const actions=[];
    for(let turn=0;state.status==='Playing';turn++){
      if(turn>level.pods.length+2)throw new Error('Non-monotone trial.');
      const action=selectMove(state,mode,r,turn);if(!action)break;
      const next=Rules.step(state,action);if(!next.accepted)throw new Error('Illegal sampled move.');
      state=next.state;actions.push(action);
    }
    if(state.status==='Won'){wins++;if(winning.length<6)winning.push(actions);}else losing??=actions;
  }
  return {wins,trials:n,rate:wins/n,losing,winning};
}

function solve(start,budget=12000){
  const memo=new Map(),choices=new Map();let nodes=0,unknown=false;
  function visit(state){
    if(state.status==='Won')return true;
    if(state.status!=='Playing')return false;
    const key=Rules.signature(state);if(memo.has(key))return memo.get(key);
    if(++nodes>budget){unknown=true;return null;}
    const moves=Rules.legal(state);
    const counts=new Int16Array(6);for(const cell of Rules.reach(state).targets)counts[state.board[cell]]++;
    moves.sort((a,b)=>(b.type==='bridge'?1000:Math.min(counts[state.meta.colours[b.pod]],state.remaining[b.pod]))-(a.type==='bridge'?1000:Math.min(counts[state.meta.colours[a.pod]],state.remaining[a.pod])));
    let incomplete=false;
    for(const move of moves){const value=visit(Rules.step(state,move).state);if(value===true){memo.set(key,true);choices.set(key,move);return true;}if(value===null)incomplete=true;}
    const value=incomplete?null:false;memo.set(key,value);return value;
  }
  const winning=visit(start),actions=[];
  if(winning===true){let state=start;while(state.status!=='Won'){const move=choices.get(Rules.signature(state));if(!move)throw new Error('Missing solver witness.');actions.push(move);state=Rules.step(state,move).state;}}
  return {winning,nodes,complete:winning!==null,unknown,actions};
}

function decisions(level,paths){
  const found=[],seen=new Set();
  for(const actions of paths){
    let state=Rules.create(level);
    for(let at=0;at<actions.length;at++){
      if(found.length>=2)return found;
      const key=Rules.signature(state);
      if(!seen.has(key)){
        seen.add(key);
        const visible=new Set(Rules.reach(state).targets.map(c=>state.board[c]));
        const plausible=move=>{
          if(move.type==='bridge')return 'visible-bridge-choice';
          if(visible.has(state.meta.colours[move.pod]))return 'matching-now';
          const column=level.tray.findIndex((s,c)=>s[state.heads[c]]===move.pod);
          const free=state.slots.filter(p=>p<0).length;
          const behind=level.tray[column].slice(state.heads[column]+1,state.heads[column]+free);
          return behind.some(p=>visible.has(state.meta.colours[p]))?'prepares-visible-colour':null;
        };
        for(const wrong of Rules.legal(state).filter(m=>JSON.stringify(m)!==JSON.stringify(actions[at])&&plausible(m))){
          const child=Rules.step(state,wrong).state,proof=solve(child,6000);
          if(proof.winning===false&&proof.complete){found.push({at,prefix:actions.slice(0,at),continuation:actions.slice(at),correct:actions[at],wrong,kind:plausible(wrong),proofNodes:proof.nodes});break;}
        }
      }
      state=Rules.step(state,actions[at]).state;
    }
  }
  return found;
}

await mkdir(OUT,{recursive:true});
let levels=[],certificates=[],quality=[];
if(FROM>1){levels=JSON.parse(await readFile(resolve(OUT,'levels.json'),'utf8')).levels.filter(l=>l.id<FROM);certificates=JSON.parse(await readFile(resolve(OUT,'certificates.json'),'utf8')).filter(l=>l.id<FROM);quality=JSON.parse(await readFile(resolve(OUT,'quality.json'),'utf8')).levels.filter(l=>l.id<FROM);}
for(let id=FROM;id<=TO;id++){
  const art=makeLevelArt(id);let accepted=null;
  for(let attempt=0;attempt<(HARD.has(id)?300:100);attempt++){
    const r=rng(id*173219+attempt*9157), base=painting(art,r),sequence=sculpt(art,base,r);
    let {level,actions}=definition(art,sequence,r);
    if(HARD.has(id)){
      // Search the joint picture/tray problem. Merely preserving a safe global
      // paint order in each column left too many equivalent winning choices.
      const order=level.pods.map((_,i)=>i);
      for(let i=order.length-1;i>0;i--){const j=Math.floor(r()*(i+1));[order[i],order[j]]=[order[j],order[i]];}
      level.tray=Array.from({length:4},()=>[]);
      order.forEach((p,i)=>level.tray[i%4].push(p));
      const witness=solve(Rules.create(level),16000);
      if(witness.winning!==true)continue;
      actions=witness.actions;
    }
    const reference=replay(level,actions);
    if(!reference||Rules.validate(level).length)continue;
    const quick=sample(level,'random',24,id*331+attempt);
    if(HARD.has(id)&&attempt%20===0)console.log(`  candidate L${id}/${attempt}: random ${quick.rate.toFixed(2)}, peak ${reference.peak}, phases ${base.map(s=>s.color||'bridge').join(' ')}`);
    if(id<7&&quick.rate<.8)continue;
    if(id>=7&&id<11&&quick.rate<.6)continue;
    if(!HARD.has(id)&&id>=11&&quick.rate<.4)continue;
    if(HARD.has(id)&&quick.rate>.4)continue;
    const random=sample(level,'random',160,id*771+attempt),visible=sample(level,'visible',48,id*571+attempt);
    if(HARD.has(id)&&random.rate<=.4)console.log(`  full L${id}/${attempt}: random ${random.rate.toFixed(2)}, visible ${visible.rate.toFixed(2)}`);
    if(HARD.has(id)&&(random.rate>.3||visible.rate>.8))continue;
    if(!HARD.has(id)&&id>=11&&random.rate<.38)continue;
    const choices=HARD.has(id)?decisions(level,[actions,...random.winning,...visible.winning]):[];
    if(HARD.has(id))console.log(`  decisions L${id}/${attempt}: ${choices.length}`);
    if(HARD.has(id)&&choices.length<2)continue;
    const bots=Object.fromEntries(['left','right','cycle'].map(mode=>[mode,sample(level,mode,1,id*19)]));
    accepted={level,cert:{id,actions,losing:random.losing,decisionProofs:choices},metrics:{id,attempt,pods:level.pods.length,cells:level.w*level.h-(level.hero?level.w:0),palette:[...new Set(level.pods.map(p=>p.color))],referencePeak:reference.peak,referenceWaitingTurns:reference.waiting,random:{wins:random.wins,trials:random.trials,rate:random.rate},visible:{wins:visible.wins,trials:visible.trials,rate:visible.rate},bots:Object.fromEntries(Object.entries(bots).map(([k,v])=>[k,v.wins===1])),provenDecisions:choices.length}};
    break;
  }
  if(!accepted)throw new Error(`L${id}: no candidate met quality conditions; not publishing a fake difficulty label.`);
  levels.push(accepted.level);certificates.push(accepted.cert);quality.push(accepted.metrics);
  const hash=sha(JSON.stringify(levels));
  await writeFile(resolve(OUT,'levels.json'),JSON.stringify({version:VERSION,hash,levels},null,2)+'\n');
  await writeFile(resolve(OUT,'certificates.json'),JSON.stringify(certificates,null,2)+'\n');
  await writeFile(resolve(OUT,'quality.json'),JSON.stringify({version:VERSION,catalogHash:hash,complete:levels.length===30,limits:['Random and visible policy rates are deterministic simulation samples, not measured human win rates.','Decision proofs use exhaustive continuation only for the listed losing branches; the entire game tree is not claimed to be enumerated.'],levels:quality},null,2)+'\n');
  console.log(`L${id} ${accepted.level.difficulty}: ${accepted.metrics.pods} pods, random ${accepted.metrics.random.rate.toFixed(3)}, visible ${accepted.metrics.visible.rate.toFixed(3)}, peak ${accepted.metrics.referencePeak}, decisions ${accepted.metrics.provenDecisions}; attempt ${accepted.metrics.attempt}`);
}
console.log(`Saved ${levels.length} levels to ${OUT}`);
