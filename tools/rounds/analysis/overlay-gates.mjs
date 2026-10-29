// Static overlay shortlist: no solver or random games. Rank legal straight walls by the work that the two
// bridge choices expose before a material is admitted. A large difference suggests a queue gate; it is NOT
// a difficulty or impossibility proof. Candidate generation and live validation remain mandatory.
import {pathToFileURL} from 'node:url';
import {CODES,picture,boardOf,measure} from '../lib.mjs';

function reachableWork(board,cell,gate){
 const g=board.rowsBottomFirst.join(''),seen=new Uint8Array(g.length),q=[...board.entries],work={};
 for(const i of q)seen[i]=1;
 while(q.length){const i=q.pop(),x=i%board.w,y=Math.floor(i/board.w);
  if(CODES.includes(g[i]))work[g[i]]=(work[g[i]]||0)+1;
  for(const j of [x?i-1:-1,x+1<board.w?i+1:-1,y?i-board.w:-1,y+1<board.h?i+board.w:-1])
   if(j>=0&&!seen[j]&&g[j]!==gate&&(g[j]!=='#'||j===cell)){seen[j]=1;q.push(j);}
 }
 return work;
}

export function overlayGateOptions(pic,side,limit=12){
 if(!['up','left','right'].includes(side))throw Error('straight side up|left|right required');
 const rows=pic.rows,h=rows.length,w=rows[0].length,entryX=Math.floor((w+4)/2)-2,total=measure(pic).N,options=[];
 const vertical=side!=='up',end=vertical?w:h,along=vertical?h:w,dx=side==='left'?1:side==='right'?-1:0,dy=side==='up'?1:0;
 const at=(x,y)=>rows[y]?.[x],isColor=c=>typeof c==='string'&&CODES.includes(c);
 for(let cross=2;cross<end-2;cross++){
  if(side==='left'&&cross>=entryX||side==='right'&&cross<=entryX)continue;
  const walls=Array.from({length:along},(_,j)=>vertical?[cross,j]:[j,cross]);
  let removed=0,far=0;
  for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(isColor(at(x,y))){
   const a=vertical?x:y;if(a===cross)removed++;else if(side==='right'?a>cross:a<cross)far++;
  }
  const N=total-removed,share=far/N;if(share<.25||share>.45)continue;
  const sites=walls.slice(1,-1).filter(([x,y])=>isColor(at(x+dx,y+dy))&&isColor(at(x-dx,y-dy)));
  if(sites.length<2)continue;
  let board;try{board=boardOf(pic,{walls,sites:[sites[0],sites.at(-1)]});}catch{continue;}
  const g=board.rowsBottomFirst.join(''),colors=[...new Set([...g].filter(isColor))];
  const index=([x,y])=>(board.h-3-y)*board.w+x+2;
  const reach=sites.map(s=>Object.fromEntries(colors.map(c=>[c,reachableWork(board,index(s),c)])));
  for(let i=0;i<sites.length;i++)for(let j=i+1;j<sites.length;j++){
   if(Math.abs(sites[i][0]-sites[j][0])+Math.abs(sites[i][1]-sites[j][1])<3)continue;
   for(const [good,bad] of [[i,j],[j,i]])for(const gate of colors){
    const win=reach[good][gate],lose=reach[bad][gate];
    const gains=Object.fromEntries(colors.filter(c=>(win[c]||0)>(lose[c]||0)).map(c=>[c,(win[c]||0)-(lose[c]||0)]));
    const gained=Object.values(gains).reduce((a,b)=>a+b,0);if(gained<20)continue;
    const fullyBlocked=Object.keys(gains).filter(c=>!lose[c]);
    const score=gained/N+fullyBlocked.length*.35;
    options.push({picture:pic.id,side,overlay:{walls,sites:[sites[i],sites[j]]},intended:good===i?0:1,gate,
     landingGate:at(sites[bad][0]-dx,sites[bad][1]-dy),anchors:[at(sites[i][0]+dx,sites[i][1]+dy),at(sites[j][0]+dx,sites[j][1]+dy)],
     N,share,removed,gained,fullyBlocked,availableBeforeGate:{win,lose},score});
   }
  }
 }
 // One representative per wall + gate + winning direction first, so the shortlist explores distinct geometry.
 options.sort((a,b)=>b.score-a.score||a.removed-b.removed);
 const unique=new Set(),out=[];for(const o of options){const cross=vertical?o.overlay.walls[0][0]:o.overlay.walls[0][1],key=[cross,o.gate,o.intended].join(':');if(unique.has(key))continue;unique.add(key);out.push(o);if(out.length===limit)break;}
 return out;
}

if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
 const ids=process.argv.slice(2).filter(x=>/^P\d+$/.test(x)),sides=process.argv.filter(x=>['up','left','right'].includes(x));
 for(const id of ids)for(const side of sides.length?sides:['up','left','right']){
  const out=overlayGateOptions(picture(id),side,6);
  console.log(JSON.stringify({picture:id,side,options:out.map(({overlay,...o})=>({...o,wall:side==='up'?overlay.walls[0][1]:overlay.walls[0][0],sites:overlay.sites}))}));
 }
}
