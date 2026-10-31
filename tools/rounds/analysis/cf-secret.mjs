// node tools/rounds/analysis/cf-secret.mjs <CF-key> [games] calm [letter:name,…]
// what separates calm wins from calm losses: for every tap group, the win rate when it is taken early vs late, and the
// ordering pairs with the largest win-rate gap
const ROOT=new URL('../../../',import.meta.url).pathname;
const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');const {engine}=await import(ROOT+'tools/level-stats/engines.mjs');const E=await engine('CF'),R=E.R,I=E.idle;
const key=process.argv[2],N=+process.argv[3]||3000,MODE=process.argv[4]||'calm';let seed=7+key.length*131;const rnd=()=>{seed^=seed<<13;seed>>>=0;seed^=seed>>17;seed^=seed<<5;seed>>>=0;return seed/4294967296;};
const rec=allRecords().find(r=>r.key===key),L=rec.model,s0=R.create(L,5);
const colOf=p=>L.tray.findIndex(s=>s.includes(p));
const NAME={...Object.fromEntries((process.argv[5]||'').split(',').filter(Boolean).map(x=>x.split(':')))};
const nm=p=>`${NAME[L.pods[p].color]||L.pods[p].color}${L.pods[p].count}${L.pods[p].mystery?'?':''}`;
const gname=g=>{const G=s0.meta.groups[g];return (G.length>1?'связка ':'')+G.map(p=>`${nm(p)} (кол.${colOf(p)+1} ряд ${L.tray[colOf(p)].indexOf(p)+1})`).join(' + ');};
const games=[];
for(let i=0;i<N;i++){let s=R.create(L,5);const ord=[];let end=null;
 while(s.status==='Playing'){const mv=R.legal(s).filter(m=>m.type==='tap');if(!mv.length)break;const m=mv[Math.floor(rnd()*mv.length)];ord.push(m.pod);s=I.step(s,m).state;}
 // where the loss happened: the pods in the places at the jam
 if(s.status!=='Won')end=Array.from(s.slots).filter(p=>p>=0);games.push({ord,won:s.status==='Won',end,len:ord.length});}
const W=games.filter(g=>g.won).length;console.log(key,rec.difficulty,'calm random win',(W/N*100).toFixed(1)+'%',N,'games');
// the jam: which pods sit in the places when a game is lost (share of losses)
const jam=new Map();for(const g of games)if(!g.won)for(const p of new Set(g.end.map(p=>s0.meta.groups[p][0])))jam.set(p,(jam.get(p)||0)+1);
const losses=N-W;console.log('\nВ местах при проигрыше (доля проигрышей):');
for(const [p,n] of [...jam].sort((a,b)=>b[1]-a[1]).slice(0,8))console.log(`  ${Math.round(n/losses*100)}%  ${gname(p)}`);
// the losing taps: where the jam came (tap number)
const lens=games.filter(g=>!g.won).map(g=>g.len).sort((a,b)=>a-b);console.log('\nПроигрыш на тапе (медиана):',lens[lens.length>>1],'из',Math.max(...games.filter(g=>g.won).map(g=>g.len)));
// early vs late: the win rate when a group is taken in the first third of its possible positions vs later
const firsts=[...new Set(L.pods.map((_,i)=>s0.meta.groups[i][0]))];const rows=[];
for(const g of firsts){let ew=0,en=0,lw=0,ln=0;for(const x of games){const i=x.ord.indexOf(g);if(i<0)continue;const rel=i/Math.max(1,x.len);if(rel<0.34){en++;if(x.won)ew++;}else if(rel>0.66){ln++;if(x.won)lw++;}}
 if(en>=40&&ln>=40)rows.push({g,early:ew/en,late:lw/ln,gap:ew/en-lw/ln});}
console.log('\nЧто решает время (победы, если взять в первой трети партии / в последней трети):');
for(const r of rows.sort((a,b)=>Math.abs(b.gap)-Math.abs(a.gap)).slice(0,8))console.log(`  рано ${Math.round(r.early*100)}% · поздно ${Math.round(r.late*100)}%  ${gname(r.g)}`);
