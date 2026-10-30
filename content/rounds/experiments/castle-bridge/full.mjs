// full builder checks (levels.mjs evaluate) on a hand-made castle level, and a preview level for the pilot
process.env.ROUNDS_DIR='/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
process.argv.splice(2,0,'noop');
const fs=await import('node:fs');
const LV=await import('/home/user/bloomlings/tools/rounds/levels.mjs');
const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const [lvFile,cls,n,out]=process.argv.slice(3);
const level=JSON.parse(fs.readFileSync(lvFile));const pic=L.picture('P0152'),m=L.measure(pic);
// ALLOW_LINKS=1: the owner asked for more links than §7's minimum dose for an additional mechanic; the links are then
// left out of the dose check only (everything else is checked as usual)
const probe=process.env.ALLOW_LINKS?{...level,recipe:level.recipe.split('+').filter(t=>t!=='links').join('+')}:level;
const e=await LV.evaluate(probe,{m,cls,kind:'single',tick:s=>process.stderr.write('  '+s+'\n')});
if(e.reason){console.log('REFUSED',e.reason);process.exit(1);}
console.log('random',e.random.share,e.random.cls,'bridge',JSON.stringify(e.bridge.each.map(x=>[x.win,Math.round(x.share*100)])),'kind',e.checks.bridgeKind,'loss',JSON.stringify(e.checks.bridgeLoss),'forced',e.checks.forced);
const Lv={level:+n,cls:e.random.cls,intro:null,picture:'P0152',candidate:'P0152-hand',recipe:level.recipe,N:m.N,W:m.N,colours:m.colours,P:level.pods.length,columns:level.tray.length,linked:level.pods.filter(p=>p.group).length,hidden:level.pods.filter(p=>p.mystery).length,random:e.random,solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,bridge:e.bridge,checks:e.checks,engine:'tools/rounds/engine.mjs',definition:{...level,id:'P0152',title:pic.title}};
fs.mkdirSync(out,{recursive:true});fs.writeFileSync(`${out}/L${String(n).padStart(4,'0')}.json`,JSON.stringify(Lv));console.log('written',out);
