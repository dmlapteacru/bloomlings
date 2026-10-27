const ROOT=new URL('../../../',import.meta.url).pathname;
const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');const {engine}=await import(ROOT+'tools/level-stats/engines.mjs');const {STRATEGIES}=await import(ROOT+'tools/level-stats/strategies.mjs');
const E=await engine('CF'),{R,T}=E;
function play(level,cap,opening){const pol=STRATEGIES['most-now'].make(1,E),t=new T.Timeline(R.create(level,cap));let g=0,i=0;
 while(g++<100000){if(t.state.status==='Won')return true;const m=R.legal(t.state).filter(a=>!t.check(a));
  if(m.length){let a;if(i<opening.length){a=m[opening[i]%m.length];i++;}else a=pol(t,m);
   if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
  if(!t.pending)break;t.advance(t.steps[0].at-t.now);}return t.state.status==='Won';}
const rows=[];
for(const r of allRecords().filter(r=>r.game==='CF'&&r.model)){const r0={key:r.key,cls:r.difficulty};
 try{for(const k of [0,1,2,3]){let seqs=[[]];for(let j=0;j<k;j++)seqs=seqs.flatMap(s=>[0,1,2,3,4].map(b=>[...s,b]));let w=0;for(const s of seqs)if(play(r.model,r.capacity||5,s))w++;r0['k'+k]=Math.round(w/seqs.length*100);}}catch(e){r0.err=String(e.message).slice(0,60);}
 rows.push(r0);}
console.table(rows);(await import('node:fs')).writeFileSync(process.env.OUT||'/dev/null',JSON.stringify(rows));
