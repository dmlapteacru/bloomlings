// Mistake moments on a level's certificate route (the visible-horizon planner, waiting for idle between commands): at
// each command, could some other tap lose the level (risky), and was the chosen one the only winning tap (tight)?
// The measure the owner's ratings follow (rounds-analysis.mjs: v8E L10 0/28 "not Super Hard", L15 7/26 "fun").
// node tools/colony-deep-research/compare/route-risk.mjs <catalog commit|WORKTREE> [ids]
import fs from 'node:fs';import path from 'node:path';import {execSync} from 'node:child_process';
const root=path.resolve(path.dirname(new URL(import.meta.url).pathname),'../../..');
const [commit='WORKTREE',ids]=process.argv.slice(2);
// The current tools (the rules' logic is the same since v8E; botsc.routeRisk: the patient model throughout, every command
// settles before the next) on the catalog of the given commit.
const read=p=>commit==='WORKTREE'?fs.readFileSync(path.join(root,p),'utf8'):execSync(`git show ${commit}:${p}`,{cwd:root,maxBuffer:1<<26}).toString();
const {routeRisk}=await import(path.join(root,'tools/gameplay-pilot-v8e/botsc.mjs'));
const levels=JSON.parse(read('content/pilot-v8e/levels.json')).levels,certs=JSON.parse(read('content/pilot-v8e/certificates.json'));
const want=ids?ids.split(',').map(Number):levels.map(l=>l.id);
for(const id of want){const r=routeRisk(levels[id-1],certs[id-1].actions);console.log(JSON.stringify({id,difficulty:levels[id-1].difficulty,...r}));}
