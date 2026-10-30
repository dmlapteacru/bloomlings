// Shared pieces of the round pipeline (product/17_LEVEL_GENERATION_RULES.md): paths, the picture manifest, the
// palette of the pilot (16 colours, 4 per hero), the rules' tables (stages, pod budgets, difficulty calendar, mechanic
// introductions), a dependency-free PNG writer and the board built around a picture.
import fs from 'node:fs';import path from 'node:path';import zlib from 'node:zlib';import {createRequire} from 'node:module';import {fileURLToPath} from 'node:url';
export const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
export const ROUNDS=process.env.ROUNDS_DIR?path.resolve(process.env.ROUNDS_DIR):path.join(root,'content/rounds');
export const APPROVED=path.join(ROUNDS,'approved'),PICTURES=path.join(ROUNDS,'pictures'),LEVELS=path.join(ROUNDS,'levels'),BANK=path.join(ROUNDS,'bank');
const P=createRequire(import.meta.url)(path.join(root,'tools/rounds/engine/palette.cjs'));
export const CODES=P.codes,PALETTE=P.palette('normal');
export const readJSON=(f,d=null)=>fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):d;
export const writeJSON=(f,o)=>{fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,JSON.stringify(o,null,1)+'\n');};
export const pid=seq=>'P'+String(seq).padStart(4,'0'),lid=n=>'L'+String(n).padStart(4,'0');
export const arg=(k,d=null)=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:d;};
export const flag=k=>process.argv.includes(k);

// ---- the picture manifest: one entry per sequence number, in the order the pictures were made ----
export const MANIFEST=path.join(PICTURES,'manifest.json');
export const manifest=()=>readJSON(MANIFEST,{format:'bloomlings-round-pictures-1',pictures:[]});
export const saveManifest=m=>{m.pictures.sort((a,b)=>a.seq-b.seq);writeJSON(MANIFEST,m);};
export const nextSeq=m=>m.pictures.reduce((n,p)=>Math.max(n,p.seq),0)+1;
export const picture=id=>readJSON(path.join(PICTURES,id+'.json'));
// which picture each placed level uses, read from content/rounds/levels/ (the truth of "in a round"): {P0007: 'L0023'}
export function usage(){const u={};if(!fs.existsSync(LEVELS))return u;
 for(const f of fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)).sort()){const L=readJSON(path.join(LEVELS,f));if(L&&L.picture)u[L.picture]=f.replace('.json','');}return u;}

// recurring motifs for the repeat guard (the owner, 2026-10-26: "не сильно повторяться, а то уже сколько рыб"): main or
// secondary, matched in a picture's title and subject
const E='(?![а-яё])';const re=x=>new RegExp(x.replace(/\$/g,E));
export const MOTIFS=[['рыбы',re('рыб(?!ак)|кит$|клоун')],['подводный мир',re('под вод|подвод|батискаф|водолаз|осьминог|аквариум|губка боб')],['маяки',re('маяк')],['космонавты и ракеты',re('космонавт|астероид|ракет|инопланет')],
 ['полив цветка',re('полива|лейк')],['роботы',re('робот|валл|бимо')],['чайники',re('чайник')],['чашки и кружки',re('чашк|кружк|какао')],['совы',re('сов[аыу]$')],['пингвины',re('пингвин')],
 ['кошки',re('(^| )(кот|кота|котик|котёнок)$|кошк')],['собаки',re('щен|собак')],['лисы',re('(^| )лис[аиёы]?$|лисён')],['медведи',re('медвед|мишк|тоторо')],['драконы',re('дракон')],['динозавры',re('динозавр')],
 ['привидения и духи',re('привиден|дух$')],['единороги',re('единорог')],['лампы и фонари',re('лампа|лампочк|фонар')],['книги',re('книг')],['ретро-техника',re('ретро|автомат|фотоаппарат|телефон|радио|проигрыват')],
 ['инструменты музыки',re('гитар|барабан|пластинк')],['торты и пончики',re('торт|пончик|капкейк')],['печенье и выпечка',re('печень|круассан|выпечк')],['сундуки',re('сундук')],['зонты',re('зонт')],
 ['воздушные шары',re('воздушн[а-яё]* шар')],['корабли и лодки',re('корабл|лодк|парусник')],['облака с лицом',re('облако спит|облачко')],['цветок в горшке',re('в горшке|горшок')],['домики-сказки',re('домик|дом в|дом на')]];
// the motifs of one picture (title and subject)
export const motifsOf=p=>{const text=`${p.title} ${p.subject||''}`.toLowerCase();return MOTIFS.filter(([,re])=>re.test(text)).map(([m])=>m);};

// ---- picture measures (§2): N = coloured coordinates, colours and their volumes ----
export function measure(pic){const vol={};let n=0;for(const r of pic.rows)for(const c of r)if(CODES.includes(c)){vol[c]=(vol[c]||0)+1;n++;}
 return {N:n,colours:Object.keys(vol).length,volumes:vol};}
// stage ranges by level number (§2); compact 200–399 cells / 3–6 colours allowed after L25
export function stageOf(level){
 if(level===1)return {cells:[181,181],colours:[3,3],fixed:'объёмы §4'};if(level===2)return {cells:[240,240],colours:[4,4],fixed:'объёмы §4'};if(level===3)return {cells:[281,281],colours:[6,6],fixed:'объёмы §4'};
 // colours raised by the owner's amendment of 2026-10-27: up to 9 on L4–9, 10 later (11 the rare exception)
 if(level<=9)return {cells:[500,600],colours:[4,9]};if(level<=24)return {cells:[550,1000],colours:[4,10]};if(level<=60)return {cells:[550,1200],colours:[5,10],compact:true};return {cells:[550,1400],colours:[5,10],compact:true};}
export function fitsStage(m,level){const s=stageOf(level),wide=c=>c===11;
 if(s.fixed)return m.N===s.cells[0]&&m.colours===s.colours[0];
 if(s.compact&&m.N>=200&&m.N<=399)return (m.colours>=3&&m.colours<=6)||wide(m.colours);
 return m.N>=s.cells[0]&&m.N<=s.cells[1]&&((m.colours>=s.colours[0]&&m.colours<=s.colours[1])||wide(m.colours));}
// the exact colour volumes of the first three levels (§4), largest first
export const FIXED_VOLUMES={1:[128,48,5],2:[120,97,19,4],3:[76,70,69,51,13,2]};
export const sizeBin=n=>n<500?'200–499':n<1000?'500–999':'1000–1400';
// pod budget and columns by area (§3): budget P by W, columns by N
export function budget(W){return W<400?[9,24]:W<700?[18,30]:W<1000?[24,40]:[30,48];}
export function columnsFor(N){return N<700?[3]:N<1000?[3,4]:[4,5];}

// ---- difficulty calendar (§5) and mechanic introductions (§9) ----
export const classOf=n=>n%15===0?'Super Hard':n%5===0?'Hard':'Normal';
export const INTRO=[['hidden',6,8],['links',11,13],['bridge',16,18],['latch',51,53],['canal',101,103],['shutters',151,153],['repair',201,203],['bud',251,253],['seed',301,303],['well',351,353],['hiddenCells',401,403],['stacks',451,453]];
export const MECH_RU={hidden:'скрытые поды',links:'связки',bridge:'мост Sprig',latch:'засов и жёлудь-ключ Twig',canal:'водный канал Drop',shutters:'деревянные створки Twig',repair:'ремонт пятого места Twig',bud:'закрытый бутон Bloom',seed:'семечко Bloom',well:'колодец Drop',hiddenCells:'скрытые клетки Drop',stacks:'стопки одного материала'};
// what the round engine (tools/rounds/engine.mjs) really plays today; the rest waits for an engine change (§14)
export const SUPPORTED=new Set(['hidden','links','bridge']);
// which mechanic a level must introduce, which may appear, and whether the level is a first meeting
export function slotOf(n){const intro=INTRO.find(([,a,b])=>n===a||n===b);
 const open=INTRO.filter(([,a,b])=>n>b).map(([m])=>m);
 return {level:n,cls:classOf(n),intro:intro?intro[0]:null,allowed:intro?[intro[0]]:n<=5?[]:open,
  randomTarget:intro||n===2||n===3?[.9,1]:n===1?[1,1]:null};}

// ---- the board around a picture: an open ring and a wall frame, the Garden Entry in the bottom middle ----
// Picture rows are top first; '.' empty, '#' an impassable strip (a stream, a fence, a stone wall), 'b' a bridge site on a
// strip. Level rows are bottom first, as the engine reads them. 17 §8 "Где стоит мост" (amendment of 2026-10-28): a strip
// may run in any direction and a bridge crosses it up, left, right (or down); the generator may lay a straight wall over
// an approved picture (overlay).
//
// overlay (optional, picture coordinates, x from the left, y from the top): {walls:[[x,y],…], sites:[[x,y],…]}; each wall
// cell becomes '#', each site 'b' (a site must be listed as, or already be, a wall cell). A wall over a colour cell removes
// that cell's work: N and W are counted on the board with the strip.
export function overlaid(pic,overlay){if(!overlay)return pic;const pw=pic.rows[0].length,ph=pic.rows.length,rows=pic.rows.map(r=>[...r]);
 const at=(c,what)=>{if(!Array.isArray(c)||!Number.isInteger(c[0])||!Number.isInteger(c[1])||c[0]<0||c[1]<0||c[0]>=pw||c[1]>=ph)throw Error(`overlay: ${what} ${JSON.stringify(c)} вне картинки ${pw}×${ph}`);return c;};
 for(const c of overlay.walls||[]){const [x,y]=at(c,'стена');rows[y][x]='#';}
 for(const c of overlay.sites||[]){const [x,y]=at(c,'место моста');if(rows[y][x]!=='#'&&rows[y][x]!=='b')throw Error(`overlay: место моста ${JSON.stringify(c)} не на стене`);rows[y][x]='b';}
 const out=rows.map(r=>r.join(''));return {...pic,rows:out,...measure({rows:out})};}
// a bridge site whose anchor cannot be told: none or several of its orthogonal neighbours are reachable from the entry
export class BridgeAnchorError extends Error{constructor(x,y,k){super(`место моста (${x}, ${y}): опора не определена — достижимых с входа соседей ${k}, нужен ровно один (17 §8)`);this.name='BridgeAnchorError';this.site=[x,y];this.reachable=k;}}
// the crossing of a bridge, from its anchor (the near-bank neighbour) to the site: anchor below → up, right → left, …
export const DIRS=[['up',0,1],['down',0,-1],['left',1,0],['right',-1,0]];
export const DIR_RU={up:'вверх',down:'вниз',left:'влево',right:'вправо'};
// a choice's crossing on a board of width w (bottom-first indices); old choices carry no dir
export const bridgeDir=(c,w)=>c.dir||(c.anchor===c.cell-w?'up':c.anchor===c.cell+w?'down':c.anchor===c.cell+1&&Math.floor(c.anchor/w)===Math.floor(c.cell/w)?'left':c.anchor===c.cell-1&&Math.floor(c.anchor/w)===Math.floor(c.cell/w)?'right':null);
//
// Ring closure: every '#' or 'b' cell on the picture's outer edge turns the ring cell directly outward into '#' (a corner
// cell closes both outward ring cells); a full-width stream row closes the ring on both sides, a full-height wall column
// at the top and the bottom, so the far part needs a bridge.
// Anchor: for each site, its orthogonal neighbour reachable from the entry when all sites are still walls (colour cells
// count as passable, they are cleared); exactly one, else BridgeAnchorError. Each choice: {cell, anchor, label, dir}.
export function boardOf(pic,overlay=null){const p=overlaid(pic,overlay),pw=p.rows[0].length,ph=p.rows.length,w=pw+4,h=ph+4,mid=Math.floor(w/2);
 const g=Array.from({length:h},(_,y)=>Array.from({length:w},(_,x)=>x===0||y===0||x===w-1||y===h-1?'#':'.'));g[h-1][mid]='.';
 const sites=[];
 p.rows.forEach((r,y)=>{for(let x=0;x<pw;x++){const c=r[x];g[y+2][x+2]=c==='b'?'#':c;if(c==='b')sites.push([x,y]);
  if(c==='#'||c==='b'){if(x===0)g[y+2][1]='#';if(x===pw-1)g[y+2][w-2]='#';if(y===0)g[1][x+2]='#';if(y===ph-1)g[h-2][x+2]='#';}}});
 const rowsBottomFirst=g.map(r=>r.join('')).reverse(),idx=(bx,by)=>(h-1-by)*w+bx,choices=[];
 if(sites.length){const seen=new Uint8Array(w*h),q=[[mid,h-1]];seen[(h-1)*w+mid]=1;
  while(q.length){const [x,y]=q.pop();for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h||seen[v*w+u]||g[v][u]==='#')continue;seen[v*w+u]=1;q.push([u,v]);}}
  for(const [x,y] of sites){const bx=x+2,by=y+2,near=DIRS.filter(([,dx,dy])=>seen[(by+dy)*w+bx+dx]);if(near.length!==1)throw new BridgeAnchorError(x,y,near.length);
   const [dir,dx,dy]=near[0];choices.push({cell:idx(bx,by),anchor:idx(bx+dx,by+dy),dir,x,y});}
  // the label says the crossing and where it stands: up/down by column, left/right by row; two equal labels by order
  const pos=c=>c.dir==='up'||c.dir==='down'?(c.x+.5<pw/3?'слева':c.x+.5>2*pw/3?'справа':'в центре'):(c.y+.5<ph/3?'сверху':c.y+.5>2*ph/3?'снизу':'в центре');
  for(const c of choices)c.label=`Мостик ${DIR_RU[c.dir]} ${pos(c)}`;
  if(choices.length===2&&choices[0].label===choices[1].label){const vert=choices[0].dir==='up'||choices[0].dir==='down',[a,b]=vert?[choices[0].x,choices[1].x]:[choices[0].y,choices[1].y];
   if(a!==b)choices.forEach((c,i)=>{const first=(i===0)===(a<b);c.label=`Мостик ${DIR_RU[c.dir]} ${vert?(first?'слева':'справа'):(first?'сверху':'снизу')}`;});}}
 const stone=[];if(overlay){const ws=[...(overlay.walls||[]),...(overlay.sites||[])],add=(bx,by)=>{const i=idx(bx,by);if(!stone.includes(i))stone.push(i);};
  for(const [x,y] of ws){add(x+2,y+2);if(x===0)add(1,y+2);if(x===pw-1)add(w-2,y+2);if(y===0)add(x+2,1);if(y===ph-1)add(x+2,h-2);}stone.sort((a,b)=>a-b);}
 return {w,h,rowsBottomFirst,entries:[mid],hero:choices.length?{id:'sprig-bridge',choices:choices.map(({cell,anchor,label,dir})=>({cell,anchor,label,dir}))}:undefined,
  ...(overlay?{overlay:{walls:overlay.walls.map(c=>[c[0],c[1]]),sites:overlay.sites.map(c=>[c[0],c[1]])},stone}:{}),
  // a split picture (bridge-scene.mjs make --mode split, 17 §8 «Одна цельная картинка», 2026-10-29): its strip is a gap in
  // the board's surface colour, not water or stone (the level carries definition.strip = 'gap' for renderers)
  ...(p.scene&&p.scene.mode==='split'?{strip:'gap'}:{})};}
// the crossing side of a level's bridge group (the wave plan's bridgeSide): the common direction of its choices, or 'two'
// when they cross in different directions or stand on different strips (8-connected '#' cells inside the picture area)
export function bridgeSide(level){if(!level||!level.hero)return null;const {w,h}=level,cs=level.hero.choices,dirs=cs.map(c=>bridgeDir(c,w));
 if(new Set(dirs).size>1)return 'two';if(cs.length<2)return dirs[0];
 const g=level.rowsBottomFirst.join(''),inside=i=>{const x=i%w,y=Math.floor(i/w);return x>=2&&y>=2&&x<w-2&&y<h-2;},comp=new Int32Array(w*h).fill(-1);
 const mark=(s,k)=>{const q=[s];comp[s]=k;while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const u=x+dx,v=y+dy,j=v*w+u;if((dx||dy)&&u>=0&&v>=0&&u<w&&v<h&&comp[j]<0&&g[j]==='#'&&inside(j)){comp[j]=k;q.push(j);}}}};
 cs.forEach((c,k)=>{if(comp[c.cell]<0)mark(c.cell,k);});return new Set(cs.map(c=>comp[c.cell])).size>1?'two':dirs[0];}
// a planned side accepts a level's crossing: 'two' = different directions or different strips, else the same direction
export const sideMatches=(side,planned)=>!planned||side===planned;
// the index among a board's bridge choices of the site at picture coordinates [x, y] (top first), or -1
export const siteChoice=(board,[x,y])=>board.hero?board.hero.choices.findIndex(c=>c.cell===(board.h-1-(y+2))*board.w+x+2):-1;

// ---- the generator's wall over an approved picture (17 §8, plan variant A) ----
// side 'up': a full-width horizontal wall row, the closed part above it, bridges cross up; 'left' / 'right': a full-height
// vertical wall column, the closed part on that side (the Garden Entry stays on the open side), bridges cross sideways;
// 'two': an L-shaped wall closing a top corner, one site on each arm (one crossing up, one sideways). The closed part
// holds 25–45 % of the colour cells left after the wall. Two sites on the wall, at least 3 cells apart, not on the
// picture's edge or an L's corner; each site's near-bank anchor is a colour cell (so the bridge is not reachable at the
// start, §8) and its far cell is a colour or open cell. Preferred: approaches of different materials, far cells of
// different materials lying deep in the silhouette (colour depth from the closed part's ring, up to 3), a share near
// 35 %, few colour cells under the wall, sites at least half the wall apart. Deterministic by seed (it breaks near ties);
// returns {walls, sites, side, share, removed} (the level stores only {walls, sites}) or null.
export function wallOverlay(pic,side,seed=1){const rows=pic.rows,pw=rows[0].length,ph=rows.length,ex=Math.floor((pw+4)/2)-2;
 if(rows.join('').includes('b')||pw<7||ph<7)return null;
 let s=(seed>>>0)||1;const r=()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};
 const at=(x,y)=>x<0||y<0||x>=pw||y>=ph?null:rows[y][x],colour=c=>c!==null&&CODES.includes(c),total=measure(pic).N;
 const walls=[];// {cells, closed(x,y), arms:[{cells:[[x,y]...], dx, dy}]} — a site's anchor at (x+dx, y+dy), far cell at (x-dx, y-dy)
 if(side==='up')for(let y0=2;y0<=ph-3;y0++)walls.push({cells:[...Array(pw).keys()].map(x=>[x,y0]),closed:(x,y)=>y<y0,arms:[{cells:[...Array(pw-2).keys()].map(i=>[i+1,y0]),dx:0,dy:1}]});
 if(side==='left')for(let x0=2;x0<ex;x0++)walls.push({cells:[...Array(ph).keys()].map(y=>[x0,y]),closed:(x,y)=>x<x0,arms:[{cells:[...Array(ph-2).keys()].map(i=>[x0,i+1]),dx:1,dy:0}]});
 if(side==='right')for(let x0=ex+1;x0<=pw-3;x0++)walls.push({cells:[...Array(ph).keys()].map(y=>[x0,y]),closed:(x,y)=>x>x0,arms:[{cells:[...Array(ph-2).keys()].map(i=>[x0,i+1]),dx:-1,dy:0}]});
 if(side==='two')for(let y0=2;y0<=ph-3;y0++)for(let x0=2;x0<=pw-3;x0++)for(const tl of [true,false]){
  const hx=tl?[...Array(x0+1).keys()]:[...Array(pw-x0).keys()].map(i=>x0+i),vy=[...Array(y0).keys()];
  walls.push({cells:[...hx.map(x=>[x,y0]),...vy.map(y=>[x0,y])],closed:tl?(x,y)=>x<x0&&y<y0:(x,y)=>x>x0&&y<y0,
   arms:[{cells:hx.filter(x=>x!==x0&&x>0&&x<pw-1).map(x=>[x,y0]),dx:0,dy:1},{cells:vy.filter(y=>y>0).map(y=>[x0,y]),dx:tl?1:-1,dy:0}]});}
 let best=null;
 for(const wl of walls){const on=new Set(wl.cells.map(([x,y])=>x+','+y));let removed=0,closed=0;
  for(let y=0;y<ph;y++)for(let x=0;x<pw;x++){if(!colour(at(x,y)))continue;if(on.has(x+','+y))removed++;else if(wl.closed(x,y))closed++;}
  const left=total-removed,share=left?closed/left:0;if(share<.25||share>.45)continue;
  // valid sites of each arm: a colour anchor on the near bank, a colour or open far cell, not under the wall
  const valid=wl.arms.map(a=>a.cells.filter(([x,y])=>{const an=at(x+a.dx,y+a.dy),far=at(x-a.dx,y-a.dy);return colour(an)&&!on.has((x+a.dx)+','+(y+a.dy))&&far!==null&&far!=='#'&&!on.has((x-a.dx)+','+(y-a.dy));}).map(([x,y])=>({x,y,a})));
  const pairs=[];if(valid.length===1){const v=valid[0];for(let i=0;i<v.length;i++)for(let j=i+1;j<v.length;j++)pairs.push([v[i],v[j]]);}else for(const p of valid[0])for(const q of valid[1])pairs.push([p,q]);
  const len=wl.cells.length;if(!pairs.length)continue;
  // colour depth of the closed part from its own ring (0-1 search: a colour cell costs 1): a bridge landing deep in the
  // silhouette opens its own front, a landing on open background reaches the ring at once and both bridges play alike
  const dep=new Float64Array(pw*ph).fill(Infinity),dq=[],inC=(x,y)=>x>=0&&y>=0&&x<pw&&y<ph&&wl.closed(x,y)&&!on.has(x+','+y);
  for(let y=0;y<ph;y++)for(let x=0;x<pw;x++)if(inC(x,y)&&(x===0||y===0||x===pw-1||y===ph-1)){const c=colour(at(x,y))?1:0;if(c<dep[y*pw+x]){dep[y*pw+x]=c;c?dq.push([x,y]):dq.unshift([x,y]);}}
  while(dq.length){const [x,y]=dq.shift();for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(!inC(u,v)||at(u,v)==='#')continue;const c=colour(at(u,v))?1:0,nd=dep[y*pw+x]+c;if(nd<dep[v*pw+u]){dep[v*pw+u]=nd;c?dq.push([u,v]):dq.unshift([u,v]);}}}
  const depthOf=(x,y)=>Math.min(3,dep[y*pw+x]);
  for(const [p,q] of pairs){const sep=Math.abs(p.x-q.x)+Math.abs(p.y-q.y);if(sep<3)continue;
   const ap=at(p.x+p.a.dx,p.y+p.a.dy),aq=at(q.x+q.a.dx,q.y+q.a.dy),fp=at(p.x-p.a.dx,p.y-p.a.dy),fq=at(q.x-q.a.dx,q.y-q.a.dy);
   // each bridge should open the far part in its own order (so one bridge alone can decide, §8): sites far apart, far
   // cells of different materials
   const score=(ap===aq?1:0)+(fp===fq?.5:0)+.4*((3-depthOf(p.x-p.a.dx,p.y-p.a.dy))+(3-depthOf(q.x-q.a.dx,q.y-q.a.dy)))+2*Math.abs(share-.35)+2*removed/total+2*Math.max(0,len/2-sep)/len+.15*r();
   if(!best||score<best.score)best={score,wl,sites:[[p.x,p.y],[q.x,q.y]].sort((a,b)=>a[1]-b[1]||a[0]-b[0]),share,removed};}}
 if(!best)return null;
 const out={walls:best.wl.cells.map(c=>[c[0],c[1]]),sites:best.sites,side,share:Math.round(best.share*1000)/1000,removed:best.removed};
 // the board must resolve every anchor (it does for a straight wall over a picture without strips; checked anyway)
 try{boardOf(pic,out);}catch(e){if(e instanceof BridgeAnchorError)return null;throw e;}
 return out;}

// ---- the owner-approved mechanics breakdown of a wave (17 §13 step 1, amendment of 2026-10-28) ----
// content/rounds/runs/<wave>/mechanics.json: per level recipe (lead first), lead, add, bridgeSide (up|left|right|two) and
// strip (new: a picture with its own strip; overlay: a wall over an approved picture; gap: one whole picture split by a
// neutral strip in the board's colour, bridge-scene.mjs make --mode split). Only an approved plan counts.
export function mechanicsPlans(){const dir=path.join(ROUNDS,'runs');if(!fs.existsSync(dir))return [];
 return fs.readdirSync(dir).sort().map(d=>readJSON(path.join(dir,d,'mechanics.json'))).filter(p=>p&&p.approved&&Array.isArray(p.wave)&&Array.isArray(p.levels));}
// picture repeats (the owner, 2026-10-29): a theme family, and a motif, at most once in a wave and not in the 20 levels
// before it; the levels a picture at n is compared with (a level outside any wave plan: the 20 levels on either side)
export function repeatWindow(n,plans=mechanicsPlans()){for(const p of plans)if(n>=p.wave[0]&&n<=p.wave[1])return [p.wave[0]-20,p.wave[1]];return [n-20,n+20];}
export function planOf(n,plans=mechanicsPlans()){for(const p of plans)if(n>=p.wave[0]&&n<=p.wave[1])return p.levels.find(l=>l.level===n)||null;return null;}
// a candidate fits a planned position: the same recipe and, for a bridge, the planned crossing side
// (a gap position, 17 §8 «Одна цельная картинка» of 2026-10-29: only a level on a split picture, definition.strip 'gap')
export const fitsPlan=(c,plan)=>!plan||((c.recipe||'none')===plan.recipe&&(!plan.bridgeSide||sideMatches(c.bridgeSide??bridgeSide(c.level||c.definition),plan.bridgeSide))
 &&(plan.strip!=='gap'||(c.level||c.definition||{}).strip==='gap'));

// ---- PNG writer (RGB) and a 3×5 digit font for labels ----
const crcT=new Uint32Array(256).map((_,n)=>{let c=n;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;return c>>>0;});
const crc=b=>{let c=0xffffffff;for(const x of b)c=crcT[(c^x)&255]^(c>>>8);return (c^0xffffffff)>>>0;};
const chunk=(t,d)=>{const len=Buffer.alloc(4);len.writeUInt32BE(d.length);const td=Buffer.concat([Buffer.from(t),d]);const c=Buffer.alloc(4);c.writeUInt32BE(crc(td));return Buffer.concat([len,td,c]);};
export function png(W,H,px){const raw=Buffer.alloc((W*3+1)*H);for(let y=0;y<H;y++){raw[y*(W*3+1)]=0;px.copy(raw,y*(W*3+1)+1,y*W*3,(y+1)*W*3);}
 const ih=Buffer.alloc(13);ih.writeUInt32BE(W,0);ih.writeUInt32BE(H,4);ih[8]=8;ih[9]=2;
 return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ih),chunk('IDAT',zlib.deflateSync(raw)),chunk('IEND',Buffer.alloc(0))]);}
export const FONT={'A':'010101111101101','B':'110101110101110','C':'011100100100011','D':'110101101101110','0':'111101101101111','1':'010110010010111','2':'111001111100111','3':'111001111001111','4':'101101111001001','5':'111100111001111','6':'111100111101111','7':'111001001001001','8':'111101111101111','9':'111101111001111','·':'000000010000000','/':'001001010100100','x':'000101010101000',' ':'000000000000000','#':'101111101111101','?':'111001011000010','!':'010010010000010'};
export const rgbOf=h=>[parseInt(h.slice(1,3),16),parseInt(h.slice(3,5),16),parseInt(h.slice(5,7),16)];
