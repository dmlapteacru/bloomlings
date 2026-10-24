// How the owner plays our pilot levels and Colony Flow's, on the same engine and with the same measures.
// Ours: the owner's exports of v8C (2026-10-22) and v8E (2026-10-24), replayed command by command on the live timeline of
// the exact build they played (git archive of the commit whose catalog hash matches the export). Theirs: the CF rounds
// of product/audits/colony-flow-rounds/levels.json (L20, L25, L26 with tap times), on the v8E live timeline.
// For every round: the patient player's view after each command (could the level still be won from here, with perfect
// play and waiting allowed: botsc.winnable on the logical state), the first command after which it could not (the fatal
// command), the alternatives at that moment, capsules that sat in a slot with nothing reachable ("parked"), and time.
// node tools/colony-deep-research/compare/rounds-analysis.mjs <v8C export> <v8E export>  → JSON on stdout
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {execSync} from 'node:child_process';
const root=path.resolve(path.dirname(new URL(import.meta.url).pathname),'../../..');
const [v8cExport,v8eExport]=process.argv.slice(2);
function build(commit,dir,patch){const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'ra-'));execSync(`git archive ${commit} ${dir} | tar -x -C ${tmp}`,{cwd:root});
 const d=path.join(tmp,dir);if(patch)patch(d);return d;}
async function engine(d){return {R:(await import(d+'/rules.cjs')).default,T:(await import(d+'/timeline.cjs')).default,bots:await import(d+'/botsc.mjs')};}
const BUDGET=+(process.env.RA_BUDGET||150000);
function measure(E,level,commands){// commands: [{t (model ms), action, real (ms)}]
 const {R,T,bots}=E,t=new T.Timeline(R.create(level)),colourOf=p=>R.CHANNELS.indexOf(level.pods[p].color);const colourReach=s=>new Set(R.reach(s).targets.map(i=>s.board[i]));
 const winState=s=>s.status==='Won'?true:s.status!=='Playing'?false:bots.winnable(s,BUDGET);
 // From a live moment the patient player waits for the work under way, then plays perfectly: settle a copy, then search.
 const settled=(tl,act)=>{const c=new T.Timeline(R.create(level),tl.snapshot());if(act){if(c.check(act))return null;c.commit(act);}c.advance(1e9);return c.state;};
 const winAt=(tl,act)=>{const s=settled(tl,act);return s?winState(s):null;};
 const steps=[];steps.push({k:0,win:winAt(t)});const parkedSince=new Map();let delays=0;
 for(let k=0;k<commands.length;k++){const c=commands[k];if(c.t>t.now)t.advance(c.t-t.now);
  let guard=0;while(t.check(c.action)&&guard++<600){t.advance(100);delays+=100;}
  const before=R.clone(t.state),snap=new T.Timeline(R.create(level),t.snapshot()),reach=colourReach(before),legal=R.legal(before).filter(a=>!t.check(a));
  const held=Array.from(before.slots).filter(p=>p>=0),idle=held.filter(p=>before.remaining[p]>0&&!reach.has(colourOf(p)));
  const pod=c.action.pod,podReach=pod>=0?reach.has(colourOf(pod)):null;
  const r=t.commit(c.action);if(!r.accepted)return {error:`refused at command ${k+1}: ${r.reason}`,steps};
  steps.push({k:k+1,t:c.t,real:c.real,pod,label:level.pods[pod]?.label||`${level.pods[pod]?.color}${level.pods[pod]?.count}`,count:level.pods[pod]?.count,freeBefore:before.slots.filter(p=>p<0).length,
   held:held.length,idleHeld:idle.length,parked:podReach===false,legal:legal.length,before,snap,win:winAt(t)});}
 t.advance(1e9);
 // fatal command: the first after which the level cannot be won any more
 const fatal=steps.findIndex((s,i)=>i>0&&s.win===false&&steps[i-1].win!==false);
 let alternatives=null;
 if(fatal>0){const pre=steps[fatal].snap,moves=R.legal(pre.state);
  alternatives=[{move:'wait (no tap)',win:winAt(pre),chosen:false},...moves.map(a=>({move:a.type==='tap'?(level.pods[a.pod].label||level.pods[a.pod].color+level.pods[a.pod].count):'bridge '+a.choice,win:winAt(pre,a),chosen:a.type==='tap'&&a.pod===steps[fatal].pod}))];}
 // the tightest moments of a won round: a state where few moves keep it winnable
 const tight=[],decisions=[];for(let i=1;i<steps.length;i++){const s=steps[i];if(!s.snap||steps[i-1].win!==true)continue;const moves=R.legal(s.snap.state);if(moves.length<2)continue;
  const good=moves.filter(a=>winAt(s.snap,a)===true).length;decisions.push({k:s.k,good,of:moves.length});if(good<=1)tight.push({k:s.k,good,of:moves.length});}
 const out=steps.slice(1).map(({before,snap,...s})=>s);
 // what holds the slots at the end, and where the colours the ants could work on next lie in the tray
 const end=t.state,front=colourReach(end),rows=level.tray.map((stack,c)=>stack.slice(end.heads[c]));
 const endSlots=Array.from(end.slots).filter(p=>p>=0).map(p=>({pod:level.pods[p].label||level.pods[p].color+level.pods[p].count,left:end.remaining[p],canWork:front.has(colourOf(p))}));
 const nextUseful=rows.map(r=>{const i=r.findIndex(p=>front.has(colourOf(p)));return i<0?null:i+1;});
 return {status:t.status,commands:commands.length,fatal:fatal>0?fatal:null,commandsAfterFatal:fatal>0?commands.length-fatal:null,
  msAfterFatal:fatal>0?(commands.at(-1).t-commands[fatal-1].t):null,realAfterFatal:fatal>0&&commands.at(-1).real!=null?(commands.at(-1).real-commands[fatal-1].real):null,
  winStart:steps[0].win,alternatives,tight,decisions,endSlots,nextUseful,parked:out.filter(s=>s.parked).length,steps:out,timingDelaysMs:delays};}
// ---- ours ----
const results={ours:[],theirs:[]};
for(const [name,commit,dir,levelsPath,exp] of [['v8C','0a0367e2','tools/gameplay-pilot-v8c','content/pilot-v8c/levels.json',v8cExport],['v8E','8892a6f4','tools/gameplay-pilot-v8e','content/pilot-v8e/levels.json',v8eExport]]){
 const d=build(commit,dir),E=await engine(d),levels=JSON.parse(execSync(`git show ${commit}:${levelsPath}`,{cwd:root,maxBuffer:1<<26})).levels,X=JSON.parse(fs.readFileSync(exp));
 const hard=new Set(levels.filter(l=>/hard/i.test(l.difficulty||'')).map(l=>l.id));
 for(const a of X.attempts){if(a.outcome==='abandoned')continue;const level=levels[a.level-1];
  const real=a.inputEvents.filter(e=>e.accepted).map(e=>e.atMs);
  const cmds=a.commands.map((c,i)=>({t:c.modelTimeMs,real:real[i]??null,action:c.type==='tap'?{type:'tap',pod:c.pod}:{type:c.type,choice:c.choice}}));
  const m=measure(E,level,cmds);results.ours.push({build:name,level:a.level,difficulty:level.difficulty,outcome:a.outcome,rating:a.rating,interest:a.interest,note:a.note||null,elapsedS:Math.round(a.elapsedMs/1000),fast:X.settings?.fast??null,...m});
  process.stderr.write(`${name} L${a.level} ${a.outcome} fatal ${m.fatal} parked ${m.parked}\n`);}}
// ---- theirs ----
{const d=build('HEAD','tools/gameplay-pilot-v8e',dd=>{const p=dd+'/rules.cjs',a=fs.readFileSync(p,'utf8'),old="if(group.some(p=>!heads.includes(p)))return 'partner-not-exposed';",
  cf="if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';";
  if(a.split(old).length!==2)throw Error('link rule moved');fs.writeFileSync(p,a.replace(old,cf));});
 const E=await engine(d),{R}=E,CF=JSON.parse(fs.readFileSync(path.join(root,'product/audits/colony-flow-rounds/levels.json'))).levels;
 for(const L of CF.filter(l=>l.rounds)){
  const tok=t=>{const m=/^([A-Za-z#])(\d+)(\?)?(?::([a-z]))?$/.exec(t);return {c:m[1],n:+m[2],mystery:!!m[3],link:m[4]||null};};
  const cols=Object.values(L.tray).map(col=>col.map(tok));const need={};for(const col of cols)for(const p of col)need[p.c]=(need[p.c]||0)+p.n;
  const rows=L.board.rows.map(r=>[...r]);const have={};for(const r of rows)for(const c of r)if(c!=='.'&&c!=='?')have[c]=(have[c]||0)+1;
  // board "?" tiles (L26): their colours are known only in total; fill them in reading order. A colour the board
  // recognition misses a cell or two of (L25 sky blue 467/469): the last capsule of it gives the difference back.
  const hidden=[];for(const c of Object.keys(need))for(let i=0;i<need[c]-(have[c]||0);i++)hidden.push(c);
  const qs=[];rows.forEach((r,y)=>r.forEach((c,x)=>{if(c==='?')qs.push([x,y]);}));
  if(qs.length){qs.forEach(([x,y],i)=>rows[y][x]=hidden[i]);}else for(const c of Object.keys(need)){let d=need[c]-(have[c]||0);for(let ci=cols.length-1;d>0&&ci>=0;ci--)for(let k=cols[ci].length-1;d>0&&k>=0;k--)if(cols[ci][k].c===c){const take=Math.min(d,cols[ci][k].n-1);cols[ci][k].n-=take;d-=take;}}
  const map=Object.fromEntries(Object.keys(need).map((c,i)=>[c,R.CHANNELS[i]]));
  const W=L.board.w+2,H=L.board.h+2,blank='.'.repeat(W),padded=[blank,...rows.map(r=>'.'+r.map(c=>c==='.'?'.':map[c]).join('')+'.'),blank];
  const pods=[],tray=cols.map(col=>col.map(p=>{pods.push({color:map[p.c],count:p.n,label:p.c+p.n,...(p.link?{group:p.link}:{}),...(p.mystery?{mystery:'depth'}:{})});return pods.length-1;}));
  const level={id:L.level,title:'CF L'+L.level,subject:'cf',w:W,h:H,rowsBottomFirst:padded.slice().reverse(),entries:[Math.floor(W/2)],capacity:L.capacity,pods,tray,difficulty:L.difficulty};
  const colIndex=c=>+c.slice(1)-1;
  for(const r of L.rounds){const heads=tray.map(()=>0),cmds=[];
   for(const tap of r.taps){const c=colIndex(tap.column),p=tray[c][heads[c]];const g=pods[p].group?pods.map((q,i)=>i).filter(i=>pods[i].group===pods[p].group):[p];
    for(const q of g){const qc=tray.findIndex(s=>s.includes(q));heads[qc]++;}cmds.push({t:Math.round((tap.t-r.start)*1000),real:Math.round((tap.t-r.start)*1000),action:{type:'tap',pod:p}});}
   const m=measure(E,level,cmds);results.theirs.push({level:L.level,difficulty:L.difficulty,attempt:r.attempt,outcome:r.outcome,elapsedS:Math.round(r.end-r.start),...m});
   process.stderr.write(`CF L${L.level} #${r.attempt} ${r.outcome} → ${m.status} fatal ${m.fatal} parked ${m.parked} delays ${m.timingDelaysMs}\n`);}}}
console.log(JSON.stringify(results));
