// v9 art proof: writes v8E and v9 pictures of the given levels to a JSON file for art-proof.py.
// node tools/gameplay-pilot-v9/art-proof.mjs "[7,10,15,22]" /tmp/proof.json
import fs from 'node:fs';import {sceneLevel} from './scenes.mjs';
const L=JSON.parse(fs.readFileSync(new URL('../../content/pilot-v8e/levels.json',import.meta.url))).levels;
const ids=JSON.parse(process.argv[2]);const out=ids.map(id=>({id,old:L[id-1],new:sceneLevel(id)}));
fs.writeFileSync(process.argv[3],JSON.stringify(out));
