// Our engine for each set: v8E as played (git archive of 8892a6f4), v9 as it is now, and Colony Flow on the played v8E
// engine with CF's link rule (a linked capsule right under another member of its group in the same column comes out with
// it). Two patches on every copy, for the checks only: 1–8 places (the pilots allow 3–5) so the smallest workable
// buffer can be searched, and L3 of Colony Flow can have its six places.
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
 fs.writeFileSync(p,a);return d;}
export async function engine(set){
 if(cache.has(set))return cache.get(set);
 const d=set==='v9'?copy(null,'tools/gameplay-pilot-v9',false):copy(V8E_COMMIT,'tools/gameplay-pilot-v8e',set==='CF');
 const E={dir:d,R:(await import(d+'/rules.cjs')).default,T:(await import(d+'/timeline.cjs')).default,bots:await import(d+'/botsc.mjs'),idle:(await import(d+'/idle-rules.cjs')).default};
 cache.set(set,E);return E;}
