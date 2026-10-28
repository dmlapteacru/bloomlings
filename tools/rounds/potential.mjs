// The queue potential of a picture: with no mechanics, how far the random test can move when the queue goes from the
// access order (t = 0) to its reverse (t = 1). A picture whose lowest share stays near 100 % cannot carry a Hard or a
// Normal in the preferred 60–90 % zone by its queue; it needs mechanics, or a change of the picture itself.
// node tools/rounds/potential.mjs <ids…|--approved> [--games 20] [--out file.json]
import fs from 'node:fs';
import {manifest,picture,measure,boardOf,columnsFor,arg,flag} from './lib.mjs';
process.argv.splice(2,0,'noop');const {splitPods,candidate}=await import('./levels.mjs');process.argv.splice(2,1);
import {quickShare} from './measure.mjs';
const games=+arg('--games',20),ids=flag('--approved')?manifest().pictures.filter(p=>p.status==='approved').map(p=>p.id):process.argv.slice(2).filter(a=>/^P\d+$/.test(a));
const out={};
// a drawing module (*.mjs) is measured straight from its matrices, before the pictures are recorded
const mod=process.argv.slice(2).find(a=>a.endsWith('.mjs'));let mods=[];
if(mod){const path=await import('node:path'),{pathToFileURL}=await import('node:url'),b=(await import(pathToFileURL(path.resolve(mod)).href)).default;mods=b.pictures.map((p,i)=>({id:'#'+(i+1),title:p.title,rows:p.rows,w:p.rows[0].length,h:p.rows.length}));}
for(const id of mod?mods.map(p=>p.id):ids){const pic=mod?mods.find(p=>p.id===id):picture(id),m=measure(pic),board=boardOf(pic),base=splitPods(m.volumes,m.N,pic.locked||null);if(base.error){out[id]={error:base.error};continue;}
 const shares=[];for(let k=0;k<12;k++){const cls=k<4?'Normal':k<8?'Hard':'Super Hard',c=candidate(pic,board,base.pods,columnsFor(m.N)[k%columnsFor(m.N).length],board.hero?'bridge':'none',cls,k*7919+101,false);if(c.error)continue;shares.push({cls,share:await quickShare(c.level,games)});}
 const s=shares.map(x=>x.share);out[id]={N:m.N,min:Math.min(...s),max:Math.max(...s),normal:shares.filter(x=>x.cls==='Normal').map(x=>x.share),hardish:shares.filter(x=>x.cls!=='Normal').map(x=>x.share)};
 console.log(id,pic.title,'min',out[id].min.toFixed(2),'Normal',out[id].normal.map(x=>x.toFixed(2)).join(' '),'Hard/SH',out[id].hardish.map(x=>x.toFixed(2)).join(' '));}
if(arg('--out'))fs.writeFileSync(arg('--out'),JSON.stringify(out,null,1));
