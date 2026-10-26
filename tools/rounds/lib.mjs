// Shared pieces of the round pipeline (product/17_LEVEL_GENERATION_RULES.md): paths, the picture manifest, the
// palette of the pilot (16 colours, 4 per hero), the rules' tables (stages, pod budgets, difficulty calendar, mechanic
// introductions), a dependency-free PNG writer and the board built around a picture.
import fs from 'node:fs';import path from 'node:path';import zlib from 'node:zlib';import {createRequire} from 'node:module';import {fileURLToPath} from 'node:url';
export const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
export const ROUNDS=process.env.ROUNDS_DIR?path.resolve(process.env.ROUNDS_DIR):path.join(root,'content/rounds');
export const APPROVED=path.join(ROUNDS,'approved'),PICTURES=path.join(ROUNDS,'pictures'),LEVELS=path.join(ROUNDS,'levels'),BANK=path.join(ROUNDS,'bank');
const P=createRequire(import.meta.url)(path.join(root,'tools/rounds/engine/palette.cjs'));
export const CODES=P.codes,PALETTE=P.palette('normal');
export const readJSON=(f,d=null)=>fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):d;
export const writeJSON=(f,o)=>{fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,JSON.stringify(o,null,1)+'\n');};
export const pid=seq=>'P'+String(seq).padStart(4,'0'),lid=n=>'L'+String(n).padStart(4,'0');
export const arg=(k,d=null)=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:d;};
export const flag=k=>process.argv.includes(k);

// ---- the picture manifest: one entry per sequence number, in the order the pictures were made ----
export const MANIFEST=path.join(PICTURES,'manifest.json');
export const manifest=()=>readJSON(MANIFEST,{format:'bloomlings-round-pictures-1',pictures:[]});
export const saveManifest=m=>{m.pictures.sort((a,b)=>a.seq-b.seq);writeJSON(MANIFEST,m);};
export const nextSeq=m=>m.pictures.reduce((n,p)=>Math.max(n,p.seq),0)+1;
export const picture=id=>readJSON(path.join(PICTURES,id+'.json'));
// which picture each placed level uses, read from content/rounds/levels/ (the truth of "in a round"): {P0007: 'L0023'}
export function usage(){const u={};if(!fs.existsSync(LEVELS))return u;
 for(const f of fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)).sort()){const L=readJSON(path.join(LEVELS,f));if(L&&L.picture)u[L.picture]=f.replace('.json','');}return u;}

// ---- picture measures (§2): N = coloured coordinates, colours and their volumes ----
export function measure(pic){const vol={};let n=0;for(const r of pic.rows)for(const c of r)if(CODES.includes(c)){vol[c]=(vol[c]||0)+1;n++;}
 return {N:n,colours:Object.keys(vol).length,volumes:vol};}
// stage ranges by level number (§2); compact 200–399 cells / 3–6 colours allowed after L25
export function stageOf(level){
 if(level===1)return {cells:[181,181],colours:[3,3],fixed:'объёмы §4'};if(level===2)return {cells:[240,240],colours:[4,4],fixed:'объёмы §4'};if(level===3)return {cells:[281,281],colours:[6,6],fixed:'объёмы §4'};
 if(level<=9)return {cells:[500,600],colours:[4,7]};if(level<=24)return {cells:[550,1000],colours:[4,8]};if(level<=60)return {cells:[550,1200],colours:[5,9],compact:true};return {cells:[550,1400],colours:[5,9],compact:true};}
export function fitsStage(m,level){const s=stageOf(level),wide=c=>c>=10&&c<=11;
 if(s.fixed)return m.N===s.cells[0]&&m.colours===s.colours[0];
 if(s.compact&&m.N>=200&&m.N<=399)return (m.colours>=3&&m.colours<=6)||wide(m.colours);
 return m.N>=s.cells[0]&&m.N<=s.cells[1]&&((m.colours>=s.colours[0]&&m.colours<=s.colours[1])||wide(m.colours));}
// the exact colour volumes of the first three levels (§4), largest first
export const FIXED_VOLUMES={1:[128,48,5],2:[120,97,19,4],3:[76,70,69,51,13,2]};
export const sizeBin=n=>n<500?'200–499':n<1000?'500–999':'1000–1400';
// pod budget and columns by area (§3): budget P by W, columns by N
export function budget(W){return W<400?[9,24]:W<700?[18,30]:W<1000?[24,40]:[30,48];}
export function columnsFor(N){return N<700?[3]:N<1000?[3,4]:[4];}

// ---- difficulty calendar (§5) and mechanic introductions (§9) ----
export const classOf=n=>n%15===0?'Super Hard':n%5===0?'Hard':'Normal';
export const INTRO=[['hidden',6,8],['links',11,13],['bridge',16,18],['latch',51,53],['canal',101,103],['shutters',151,153],['repair',201,203],['bud',251,253],['seed',301,303],['well',351,353],['hiddenCells',401,403],['stacks',451,453]];
export const MECH_RU={hidden:'скрытые поды',links:'связки',bridge:'мост Sprig',latch:'засов и жёлудь-ключ Twig',canal:'водный канал Drop',shutters:'деревянные створки Twig',repair:'ремонт пятого места Twig',bud:'закрытый бутон Bloom',seed:'семечко Bloom',well:'колодец Drop',hiddenCells:'скрытые клетки Drop',stacks:'стопки одного материала'};
// what the round engine (tools/rounds/engine.mjs) really plays today; the rest waits for an engine change (§14)
export const SUPPORTED=new Set(['hidden','links','bridge']);
// which mechanic a level must introduce, which may appear, and whether the level is a first meeting
export function slotOf(n){const intro=INTRO.find(([,a,b])=>n===a||n===b);
 const open=INTRO.filter(([,a,b])=>n>b).map(([m])=>m);
 return {level:n,cls:classOf(n),intro:intro?intro[0]:null,allowed:intro?[intro[0]]:n<=5?[]:open,
  randomTarget:intro||n===2||n===3?[.9,1]:n===1?[1,1]:null};}

// ---- the board around a picture: an open ring and a wall frame, the Garden Entry in the bottom middle ----
// Picture rows are top first; '.' empty, '#' impassable strip (stream), 'b' a bridge site on the strip (its anchor is the
// cell below it, on the near bank). Level rows are bottom first, as the pilot engine reads them.
export function boardOf(pic){const w=pic.w+4,h=pic.h+4,top=[];
 const wall='#'.repeat(w),ring='#'+'.'.repeat(w-2)+'#';top.push(wall,ring);
 // a stream row (only '#' and 'b' across the whole width) closes the open ring too, so the far bank needs a bridge
 for(const r of pic.rows){const e=/^[#b]+$/.test(r)?'#':'.';top.push('#'+e+r.replace(/b/g,'#')+e+'#');}
 const mid=Math.floor(w/2);top.push(ring,wall.slice(0,mid)+'.'+wall.slice(mid+1));
 const rowsBottomFirst=top.slice().reverse(),choices=[];
 pic.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(r[x]==='b'){const by=h-1-(y+2),bx=x+2,cell=by*w+bx,anchor=cell-w;choices.push({cell,anchor,label:bx<w/2?'Левый мостик':'Правый мостик'});}});
 return {w,h,rowsBottomFirst,entries:[mid],hero:choices.length?{id:'sprig-bridge',choices}:undefined};}

// ---- PNG writer (RGB) and a 3×5 digit font for labels ----
const crcT=new Uint32Array(256).map((_,n)=>{let c=n;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;return c>>>0;});
const crc=b=>{let c=0xffffffff;for(const x of b)c=crcT[(c^x)&255]^(c>>>8);return (c^0xffffffff)>>>0;};
const chunk=(t,d)=>{const len=Buffer.alloc(4);len.writeUInt32BE(d.length);const td=Buffer.concat([Buffer.from(t),d]);const c=Buffer.alloc(4);c.writeUInt32BE(crc(td));return Buffer.concat([len,td,c]);};
export function png(W,H,px){const raw=Buffer.alloc((W*3+1)*H);for(let y=0;y<H;y++){raw[y*(W*3+1)]=0;px.copy(raw,y*(W*3+1)+1,y*W*3,(y+1)*W*3);}
 const ih=Buffer.alloc(13);ih.writeUInt32BE(W,0);ih.writeUInt32BE(H,4);ih[8]=8;ih[9]=2;
 return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ih),chunk('IDAT',zlib.deflateSync(raw)),chunk('IEND',Buffer.alloc(0))]);}
export const FONT={'A':'010101111101101','B':'110101110101110','C':'011100100100011','D':'110101101101110','0':'111101101101111','1':'010110010010111','2':'111001111100111','3':'111001111001111','4':'101101111001001','5':'111100111001111','6':'111100111101111','7':'111001001001001','8':'111101111101111','9':'111101111001111','·':'000000010000000','/':'001001010100100','x':'000101010101000',' ':'000000000000000','#':'101111101111101','?':'111001011000010','!':'010010010000010'};
export const rgbOf=h=>[parseInt(h.slice(1,3),16),parseInt(h.slice(3,5),16),parseInt(h.slice(5,7),16)];
