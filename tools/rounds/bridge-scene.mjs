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
// node tools/rounds/bridge-scene.mjs make --side up|left|right|two --kind single|better --cls Normal|Hard|"Super Hard"
//      --recipe bridge[,bridge+links…] --pictures P0012,P0048|--free [--level 26] [--count 1] [--scenes 40]
//      [--candidates 12] [--seed 1] [--minutes 20] [--opts '{…}'] --out content/rounds/pictures/src/scenes-NNNN.mjs [--dry]
//      searches scenes (parameters drawn by seed, or --opts) and, on each, candidates of the builder (levels.mjs candidate
//      + evaluate: balance, class by the random test, forced decisions, opening test, the bridge kind first) until --count
//      scenes pass; writes them as a draft picture module ("<title> за мостом", the source's family, source "сцена моста
//      из P####", forLevel, scene) for the owner's sheet (pictures.mjs render / sheet / approve). Never approves.
//      Evidence → <out>.json.
// node tools/rounds/bridge-scene.mjs adapt <module.mjs> --out <module.mjs> [--index 0,2] [--minutes 20] [--per 8]
//      a hand-drawn strip module (each picture's forLevel names its wave plan position): strips laid behind the sites
//      (stripsOver) and searched like make; writes the passing pictures as an adapted copy, the evidence beside it.
import {deltaE} from './colour-distance.mjs';
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
import {siteChoice,CODES,PALETTE,measure,fitsStage,stageOf,boardOf,bridgeSide,BridgeAnchorError,picture,manifest,usage,arg,flag,columnsFor,root,DIR_RU,planOf} from './lib.mjs';
import {fitness} from './fitness.mjs';
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
  for(const recipe of recipes)for(let k=0;k<candidates&&Date.now()<deadline;k++){nC++;const cs=columnsFor(m.N),cols=cs[k%cs.length],pos=LV.DOSE_CYCLE[k%LV.DOSE_CYCLE.length],rows=Math.ceil(base.pods.length/cols),gate=kind==='better'?(r()<.5?0:Math.max(2,rows-4)):Math.max(3,rows-1-Math.floor(r()*3)),delay=Math.floor(r()*4);
   const c=LV.candidate(scPic,board,base.pods,cols,recipe,cls,seed*1000003+s*7919+k*131,false,pos,{gate,intended,delay});if(c.error){reasons[c.error]=(reasons[c.error]||0)+1;continue;}
   const e=await LV.evaluate(c.level,{m,cls,kind});
   if(!e.reason&&e.random.cls!==cls)e.reason=`класс по случайному тесту ${e.random.cls}`;
   if(e.reason){const k=e.reason+(e.quick!=null?' q≈'+Math.round(e.quick*10)/10:'');reasons[k]=(reasons[k]||0)+1;continue;}
   log(`  ${pic.id} ${side}: сцена ${s+1}, кандидат ${k+1} прошёл (${Math.round(e.random.share*100)} %)`);
   return {scene:sc,opts:o,level:c.level,recipe,cols,eval:e,N:m.N,colours:m.colours,tried:{scenes:nS,candidates:nC,reasons},seconds:Math.round((Date.now()-t0)/1000)};}}
 log(`  ${pic.id} ${side}: не найдено (сцен ${nS}, кандидатов ${nC}) ${JSON.stringify(reasons)}`);
 return {fail:true,tried:{scenes:nS,candidates:nC,reasons},seconds:Math.round((Date.now()-t0)/1000)};}

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
else if(cmd==='make'){const side=arg('--side'),kind=arg('--kind','single'),cls=arg('--cls','Normal'),recipes=arg('--recipe','bridge').split(','),level=+arg('--level',26),count=+arg('--count',1),out=arg('--out');
 if(!SIDES.includes(side))throw Error(`--side ${SIDES.join('|')}`);if(!['single','better'].includes(kind))throw Error('--kind single|better');if(!out&&!flag('--dry'))throw Error('--out <module.mjs> (или --dry)');
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
