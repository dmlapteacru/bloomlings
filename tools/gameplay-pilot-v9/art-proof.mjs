// v9 art proof: writes v8E and v9 pictures of the given levels to a JSON file for art-proof.py.
// node tools/gameplay-pilot-v9/art-proof.mjs "[7,10,15,22]" /tmp/proof.json
import fs from 'node:fs';
const current=JSON.parse(fs.readFileSync(new URL('../../content/pilot-v9/levels.json',import.meta.url))).levels;
const L=JSON.parse(fs.readFileSync(new URL('../../content/pilot-v9/baseline-v8e-c6b0bfd4/levels.json',import.meta.url))).levels;
const ids=JSON.parse(process.argv[2]);const out=ids.map(id=>({id,old:L[id-1],new:current[id-1]}));
fs.writeFileSync(process.argv[3],JSON.stringify(out));
