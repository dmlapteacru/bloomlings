// Simple strategies under one tempo rule: the player taps the moment a tap is possible (a free place and a tappable
// capsule on top); `patient` variants wait until all current work has settled first. A player sees what the game shows:
// the top capsules (their colour and number), the board; it never reads a hidden capsule below the top. "Direction" is
// the order of choosing capsules (columns of the tray), not the direction the ants clear the board.
// "top-down" / "bottom-up" use all known materials in the group; "round-robin" cycles columns, while "diagonal"
// minimizes original row + column. Ties: the leftmost column first. Bridges: the first choice, the planner
// tries each.
export const rng=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const columnOf=(s,p)=>s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===p);
const taps=m=>m.filter(a=>a.type==='tap');
const members=(s,a)=>s.meta.groups[a.pod];
const columns=(s,a)=>members(s,a).map(p=>s.level.tray.findIndex(stack=>stack.includes(p)));
const left=(s,a)=>Math.min(...columns(s,a));
const right=(s,a)=>Math.max(...columns(s,a));
const groupLeft=(s,a)=>members(s,a).some(p=>!s.revealed[p])?null:members(s,a).reduce((n,p)=>n+s.remaining[p],0);
function quotas(s,a){const out=new Map();for(const p of members(s,a))if(s.revealed[p]){const c=s.meta.colours[p];out.set(c,(out.get(c)||0)+s.remaining[p]);}return out;}
// Union of the group's materials: a cell of a repeated color is counted once, capped by that color's total quota.
function availableWork(E,t,a){const s=t.state,q=quotas(s,a),work=new Map();
 for(const i of E.R.reach(s).targets)if(t.reserved[i]<0&&q.has(s.board[i]))work.set(s.board[i],(work.get(s.board[i])||0)+1);
 return [...work].reduce((n,[c,v])=>n+Math.min(v,q.get(c)),0);}
function rowOf(s,a){const colors=quotas(s,a),w=s.level.w,h=s.level.h;let sum=0,n=0;
 for(let i=0;i<s.board.length;i++)if(colors.has(s.board[i])){sum+=h-1-Math.floor(i/w);n++;}return n?sum/n:null;}
const select=(s,m,score,max=false)=>taps(m).sort((a,b)=>{
 const x=score(a),y=score(b);return x==null?(y==null?left(s,a)-left(s,b):1):y==null?-1:((max?y-x:x-y)||left(s,a)-left(s,b));})[0];
function diagonal(s,a){return Math.min(...members(s,a).map(p=>{const c=s.level.tray.findIndex(stack=>stack.includes(p));return c+s.level.tray[c].indexOf(p);}));}
function releaseTime(E,t,a,snapshot){const c=new E.T.Timeline(t.state,snapshot),group=members(t.state,a);
 if(!c.commit(a).accepted)return null;c.advance(1e9);
 const releases=c.lastEvents.filter(e=>e.type==='complete'&&group.includes(e.pod));
 return group.every(p=>releases.some(e=>e.pod===p))?Math.max(...releases.map(e=>e.at))-t.now:null;}
const bridgeFirst=m=>m.find(a=>a.type==='bridge');
export const STRATEGIES={
 'left-to-right':{label:'слева направо',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>left(t.state,a))},
 'right-to-left':{label:'справа налево',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>right(t.state,a),true)},
 'top-down':{label:'цвет выше на поле — первым',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>rowOf(t.state,a))},
 'bottom-up':{label:'цвет ниже на поле — первым',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>rowOf(t.state,a),true)},
 'round-robin':{label:'колонки по кругу',make:()=>{let k=0;return (t,m)=>{const b=bridgeFirst(m);if(b)return b;const tp=taps(m),n=t.state.level.tray.length;for(let j=0;j<n;j++){const c=(k+j)%n,a=tp.find(x=>columns(t.state,x).includes(c));if(a){k=(c+1)%n;return a;}}return tp[0];};}},
 'diagonal':{label:'диагональ очереди',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>diagonal(t.state,a))},
 'random':{label:'случайно',random:true,make:seed=>{const r=rng(seed);return (t,m)=>m[Math.floor(r()*m.length)];}},
 'small-first':{label:'маленькие сначала',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>groupLeft(t.state,a))},
 'big-first':{label:'большие сначала',make:()=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>groupLeft(t.state,a),true)},
 'most-now':{label:'больше всего очистит сейчас',make:(seed,E)=>(t,m)=>bridgeFirst(m)||select(t.state,m,a=>availableWork(E,t,a),true)},
 'fast-release':{label:'быстрее освободит место',make:(seed,E)=>(t,m)=>{const b=bridgeFirst(m);if(b)return b;
  // §11: measure the actual common release, with automatic events and no further player action. A stalled group ranks last.
  const snapshot=t.snapshot(),times=new Map(taps(m).map(a=>[a.pod,releaseTime(E,t,a,snapshot)]));
  return select(t.state,m,a=>times.get(a.pod));}},
 'planner':{label:'с планированием (на ход вперёд)',make:(seed,E)=>(t,m)=>{
  // try each move on a copy and let the work it starts run out without further taps (no hidden capsule is read:
  // only the board and the places count); best: won, then free places, then cells cleared, then the tray's left column
  let best=null,bs=-Infinity;for(const a of m){const c=new E.T.Timeline(E.R.create(t.state.level,t.state.slots.length),t.snapshot());c.commit(a);c.advance(1e9);
   const s=c.state,free=s.slots.reduce((n,p)=>n+(p<0),0),score=(s.status==='Won'?1e9:0)+free*1e5+c.processedCells-(columnOf(t.state,a.pod)>=0?columnOf(t.state,a.pod):0)*1e-3;
   if(score>bs){bs=score;best=a;}}return best;}},
};
export const ORDER=Object.keys(STRATEGIES);
