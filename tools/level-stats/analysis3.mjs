// Mechanics and their combinations, and what goes with difficulty (the owner's task of 2026-10-26). Four kinds of extra
// rule ("доп"): hidden capsules, links, padlocks with keys (Colony Flow), board objects (Colony Flow: crates counting one
// colour's work, chocolate counting any work, jars taking work). Linked capsules are counted once; a key and its padlock
// are one pair; hidden capsules are never merged with "?" board tiles; a counter whose function was not confirmed is not
// counted as a box (all of Colony Flow's were confirmed from the videos: mechanics.md).
// Times of padlock blocks come from the recordings (taps and recorded openings); box openings and the constraints still
// active by stage come from replaying the recorded won attempt on the model (exact for v8E, approximate for Colony Flow)
// and are labelled so. Reads tools/level-stats/out/data.json and adds `extra3`; random-play loss reasons from
// tools/level-stats/out/random/, the speed calibration from tools/level-stats/out/calibrate.json.
// node tools/level-stats/analysis3.mjs
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root,parseToken} from './levels.mjs';import {engine} from './engines.mjs';
const file=path.join(root,'tools/level-stats/out/data.json'),D=JSON.parse(fs.readFileSync(file,'utf8'));
const isNum=x=>typeof x==='number'&&Number.isFinite(x),r2=x=>isNum(x)?Math.round(x*100)/100:null;
const q=(a,p)=>{a=a.filter(isNum).sort((x,y)=>x-y);if(!a.length)return null;const i=(a.length-1)*p,k=Math.floor(i);return a[k]+(a[Math.min(k+1,a.length-1)]-a[k])*(i-k);};
const sum=a=>{const v=a.filter(isNum);return {n:v.length,median:r2(q(v,.5)),p25:r2(q(v,.25)),p75:r2(q(v,.75)),min:v.length?Math.min(...v):null,max:v.length?Math.max(...v):null};};
const OBJ=JSON.parse(fs.readFileSync(path.join(root,'tools/colony-deep-research/rounds/site/objects.json'),'utf8'));
const recs=Object.fromEntries(allRecords().map(r=>[r.key,r])),LV=Object.fromEntries(D.levels.map(l=>[l.key,l])),ROWS=D.rows;
const KINDS=['hidden','links','locks','boxes'],KIND_RU={hidden:'скрытые',links:'связки',locks:'замки',boxes:'предметы'};
// ---- per level: the four kinds ----
const per={};
for(const r of ROWS){const l=LV[r.key],cols=Object.values(l.tray).map(col=>col.map(parseToken)),o=r.game==='CF'?OBJ['L'+r.level]:null;
 const pods=cols.flatMap((col,ci)=>col.map((p,ri)=>({...p,col:ci,row:ri}))).filter(p=>!p.lock),caps=pods.length;
 const groups={};for(const p of pods)if(p.link)(groups[p.link]=groups[p.link]||[]).push(p);const G=Object.values(groups);
 // a hidden capsule taken without being seen: the lower member of a vertical link (it comes out with the one above)
 const unseen=pods.filter(p=>p.hidden&&p.link&&groups[p.link].some(q=>q.col===p.col&&q.row<p.row));
 const locks=cols.flatMap((col,ci)=>col.map((p,ri)=>p.lock?{colour:p.lock,col:ci,row:ri,below:col.slice(ri+1).filter(x=>!x.lock).length,belowCells:col.slice(ri+1).filter(x=>!x.lock).reduce((n,x)=>n+x.count,0)}:null).filter(Boolean));
 const cells=r.cells,board=l.board;
 for(const k of locks){const key=o?.keys?.find(x=>x.colour===k.colour);if(key&&board){const hole=key.trueHole||key.hole,h=board.rows.length,w=board.rows[0].length,xs=hole.map(c=>c[0]),ys=hole.map(c=>c[1]);
   k.key={x:r2((Math.min(...xs)+.5)/w),y:r2((Math.min(...ys)+.5)/h),edge:Math.min(...xs)<=1||Math.max(...xs)>=w-2||Math.min(...ys)<=1||Math.max(...ys)>=h-2};
   k.keyUnderBox=(o.objects||[]).filter(ob=>ob.kind!=='hidden').some(ob=>(ob.trueCells||ob.cells||[]).some(([x,y])=>hole.some(([a,b])=>Math.abs(a-x)+Math.abs(b-y)<=1)));}}
 const boxes=(o?.objects||[]).filter(ob=>ob.kind!=='hidden').map(ob=>({kind:ob.kind==='door'?'ящик (счёт работы своего цвета)':ob.kind==='chocolate'?'шоколад (счёт любой работы)':'кувшин (забирает работу своего цвета)',n:ob.n,cellsCovered:(ob.trueCells||ob.cells||[]).length,colour:ob.colour||null,under:ob.under||null}));
 const kinds={hidden:pods.some(p=>p.hidden),links:G.length>0,locks:locks.length>0,boxes:boxes.length>0};
 per[r.key]={game:r.game,level:r.level,difficulty:r.difficulty,cells,capsules:caps,kinds,count:KINDS.filter(k=>kinds[k]).length,combo:KINDS.filter(k=>kinds[k]).map(k=>KIND_RU[k]).join(' + ')||'без допов',
  hidden:{count:pods.filter(p=>p.hidden).length,share:r2(pods.filter(p=>p.hidden).length/caps),depths:pods.filter(p=>p.hidden).map(p=>p.row),inLinks:pods.filter(p=>p.hidden&&p.link).length,unseenAtChoice:unseen.length},
  links:{groups:G.length,sizes:G.map(g=>g.length),pods:G.reduce((n,g)=>n+g.length,0),share:r2(G.reduce((n,g)=>n+g.length,0)/caps),sameColour:G.filter(g=>new Set(g.map(p=>p.color)).size===1).length,
   shapes:G.map(g=>new Set(g.map(p=>p.col)).size===1?'вертикальная':new Set(g.map(p=>p.row)).size===1?'горизонтальная':'диагональная'),rowSpread:G.map(g=>Math.max(...g.map(p=>p.row))-Math.min(...g.map(p=>p.row))),colSpread:G.map(g=>Math.max(...g.map(p=>p.col))-Math.min(...g.map(p=>p.col))),slots:G.map(g=>g.length)},
  locks,boxes,boxCoverShare:boxes.length&&isNum(cells)?r2(boxes.reduce((n,b)=>n+b.cellsCovered,0)/cells):null,
  overlaps:{hiddenInLink:pods.filter(p=>p.hidden&&p.link).length,linkedBehindLock:locks.reduce((n,k)=>n+cols[k.col].slice(k.row+1).filter(x=>x.link).length,0),hiddenBehindLock:locks.reduce((n,k)=>n+cols[k.col].slice(k.row+1).filter(x=>x.hidden).length,0),keyUnderBox:locks.filter(k=>k.keyUnderBox).length}};}

// ---- padlock blocks from the recordings: a padlock blocks its column from the tap that brings it on top to its opening ----
const lockTimes={};
for(const r of ROWS.filter(x=>x.game==='CF'&&per[x.key].kinds.locks)){const rec=recs[r.key],cols=Object.values(rec.tray).map(col=>col.map(parseToken));
 lockTimes[r.key]=rec.attempts.filter(a=>a.outcome==='won').map(a=>{const heads=cols.map(()=>0),blocks=[];const onTop=(c,t)=>{const p=cols[c][heads[c]];if(p?.lock&&!blocks.some(b=>b.col===c&&b.row===heads[c]))blocks.push({col:c,row:heads[c],from:t-a.start,colour:p.lock});};
  cols.forEach((c,i)=>onTop(i,a.start));
  const evs=[...a.taps.map(x=>({t:x.t,c:+x.column.slice(1)-1})),...(a.unlocks||[]).map(u=>({t:u.t,c:+u.column.slice(1)-1,unlock:true}))].sort((x,y)=>x.t-y.t||!!y.unlock-!!x.unlock);
  const groupsOf=new Map();cols.forEach((col,ci)=>col.forEach((p,ri)=>{if(p.link){if(!groupsOf.has(p.link))groupsOf.set(p.link,[]);groupsOf.get(p.link).push([ci,ri]);}}));
  for(const e of evs){if(e.unlock){const b=blocks.find(x=>x.col===e.c&&x.row===heads[e.c]&&x.to==null);if(b)b.to=e.t-a.start;heads[e.c]++;onTop(e.c,e.t);continue;}
   const p=cols[e.c][heads[e.c]];if(!p||p.lock)continue;const g=p.link?groupsOf.get(p.link):[[e.c,heads[e.c]]];for(const [ci] of g){heads[ci]++;onTop(ci,e.t);}}
  const end=a.end-a.start;for(const b of blocks){if(b.to==null)b.to=end;b.duration=r2(b.to-b.from);}
  let maxSim=0;for(const b of blocks){const t=b.from+1e-6;maxSim=Math.max(maxSim,blocks.filter(x=>x.from<=t&&x.to>t).length);}
  return {attempt:a.attempt,blocks:blocks.map(b=>({...b,from:r2(b.from),to:r2(b.to)})),maxSimultaneous:maxSim,end:r2(end)};});}

// ---- constraints still active by stage, and openings by stage: replay of the recorded won attempt on the model ----
const STAGES=[0,.2,.4,.6,.8];const stageOf=[];
async function stages(r){const rec=recs[r.key],l=LV[r.key];if(!rec?.model||!l.model)return null;const won=rec.attempts.find(a=>a.outcome==='won');if(!won)return null;
 const E=await engine(r.game),R=E.R,T=E.T;let level=rec.model;if(rec.models){const pick=rec.models.find(m=>l.modelNote?.startsWith(m.modelNote));if(pick)level=pick.model;}
 if(r.game==='CF')level={...level,...(D.extra3prev?.calibration||{})};
 let cmds;if(r.game==='v8E')cmds=won.commands;else{const tray=level.tray,pods=level.pods,heads=tray.map(()=>0);cmds=[];
  const evs=[...won.taps.map(x=>({t:x.t,c:+x.column.slice(1)-1})),...(won.unlocks||[]).map(u=>({t:u.t,c:+u.column.slice(1)-1,unlock:true}))].sort((x,y)=>x.t-y.t||!!y.unlock-!!x.unlock);
  for(const e of evs){if(e.unlock){heads[e.c]++;cmds.push({t:Math.round((e.t-won.start)*1000),action:{type:'unlock',column:e.c}});continue;}const p=tray[e.c][heads[e.c]];if(p==null||pods[p].lock)return null;const g=pods[p].group?pods.map((x,i)=>i).filter(i=>pods[i].group===pods[p].group):[p];for(const x of g)heads[tray.findIndex(s=>s.includes(x))]++;cmds.push({t:Math.round((e.t-won.start)*1000),action:{type:'tap',pod:g[0]}});}}
 const t=new T.Timeline(R.create(level,level.capacity||5)),total=level.pods.reduce((n,p)=>n+(p.count||0),0),groupsLeft=s=>{const seen=new Set();for(const [c,stack] of s.level.tray.entries())for(const p of stack.slice(s.heads[c])){const g=s.level.pods[p].group;if(g)seen.add(g);}return seen.size;};
 const snap=()=>{const s=t.state;return {hidden:s.level.pods.filter((p,i)=>p.mystery&&!s.revealed[i]&&s.level.tray.some((st,c)=>st.indexOf(i)>=s.heads[c])).length,links:groupsLeft(s),locks:s.level.tray.reduce((n,st,c)=>n+st.slice(s.heads[c]).filter(p=>s.level.pods[p].lock).length,0),boxes:(s.objN?Array.from(s.objN).filter(x=>x>0).length:0)};};
 const out=STAGES.map(()=>null);let k=0;const opens=STAGES.map(()=>({hidden:0,locks:0,boxes:0}));let prev=snap();
 const visit=()=>{const share=t.processedCells/total;while(k<STAGES.length&&share>=STAGES[k]-1e-9){out[k]=snap();k++;}const now=snap(),st=Math.max(0,k-1);
  if(now.hidden<prev.hidden)opens[st].hidden+=prev.hidden-now.hidden;if(now.locks<prev.locks)opens[st].locks+=prev.locks-now.locks;if(now.boxes<prev.boxes)opens[st].boxes+=prev.boxes-now.boxes;prev=now;};
 visit();for(const c of cmds){while(t.pending&&t.steps[0].at<=c.t){t.advance(t.steps[0].at-t.now);visit();}if(c.t>t.now)t.advance(c.t-t.now);
  if(c.action.type==='unlock'){t.forceUnlock?.(c.action.column);visit();continue;}let g=0;while(t.check(c.action)&&t.pending&&g++<3000){t.advance(Math.max(1,Math.min(100,t.steps[0].at-t.now)));visit();}if(!t.check(c.action))t.commit(c.action);visit();}
 while(t.pending){t.advance(t.steps[0].at-t.now);visit();}
 return {source:r.game==='v8E'?'точный повтор':'модель (приближённо)',active:out.map(x=>x?KINDS.filter(kk=>(x[kk]||0)>0).length:null),remaining:out,opens};}
// the calibrated Colony Flow speed, if fitted
const cal=fs.existsSync(path.join(root,'tools/level-stats/out/calibrate.json'))?JSON.parse(fs.readFileSync(path.join(root,'tools/level-stats/out/calibrate.json'),'utf8')):null;
D.extra3prev={calibration:cal?{gapScale:cal.best.gap,tripScale:cal.best.trip,actScale:cal.best.act,releaseLagMs:cal.best.release}:{}};
const stageData={};for(const r of ROWS.filter(x=>x.game!=='v9')){try{stageData[r.key]=await stages(r);}catch(e){stageData[r.key]={error:e.message};}}
delete D.extra3prev;

// ---- aggregates ----
const games=['CF','v8E','v9'],diffs=['Normal','Hard','Super Hard'];
const combos={};for(const g of games){const c={};for(const [k,p] of Object.entries(per).filter(([,p])=>p.game===g)){const key=p.combo;c[key]=c[key]||{all:0,Normal:0,Hard:0,'Super Hard':0};c[key].all++;c[key][p.difficulty]++;}combos[g]=c;}
const kindCount=g=>Object.fromEntries([0,1,2,3,4].map(n=>[n,Object.values(per).filter(p=>p.game===g&&p.count===n).length]));
// each kind's amount: alone, with one other kind, with two or three others
const amount=(p,k)=>k==='hidden'?p.hidden.count:k==='links'?p.links.groups:k==='locks'?p.locks.length:p.boxes.length;
const byCompany=Object.fromEntries(games.map(g=>[g,Object.fromEntries(KINDS.map(k=>{const ps=Object.values(per).filter(p=>p.game===g&&p.kinds[k]);return [k,{alone:sum(ps.filter(p=>p.count===1).map(p=>amount(p,k))),pair:sum(ps.filter(p=>p.count===2).map(p=>amount(p,k))),threeOrFour:sum(ps.filter(p=>p.count>=3).map(p=>amount(p,k)))}];}))]));
// does a kind's amount fall when another kind is added (levels that have kind k: with vs without kind j)
const withWithout=Object.fromEntries(games.map(g=>[g,KINDS.flatMap(k=>KINDS.filter(j=>j!==k).map(j=>{const ps=Object.values(per).filter(p=>p.game===g&&p.kinds[k]);const a=ps.filter(p=>p.kinds[j]).map(p=>amount(p,k)),b=ps.filter(p=>!p.kinds[j]).map(p=>amount(p,k));return {kind:KIND_RU[k],other:KIND_RU[j],with:sum(a),without:sum(b)};})).filter(x=>x.with.n&&x.without.n)]));
// introduction (first observation in the sample, not a confirmed introduction), use after it, amount where present
const intro=Object.fromEntries(games.map(g=>{const ps=Object.values(per).filter(p=>p.game===g).sort((a,b)=>a.level-b.level);return [g,Object.fromEntries(KINDS.map(k=>{const first=ps.find(p=>p.kinds[k]);if(!first)return [k,null];const after=ps.filter(p=>p.level>=first.level);
  const firstCombined=ps.find(p=>p.kinds[k]&&p.count>=2);const seq=ps.filter(p=>p.kinds[k]).map(p=>p.level);
  return [k,{firstSeen:first.level,levelsAfter:after.length,usedAfter:after.filter(p=>p.kinds[k]).length,useShare:r2(after.filter(p=>p.kinds[k]).length/after.length),appearancesBeforeCombined:firstCombined?seq.indexOf(firstCombined.level):null,firstCombinedAt:firstCombined?.level??null,amountWherePresent:sum(after.filter(p=>p.kinds[k]).map(p=>amount(p,k)))}];}))];}));
// the sequence of combinations level after level (the levels in the sample, in order)
const sequence=Object.fromEntries(games.map(g=>[g,Object.values(per).filter(p=>p.game===g).sort((a,b)=>a.level-b.level).map(p=>[p.level,p.difficulty[0]+(p.difficulty.includes('Super')?'S':''),p.count,p.combo])]));
// details of each kind (Colony Flow and ours)
const hiddenAgg=Object.fromEntries(games.map(g=>{const ps=Object.values(per).filter(p=>p.game===g);const all=ps.reduce((n,p)=>n+p.capsules,0),h=ps.reduce((n,p)=>n+p.hidden.count,0);
 return [g,{capsules:all,hidden:h,share:r2(h/all),levelsWith:ps.filter(p=>p.hidden.count).length,depth:sum(ps.flatMap(p=>p.hidden.depths)),inLinks:ps.reduce((n,p)=>n+p.hidden.inLinks,0),unseenAtChoice:ps.reduce((n,p)=>n+p.hidden.unseenAtChoice,0),perLevel:sum(ps.filter(p=>p.hidden.count).map(p=>p.hidden.count))}];}));
const linkAgg=Object.fromEntries(games.map(g=>{const ps=Object.values(per).filter(p=>p.game===g);const all=ps.reduce((n,p)=>n+p.capsules,0),sizes=ps.flatMap(p=>p.links.sizes),shapes=ps.flatMap(p=>p.links.shapes);
 // how long a group holds its places: tap to release of its members, in the recorded won attempts (v8E exact replay, CF model replay)
 const holds=[];for(const p of ps){const l=LV[Object.keys(per).find(k=>per[k]===p)];const won=l.attempts.find(a=>a.outcome==='won');const m=won?.replay;if(!m?.timeline)continue;const lv=recs[l.key]?.model;if(!lv)continue;
  const tl=new Map(m.timeline.map(([i,tap,rel])=>[i,[tap,rel]]));const seen=new Set();for(const [i,pod] of lv.pods.entries()){if(!pod.group||seen.has(pod.group))continue;seen.add(pod.group);const mem=lv.pods.map((x,j)=>x.group===pod.group?j:-1).filter(j=>j>=0);const ts=mem.map(j=>tl.get(j)).filter(Boolean);if(ts.length===mem.length&&ts.every(x=>isNum(x[0])&&isNum(x[1])))holds.push(Math.max(...ts.map(x=>x[1]))-Math.min(...ts.map(x=>x[0])));}}
 return [g,{groups:sizes.length,pods:sizes.reduce((a,b)=>a+b,0),share:r2(sizes.reduce((a,b)=>a+b,0)/all),sizes:Object.fromEntries([...new Set(sizes)].sort().map(s=>[s,sizes.filter(x=>x===s).length])),sameColour:ps.reduce((n,p)=>n+p.links.sameColour,0),
  shapes:Object.fromEntries(['вертикальная','горизонтальная','диагональная'].map(s=>[s,shapes.filter(x=>x===s).length])),rowSpread:sum(ps.flatMap(p=>p.links.rowSpread)),colSpread:sum(ps.flatMap(p=>p.links.colSpread)),holdSeconds:sum(holds.map(r2)),holdSource:g==='v8E'?'точный повтор':g==='CF'?'модель по записанным нажатиям':'—'}];}));
const lockAgg={pairs:Object.values(per).filter(p=>p.game==='CF').reduce((n,p)=>n+p.locks.length,0),levels:Object.values(per).filter(p=>p.game==='CF'&&p.locks.length).length,
 depth:sum(Object.values(per).flatMap(p=>p.locks.map(k=>k.row))),podsBehind:sum(Object.values(per).flatMap(p=>p.locks.map(k=>k.below))),cellsBehind:sum(Object.values(per).flatMap(p=>p.locks.map(k=>k.belowCells))),
 keysOnEdge:Object.values(per).flatMap(p=>p.locks.filter(k=>k.key?.edge)).length,keysLocated:Object.values(per).flatMap(p=>p.locks.filter(k=>k.key)).length,keyUnderBox:Object.values(per).reduce((n,p)=>n+p.overlaps.keyUnderBox,0),
 blocks:sum(Object.values(lockTimes).flat().flatMap(a=>a.blocks.map(b=>b.duration))),blocksAtStart:Object.values(lockTimes).flat().flatMap(a=>a.blocks.map(b=>b.from)),
 maxSimultaneous:sum(Object.values(lockTimes).flat().map(a=>a.maxSimultaneous)),perLevel:lockTimes};
const boxAgg={levels:Object.values(per).filter(p=>p.game==='CF'&&p.boxes.length).length,byKind:Object.fromEntries([...new Set(Object.values(per).flatMap(p=>p.boxes.map(b=>b.kind)))].map(k=>{const bs=Object.values(per).flatMap(p=>p.boxes.filter(b=>b.kind===k));return [k,{count:bs.length,counter:sum(bs.map(b=>b.n)),cellsCovered:sum(bs.map(b=>b.cellsCovered)),colourKnown:bs.filter(b=>b.colour).length}];})),
 coverShare:sum(Object.values(per).filter(p=>p.boxCoverShare!=null).map(p=>p.boxCoverShare))};
// constraints active by stage, per game and difficulty; openings by stage
const stageAgg={};for(const g of ['CF','v8E'])for(const d of ['all',...diffs]){const ks=Object.keys(stageData).filter(k=>per[k].game===g&&(d==='all'||per[k].difficulty===d)&&stageData[k]&&!stageData[k].error);if(!ks.length)continue;
 stageAgg[g+'|'+d]={levels:ks.length,source:g==='v8E'?'точный повтор':'модель (приближённо)',active:STAGES.map((_,i)=>sum(ks.map(k=>stageData[k].active[i]))),opens:STAGES.map((_,i)=>({hidden:ks.reduce((n,k)=>n+stageData[k].opens[i].hidden,0),locks:ks.reduce((n,k)=>n+stageData[k].opens[i].locks,0),boxes:ks.reduce((n,k)=>n+stageData[k].opens[i].boxes,0)}))};}
// ---- difficulty: comparable boards and stage, correlations, random loss reasons ----
const rank={Normal:0,Hard:1,'Super Hard':2};
const spearman=(x,y)=>{const pts=x.map((v,i)=>[v,y[i]]).filter(([a,b])=>isNum(a)&&isNum(b));if(pts.length<5)return null;const rk=a=>{const s=a.map((v,i)=>[v,i]).sort((p,q)=>p[0]-q[0]);const r=Array(a.length);for(let i=0;i<s.length;){let j=i;while(j+1<s.length&&s[j+1][0]===s[i][0])j++;for(let k=i;k<=j;k++)r[s[k][1]]=(i+j)/2;i=j+1;}return r;};
 const a=rk(pts.map(p=>p[0])),b=rk(pts.map(p=>p[1])),ma=a.reduce((s,v)=>s+v,0)/a.length,mb=b.reduce((s,v)=>s+v,0)/b.length;let num=0,da=0,db=0;for(let i=0;i<a.length;i++){num+=(a[i]-ma)*(b[i]-mb);da+=(a[i]-ma)**2;db+=(b[i]-mb)**2;}return {rho:r2(num/Math.sqrt(da*db||1)),n:pts.length};};
const FEAT=[['cells','клеток'],['types','цветов'],['capsules','капсул'],['columns','колонок'],['countSd','разброс чисел'],['linkSets','связок'],['hidden','скрытых'],['padlocks','замков'],['kinds','видов допов',r=>per[r.key].count],['risky','рискованных решений (модель)'],['s_random','доля побед случайного выбора (модель)'],['o_zeroFreeShare','время с полным буфером (человек)'],['wonSeconds','время победы (человек)'],['m_seconds','время маршрута (модель)']];
const corr=Object.fromEntries(games.map(g=>{const rs=ROWS.filter(r=>r.game===g);return [g,FEAT.map(([k,t,f])=>({feature:t,withDifficulty:spearman(rs.map(r=>rank[r.difficulty]),rs.map(r=>f?f(r):r[k])),withWonSeconds:spearman(rs.map(r=>r.wonSeconds),rs.map(r=>f?f(r):r[k])),withRouteSeconds:spearman(rs.map(r=>r.m_seconds),rs.map(r=>f?f(r):r[k])),withRandomWin:spearman(rs.map(r=>r.s_random),rs.map(r=>f?f(r):r[k]))}))];}));
// difficulty on comparable boards and stage
const STAGE_G=[['№1–30',r=>r.level<=30],['№31–300',r=>r.level>30&&r.level<=300],['№301+',r=>r.level>300]],SIZE_G=[['до 700 клеток',r=>isNum(r.cells)&&r.cells<700],['700–999',r=>isNum(r.cells)&&r.cells>=700&&r.cells<1000],['1000+',r=>isNum(r.cells)&&r.cells>=1000]];
const diffTable=[];for(const g of games)for(const [sl,sp] of STAGE_G)for(const [zl,zp] of SIZE_G){const rs=ROWS.filter(r=>r.game===g&&sp(r)&&zp(r));if(!rs.length)continue;const row={game:g,stage:sl,size:zl,levels:rs.length};
 for(const d of diffs){const v=rs.filter(r=>r.difficulty===d);row[d]=v.length?{n:v.length,capsules:r2(q(v.map(r=>r.capsules),.5)),types:r2(q(v.map(r=>r.types),.5)),kinds:r2(q(v.map(r=>per[r.key].count),.5)),linkShare:r2(q(v.map(r=>per[r.key].links.share),.5)),random:r2(q(v.map(r=>r.s_random),.5)),risky:r2(q(v.map(r=>r.risky),.5)),zero:r2(q(v.map(r=>r.o_zeroFreeShare),.5)),won:r2(q(v.map(r=>r.wonSeconds),.5))}:null;}
 diffTable.push(row);}
// interactions: random win share by presence of two kinds
const inter=Object.fromEntries(games.map(g=>[g,[['links','hidden'],['links','locks'],['links','boxes'],['hidden','locks']].map(([a,b])=>{const rs=ROWS.filter(r=>r.game===g&&isNum(r.s_random));const cell=(x,y)=>{const v=rs.filter(r=>per[r.key].kinds[a]===x&&per[r.key].kinds[b]===y);return {n:v.length,random:r2(q(v.map(r=>r.s_random),.5)),risky:r2(q(v.map(r=>r.risky),.5))};};
 return {a:KIND_RU[a],b:KIND_RU[b],none:cell(false,false),onlyA:cell(true,false),onlyB:cell(false,true),both:cell(true,true)};}).filter(x=>x.both.n||x.onlyA.n||x.onlyB.n)]));
// random play with loss reasons
const randDir=path.join(root,'tools/level-stats/out/random');const rand={};if(fs.existsSync(randDir))for(const f of fs.readdirSync(randDir)){const x=JSON.parse(fs.readFileSync(path.join(randDir,f),'utf8'));rand[x.key]=x;}
const randAgg={};for(const g of games)for(const d of ['all',...diffs]){const ks=Object.keys(rand).filter(k=>per[k]?.game===g&&(d==='all'||per[k].difficulty===d));if(!ks.length)continue;const reasons={};let runs=0,wins=0;for(const k of ks){runs+=rand[k].runs;wins+=rand[k].wins;for(const [why,n] of Object.entries(rand[k].reasons))reasons[why]=(reasons[why]||0)+n;}
 randAgg[g+'|'+d]={levels:ks.length,runs,wins,winShare:r2(wins/runs),reasons,clearedAtLoss:sum(ks.flatMap(k=>rand[k].clearedAtLoss))};}
// stage measures by difficulty (human: CF video, v8E exact replay; v9: model player)
const stageBuf={};for(const g of games)for(const d of diffs){const ks=ROWS.filter(r=>r.game===g&&r.difficulty===d).map(r=>r.key);const ms=ks.map(k=>{const l=LV[k],won=l.attempts.find(a=>a.outcome==='won');return g==='CF'?won?.video:g==='v8E'?won?.replay:l.checks?.routeLive;}).filter(Boolean);if(!ms.length)continue;
 stageBuf[g+'|'+d]={attempts:ms.length,source:g==='CF'?'видео':g==='v8E'?'точный повтор':'модель: маршрут с ожиданием',stages:[0,1,2,3,4].map(i=>{const s=ms.map(m=>(m.segmentsByCleared||[])[i]).filter(x=>x&&x.reached);return {meanFree:sum(s.map(x=>x.meanFree)),minFree:sum(s.map(x=>x.minFree)),zero:sum(s.map(x=>x.zeroFreeShare)),one:sum(s.map(x=>x.oneFreeShare)),taps:sum(s.map(x=>x.taps)),stalled:sum(s.map(x=>x.stalled))};})};}
// after 80 % by difficulty (human)
const after80={};for(const c of D.extra?.choices||[]){const p=per[c.key];if(!p)continue;const k=p.game+'|'+p.difficulty;(after80[k]=after80[k]||[]).push(c);}
const after80Agg=Object.fromEntries(Object.entries(after80).map(([k,v])=>[k,{attempts:v.length,tapsAfter80:sum(v.map(x=>x.tapsAfter80)),clearedAtLastTap:sum(v.map(x=>x.clearedAtLastTap)),lastTapToWin:sum(v.map(x=>x.game==='v8E'?x.realLastTapToWin:x.lastTapToWin))}]));
D.extra3={built:'tools/level-stats/analysis3.mjs',per,combos,kindCount:Object.fromEntries(games.map(g=>[g,kindCount(g)])),byCompany,withWithout,intro,sequence,hiddenAgg,linkAgg,lockAgg,boxAgg,stageAgg,corr,diffTable,inter,randAgg,stageBuf,after80Agg,calibration:cal,
 durations:fs.existsSync(path.join(root,'tools/level-stats/out/durations.json'))?(({runs,...rest})=>rest)(JSON.parse(fs.readFileSync(path.join(root,'tools/level-stats/out/durations.json'),'utf8'))):null};
fs.writeFileSync(file,JSON.stringify(D));
console.log('combos',JSON.stringify(combos));console.log('kinds',JSON.stringify(D.extra3.kindCount));console.log('intro CF',JSON.stringify(intro.CF));
console.log('hidden',JSON.stringify(hiddenAgg));console.log('links',JSON.stringify(linkAgg));console.log('locks',JSON.stringify({...lockAgg,perLevel:undefined,blocksAtStart:undefined}));console.log('boxes',JSON.stringify(boxAgg));
console.log('stage',JSON.stringify(stageAgg));console.log('rand',JSON.stringify(randAgg));console.log('withWithout CF',JSON.stringify(withWithout.CF));console.log('inter',JSON.stringify(inter));
console.log('corr CF',JSON.stringify(corr.CF.map(c=>[c.feature,c.withDifficulty?.rho,c.withWonSeconds?.rho,c.withRandomWin?.rho])));console.log('corr v8E',JSON.stringify(corr.v8E.map(c=>[c.feature,c.withDifficulty?.rho,c.withWonSeconds?.rho,c.withRandomWin?.rho])));
