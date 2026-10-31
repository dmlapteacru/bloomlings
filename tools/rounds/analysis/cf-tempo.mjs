// node tools/rounds/analysis/cf-tempo.mjs <CF-key> [games]
// random column choices in two tempos: calm (wait until the board settles before each tap) and fast (tap as soon as a
// place is free), in the CF engine's live timeline
const ROOT=new URL('../../../',import.meta.url).pathname;
const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');const {engine}=await import(ROOT+'tools/level-stats/engines.mjs');const E=await engine('CF'),R=E.R,T=E.T,I=E.idle;
const key=process.argv[2],N=+process.argv[3]||400;let seed=99+key.length*31;const rnd=()=>{seed^=seed<<13;seed>>>=0;seed^=seed>>17;seed^=seed<<5;seed>>>=0;return seed/4294967296;};
const rec=allRecords().find(r=>r.key===key),L=rec.model;
function calm(){let s=R.create(L,5);while(s.status==='Playing'){const mv=R.legal(s).filter(m=>m.type==='tap');if(!mv.length)break;s=I.step(s,mv[Math.floor(rnd()*mv.length)]).state;}return s.status==='Won';}
function fast(){const t=new T.Timeline(R.create(L,5));let k=0;
 while(k++<200000){if(t.state.status!=='Playing')break;const mv=R.legal(t.state).filter(m=>m.type==='tap'&&!t.check(m));
  if(mv.length){t.commit(mv[Math.floor(rnd()*mv.length)]);continue;}if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
 return t.state.status==='Won';}
let c=0,f=0;for(let i=0;i<N;i++){if(calm())c++;if(fast())f++;}
console.log(key,rec.difficulty,'calm',Math.round(c/N*100)+'%','fast',Math.round(f/N*100)+'%');
