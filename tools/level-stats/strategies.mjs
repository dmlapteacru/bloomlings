// Simple strategies under one tempo rule: the player taps the moment a tap is possible (a free place and a tappable
// capsule on top); `patient` variants wait until all current work has settled first. A player sees what the game shows:
// the top capsules (their colour and number), the board; it never reads a hidden capsule below the top. "Direction" is
// the order of choosing capsules (columns of the tray), not the direction the ants clear the board.
// Names say what they do: "top-down" takes the capsule whose colour lies highest on the board, "bottom-up" the lowest,
// "diagonal" goes round the columns (c1, c2, c3, c1, …). Ties: the leftmost column first. Bridges (v8E/v9 pictures with a choice of bridge): the first choice, the planner
// tries each.
export const rng=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const columnOf=(s,p)=>s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===p);
const taps=m=>m.filter(a=>a.type==='tap');
// how many cells of a capsule's colour the ants can reach now, not yet claimed
function reachable(E,t,p){const s=t.state,col=s.meta.colours[p];let n=0;for(const c of E.R.reach(s).targets)if(s.board[c]===col&&t.reserved[c]<0)n++;return n;}
const groupLeft=(s,a)=>s.meta.groups[a.pod].reduce((n,p)=>n+s.remaining[p],0);
// mean row (0 = top) of a colour's cells on the board
function rowOf(s,p){const col=s.meta.colours[p],w=s.level.w,h=s.level.h;let sum=0,n=0;for(let i=0;i<s.board.length;i++)if(s.board[i]===col){sum+=h-1-Math.floor(i/w);n++;}return n?sum/n:h;}
const bridgeFirst=m=>m.find(a=>a.type==='bridge');
export const STRATEGIES={
 'left-to-right':{label:'слева направо',make:()=>(t,m)=>bridgeFirst(m)||taps(m).sort((a,b)=>columnOf(t.state,a.pod)-columnOf(t.state,b.pod))[0]},
 'right-to-left':{label:'справа налево',make:()=>(t,m)=>bridgeFirst(m)||taps(m).sort((a,b)=>columnOf(t.state,b.pod)-columnOf(t.state,a.pod))[0]},
 'top-down':{label:'цвет выше на поле — первым',make:()=>(t,m)=>bridgeFirst(m)||taps(m).sort((a,b)=>rowOf(t.state,a.pod)-rowOf(t.state,b.pod))[0]},
 'bottom-up':{label:'цвет ниже на поле — первым',make:()=>(t,m)=>bridgeFirst(m)||taps(m).sort((a,b)=>rowOf(t.state,b.pod)-rowOf(t.state,a.pod))[0]},
 'diagonal':{label:'колонки по кругу',make:()=>{let k=0;return (t,m)=>{const b=bridgeFirst(m);if(b)return b;const tp=taps(m),n=t.state.level.tray.length;for(let j=0;j<n;j++){const c=(k+j)%n,a=tp.find(x=>columnOf(t.state,x.pod)===c);if(a){k=c+1;return a;}}return tp[0];};}},
 'random':{label:'случайно',random:true,make:seed=>{const r=rng(seed);return (t,m)=>m[Math.floor(r()*m.length)];}},
 'small-first':{label:'маленькие сначала',make:()=>(t,m)=>bridgeFirst(m)||taps(m).reduce((x,a)=>groupLeft(t.state,a)<groupLeft(t.state,x)?a:x)},
 'big-first':{label:'большие сначала',make:()=>(t,m)=>bridgeFirst(m)||taps(m).reduce((x,a)=>groupLeft(t.state,a)>groupLeft(t.state,x)?a:x)},
 'most-now':{label:'больше всего очистит сейчас',make:(seed,E)=>(t,m)=>bridgeFirst(m)||taps(m).reduce((x,a)=>{const v=y=>t.state.meta.groups[y.pod].reduce((n,p)=>n+Math.min(t.state.remaining[p],reachable(E,t,p)),0);return v(a)>v(x)?a:x;})},
 'fast-release':{label:'быстрее освободит место',make:(seed,E)=>(t,m)=>{const b=bridgeFirst(m);if(b)return b;
  // can it finish with what is reachable now? then the least work; else the most of its work reachable
  const score=a=>{const g=t.state.meta.groups[a.pod],left=g.reduce((n,p)=>n+t.state.remaining[p],0),can=g.reduce((n,p)=>n+Math.min(t.state.remaining[p],reachable(E,t,p)),0);return can>=left?1e6-left:can/left;};
  return taps(m).reduce((x,a)=>score(a)>score(x)?a:x);}},
 'planner':{label:'с планированием (на ход вперёд)',make:(seed,E)=>(t,m)=>{
  // try each move on a copy and let the work it starts run out without further taps (no hidden capsule is read:
  // only the board and the places count); best: won, then free places, then cells cleared, then the tray's left column
  let best=null,bs=-Infinity;for(const a of m){const c=new E.T.Timeline(E.R.create(t.state.level,t.state.slots.length),t.snapshot());c.commit(a);c.advance(1e9);
   const s=c.state,free=s.slots.reduce((n,p)=>n+(p<0),0),score=(s.status==='Won'?1e9:0)+free*1e5+c.processedCells-(columnOf(t.state,a.pod)>=0?columnOf(t.state,a.pod):0)*1e-3;
   if(score>bs){bs=score;best=a;}}return best;}},
};
export const ORDER=Object.keys(STRATEGIES);
