// one whole picture with a divider (the owner, 2026-10-30): the far region is an outlined subject that touches no edge
// (an island with its pagoda), the near region everything round it. The queue's skeleton makes exactly one bridge decide:
// the pods of the two approaches (APPROACH, the colours before each bridge's anchor) first in a random colour order, so
// either bridge may be offered first; a few of the subject's pods (MID, up to EARLYN) and then the deciding bridge's
// landing colour (ENTRY); the rest of the subject; the colour where the other bridge lands (LAST); the background (OUT)
// last. The subject's colours are split finer (FARSIZE cells a pod, 17 §3) so that with the other bridge more of them
// stand before LAST than there are places. Links take the lead dose of a Super Hard (17 §7 as amended on 2026-10-30): 3
// pairs in the first three rows, the rest through the middle; 2 hidden pods. A candidate is kept when only the deciding
// bridge (WIN_DIR, its crossing direction) wins, alone it is in ALO-AHI by QG random games (17 §5 as amended on
// 2026-10-30), §5's forced-decision and opening checks pass, both bridges are offered first now and then, and pods
// without work reach PMIN.
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||'/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
process.argv.splice(2,0,'noop');
const fs=await import('node:fs');
const LV=await import('/home/user/bloomlings/tools/rounds/levels.mjs');
const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const M=await import('/home/user/bloomlings/tools/rounds/measure.mjs');
const [rowsFile,outDir,seedArg,nArg]=process.argv.slice(3);fs.mkdirSync(outDir,{recursive:true});
const rows=JSON.parse(fs.readFileSync(rowsFile)).rows,pic={...L.picture(process.env.PIC),rows,h:rows.length};
const m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null).pods;
const geo=LV.candidate(pic,board,base,3,'bridge','Normal',1,false,null,{}).level,v=m.volumes;
const WIN=geo.hero.choices.findIndex(c=>c.dir===(process.env.WIN_DIR||'up'));
let s=+(seedArg||1)*2654435761>>>0;const r=()=>{s^=s<<13;s>>>=0;s^=s>>17;s^=s<<5;s>>>=0;return s/4294967296;};
const split=(c,n,tot=v[c])=>{const b=Math.floor(tot/n),k=tot-b*n;return Array.from({length:n},(_,i)=>({color:c,count:b+(i<k?1:0)}));};
// the subject's colours split finer (FARSIZE cells a pod, 17 §3: 3-6 pods for a colour of 60 and more, at most half of
// it a pod), so that with the other bridge more of them stand before the last colour than there are places
const FAR=()=>[process.env.ENTRY,process.env.LAST,...JSON.parse(process.env.MID)],FS=+(process.env.FARSIZE||0);
const per=c=>{const lo=Math.max(v[c]>=20?2:1,Math.ceil(v[c]/55));if(FS&&FAR().includes(c))return Math.min(Math.max(lo,Math.round(v[c]/(FS*(.85+.3*r())))),v[c]>=60?6:v[c]>=20?Math.floor(v[c]/10):2);return Math.max(lo,Math.round(v[c]/(32+r()*14)));};
const APPROACH=JSON.parse(process.env.APPROACH),OUT=JSON.parse(process.env.OUT),ENTRY=process.env.ENTRY,MID=JSON.parse(process.env.MID),LAST=process.env.LAST,PMIN=+(process.env.PMIN||2);
const TLO=+(process.env.TLO||.1),THI=+(process.env.THI||.5);
const keyed=(cols,spread)=>{let ps=cols.flatMap((c,i)=>split(c,per(c)).map(p=>({...p,key:i+r()*spread})));const t=TLO+(THI-TLO)*r();ps.forEach(p=>p.key=(1-t)*p.key+t*(cols.length-p.key)+(r()-.5));return ps.sort((a,b)=>a.key-b.key).map(({key,...p})=>p);};
const found=[];const N=+(nArg||40);console.log('deciding bridge',WIN,geo.hero.choices.map(c=>c.label).join(' / '));
for(let k=0;k<N;k++){
 const cols=+(process.env.COLS||4);
 // the approaches (APPROACH: the colours before each bridge's anchor) first, in a random colour order, so either bridge
 // may be offered first; the landing colour right after; the subject's colours (MID), a few of them early; the colour
 // where the other bridge lands (LAST) late; the background (OUT) last, a share of it slipped in among the subject's pods
 const ap=(r()<.5?APPROACH:[...APPROACH].reverse()).flatMap(c=>split(c,per(c)));
 const mid=keyed(MID,1.2),out=keyed(OUT,1.4),slip=Math.floor(out.length*(+(process.env.SLIP||.3))*r());
 const rest=[...mid.map((p,i)=>({...p,key:i/mid.length+(r()-.5)*.3})),...out.slice(0,slip).map(p=>({...p,key:.2+.8*r()}))].sort((a,b)=>a.key-b.key).map(({key,...p})=>p);
 // a few of the subject's pods before the landing pods: they wait in places until the bridge stands (EARLYN at most)
 const early=Math.floor(r()*(+(process.env.EARLYN||0)+1)),pre=[];for(let i=0;i<early;i++){const j=rest.findIndex(p=>MID.includes(p.color));if(j>=0)pre.push(...rest.splice(j,1));}
 const order=[...ap,...pre,...split(ENTRY,per(ENTRY)),...rest,...split(LAST,per(LAST)),...out.slice(slip)];
 const tray=Array.from({length:cols},()=>[]);order.forEach((p,i)=>tray[i%cols].push(i));const pods=order.map(p=>({...p}));
 const used=new Set();let g=0;
 {const opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<3;row++)opts.push([tray[c][row],tray[c+1][row],row]);
  opts.sort((a,b)=>(a[2]===0?-2:a[2]===1?-1:0)+r()-((b[2]===0?-2:b[2]===1?-1:0)+r()));
  for(const [a,b,row] of opts){if(g>=3)break;if(g===0&&row!==0)continue;if(used.has(a)||used.has(b))continue;used.add(a);used.add(b);const n=String.fromCharCode(65+g++);pods[a].group=n;pods[b].group=n;}}
 {const depth=Math.min(...tray.map(t=>t.length)),mids=[];for(let c=0;c+1<cols;c++)for(let row=3;row<depth-1;row++)mids.push([tray[c][row],tray[c+1][row],row]);
  mids.sort(()=>r()-.5);const minL=Math.ceil(.35*pods.length),pairs=Math.ceil(minL/2)+(r()<.35?1:0),rowsUsed=new Set();
  for(const [a,b,row] of mids){if(g>=pairs)break;if(used.has(a)||used.has(b)||rowsUsed.has(row))continue;used.add(a);used.add(b);rowsUsed.add(row);const n=String.fromCharCode(65+g++);pods[a].group=n;pods[b].group=n;}}
 tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5).slice(0,2).forEach(([p])=>pods[p].mystery='depth');
 const level={...geo,pods,tray,recipe:'bridge+links+hidden',strip:'gap'};
 {const ce=LV.checkPods(pods,v);if(ce.length){console.log(k,'balance',JSON.stringify(ce).slice(0,160));continue;}}
 const w=(await M.bridgeChoicesWin(level)).each;if(!(w[WIN]===true&&w[1-WIN]===false)){console.log(k,'bridges',JSON.stringify(w));continue;}
 const alone=await M.quickShare(level,+(process.env.QG||60),11,WIN),off=(await M.bridgeSingle(level,60,3,w)).offered;
 console.log(k,'offered',JSON.stringify(off),'pods',pods.length,'links',g,'alone',Math.round(alone*100));
 if(!(alone>=+(process.env.ALO||.06)&&alone<=+(process.env.AHI||.2))||Math.min(...off)<12)continue;
 const fo=await M.forcedShare(level),op=await M.openingTest(level,'Super Hard');console.log(k,'  forced',Math.round(fo*100),'opening',op.first.wins+'/'+op.first.openings,Math.round(op.three.share*100),op.ok?'OK':'');
 if(fo>.35||!op.ok)continue;const pr=await M.pressure(level);console.log(k,'  pressure',pr.stuck);if(pr.stuck<PMIN)continue;
 const f=`${outDir}/cand-${seedArg}-${k}.json`;fs.writeFileSync(f,JSON.stringify(level));found.push({f,alone,off,P:pods.length,links:g,pressure:pr.stuck});}
fs.writeFileSync(`${outDir}/found-${seedArg}.json`,JSON.stringify(found));
