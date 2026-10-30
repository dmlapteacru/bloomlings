// Bridge scenes (product/17_LEVEL_GENERATION_RULES.md §8 "Где стоит мост", the owner, 2026-10-28: "можно просто взять
// какую-нибудь картинку поменьше и вокруг неё строить мосты и стены … нужно подшаманить с лотами в начале, чтобы был только
// один выигрышный мост … возможно, движок отдельный, который придумывает эти мосты"): a new picture composed from a
// smaller approved one, so that exactly one Sprig bridge wins (or both win and one is clearly better), as the old pilots'
// L22 "Домик у ручья" and L29 "Садовый фонарь" did (tools/gameplay-pilot-v2/art.mjs).
//
// The composition (scene): the picture is the FAR side, cropped to its silhouette, its empty space kept (z '.') or filled
// with one environment colour, framed by '#' on every edge that faces the board's ring, so boardOf's ring closure leaves
// no path round it. Between far and near stands a wall with two 'b' sites. Right behind each site, on the far side, a
// strip of its own colour (2 rows), a '#' divider between the two: the strip is the gate of its bridge, nothing on the
// far side is reachable over that bridge until its strip is eaten. On the near bank, a bed before each site (its
// approach, colour cells to clear first, §8) and a meadow of the one extra colour (the near bank's ground). The winning
// site's bed is x and its strip y; the other site's bed is w and its strip v (the old pilots' crossing: w = y, v = x).
// The queue (levels.mjs candidate opts) leans to the winning site and keeps the pods of v deep: over the winning bridge
// the y pods come early and open the picture; over the other one the places fill with pods that find no work before
// the v pods come up, so the loss comes later, through materials and places.
//
//   up                     left (right mirrored)     two (an L closing the top-left corner)
//   ##############         ##########....            #########.....
//   #....pic.....#         #..pic.VV#....            #.pic..VV#....
//   #YYYYY##VVVVV#         #......VVbWWW.            #......VVbWWW.
//   ###b######b###         #......###....            #YYYYYY##.....
//   .XXX....WWW...         #......YYbXXX.            ###b######....
//   .XXX....WWW...         #......YY#....            ..XXX.........
//   LLLLLLLLLLLLLL         ##########....            LLLLLLLLLLLLLL
//                          LLLLLLLLLLLLLL
// up: a full-width wall; left/right: the far side in a top corner, both sites on its vertical wall (the winning one the
// lower, nearer the entry), a band of open ground below it so the Garden Entry stays in the bottom middle (full: the
// wall the whole height, the near bank wider than the far side); two: one site crossing up, one crossing left. The
// picture itself is never mirrored or redrawn.
//
// node tools/rounds/bridge-scene.mjs show <P####> --side up|left|right|two [--x C --y C --z C|. --strip 2 --divider 0
//      --bedDepth 3 --bedBreadth 3 --bedDepthY n --bedBreadthY n --pad 0 --margin 0 --ground C --fill --shield n --full]
//      the scene as text, with its measures and board check
// node tools/rounds/bridge-scene.mjs make --side up|left|right|two --kind single|better|any --cls Normal|Hard|"Super Hard"
//      --recipe bridge[,bridge+links…] --pictures P0012,P0048|--free [--level 26] [--count 1] [--scenes 40]
//      [--candidates 12] [--seed 1] [--minutes 20] [--opts '{…}'] --out content/rounds/pictures/src/scenes-NNNN.mjs [--dry]
//      searches scenes (parameters drawn by seed, or --opts) and, on each, candidates of the builder (levels.mjs candidate
//      + evaluate: balance, class by the random test, forced decisions, opening test, the bridge kind first) until --count
//      scenes pass; writes them as a draft picture module ("<title> за мостом", the source's family, source "сцена моста
//      из P####", forLevel, scene) for the owner's sheet (pictures.mjs render / sheet / approve). Never approves.
//      Evidence → <out>.json.
// node tools/rounds/bridge-scene.mjs make --mode split --from P####[,P####…] --side up|left|right|two --kind single|better|any
//      --cls Normal|Hard|"Super Hard" --recipe bridge[+links][+hidden] --level n --out content/rounds/pictures/src/split-L####.mjs
//      [--minutes 35] [--candidates 8] [--per 3] [--seed 1] [--zone a,b] [--share 0.25,0.45] [--gap-tolerance 0] [--no-hold] [--worse-min f] [--retry regex] [--restart]
//      [--again] [--dry]
//      the owner's design of 2026-10-29 (17 §8 «Одна цельная картинка», below at "split"): one whole free approved picture
//      split by an inserted neutral strip; searches split positions × site pairs × candidate queues until levels.mjs
//      evaluate passes with the planned bridge kind and class; writes the draft module ("<title> — мост", the source's
//      family and tags, scene {from, mode: 'split', side, at, sites, kind, win, lose, …}) and the evidence <out>.json
//      with the found candidate level. Resumable: the evidence keeps every split tried and a rerun skips them, so run
//      it in chunks; --share / --gap-tolerance / --no-hold (a Normal without the holding pod of §6, for a picture
//      with no deep colour) are explicit exceptions, recorded in the scene (rules). Never approves.
// node tools/rounds/bridge-scene.mjs preview <split module.mjs…> --out <dir>
//      the found level of each module on its rendered draft picture, in the placed-level shape the pilot's --preview reads
// node tools/rounds/bridge-scene.mjs adapt <module.mjs> --out <module.mjs> [--index 0,2] [--minutes 20] [--per 8]
//      a hand-drawn strip module (each picture's forLevel names its wave plan position): strips laid behind the sites
//      (stripsOver) and searched like make; writes the passing pictures as an adapted copy, the evidence beside it.
import {deltaE,deltaEHex} from './colour-distance.mjs';
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
import {siteChoice,CODES,PALETTE,measure,fitsStage,stageOf,boardOf,bridgeSide,BridgeAnchorError,picture,manifest,usage,arg,flag,columnsFor,root,DIR_RU,planOf,lid} from './lib.mjs';
import {fitness} from './fitness.mjs';import {quickShare,bridgeSingle} from './measure.mjs';
const saved=process.argv.slice(2);process.argv.splice(2,process.argv.length,'noop');const LV=await import('./levels.mjs');process.argv.splice(2,1,...saved);

export const SIDES=['up','left','right','two'];
// environment colours for the far side's empty space, in order of preference (sky, lime, cream, moss, leaf, blush), and
// for the near bank's meadow (lime, leaf, moss first)
export const ENV='SLCMGKEY',MEADOW='LGMCKSEY';
const rngOf=seed=>{let s=(seed>>>0)||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};};

// ---- the far picture: cropped to its silhouette, the outer empty space filled with z, padded with z ----
export function farPicture(pic,{z,pad=0}){const rows=pic.rows,w=rows[0].length,h=rows.length;let x0=w,y0=h,x1=-1,y1=-1;
 for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(CODES.includes(rows[y][x])){x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);}
 if(x1<0)throw Error('пустая картинка');
 if(/[#b]/.test(rows.join('')))throw Error('у картинки уже есть полоса — сцена строится из картинки без полосы');
 const cw=x1-x0+1+2*pad,ch=y1-y0+1+2*pad,g=Array.from({length:ch},(_,y)=>Array.from({length:cw},(_,x)=>{const sx=x-pad+x0,sy=y-pad+y0;return sx<x0||sx>x1||sy<y0||sy>y1?z:rows[sy][sx];}));
 // the empty space joined to the crop's edge is environment; enclosed holes stay empty
 const q=[];if(z!=='.')for(let y=pad;y<ch-pad;y++)for(let x=pad;x<cw-pad;x++)if((x===pad||y===pad||x===cw-1-pad||y===ch-1-pad)&&g[y][x]==='.'){g[y][x]=z;q.push([x,y]);}
 while(q.length){const [x,y]=q.pop();for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=cw||v>=ch||g[v][u]!=='.')continue;g[v][u]=z;q.push([u,v]);}}
 return g;}

const grid=(w,h,c='.')=>Array.from({length:h},()=>Array(w).fill(c));
const box=(g,x0,y0,x1,y1,c)=>{for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++)if(y>=0&&x>=0&&y<g.length&&x<g[0].length)g[y][x]=c;};
const blit=(g,s,ox,oy)=>s.forEach((r,y)=>r.forEach((c,x)=>{g[oy+y][ox+x]=c;}));
const span=(mid,n)=>[mid-Math.floor((n-1)/2),mid-Math.floor((n-1)/2)+n-1];

// ---- the scene ----
// opts: side; x (the winning site's bed), y (the strip behind it), w, v (the other site's bed and strip; default y, x:
// crossed); z (the far side's empty space: '.' or an environment colour); strip (rows, 2); divider (cells the divider
// reaches into the picture past the strips); bedDepth, bedBreadth (the x bed: cells from the wall, odd breadth along it),
// bedDepthY, bedBreadthY (the w bed); pad (environment round the crop); margin (extra open ground on the near bank);
// ground (the meadow's colour, several for bands with fill), fill (the whole open near bank), shield (cells of meadow
// round the w bed only); full (left/right: the wall the whole height).
// Returns {rows (top first), side, opts, sites, win, lose} (sites in picture coordinates).
export function scene(pic,o){const side=o.side,S=o.strip??2,dv=o.divider??0,mg=o.margin??0,X=o.x,Y=o.y,Z=o.z,W=o.w||Y,V=o.v||X;
 // each bed: depth from the wall and breadth along it (odd); the second bed takes the first one's unless given
 const G0=o.ground?o.ground[0]:null;
 const dX=o.bedDepth??3,bX=o.bedBreadth??3,dY=o.bedDepthY??dX,bY=o.bedBreadthY??bX,D=Math.max(dX,dY),B=Math.max(bX,bY);
 if(!SIDES.includes(side))throw Error(`сторона: ${SIDES.join(', ')}`);
 if(!X||!Y||!Z||X===Y||[X,Y,W,V].includes(Z)||![X,Y,W,V].every(c=>CODES.includes(c))||!(Z==='.'||CODES.includes(Z)))throw Error('цвета: x, y (и w, v) — материалы палитры, x ≠ y, z — другой материал или пустота');
 if([bX,bY].some(b=>b<3||b%2===0)||[dX,dY].some(d=>d<1))throw Error('грядка: ширина нечётная от 3, глубина от 1');
 if(side==='right'){const m=scene({...pic,rows:pic.rows.map(r=>[...r].reverse().join(''))},{...o,side:'left'});
  const rows=m.rows.map(r=>[...r].reverse().join('')),w=rows[0].length;const mir=([x,y])=>[w-1-x,y];return {...m,side:'right',opts:{...o},rows,sites:m.sites.map(mir),win:mir(m.win),lose:mir(m.lose),strips:m.strips.map(([x0,y0,x1,y1,c])=>[w-1-x1,y0,w-1-x0,y1,c])};}
 const P=farPicture(pic,{z:Z,pad:o.pad??0}),pb=P[0].length,ph=P.length;let g,sites,near,bedY,strips;
 const apart=([a0,a1],[b0,b1],lo,hi)=>{if(a0<lo||b1>hi||a1+1>=b0)throw Error('грядки не помещаются у стены');};
 if(side==='up'){const pw=pb+2,yw=ph+S+1,H=yw+1+D+mg;g=grid(pw,H);
  box(g,0,0,pw-1,yw-1,'#');blit(g,P,1,1);
  const cL=Math.floor(pw/2)-1;box(g,1,ph+1,cL-1,ph+S,Y);box(g,cL+2,ph+1,pw-2,ph+S,V);strips=[[1,ph+1,cL-1,ph+S,Y],[cL+2,ph+1,pw-2,ph+S,V]];box(g,cL,ph+1-dv,cL+1,ph+S,'#');
  const xL=Math.max(1+Math.floor((B-1)/2),Math.round((pw-1)/4)),xR=pw-1-xL;if(xL>=cL-1)throw Error('картинка слишком узкая для двух мест');
  box(g,0,yw,pw-1,yw,'#');sites=[[xL,yw],[xR,yw]];
  const a=span(xL,bX),b=span(xR,bY);apart(a,b,0,pw-1);box(g,a[0],yw+1,a[1],yw+dX,X);box(g,b[0],yw+1,b[1],yw+dY,W);bedY=[b[0],yw+1,b[1],yw+dY];
  near=(x,y)=>y>yw;if(o.ground)box(g,0,yw+D+1,pw-1,H-1,G0);}
 else if(side==='left'){const F=ph+2,xw=1+pb+S;
  // the Garden Entry stands in the bottom middle of the board. Default: the far side closes the top-left corner (its
  // bottom edge a plain wall, both sites on the vertical wall) and a band of open ground below it leads to the near bank,
  // so the board stays compact; full: the wall runs the whole height and the near bank must be wider than the far side
  const band=1+mg;let pw=xw+1+D+1+mg,H=F+band;if(o.full){H=F;while(Math.floor((pw+4)/2)-2<=xw+mg+1)pw++;}g=grid(pw,H);
  box(g,0,0,xw,F-1,'#');blit(g,P,1,1);
  const rD=Math.floor(F/2)-1;box(g,pb+1,1,pb+S,rD-1,V);box(g,pb+1,rD+2,pb+S,F-2,Y);strips=[[pb+1,1,pb+S,rD-1,V],[pb+1,rD+2,pb+S,F-2,Y]];box(g,pb+1-dv,rD,pb+S,rD+1,'#');
  const yU=Math.max(1+Math.floor((B-1)/2),Math.round((F-1)/4)),yD=F-1-yU;if(yU>=rD-1)throw Error('картинка слишком низкая для двух мест');
  if(o.full)box(g,xw,F,xw,H-1,'#');sites=[[xw,yD],[xw,yU]];
  // the winning site is the lower one, nearer the entry (its bed is reached first), the other the upper one
  const a=span(yU,bY),b=span(yD,bX);apart(a,b,0,H-1);box(g,xw+1,b[0],xw+dX,b[1],X);box(g,xw+1,a[0],xw+dY,a[1],W);bedY=[xw+1,a[0],xw+dY,a[1]];
  near=(x,y)=>x>xw||y>=F;if(o.ground){if(o.full)box(g,pw-1-mg,0,pw-1,H-1,G0);else{box(g,0,H-1,pw-1,H-1,G0);box(g,pw-1-mg,0,pw-1,H-1,G0);}}}
 else{// two: the far box in the top-left corner, an L wall (a horizontal leg crossing up, a vertical leg crossing left)
  const xw=1+pb+S,yw=1+ph+S,pw=xw+1+D+1+mg,H=yw+1+D+1+mg;g=grid(pw,H);
  box(g,0,0,xw,yw,'#');blit(g,P,1,1);
  box(g,1,ph+1,pb,ph+S,Y);box(g,pb+1,1,pb+S,ph,V);strips=[[1,ph+1,pb,ph+S,Y],[pb+1,1,pb+S,ph,V]];
  const xA=Math.max(1+Math.floor((bX-1)/2),Math.round(pb/2)),yB=Math.max(1+Math.floor((bY-1)/2),Math.round(ph/2));
  if(xA>pb-1||yB>ph-1)throw Error('картинка слишком мала для двух мест');
  if(dv)box(g,pb+1-dv,ph+1-dv,pb,ph,'#');
  sites=[[xA,yw],[xw,yB]];
  const a=span(xA,bX),b=span(yB,bY);if(a[0]<0||a[1]>pw-1||b[0]<0||b[1]>H-1)throw Error('грядки не помещаются у стены');
  box(g,a[0],yw+1,a[1],yw+dX,X);box(g,xw+1,b[0],xw+dY,b[1],W);bedY=[xw+1,b[0],xw+dY,b[1]];
  near=(x,y)=>x>xw||y>yw;if(o.ground){box(g,0,H-1-mg,pw-1,H-1,G0);box(g,pw-1-mg,0,pw-1,H-1,G0);}}
 // fill: the whole open near bank is ground (beds then open only after their ground is eaten, as on the old pilots)
 // (several ground colours: the near bank in vertical bands of those colours, from the left)
 if(o.ground&&o.fill){const n=o.ground.length,W0=g[0].length;g.forEach((r,y)=>r.forEach((c,x)=>{if(c==='.'&&near(x,y))r[x]=o.ground[Math.min(n-1,Math.floor(x*n/W0))];}));}
 // shield: ground round the y bed only (the approach of the losing bridge), so the winning bridge (its bed x open on
 // the near bank) tends to be offered first and the other one later
 if(o.ground&&o.shield){const t=o.shield,[x0,y0,x1,y1]=bedY;g.forEach((r,y)=>r.forEach((c,x)=>{if(c==='.'&&near(x,y)&&x>=x0-t&&x<=x1+t&&y>=y0-t&&y<=y1+t)r[x]=G0;}));}
 for(const [x,y] of sites)g[y][x]='b';
 // win: the site of the x bed (its bridge leads to the y strip, the colour the early pods bring); lose: the other one
 return {rows:g.map(r=>r.join('')),side,opts:{...o},sites,win:sites[0],lose:sites[1],strips};}
// the index of a scene's site among the board's bridge choices (boardOf lists them in reading order)
export const choiceOf=(sc,board,site)=>siteChoice(board,site);

// ---- strips laid behind the sites of a picture that already has its wall (a drawn strip picture, or an approved one
// with the generator's wall): the far cells within `depth` of the wall take the colour of the nearer site's strip (a
// behind the winning site, b behind the other), a '#' divider where the two meet, and with border a '#' on every far
// cell of the picture's edge. win: the index (reading order) of the winning site. Returns a scene like scene() ----
export function stripsOver(pic,o){const rows=pic.rows.map(r=>[...r]),pw=rows[0].length,ph=rows.length,depth=o.depth??2,A=o.a,B=o.b,win=o.win??0;
 if(!A||!B||A===B||![A,B].every(c=>CODES.includes(c)))throw Error('полосы a, b: два разных материала палитры');
 const board=boardOf(pic);if(!board.hero||board.hero.choices.length!==2)throw Error('нужна картинка со стеной и двумя местами');
 const {w,h}=board,g=board.rowsBottomFirst.join(''),P=i=>[i%w-2,h-3-Math.floor(i/w)],near=new Uint8Array(pw*ph),q=[...board.entries],seen=new Uint8Array(w*h);for(const i of q)seen[i]=1;
 while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy,j=v*w+u;if(u<0||v<0||u>=w||v>=h||seen[j]||g[j]==='#')continue;seen[j]=1;q.push(j);}}
 for(let i=0;i<w*h;i++)if(seen[i]){const [x,y]=P(i);if(x>=0&&y>=0&&x<pw&&y<ph)near[y*pw+x]=1;}
 const sites=board.hero.choices.map(c=>P(c.cell)),wallAt=(x,y)=>x>=0&&y>=0&&x<pw&&y<ph&&(rows[y][x]==='#'||rows[y][x]==='b');
 const far=(x,y)=>x>=0&&y>=0&&x<pw&&y<ph&&!near[y*pw+x]&&!wallAt(x,y);
 // the wall the sites stand on: '#'/'b' cells 8-connected to a site
 const wall=new Set(),st=[...sites];for(const [x,y] of st)wall.add(x+','+y);
 while(st.length){const [x,y]=st.pop();for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const u=x+dx,v=y+dy;if(wallAt(u,v)&&!wall.has(u+','+v)){wall.add(u+','+v);st.push([u,v]);}}}
 const wl=[...wall].map(k=>k.split(',').map(Number)),dist=([x,y],[u,v])=>Math.hypot(x-u,y-v);
 for(let y=0;y<ph;y++)for(let x=0;x<pw;x++){if(!far(x,y))continue;const dw=Math.min(...wl.map(([u,v])=>Math.max(Math.abs(u-x),Math.abs(v-y))));if(dw>depth)continue;
  const d0=dist([x,y],sites[win]),d1=dist([x,y],sites[1-win]);rows[y][x]=Math.abs(d0-d1)<1?'#':d0<d1?A:B;}
 if(o.border)for(let y=0;y<ph;y++)for(let x=0;x<pw;x++)if(far(x,y)&&(x===0||y===0||x===pw-1||y===ph-1))rows[y][x]='#';
 const out=rows.map(r=>r.join('')),side=bridgeSide(board);return {rows:out,side,opts:{...o},sites,win:sites[win],lose:sites[1-win]};}

// ---- the board check of a scene: boardOf accepts it, two sites on the planned side, every colour cell reachable once
// a bridge stands (each bridge alone), anchors are colour cells, nothing reachable on the far side before a bridge ----
// the smallest CIEDE2000 between a strip and the picture cells beside it (2026-10-28; the palette's closest pair is 14)
export const STRIP_CONTRAST=20;
export function checkScene(sc,level=null,{maxColours=9,contrast=true}={}){const issues=[];let b=null;
 try{b=boardOf(sc);}catch(e){if(e instanceof BridgeAnchorError)return {ok:false,issues:[e.message]};throw e;}
 if(!b.hero||b.hero.choices.length!==2)issues.push('нужно ровно два места моста');
 const m=measure(sc);if(m.colours>maxColours)issues.push(`цветов ${m.colours} > ${maxColours}`);
 // a strip stands out from the picture cells it touches (the owner, 2026-10-28: a strip never merges with the picture
 // part next to it): every colour cell beside it, outside the strips, is at least STRIP_CONTRAST away in CIEDE2000
 for(const [x0,y0,x1,y1,c] of contrast?sc.strips||[]:[]){let worst=Infinity,near=null;const inStrip=(x,y)=>(sc.strips||[]).some(([a,b,e,f])=>x>=a&&x<=e&&y>=b&&y<=f);
  for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++)for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy,k=sc.rows[v]?.[u];
   if(!k||!CODES.includes(k)||inStrip(u,v))continue;const d=deltaE(c,k);if(d<worst){worst=d;near=k;}}
  if(worst<STRIP_CONTRAST)issues.push(`полоса ${c} сливается с картинкой рядом (${near}, ΔE ${worst.toFixed(1)} < ${STRIP_CONTRAST})`);}
 if(level&&!fitsStage(m,level))issues.push(`вне этапа L${level}: ${m.N} кл., ${m.colours} цв. (${stageOf(level).cells.join('–')})`);
 if(b.w>48||b.h>48)issues.push(`поле ${b.w}×${b.h} больше 48×48`);
 if(b.hero){const side=bridgeSide(b);if(side!==sc.side)issues.push(`сторона борда ${side}, нужна ${sc.side}`);
  const g=b.rowsBottomFirst.join(''),{w,h}=b,reach=open=>{const seen=new Uint8Array(w*h),q=[...b.entries];for(const i of q)seen[i]=1;
   while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy,j=v*w+u;if(u<0||v<0||u>=w||v>=h||seen[j]||(g[j]==='#'&&!open.includes(j)))continue;seen[j]=1;q.push(j);}}return seen;};
  for(const c of b.hero.choices)if(!CODES.includes(g[c.anchor]))issues.push(`опора моста ${DIR_RU[c.dir]} не цветная`);
  b.hero.choices.forEach((c,k)=>{const seen=reach([c.cell]);let shut=0;for(let i=0;i<g.length;i++)if(CODES.includes(g[i])&&!seen[i])shut++;if(shut)issues.push(`с мостом ${k+1} отрезано ${shut} цветных клеток`);});
  // the far side is shut while both sites are walls: no far cell (beyond the wall) is joined to the entry through open cells
  const open0=reach([]),far=new Set();b.hero.choices.forEach(c=>far.add(c.cell+(c.cell-c.anchor)));for(const f of far)if(open0[f])issues.push('дальний берег открыт без моста');}
 return {ok:!issues.length,issues,board:b,measure:m};}

// ---- the parameter space of the search, drawn by seed ----
export function paramsFor(pic,side,r){const vol=measure(pic).volumes,cols=Object.keys(vol).filter(c=>vol[c]>=4);if(cols.length<2)return null;
 const pick=a=>a[Math.floor(r()*a.length)],int=(a,b)=>a+Math.floor(r()*(b-a+1));
 // the winning site's bed x: a small material (its first pod clears the bed at once); the strip behind it y
 const small=cols.filter(c=>vol[c]<=90),X=pick(small.length>=2&&r()<.8?small:cols),Y=pick(cols.filter(c=>c!==X));
 const env=[...ENV].filter(c=>!vol[c]&&c!==X&&c!==Y);if(!env.length)return null;
 // crossed (the old pilots): the other site's bed is y and its strip x; open (found to work far more often, 2026-10-28):
 // two more materials, w the other site's bed and v its strip, the gate whose pods the queue keeps deep
 const more=cols.filter(c=>c!==X&&c!==Y),open=more.length>=2&&r()<.85,w=open?pick(more):null,v=open?pick(more.filter(c=>c!==w)):null;
 // the far side's empty space stays empty (the picture as approved) or takes one environment colour; the near bank is a
 // meadow of the one extra colour (its pods find all their work there, so the opening has work), filled or round the
 // other bed only
 const z=r()<.85?'.':pick(env.slice(0,3)),meadow=[...MEADOW].find(c=>!vol[c]&&c!==X&&c!==Y),ground=r()<.7?(z==='.'?meadow:z):null,fill=!!ground&&r()<.4,shield=ground&&!fill?int(1,2):0;
 // the winning bridge's bed small and quick, the other one's deeper: the winning bridge tends to be offered first
 return {side,x:X,y:Y,...(open?{w,v}:{}),z,strip:2,divider:int(0,2),bedDepth:int(1,2),bedBreadth:3,bedDepthY:int(2,4),bedBreadthY:pick([3,5,7]),pad:0,margin:int(1,3),
  ground,shield,fill};}

// ---- beds sized to the pods: each bed about one pod of its colour (17 §3 splits a material into even pods), so the
// first pod of a colour clears its bed and opens its bridge, and the colour's later pods are what its strip on the far
// side needs; then the strip behind the other bridge waits for late pods, and with that bridge the places jam ----
const BEDS=[];for(let d=2;d<=6;d++)for(let b=3;b<=11;b+=2)BEDS.push([d,b]);
export function fitBeds(pic,o,tol=.2){let cur={...o};
 const podOf=(sc,c)=>{const m=measure(sc),s=LV.splitPods(m.volumes,m.N,null);if(s.error)return null;const ps=s.pods.filter(p=>p.color===c).map(p=>p.count).sort((a,b)=>a-b);return ps[0]||null;};
 for(let pass=0;pass<2;pass++)for(const [c,dk,bk] of [[cur.x,'bedDepth','bedBreadth'],[cur.w||cur.y,'bedDepthY','bedBreadthY']]){let best=null;
  for(const [d,b] of BEDS){const t={...cur,[dk]:d,[bk]:b};let sc;try{sc=scene(pic,t);}catch{continue;}const p=podOf(sc,c);if(!p)continue;const err=Math.abs(d*b-p)/p;if(!best||err<best.err)best={err,t};}
  if(!best)return null;cur=best.t;}
 const sc=scene(pic,cur);for(const [c,dk,bk] of [[cur.x,'bedDepth','bedBreadth'],[cur.w||cur.y,'bedDepthY','bedBreadthY']]){const p=podOf(sc,c),v=(cur[dk]??cur.bedDepth)*(cur[bk]??cur.bedBreadth);if(!p||Math.abs(v-p)/p>tol)return null;}
 return cur;}

// ---- search: scenes × candidates until one passes the builder's checks and the bridge kind ----
// returns {scene, opts, level, eval, tried:{scenes, candidates}, seconds} or null
export async function search(pic,{side,kind,cls='Normal',recipes=['bridge'],level=26,scenes=40,candidates=12,seed=1,deadline=Infinity,log=()=>{},opts=null,compose=null,maxColours=9}){const r=rngOf(seed*2654435761+side.length*97+pic.id.length),t0=Date.now();let nS=0,nC=0;const reasons={};
 const strong=cls!=='Normal';
 for(let s=0;s<scenes&&Date.now()<deadline;s++){let o,sc;
  try{if(compose){[o,sc]=compose(r);if(!o)return null;}else{o=opts?{side,...opts}:paramsFor(pic,side,r);if(!o)return null;if(!opts&&r()<.1)o=fitBeds(pic,o)||o;sc=scene(pic,o);}}catch(e){reasons['сцена: '+e.message]=(reasons['сцена: '+e.message]||0)+1;continue;}
  const ck=checkScene(sc,level,{maxColours});if(!ck.ok){const k=ck.issues[0].replace(/\d+/g,'n');reasons[k]=(reasons[k]||0)+1;continue;}
  if(strong&&fitness(sc).hiddenColours<3){reasons['Hard/SH: меньше 3 скрытых цветов']=(reasons['Hard/SH: меньше 3 скрытых цветов']||0)+1;continue;}
  nS++;const m=ck.measure,board=ck.board,base=LV.splitPods(m.volumes,m.N,null);if(base.error){reasons[base.error]=(reasons[base.error]||0)+1;continue;}
  const scPic={id:pic.id,title:pic.title,rows:sc.rows,locked:null};
  // the winning bridge is the x bed's (its strip is y); the queue leans to it, and the pods of the gate (the strip behind
  // the other bridge) but the first stand at least `gate` deep (levels.mjs candidate opts)
  const intended=choiceOf(sc,board,sc.win);
  for(const recipe of recipes)for(let k=0;k<candidates&&Date.now()<deadline;k++){nC++;const cs=columnsFor(m.N),cols=cs[k%cs.length],pos=LV.DOSE_CYCLE[k%LV.DOSE_CYCLE.length],rows=Math.ceil(base.pods.length/cols),gate=kind==='better'||(kind==='any'&&k%2===0)?(r()<.5?0:Math.max(2,rows-4)):Math.max(3,rows-1-Math.floor(r()*3)),delay=Math.floor(r()*4);
   const c=LV.candidate(scPic,board,base.pods,cols,recipe,cls,seed*1000003+s*7919+k*131,false,pos,{gate,intended,delay});if(c.error){reasons[c.error]=(reasons[c.error]||0)+1;continue;}
   const e=await LV.evaluate(c.level,{m,cls,kind});
   if(!e.reason&&e.random.cls!==cls)e.reason=`класс по случайному тесту ${e.random.cls}`;
   if(e.reason){const k=e.reason+(e.quick!=null?' q≈'+Math.round(e.quick*10)/10:'');reasons[k]=(reasons[k]||0)+1;continue;}
   log(`  ${pic.id} ${side}: сцена ${s+1}, кандидат ${k+1} прошёл (${Math.round(e.random.share*100)} %)`);
   return {scene:sc,opts:o,level:c.level,recipe,cols,eval:e,N:m.N,colours:m.colours,tried:{scenes:nS,candidates:nC,reasons},seconds:Math.round((Date.now()-t0)/1000)};}}
 log(`  ${pic.id} ${side}: не найдено (сцен ${nS}, кандидатов ${nC}) ${JSON.stringify(reasons)}`);
 return {fail:true,tried:{scenes:nS,candidates:nC,reasons},seconds:Math.round((Date.now()-t0)/1000)};}

// ---- split (17 §8 «Одна цельная картинка», the owner, 2026-10-29: «сделать одну большую картинку и просто в рандомных
// местах разделить её и поставить мосты … они должны быть одним большим целым, просто разделённым чем-то (хоть линией,
// полоской нейтральной в цвет фона)»): one whole approved picture split by an INSERTED strip line, so no picture cell is
// overwritten: the part beyond the line moves one cell away. The strip is impassable ('#') with exactly two Sprig sites
// ('b') on it and runs through the whole picture to the walkway ring, where boardOf closes the ring at its ends (as for a
// full-width stream or a full-height wall). The level marks it definition.strip = 'gap' (lib.mjs boardOf reads the
// picture's scene.mode 'split'), so renderers draw it as a neutral line in the board's surface colour, not as water or
// stone. The round engine plays it as any strip.
//
//   up: a row inserted           left: a column inserted          two: an L; the top-left block (corner tl, or the
//   (the far part above)         (the far part on the left;       top-right one, tr) moved one cell out diagonally
//   ...AAAA...                   right: on the right)             AAA#......
//   ..AAAAAA..                   ..AA#BB..                        AAA#BBBBBB
//   #b#####b##                   .AAAbBBB.                        AAAbBBBBBB   (a site crossing left)
//   ..BBBBBB..                   .AAA#BBB.                        #b##BBBBBB   (a site crossing up)
//   ...BBBB...                   ..AAbBB..                        .CCCDDDDDD
//
// The far part holds 25–45 % of the picture's cells. Each site's anchor is its near-bank neighbour (below for up, beside it
// for left / right) and a colour cell (the approach to clear first, §8); its far neighbour is a colour or empty cell. The
// gap contrast (the strip contrast of 2026-10-28 for the gap look): every colour cell beside the strip is at least
// GAP_CONTRAST away in CIEDE2000 from the board's surface (the game's board.surface and the pilot's ground), else the split
// cannot be read there and that position is not used (in the palette only Жемчуг C falls below: 9.2 / 2.3).
export const GAP_SURFACES={'board.surface':'#DCD3C4','земля пилота v12':'#EADCB5'},GAP_CONTRAST=15,GAP_SHARE=[.25,.45];
const D4=[[1,0],[-1,0],[0,1],[0,-1]],GAP_DE=Object.fromEntries(CODES.split('').map(c=>[c,Math.min(...Object.values(GAP_SURFACES).map(h=>deltaEHex(c,h)))]));
const mirrorRows=rows=>rows.map(r=>[...r].reverse().join(''));
// how a split grows the picture: up a row, left / right a column, two both; a board is the picture with the ring and the
// frame (+4), at most 48×48 (the engine)
export const growth=side=>({dw:side==='up'?0:1,dh:side==='left'||side==='right'?0:1});
// the source's empty edge rows and columns trimmed, only as many as the board with the strip needs (no colour cell goes);
// {rows, left, top} or null when a colour cell would go
export function trimmed(rows,side,max=44){const {dw,dh}=growth(side);let left=0,top=0;rows=rows.slice();
 const empty=(r)=>!/[^.]/.test(r),col=(k)=>rows.every(r=>r[k]==='.');
 while(rows[0].length+dw>max){const a=col(0),b=col(rows[0].length-1);if(!a&&!b)return null;
  const la=(()=>{let k=0;while(k<rows[0].length&&col(k))k++;return k;})(),lb=(()=>{let k=0;while(k<rows[0].length&&col(rows[0].length-1-k))k++;return k;})();
  if(la>=lb){rows=rows.map(r=>r.slice(1));left++;}else rows=rows.map(r=>r.slice(0,-1));}
 while(rows.length+dh>max){const a=empty(rows[0]),b=empty(rows.at(-1));if(!a&&!b)return null;
  const ta=(()=>{let k=0;while(k<rows.length&&empty(rows[k]))k++;return k;})(),tb=(()=>{let k=0;while(k<rows.length&&empty(rows[rows.length-1-k]))k++;return k;})();
  if(ta>=tb){rows=rows.slice(1);top++;}else rows=rows.slice(0,-1);}
 return {rows,left,top};}

// the split picture of a source without a strip. up: a row inserted before row `at`; left / right: a column inserted before
// column `at`; two: at = [x0, y0], the vertical leg in column x0 (of the split) from the top to row y0, the horizontal leg
// in row y0 from column x0 to the picture's left (tl) or right (tr) edge, the corner block moved one cell out. sites:
// positions along the strip (up: x; left / right: y; two: [x on the horizontal leg, y on the vertical leg]); without sites
// the strip is all '#'. Returns {rows, side, at, corner, sites (split coordinates), strip (its cells), far(x, y), map(x, y)
// (a source cell's place in the split), arm(x, y) (the step from a strip cell to its near-bank neighbour)}.
export function splitOf(src,{side,at,sites=null,corner='tl'}){const rows=src.rows,pw=rows[0].length,ph=rows.length,S=sites||[];
 if(/[#b]/.test(rows.join('')))throw Error('у картинки уже есть полоса — разделяется картинка без полосы');
 if(side==='up'){if(!(Number.isInteger(at)&&at>=1&&at<=ph-1))throw Error(`up: строка разреза ${at} вне 1–${ph-1}`);
  const s=[...'#'.repeat(pw)];for(const x of S)s[x]='b';
  return {rows:[...rows.slice(0,at),s.join(''),...rows.slice(at)],side,at,sites:S.map(x=>[x,at]),strip:[...Array(pw).keys()].map(x=>[x,at]),far:(x,y)=>y<at,map:(x,y)=>[x,y<at?y:y+1],arm:()=>[0,1]};}
 if(side==='left'||side==='right'){if(!(Number.isInteger(at)&&at>=1&&at<=pw-1))throw Error(`${side}: столбец разреза ${at} вне 1–${pw-1}`);
  return {rows:rows.map((r,y)=>r.slice(0,at)+(S.includes(y)?'b':'#')+r.slice(at)),side,at,sites:S.map(y=>[at,y]),strip:[...Array(ph).keys()].map(y=>[at,y]),
   far:side==='left'?(x,y)=>x<at:(x,y)=>x>at,map:(x,y)=>[x<at?x:x+1,y],arm:()=>side==='left'?[1,0]:[-1,0]};}
 if(side!=='two')throw Error(`сторона: up, left, right, two`);
 if(corner==='tr'){const W=pw+1,mir=([x,y])=>[W-1-x,y],m=splitOf({rows:mirrorRows(rows)},{side,at:[W-1-at[0],at[1]],sites:sites&&[W-1-sites[0],sites[1]],corner:'tl'});
  return {...m,rows:mirrorRows(m.rows),at,corner,sites:m.sites.map(mir),strip:m.strip.map(mir),far:(x,y)=>m.far(W-1-x,y),map:(x,y)=>mir(m.map(pw-1-x,y)),arm:(x,y)=>{const [dx,dy]=m.arm(W-1-x,y);return [-dx,dy];}};}
 const [x0,y0]=at;if(!(x0>=2&&x0<=pw-1&&y0>=2&&y0<=ph-1))throw Error(`two: угол разреза ${JSON.stringify(at)} вне картинки ${pw}×${ph}`);
 const g=Array.from({length:ph+1},()=>Array(pw+1).fill('.')),inC=(x,y)=>x<x0&&y<y0;
 for(let y=0;y<ph;y++)for(let x=0;x<pw;x++){const t=inC(x,y);g[t?y:y+1][t?x:x+1]=rows[y][x];}
 const strip=[];for(let y=0;y<=y0;y++)strip.push([x0,y]);for(let x=0;x<x0;x++)strip.push([x,y0]);for(const [x,y] of strip)g[y][x]='#';
 const st=sites?[[sites[0],y0],[x0,sites[1]]]:[];for(const [x,y] of st)g[y][x]='b';
 return {rows:g.map(r=>r.join('')),side,at,corner,sites:st,strip,far:(x,y)=>x<x0&&y<y0,map:(x,y)=>inC(x,y)?[x,y]:[x+1,y+1],arm:(x,y)=>y===y0&&x<x0?[0,1]:[1,0]};}

// the share of the picture's colour cells beyond the strip
export function farShare(sp){let far=0,all=0;sp.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(CODES.includes(r[x])){all++;if(sp.far(x,y))far++;}});return all?far/all:0;}
// the gap contrast: the colour cell beside the strip closest to the board's surface (worst, code; Infinity without one),
// how many distinct colour cells stand beside the strip (cells) and how many of them fall below GAP_CONTRAST (low)
export function gapContrast(sp){const on=new Set(sp.strip.map(([x,y])=>x+','+y)),seen=new Set();let worst=Infinity,code=null,low=0;
 for(const [x,y] of sp.strip)for(const [dx,dy] of D4){const k=sp.rows[y+dy]?.[x+dx],key=(x+dx)+','+(y+dy);if(!k||!CODES.includes(k)||on.has(key)||seen.has(key))continue;seen.add(key);
  if(GAP_DE[k]<GAP_CONTRAST)low++;if(GAP_DE[k]<worst){worst=GAP_DE[k];code=k;}}
 return {worst,code,low,cells:seen.size};}
// a split's gap contrast passes: no colour cell beside the strip near the board's colour, or (an explicit, recorded
// exception, make --gap-tolerance f) at most that share of them
export const gapOk=(gc,tolerance=0)=>gc.worst>=GAP_CONTRAST||(tolerance>0&&gc.low<=tolerance*gc.cells);
// strip cells that may carry a site: not on the picture's edge nor the L's corner, a colour anchor (near neighbour) and a
// colour or empty far neighbour; leg 0 = the horizontal leg of an L (or the only strip), 1 = its vertical leg
function siteCells(sp){const rows=sp.rows,W=rows[0].length,H=rows.length,at=(x,y)=>x<0||y<0||x>=W||y>=H?null:rows[y][x],out=[];
 for(const [x,y] of sp.strip){if(x===0||y===0||x===W-1||y===H-1)continue;if(sp.side==='two'&&x===sp.at[0]&&y===sp.at[1])continue;
  const [dx,dy]=sp.arm(x,y),near=at(x+dx,y+dy),far=at(x-dx,y-dy);if(!CODES.includes(near)||far===null||far==='#'||far==='b')continue;
  const leg=sp.side==='two'?(dy?0:1):0;out.push({x,y,leg,s:sp.side==='up'||(sp.side==='two'&&leg===0)?x:y,near,far,fx:x-dx,fy:y-dy,arm:[dx,dy]});}
 return out;}
// colour depth of the far part from its own edge (the ring beyond the strip belongs to it), up to 3: a site landing deep in
// the subject opens its own front, one landing on open background reaches the ring at once and both bridges play alike
function farDepth(sp){const rows=sp.rows,W=rows[0].length,H=rows.length,d=new Float64Array(W*H).fill(Infinity),dq=[],ok=(x,y)=>x>=0&&y>=0&&x<W&&y<H&&sp.far(x,y)&&rows[y][x]!=='#'&&rows[y][x]!=='b';
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(ok(x,y)&&(x===0||y===0||x===W-1||y===H-1)){const c=CODES.includes(rows[y][x])?1:0;if(c<d[y*W+x]){d[y*W+x]=c;c?dq.push([x,y]):dq.unshift([x,y]);}}
 while(dq.length){const [x,y]=dq.shift();for(const [dx,dy] of D4){const u=x+dx,v=y+dy;if(!ok(u,v))continue;const c=CODES.includes(rows[v][u])?1:0,nd=d[y*W+x]+c;if(nd<d[v*W+u]){d[v*W+u]=nd;c?dq.push([u,v]):dq.unshift([u,v]);}}}
 return (x,y)=>Math.min(3,d[y*W+x]);}

// colour depth of the near bank from the Garden Entry (0-1 search on the board, colour cells cost 1, the strip a wall)
// (walk: the plain number of steps instead, colour cells passable: how far a team walks to that cell)
function nearDepth(sp,walk=false){const b=boardOf({rows:sp.rows.map(r=>r.replace(/b/g,'#'))}),{w,h}=b,g=b.rowsBottomFirst.join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];
 for(const e of b.entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of D4){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#')continue;const cost=walk||CODES.includes(g[n])?1:0,nv=d[c]+cost;if(nv<d[n]){d[n]=nv;cost?dq.push(n):dq.unshift(n);}}}
 return (x,y)=>{const v=d[(h-3-y)*w+x+2];return Number.isFinite(v)?v:99;};}
// every split of a source for a side, best first: positions with 25–45 % beyond the strip, the gap contrast kept and the
// entry (bottom middle of the board) on the near side; per position the `per` best site pairs, then round robin over the
// positions by rank, so the search moves through different places early. A pair is better with anchors of different
// materials, far cells of different materials deep in the subject, a share near 35 %, the sites apart and a strip that
// runs through the subject rather than through empty space. Deterministic by seed (it breaks near ties).
export function splitsFor(src,side,{per=3,seed=1,limit=Infinity,share:range=GAP_SHARE,tolerance=0,first=false,kind=null}={}){const r=rngOf(seed*7919+side.length*31+1),rows=src.rows,pw=rows[0].length,ph=rows.length,{N,volumes:vols}=measure(src);
 const colour=c=>CODES.includes(c),cum=Array.from({length:ph+1},()=>new Int32Array(pw+1));
 for(let y=0;y<ph;y++)for(let x=0;x<pw;x++)cum[y+1][x+1]=cum[y][x+1]+cum[y+1][x]-cum[y][x]+(colour(rows[y][x])?1:0);
 const box=(x0,y0,x1,y1)=>x1<x0||y1<y0?0:cum[y1+1][x1+1]-cum[y0][x1+1]-cum[y1+1][x0]+cum[y0][x0];// colour cells in a source box
 const {dw}=growth(side),e=Math.floor((pw+dw+4)/2)-2,pos=[];
 if(side==='up')for(let at=1;at<ph;at++)pos.push({at,far:box(0,0,pw-1,at-1)});
 if(side==='left')for(let at=1;at<pw&&at<e;at++)pos.push({at,far:box(0,0,at-1,ph-1)});
 if(side==='right')for(let at=Math.max(1,e+1);at<pw;at++)pos.push({at,far:box(at,0,pw-1,ph-1)});
 if(side==='two')for(let y0=2;y0<ph;y0++){for(let x0=2;x0<pw;x0++)pos.push({at:[x0,y0],corner:'tl',far:box(0,0,x0-1,y0-1)});for(let x0=1;x0<=pw-2;x0++)pos.push({at:[x0,y0],corner:'tr',far:box(x0,0,pw-1,y0-1)});}
 // first: only the first position that carries a pair, positions taken by a share nearest 35 % (a quick "can it split")
 if(first)pos.sort((a,b)=>Math.abs(a.far/N-.35)-Math.abs(b.far/N-.35));
 const byPos=[];
 for(const p of pos){if(first&&byPos.length)break;const share=p.far/N;if(share<range[0]||share>range[1])continue;let sp;try{sp=splitOf(src,{side,at:p.at,corner:p.corner});}catch{continue;}
  const gc=gapContrast(sp);if(!gapOk(gc,tolerance))continue;
  const cells=siteCells(sp);if(!cells.length)continue;const dep=farDepth(sp),len=sp.strip.length,nd=nearDepth(sp),nw=kind==='single'?nearDepth(sp,true):null;
  // the strip through the subject: the share of strip cells without a colour neighbour
  const bare=sp.strip.filter(([x,y])=>!D4.some(([dx,dy])=>colour(sp.rows[y+dy]?.[x+dx]))).length/len;
  // each cell alone: a colour far cell deep in the subject; the best 14 per leg take part in the pairs
  const one=c=>(colour(c.far)?.4*(3-dep(c.fx,c.fy)):1.6),legs=[0,1].map(l=>cells.filter(c=>c.leg===l).sort((a,b)=>one(a)-one(b)).slice(0,14));
  const pairs=[];if(side==='two'){for(const a of legs[0])for(const b of legs[1])pairs.push([a,b]);}else{const l=legs[0];for(let i=0;i<l.length;i++)for(let j=i+1;j<l.length;j++)pairs.push(l[i].s<l[j].s?[l[i],l[j]]:[l[j],l[i]]);}
  const scored=[];for(const [a,b] of pairs){const sep=Math.abs(a.x-b.x)+Math.abs(a.y-b.y);if(sep<3)continue;
   // both approaches about as deep from the entry, so either bridge can be the first one offered (17 §8: both sites
   // reachable at the choice in part of the random games; bridgeSingle); a single choice prefers one anchor material and
   // shallow approaches: bridgeSingle's random games tap whenever they can, and on a Super Hard they jam early, so an
   // approach deeper than the first layers is never cleared there
   // (a single choice: also approaches of small colours, which one pod clears, and close to the entry, eaten early)
   const da=nd(a.x+a.arm[0],a.y+a.arm[1]),db=nd(b.x+b.arm[0],b.y+b.arm[1]),depthGap=Math.min(4,Math.abs(da-db));
   const shallow=kind==='single'?.5*Math.min(6,Math.max(da,db)-1)+.4*Math.min(3,vols[a.near]/40)+.4*Math.min(3,vols[b.near]/40)+.03*Math.min(60,Math.max(nw(a.x+a.arm[0],a.y+a.arm[1]),nw(b.x+b.arm[0],b.y+b.arm[1]))):0;
   const score=(a.near===b.near?(kind==='single'?-.5:1):0)+(a.far===b.far?.5:0)+one(a)+one(b)+2*Math.abs(share-.35)+2*Math.max(0,len/2-sep)/len*(kind==='single'?.5:1)+1.5*bare+(gc.cells?2*gc.low/gc.cells:0)+.3*depthGap+shallow+.15*r();
   scored.push({score,sites:[a.s,b.s]});}
  scored.sort((a,b)=>a.score-b.score);
  // the `per` best pairs of this position, each moving a site at least 3 cells from the ones already taken
  const take=[];for(const q of scored){if(take.length>=per)break;if(take.some(t=>Math.abs(t.sites[0]-q.sites[0])+Math.abs(t.sites[1]-q.sites[1])<3))continue;take.push(q);}
  if(take.length)byPos.push({p,share,take});}
 byPos.sort((a,b)=>a.take[0].score-b.take[0].score);
 // spread: positions in waves, each wave taking only positions at least 3 cells from those it already holds (neighbouring
 // cuts play alike), the best first in each wave
 const near=(a,b)=>Math.max(...[].concat(a.p.at).map((v,i)=>Math.abs(v-[].concat(b.p.at)[i])))<3&&a.p.corner===b.p.corner,waves=[];let rest=byPos;
 while(rest.length){const wave=[],left=[];for(const b of rest)(wave.some(w=>near(w,b))?left:wave).push(b);waves.push(...wave);rest=left;}
 const ordered=waves;
 const out=[];for(let k=0;k<per&&out.length<limit;k++)for(const b of ordered){if(out.length>=limit)break;const q=b.take[k];if(!q)continue;
  out.push({key:`${side}:${b.p.corner?b.p.corner+':':''}${[].concat(b.p.at).join(',')}:${q.sites.join(',')}`,side,at:b.p.at,...(b.p.corner?{corner:b.p.corner}:{}),sites:q.sites,share:Math.round(b.share*1000)/1000,score:Math.round(q.score*1000)/1000});}
 return out;}

// a quick "can this picture split to this side": the first valid split (trimmed to 48×48 when needed) with the colours
// hidden at the start on its board, or null
export function splittable(src,side,opts={}){const tr=trimmed(src.rows,side);if(!tr)return null;const t={...src,rows:tr.rows},[s]=splitsFor(t,side,{...opts,per:1,first:true});
 if(!s)return null;const sp=splitOf(t,s);return {split:s,hiddenColours:fitness({rows:sp.rows}).hiddenColours};}

// the board check of a split: the scene checks (boardOf, two sites on the planned side, colour anchors, each bridge alone
// opens every colour cell, the far side shut while both sites are walls, stage, 48×48), no picture cell lost or moved but
// as the split moves it, the far part 25–45 %, nothing beyond the strip reachable without a bridge, and the gap contrast
export function checkSplit(sp,src,level=null,{maxColours=11,share:range=GAP_SHARE,tolerance=0}={}){const sc={rows:sp.rows,side:sp.side,scene:{mode:'split'}},ck=checkScene(sc,level,{maxColours,contrast:false}),issues=[...ck.issues];
 const sm=measure(src),m=ck.measure||measure(sc);let moved=0;
 src.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(CODES.includes(r[x])){const [u,v]=sp.map(x,y);if(sp.rows[v]?.[u]!==r[x])moved++;}});
 if(moved||m.N!==sm.N)issues.push(`клетки картинки потеряны или сдвинуты не так: ${moved}, N ${m.N} ≠ ${sm.N}`);
 const share=farShare(sp);if(share<range[0]||share>range[1])issues.push(`за полосой ${Math.round(share*100)} % клеток, нужно ${range.map(x=>Math.round(x*100)).join('–')} %`);
 const gc=gapContrast(sp);if(!gapOk(gc,tolerance))issues.push(`у полосы клеток почти цвета фона ${gc.low} из ${gc.cells} (${gc.code}, ΔE ${gc.worst.toFixed(1)} < ${GAP_CONTRAST})`);
 if(ck.board&&ck.board.hero){const b=ck.board,g=b.rowsBottomFirst.join(''),{w,h}=b,seen=new Uint8Array(w*h),q=[...b.entries];for(const i of q)seen[i]=1;
  while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(const [dx,dy] of D4){const u=x+dx,v=y+dy,j=v*w+u;if(u<0||v<0||u>=w||v>=h||seen[j]||g[j]==='#')continue;seen[j]=1;q.push(j);}}
  let open=0;sp.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(sp.far(x,y)&&r[x]!=='#'&&r[x]!=='b'&&seen[(h-3-y)*w+x+2])open++;});if(open)issues.push(`за полосой ${open} клеток доступны без моста`);}
 return {...ck,ok:!issues.length,issues,share,contrast:gc};}

// the picture coordinates of a board's bridge choice
const siteOf=(b,c)=>[c.cell%b.w-2,b.h-3-Math.floor(c.cell/b.w)];
// search: splits × candidates of the builder (levels.mjs candidate + evaluate, the planned bridge kind tested first) until
// one passes. The queue leans to one site (intended, alternating) with the gate (the colour right behind the other site
// stands deep) and delay; each bridge is solved with a fresh solver (bridgeChoicesWin); a Super Hard may lose at the
// choice (evaluate). done: keys of splits already tried (a resumed search skips them); onSplit(record) after each split.
// Returns {split, sp, level, recipe, cols, eval, N, colours, win, lose, tried} or {fail, tried}
// zone: the random test's share the search accepts (default: Normal its preferred 50–75 % of §5, Hard and Super Hard their
// class); a Normal also needs the holding pod (§6, checks.hold: placement wants it in a regular Normal). The queue's blend
// towards the reverse access order is steered by the shares measured so far (candidate opts.t for a Normal, aiming at
// 62 %, as levels.mjs build steers its Normals; opts.tStrong for Hard and Super Hard, aiming at 33 % and 12 %)
export const ZONES={Normal:[.5,.75],Hard:[.2,.5],'Super Hard':[.05,.2]};
export async function searchSplit(src,{side,kind,cls='Normal',recipes=['bridge'],level=26,candidates=8,seed=1,deadline=Infinity,log=()=>{},done=new Set(),onSplit=()=>{},per=3,splits=null,title=null,share=GAP_SHARE,tolerance=0,zone=ZONES[cls],needHold=cls==='Normal',worseMin=0}){
 const t0=Date.now(),strong=cls!=='Normal',r=rngOf(seed*2654435761+side.length*97+level*13),list=splits||splitsFor(src,side,{per,seed,share,tolerance,kind});let nS=0,nC=0;const reasons={};
 const steer={Normal:{t:.3,aim:.62,n:0},Hard:{t:.6,aim:.33,n:0},'Super Hard':{t:.75,aim:.12,n:0}}[cls]||null;
 // the steering step, shrinking with the attempts: the miss scaled to its side of the aim (too hard moves the blend as far
 // as too easy, though the aim of a Super Hard sits near 0)
 const step=(st,q)=>.3/Math.sqrt(st.n)*(q<st.aim?(q-st.aim)/st.aim:(q-st.aim)/(1-st.aim));
 for(const [n,s] of list.entries()){if(Date.now()>=deadline)break;if(done.has(s.key))continue;nS++;
  const rec={key:s.key,at:s.at,...(s.corner?{corner:s.corner}:{}),sites:s.sites,share:s.share,score:s.score,candidates:0,reasons:{}},ts=Date.now(),no=k=>{rec.reasons[k]=(rec.reasons[k]||0)+1;reasons[k]=(reasons[k]||0)+1;};
  const fin=x=>{rec.seconds=Math.round((Date.now()-ts)/1000);onSplit(rec);return x;};
  const sp=splitOf(src,s),ck=checkSplit(sp,src,level,{share,tolerance});
  if(!ck.ok){no(ck.issues[0].replace(/\d+(\.\d+)?/g,'n'));fin();continue;}
  if(strong&&fitness({rows:sp.rows}).hiddenColours<3){no('Hard/SH: меньше 3 скрытых цветов');fin();continue;}
  const m=ck.measure,board=ck.board,base=LV.splitPods(m.volumes,m.N,null);if(base.error){no(base.error);fin();continue;}
  const scPic={id:src.id,title:title||src.title,rows:sp.rows,locked:null};
  for(const recipe of recipes)for(let k=0;k<candidates&&Date.now()<deadline;k++){nC++;rec.candidates++;
   const cs=columnsFor(m.N),cols=cs[k%cs.length],pos=LV.DOSE_CYCLE[k%LV.DOSE_CYCLE.length],rows=Math.ceil(base.pods.length/cols),intended=k%2;
   // delay pushes the other site's approach deep; a single choice keeps it low (bridgeSingle wants either site offered first
   // in part of the random games)
   const gate=kind==='better'?(r()<.5?0:Math.max(2,rows-4)):Math.max(3,rows-1-Math.floor(r()*3)),dr=r(),delay=kind==='single'?(dr<.75?0:1):Math.floor(dr*4);
   const cseed=seed*1000003+n*7919+k*131,hi=cls==='Normal'?.9:1,blend=steer?Math.min(hi,Math.max(0,steer.t+(rngOf(cseed^0x5bd1e995)()-.5)*.12)):null;
   // a single choice exposes the other approach in most attempts (candidate opts.expose)
   const expose=kind==='single'&&k%4!==3;
   const c=LV.candidate(scPic,board,base.pods,cols,recipe,cls,cseed,false,pos,{gate,intended,delay,...(expose?{expose}:{}),...(blend!==null?(cls==='Normal'?{t:blend}:{tStrong:blend}):{})});if(c.error){no(c.error);continue;}
   // ROUNDS_TRACE=1: each check's time on stderr (a slow solver branch shows which candidate and stage)
   let quick=null,verdicts=null,tl=Date.now();const trace=process.env.ROUNDS_TRACE?l=>{const t=Date.now();console.error(`  ${src.id} ${s.key} #${k} +${((t-tl)/1000).toFixed(1)} s ${l}`);tl=t;}:()=>{};
   // a Super Hard with no win in the 40 quick games (the same games evaluate plays first) is almost surely below 5 %: it
   // goes no further, before the per-bridge solver (minutes on a 48×48 board)
   if(cls==='Super Hard'){const q0=await quickShare(c.level,40);if(q0===0){if(steer){steer.n++;steer.t=Math.min(hi,Math.max(0,steer.t+step(steer,0)));}no('ниже 5 % по быстрой оценке (0 из 40)');trace('0 из 40');continue;}}
   const e=await LV.evaluate(c.level,{m,cls,kind,tick:l=>{const q=/^быстрая оценка ([\d.]+)/.exec(l);if(q)quick=+q[1];const v=/^мосты (\[.*\])/.exec(l);if(v)verdicts=v[1];trace(l);}});trace(e.reason||'прошёл');
   // the solver's verdict per bridge goes with a bridge reason (both win, or neither: a different fix)
   if(e.reason&&/^мост: (решает не ровно|выигрывают не оба)/.test(e.reason)&&verdicts)e.reason+=' '+verdicts;
   // one bridge wins but bridgeSingle refused: which site random games offer first (both must be, in some games)
   if(e.reason&&/^мост: ни/.test(e.reason)&&verdicts){const bs=await bridgeSingle(c.level,60,3,JSON.parse(verdicts));e.reason+=` ${verdicts} первым предложен ${JSON.stringify(bs.offered)}`;trace(e.reason);}
   if(steer){const q=e.random?e.random.share:quick;if(q!==null){steer.n++;steer.t=Math.min(hi,Math.max(0,steer.t+step(steer,q)));}}
   if(!e.reason&&e.random.cls!==cls)e.reason=`класс по случайному тесту ${e.random.cls}`;
   if(!e.reason&&(e.random.share<zone[0]||e.random.share>zone[1]))e.reason=`вне зоны ${zone.map(x=>Math.round(x*100)).join('–')} % (q≈${Math.round(e.random.share*10)/10})`;
   if(!e.reason&&needHold&&!e.checks.hold)e.reason='нет удерживающего пода (§6)';
   // worseMin (planned better): the worse bridge's random share at least this, so both bridges win for a player too
   if(!e.reason&&kind==='better'&&worseMin>0&&Math.min(...e.bridge.each.map(x=>x.share))<worseMin)e.reason=`мост: худший ниже ${Math.round(worseMin*100)} % по случайному тесту`;
   if(e.reason){no(e.reason+(e.quick!=null?' q≈'+Math.round(e.quick*10)/10:''));continue;}
   const ch=board.hero.choices,wi=e.checks.bridgeKind==='single'?e.checks.bridgeSingle.each.indexOf(true):e.bridge.better;
   rec.found=true;log(`  ${src.id} ${side}: разрез ${rec.key}, кандидат ${k+1} прошёл (${Math.round(e.random.share*100)} %, мосты ${e.bridge.each.map(x=>Math.round(x.share*100)+' %').join(' / ')})`);
   return fin({split:s,sp,level:c.level,recipe,cols,gate,delay,intended,expose,attempt:k,dosePos:pos,blend,eval:e,N:m.N,colours:m.colours,win:siteOf(board,ch[wi]),lose:siteOf(board,ch[1-wi]),tried:{splits:nS,candidates:nC,reasons},seconds:Math.round((Date.now()-t0)/1000)});}
  fin();}
 log(`  ${src.id} ${side}: не найдено (разрезов ${nS}, кандидатов ${nC}) ${JSON.stringify(reasons)}`);
 return {fail:true,tried:{splits:nS,candidates:nC,reasons},seconds:Math.round((Date.now()-t0)/1000)};}

// the draft module of found splits: "<title> — мост", the source's family and tags, scene {from, mode: 'split', …}
export function splitModuleText(found,{name}){const tags=['family','direction','device','subject','source','reference'];
 const pics=found.map(f=>({title:`${f.source.title} — мост`,...Object.fromEntries(tags.filter(k=>f.source[k]).map(k=>[k,f.source[k]])),forLevel:f.forLevel,
  scene:{from:f.source.id,mode:'split',side:f.split.side,...(f.split.corner?{corner:f.split.corner}:{}),at:f.split.at,sites:f.sp.sites,share:f.split.share,...(f.trim&&(f.trim.left||f.trim.top||f.trim.cut)?{trim:f.trim}:{}),
   kind:f.kind,cls:f.cls,recipe:f.recipe,win:f.win,lose:f.lose,queue:{gate:f.gate,delay:f.delay},...(f.rules&&Object.keys(f.rules).length?{rules:f.rules}:{})},rows:f.sp.rows}));
 return `// ${name}: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in ${name}.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default ${JSON.stringify({theme:'bridge-splits',name,pictures:pics},null,1)};
`;}

// ---- the draft module of found scenes (bloomlings-round-pictures format) ----
export function moduleText(found,{name}){const pics=found.map(f=>({title:`${f.source.title} за мостом`,...(f.source.family?{family:f.source.family}:{}),...(f.source.direction?{direction:f.source.direction}:{}),
  device:'сцена моста',subject:f.source.subject||f.source.title,source:`сцена моста из ${f.source.id}`,forLevel:f.forLevel,
  scene:{from:f.source.id,side:f.scene.side,kind:f.kind,cls:f.cls,recipe:f.recipe,win:f.scene.win,lose:f.scene.lose,opts:f.opts},rows:f.scene.rows}));
 return `// ${name}: bridge scenes composed by tools/rounds/bridge-scene.mjs (17 §8): a smaller approved picture on the far side
// behind a wall with two Sprig sites, crossed strips and near-bank beds; each one passed the builder's checks with the
// planned bridge kind (evidence in ${name}.json). Drafts for the owner's sheet: pictures.mjs render / sheet / approve.
export default ${JSON.stringify({theme:'bridge-scenes',name,pictures:pics},null,1)};
`;}

const sourceOf=id=>{const p=picture(id);if(!p)throw Error(`нет картинки ${id}`);if(p.status!=='approved')throw Error(`${id} не утверждена`);return p;};
const show=sc=>sc.rows.join('\n');

const isMain=!!process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href,cmd=isMain?process.argv[2]:null;
if(cmd==='show'){const id=process.argv[3],p=sourceOf(id),side=arg('--side','up'),vol=measure(p).volumes,cols=Object.keys(vol).sort((a,b)=>vol[b]-vol[a]);
 const env=[...ENV].filter(c=>!vol[c]);const o={side,x:arg('--x',cols[1]||cols[0]),y:arg('--y',cols[2]||cols[1]),z:arg('--z',env[0]),strip:+arg('--strip',2),divider:+arg('--divider',0),bedDepth:+arg('--bedDepth',3),bedBreadth:+arg('--bedBreadth',3),pad:+arg('--pad',0),margin:+arg('--margin',0),ground:arg('--ground'),shield:+arg('--shield',0),fill:flag('--fill'),full:flag('--full'),...(arg('--bedDepthY')?{bedDepthY:+arg('--bedDepthY')}:{}),...(arg('--bedBreadthY')?{bedBreadthY:+arg('--bedBreadthY')}:{})};
 const sc=scene(p,o),ck=checkScene(sc,+arg('--level',26));console.log(show(sc));console.log(JSON.stringify(o));
 console.log(`${sc.rows[0].length}×${sc.rows.length}, ${ck.measure.N} кл., ${ck.measure.colours} цв., скрытых на старте ${fitness(sc).hiddenColours}; ${ck.ok?'борд OK':'НЕТ: '+ck.issues.join('; ')}`);}
else if(cmd==='make'&&arg('--mode')==='split'){
 // one whole picture split by a gap strip (17 §8 «Одна цельная картинка», 2026-10-29); resumable: the evidence file keeps
 // every split tried (a rerun with the same parameters skips them), so the search runs in chunks of --minutes
 const side=arg('--side'),kind=arg('--kind','single'),cls=arg('--cls','Normal'),recipes=arg('--recipe','bridge').split(','),level=+arg('--level',26),out=arg('--out'),seed=+arg('--seed',1),candidates=+arg('--candidates',8),per=+arg('--per',3);
 const share=arg('--share')?arg('--share').split(',').map(Number):GAP_SHARE,tolerance=+arg('--gap-tolerance',0),froms=(arg('--from')||'').split(',').filter(Boolean),zone=arg('--zone')?arg('--zone').split(',').map(Number):ZONES[cls],noHold=flag('--no-hold'),worseMin=+arg('--worse-min',0);
 if(!SIDES.includes(side))throw Error(`--side ${SIDES.join('|')}`);if(!['single','better','any'].includes(kind))throw Error('--kind single|better|any');if(!froms.length)throw Error('--from P####[,P####…]: утверждённые свободные картинки без полосы');
 if(!out&&!flag('--dry'))throw Error('--out <module.mjs> (или --dry)');if(share.length!==2||!(share[0]<share[1]))throw Error('--share a,b (доли, например 0.25,0.45)');
 const evFile=out?out.replace(/\.mjs$/,'.json'):null,params={from:froms,side,kind,cls,recipes,level,seed,candidates,per,share,gapTolerance:tolerance,zone,...(noHold?{noHold}:{}),...(worseMin?{worseMin}:{})};
 let ev=evFile&&fs.existsSync(evFile)?JSON.parse(fs.readFileSync(evFile,'utf8')):null;
 if(ev&&(ev.format!=='bloomlings-split-search-1'||JSON.stringify(ev.params)!==JSON.stringify(params))){if(!flag('--restart'))throw Error(`${evFile}: поиск с другими параметрами — --restart начинает заново`);ev=null;}
 ev=ev||{format:'bloomlings-split-search-1',params,created:new Date().toISOString(),runs:[],tried:[],found:null};
 const save=()=>{if(!evFile)return;ev.updated=new Date().toISOString();fs.mkdirSync(path.dirname(path.resolve(evFile)),{recursive:true});fs.writeFileSync(evFile+'.tmp',JSON.stringify(ev,null,1)+'\n');fs.renameSync(evFile+'.tmp',evFile);};
 const u=usage(),srcOf=id=>{const p=sourceOf(id);if(u[id])throw Error(`${id} уже в раунде ${u[id]}`);if(p.locked)throw Error(`${id} закреплена за L${p.locked}`);const tr=trimmed(p.rows,side);return {p,tr,src:tr&&{id,title:p.title,rows:tr.rows}};};
 const writeModule=f=>{if(!out||flag('--dry'))return;const {p,tr}=srcOf(f.from),sp=splitOf({rows:tr.rows},f.split),name=path.basename(out,'.mjs');
  const rules={...(share[0]!==GAP_SHARE[0]||share[1]!==GAP_SHARE[1]?{share}:{}),...(tolerance?{gapTolerance:tolerance,gapLow:[f.gap.low,f.gap.cells]}:{}),...(noHold&&cls==='Normal'?{noHold:true}:{})};
  fs.writeFileSync(out,splitModuleText([{source:p,sp,split:f.split,kind,cls,recipe:f.recipe,forLevel:level,win:f.win,lose:f.lose,gate:f.queue.gate,delay:f.queue.delay,trim:{left:tr.left,top:tr.top,cut:p.w-tr.rows[0].length+p.h-tr.rows.length},rules}],{name}));
  console.log(`→ ${out} (черновик; дальше pictures.mjs render ${out})`);};
 if(ev.found&&!flag('--again')){console.log(`уже найдено: ${ev.found.from} ${ev.found.key} (бот ${Math.round(ev.found.random.share*100)} %)`);writeModule(ev.found);process.exit(0);}
 const minutes=+arg('--minutes',35),deadline=Date.now()+60000*minutes,log=x=>console.log(x);ev.runs.push({started:new Date().toISOString(),minutes});ev.found=null;save();
 for(const id of froms){if(Date.now()>=deadline)break;const {p,tr,src}=srcOf(id);if(!tr){console.log(`${id}: не помещается в 48×48 и с обрезкой пустых краёв`);continue;}
  // --retry <regex>: a tried split whose reasons match is tried once more (after a change of the search; the new record
  // carries the regex, so it is not retried again)
  const retry=arg('--retry'),rx=retry?new RegExp(retry):null,again=t=>rx&&Object.keys(t.reasons).some(k=>rx.test(k))&&!ev.tried.some(u=>u.from===id&&u.key===t.key&&u.retry===retry);
  const done=new Set(ev.tried.filter(t=>t.from===id&&!again(t)).map(t=>t.key));
  const r=await searchSplit(src,{side,kind,cls,recipes,level,candidates,seed,deadline,log,done,per,share,tolerance,zone,needHold:cls==='Normal'&&!noHold,worseMin,title:`${p.title} — мост`,onSplit:rec=>{ev.tried.push({from:id,...rec,...(retry?{retry}:{})});save();}});
  if(r.fail)continue;
  const e=r.eval,k=e.checks,f=fitness({rows:r.sp.rows});
  ev.found={from:id,key:r.split.key,split:r.split,trim:{left:tr.left,top:tr.top},size:`${r.sp.rows[0].length}×${r.sp.rows.length}`,N:r.N,colours:r.colours,hiddenColours:f.hiddenColours,visible:f.visible,gap:gapContrast(r.sp),
   recipe:r.recipe,columns:r.cols,attempt:r.attempt,dosePos:r.dosePos,queue:{gate:r.gate,delay:r.delay,intended:r.intended,...(r.expose?{expose:true}:{}),...(r.blend!=null?{blend:Math.round(r.blend*1000)/1000}:{})},win:r.win,lose:r.lose,
   random:{share:e.random.share,runs:e.random.runs,wilson95:e.random.wilson95,cls:e.random.cls,borderline:!!e.random.borderline},bridge:e.bridge,bridgeKind:k.bridgeKind,bridgeLoss:k.bridgeLoss||null,forced:k.forced,opening:k.opening,greedyLoses:k.greedyLoses,
   pods:r.level.pods.length,linked:r.level.pods.filter(x=>x.group).length,hidden:r.level.pods.filter(x=>x.mystery).length,seconds:r.seconds,
   level:r.level,eval:{random:e.random,solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,bridge:e.bridge,checks:e.checks}};
  save();console.log(r.sp.rows.join('\n'));
  console.log(`${id} → разрез ${r.split.key} (${Math.round(r.split.share*100)} % за полосой), ${r.N} кл., ${r.colours} цв.; ${cls} ${r.recipe}: бот ${Math.round(e.random.share*100)} %, мосты ${k.bridgeKind} ${e.bridge.each.map(x=>`${x.label} ${x.win?'выигрыш':'проигрыш'} ${Math.round(x.share*100)} %`).join(' / ')}; ${r.seconds} с`);
  writeModule(ev.found);break;}
 if(!ev.found){const t=ev.tried.length;console.log(`пока не найдено: опробовано разрезов ${t}${evFile?`; продолжить той же командой (${evFile})`:''}`);process.exitCode=1;}}
// preview levels for the pilot (gameplay-pilot-v12/build.mjs --preview <dir>): the found candidate of each split module,
// on the rendered draft picture with the same rows, in the placed-level shape (never content/rounds/levels/)
else if(cmd==='preview'){const out=arg('--out');if(!out)throw Error('--out <папка>');const files=process.argv.slice(3).filter(a=>a.endsWith('.mjs'));
 const m=manifest();fs.mkdirSync(out,{recursive:true});
 for(const file of [...new Set(files)]){const mod=(await import(pathToFileURL(path.resolve(file)).href)).default,ev=JSON.parse(fs.readFileSync(file.replace(/\.mjs$/,'.json'),'utf8')),f=ev.found;if(!f){console.log(file,'— не найдено');continue;}
  const pm=mod.pictures[0],e=m.pictures.find(x=>{const q=picture(x.id);return q&&q.rows.join('\n')===pm.rows.join('\n');});if(!e)throw Error(`${file}: картинка не записана — сначала pictures.mjs render`);
  const pic=picture(e.id),n=pm.forLevel,lv=f.level,id=e.id;
  const L={level:n,cls:ev.params.cls,intro:null,picture:id,candidate:`${id}-split-${f.key}`,recipe:f.recipe,N:f.N,W:f.N,colours:f.colours,P:lv.pods.length,columns:f.columns,linked:f.linked,hidden:f.hidden,
   random:f.eval.random,solution:f.eval.solution,fairness:f.eval.fairness,strategies:f.eval.strategies,rhythm:f.eval.rhythm,dosePos:f.dosePos??null,bridge:f.eval.bridge,bridgeSide:bridgeSide(lv),checks:f.eval.checks,
   engine:'tools/rounds/engine.mjs (pilot v9 + §8 link rule)',preview:`черновик разделённой картинки ${id} (${pic.status}), bridge-scene.mjs make --mode split`,definition:{...lv,id:n,title:pic.title,subject:id,difficulty:ev.params.cls}};
  fs.writeFileSync(path.join(out,lid(n)+'.json'),JSON.stringify(L,null,1)+'\n');console.log(`${lid(n)} ← ${id} «${pic.title}» ${ev.params.cls} ${f.recipe}, бот ${Math.round(f.random.share*100)} %`);}}
else if(cmd==='make'){const side=arg('--side'),kind=arg('--kind','single'),cls=arg('--cls','Normal'),recipes=arg('--recipe','bridge').split(','),level=+arg('--level',26),count=+arg('--count',1),out=arg('--out');
 if(!SIDES.includes(side))throw Error(`--side ${SIDES.join('|')}`);if(!['single','better','any'].includes(kind))throw Error('--kind single|better|any');if(!out&&!flag('--dry'))throw Error('--out <module.mjs> (или --dry)');
 const u=usage(),ids=flag('--free')?manifest().pictures.filter(e=>e.status==='approved'&&!u[e.id]).map(e=>picture(e.id)).filter(p=>p&&!p.locked&&!['сторонняя вселенная','мем'].includes(p.source)&&!/[#b]/.test(p.rows.join(''))&&measure(p).N>=200&&measure(p).N<=800).sort((a,b)=>measure(a).N-measure(b).N).map(p=>p.id):arg('--pictures','').split(',').filter(Boolean);
 const deadline=Date.now()+60000*+arg('--minutes',20),found=[],log=x=>console.log(x),evidence=[];
 for(const id of ids){if(found.length>=count||Date.now()>deadline)break;const p=sourceOf(id);
  const r=await search(p,{side,kind,cls,recipes,level,scenes:+arg('--scenes',40),candidates:+arg('--candidates',12),seed:+arg('--seed',1),deadline,log,opts:arg('--opts')?JSON.parse(arg('--opts')):null});
  if(!r||r.fail){evidence.push({source:id,side,kind,cls,found:false,...(r?{tried:r.tried,seconds:r.seconds}:{})});continue;}
  const e=r.eval,k=e.checks;found.push({source:p,scene:r.scene,opts:r.opts,kind,cls,recipe:r.recipe,forLevel:level});
  evidence.push({source:id,side,kind,cls,recipe:r.recipe,found:true,seconds:r.seconds,tried:{scenes:r.tried.scenes,candidates:r.tried.candidates},size:`${r.scene.rows[0].length}×${r.scene.rows.length}`,N:r.N,colours:r.colours,opts:r.opts,
   random:{share:e.random.share,runs:e.random.runs,wilson95:e.random.wilson95,cls:e.random.cls},bridge:e.bridge,bridgeKind:k.bridgeKind,bridgeSingle:k.bridgeSingle,bridgeLoss:k.bridgeLoss||null,forced:k.forced,opening:k.opening,pressure:k.pressure,greedyLoses:k.greedyLoses,
   pods:r.level.pods.length,columns:r.cols,linked:r.level.pods.filter(x=>x.group).length,hidden:r.level.pods.filter(x=>x.mystery).length,solution:e.solution.route.length,level:r.level});
  console.log(show(r.scene));console.log(`${id} → сцена ${side}, ${r.N} кл., ${r.colours} цв.; ${cls} ${r.recipe}: бот ${Math.round(e.random.share*100)} %, мост ${k.bridgeKind} ${JSON.stringify(e.bridge.each)}${k.bridgeLoss?`, проигрыш с другим мостом через ${k.bridgeLoss.medianLaunchesAfter} запусков (медиана)`:''}; ${r.seconds} с`);}
 if(out&&!flag('--dry')){const name=path.basename(out,'.mjs');fs.mkdirSync(path.dirname(path.resolve(out)),{recursive:true});
  if(found.length)fs.writeFileSync(out,moduleText(found,{name}));fs.writeFileSync(out.replace(/\.mjs$/,'.json'),JSON.stringify({created:new Date().toISOString(),side,kind,cls,recipes,level,results:evidence},null,1)+'\n');
  console.log(found.length?`→ ${out} (${found.length} сцен, черновики; дальше pictures.mjs render ${out})`:'сцен не найдено',`· подробности ${out.replace(/\.mjs$/,'.json')}`);}
 if(found.length<count)process.exitCode=1;}
else if(cmd==='adapt'){// strips over the pictures of a hand-drawn strip module (each picture's forLevel names its wave plan position)
 const file=process.argv[3],out=arg('--out'),mod=(await import(pathToFileURL(path.resolve(file)).href)).default,only=arg('--index')?arg('--index').split(',').map(Number):null;
 const deadline=Date.now()+60000*+arg('--minutes',20),log=x=>console.log(x),found=[],evidence=[];
 for(const [k,p0] of mod.pictures.entries()){if(only&&!only.includes(k))continue;const pl=planOf(p0.forLevel)||{},kind=arg('--kind',pl.bridgeKind||'single'),cls=arg('--cls',pl.cls||'Normal'),recipes=arg('--recipe',pl.recipe||'bridge').split(',');
  const p={id:`#${k+1}`,title:p0.title,rows:p0.rows},vol=measure(p).volumes,cols=Object.keys(vol).filter(c=>vol[c]>=4),side=bridgeSide(boardOf(p));
  const compose=r=>{const pick=a=>a[Math.floor(r()*a.length)],a=pick(cols),b=pick(cols.filter(c=>c!==a)),o={a,b,depth:2+Math.floor(r()*2),win:Math.floor(r()*2),border:r()<.5};return [o,stripsOver(p,o)];};
  const r=await search(p,{side,kind,cls,recipes,level:p0.forLevel,scenes:+arg('--scenes',12),candidates:+arg('--candidates',5),seed:+arg('--seed',1),deadline:Math.min(deadline,Date.now()+60000*+arg('--per',8)),log,compose,maxColours:10});
  if(!r||r.fail){evidence.push({index:k+1,title:p0.title,side,kind,cls,found:false,...(r?{tried:r.tried,seconds:r.seconds}:{})});continue;}
  const e=r.eval;found.push({...p0,title:p0.title,device:`${p0.device||''}; полосы за местами (bridge-scene.mjs adapt)`.replace(/^; /,''),scene:{from:`${path.basename(file)} #${k+1}`,side,kind,cls,recipe:r.recipe,win:r.scene.win,lose:r.scene.lose,opts:r.opts},rows:r.scene.rows});
  evidence.push({index:k+1,title:p0.title,side,kind,cls,recipe:r.recipe,found:true,seconds:r.seconds,opts:r.opts,N:r.N,colours:r.colours,random:{share:e.random.share,cls:e.random.cls},bridge:e.bridge,bridgeKind:e.checks.bridgeKind,bridgeLoss:e.checks.bridgeLoss||null,forced:e.checks.forced,opening:e.checks.opening});
  console.log(show(r.scene));console.log(`#${k+1} «${p0.title}» → ${side}, ${cls} ${r.recipe}: бот ${Math.round(e.random.share*100)} %, мост ${e.checks.bridgeKind} ${JSON.stringify(e.bridge.each)}; ${r.seconds} с`);}
 if(out){const name=path.basename(out,'.mjs');if(found.length)fs.writeFileSync(out,`// ${name}: ${path.basename(file)} with a strip laid behind each bridge site and a divider between them (bridge-scene.mjs adapt,\n// 17 §8): each one passed the builder's checks with its wave position's bridge kind (evidence in ${name}.json). Drafts.\nexport default ${JSON.stringify({theme:mod.theme,name,pictures:found},null,1)};\n`);
  fs.writeFileSync(out.replace(/\.mjs$/,'.json'),JSON.stringify({created:new Date().toISOString(),from:file,results:evidence},null,1)+'\n');console.log(`→ ${found.length} из ${evidence.length}`);}}
else if(isMain&&cmd!=='noop'){console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));}
