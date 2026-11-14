// The full builder checks (levels.mjs evaluate, 17 §5–§9 and layer B) on a hand-built contour bridge level of wave
// L51–L100, and the level written as a preview for the pilot (--preview) and for `levels.mjs adopt <n> preview/L####.json`.
// The level's accents (definition.recolor, 17 §6) count in N, W and the colours; the card's accent record (accentCheck:
// materials, cells, their pods' rows, «стоят без работы» with and without them) is added as in levels.mjs build.
//
// node full.mjs <level.json> <cls> <n> <outDir>     env: PIC, KIND (better | single)
// Reads pictures from ROUNDS_DIR (default: the repository's content/rounds), writes only <outDir>/L####.json.
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const HERE=path.dirname(fileURLToPath(import.meta.url)),REPO=path.resolve(HERE,'../../../..');
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||path.join(REPO,'content/rounds');
process.argv.splice(2,0,'noop');
const LV=await import(path.join(REPO,'tools/rounds/levels.mjs'));
const L=await import(path.join(REPO,'tools/rounds/lib.mjs'));
const M=await import(path.join(REPO,'tools/rounds/measure.mjs'));
const [lvFile,cls,n,out]=process.argv.slice(3);
const level=JSON.parse(fs.readFileSync(lvFile));const PIC=process.env.PIC,pic=L.picture(PIC),m=L.measure(L.overlaid(pic,null,level.recolor||null));
const e=await LV.evaluate(level,{m,cls,kind:process.env.KIND||'better',tick:s=>process.stderr.write('  '+s+'\n')});
if(e.reason){console.log('REFUSED',e.reason);process.exit(1);}
// the accents as levels.mjs build records them: the materials of the recolouring that the picture does not have
if(level.recolor){const present=new Set([...pic.rows.join('')]),acc={};for(const [x,y,c] of level.recolor){if(present.has(c))continue;const a=acc[c]||(acc[c]={code:c,from:pic.rows[y][x],cells:0,clusters:[]});a.cells++;}
 // clusters: 4-connected groups of one accent material
 for(const a of Object.values(acc)){const cells=level.recolor.filter(r=>r[2]===a.code).map(([x,y])=>[x,y]),seen=new Set();
  for(const c of cells){const k=c+'';if(seen.has(k))continue;const cl=[c];seen.add(k);for(let i=0;i<cl.length;i++)for(const d of cells){const kk=d+'';if(!seen.has(kk)&&Math.abs(d[0]-cl[i][0])+Math.abs(d[1]-cl[i][1])===1){seen.add(kk);cl.push(d);}}a.clusters.push(cl);}}
 e.checks.accents=await LV.accentCheck(level,pic,Object.values(acc),e.checks.pressure);}
const c=e.checks;
console.log('random',e.random.share,e.random.runs,e.random.cls,'all',e.randomAll&&e.randomAll.share,'bridge',JSON.stringify(e.bridge.each.map(x=>[x.win,Math.round(x.share*100)])),'gap',e.bridge.gap,'kind',c.bridgeKind,
 'offered',JSON.stringify(c.bridgeSingle.offered),'forced',c.forced,'opening',c.opening&&`${c.opening.first.wins}/${c.opening.first.openings} ${Math.round(c.opening.three.share*100)}%`,'calm',c.calm&&c.calm.share,
 'pressure',c.pressure.stuck,'traps',JSON.stringify(c.traps),'fair',JSON.stringify(e.fairness),'accents',c.accents&&JSON.stringify(c.accents.pressure),'strategies won',Object.values(e.strategies).filter(Boolean).length+'/'+Object.keys(e.strategies).length);
const Lv={level:+n,cls:e.random.cls,intro:null,picture:PIC,candidate:PIC+'-hand',recipe:level.recipe,N:m.N,W:m.N,colours:m.colours,P:level.pods.length,columns:level.tray.length,linked:level.pods.filter(p=>p.group).length,hidden:level.pods.filter(p=>p.mystery).length,
 ...(level.hiddenCells?{cells:M.cellsHiddenAtStart(level)}:{}),random:e.random,...(e.randomAll?{randomAll:e.randomAll}:{}),solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,bridge:e.bridge,checks:e.checks,engine:'tools/rounds/engine.mjs',definition:{...level,id:PIC,title:pic.title}};
fs.mkdirSync(out,{recursive:true});fs.writeFileSync(`${out}/L${String(n).padStart(4,'0')}.json`,JSON.stringify(Lv));console.log('written',`${out}/L${String(n).padStart(4,'0')}.json`);
