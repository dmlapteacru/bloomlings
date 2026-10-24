import R from './idle-rules.cjs';import {winnable} from './botsc.mjs';
export function diagnose(l,actions,budget=2000){let s=R.create(l),decisions=0,risky=0,tight=0,unknown=0,confirmedRiskyLowerBound=0;const points=[];
 for(let i=0;i<actions.length;i++){const chosen=actions[i],moves=R.legal(s);if(moves.length>1){decisions++;
  const verdicts=moves.map(a=>{if(JSON.stringify(a)===JSON.stringify(chosen))return true;const n=R.step(s,a).state;return n.status==='Won'?true:n.status!=='Playing'?false:winnable(n,budget);});
  const good=verdicts.filter(v=>v===true).length,unresolved=verdicts.includes(null),hasLoss=verdicts.includes(false);if(unresolved)unknown++;else{if(hasLoss)risky++;if(good===1)tight++;}if(hasLoss)confirmedRiskyLowerBound++;
  if(hasLoss||unresolved)points.push({command:i+1,clearedFraction:1-s.board.filter(c=>c>=0).length/l.pods.reduce((n,p)=>n+p.count,0),free:s.slots.filter(p=>p<0).length,alternatives:moves.map((a,j)=>({action:a,patientWin:verdicts[j],certificateChoice:JSON.stringify(a)===JSON.stringify(chosen)}))});
 }s=R.step(s,chosen).state;}
 if(s.status!=='Won')throw Error('Invalid patient witness L'+l.id);return {decisions,risky,tight,unknown,confirmedRiskyLowerBound,possibleRiskyUpperBound:risky+unknown,points};}
