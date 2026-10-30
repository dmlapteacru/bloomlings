// fair P0154, the second hand-built Super Hard (17 §13 «Кто строит уровни», the owner, 2026-10-30), the garden's search
// (../garden-bridge/search.mjs) with WIN=0: the left bridge decides, so the left bed's moss comes first, the blush for the
// right bed and the left pocket next, the moss for the right pocket last. SK=A keeps both beds' pods side by side (the
// other skeleton offered only the winning bridge first), EARLY0 slips more picture pods in early, and a candidate must
// be offered both bridges first now and then and keep pods without work at PMIN or more (layer B of 17 §13)
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||'/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
process.argv.splice(2,0,'noop');
const fs=await import('node:fs');
const LV=await import('/home/user/bloomlings/tools/rounds/levels.mjs');
const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const M=await import('/home/user/bloomlings/tools/rounds/measure.mjs');
const [rowsFile,outDir,seedArg,nArg]=process.argv.slice(3);fs.mkdirSync(outDir,{recursive:true});
const rows=JSON.parse(fs.readFileSync(rowsFile)).rows,pic={...L.picture(process.env.PIC||'P0152'),rows,h:rows.length};
const m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null).pods;
const geo=LV.candidate(pic,board,base,3,'bridge','Normal',1,false,null,{}).level,v=m.volumes;
let s=+(seedArg||1)*2654435761>>>0;const r=()=>{s^=s<<13;s>>>=0;s^=s>>17;s^=s<<5;s>>>=0;return s/4294967296;};
const split=(c,n,tot=v[c])=>{const b=Math.floor(tot/n),k=tot-b*n;return Array.from({length:n},(_,i)=>({color:c,count:b+(i<k?1:0)}));};
// the castle colours from the right pocket upward: hill, outline, path, walls, windows, roofs, sky
const castle=JSON.parse(process.env.ORDER||'["G","D","E","C","Y","R","S"]'),BED_R=process.env.BED_R||'M',BED_L=process.env.BED_L||'O',RB=+(process.env.RB||42),LB=+(process.env.LB||42),RP=+(process.env.RP||78),LP=+(process.env.LP||78);
const found=[];const N=+(nArg||40);
for(let k=0;k<N;k++){
 const cols=4;
 const per=Object.fromEntries(castle.map(c=>[c,Math.max(v[c]>=20?2:1,Math.ceil(v[c]/60),Math.round(v[c]/(32+r()*14)))]));
 let mid=castle.flatMap((c,i)=>split(c,per[c]).map(p=>({...p,key:i+r()*1.2})));
 const t=.15+.5*r();mid.forEach(p=>p.key=(1-t)*p.key+t*(castle.length-p.key)+(r()-.5));mid.sort((a,b)=>a.key-b.key);mid=mid.map(({key,...p})=>p);
 const nO=Math.max(2+Math.floor(r()*2),Math.ceil(RP/55));
 // skeleton A: the moss and pumpkin bed pods side by side in the top row; B: the top row is the moss pods and castle
 // pods (waiting), the pumpkins a row later, so the right bridge is offered first more often
 const sk=process.env.SK||(r()<.65?'B':'A'),top=sk==='B'?cols-2:0;
 let order=[...split(BED_R,2,RB),...mid.slice(0,top),...split(BED_L,2,LB),...split(BED_L,nO,RP)];
 // some castle pods slip in among the pumpkins (they wait in places until the right pocket opens)
 const early=+(process.env.EARLY0||0)+Math.floor(r()*3);order=[...order,...mid.slice(top,top+early)];const rest=mid.slice(top+early);
 order=[...order,...rest.slice(0,Math.floor(rest.length*.7)),...split(BED_R,Math.max(2,Math.ceil(LP/55)),LP),...rest.slice(Math.floor(rest.length*.7))];
 const tray=Array.from({length:cols},()=>[]);order.forEach((p,i)=>tray[i%cols].push(i));
 const pods=order.map(p=>({...p}));
 // links: pairs in neighbouring columns at the same row, rows 1+ (the skeleton's top row stays free), 2-4 pairs
 // links seen from the start (the owner, 2026-10-30: «должно быть с самого начала»): a pair in the top row, one in the
 // second, the rest within the first three rows
 const want=3,opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<Math.min(3,tray[c].length,tray[c+1].length);row++)opts.push([tray[c][row],tray[c+1][row],row]);
 opts.sort((a,b)=>(a[2]===0?-2:a[2]===1?-1:0)+r()-((b[2]===0?-2:b[2]===1?-1:0)+r()));const used=new Set();let g=0;for(const [a,b,row] of opts){if(g>=want)break;if(g===0&&row!==0)continue;if(g===1&&row>1)continue;if(used.has(a)||used.has(b))continue;used.add(a);used.add(b);const name=String.fromCharCode(65+g++);pods[a].group=name;pods[b].group=name;}
 // and through the middle of the queue too (the owner, 2026-10-30: «как будто и в середине тоже надо»): 2-3 more pairs
 // in rows 3 and deeper, at most one per row
 {const depth=Math.min(...tray.map(t=>t.length)),mids=[];for(let c=0;c+1<cols;c++)for(let row=3;row<depth-1;row++)mids.push([tray[c][row],tray[c+1][row],row]);
  mids.sort(()=>r()-.5);
  // the links take the lead dose of a Super Hard (17 §7 as amended on 2026-10-30): at least 35 % of the pods, in pairs
  const minL=Math.ceil(.35*pods.length),pairs=Math.ceil(minL/2)+(r()<.35?1:0),extra=Math.max(2,pairs-g),rowsUsed=new Set();for(const [a,b,row] of mids){if(rowsUsed.size>=extra)break;if(used.has(a)||used.has(b)||rowsUsed.has(row))continue;used.add(a);used.add(b);rowsUsed.add(row);const name=String.fromCharCode(65+g++);pods[a].group=name;pods[b].group=name;}}
 // two hidden pods, rows 1+, not in a group
 const hs=tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5).slice(0,2);hs.forEach(([p])=>pods[p].mystery='depth');
 const level={...geo,pods,tray,recipe:'bridge+links+hidden'};
 {const ce=LV.checkPods(pods,v);if(ce.length){console.log(k,"balance",JSON.stringify(ce).slice(0,200));continue;}}
 // WIN: the deciding bridge (0 left, 1 right); the fair mirrors the garden, its left bridge decides
 const WIN=+(process.env.WIN??1);const w=(await M.bridgeChoicesWin(level)).each;if(!(w[WIN]===true&&w[1-WIN]===false)){console.log(k,'bridges',JSON.stringify(w));continue;}
 const bc=await M.bridgeChoices(level,60,5,w),q=await M.quickShare(level,60),right=bc.each[WIN].share;
 const off=(await M.bridgeSingle(level,60,3,w)).offered;console.log(k,'sk',sk,'offered',JSON.stringify(off),'cols',cols,'pods',pods.length,'links',g,'q',Math.round(q*100),'right alone',Math.round(right*100));
 // the class is the deciding bridge's (17 §5 as amended on 2026-10-30): the right bridge alone in 6–20 %
 if(!(right>=.06&&right<=.2))continue;
 // §5 checks the middle links tend to break: forced decisions at most 35 %, the greedy opening test
 const fo=await M.forcedShare(level);const op=await M.openingTest(level,'Super Hard');console.log(k,'  forced',Math.round(fo*100),'opening',op.first.wins+'/'+op.first.openings,Math.round(op.three.share*100),op.ok?'OK':'');
 if(fo>.35||!op.ok)continue;
 // both bridges offered first now and then (a real choice), and slot pressure of a Super Hard (17 §13 layer B: pods
 // without work ≥ 2.0)
 if(Math.min(...off)<15)continue;const pr=await M.pressure(level);console.log(k,'  pressure',pr.stuck);if(pr.stuck<+(process.env.PMIN||2))continue;
 const f=`${outDir}/cand-${seedArg}-${k}.json`;fs.writeFileSync(f,JSON.stringify(level));found.push({f,q,right,cols,P:pods.length,links:g,sk,off});}
fs.writeFileSync(`${outDir}/found-${seedArg}.json`,JSON.stringify(found));
