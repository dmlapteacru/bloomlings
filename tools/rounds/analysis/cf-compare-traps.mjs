// Our round levels against the Colony Flow findings of 2026-10-31 (product/audits/colony-flow-patterns/README.md):
//   trap links — a linked group with a member whose colour has no cell on the open front at the start (the picture hides
//     its work, so the group holds two places for long), and the row it starts in (a bait on top);
//   tempo — random column choices calm (waiting for the board before each tap) and fast (a tap as soon as a place is free);
//   jams — in calm losses, the share where a linked group sits in the places.
// node tools/rounds/analysis/cf-compare-traps.mjs <from> <to> [--cf CF-19,CF-20,CF-25,CF-26] [--games 200] [--out <json>]
const ROOT=new URL('../../../',import.meta.url).pathname;
const fs=await import('node:fs'),path=await import('node:path');
const arg=(k,d)=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:d;};
const [from,to]=process.argv.slice(2).map(Number),G=+arg('--games',200);
const ours=await (await import(ROOT+'tools/rounds/engine.mjs')).engine();
const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');const cf=await (await import(ROOT+'tools/level-stats/engines.mjs')).engine('CF');
const CODES='GPBACDVYOLMRKNSE';
// access depth of every cell from the entries: coloured cells cost 1 to pass (0 = on the open front)
function depths(level){const {w,h,rowsBottomFirst:rows,entries}=level,g=rows.join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#')continue;
  const cost=CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 const minD={};for(let i=0;i<g.length;i++)if(CODES.includes(g[i]))minD[g[i]]=Math.min(minD[g[i]]??Infinity,d[i]);return minD;}
function analyse(name,cls,level,E){const minD=depths(level),groups=new Map();level.pods.forEach((p,i)=>{if(p.group){if(!groups.has(p.group))groups.set(p.group,[]);groups.get(p.group).push(i);}});
 const rowOf=i=>{for(const st of level.tray){const r=st.indexOf(i);if(r>=0)return r;}return -1;};
 const links=[...groups.values()].map(m=>({hidden:m.some(i=>(minD[level.pods[i].color]??0)>0),row:Math.min(...m.map(rowOf))+1}));
 let seed=17+name.length*7919;const rnd=()=>{seed^=seed<<13;seed>>>=0;seed^=seed>>17;seed^=seed<<5;seed>>>=0;return seed/4294967296;};
 const {R,T,idle:I}=E,moves=s=>R.legal(s);let cw=0,fw=0,losses=0,linkJam=0;
 for(let g=0;g<G;g++){let s=R.create(level,level.capacity||5);while(s.status==='Playing'){const mv=moves(s);if(!mv.length)break;s=I.step(s,mv[Math.floor(rnd()*mv.length)]).state;}
  if(s.status==='Won')cw++;else{losses++;if(Array.from(s.slots).some(p=>p>=0&&level.pods[p].group))linkJam++;}
  const t=new T.Timeline(R.create(level,level.capacity||5));let k=0;while(k++<200000){if(t.state.status!=='Playing')break;const mv=moves(t.state).filter(m=>!t.check(m));if(mv.length){t.commit(mv[Math.floor(rnd()*mv.length)]);continue;}if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
  if(t.state.status==='Won')fw++;}
 return {name,cls,pods:level.pods.length,links:links.length,trapLinks:links.filter(l=>l.hidden).length,trapTop:links.filter(l=>l.hidden&&l.row<=2).length,
  calm:Math.round(cw/G*100),fast:Math.round(fw/G*100),linkInJam:losses?Math.round(linkJam/losses*100):null};}
const rows=[];
for(const k of String(arg('--cf','CF-19,CF-20,CF-25,CF-26')).split(',').filter(Boolean)){const r=allRecords().find(x=>x.key===k);rows.push(analyse(k,r.difficulty,{...r.model,capacity:r.capacity||5},cf));}
for(let n=from;n<=to;n++){const f=path.join(ROOT,'content/rounds/levels',`L${String(n).padStart(4,'0')}.json`);if(!fs.existsSync(f))continue;const L=JSON.parse(fs.readFileSync(f,'utf8'));rows.push(analyse('L'+n,L.cls,L.definition,ours));process.stderr.write('.');}
process.stderr.write('\n');
const out=arg('--out',null);if(out)fs.writeFileSync(out,JSON.stringify(rows,null,1));
console.log('уровень класс подов связок ловушек(сверху) спокойно быстро связка-в-проигрыше');for(const r of rows)console.log(r.name,r.cls,r.pods,r.links,`${r.trapLinks}(${r.trapTop})`,r.calm+'%',r.fast+'%',r.linkInJam==null?'—':r.linkInJam+'%');
