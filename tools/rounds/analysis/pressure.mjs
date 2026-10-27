const ROOT=new URL('../../../',import.meta.url).pathname;
// slot pressure along random games (a launch as soon as one is possible, uniform among launches, else wait): the share
// of time with at most one free place, and the mean number of placed pods without reachable work
// node tools/rounds/analysis/pressure.mjs <results.json> [preview-folder]  (prints one row per level with the owner's rating)
const {engine}=await import(ROOT+'tools/rounds/engine.mjs');const fs=await import('node:fs');
const E=await engine(),{R,T}=E;const rng=s=>()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};
export function pressure(level,games=30,seed=777){const r=rng(seed);let tight=0,idle=0,total=0;
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5));let k=0;
  while(k++<100000&&t.state.status!=='Won'){const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){t.commit(m[Math.floor(r()*m.length)]);continue;}if(!t.pending)break;
   const dt=t.steps[0].at-t.now,s=t.state,free=[...s.slots].filter(x=>x<0).length,reach=new Set(R.reach(s).targets.map(i=>s.board[i]));
   const stuck=[...s.slots].filter(p=>p>=0&&s.remaining[p]>0&&!reach.has(s.meta.colours[p])).length;
   if(dt>0){total+=dt;if(free<=1)tight+=dt;idle+=stuck*dt;}t.advance(dt);}}
 return {tight:Math.round(tight/total*100)/100,stuck:Math.round(idle/total*100)/100};}
if(process.argv[2]){const res=JSON.parse(fs.readFileSync(process.argv[2]));const rows=[];
 for(const n of [...new Set(res.attempts.map(a=>a.level))].sort((a,b)=>a-b)){let f=ROOT+`content/rounds/levels/L${String(n).padStart(4,'0')}.json`;if(!fs.existsSync(f)&&process.argv[3])f=`${process.argv[3]}/L${String(n).padStart(4,'0')}.json`;if(!fs.existsSync(f))continue;
  const L=JSON.parse(fs.readFileSync(f)),w=res.attempts.filter(a=>a.level===n&&a.outcome==='won').at(-1)||res.attempts.filter(a=>a.level===n).at(-1);
  rows.push({n,cls:L.cls,intro:!!L.intro,...pressure(L.definition),rating:w.rating,interest:w.interest});}
 console.table(rows);}
