// Every studied level in one shape: Colony Flow (the owner's recordings and the colony-flow.com videos), our pilot v8E
// as the owner played it on 2026-10-24 (commit 8892a6f4) and the current v9 (content/pilot-v9). Each record keeps what
// the source says (board, tray, attempts) and, where the level can run on our engine, an engine level (`model`).
// Engines: engines.mjs. Colony Flow on our engine is an approximation (one hole under the board, ants walk round a
// ground border; padlocks, crates, chocolate and jars as established from the videos, in tools/level-stats/cf-engine), so
// a CF level runs there only when its board and capsules agree (at most 4% of the cells recoloured or trimmed);
// `modelNote` says how it was built or why not.
import fs from 'node:fs';import path from 'node:path';import {execSync} from 'node:child_process';
export const root=path.resolve(path.dirname(new URL(import.meta.url).pathname),'../..');
const read=p=>JSON.parse(fs.readFileSync(path.join(root,p),'utf8'));
export const V8E_COMMIT='8892a6f4';
export const V8E_EXPORT='product/playtests/pilot-v8e-owner-2026-10-24/results.json';
export const CHANNELS='GPBACDVYOLMRKNSE';
const TOKEN=/^([A-Za-z#])(\d+)(\?)?(?::([a-z]))?(?:!(\w+))?$/;
export const parseToken=t=>{const k=/^#([a-z]+)$/.exec(t);if(k)return {lock:k[1]};const m=TOKEN.exec(t);if(!m)throw Error('token '+t);return {color:m[1],count:+m[2],hidden:!!m[3],link:m[4]||null};};

// ---- Colony Flow ----
// Levels with boxes, chocolate bars or counters on the board (the transcribers' mechanics notes, 2026-10-25)
const BOX_LEVELS={67:'two "100" counters',113:'chocolate 300',115:'chocolate 600, box 20',185:'chocolate 900 and 500, marker 90',192:'counters 60, 80, 90',245:'crate 43',266:'boxes 20 and 50',290:'box 500, crates 397 and 48, counter 100',350:'chocolate 700, crate 50',358:'crates 100 and 50, three counters 100',790:'crate 230'};
function cfRecords(){
 const owner=read('product/audits/colony-flow-rounds/levels.json').levels,site=read('product/audits/colony-flow-rounds/site/levels.json').levels,out=[];
 for(const L of owner){
  const rounds=(L.rounds||[]).map(r=>({attempt:r.attempt,outcome:r.outcome,start:r.start,end:r.end,videos:r.videos||(L.source.video||'').split(', '),
   taps:r.taps.map(t=>({t:t.t,column:t.column,note:t.note||null})),unlocks:[],events:r.events||[]}));
  out.push({key:'CF-'+L.level,game:'CF',set:'owner',level:L.level,difficulty:L.difficulty,title:L.title,app:L.source.app,
   source:{videos:[...new Set(rounds.flatMap(r=>r.videos))],recorded:L.source.recorded,note:L.source.note||null},
   board:{w:L.board.w,h:L.board.h,rows:L.board.rows,legend:Object.fromEntries(Object.entries(L.board.legend).map(([k,v])=>[k,v.name]))},
   tray:L.tray,capacity:L.capacity,mechanicsText:[L.linkNote,...(L.mechanics||[])].filter(Boolean),uncertain:L.uncertain||[],boxes:false,
   attempts:rounds,storyboardText:L.storyboardText||null});}
 for(const L of site){
  const text=[...(L.mechanics||[]),...(L.notes||[])];
  out.push({key:'CF-'+L.level,game:'CF',set:'site',level:L.level,difficulty:L.difficulty,title:L.title,app:'Colony Flow (colony-flow.com video)',
   source:{videos:[L.source.video.split('/').pop()],recorded:'2026-10-25 (given)',cover:L.source.cover,coverNote:L.source.coverNote||null},
   board:L.board?{w:L.board.w,h:L.board.h,rows:L.board.rows,legend:Object.fromEntries(Object.entries(L.board.legend).map(([k,v])=>[k,v.name])),fromVideo:!!L.board.fromVideo}:null,
   tray:L.tray,capacity:5,mechanicsText:text,uncertain:L.uncertain||[],boxes:BOX_LEVELS[L.level]||false,
   attempts:L.rounds.map(r=>({attempt:r.attempt,outcome:r.outcome,start:r.start,end:r.end,videos:[L.source.video.split('/').pop()],
    taps:r.taps.map(t=>({t:t.t,column:t.column,note:t.note||null})),unlocks:(r.unlocks||[]).map(u=>({t:u.t,column:u.column})),events:r.events||[]}))});}
 // L3 has six places after the forced Extra Slot booster (owner recording, about 80 s)
 for(const r of out)if(r.key==='CF-3')r.capacityNote='5 places, 6 after the forced Extra Slot booster before the first tap';
 // a level with board objects gets two reconstructions (recognition slips fixed before or after the tiles under the
 // objects are given); measure.mjs keeps the one the recorded attempt replays on
 for(const r of out){const a=cfModel(r,true),o=OBJECTS['L'+r.level];Object.assign(r,a);
  if(o?.objects?.some(x=>x.kind!=='hidden')){const b=cfModel(r,false);r.models=[a,b].filter(m=>m.model).map((m,i)=>({...m,modelNote:m.modelNote+(i||!a.model?'; tiles under the objects given before fixing recognition':'; recognition fixed before giving the tiles under the objects')}));}}
 return out;}
// Board and capsules must hold the same cells of each colour on our engine. Board "?" tiles take the colours the
// capsules have beyond the visible board; a surplus cell is recoloured to a neighbouring colour in deficit
// (recognition slips at colour edges). More than 3% recoloured, padlocks or boxes: no model.
// Board objects, keys and corrected grids from the videos (tools/colony-deep-research/rounds/site/objects.json,
// product/audits/colony-flow-rounds/site/mechanics.md). Eight cover grids repeat one row near the watermark; L266's grid
// has a margin column on each side; L358's pink arms were read as another colour.
const OBJECTS=read('tools/colony-deep-research/rounds/site/objects.json');
function corrected(r){const o=OBJECTS['L'+r.level];let rows=r.board.rows.map(s=>[...s]);const notes=[];
 const m=o?.notes?.map(n=>/has \d+ rows[^(]*\(row (\d+) doubled\)/.exec(n)).find(Boolean);
 if(m){const k=+m[1];rows.splice(k,1);notes.push(`row ${k} of the cover grid repeated: removed`);}
 if(r.level===266){rows=rows.map(x=>x.slice(1,-1));notes.push('margin columns removed');}
 if(r.level===358){const cover=read('tools/colony-deep-research/rounds/site/boards/L358.cover.json'),pink=cover.palette.reduce((b,p)=>{const d=[1,3,5].reduce((a,i,j)=>a+(parseInt(p.rgb.slice(i,i+2),16)-[0xf7,0xbf,0xe4][j])**2,0);return d<b[0]?[d,p.letter]:b;},[1e9,null])[1];
  let crows=cover.rows.map(x=>[...x]);if(m)crows.splice(+m[1],1);let n=0;crows.forEach((row,y)=>row.forEach((c,x)=>{if(c===pink){rows[y][x]='K';n++;}}));notes.push(`${n} pink cells read as another colour: set to pink`);}
 return {rows,notes,o};}
function cfModel(r,recolourFirst=true){
 if(!r.board)return {model:null,modelNote:'no board (the site cover shows another picture)'};
 const cols=Object.entries(r.tray).map(([c,col])=>col.map(parseToken));
 const {rows,notes,o}=corrected(r),h=rows.length,w=rows[0].length;
 if(cols.flat().some(p=>p.lock)&&!o?.keys?.length)return {model:null,modelNote:'padlocks without located keys'};
 // keys: their 2x2 holes hold no tiles; objects: crates and chocolate cover cells ('*'), a jar's 2x2 too
 const keyCells={};for(const k of o?.keys||[]){const hole=k.trueHole||k.hole;for(const [x,y] of hole)if(rows[y]?.[x]!==undefined)rows[y][x]='.';keyCells[k.colour]=hole;}
 const objs=[],jars=[];for(const ob of o?.objects||[]){const cells=(ob.trueCells||ob.cells||[]).filter(([x,y])=>rows[y]?.[x]!==undefined);if(!cells.length)continue;
  if(ob.kind==='hidden'){for(const [x,y] of cells)rows[y][x]='?';continue;}
  for(const [x,y] of cells)rows[y][x]='*';
  if(ob.kind==='pot')jars.push({kind:'jar',colour:ob.colour,n:ob.n,cells,at:cells[0]});else objs.push({kind:ob.kind==='door'?'crate':'chocolate',colour:ob.colour,n:ob.n,cells,under:ob.under&&Object.values(ob.under).every(v=>typeof v==='number')?ob.under:null});}
 const need={};for(const col of cols)for(const p of col)if(!p.lock)need[p.color]=(need[p.color]||0)+p.count;
 const have={};for(const s of rows)for(const c of s)if(c!=='.'&&c!=='?'&&c!=='*')have[c]=(have[c]||0)+1;
 // a jar takes units of its colour; a jar of unknown colour takes the colour most short of tiles
 for(const j of jars){if(!j.colour||!need[j.colour]){const d=Object.keys(need).map(c=>[c,need[c]-(have[c]||0)]).sort((a,b)=>b[1]-a[1])[0];j.colour=d[0];j.guessed=true;}have[j.colour]=(have[j.colour]||0)+j.n;}
 for(const c of Object.keys(have))if(!need[c])return {model:null,modelNote:`board colour ${c} has no capsules`};
 let recoloured=0,trimmed=0;const qs=[];rows.forEach((s,y)=>s.forEach((c,x)=>{if(c==='?')qs.push([x,y]);}));
 const deficit=()=>Object.keys(need).filter(c=>need[c]>(have[c]||0)),surplus=()=>Object.keys(have).filter(c=>have[c]>(need[c]||0));
 for(const [x,y] of qs){const d=deficit()[0];if(!d)return {model:null,modelNote:'more board "?" tiles than missing capsule cells'};rows[y][x]=d;have[d]=(have[d]||0)+1;}
 // recognition slips first: a cell of a colour the board has too much of, next to a colour it is short of, takes that colour
 let guard=0;while(recolourFirst&&surplus().length&&deficit().length&&guard++<5000){const s=surplus(),d=deficit();
  let best=null;for(let y=0;y<h&&!best;y++)for(let x=0;x<w&&!best;x++){if(!s.includes(rows[y][x]))continue;for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const c=rows[y+dy]?.[x+dx];if(d.includes(c)){best=[x,y,c];break;}}}
  if(!best)break;const [x,y,c]=best;have[rows[y][x]]--;have[c]=(have[c]||0)+1;rows[y][x]=c;recoloured++;}
 // then the tiles under crates and chocolate: the colours the visible board is still short of, in reading order (or as
 // the video showed)
 let underCells=0;for(const ob of objs){ob.underCells={};const order=ob.cells.slice().sort((a,b)=>a[1]-b[1]||a[0]-b[0]);let k=0;
  if(ob.under)for(const [c,n] of Object.entries(ob.under))for(let i=0;i<n&&k<order.length;i++){ob.underCells[order[k].join(',')]=c;have[c]=(have[c]||0)+1;k++;underCells++;}
  for(;k<order.length;k++){const d=deficit()[0];if(!d)break;ob.underCells[order[k].join(',')]=d;have[d]=(have[d]||0)+1;underCells++;}}
 guard=0;while((surplus().length||deficit().length)&&guard++<5000){
  const s=surplus(),d=deficit();
  if(!s.length){// the capsules hold more than the board shows: the last capsules of that colour give it back
   for(const c of d){let k=need[c]-(have[c]||0);for(let ci=cols.length-1;k>0&&ci>=0;ci--)for(let j=cols[ci].length-1;k>0&&j>=0;j--)if(cols[ci][j].color===c){const t=Math.min(k,cols[ci][j].count-1);cols[ci][j].count-=t;need[c]-=t;k-=t;trimmed+=t;}}continue;}
  if(!d.length){// the board shows more: the surplus cells farthest down the reading order become ground
   let done=false;outer2:for(let y=h-1;y>=0;y--)for(let x=w-1;x>=0;x--)if(s.includes(rows[y][x])){have[rows[y][x]]--;rows[y][x]='.';trimmed++;done=true;break outer2;}
   if(!done)return {model:null,modelNote:'board and capsules cannot be matched'};continue;}
  let best=null;for(let y=0;y<h&&!best;y++)for(let x=0;x<w&&!best;x++){if(!s.includes(rows[y][x]))continue;for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const c=rows[y+dy]?.[x+dx];if(d.includes(c)){best=[x,y,c];break;}}}
  if(!best){outer:for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(s.includes(rows[y][x])){best=[x,y,d[0]];break outer;}}
  if(!best)return {model:null,modelNote:`board and capsules differ (${s.join('')} more on the board than in capsules, not as tiles)`};
  const [x,y,c]=best;have[rows[y][x]]--;have[c]=(have[c]||0)+1;rows[y][x]=c;recoloured++;}
 const cells=rows.flat().filter(c=>c!=='.').length+underCells;
 if(recoloured+trimmed>Math.max(2,cells*.04))return {model:null,modelNote:`board and capsules differ by ${recoloured+trimmed} cells (${(100*(recoloured+trimmed)/cells).toFixed(1)}%)`};
 const letters=Object.keys(need);if(letters.length>CHANNELS.length)return {model:null,modelNote:'more colours than the engine has'};
 const map=Object.fromEntries(letters.map((c,i)=>[c,CHANNELS[i]]));
 const W=w+2,H=h+2,blank='.'.repeat(W),idx=([x,y])=>(H-2-y)*W+(x+1);
 for(const j of jars){const [x,y]=j.at;rows[y][x]='%'+j.colour;}
 const padded=[blank,...rows.map(s=>'.'+s.map(c=>c==='.'?'.':c==='*'?'*':c[0]==='%'?map[c.slice(1)]:map[c]).join('')+'.'),blank];
 const pods=[],tray=cols.map(col=>col.map(p=>{if(p.lock){const hole=keyCells[p.lock];pods.push({lock:true,count:0,label:'#'+p.lock,key:hole?hole.map(idx):[]});return pods.length-1;}
  pods.push({color:map[p.color],count:p.count,label:p.color+p.count,...(p.link?{group:p.link}:{}),...(p.hidden?{mystery:'depth'}:{})});return pods.length-1;}));
 const objects=[...objs.map(ob=>({kind:ob.kind,colour:ob.colour?map[ob.colour]:null,n:ob.n,cells:ob.cells.map(idx),under:Object.fromEntries(Object.entries(ob.underCells).map(([k,c])=>[idx(k.split(',').map(Number)),map[c]]))})),
  ...jars.map(j=>({kind:'jar',colour:map[j.colour],n:j.n,cells:j.cells.map(idx),jarAt:idx(j.at),under:{}}))];
 const level={id:r.level,title:'CF L'+r.level,subject:'cf',w:W,h:H,rowsBottomFirst:padded.slice().reverse(),entries:[Math.floor(W/2)],capacity:r.key==='CF-3'?6:r.capacity,pods,tray,difficulty:r.difficulty,objects};
 const parts=[...notes,qs.length?`${qs.length} board "?" tiles given the capsules' extra colours in reading order`:null,recoloured?`${recoloured} board cells recoloured to match the capsules`:null,trimmed?`${trimmed} cells trimmed (capsule counts or board cells) to match`:null,
  o?.keys?.length?`${o.keys.length} padlocks with their keys`:null,objs.length?`${objs.length} crates/chocolate (tiles under them: ${underCells}, colours the visible board is short of)`:null,jars.length?`${jars.length} jars${jars.some(j=>j.guessed)?' (a colour guessed)':''}`:null].filter(Boolean);
 return {model:level,colourMap:map,modelNote:'approximation: one hole under the middle of the board, ants walk round a ground border'+(parts.length?'; '+parts.join('; '):''),recoloured,trimmed,boardMysteryTiles:qs.length};}

// ---- our pilots ----
function tokensOf(level){const tray={};level.tray.forEach((stack,c)=>{tray['c'+(c+1)]=stack.map(p=>{const q=level.pods[p];return q.color+q.count+(q.mystery?'?':'')+(q.group?':'+String(q.group).toLowerCase().replace(/[^a-z]/g,'').slice(0,1)||':g':'');});});return tray;}
function boardOf(level){const rows=level.rowsBottomFirst.slice().reverse();return {w:level.w,h:level.h,rows,walls:true};}
function ours(version,levels,attemptsByLevel){
 return levels.map(L=>({key:`${version}-${L.id}`,game:version,set:version,level:L.id,difficulty:L.difficulty||'Normal',title:L.title,app:version==='v8E'?`Bloomlings pilot v8E (${V8E_COMMIT})`:'Bloomlings pilot v9 (content/pilot-v9)',
  source:{},board:boardOf(L),tray:tokensOf(L),capacity:L.capacity||5,mechanicsText:[L.hero?`bridges: ${L.hero.choices.length} choices`:null].filter(Boolean),uncertain:[],boxes:false,
  attempts:attemptsByLevel?.[L.id]||[],model:L,modelNote:'exact: the engine the level was built for'}));}
export function v8eLevels(){return JSON.parse(execSync(`git show ${V8E_COMMIT}:content/pilot-v8e/levels.json`,{cwd:root,maxBuffer:1<<26})).levels;}
export function v9Levels(){return read('content/pilot-v9/levels.json').levels;}
// The owner's v8E export: every attempt, its commands at model time and the real time of each accepted input.
export function v8eAttempts(){
 const X=read(V8E_EXPORT),by={};
 for(const a of X.attempts){const real=a.inputEvents.filter(e=>e.accepted).map(e=>e.atMs);
  (by[a.level]=by[a.level]||[]).push({attempt:(by[a.level]?.length||0)+1,outcome:a.outcome==='won'?'won':a.outcome==='jammed'?'lost':a.outcome,rawOutcome:a.outcome,
   start:0,end:a.elapsedMs/1000,commands:a.commands.map((c,i)=>({t:c.modelTimeMs,real:real[i]??null,action:c.type==='tap'?{type:'tap',pod:c.pod}:{type:c.type,choice:c.choice}})),
   speed:{fastSetting:X.settings?.fast??null,autoFastAtMs:a.autoFastAtMs??null,initial:a.config?.initialSettings?.effectiveSpeed??null},
   rating:a.rating,interest:a.interest,note:a.note||null,timeBuckets:a.timeBuckets||null});}
 return {by,exportedAt:X.exportedAt,catalogHash:X.catalogHash};}
export function allRecords(){
 const att=v8eAttempts();
 return [...cfRecords(),...ours('v8E',v8eLevels(),att.by),...ours('v9',v9Levels(),null)];}
