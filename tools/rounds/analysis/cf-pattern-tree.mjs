// The winning tap patterns of a Colony Flow round, in a form a person can read (the owner, 2026-10-30): a pattern is the
// sequence of chosen columns ("1 1 2 3"); a linked group goes in one tap whichever column is pressed, so it is written as
// its sorted columns ("1-3"; a pair stacked in one column "2-2"). There are ~1e11–1e15 winning patterns per round, so they
// are not listed; instead:
//   - the decision tree of the first taps: at each position, which taps can still win (share of the sampled wins through
//     it) and which never won in the checks (random continuations, then a bounded exact search);
//   - the ordering rules every sampled win keeps (pod A before pod B), which random sequences often break;
//   - where the choice matters: by tap number, the share of positions on winning paths where some tap loses.
// Model: the CF engine (tools/level-stats/cf-engine) with the player waiting for the board to settle between taps
// (idle-rules: the live walkers, then the next tap), which replays the owner's recorded wins of L20, L25 and L26.
// node tools/rounds/analysis/cf-pattern-tree.mjs <CF-key> [--wins 2000] [--depth 6] [--checks 60] [--out <dir>]
const ROOT=new URL('../../../',import.meta.url).pathname;
const fs=await import('node:fs'),path=await import('node:path');
const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');const {engine}=await import(ROOT+'tools/level-stats/engines.mjs');
const E=await engine('CF'),I=E.idle,R=E.R;
const arg=(k,d)=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:d;};
const key=process.argv[2],WINS=+arg('--wins',2000),DEPTH=+arg('--depth',6),CHECKS=+arg('--checks',60),BUDGET=+arg('--budget',4000),OUT=arg('--out',ROOT+'product/audits/colony-flow-patterns');
const rec=allRecords().find(r=>r.key===key);if(!rec||!rec.model)throw Error('no CF model '+key);const L=rec.model,cap=rec.capacity||5;
let seed=1234567+key.length*7919;const rnd=()=>{seed^=seed<<13;seed>>>=0;seed^=seed>>17;seed^=seed<<5;seed>>>=0;return seed/4294967296;};
const taps=s=>R.legal(s).filter(m=>m.type==='tap');
const colOf=p=>L.tray.findIndex(st=>st.includes(p))+1;
const label=(s,mv)=>s.meta.groups[mv.pod].map(colOf).sort((a,b)=>a-b).join('-');
const podName=p=>`${L.pods[p].color}${L.pods[p].count}${L.pods[p].mystery?'?':''}`;
const groupName=(s,first)=>{const g=s.meta.groups[first];return g.length>1?'связка '+g.map(p=>`${podName(p)} (кол. ${colOf(p)}, №${L.tray[colOf(p)-1].indexOf(p)+1})`).join(' + '):`${podName(first)} (кол. ${colOf(first)}, №${L.tray[colOf(first)-1].indexOf(first)+1})`;};
function playout(s){let st=s;while(st.status==='Playing'){const mv=taps(st);if(!mv.length)break;st=I.step(st,mv[Math.floor(rnd()*mv.length)]).state;}return st.status==='Won';}
// bounded exact search for a win from s (memo by R.signature); 'win' | 'loss' (proven) | 'unknown' (budget spent)
function search(s,budget){const seen=new Set();let nodes=0;
 const go=st=>{if(st.status==='Won')return 'win';if(st.status!=='Playing')return 'loss';const k=R.signature(st);if(seen.has(k))return 'loss';if(++nodes>budget)return 'unknown';
  let unk=false;for(const mv of taps(st)){const r=go(I.step(st,mv).state);if(r==='win')return 'win';if(r==='unknown')unk=true;}seen.add(k);return unk?'unknown':'loss';};
 return go(s);}
// 1. sample winning patterns by random play (uniform over the taps on offer, waiting between taps)
const t0=Date.now(),wins=[],podOrder=[];let games=0,allOrders=[];
while(wins.length<WINS&&games<WINS*60){games++;let st=R.create(L,cap);const seq=[],order=[];
 while(st.status==='Playing'){const mv=taps(st);if(!mv.length)break;const m=mv[Math.floor(rnd()*mv.length)];seq.push(label(st,m));order.push(m.pod);st=I.step(st,m).state;}
 if(allOrders.length<4000)allOrders.push(order);if(st.status==='Won'){wins.push(seq);podOrder.push(order);}}
const sampleSec=(Date.now()-t0)/1000;process.stderr.write(`${key}: ${wins.length} wins of ${games} games, ${sampleSec.toFixed(0)} s\n`);
// 2. the decision tree of the first DEPTH taps
function node(prefix,depth){let s=R.create(L,cap);for(const lab of prefix){const m=taps(s).find(x=>label(s,x)===lab);s=I.step(s,m).state;}
 const through=wins.filter(w=>prefix.every((x,i)=>w[i]===x));const n=through.length;const opts=[];
 for(const mv of taps(s)){const lab=label(s,mv),k=through.filter(w=>w[prefix.length]===lab).length;const o={tap:lab,pod:groupName(s,mv.pod),wins:k,share:n?Math.round(k/n*1000)/10:0};
  if(k===0){const c=I.step(s,mv).state;let w=0;for(let i=0;i<CHECKS&&!w;i++)if(playout(c))w++;o.check=w?'выигрывает (редко)':search(c,BUDGET)==='loss'?'проигрывает (доказано)':(c.status!=='Playing'?'проигрывает сразу':'не выиграл в проверках');}
  opts.push(o);}
 const res={prefix:prefix.join(' '),winsThrough:n,options:opts};
 if(depth>0)res.children=opts.filter(o=>o.wins>=Math.max(20,n*0.05)).map(o=>node([...prefix,o.tap],depth-1));
 return res;}
const tree=node([],DEPTH);
// 3. ordering rules: pairs of tap groups (first pod of a group) that every win launches in one order
const firsts=[...new Set(L.pods.map((p,i)=>R.create(L,cap).meta.groups[i][0]))];
const posIn=order=>{const m=new Map();order.forEach((p,i)=>m.set(p,i));return m;};
const winPos=podOrder.map(posIn),allPos=allOrders.map(posIn),rules=[];
for(const a of firsts)for(const b of firsts){if(a>=b)continue;
 let ab=0,ba=0;for(const m of winPos){if(m.has(a)&&m.has(b)){if(m.get(a)<m.get(b))ab++;else ba++;}}
 let rab=0,rba=0;for(const m of allPos){if(m.has(a)&&m.has(b)){if(m.get(a)<m.get(b))rab++;else rba++;}}
 const tot=ab+ba,rtot=rab+rba;if(tot<50||rtot<50)continue;
 // keep a rule every win keeps but at least a fifth of all sequences break (a choice, not a given of the queue)
 // every win keeps it (100 %) or nearly every (97 %+, a rare exception in the sample)
 if(ab/tot>=0.97&&rba/rtot>=0.2)rules.push({first:a,then:b,inWins:Math.round(ab/tot*1000)/10+' %',brokenInRandom:Math.round(rba/rtot*100)+' %'});
 if(ba/tot>=0.97&&rab/rtot>=0.2)rules.push({first:b,then:a,inWins:Math.round(ba/tot*1000)/10+' %',brokenInRandom:Math.round(rab/rtot*100)+' %'});}
const s0=R.create(L,cap);const rulesText=rules.sort((x,y)=>parseFloat(y.inWins)-parseFloat(x.inWins)||parseInt(y.brokenInRandom)-parseInt(x.brokenInRandom)).map(r=>({...r,text:`${groupName(s0,r.first)} — раньше, чем ${groupName(s0,r.then)}`}));
// 4. where the choice matters: on sampled winning paths, positions where some offered tap never wins in the checks
const profile=[];const PATHS=Math.min(+arg('--paths',20),wins.length),PCHECKS=+arg('--pchecks',20);
for(let w=0;w<PATHS;w++){let s=R.create(L,cap);const seq=wins[Math.floor(w*wins.length/PATHS)];
 for(let t=0;t<seq.length;t++){const mv=taps(s);let lose=0;
  if(mv.length>1)for(const m of mv){const c=I.step(s,m).state;if(c.status==='Won')continue;if(c.status!=='Playing'){lose++;continue;}let ok=false;for(let i=0;i<PCHECKS&&!ok;i++)ok=playout(c);if(!ok)lose++;}
  (profile[t]=profile[t]||{tap:t+1,positions:0,choice:0,matters:0,losing:0}).positions++;if(mv.length>1)profile[t].choice++;if(lose)profile[t].matters++;profile[t].losing+=lose;
  const m=mv.find(x=>label(s,x)===seq[t]);s=I.step(s,m).state;}}
// the owner's recorded win in the same notation
const rounds=JSON.parse(fs.readFileSync(ROOT+'product/audits/colony-flow-rounds/levels.json','utf8'));const lv=(Array.isArray(rounds.levels||rounds)?(rounds.levels||rounds):Object.values(rounds.levels||rounds)).find(x=>x.level===rec.level);
let recorded=null;if(lv){const r=(lv.rounds||[]).find(r=>r.outcome==='won');if(r){let s=R.create(L,cap);const out=[];for(const t of r.taps){const c=+t.column.slice(1)-1,p=L.tray[c][s.heads[c]],g=s.meta.groups[p];out.push(g.map(colOf).sort((a,b)=>a-b).join('-'));s=I.step(s,{type:'tap',pod:g[0]}).state;}recorded={pattern:out.join(' '),model:s.status};}}
const res={key,level:rec.level,difficulty:rec.difficulty,model:'CF engine, live walkers, the player waits for the board to settle between taps (idle-rules)',columns:L.tray.length,pods:L.pods.length,
 trayHeights:L.tray.map(s=>s.length),links:L.pods.filter(p=>p.group).length/2,hidden:L.pods.filter(p=>p.mystery).length,games,wins:wins.length,randomWinShare:Math.round(wins.length/games*1000)/10,
 recorded,tree,rules:rulesText,profile,examples:[...new Set(wins.map(w=>w.join(' ')))].slice(0,200),seconds:Math.round((Date.now()-t0)/1000)};
fs.mkdirSync(OUT,{recursive:true});fs.writeFileSync(path.join(OUT,key+'.json'),JSON.stringify(res,null,1));
console.log(key,'done',res.seconds+' s','wins',wins.length,'of',games,'rules',rulesText.length);
