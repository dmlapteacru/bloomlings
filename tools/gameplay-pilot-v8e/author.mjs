// v8E level author: capsules and tray for a v8E picture (scenes.mjs). Pod sizes follow the role (a capsule ~28-34 cells,
// as in Colony Flow's L5, L10 and L20); links (pairs) go mostly to the hard levels; "?" capsules are placed afterwards
// (fix-lib.mjs). Super Hard levels and L24 use 4 columns, the rest 3.
import R from './rules.cjs';
const pick=(a,r)=>a[Math.floor(r()*a.length)];
const shuffle=(a,r)=>{a=a.slice();for(let i=a.length-1;i>0;i--){const j=Math.floor(r()*(i+1));[a[i],a[j]]=[a[j],a[i]];}return a;};
// Links per level (Colony Flow: 70-80% of Hard and Super Hard levels have them, about a third of the others).
// L8 teaches a same-colour pair, L9 mixed pairs; L22 is the links Super Hard.
export const GROUPS={8:1,9:2,10:2,13:1,14:2,15:2,18:2,21:1,22:4,24:2,25:3,27:2,29:2};
// Cells per capsule by role (fewer, larger capsules on relief levels).
export const CELLS_PER_POD={tutorial:30,relief:34,normal:30,task:28,hard:28,superHard:28};
const TUTORIAL_PODS={1:9,2:10,3:12,4:18};
export const columnsOf=(id,role)=>role==='superHard'||id===24?4:3;
// L29 is the big-capsule Super Hard: fewer, larger capsules (a big background with large capsules is its axis).
const CELLS_PER_POD_OF={29:44};
export function podTarget(id,role,cells){return TUTORIAL_PODS[id]||Math.max(12,Math.round(cells/(CELLS_PER_POD_OF[id]||CELLS_PER_POD[role])));}
// Layers of other colours between the entries and each colour's nearest cell (0: reachable at once).
function colourLayers(art){const W=art.w,H=art.h,g=art.rowsBottomFirst.join(''),n=g.length,cost=new Int16Array(n).fill(32767),q=[];
 for(const e of art.entries||[Math.floor(W/2)])if(g[e]!=='#'){cost[e]=g[e]==='.'?0:1;q.push(e);}
 const bridges=new Set((art.hero?.choices||[]).map(c=>c.cell)); // a bridge site counts as open: the far bank is layered too
 for(let h=0;h<q.length;h++){const i=q[h],x=i%W;for(const j of [i-W,i+W,x>0?i-1:-1,x<W-1?i+1:-1]){if(j<0||j>=n||(g[j]==='#'&&!bridges.has(j)))continue;const c=cost[i]+(g[j]==='.'||g[j]==='#'?0:1);if(c<cost[j]){cost[j]=c;q.push(j);}}}
 const best={};for(let i=0;i<n;i++)if(g[i]!=='.'&&g[i]!=='#')best[g[i]]=Math.min(best[g[i]]??99,cost[i]-1);return best;}
export function definition(art,r,{role,difficulty,extra=0,order=1}={}){
 const id=art.id,counts=Object.fromEntries(R.CHANNELS.split('').map(c=>[c,0]));
 for(const c of art.rowsBottomFirst.join(''))if(c!=='#'&&c!=='.')counts[c]++;
 const cells=Object.values(counts).reduce((a,b)=>a+b,0),n=podTarget(id,role,cells)+extra;
 const colours=R.CHANNELS.split('').filter(c=>counts[c]>0),alloc=Object.fromEntries(colours.map(c=>[c,1])),minimum=4;
 if(colours.length>n)return null;
 while(Object.values(alloc).reduce((a,b)=>a+b,0)<n){
  const options=colours.filter(c=>counts[c]>=minimum*(alloc[c]+1));if(!options.length)return null;
  const weights=options.map(c=>counts[c]/(alloc[c]+.4)**1.5),total=weights.reduce((a,b)=>a+b,0);let point=r()*total,chosen=options.at(-1);
  for(let i=0;i<options.length;i++){point-=weights[i];if(point<=0){chosen=options[i];break;}}alloc[chosen]++;
 }
 const spiky=['task','hard','superHard'].includes(role),pods=[];
 for(const color of colours){
  const total=counts[color],number=alloc[color],floor=Math.min(minimum,Math.floor(total/number));
  const weights=Array.from({length:number},()=>role==='tutorial'?.7+r()*.6:spiky?.18+r()**2*4:.35+r()*1.8),sum=weights.reduce((a,b)=>a+b,0);let remaining=total;
  for(let i=0;i<number;i++){const count=i===number-1?remaining:floor+Math.floor((total-floor*number)*weights[i]/sum);pods.push({color,count});remaining-=count;}
 }
 if(pods.length!==n||pods.some(p=>p.count<1))return null;
 const candidates=shuffle(pods.map((p,i)=>i),r),groups=[];
 for(let g=0;g<(GROUPS[id]||0);g++){
  if(candidates.length<2)return null;const same=id===8&&g===0;
  const aIndex=same?candidates.findIndex(a=>candidates.some(b=>a!==b&&pods[a].color===pods[b].color)):candidates.length-1;if(aIndex<0)return null;
  const [a]=candidates.splice(aIndex,1),bIndex=candidates.findIndex(p=>same?pods[p].color===pods[a].color:pods[p].color!==pods[a].color);if(bIndex<0)return null;
  const [b]=candidates.splice(bIndex,1),name=String.fromCharCode(65+g);pods[a].group=pods[b].group=name;groups.push([a,b].sort((x,y)=>x-y));
 }
 // Queue order: capsules of outer colours tend to come first, inner ones later, with noise. order is the dial
 // (0: strictly outside-in, ~1: loose, 3+: nearly shuffled); the search picks it per role to hit the bots' windows.
 const layer=colourLayers(art),units=[...pods.map((p,i)=>p.group?null:[i]).filter(Boolean),...groups];
 const key=new Map(units.map(u=>[u,Math.min(...u.map(i=>layer[pods[i].color]))+order*r()*3]));
 units.sort((a,b)=>key.get(a)-key.get(b));
 const columns=columnsOf(id,role),rowsOut=[];let row=[],waiting=[];
 const flush=()=>{rowsOut.push(row);row=[];};
 for(const u of units){
  if(u.length===2&&columns-row.length<2){waiting.push(u);continue;}
  row.push(u);if(row.flat().length>=columns)flush();
  while(waiting.length&&row.flat().length+2<=columns){row.push(waiting.shift());if(row.flat().length>=columns)flush();}
 }
 for(const u of waiting){if(columns-row.flat().length<2)flush();row.push(u);if(row.flat().length>=columns)flush();}
 if(row.length)flush();
 const tray=Array.from({length:columns},()=>[]);
 for(const units of rowsOut){
  // Shuffle the row's units over its columns, keeping a pair side by side.
  const placed=new Array(columns).fill(-1),order2=shuffle(units,r);let free=shuffle([...Array(columns).keys()],r);
  for(const u of order2){if(u.length===1){const c=free.find(c=>placed[c]<0);placed[c]=u[0];}else{const c=[...Array(columns-1).keys()].find(c=>placed[c]<0&&placed[c+1]<0);if(c===undefined)return null;placed[c]=u[0];placed[c+1]=u[1];}}
  // A short last row keeps its capsules in the first columns of the free ones (columns may differ in depth).
  placed.forEach((p,c)=>{if(p>=0)tray[c].push(p);});
 }
 for(const g of groups){const d=g.map(p=>tray.find(s=>s.includes(p)).indexOf(p));if(d[0]!==d[1])return null;}
 return {...art,capacity:5,pods,tray,difficulty};
}
