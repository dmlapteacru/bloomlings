// castle P0152 with a hand-made queue after v8's bridge logic; prints the engine's verdicts
process.env.ROUNDS_DIR='/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
process.argv.splice(2,0,'noop');
const fs=await import('node:fs');
const LV=await import('/home/user/bloomlings/tools/rounds/levels.mjs');
const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const M=await import('/home/user/bloomlings/tools/rounds/measure.mjs');
const rows=JSON.parse(fs.readFileSync(process.argv[3])).rows;
const pic={...L.picture('P0152'),rows,h:rows.length};
const m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null).pods;
const geo=LV.candidate(pic,board,base,3,'bridge','Normal',1,false,null,{}).level;
console.log('volumes',JSON.stringify(m.volumes));
// the order: right bed (M), the pumpkins for the right pocket and the left bed (O), the picture from the right pocket up,
// the moss of the left pocket last
const v=m.volumes,split=(c,n)=>{const b=Math.floor(v[c]/n),r=v[c]-b*n;return Array.from({length:n},(_,i)=>({color:c,count:b+(i<r?1:0)}));};
const order=JSON.parse(process.argv[4]||'null')||[...split('M',1).map(p=>({...p,count:42})),
  ...split('O',3),...split('G',2),...split('D',3),...split('E',1),...split('C',3),...split('Y',2),...split('R',2),...split('S',5),{color:'M',count:39},{color:'M',count:39}];
// fix M: 42 for the bed first, the rest late
const sumM=order.filter(p=>p.color==='M').reduce((a,p)=>a+p.count,0);if(sumM!==v.M)console.log('M mismatch',sumM,v.M);
const cols=+(process.argv[5]||3),tray=Array.from({length:cols},()=>[]);order.forEach((p,i)=>tray[i%cols].push(i));
const level={...geo,pods:order,tray,recipe:'bridge'};
const errs=LV.checkPods(level.pods,m.volumes);if(errs.length)console.log('balance',errs);
const q=await M.quickShare(level,60);console.log('quick share',q);
const w=await M.bridgeChoicesWin(level);console.log('each bridge alone (solver)',JSON.stringify(w.each));
const bc=await M.bridgeChoices(level,60,5,w.each);console.log('random share with each bridge',JSON.stringify(bc.each.map(x=>[x.label,x.win,Math.round(x.share*100)])));
const bs=await M.bridgeSingle(level,60,3,w.each);console.log('single',JSON.stringify({ok:bs.ok,offered:bs.offered}));
const lose=w.each.indexOf(false);if(lose>=0){const bl=await M.bridgeLoss(level,lose);console.log('loss after wrong bridge',JSON.stringify(bl));}
fs.writeFileSync(process.argv[3].replace('.json','-level.json'),JSON.stringify(level));
