// castle P0152: keep the v8 bridge skeleton (moss for the right bed first, pumpkins for the left bed and the right
// pocket next, moss for the left pocket last) and search the rest: more, smaller pods for the castle, a partly
// reversed castle order, link pairs (neighbouring columns, same row) and 2 hidden pods; keep candidates where exactly
// the right bridge wins and the right bridge alone is hard (its random share low)
process.env.ROUNDS_DIR='/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
process.argv.splice(2,0,'noop');
const fs=await import('node:fs');
const LV=await import('/home/user/bloomlings/tools/rounds/levels.mjs');
const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const M=await import('/home/user/bloomlings/tools/rounds/measure.mjs');
const [rowsFile,outDir,seedArg,nArg]=process.argv.slice(3);fs.mkdirSync(outDir,{recursive:true});
const rows=JSON.parse(fs.readFileSync(rowsFile)).rows,pic={...L.picture('P0152'),rows,h:rows.length};
const m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null).pods;
const geo=LV.candidate(pic,board,base,3,'bridge','Normal',1,false,null,{}).level,v=m.volumes;
let s=+(seedArg||1)*2654435761>>>0;const r=()=>{s^=s<<13;s>>>=0;s^=s>>17;s^=s<<5;s>>>=0;return s/4294967296;};
const split=(c,n,tot=v[c])=>{const b=Math.floor(tot/n),k=tot-b*n;return Array.from({length:n},(_,i)=>({color:c,count:b+(i<k?1:0)}));};
// the castle colours from the right pocket upward: hill, outline, path, walls, windows, roofs, sky
const castle=['G','D','E','C','Y','R','S'];
const found=[];const N=+(nArg||40);
for(let k=0;k<N;k++){
 const cols=r()<.5?3:4;
 const per={G:2+Math.floor(r()*2),D:3+Math.floor(r()*2),E:1,C:3+Math.floor(r()*2),Y:2,R:2,S:4+Math.floor(r()*3)};
 let mid=castle.flatMap((c,i)=>split(c,per[c]).map(p=>({...p,key:i+r()*1.2})));
 const t=.15+.5*r();mid.forEach(p=>p.key=(1-t)*p.key+t*(castle.length-p.key)+(r()-.5));mid.sort((a,b)=>a.key-b.key);mid=mid.map(({key,...p})=>p);
 const nO=2+Math.floor(r()*2);
 // skeleton A: the moss and pumpkin bed pods side by side in the top row; B: the top row is the moss pods and castle
 // pods (waiting), the pumpkins a row later, so the right bridge is offered first more often
 const sk=r()<.65?'B':'A',top=sk==='B'?cols-2:0;
 let order=[...split('M',2,42),...mid.slice(0,top),...split('O',2,42),...split('O',nO,78)];
 // some castle pods slip in among the pumpkins (they wait in places until the right pocket opens)
 const early=Math.floor(r()*3);order=[...order,...mid.slice(top,top+early)];const rest=mid.slice(top+early);
 order=[...order,...rest.slice(0,Math.floor(rest.length*.7)),...split('M',2,78),...rest.slice(Math.floor(rest.length*.7))];
 const tray=Array.from({length:cols},()=>[]);order.forEach((p,i)=>tray[i%cols].push(i));
 const pods=order.map(p=>({...p}));
 // links: pairs in neighbouring columns at the same row, rows 1+ (the skeleton's top row stays free), 2-4 pairs
 const want=2+Math.floor(r()*3),opts=[];for(let c=0;c+1<cols;c++)for(let row=1;row<Math.min(tray[c].length,tray[c+1].length);row++)opts.push([tray[c][row],tray[c+1][row]]);
 opts.sort(()=>r()-.5);const used=new Set();let g=0;for(const [a,b] of opts){if(g>=want)break;if(used.has(a)||used.has(b))continue;used.add(a);used.add(b);const name=String.fromCharCode(65+g++);pods[a].group=name;pods[b].group=name;}
 // two hidden pods, rows 1+, not in a group
 const hs=tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5).slice(0,2);hs.forEach(([p])=>pods[p].mystery='depth');
 const level={...geo,pods,tray,recipe:'bridge+links+hidden'};
 if(LV.checkPods(pods,v).length){continue;}
 const w=(await M.bridgeChoicesWin(level)).each;if(!(w[0]===false&&w[1]===true)){console.log(k,'bridges',JSON.stringify(w));continue;}
 const bc=await M.bridgeChoices(level,60,5,w),q=await M.quickShare(level,60),right=bc.each[1].share;
 const off=(await M.bridgeSingle(level,60,3,w)).offered;console.log(k,'sk',sk,'offered',JSON.stringify(off),'cols',cols,'pods',pods.length,'links',g,'q',Math.round(q*100),'right alone',Math.round(right*100));
 const f=`${outDir}/cand-${seedArg}-${k}.json`;fs.writeFileSync(f,JSON.stringify(level));found.push({f,q,right,cols,P:pods.length,links:g,sk,off});}
fs.writeFileSync(`${outDir}/found-${seedArg}.json`,JSON.stringify(found));
