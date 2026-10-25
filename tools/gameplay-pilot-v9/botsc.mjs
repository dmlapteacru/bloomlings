// Bot battery used by v8C acceptance (patient player: each command settles before the next).
import Idle from './idle-rules.cjs';
// Authoring-only memo: replay identical patient transitions, never change the live simulator.
const transitions=new WeakMap();
const R={...Idle,step(s,a){let memo=transitions.get(s.level);if(!memo){memo=new Map();transitions.set(s.level,memo);}const key=Idle.signature(s)+'|'+s.serial+'|'+Array.from(s.ages).join(',')+'|'+JSON.stringify(a);if(memo.has(key))return memo.get(key);const result=Idle.step(s,a);if(memo.size>=12000)memo.clear();memo.set(key,result);return result;}};
export const rng=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const visible=s=>new Set(R.reach(s).targets.map(i=>s.board[i]));
const known=(s,a)=>s.meta.groups[a.pod].every(p=>s.revealed[p]);
const matches=(s,a)=>{const v=visible(s);return s.meta.groups[a.pod].some(p=>s.revealed[p]&&v.has(s.meta.colours[p]));};
const workOf=s=>s.remaining.reduce((a,b)=>a+b,0);
const byVisible=(s,m)=>{const f=m.filter(a=>a.type==='bridge'||(a.type==='tap'&&matches(s,a)));return f.length?f:m;};
export const POLICIES={
 random:(s,m,r)=>m[Math.floor(r()*m.length)],
 visible:(s,m,r)=>{const c=byVisible(s,m);return c[Math.floor(r()*c.length)];},
 left:(s,m)=>m[0],
 right:(s,m)=>{const t=m.filter(a=>a.type==='tap');return t.length?t.at(-1):m[0];},
 leftVisible:(s,m)=>m.find(a=>a.type==='bridge')||m.find(a=>a.type==='tap'&&matches(s,a))||m[0],
 biggestVisible:(s,m)=>{const b=m.find(a=>a.type==='bridge');if(b)return b;const c=byVisible(s,m);return c.reduce((x,a)=>s.remaining[a.pod]>s.remaining[x.pod]?a:x);},
 smallestVisible:(s,m)=>{const b=m.find(a=>a.type==='bridge');if(b)return b;const c=byVisible(s,m);return c.reduce((x,a)=>s.remaining[a.pod]<s.remaining[x.pod]?a:x);},
 mostWork:(s,m)=>{let best=m[0],bw=-1;for(const a of m){if(a.type==='tap'&&!known(s,a))continue;const n=R.step(s,a).state,w=workOf(s)-workOf(n)+(n.status==='Won'?1e6:0);if(w>bw){bw=w;best=a;}}return best;},
};
export const SENSIBLE=['leftVisible','biggestVisible','smallestVisible','mostWork'];
export function play(level,mode,seed){const r=rng(seed);let s=R.create(level),g=0;while(s.status==='Playing'&&g++<500){const m=R.legal(s);s=R.step(s,POLICIES[mode](s,m,r)).state;}return s.status==='Won';}
export function rate(level,mode,n,seed=17){if(!['random','visible'].includes(mode))return play(level,mode,seed)?1:0;let w=0;for(let i=0;i<n;i++)w+=play(level,mode,seed+i*7919);return w/n;}
export function sensible(level){return SENSIBLE.filter(m=>play(level,m,17)).length;}
// Full-information winnability over the same patient steps (null when the budget runs out).
export function winnable(s,budget=60000){let n=0;const memo=new Map();const v=s=>{if(s.status==='Won')return true;if(s.status!=='Playing')return false;const k=R.signature(s);if(memo.has(k))return memo.get(k);if(++n>budget)throw 0;for(const a of R.legal(s))if(v(R.step(s,a).state)){memo.set(k,true);return true;}memo.set(k,false);return false;};try{return v(s);}catch{return null;}}
// First fatal tap of random play: how often it happens on the first or second command.
export function earlyFatal(level,n=40){let early=0,lost=0;for(let i=0;i<n;i++){const r=rng(101+i*7919);let s=R.create(level),k=0;while(s.status==='Playing'){const m=R.legal(s),a=m[Math.floor(r()*m.length)],next=R.step(s,a).state;k++;const w0=winnable(s),w1=winnable(next);if(w0===null||w1===null)break;if(w0&&!w1){lost++;if(k<=2)early++;break;}s=next;}}return {early,lost,n};}
export function report(level,n=300){const out={};for(const m of Object.keys(POLICIES))out[m]=rate(level,m,n);out.sensibleWins=SENSIBLE.filter(m=>out[m]===1).length;return out;}
// "Mindless" pattern tapping: a fixed order of columns (or by the number), ignoring colours and the board.
const columnOf=(s,pod)=>s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===pod);
export const PATTERNS={
 'колонка слева до конца':n=>({order:[...Array(n).keys()]}),
 'колонка справа до конца':n=>({order:[...Array(n).keys()].reverse()}),
 'ряд слева направо':n=>({at:k=>k%n}),
 'ряд справа налево':n=>({at:k=>n-1-(k%n)}),
 'змейка':n=>({at:k=>{const r=k%(2*n);return r<n?r:2*n-1-r;}}),
 'середина, потом края':n=>{const m=Math.floor(n/2),o=[m,...[...Array(n).keys()].filter(c=>c!==m)];return {at:k=>o[k%n]};},
 'по две из колонки':n=>({at:k=>Math.floor(k/2)%n}),
 'самое большое число':()=>({number:'big'}),
 'самое маленькое число':()=>({number:'small'}),
};
export function playPattern(level,name){let s=R.create(level),k=0,g=0;const p=PATTERNS[name](level.tray.length),n=level.tray.length;
 while(s.status==='Playing'&&g++<500){const m=R.legal(s);let a=m.find(x=>x.type==='bridge');
  if(!a){const taps=m.filter(x=>x.type==='tap');
   if(p.number)a=taps.reduce((x,y)=>(p.number==='big'?s.remaining[y.pod]>s.remaining[x.pod]:s.remaining[y.pod]<s.remaining[x.pod])?y:x);
   else{const byCol=new Map(taps.map(t=>[columnOf(s,t.pod),t]));if(p.order)a=p.order.map(c=>byCol.get(c)).find(Boolean);else{for(let j=0;j<4*n&&!a;j++){a=byCol.get(p.at(k+j));if(a)k+=j;}k++;}a||=taps[0];}}
  s=R.step(s,a).state;}
 return s.status==='Won';}
export function patternWins(level){return Object.keys(PATTERNS).filter(n=>playPattern(level,n));}
// Diagnostic of fatal random taps within the first two commands (full-information search;
// unresolved positions count as winnable, so the result is optimistic, not an exact probability).
export function earlyRisk(level,budget=60000){const memo=new Map(),win=s=>{const k=R.signature(s);if(!memo.has(k)){const w=winnable(s,budget);memo.set(k,w!==false);}return memo.get(k);};
 const start=R.create(level);if(!win(start))return 1;let risk=0;const m1=R.legal(start);
 for(const a of m1){const s1=R.step(start,a).state;if(!win(s1)){risk+=1/m1.length;continue;}if(s1.status!=='Playing')continue;const m2=R.legal(s1);for(const b of m2)if(!win(R.step(s1,b).state))risk+=1/m1.length/m2.length;}
 return Math.round(risk*1000)/1000;}
