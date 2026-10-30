// the contour bridge levels where both bridges win and one is better (17 §8 'better'; the wave plan of 2026-10-30: L23,
// L33, L43 Normal bridge+hidden, L35, L50 Hard bridge+links+hidden). Skeleton as in the Super Hard search (island/search2):
// the two approaches first in a random colour order, a few of the subject's pods (EARLYN), the better bridge's landing
// colour (ENTRY), the subject's other colours (MID) with the other bridge's landing colour (LAST) slipped in at a share
// LPOS of them, the background (OUT) partly among them (SLIP) and the rest last. Links (LINKS=lead) take the lead dose of
// the class (3 pairs in rows 0–2, the rest through the middle, the dose place DOSE of the range); HID hidden pods. A
// candidate is kept when both bridges win, the better one by quick test is GAP points over the other and in ALO–AHI,
// forced decisions ≤ 35 % and (Hard) the opening check passes.
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||'/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
process.argv.splice(2,0,'noop');
const fs=await import('node:fs');
const LV=await import('/home/user/bloomlings/tools/rounds/levels.mjs');
const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const M=await import('/home/user/bloomlings/tools/rounds/measure.mjs');
const [outDir,seedArg,nArg]=process.argv.slice(3);fs.mkdirSync(outDir,{recursive:true});
const E=process.env,pic=L.picture(E.PIC),m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null).pods;
const geo=LV.candidate(pic,board,base,3,'bridge','Normal',1,false,null,{}).level,v=m.volumes,CLS=E.CLS||'Normal';
const grid=geo.rowsBottomFirst.join(''),landOf=c=>grid[2*c.cell-c.anchor];
const BETTER=geo.hero.choices.findIndex(c=>landOf(c)===E.ENTRY);
let s=+(seedArg||1)*2654435761>>>0;const r=()=>{s^=s<<13;s>>>=0;s^=s>>17;s^=s<<5;s>>>=0;return s/4294967296;};
const split=(c,n,tot=v[c])=>{const b=Math.floor(tot/n),k=tot-b*n;return Array.from({length:n},(_,i)=>({color:c,count:b+(i<k?1:0)}));};
const APPROACH=JSON.parse(E.APPROACH),OUT=JSON.parse(E.OUT||'[]'),ENTRY=E.ENTRY,MID=JSON.parse(E.MID),LAST=E.LAST;
const FAR=[ENTRY,LAST,...MID],FS=+(E.FARSIZE||0);
const per=c=>{const lo=Math.max(v[c]>=20?2:1,Math.ceil(v[c]/55));if(FS&&FAR.includes(c))return Math.min(Math.max(lo,Math.round(v[c]/(FS*(.85+.3*r())))),v[c]>=60?6:v[c]>=20?Math.floor(v[c]/10):2);return Math.max(lo,Math.round(v[c]/(32+r()*14)));};
const TLO=+(E.TLO||.1),THI=+(E.THI||.5);
const keyed=(cols,spread)=>{let ps=cols.flatMap((c,i)=>split(c,per(c)).map(p=>({...p,key:i+r()*spread})));const t=TLO+(THI-TLO)*r();ps.forEach(p=>p.key=(1-t)*p.key+t*(cols.length-p.key)+(r()-.5));return ps.sort((a,b)=>a.key-b.key).map(({key,...p})=>p);};
const DOSE={Normal:[.1,.3],Hard:[.2,.45],'Super Hard':[.35,.6]}[CLS],HID=+(E.HID||2),LINKS=E.LINKS==='lead';
const found=[];const N=+(nArg||40);console.log('better bridge',BETTER,geo.hero.choices.map(c=>c.label+' → '+landOf(c)).join(' / '));
for(let k=0;k<N;k++){
 const cols=+(E.COLS||4);
 const mid=keyed(MID,1.2),out=keyed(OUT,1.4);
 // APFIRST: the far bridge's approach first and APGAP background pods before the near one's, so that either bridge may be
 // offered first although the near anchor is walked to sooner
 const apOrder=E.APFIRST?[E.APFIRST,...APPROACH.filter(c=>c!==E.APFIRST)]:(r()<.5||APPROACH.length<2?APPROACH:[...APPROACH].reverse());
 const gapN=Math.floor(r()*(+(E.APGAP||0)+1)),ap=apOrder.flatMap((c,i)=>[...(i===1?out.splice(0,gapN):[]),...split(c,per(c))]);
 const slip=Math.floor(out.length*(+(E.SLIP||.3))*r());
 const lp=+(E.LPLO||.5)+(+(E.LPHI||.9)-(+(E.LPLO||.5)))*r();
 const rest=[...mid.map((p,i)=>({...p,key:i/mid.length+(r()-.5)*.3})),...out.slice(0,slip).map(p=>({...p,key:.2+.8*r()})),...split(LAST,per(LAST)).map(p=>({...p,key:lp+(r()-.5)*.2}))].sort((a,b)=>a.key-b.key).map(({key,...p})=>p);
 const early=Math.floor(r()*(+(E.EARLYN||0)+1)),pre=[];for(let i=0;i<early;i++){const j=rest.findIndex(p=>MID.includes(p.color));if(j>=0)pre.push(...rest.splice(j,1));}
 // PRE: up to that many of the subject's pods (the landing colours' first) before the approaches: they wait in places
 // until a bridge stands (a weak picture's only lever, the dinosaur's)
 const entry=split(ENTRY,per(ENTRY)),preN=Math.floor(r()*(+(E.PRE||0)+1)),front=[];
 for(let i=0;i<preN;i++){const j=rest.findIndex(p=>p.color===LAST);front.push(i%2===0&&entry.length>1?entry.shift():j>=0?rest.splice(j,1)[0]:entry.shift());}
 const order=[...front,...ap,...pre,...entry,...rest,...out.slice(slip)];
 const tray=Array.from({length:cols},()=>[]);order.forEach((p,i)=>tray[i%cols].push(i));const pods=order.map(p=>({...p}));
 const used=new Set();let g=0;
 if(LINKS){const P=pods.length,lo=Math.ceil(DOSE[0]*P/2),hi=Math.floor(DOSE[1]*P/2),pairs=Math.max(lo,Math.min(hi,Math.round(lo+(hi-lo)*(+(E.DOSE||.4))+(r()-.5))));
  const opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<3;row++)opts.push([tray[c][row],tray[c+1][row],row]);
  opts.sort((a,b)=>(a[2]===0?-2:a[2]===1?-1:0)+r()-((b[2]===0?-2:b[2]===1?-1:0)+r()));
  for(const [a,b,row] of opts){if(g>=Math.min(3,pairs))break;if(g===0&&row!==0)continue;if(used.has(a)||used.has(b))continue;used.add(a);used.add(b);const n=String.fromCharCode(65+g++);pods[a].group=n;pods[b].group=n;}
  const depth=Math.min(...tray.map(t=>t.length)),mids=[];for(let c=0;c+1<cols;c++)for(let row=3;row<depth-1;row++)mids.push([tray[c][row],tray[c+1][row],row]);
  mids.sort(()=>r()-.5);const rowsUsed=new Set();
  for(const [a,b,row] of mids){if(g>=pairs)break;if(used.has(a)||used.has(b)||rowsUsed.has(row))continue;used.add(a);used.add(b);rowsUsed.add(row);const n=String.fromCharCode(65+g++);pods[a].group=n;pods[b].group=n;}}
 tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5).slice(0,HID).forEach(([p])=>pods[p].mystery='depth');
 const level={...geo,pods,tray,recipe:E.RECIPE||'bridge+hidden',strip:'gap'};
 {const ce=LV.checkPods(pods,v);if(ce.length){console.log(k,'balance',JSON.stringify(ce).slice(0,160));continue;}}
 {const de=LV.dosesOk(level,CLS);if(de){console.log(k,'doses',de);continue;}}
 const w=(await M.bridgeChoicesWin(level)).each;if(!(w[0]===true&&w[1]===true)){console.log(k,'bridges',JSON.stringify(w));continue;}
 const QG=+(E.QG||60),qs=[await M.quickShare(level,QG,11,0),await M.quickShare(level,QG,11,1)],best=qs[0]>=qs[1]?0:1,gap=Math.abs(qs[0]-qs[1]);
 console.log(k,'pods',pods.length,'links',g,'shares',qs.map(x=>Math.round(x*100)).join('/'),'better',best);
 if(gap<+(E.GAP||.12)||!(qs[best]>=+(E.ALO||.5)&&qs[best]<=+(E.AHI||.75)))continue;
 const off=(await M.bridgeSingle(level,60,3,w)).offered;console.log(k,'  offered',JSON.stringify(off));if(Math.min(...off)<+(E.OFFMIN||0))continue;
 const fo=await M.forcedShare(level);let op={ok:true};if(CLS!=='Normal')op=await M.openingTest(level,CLS);console.log(k,'  forced',Math.round(fo*100),'opening',op.ok?'OK':'no');
 if(fo>.35||!op.ok)continue;
 const f=`${outDir}/cand-${seedArg}-${k}.json`;fs.writeFileSync(f,JSON.stringify(level));found.push({f,qs,best,off,P:pods.length,links:g});}
fs.writeFileSync(`${outDir}/found-${seedArg}.json`,JSON.stringify(found));
