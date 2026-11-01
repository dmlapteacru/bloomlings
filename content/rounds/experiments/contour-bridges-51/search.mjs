// Hand search for the three contour bridge levels of wave L51–L100 (L65, L75, L85; the owner's plan v2 of 2026-11-01,
// content/rounds/runs/L0051-L0100/mechanics.json). Generalises ../contour-bridges/search3.mjs ('better': both bridges win,
// one 10+ points better) with the 'single' mode of ../island-bridge/search.mjs (exactly one bridge wins, the solver proves
// the other loses), hidden cells (levels.mjs pickHiddenCells, 17 §8, the additional dose: exactly ceil(5 % N) still hidden
// after the start reveal, never on the far bank) and accents (levels.mjs pickAccents, 17 §6: the level's recolouring, one
// accent material in 2–4 cube clusters, its 1–2 pods in an early or middle row).
//
// The queue's skeleton (17 §8 «Какой мост решает, задаёт очередь»): the approaches (APPROACH, the near colours before each
// site's anchor) first in a random colour order (or APFIRST first, APGAP background pods between), so either bridge may be
// offered first; up to PRE pods of the far bank before them (they wait in places until a bridge stands); up to EARLYN of
// the subject's pods (MID) before ENTRY, the colour behind the deciding (better) bridge that comes early; the subject's
// other colours (MID, blended towards the reverse of their order by TLO–THI); LAST, the colour round the other bridge's
// landing, slipped in at the share LPLO–LPHI of them (late, a thick layer); the background (OUT) last, a share SLIP of it
// among the subject's pods. FARSIZE: cells a pod for the far colours (finer pods wait longer). Links take the lead dose of
// the class (17 §7, «Сложность строят поды»: the bridge takes no part): 3 pairs in rows 0–2, the rest through the middle,
// the place DOSE of the range; a Hard or Super Hard needs a trap link (17 §8 «Связка-ловушка»).
//
// node search.mjs <outDir> <seed> <n>      env: PIC, CLS (Hard | Super Hard), KIND (better | single), WIN_ANCHOR (the anchor
//   colour of the deciding / better bridge), APPROACH, ENTRY, MID, LAST, OUT (JSON lists / codes), COLS, CELLS=1 (hidden
//   cells), ACCENTS=1, QG (quick games), ALO–AHI (the deciding / better bridge's quick share), GAP ('better'), OFFMIN (each
//   bridge offered first in at least this many of 60 games), PMIN (pods without work, 'single'), CALM=1 (the §5 calm test)
// Reads pictures from ROUNDS_DIR (default: the repository's content/rounds), writes only into <outDir>.
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const HERE=path.dirname(fileURLToPath(import.meta.url)),REPO=path.resolve(HERE,'../../../..');
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||path.join(REPO,'content/rounds');
process.argv.splice(2,0,'noop');
const LV=await import(path.join(REPO,'tools/rounds/levels.mjs'));
const L=await import(path.join(REPO,'tools/rounds/lib.mjs'));
const M=await import(path.join(REPO,'tools/rounds/measure.mjs'));
const [outDir,seedArg,nArg]=process.argv.slice(3);fs.mkdirSync(outDir,{recursive:true});
const E=process.env,pic=L.picture(E.PIC),CLS=E.CLS||'Hard',KIND=E.KIND||'better';
let s=+(seedArg||1)*2654435761>>>0;const r=()=>{s^=s<<13;s>>>=0;s^=s>>17;s^=s<<5;s>>>=0;return s/4294967296;};
const j=(k,d)=>E[k]?JSON.parse(E[k]):d;
const APPROACH=j('APPROACH',[]),OUT=j('OUT',[]),MID=j('MID',[]),ENTRY=E.ENTRY||'',LAST=E.LAST||'',FS=+(E.FARSIZE||0),FAR=[ENTRY,LAST,...MID].filter(Boolean);
const TLO=+(E.TLO||.1),THI=+(E.THI||.5),DOSE={Normal:[.1,.3],Hard:[.2,.45],'Super Hard':[.35,.6]}[CLS];
const found=[];const N=+(nArg||40),t0=Date.now();
for(let k=0;k<N;k++){
 // accents (17 §6): one material (the picture has 9 colours, the stage allows 10), painted afresh for each candidate
 let recolor=null,accents=[];
 if(E.ACCENTS){const a=LV.pickAccents(pic,null,r,{materials:1});if(a){recolor=a.recolor;accents=a.accents;}}
 const m=L.measure(L.overlaid(pic,null,recolor)),board=L.boardOf(pic,null,recolor),v=m.volumes,grid=board.rowsBottomFirst.join('');
 const WIN=board.hero.choices.findIndex(c=>grid[c.anchor]===E.WIN_ANCHOR);if(WIN<0)throw Error('WIN_ANCHOR: no site with that anchor colour');
 if(k===0)console.log('deciding / better bridge',WIN,board.hero.choices.map(c=>`${c.label}: ${grid[c.anchor]} → ${grid[2*c.cell-c.anchor]}`).join(' / '));
 const split=(c,n,tot=v[c])=>{const b=Math.floor(tot/n),q=tot-b*n;return Array.from({length:n},(_,i)=>({color:c,count:b+(i<q?1:0)}));};
 const per=c=>{const lo=Math.max(v[c]>=20?2:1,Math.ceil(v[c]/55));if(FS&&FAR.includes(c))return Math.min(Math.max(lo,Math.round(v[c]/(FS*(.85+.3*r())))),v[c]>=60?6:v[c]>=20?Math.floor(v[c]/10):2);return Math.max(lo,Math.round(v[c]/(32+r()*14)));};
 const keyed=(cols,spread)=>{let ps=cols.flatMap((c,i)=>split(c,per(c)).map(p=>({...p,key:i+r()*spread})));const t=TLO+(THI-TLO)*r();ps.forEach(p=>p.key=(1-t)*p.key+t*(cols.length-p.key)+(r()-.5));return ps.sort((a,b)=>a.key-b.key).map(({key,...p})=>p);};
 const cols=+(E.COLS||4),mid=keyed(MID,1.2),out=keyed(OUT,1.4);
 const apOrder=E.APFIRST?[E.APFIRST,...APPROACH.filter(c=>c!==E.APFIRST)]:(r()<.5||APPROACH.length<2?APPROACH:[...APPROACH].reverse());
 const gapN=Math.floor(r()*(+(E.APGAP||0)+1));let ap=apOrder.flatMap((c,i)=>[...(i===1?out.splice(0,gapN):[]),...split(c,per(c))]);
 // APMIX: the approaches' pods alternate (one of each colour in the first row), so the bot opens either site first
 if(E.APMIX){const qs=apOrder.map(c=>ap.filter(p=>p.color===c)),rest0=ap.filter(p=>!apOrder.includes(p.color));ap=[];for(let i=0;qs.some(q=>q.length);i++)for(const q of qs)if(q.length)ap.push(q.shift());ap.splice(apOrder.length,0,...rest0);}
 const slip=Math.floor(out.length*(+(E.SLIP||.3))*r()),lp=+(E.LPLO||.5)+(+(E.LPHI||.9)-(+(E.LPLO||.5)))*r();
 const rest=[...mid.map((p,i)=>({...p,key:i/mid.length+(r()-.5)*.3})),...out.slice(0,slip).map(p=>({...p,key:.2+.8*r()})),...(LAST?split(LAST,per(LAST)).map(p=>({...p,key:lp+(r()-.5)*.2})):[])].sort((a,b)=>a.key-b.key).map(({key,...p})=>p);
 const early=Math.floor(r()*(+(E.EARLYN||0)+1)),pre=[];for(let i=0;i<early;i++){const q=rest.findIndex(p=>MID.includes(p.color));if(q>=0)pre.push(...rest.splice(q,1));}
 const entry=ENTRY?split(ENTRY,per(ENTRY)):[],preN=Math.floor(r()*(+(E.PRE||0)+1)),front=[];
 for(let i=0;i<preN;i++){const q=rest.findIndex(p=>p.color===LAST);const x=i%2===0&&entry.length>1?entry.shift():q>=0?rest.splice(q,1)[0]:entry.shift();if(x)front.push(x);}
 const order=[...front,...ap,...pre,...entry,...rest,...out.slice(slip)];
 // the accent pods (1–2, the accent's few cubes) in an early or middle row: from the second row to the middle of the tray
 for(const a of accents){const n=a.cells>=4&&r()<.5?2:1,rows=Math.ceil((order.length+n)/cols),hi=Math.max(1,Math.floor((rows-1)/2));
  for(const p of split(a.code,n,a.cells)){const at=cols+Math.floor(r()*cols*hi);order.splice(Math.min(at,order.length),0,p);}}
 const tray=Array.from({length:cols},()=>[]);order.forEach((p,i)=>tray[i%cols].push(i));const pods=order.map(p=>({...p}));
 const used=new Set();let g=0;
 {const P=pods.length,lo=Math.ceil(DOSE[0]*P/2),hi=Math.floor(DOSE[1]*P/2),pairs=Math.max(lo,Math.min(hi,Math.round(lo+(hi-lo)*(+(E.DOSE||.5))+(r()-.5))));
  const opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<3;row++)opts.push([tray[c][row],tray[c+1][row],row]);
  opts.sort((a,b)=>(a[2]===0?-2:a[2]===1?-1:0)+r()-((b[2]===0?-2:b[2]===1?-1:0)+r()));
  for(const [a,b,row] of opts){if(g>=Math.min(3,pairs))break;if(g===0&&row!==0)continue;if(used.has(a)||used.has(b))continue;used.add(a);used.add(b);const n=String.fromCharCode(65+g++);pods[a].group=n;pods[b].group=n;}
  const depth=Math.min(...tray.map(t=>t.length)),mids=[];for(let c=0;c+1<cols;c++)for(let row=3;row<depth-1;row++)mids.push([tray[c][row],tray[c+1][row],row]);
  mids.sort(()=>r()-.5);const rowsUsed=new Set();
  for(const [a,b,row] of mids){if(g>=pairs)break;if(used.has(a)||used.has(b)||(rowsUsed.has(row)&&r()<.7))continue;used.add(a);used.add(b);rowsUsed.add(row);const n=String.fromCharCode(65+g++);pods[a].group=n;pods[b].group=n;}}
 const recipe=E.CELLS?'bridge+links+cells':'bridge+links';
 const level={id:1,title:pic.title,subject:pic.id,w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,pods,tray,recipe,hero:board.hero,...(recolor?{recolor}:{}),strip:'gap'};
 // hidden cells (17 §8): the additional dose, exactly its minimum ceil(5 % N) (levels.mjs doses), never an accent cube
 if(E.CELLS){const want=Math.ceil(.05*LV.cellsOf(level)),avoid=new Set((recolor||[]).map(([x,y])=>(board.h-1-(y+2))*board.w+x+2));
  const hc=LV.pickHiddenCells(board,want,r,{avoid});if(!hc){console.log(k,'hidden cells: too few deep cells');continue;}level.hiddenCells=hc;}
 {const ce=LV.checkPods(pods,v);if(ce.length){console.log(k,'balance',JSON.stringify(ce).slice(0,160));continue;}}
 {const de=LV.dosesOk(level,CLS);if(de){console.log(k,'doses',de);continue;}}
 {const [blo,bhi]=L.budget(m.N);if(pods.length<blo||pods.length>bhi){console.log(k,'budget',pods.length,blo+'-'+bhi);continue;}}
 if(!M.trapLinks(level).some(x=>x.trap)){console.log(k,'no trap link');continue;}
 const w=(await M.bridgeChoicesWin(level)).each,QG=+(E.QG||60);
 let qs,best,gap;
 if(KIND==='better'){if(!(w[0]===true&&w[1]===true)){console.log(k,'bridges',JSON.stringify(w));continue;}
  qs=[await M.quickShare(level,QG,11,0),await M.quickShare(level,QG,11,1)];best=qs[0]>=qs[1]?0:1;gap=Math.abs(qs[0]-qs[1]);
  console.log(k,'pods',pods.length,'links',g,'shares',qs.map(x=>Math.round(x*100)).join('/'),'better',best);
  if(best!==WIN&&E.STRICTWIN)continue;
  if(gap<+(E.GAP||.12)||!(qs[best]>=+(E.ALO||.25)&&qs[best]<=+(E.AHI||.45)))continue;}
 else{if(!(w[WIN]===true&&w[1-WIN]===false)){console.log(k,'bridges',JSON.stringify(w));continue;}
  best=WIN;const alone=await M.quickShare(level,QG,11,WIN);qs=[0,0];qs[WIN]=alone;gap=alone;
  console.log(k,'pods',pods.length,'links',g,'alone',Math.round(alone*100));
  if(!(alone>=+(E.ALO||.07)&&alone<=+(E.AHI||.18)))continue;}
 const off=(await M.bridgeSingle(level,60,3,w)).offered;console.log(k,'  offered',JSON.stringify(off));if(Math.min(...off)<+(E.OFFMIN||8))continue;
 const fo=await M.forcedShare(level);if(fo>.35){console.log(k,'  forced',Math.round(fo*100));continue;}
 const op=await M.openingTest(level,CLS);console.log(k,'  forced',Math.round(fo*100),'opening',op.first.wins+'/'+op.first.openings,Math.round(op.three.share*100),op.ok?'OK':'no');if(!op.ok)continue;
 const pr=await M.pressure(level,30);console.log(k,'  pressure',pr.stuck);if(pr.stuck<+(E.PMIN||0))continue;
 let calm=null;if(E.CALM){calm=await M.calmTest(level,{bridge:best});console.log(k,'  calm',calm.share);if(M.calmFails(CLS,calm))continue;}
 const f=`${outDir}/cand-${seedArg}-${k}.json`;fs.writeFileSync(f,JSON.stringify(level));
 found.push({f,qs,best,gap,off,P:pods.length,links:g,cells:level.hiddenCells?M.cellsHiddenAtStart(level):0,forced:fo,pressure:pr.stuck,calm:calm&&calm.share,accents:accents.map(a=>a.code+a.cells)});
 fs.writeFileSync(`${outDir}/found-${seedArg}.json`,JSON.stringify(found));}
fs.writeFileSync(`${outDir}/found-${seedArg}.json`,JSON.stringify(found));console.log('done',found.length,'found in',Math.round((Date.now()-t0)/1000),'s');
