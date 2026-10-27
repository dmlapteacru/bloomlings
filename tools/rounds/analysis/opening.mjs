const ROOT=new URL('../../../',import.meta.url).pathname;
// "opening then colours": every opening of k taps (as soon as possible), then "most work now"; share of openings that win
const {engine}=await import(ROOT+'tools/rounds/engine.mjs');const {STRATEGIES}=await import(ROOT+'tools/level-stats/strategies.mjs');const fs=await import('node:fs');
const E=await engine(),{R,T}=E;
function play(level,opening,side){const pol=STRATEGIES['most-now'].make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));let g=0,i=0;
 while(g++<100000){if(t.state.status==='Won')return true;const m=R.legal(t.state).filter(a=>!t.check(a));
  if(m.length){const br=m.filter(a=>a.type==='bridge');let a;if(br.length)a=side==='left'?br[0]:br.at(-1);else if(i<opening.length){a=m[opening[i]%m.length];i++;}else a=pol(t,m);
   if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
  if(!t.pending)break;t.advance(t.steps[0].at-t.now);}return t.state.status==='Won';}
// enumerate openings as index sequences into the legal list (max branching 6)
const rows=[];
for(let n=1;n<=20;n++){const L=JSON.parse(fs.readFileSync(`${ROOT}content/rounds/levels/L${String(n).padStart(4,'0')}.json`)),d=L.definition;const r={n,cls:L.cls,recipe:L.recipe,bot:L.random.share};
 for(const k of [0,1,2,3]){const seqs=[[]];for(let j=0;j<k;j++){const nx=[];for(const s of seqs)for(let b=0;b<5;b++)nx.push([...s,b]);seqs.splice(0,seqs.length,...nx);}
  // dedupe: the same legal index modulo m may repeat; acceptable as a share estimate
  let w=0;for(const s of seqs){if(play(d,s,'left')||play(d,s,'right'))w++;}r['k'+k]=Math.round(w/seqs.length*100);}
 rows.push(r);}
console.table(rows);fs.writeFileSync(process.env.OUT||'/dev/null',JSON.stringify(rows));
