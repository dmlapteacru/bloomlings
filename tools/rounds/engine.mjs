// The round engine: the pilot v9 rules and timeline (v8 base with the slot positions of §1: a single pod takes the
// first free place, a release leaves a hole, only an accepted group compacts to the left), copied to a temporary folder
// with three patches for the rules of product/17_LEVEL_GENERATION_RULES.md:
// - a group is available when no other pod stands before any of its members (§8: members may stand over each other);
// - boards up to 48×48 with the frame (pictures up to 1400 cells, §2);
// - 4 or 5 places accepted (the repair of the fifth place, §8, is not played yet).
// Mechanics this engine plays: hidden pods (reveal on reaching the top), linked groups, Sprig's bridge. Others are
// listed in lib.mjs SUPPORTED and are refused by the level builder until the engine plays them.
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';
import {root} from './lib.mjs';
let cached=null;
export async function engine(){
 if(cached)return cached;
 const src=path.join(root,'tools/gameplay-pilot-v9'),dir=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-'));
 for(const f of ['rules.cjs','timeline.cjs','idle-rules.cjs'])fs.copyFileSync(path.join(src,f),path.join(dir,f));
 const p=path.join(dir,'rules.cjs');let a=fs.readFileSync(p,'utf8');
 const swap=(x,y)=>{if(a.split(x).length!==2)throw Error('rules.cjs moved: '+x.slice(0,50));a=a.replace(x,y);};
 swap("if(group.some(p=>!heads.includes(p)))return 'partner-not-exposed';","if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';");
 swap("level.w > 36 || level.h > 36","level.w > 48 || level.h > 48");
 swap("if(level.capacity!==undefined&&![3,4,5].includes(level.capacity))","if(level.capacity!==undefined&&![4,5].includes(level.capacity))");
 fs.writeFileSync(p,a);
 cached={dir,R:(await import(path.join(dir,'rules.cjs'))).default,T:(await import(path.join(dir,'timeline.cjs'))).default,idle:(await import(path.join(dir,'idle-rules.cjs'))).default};
 return cached;}
