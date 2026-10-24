// v8D: what the player sees, and a planner that plays only from that.
// The owner, 2026-10-22: the tray shows its top 3 rows and the edge of the 4th, as in Colony Flow; deeper capsules
// show nothing. In Colony Flow's L10 recording the 4th row's edge shows its colour and number (half under the booster
// bar), and a "?" shows neither, so the player can plan with ROWS=4. A "?" hides its colour and count until it is on top.
// The planner looks ahead over the visible rows only (a "?" ends its column's plan),
// on the same patient live rules as the game, and plays one command at a time in the patient game. Its win in every level is the
// v8D fairness proof: a strategy that never looks deeper than the player wins.
import Rules from './rules.cjs';import R from './idle-rules.cjs';
export const ROWS=4,FULL_ROWS=3;
// The visible tray: per column, rows 1..ROWS as {pod, color|null, count|null} (null: a "?"), the last one an edge.
export function visibleTray(s,rows=ROWS){
 return s.level.tray.map((stack,c)=>stack.slice(s.heads[c],s.heads[c]+rows).map((p,k)=>s.revealed[p]?{pod:p,color:s.level.pods[p].color,count:s.remaining[p],edge:k>=FULL_ROWS}:{pod:p,color:null,count:null,edge:k>=FULL_ROWS}));
}
// The capsules the player can plan with: each column's visible rows, before its first "?". The model keeps the whole
// tray (so the live rules stay whole) but never taps a capsule outside this set, and never reads a hidden colour.
function plannable(s,rows){
 const set=new Set();s.level.tray.forEach((stack,c)=>{for(const p of stack.slice(s.heads[c],s.heads[c]+rows)){if(!s.revealed[p])break;set.add(p);}});return set;
}
const work=s=>s.remaining.reduce((a,b)=>a+b,0);
// Layers of other colours between the open front and the nearest cell of each colour (0: reachable now).
function layers(s){
 const n=s.board.length,cost=new Int16Array(n).fill(32767),deque=[];
 for(const e of s.meta.entries)if(s.board[e]!==Rules.WALL){const c=s.board[e]===Rules.OPEN?0:1;if(c<cost[e]){cost[e]=c;deque.push(e);}}
 for(let h=0;h<deque.length;h++){const i=deque[h];for(let d=0;d<4;d++){const j=s.meta.neighbours[i*4+d];if(j<0||s.board[j]===Rules.WALL)continue;
  const c=cost[i]+(s.board[j]===Rules.OPEN?0:1);if(c<cost[j]){cost[j]=c;deque.push(j);}}}
 const best=new Int16Array(Rules.CHANNELS.length).fill(32767);
 for(let i=0;i<n;i++)if(s.board[i]>=0)best[s.board[i]]=Math.min(best[s.board[i]],cost[i]-1);
 return best;
}
function value(s,start,weight,stall){
 if(s.status==='Won')return 1e9;
 const free=s.slots.reduce((n,p)=>n+(p<0),0);
 if(s.status==='Jammed')return -1e9+start-work(s);
 let v=start-work(s)+weight*free;
 // A waiting capsule costs more the deeper its colour lies behind other colours.
 if(stall){const deep=layers(s);for(const p of s.slots)if(p>=0&&s.remaining[p]>0)v-=stall*Math.min(4,deep[s.meta.colours[p]]);}
 return v;
}
// The planner's command at s (null when there is none). depth: commands looked ahead; weight: a free place in cells.
export function plan(s,{rows=ROWS,depth=4,weight=12,stall=0}={}){
 const allowed=plannable(s,rows),start=work(s),memo=new Map();
 const options=m=>R.legal(m).filter(a=>a.type==='bridge'||m.meta.groups[a.pod].every(p=>allowed.has(p)));
 const moves=R.legal(s);if(!moves.length)return null;
 function visit(m,d){
  if(d===0||m.status!=='Playing')return value(m,start,weight,stall);
  const k=Rules.signature(m)+'@'+d;if(memo.has(k))return memo.get(k);
  const next=options(m);let best=next.length?-Infinity:value(m,start,weight,stall);
  for(const a of next)best=Math.max(best,visit(R.step(m,a).state,d-1));
  memo.set(k,best);return best;
 }
 let best=null,score=-Infinity;
 for(const a of options(s)){const v=visit(R.step(s,a).state,depth-1);if(v>score){score=v;best=a;}}
 return best||moves[0];
}
// Plays a level with the planner in the patient game; returns {won, actions, reveals}.
export function playPlanner(level,opts={}){
 let s=R.create(level);const actions=[];
 while(s.status==='Playing'&&actions.length<500){const a=plan(s,opts);if(!a)break;actions.push(a);s=R.step(s,a).state;}
 return {won:s.status==='Won',status:s.status,actions};
}
