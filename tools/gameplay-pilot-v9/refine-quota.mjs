// A concrete CF construction experiment: same color sum and geometry, different packet sizes.
// Avoid a number-only winning rule without adding noise or changing the illustration.
import fs from 'node:fs';import crypto from 'node:crypto';import {assess,opening,cfgFor} from './synthesis.mjs';import {rng,rate,patternWins} from './botsc.mjs';import {sceneLevel} from './scenes.mjs';import {definition} from './author.mjs';import {planner,withMysteries,MYSTERY} from './fix-lib.mjs';
const id=+(process.argv[2]||25),attempt=+(process.argv[3]||17),role=id===29?'superHard':'hard',r=rng(id*59021+attempt*193),order=.3+r()*3.7,extra=[0,1,-1,2][Math.floor(attempt/100)%4];
let base=definition({...sceneLevel(id),id,subject:'v9-synthesis-'+id},r,{role,difficulty:id===29?'Super Hard':'Hard',order,extra});if(MYSTERY[id])base=withMysteries(base,MYSTERY[id],{tries:25}).level;
const inputHash=crypto.createHash('sha256').update(JSON.stringify(base)).digest('hex'),cfg=cfgFor(id),inside=(n,[a,b])=>n>=a&&n<=b;let tried=0;
for(let a=0;a<base.pods.length;a++)for(let b=a+1;b<base.pods.length;b++)if(base.pods[a].color===base.pods[b].color)for(const delta of [1,-1,2,-2,4,-4,8,-8]){
 const l={...base,pods:base.pods.map(p=>({...p}))};l.pods[a].count+=delta;l.pods[b].count-=delta;if(l.pods.some(p=>p.count<4||p.count>(id===25?50:60)))continue;tried++;
 const patterns=patternWins(l);if(patterns.some(p=>['самое маленькое число','самое большое число'].includes(p))||patterns.length>cfg.patterns)continue;
 if(!inside(rate(l,'random',40,id*137),[cfg.random[0]-.08,cfg.random[1]+.08])||!inside(rate(l,'visible',40,id*149),[cfg.visible[0]-.08,cfg.visible[1]+.08]))continue;
 const p=planner(l);if(!p)continue;console.error('Refine',a,b,delta,'assess');const f=assess(l,{actions:p.actions,planner:p.opts},{strict:true,source:'new-queue'});if(!f)continue;const safety=opening(l);if(safety.upperBound>.1+1e-8)continue;
 const out={...f,attempt,banned:['самое маленькое число','самое большое число'],opening:safety,refinement:{baseAttempt:attempt,inputHash,fromPod:b,toPod:a,delta},heldOut:{trials:200,seed:1850011+id*43,random:rate(l,'random',200,1850011+id*43),visible:rate(l,'visible',200,1850011+id*43)}};
 fs.writeFileSync('content/pilot-v9/synthesis-work/L'+id+'.json',JSON.stringify(out,null,2)+'\n');console.log('Refined',id,tried,a,b,delta,f.primaryDecision.risky,f.decision.risky,f.pacePolicy);process.exit(0);
}
throw Error('No accepted refinement after '+tried+' quota changes');
