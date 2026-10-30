process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||'/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
const fs=await import('node:fs');const M=await import('/home/user/bloomlings/tools/rounds/measure.mjs');
for(const f of process.argv.slice(2)){const level=JSON.parse(fs.readFileSync(f));const w=(await M.bridgeChoicesWin(level)).each;const bs=await M.bridgeSingle(level,60,3,w);console.log(f.split('/').pop(),'offered',JSON.stringify(bs.offered));}
