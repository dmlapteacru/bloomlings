// The water channel's island on rebuilt levels for the owner's review (17 §8 «Остров за водой», 2026-11-07), before the
// wave is transferred: writes each level as round-level.v1 (export.mjs levelV1, the reference engine's fingerprints) and
// its saved solution as commands into a work folder, never into content/game/rounds; the playtest preview renders them
// (`dotnet run --project playtest/preview -- --island-stills <dir> <out>`, IslandStills.cs).
//
// node tools/rounds/island-stills.mjs <dir> <level>…
import path from 'node:path';import fs from 'node:fs';
import {LEVELS,PICTURES,readJSON,lid} from './lib.mjs';
import {levelV1} from './export.mjs';import {fingerprints} from './conformance.mjs';import {validate} from './schema.mjs';
const [dir,...levels]=process.argv.slice(2);if(!dir||!levels.length){console.log('node tools/rounds/island-stills.mjs <dir> <level>…');process.exit(1);}
fs.mkdirSync(dir,{recursive:true});const engine=fingerprints();
for(const n of levels.map(Number)){const L=readJSON(path.join(LEVELS,lid(n)+'.json'));if(!L){console.log(lid(n),'не поставлен');continue;}
 const pic=readJSON(path.join(PICTURES,L.picture+'.json')),lv=levelV1(L,engine);lv.picture.version=pic.version;
 const errs=validate('round-level',lv);if(errs&&errs.length){console.log(lid(n),'схема:',errs.slice(0,3).join('; '));continue;}
 fs.writeFileSync(path.join(dir,lid(n)+'.json'),JSON.stringify(lv,null,1)+'\n');
 fs.writeFileSync(path.join(dir,lid(n)+'.commands.json'),JSON.stringify(L.solution.route.map(a=>a.type==='channel'?{type:'channel',exit:a.exit}:a.type==='bridge'?{type:'bridge',index:a.choice}:{type:'tap',index:a.pod}))+'\n');
 console.log(lid(n),L.picture,'→',dir);}
