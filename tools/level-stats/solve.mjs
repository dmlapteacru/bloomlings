// Full-information checks on the patient model (each tap's work settles before the next tap; botsc's rules): is the
// level winnable, one winning route, how many winning tap sequences there are, the smallest buffer that still wins, the
// risky and one-right-move decisions on the route, and for a recorded attempt the first tap after which the level could
// not be won any more. A search that runs out of budget answers null ("unknown"), never false.
export function solver(E,budget=60000){
 const {idle:R}=E;
 function route(level,capacity){const memo=new Map();let n=0;
  const go=s=>{if(s.status==='Won')return [];if(s.status!=='Playing')return null;const k=R.signature(s);if(memo.has(k))return memo.get(k);if(++n>budget)throw 0;
   for(const a of R.legal(s)){const r=go(R.step(s,a).state);if(r){const out=[a,...r];memo.set(k,out);return out;}}memo.set(k,null);return null;};
  try{const r=go(R.create(level,capacity));return {win:!!r,route:r,nodes:n};}catch{return {win:null,route:null,nodes:n};}}
 // winning tap sequences (each settled), counted exactly while the budget lasts; log10 when it is huge
 function count(level,capacity,cb=budget){const memo=new Map();let n=0;
  const go=s=>{if(s.status==='Won')return 1;if(s.status!=='Playing')return 0;const k=R.signature(s);if(memo.has(k))return memo.get(k);if(++n>cb)throw 0;
   let c=0;for(const a of R.legal(s))c+=go(R.step(s,a).state);memo.set(k,c);return c;};
  try{const c=go(R.create(level,capacity));return {sequences:c,log10:c>0?Math.round(Math.log10(c)*100)/100:null,states:n};}catch{return {sequences:null,states:n};}}
 // one memo for the whole level: a position settled once is never searched again; an unsettled search is not stored
 const memo=new Map();
 const winnable=(s0,b=budget)=>{let n=0;const go=s=>{if(s.status==='Won')return true;if(s.status!=='Playing')return false;const k=R.signature(s);const m=memo.get(k);if(m!==undefined)return m;if(++n>b)throw 0;
   for(const a of R.legal(s))if(go(R.step(s,a).state)){memo.set(k,true);return true;}memo.set(k,false);return false;};
  try{return go(s0);}catch{return null;}};
 // along a route: at each decision, how many legal moves keep it winnable
 function decisions(level,capacity,actions){let s=R.create(level,capacity);const out=[];
  for(const a of actions){const moves=R.legal(s);if(moves.length>1){const res=moves.map(m=>winnable(R.step(s,m).state));out.push({legal:moves.length,good:res.filter(x=>x===true).length,unknown:res.filter(x=>x===null).length});}else out.push({legal:moves.length,good:moves.length,unknown:0});s=R.step(s,a).state;}
  const real=out.filter(d=>d.legal>1&&!d.unknown);
  // from which tap on no move loses any more (risk gone), and how much work is left there
  let safeFrom=out.length;for(let i=out.length-1;i>=0;i--){if(out[i].legal>1&&(out[i].good<out[i].legal||out[i].unknown))break;safeFrom=i;}
  return {steps:out,decisions:real.length,risky:real.filter(d=>d.good<d.legal).length,tight:real.filter(d=>d.good===1).length,unknown:out.filter(d=>d.unknown).length,safeFrom};}
 function minCapacity(level,start){const res={};for(let c=start;c>=1;c--){const r=route(level,c);res[c]=r.win;if(r.win!==true)break;}
  const caps=Object.keys(res).map(Number).sort((a,b)=>a-b),wins=caps.filter(c=>res[c]===true);const min=wins.length?Math.min(...wins):null;
  return {min,provenBelow:min!=null&&res[min-1]===false,tried:res};}
 // a recorded attempt on the live timeline: after each tap, settle a copy (the patient continuation) and search
 function fatal(level,capacity,commands){const {R:RR,T}=E,t=new T.Timeline(RR.create(level,capacity));const wins=[];
  const settled=()=>{const c=new T.Timeline(RR.create(level,capacity),t.snapshot());c.advance(1e9);return winnable(c.state);};
  wins.push(settled());let lastAt=0,idle=0;
  // one clock with the recording: while nothing is pending the engine's clock stands still and the time goes to idle
  for(const c of commands){if(c.t>t.now+idle){if(t.pending){t.advance(c.t-t.now-idle);if(!t.pending&&t.now<c.t-idle)idle+=c.t-idle-t.now;}else idle+=c.t-t.now-idle;}if(c.action.type==='unlock'){if(t.forceUnlock)t.forceUnlock(c.action.column);continue;}let g=0;while(t.check(c.action)&&t.pending&&g++<2000)t.advance(100);const r=t.commit(c.action);if(!r.accepted)return {error:r.reason,wins};lastAt=t.now;wins.push(settled());}
  t.advance(1e9);const k=wins.findIndex((w,i)=>i>0&&w===false&&wins[i-1]!==false);
  return {wins,fatalTap:k>0?k:null,fatalAtMs:k>0?commands[k-1].t:null,lossAtMs:t.state.status==='Won'?null:t.now+idle,tapsAfterFatal:k>0?commands.length-k:null,status:t.state.status};}
 return {route,count,decisions,minCapacity,fatal,winnable};}
