// Shared by fix.mjs and verify.mjs: the "?" placement (v8D) and the planner settings.
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;import {playPlanner,ROWS,FULL_ROWS} from './horizon.mjs';
// How many "?" capsules each level gets.
// v8E: rare on Normal (CF ~11%), more on the hard levels (CF ~20-24%); L10 is the "?" Super Hard, like Colony Flow's L10.
export const MYSTERY={6:1,10:3,19:1,25:2,30:1};
// Planner settings tried in order; the first that wins is recorded in the certificate.
export const PLANNERS=[{rows:ROWS,depth:4,weight:12},{rows:ROWS,depth:8,weight:12},{rows:ROWS,depth:6,weight:30}];
// A key "?" reveals in the middle of the level; any other "?" neither at its very start nor at its very end.
export const REVEAL_WINDOW=[.25,.8],REVEAL_OUTER=[.15,.9];
export const revealsFit=(level,list)=>list.every(e=>e.clearedFraction>=REVEAL_OUTER[0]&&e.clearedFraction<=REVEAL_OUTER[1])&&list.some(e=>isKey(strip(level),e.pod)&&e.clearedFraction>=REVEAL_WINDOW[0]&&e.clearedFraction<=REVEAL_WINDOW[1]);
export const strip=l=>({...l,pods:l.pods.map(({mystery,...p})=>p)});
// A colour is a gate when its cells alone cut other colours' cells off the entries.
export function gateColours(level){
 const s=R.create(level),n=s.board.length,gates=[];
 for(let c=0;c<R.CHANNELS.length;c++){if(!s.board.some(v=>v===c))continue;
  const seen=new Uint8Array(n),queue=[];for(const e of s.meta.entries)if(s.board[e]!==R.WALL&&s.board[e]!==c){seen[e]=1;queue.push(e);}
  for(let h=0;h<queue.length;h++){const i=queue[h];for(let d=0;d<4;d++){const j=s.meta.neighbours[i*4+d];if(j>=0&&!seen[j]&&s.board[j]!==R.WALL&&s.board[j]!==c){seen[j]=1;queue.push(j);}}}
  if(s.board.some((v,i)=>v>=0&&v!==c&&!seen[i]))gates.push(R.CHANNELS[c]);}
 return gates;
}
export function isKey(level,p){const counts=level.pods.map(q=>q.count).sort((a,b)=>a-b),median=counts[Math.floor(counts.length/2)];return gateColours(level).includes(level.pods[p].color)&&level.pods[p].count>=median;}
// Replays a route in the live game: when each "?" reached the top, as a share of the level's work.
export function reveals(level,actions){const t=new Timeline(R.create(level)),total=level.pods.reduce((n,p)=>n+p.count,0),list=[];
 for(let i=0;i<actions.length;i++){const r=t.commit(actions[i]);if(!r.accepted)return null;for(const e of r.events)if(e.type==='reveal')list.push({pod:e.pod,command:i+1,clearedFraction:Math.round(t.processedCells/total*1000)/1000});t.advance(1e9);}
 return t.status==='Won'?list:null;}
export function planner(level){for(const opts of PLANNERS){const r=playPlanner(level,opts);if(r.won)return {opts,actions:r.actions};}return null;}
function combos(list,k,from=0,acc=[]){if(acc.length===k)return [acc];const res=[];for(let i=from;i<list.length;i++)res.push(...combos(list,k,i+1,[...acc,list[i]]));return res;}
export function withMysteries(level,k,{tries=60}={}){
 const depthOf=p=>level.tray.find(s=>s.includes(p)).indexOf(p),columnOf=p=>level.tray.findIndex(s=>s.includes(p));
 const candidates=level.pods.map((p,i)=>i).filter(p=>depthOf(p)>=FULL_ROWS).sort((a,b)=>isKey(level,b)-isKey(level,a)||level.pods[b].count-level.pods[a].count||a-b);
 let tried=0;
 for(const set of combos(candidates.slice(0,18),k)){
  if(!set.some(p=>isKey(level,p))||new Set(set.map(columnOf)).size!==k)continue;if(++tried>tries)break;
  const candidate={...level,pods:level.pods.map((p,i)=>set.includes(i)?{...p,mystery:'depth'}:p)};if(R.validate(candidate).length)continue;
  const won=planner(candidate);if(!won)continue;const list=reveals(candidate,won.actions);
  if(!list||list.length!==k||!revealsFit(level,list))continue;
  return {level:candidate,hidden:set,keys:set.filter(p=>isKey(level,p)),planner:won,reveals:list};
 }
 throw Error('No "?" placement for L'+level.id);
}
