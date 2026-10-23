// usage: node tools/gameplay-pilot-v8c/review.mjs $PWD/tools/gameplay-pilot-v8c <levels.json the export was played on> <export.json> <rows.json>
// Replays the owner's v8C export on the exact catalog it was played on (timed commands, live timeline).
import fs from 'fs';import {createRequire} from 'module';
const require=createRequire(import.meta.url);
const [tool,levelsPath,resultsPath]=process.argv.slice(2);
const R=require(tool+'/rules.cjs'),{Timeline}=require(tool+'/timeline.cjs');
const data=JSON.parse(fs.readFileSync(levelsPath)),res=JSON.parse(fs.readFileSync(resultsPath));
const crypto=await import('crypto');const hash=crypto.createHash('sha256').update(JSON.stringify(data.levels)).digest('hex');
let ok=0,bad=[],accepted=0,rejected=0;const rows=[];
for(const a of res.attempts){const l=data.levels[a.level-1],t=new Timeline(R.create(l,a.capacity));
 for(const c of a.commands){if(c.modelTimeMs>t.now)t.advance(c.modelTimeMs-t.now);const r=t.commit(c);if(r.accepted)accepted++;else rejected++;}
 const end=a.outcome==='won'||a.outcome==='jammed'?(t.advance(1e9),t.status):t.status;
 const expect=a.outcome==='abandoned'?null:a.outcome==='won'?'Won':'Jammed';
 const match=expect===null||end===expect||(expect==='Jammed'&&end==='Stuck');if(match)ok++;else bad.push({id:a.id,level:a.level,expect,end});
 rows.push({id:a.id,level:a.level,outcome:a.outcome,replayed:end,match});}
console.log(JSON.stringify({catalogHashMatches:hash===res.catalogHash,attempts:res.attempts.length,matched:ok,mismatches:bad,acceptedCommands:accepted,rejectedCommands:rejected}));
fs.writeFileSync(process.argv[5],JSON.stringify(rows));
