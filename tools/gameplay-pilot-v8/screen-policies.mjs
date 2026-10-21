import R from './rules.cjs';
import {held,work} from './screen-search.mjs';
export const MODES=['random','leftmost','largest','smallest','release','two-then-random'];
export function rng(seed){let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};}
export function select(s,mode,random,taps){
 let moves=R.legal(s);const pick=a=>a[Math.floor(random()*a.length)];
 if(mode==='random')return pick(moves);
 const bridges=moves.filter(a=>a.type==='bridge');if(bridges.length)return ['largest','leftmost','smallest'].includes(mode)?bridges[0]:pick(bridges);
 if(mode==='two-then-random'&&taps>=2)return pick(moves);
 const colours=R.view(s).colours,visible=new Set(R.reach(s).targets.map(i=>R.CHANNELS[s.board[i]]));
 const known=a=>s.meta.groups[a.pod].every(p=>colours[p]!==null);
 const matching=a=>s.meta.groups[a.pod].filter(p=>colours[p]!==null&&visible.has(colours[p]));
 if(mode==='release'){
  const scored=moves.map(a=>{
   // A one-step evaluator may inspect the resolution of KNOWN actions only.
   // It must never simulate a hidden colour to choose whether to reveal it.
   if(!known(a))return {a,score:held(s)<=2?400:-400};
   const child=R.step(s,a).state;return {a,score:(child.status==='Won'?100000:0)-(child.status!=='Playing'&&child.status!=='Won'?100000:0)+(5-held(child))*1000+work(s)-work(child)};
  });
  const best=Math.max(...scored.map(v=>v.score));return pick(scored.filter(v=>v.score===best)).a;
 }
 const matches=moves.filter(a=>matching(a).length);if(matches.length)moves=matches;
 else {const hidden=moves.filter(a=>!known(a));if(hidden.length&&held(s)<=3)moves=hidden;}
 if(mode==='leftmost')return moves[0];
 if(mode==='largest'||mode==='smallest'){
  const sizes=moves.map(a=>{const members=matching(a);return Math[mode==='largest'?'max':'min'](...(members.length?members:s.meta.groups[a.pod]).map(p=>s.remaining[p]));});
  const target=Math[mode==='largest'?'max':'min'](...sizes);return moves[sizes.indexOf(target)];
 }
 return pick(moves);
}
export function sample(level,mode,trials=32,seed=1){
 const fixed=['leftmost','largest','smallest'].includes(mode),n=fixed?1:trials;
 let wins=0,firstLoss=null;
 for(let trial=0;trial<n;trial++){
  const random=rng(seed+trial*7919);let s=R.create(level),taps=0;const actions=[];
  for(let turn=0;s.status==='Playing';turn++){
   if(turn>level.pods.length+2)throw new Error('Non-monotone policy');
   const action=select(s,mode,random,taps),next=R.step(s,action);if(!next.accepted)throw new Error('Policy made an illegal move');actions.push(action);if(action.type==='tap')taps++;s=next.state;
  }
  if(s.status==='Won')wins++;else firstLoss??=actions;
 }
 return {wins,trials:n,rate:wins/n,firstLoss};
}
