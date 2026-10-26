// Our engine for each set: v8E as played (git archive of 8892a6f4), v9 as it is now, and Colony Flow on cf-engine/ (the
// played v8E engine with CF's link rule, padlocks, crates, chocolate and jars). One patch on the pilot copies, for the
// checks only: 1–8 places (the pilots allow 3–5) so the smallest workable buffer can be searched.
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {execSync} from 'node:child_process';
import {root,V8E_COMMIT} from './levels.mjs';
const cache=new Map();
function copy(commit,dir,cf){
 const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'ls-'));
 if(commit)execSync(`git archive ${commit} ${dir} | tar -x -C ${tmp}`,{cwd:root});else fs.cpSync(path.join(root,dir),path.join(tmp,dir),{recursive:true,filter:s=>!/node_modules|\/work|\/logs/.test(s)});
 const d=path.join(tmp,dir),p=d+'/rules.cjs';let a=fs.readFileSync(p,'utf8');
 const cap="if(!Number.isInteger(capacity)||capacity<3||capacity>5)";if(a.split(cap).length!==2)throw Error('capacity check moved in '+dir);a=a.replace(cap,"if(!Number.isInteger(capacity)||capacity<1||capacity>8)");
 if(cf){const old="if(group.some(p=>!heads.includes(p)))return 'partner-not-exposed';",
  rule="if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';";
  if(a.split(old).length!==2)throw Error('link rule moved');a=a.replace(old,rule);}
 fs.writeFileSync(p,a);
 // the speed knobs of the calibration (cf-engine has them too); a level without them plays as the pilot does
 const t=d+'/timeline.cjs';let b=fs.readFileSync(t,'utf8');
 const knobs=[["gap=tailOf(s)?TAIL.departureGap:PROFILE.departureGap;","gk=s.level.gapScale||1,tk=s.level.tripScale||1,ak=s.level.actScale??tk,gap=(tailOf(s)?TAIL.departureGap:PROFILE.departureGap)*gk;"],
  ["arrive=start+Math.max(PROFILE.minOut,distance*PROFILE.outPerCell),clear=arrive+PROFILE.act,done=clear+Math.max(PROFILE.minReturn,distance*PROFILE.returnPerCell);","arrive=start+Math.max(PROFILE.minOut,distance*PROFILE.outPerCell)*tk,clear=arrive+PROFILE.act*ak,done=clear+Math.max(PROFILE.minReturn,distance*PROFILE.returnPerCell)*tk;"],
  ["type:'release',at:this.now+PROFILE.releaseLag,","type:'release',at:this.now+(s.level.releaseLagMs??PROFILE.releaseLag),"]];
 for(const [x,y] of knobs){if(b.split(x).length!==2)throw Error('timeline moved in '+dir);b=b.replace(x,y);}
 fs.writeFileSync(t,b);return d;}
export async function engine(set){
 if(cache.has(set))return cache.get(set);
 const d=set==='v9'?copy(null,'tools/gameplay-pilot-v9',false):set==='CF'?path.join(root,'tools/level-stats/cf-engine'):copy(V8E_COMMIT,'tools/gameplay-pilot-v8e',false);
 const E={dir:d,R:(await import(d+'/rules.cjs')).default,T:(await import(d+'/timeline.cjs')).default,bots:await import(d+'/botsc.mjs'),idle:(await import(d+'/idle-rules.cjs')).default};
 cache.set(set,E);return E;}
