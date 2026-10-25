// What a level is before anyone plays it: the board (occupied cells, types, their patches, where they lie) and the tray
// (capsule numbers, columns, links, hidden capsules, padlocks), from the source as transcribed. Types are the board's
// colours; a board "?" tile is counted apart, never folded into a colour.
import {parseToken} from './levels.mjs';
import {stats} from './metrics.mjs';
const r2=x=>x==null||!Number.isFinite(x)?null:Math.round(x*100)/100;
export function staticOf(r){
 const out={board:null,tray:null};
 if(r.board){// '#' is a wall on our boards and a colour (the dark outline) on Colony Flow's L3, L4, L5 and L10
  const rows=r.board.rows,h=rows.length,w=rows[0].length,isCell=c=>c!=='.'&&c!==' '&&!(c==='#'&&r.board.walls);
  const cells=[];rows.forEach((s,y)=>[...s].forEach((c,x)=>{if(isCell(c))cells.push({x,y,c});}));
  const xs=cells.map(c=>c.x),ys=cells.map(c=>c.y),minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys);
  const bw=maxX-minX+1,bh=maxY-minY+1,types={};for(const c of cells)types[c.c]=(types[c.c]||0)+1;
  // patches: 4-neighbour components of one type
  const at=new Map(cells.map(c=>[c.x+','+c.y,c])),seen=new Set(),patches={};
  for(const c of cells){const k=c.x+','+c.y;if(seen.has(k))continue;let n=0;const st=[c];seen.add(k);
   while(st.length){const p=st.pop();n++;for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const k2=(p.x+dx)+','+(p.y+dy),q=at.get(k2);if(q&&q.c===c.c&&!seen.has(k2)){seen.add(k2);st.push(q);}}}
   (patches[c.c]=patches[c.c]||[]).push(n);}
  // where each type lies: the edge ring of the occupied box, the middle third, top and bottom halves, near the hole
  // (ours: the level's entries; Colony Flow: the hole under the middle of the board, as seen in every video)
  const entries=r.model&&r.game!=='CF'?r.model.entries.map(e=>({x:e,y:h-1})):[{x:(minX+maxX)/2,y:maxY+1}];
  const dist=c=>Math.min(...entries.map(e=>Math.abs(e.x-c.x)+Math.abs(e.y-c.y)));const ds=cells.map(dist).sort((a,b)=>a-b),near=ds[Math.floor(ds.length*.2)];
  const zone={};for(const c of cells){const z=zone[c.c]=zone[c.c]||{edge:0,centre:0,top:0,bottom:0,nearHole:0};
   if(c.x===minX||c.x===maxX||c.y===minY||c.y===maxY)z.edge++;
   if(c.x>=minX+bw/3&&c.x<minX+2*bw/3&&c.y>=minY+bh/3&&c.y<minY+2*bh/3)z.centre++;
   if(c.y<minY+bh/2)z.top++;else z.bottom++;if(dist(c)<=near)z.nearHole++;}
  const total=cells.length,named=Object.keys(types).filter(k=>k!=='?');
  out.board={w,h,box:[bw,bh],cells:total,fill:r2(total/(bw*bh)),types:named.length,mysteryTiles:types['?']||0,
   byType:Object.fromEntries(Object.entries(types).sort((a,b)=>b[1]-a[1]).map(([k,n])=>[k,{cells:n,share:r2(n/total),name:r.board.legend?.[k]||null,patches:(patches[k]||[]).length,largest:Math.max(...(patches[k]||[0])),islands:(patches[k]||[]).filter(s=>s<=3).length,zone:zone[k]}])),
   patches:Object.values(patches).flat().length,islands:Object.values(patches).flat().filter(s=>s<=3).length,largestPatch:Math.max(...Object.values(patches).flat()),
   holeNote:r.game==='CF'?'hole assumed under the middle of the board':'the level\'s entries'};}
 // tray
 const cols=Object.entries(r.tray).map(([c,col])=>col.map(parseToken)),pods=cols.flatMap((col,ci)=>col.map((p,ri)=>({...p,col:ci,row:ri}))).filter(p=>!p.lock);
 const counts=pods.map(p=>p.count),freq={};for(const n of counts)freq[n]=(freq[n]||0)+1;
 const gcd=(a,b)=>b?gcd(b,a%b):a,step=counts.reduce((g,n)=>gcd(g,n),0);
 const med=stats(counts).median,byType={};for(const p of pods){const t=byType[p.color]=byType[p.color]||{capsules:0,cells:0,counts:[]};t.capsules++;t.cells+=p.count;t.counts.push(p.count);}
 for(const t of Object.values(byType)){t.small=t.counts.filter(n=>n<med).reduce((a,b)=>a+b,0);t.counts.sort((a,b)=>a-b);}
 const total=counts.reduce((a,b)=>a+b,0),groups={};for(const p of pods)if(p.link)(groups[p.link]=groups[p.link]||[]).push(p);
 const sets=Object.values(groups).map(g=>{const c=g.map(p=>p.col),rr=g.map(p=>p.row);return {size:g.length,length:Math.max(...c)-Math.min(...c)+Math.max(...rr)-Math.min(...rr),sameColour:new Set(g.map(p=>p.color)).size===1,vertical:new Set(c).size===1,counts:g.map(p=>p.count),hidden:g.filter(p=>p.hidden).length,rows:rr};});
 const locks=cols.flat().filter(p=>p.lock).length;
 out.tray={columns:cols.length,depth:cols.map(c=>c.length),capsules:pods.length,cells:total,counts:stats(counts),sd:r2(Math.sqrt(counts.reduce((a,n)=>a+(n-total/pods.length)**2,0)/pods.length)),
  frequent:Object.entries(freq).sort((a,b)=>b[1]-a[1]||a[0]-b[0]).slice(0,8).map(([n,k])=>[+n,k]),values:Object.keys(freq).length,step,
  smallVolumeShare:r2(pods.filter(p=>p.count<med).reduce((a,p)=>a+p.count,0)/total),
  byType:Object.fromEntries(Object.entries(byType).map(([k,t])=>[k,{capsules:t.capsules,cells:t.cells,counts:t.counts,smallShare:r2(t.small/t.cells)}])),
  meanToBoard:out.board?r2(total/pods.length/out.board.cells):null,
  startTops:cols.filter(c=>c.length&&!c[0].lock).length,
  links:{sets:sets.length,capsules:sets.reduce((a,s)=>a+s.size,0),sizes:sets.map(s=>s.size),lengths:stats(sets.map(s=>s.length)),sameColour:sets.filter(s=>s.sameColour).length,vertical:sets.filter(s=>s.vertical).length,withHidden:sets.filter(s=>s.hidden).length,list:sets},
  hidden:{capsules:pods.filter(p=>p.hidden).length,colourHidden:pods.filter(p=>p.hidden).length,numberHidden:pods.filter(p=>p.hidden).length,both:pods.filter(p=>p.hidden).length,
   byRow:pods.filter(p=>p.hidden).map(p=>p.row),note:'a "?" capsule hides both its colour and its number until it reaches the top (all three games)'},
  padlocks:locks};
 // accounting: capsule cells of each type against the board's cells of that type
 if(out.board){const acc={};for(const k of new Set([...Object.keys(out.board.byType),...Object.keys(byType)]))if(k!=='?')acc[k]={board:out.board.byType[k]?.cells||0,capsules:byType[k]?.cells||0};
  out.accounting={byType:acc,off:Object.values(acc).reduce((n,a)=>n+Math.abs(a.board-a.capsules),0),mysteryTiles:out.board.mysteryTiles};}
 return out;}
