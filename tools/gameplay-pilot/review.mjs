// Replay an exported human playtest without changing the pilot or its catalog.
import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const argument=(name,fallback)=>process.argv.find(a=>a.startsWith(`--${name}=`))?.slice(name.length+3)||fallback;
const input=argument('results');
if(!input)throw new Error('Pass --results=/path/to/export.json');
const OUT=resolve(argument('out','/tmp/bloomlings-pilot-review'));
const TRIALS=Number(argument('trials','64'));
assert(Number.isInteger(TRIALS)&&TRIALS>0&&TRIALS<=1000);
const raw=await readFile(input),results=JSON.parse(raw);
const dataset=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v1/levels.json'),'utf8'));
const oldQuality=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v1/quality.json'),'utf8'));
const sha=value=>createHash('sha256').update(value).digest('hex');
assert.equal(results.format,'bloomlings-pilot-results-1');
assert.equal(results.catalogHash,dataset.hash,'Export belongs to a different catalog');
assert.equal(results.rulesHash,sha(await readFile(resolve(ROOT,'tools/gameplay-pilot/rules.cjs'))),'Rules do not match export');
assert.equal(dataset.hash,sha(JSON.stringify(dataset.levels)));
const levelById=new Map(dataset.levels.map(l=>[l.id,l]));
const rng=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const occupied=state=>Array.from(state.slots).filter(p=>p>=0).length;
const work=state=>state.remaining.reduce((sum,n)=>sum+n,0);
const median=values=>{const sorted=values.slice().sort((a,b)=>a-b),n=sorted.length;return n%2?sorted[(n-1)/2]:(sorted[n/2-1]+sorted[n/2])/2;};
const publicPods=state=>Array.from(state.slots).filter(p=>p>=0).map(p=>({pod:p,color:state.level.pods[p].color,remaining:state.remaining[p]}));

function replay(record){
  const level=levelById.get(record.level);assert(level);
  let state=R.create(level);const trace=[];
  for(const [i,action] of record.commands.entries()){
    const next=R.step(state,action);assert(next.accepted,`Invalid action ${i+1} in attempt ${record.id}`);R.assertAccounting(next.state);
    const matching=new Set(R.reach(state).targets.map(c=>state.board[c]));
    const row={turn:i+1,action,occupiedBefore:occupied(state),occupiedAfter:occupied(next.state),cleared:work(state)-work(next.state),slotsBefore:publicPods(state),slotsAfter:publicPods(next.state),remaining:work(next.state),bridgeReadyBefore:R.bridgeChoices(state).map(c=>c.choice),bridgeReadyAfter:R.bridgeChoices(next.state).map(c=>c.choice)};
    if(action.type==='tap'){
      row.pod={...level.pods[action.pod],id:action.pod};
      row.column=level.tray.findIndex((stack,c)=>stack[state.heads[c]]===action.pod);
      row.matchingNow=matching.has(state.meta.colours[action.pod]);
      row.sameColourTops=R.topPods(state).filter(p=>state.meta.colours[p]===state.meta.colours[action.pod]).length;
    }
    trace.push(row);state=next.state;
  }
  const expected=record.outcome==='abandoned'?'Playing':record.outcome[0].toUpperCase()+record.outcome.slice(1);
  assert.equal(state.status,expected,`Outcome mismatch for attempt ${record.id}`);
  return {id:record.id,level:record.level,outcome:record.outcome,rating:record.rating,art:record.art,note:record.note||'',elapsedMs:record.elapsedMs,moves:trace.length,taps:record.commands.filter(c=>c.type==='tap').length,peakHeld:Math.max(0,...trace.map(t=>t.occupiedAfter)),decisionsWithOneFree:trace.filter(t=>t.occupiedBefore===4).length,decisionsWithTwoFree:trace.filter(t=>t.occupiedBefore===3).length,turnsWithoutClearing:trace.filter(t=>t.cleared===0).length,trace};
}

function solveFrom(start,budget=30000){
  const memo=new Map();let nodes=0;
  function visit(state){
    if(state.status==='Won')return true;
    if(state.status!=='Playing')return false;
    const key=R.signature(state);if(memo.has(key))return memo.get(key);
    if(++nodes>budget)return null;
    const options=R.legal(state).map(action=>({action,state:R.step(state,action).state}));
    options.sort((a,b)=>occupied(a.state)-occupied(b.state)||work(a.state)-work(b.state));
    let unknown=false;
    for(const option of options){const answer=visit(option.state);if(answer===true){memo.set(key,true);return true;}if(answer===null)unknown=true;}
    const answer=unknown?null:false;memo.set(key,answer);return answer;
  }
  return {winning:visit(start),nodes};
}

function reviewFailure(record){
  let state=R.create(levelById.get(record.level));const stages=[];
  for(const [i,action] of record.commands.entries()){
    const after=R.step(state,action).state,proof=solveFrom(after);
    stages.push({turn:i+1,winningAfter:proof.winning,nodes:proof.nodes});
    if(proof.winning===false){
      const alternatives=R.legal(state).map(candidate=>{
        const child=R.step(state,candidate).state,answer=solveFrom(child);
        return {action:candidate,pod:candidate.type==='tap'?state.level.pods[candidate.pod]:null,cleared:work(state)-work(child),occupiedAfter:occupied(child),winning:answer.winning,nodes:answer.nodes};
      });
      return {attempt:record.id,level:record.level,firstProvenFatalTurn:i+1,precedingStates:stages,chosen:action,alternatives,slotsBefore:publicPods(state),slotsAfter:publicPods(after)};
    }
    state=after;
  }
  return {attempt:record.id,level:record.level,precedingStates:stages};
}

const MODES=['match-visible','leftmost-visible','smallest-visible','largest-visible','prefer-release','two-visible-then-random'];
const descriptions={
  'match-visible':'Random among exposed capsules with a colour on the currently reachable frontier; otherwise random. Build a ready bridge immediately.',
  'leftmost-visible':'Take the leftmost exposed capsule whose colour is reachable now; otherwise the leftmost capsule. Build the leftmost ready bridge.',
  'smallest-visible':'Choose the smallest exposed capsule of a reachable colour; random ties. No future search. Build a ready bridge immediately.',
  'largest-visible':'Choose the largest exposed capsule of a reachable colour, leftmost on ties; otherwise largest exposed capsule. Build the leftmost ready bridge. Deterministic, no future search.',
  'prefer-release':'Evaluate only the current automatic settle for each action. Prefer a win, avoid an immediate jam, then prefer more free slots and more cells cleared. No future search.',
  'two-visible-then-random':'For the first two capsule taps choose a reachable colour; then random legal taps. Build a ready bridge immediately.'
};
function choose(state,mode,random,taps){
  const legal=R.legal(state),bridges=legal.filter(a=>a.type==='bridge');
  if(bridges.length)return ['leftmost-visible','largest-visible'].includes(mode)?bridges[0]:bridges[Math.floor(random()*bridges.length)];
  const visible=new Set(R.reach(state).targets.map(i=>state.board[i]));
  let options=legal;
  if(mode==='prefer-release'){
    const scored=legal.map(action=>{const child=R.step(state,action).state;return {action,score:(child.status==='Won'?100000:0)-(child.status==='Jammed'?100000:0)+(5-occupied(child))*1000+work(state)-work(child)};});
    const best=Math.max(...scored.map(s=>s.score));options=scored.filter(s=>s.score===best).map(s=>s.action);
  }else if(mode!=='two-visible-then-random'||taps<2){
    const matches=legal.filter(a=>visible.has(state.meta.colours[a.pod]));if(matches.length)options=matches;
    if(mode==='leftmost-visible')return options[0];
    if(mode==='smallest-visible'){const count=Math.min(...options.map(a=>state.remaining[a.pod]));options=options.filter(a=>state.remaining[a.pod]===count);}
    if(mode==='largest-visible'){const count=Math.max(...options.map(a=>state.remaining[a.pod]));return options.find(a=>state.remaining[a.pod]===count);}
  }
  return options[Math.floor(random()*options.length)];
}
function simulate(level,mode,modeIndex){
  const trials=['leftmost-visible','largest-visible'].includes(mode)?1:TRIALS;let wins=0;
  for(let trial=0;trial<trials;trial++){
    const random=rng(level.id*3217+trial*9157+modeIndex*717);let state=R.create(level),taps=0;
    for(let guard=0;state.status==='Playing';guard++){
      assert(guard<=level.pods.length+2);const action=choose(state,mode,random,taps),result=R.step(state,action);assert(result.accepted);state=result.state;if(action.type==='tap')taps++;
    }
    if(state.status==='Won')wins++;
  }
  return {wins,trials,rate:wins/trials};
}

const attempts=results.attempts.map(replay),wins=attempts.filter(a=>a.outcome==='won'),jams=attempts.filter(a=>a.outcome==='jammed');
const feedbackByLevel=new Map(wins.map(a=>[a.level,a]));
assert.equal(wins.length,30);assert.equal(feedbackByLevel.size,30);
const summary={attempts:attempts.length,completedLevels:feedbackByLevel.size,abandoned:attempts.filter(a=>a.outcome==='abandoned').length,jammed:jams.length,firstCompletedAttemptWins:dataset.levels.filter(l=>attempts.find(a=>a.level===l.id&&a.outcome!=='abandoned')?.outcome==='won').length,ratings:{easy:wins.filter(a=>a.rating==='easy').length,good:wins.filter(a=>a.rating==='good').length,hard:wins.filter(a=>a.rating==='hard').length},recognition:{yes:wins.filter(a=>a.art==='yes').length,no:wins.filter(a=>a.art==='no').length},medianWinningElapsedSeconds:median(wins.map(a=>a.elapsedMs/1000)),totalReportedActiveSeconds:attempts.reduce((n,a)=>n+a.elapsedMs/1000,0),winningLevelsAtMostTwoHeld:wins.filter(a=>a.peakHeld<=2).length,winningLevelsReachingFourHeld:wins.filter(a=>a.peakHeld>=4).map(a=>a.level),winningDecisionsWithOneFree:wins.reduce((n,a)=>n+a.decisionsWithOneFree,0),winningDecisionsWithTwoFree:wins.reduce((n,a)=>n+a.decisionsWithTwoFree,0),winningMoves:wins.reduce((n,a)=>n+a.moves,0)};
console.log('Human replay:',JSON.stringify(summary));
const failures=results.attempts.filter(a=>a.outcome==='jammed').map(reviewFailure);
console.log('Failure review:',JSON.stringify(failures));
const bridgeChoices=[];
for(const record of results.attempts.filter(a=>a.outcome==='won'&&levelById.get(a.level).hero?.choices.length>1)){
  let state=R.create(levelById.get(record.level));
  for(const [i,action] of record.commands.entries()){
    if(action.type==='bridge'){
      const alternatives=R.bridgeChoices(state).map(b=>{
        let child=R.step(state,{type:'bridge',choice:b.choice}).state,refused=false;
        const proof=solveFrom(child);
        for(const next of record.commands.slice(i+1)){const move=R.step(child,next);if(!move.accepted){refused=true;break;}child=move.state;}
        return {choice:b.choice,winningContinuationExists:proof.winning,sameTapOrderWins:!refused&&child.status==='Won'};
      });
      bridgeChoices.push({level:record.level,turn:i+1,selected:action.choice,alternatives});
    }
    state=R.step(state,action).state;
  }
}
console.log('Bridge choice check:',JSON.stringify(bridgeChoices));
const policies=[];
for(const level of dataset.levels){
  const row={level:level.id,difficulty:level.difficulty,oldRandom:oldQuality.levels.find(l=>l.id===level.id).random,oldVisible:oldQuality.levels.find(l=>l.id===level.id).visible,policies:Object.fromEntries(MODES.map((mode,i)=>[mode,simulate(level,mode,i)]))};
  policies.push(row);
  if([16,23,26,29].includes(level.id))console.log('Policies:',JSON.stringify(row));
}
const report={format:'bloomlings-pilot-human-review-1',pilotVersion:dataset.version,catalogHash:dataset.hash,rulesHash:results.rulesHash,sourceHash:sha(raw),participants:1,summary,policyDescriptions:descriptions,policies,failures,bridgeChoices,attempts,limits:['Single genre-experienced participant; not a population win-rate study.','Elapsed time is the HTML client reported active time, not pure thinking time or production game duration.','Occupancy is measured after automatic settle; immediate temporary allocation is not sustained pressure.','Policies use only information exposed in this fully visible pilot, but are not exact models of this participant.','Solver null means budget exhausted, never a proven loss.']};
await mkdir(OUT,{recursive:true});
await writeFile(resolve(OUT,'results.json'),raw);
await writeFile(resolve(OUT,'review.json'),JSON.stringify(report,null,2)+'\n');
console.log(`Saved reproducible review to ${OUT}`);
