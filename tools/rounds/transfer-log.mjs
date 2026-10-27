// The transfer log of round waves (spec 006 FR-053, data-model.md TransferLogEntry): content/game/rounds/transfers.jsonl,
// one JSON line per transfer of a wave, moved through exported → validated → published → confirmed.
//
// node tools/rounds/transfer-log.mjs exported <export-dir> [--note "…"]
//      from the exporter's export.json: wave, levels, exceptions, engine, exportedAt. A wave whose last transfer is not
//      confirmed yet is updated in place (its later stages start again); after a confirmed one a new line is appended.
// node tools/rounds/transfer-log.mjs validated <from> <to> [--at <ISO time>]
// node tools/rounds/transfer-log.mjs published <from> <to> --content-version <v> --apk <file or link> [--at <ISO time>]
// node tools/rounds/transfer-log.mjs confirmed <from> <to> [--at <ISO time>]   (the owner checked the sample on the phone)
// node tools/rounds/transfer-log.mjs show [<from> <to>]
//      each stage needs the one before it; --note on any stage replaces the entry's note; --log <file> uses another log.
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath,pathToFileURL} from 'node:url';
import {root} from './lib.mjs';

export const LOG=path.join(root,'content/game/rounds/transfers.jsonl');
const FIELDS=['wave','levels','exceptions','contentVersion','engine','exportedAt','validatedAt','apk','ownerConfirmedAt','note'];
export const stateOf=e=>e.ownerConfirmedAt?'confirmed':e.apk?'published':e.validatedAt?'validated':'exported';
export function read(file=LOG){if(!fs.existsSync(file))return [];return fs.readFileSync(file,'utf8').split('\n').filter(l=>l.trim()).map((l,i)=>{try{return JSON.parse(l);}catch{throw Error(`${file}:${i+1}: not JSON`);}});}
function save(entries,file){fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,entries.map(e=>JSON.stringify(Object.fromEntries(FIELDS.map(k=>[k,e[k]??null])))).join('\n')+(entries.length?'\n':''));}
const lastOf=(entries,a,b)=>{for(let i=entries.length-1;i>=0;i--)if(entries[i].wave[0]===a&&entries[i].wave[1]===b)return i;return -1;};

// one stage; returns the updated entry or throws with a named reason
export function record(stage,{exportDir,from,to,at=new Date().toISOString(),contentVersion,apk,note,file=LOG}={}){const entries=read(file);
 if(stage==='exported'){const m=JSON.parse(fs.readFileSync(path.join(exportDir,'export.json'),'utf8'));
  if(m.format!=='round-export.v1')throw Error(`${exportDir}/export.json is not an export manifest`);
  if(m.fixtureErrors&&m.fixtureErrors.length)throw Error(`the export has ${m.fixtureErrors.length} fixture errors; it cannot be transferred`);
  const [a,b]=m.wave,i=lastOf(entries,a,b),e={wave:[a,b],levels:m.levels,exceptions:m.exceptions||[],contentVersion:null,engine:m.engine,exportedAt:m.exportedAt,validatedAt:null,apk:null,ownerConfirmedAt:null,note:note??(i>=0&&!entries[i].ownerConfirmedAt?entries[i].note:null)};
  if(i>=0&&!entries[i].ownerConfirmedAt)entries[i]=e;else entries.push(e);save(entries,file);return e;}
 const i=lastOf(entries,from,to);if(i<0)throw Error(`no transfer of L${from}–L${to} in the log; record "exported" first`);
 const e=entries[i],need={validated:'exported',published:'validated',confirmed:'published'}[stage];
 if(!need)throw Error(`unknown stage ${stage}`);
 if(e.ownerConfirmedAt)throw Error(`the last transfer of L${from}–L${to} is confirmed already; export again for a new one`);
 if(stateOf(e)!==need&&!(stage==='validated'&&stateOf(e)==='validated'))throw Error(`L${from}–L${to} is ${stateOf(e)}; "${stage}" needs "${need}"`);
 if(stage==='validated')e.validatedAt=at;
 if(stage==='published'){if(!contentVersion||!apk)throw Error('published needs --content-version and --apk');e.contentVersion=contentVersion;e.apk=apk;}
 if(stage==='confirmed')e.ownerConfirmedAt=at;
 if(note!=null)e.note=note;save(entries,file);return e;}

if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 const a=process.argv.slice(2),opt=k=>{const i=a.indexOf(k);return i>=0?a[i+1]:undefined;},stage=a[0],file=opt('--log')?path.resolve(opt('--log')):LOG;
 try{
  if(stage==='show'||!stage){const all=read(file),f=+a[1],t=+a[2],list=f?all.filter(e=>e.wave[0]===f&&e.wave[1]===t):all;
   for(const e of list)console.log(`L${e.wave[0]}–L${e.wave[1]}: ${stateOf(e)} · exported ${e.exportedAt} · validated ${e.validatedAt??'—'} · content ${e.contentVersion??'—'} · apk ${e.apk??'—'} · owner ${e.ownerConfirmedAt??'—'}${e.exceptions.length?` · exceptions ${e.exceptions.map(x=>'L'+x.level).join(', ')}`:''}${e.note?` · ${e.note}`:''}`);
   if(!list.length)console.log('no transfers');}
  else{const e=stage==='exported'?record(stage,{exportDir:path.resolve(a[1]),note:opt('--note'),file}):record(stage,{from:+a[1],to:+a[2],at:opt('--at'),contentVersion:opt('--content-version'),apk:opt('--apk'),note:opt('--note'),file});
   console.log(`${path.relative(root,file)}: L${e.wave[0]}–L${e.wave[1]} ${stateOf(e)}`);}}
 catch(err){console.error(`transfer-log: ${err.message}`);process.exit(1);}}
