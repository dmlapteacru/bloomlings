const ROOT=new URL('../../../',import.meta.url).pathname;
// at each owner tap (winning attempt): legal launches (tappable now), and of them with work on the open front
const {engine}=await import(ROOT+'tools/rounds/engine.mjs');const fs=await import('node:fs');
const E=await engine(),{R,T}=E,res=JSON.parse(fs.readFileSync(process.argv[2]));const rows=[];
for(let n=1;n<=20;n++){const L=JSON.parse(fs.readFileSync(`${ROOT}content/rounds/levels/L${String(n).padStart(4,'0')}.json`)),d=L.definition;
 const at=res.attempts.filter(a=>a.level===n&&a.outcome==='won').at(-1);if(!at)continue;const t=new T.Timeline(R.create(d,5));const legal=[],useful=[];
 for(const c of at.commands){if(c.type!=='tap'&&c.type!=='bridge')continue;const target=c.modelTimeMs||0;while(t.pending&&t.steps[0].at<=target+1e-6)t.advance(t.steps[0].at-t.now);if(target>t.now)t.advance(target-t.now);
  const st=t.state,front=new Set(R.reach(st).targets.map(i=>R.CHANNELS[st.board[i]]));
  const opts=R.legal(st).filter(x=>x.type!=='bridge'&&!t.check(x));
  if(c.type==='tap'){legal.push(opts.length);useful.push(opts.filter(x=>(x.pods||[x.pod]).some(p=>front.has(d.pods[p].color))).length);}
  const act=c.type==='bridge'?{type:'bridge',choice:c.choice}:(opts.find(x=>x.pod===c.pod||(x.pods&&x.pods.includes(c.pod)))||{type:'tap',pod:c.pod});if(t.check(act))continue;t.commit(act);}
 const pct=(a,f)=>Math.round(a.filter(f).length/a.length*100);
 rows.push({n,cls:L.cls,recipe:L.recipe,taps:legal.length,oneLegal:pct(legal,x=>x<=1),avgLegal:+(legal.reduce((a,b)=>a+b,0)/legal.length).toFixed(1),noWork:pct(useful,x=>x===0),oneUseful:pct(useful,x=>x<=1),avgUseful:+(useful.reduce((a,b)=>a+b,0)/useful.length).toFixed(1)});}
console.table(rows);fs.writeFileSync(process.env.OUT||'/dev/null',JSON.stringify(rows));
