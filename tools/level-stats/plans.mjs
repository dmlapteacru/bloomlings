// Different winning plans on a few levels the model can be trusted on (full information, the patient model: each tap's
// work settles before the next). A plan's signature is the order in which colours open on the board (become reachable);
// two routes that open the board in the same order are one plan, whatever the order of the taps in between. The search
// branches on every move at the first four decisions (PLANS_BRANCHES, 30 by default), finds a winning continuation for each, and
// keeps the two plans whose opening orders part earliest. Each plan is told as
// a few steps "what is tapped → which colours open → how many places are freed". A second plan that is not found is
// reported as "not found in the search made", never as "the only solution".
// node tools/level-stats/plans.mjs CF-10 CF-25 v8E-15 v9-22 → tools/level-stats/out/plans.json
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root} from './levels.mjs';import {engine} from './engines.mjs';
const keys=process.argv.slice(2).length?process.argv.slice(2):['CF-10','CF-25','v8E-15','v8E-22','v9-22'];
const recs=Object.fromEntries(allRecords().map(r=>[r.key,r])),out=[];
for(const key of keys){const r=recs[key],E=await engine(r.game),R=E.idle,level=r.model,cap=level.capacity||5,CH=E.R.CHANNELS;
 const OURS={G:'Лист',P:'Лепесток',B:'Вода',A:'Янтарь',C:'Жемчуг',D:'Земля',V:'Слива',Y:'Солнце',O:'Тыква',L:'Лайм',M:'Мох',R:'Ягода',K:'Румянец',N:'Глубина',S:'Небо',E:'Кора'};
 const name=c=>{const ch=CH[c];if(r.game==='CF'){const letter=Object.entries(r.colourMap||{}).find(([,v])=>v===ch)?.[0];return r.board?.legend?.[letter]||letter||ch;}return OURS[ch]||ch;};
 const label=p=>r.game==='CF'?(level.pods[p].label||level.pods[p].color+level.pods[p].count):(OURS[level.pods[p].color]||level.pods[p].color)+' '+level.pods[p].count;
 // a winning continuation from a state (depth-first, memo of failures, budget)
 function finish(s0,budget=+(process.env.PLANS_BUDGET||8000)){const memo=new Set();let n=0;const go=s=>{if(s.status==='Won')return [];if(s.status!=='Playing')return null;const k=R.signature(s);if(memo.has(k))return null;if(++n>budget)throw 0;
   for(const a of R.legal(s)){const t=go(R.step(s,a).state);if(t)return [a,...t];}memo.add(k);return null;};try{return go(s0);}catch{return undefined;}}
 const colourOf=a=>a.type==='tap'?level.pods[a.pod].color:'bridge';
 const signature=route=>{let s=R.create(level,cap);const seen=[];const add=st=>{for(const i of E.R.reach(st).targets){const c=st.board[i];if(!seen.includes(c))seen.push(c);}};add(s);for(const a of route){s=R.step(s,a).state;add(s);}return seen;};
 const s0=R.create(level,cap),routes=[];let branches=0;const MAXB=+(process.env.PLANS_BRANCHES||30);
 const branch=(s,prefix,depth)=>{if(branches>=MAXB)return;if(depth===4||s.status!=='Playing'){branches++;const rest=finish(s);if(rest)routes.push([...prefix,...rest]);return;}
  for(const a of R.legal(s)){if(branches>=MAXB)return;branch(R.step(s,a).state,[...prefix,a],depth+1);}};
 branch(s0,[],0);
 // the two plans whose colour orders part earliest
 let best=null;for(let i=0;i<routes.length;i++)for(let j=i+1;j<routes.length;j++){const A=signature(routes[i]),B=signature(routes[j]);let k=0;while(k<Math.min(A.length,B.length)&&A[k]===B[k])k++;
  if(k<Math.min(A.length,B.length)&&(!best||k<best.k))best={k,i,j};}
 // a plan in steps: taps grouped until something opens or frees
 function story(route){let s=R.create(level,cap);const steps=[];let cur=null;
  for(const a of route){const before=new Set(E.R.reach(s).targets.map(i=>s.board[i])),freeBefore=s.slots.filter(p=>p<0).length,n=R.step(s,a).state;
   const after=new Set(E.R.reach(n).targets.map(i=>n.board[i])),opened=[...after].filter(c=>!before.has(c)),freed=Math.max(0,n.slots.filter(p=>p<0).length-freeBefore+(a.type==='tap'?n.meta.groups[a.pod].length:0));
   const tap=a.type==='tap'?n.meta.groups[a.pod].map(label).join('+')+(n.meta.groups[a.pod].length>1?' (связка)':''):'мостик '+a.choice;
   cur=cur||{taps:[],opened:[],freed:0};cur.taps.push(tap);cur.opened.push(...opened.map(name));cur.freed+=freed;
   if(opened.length||freed>=2){steps.push(cur);cur=null;}s=n;}
  if(cur)steps.push(cur);return steps.map(x=>({taps:x.taps,opened:[...new Set(x.opened)],freed:x.freed}));}
 const plans=best?[routes[best.i],routes[best.j]]:routes.slice(0,1);
 out.push({key,difficulty:r.difficulty,game:r.game,routesFound:routes.length,distinctSignatures:new Set(routes.map(x=>signature(x).join(','))).size,partAt:best?best.k+1:null,
  plans:plans.map(p=>({taps:p.length,openingOrder:signature(p).map(name),steps:story(p)})),
  note:best?null:'второй план не найден в выполненном поиске (первые два хода перебраны, продолжение — поиском с бюджетом)'});
 process.stderr.write(`${key}: ${routes.length} routes, ${out.at(-1).distinctSignatures} colour orders, part at ${out.at(-1).partAt}\n`);}
fs.writeFileSync(path.join(root,'tools/level-stats/out/plans.json'),JSON.stringify(out,null,1));
for(const p of out){console.log('\n##',p.key,p.difficulty,'routes',p.routesFound,'orders',p.distinctSignatures,'part at colour',p.partAt);for(const [i,pl] of p.plans.entries()){console.log(' plan',i+1,pl.taps,'taps; opens',pl.openingOrder.join(' → '));for(const s of pl.steps)console.log('   ',s.taps.join(', '),'→ opens',s.opened.join(', ')||'—','; frees',s.freed);}}
