const ROOT=new URL('../../../',import.meta.url).pathname;
// share of decisions (a tap is possible) where exactly one pod can be tapped, along random games (tap as soon as possible)
const fs=await import('node:fs');const set=process.argv[2];
let E,levels;
if(set==='CF'){const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');E=await (await import(ROOT+'tools/level-stats/engines.mjs')).engine('CF');levels=allRecords().filter(r=>r.game==='CF'&&r.model).map(r=>({key:r.key,cls:r.difficulty,d:r.model,cap:r.capacity||5,links:r.model.pods.filter(p=>p.group!=null).length/r.model.pods.length}));}
else{E=await (await import(ROOT+'tools/rounds/engine.mjs')).engine();levels=[];for(let n=1;n<=20;n++){const L=JSON.parse(fs.readFileSync(`${ROOT}content/rounds/levels/L${String(n).padStart(4,'0')}.json`));levels.push({key:'L'+n,cls:L.cls,d:L.definition,cap:5,links:L.definition.pods.filter(p=>p.group!=null).length/L.definition.pods.length});}}
const {R,T}=E;const rng=s=>()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};
const rows=[];
for(const L of levels){let one=0,all=0,tot=0;const r=rng(12345);
 for(let g=0;g<30;g++){const t=new T.Timeline(R.create(L.d,L.cap));let k=0;
  while(k++<100000){if(t.state.status!=='Playing'&&t.state.status!=='Working'&&t.state.status!==undefined&&t.state.status!=='Playing')break;
   const m=R.legal(t.state).filter(a=>!t.check(a));const taps=m.filter(a=>a.type!=='bridge');
   if(m.length){if(taps.length){all++;tot+=taps.length;if(taps.length===1)one++;}const a=m[Math.floor(r()*m.length)];t.commit(a);continue;}
   if(!t.pending)break;t.advance(t.steps[0].at-t.now);}}
 rows.push({key:L.key,cls:L.cls,links:Math.round(L.links*100),oneTap:Math.round(one/all*100),avg:+(tot/all).toFixed(1)});}
console.table(rows);fs.writeFileSync(process.env.OUT||'/dev/null',JSON.stringify(rows));
