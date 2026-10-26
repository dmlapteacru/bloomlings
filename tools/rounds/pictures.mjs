// Round pictures (skill bloomlings-round-pictures; rules: product/17_LEVEL_GENERATION_RULES.md §2).
// Pictures are numbered in the order they are made (P0001, P0002, …): a new batch always continues after the highest
// number, rejected ones keep their number and are redrawn in place. The owner approves them on a numbered contact sheet.
//
// node tools/rounds/pictures.mjs next                          next free sequence number
// node tools/rounds/pictures.mjs render <batch.mjs> [--start N] write a batch module's pictures as drafts from N (default: next)
// node tools/rounds/pictures.mjs replace <id> <batch.mjs> [--index k]   redraw one picture in place (version + 1, draft)
// node tools/rounds/pictures.mjs check [ids…|--status draft]   rules check of each picture (§2), with its intended level
// node tools/rounds/pictures.mjs sheet [ids…|--status draft] --out <png> [--columns 5]   numbered contact sheet
// node tools/rounds/pictures.mjs approve <ids…> | reject <ids…> [--note "…"]   record the owner's decision
// node tools/rounds/pictures.mjs list [--status approved|draft|rejected]
// A batch module: export default {theme, pictures:[{title, theme?, forLevel?, rows:[…top first…]}]}; rows use the
// palette letters GPBACDVYOLMRKNSE, '.' for empty, '#' for an impassable strip and 'b' for a bridge site on it.
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
import {PICTURES,CODES,PALETTE,manifest,saveManifest,nextSeq,pid,picture,writeJSON,measure,stageOf,fitsStage,sizeBin,png,FONT,rgbOf,arg,flag} from './lib.mjs';
const cmd=process.argv[2];
const idsFrom=args=>{const m=manifest(),st=arg('--status');let ids=args.filter(a=>/^P?\d+$/.test(a)).map(a=>a.startsWith('P')?a:pid(+a));
 if(!ids.length&&st)ids=m.pictures.filter(p=>p.status===st).map(p=>p.id);if(!ids.length)ids=m.pictures.map(p=>p.id);return ids;};
async function load(file){return (await import(pathToFileURL(path.resolve(file)).href+'?t='+Date.now())).default;}
function validRows(rows){if(!Array.isArray(rows)||!rows.length)return 'нет строк';const w=rows[0].length;if(rows.some(r=>r.length!==w))return 'строки разной длины';
 const bad=new Set(rows.join('').replace(new RegExp('['+CODES+'.#b]','g'),''));return bad.size?'чужие символы: '+[...bad].join(''):null;}
function record(entry,seq,batch,version=1,history=[]){const err=validRows(entry.rows);if(err)throw Error(`#${seq} «${entry.title}»: ${err}`);
 const pic={id:pid(seq),seq,version,title:entry.title,theme:entry.theme||batch.theme||null,forLevel:entry.forLevel||seq,w:entry.rows[0].length,h:entry.rows.length,rows:entry.rows,...measure(entry),status:'draft',batch:batch.name||null,createdAt:new Date().toISOString().slice(0,10),history};
 writeJSON(path.join(PICTURES,pic.id+'.json'),pic);return pic;}
const entryOf=p=>({id:p.id,seq:p.seq,version:p.version,title:p.title,theme:p.theme,forLevel:p.forLevel,w:p.w,h:p.h,N:p.N,colours:p.colours,status:p.status,batch:p.batch,note:p.note||null,usedBy:p.usedBy||null});
function check(p){const issues=[],warn=[];const s=stageOf(p.forLevel),m=measure(p);
 if(!fitsStage(m,p.forLevel))issues.push(s.fixed?`уровень ${p.forLevel} требует ${s.fixed}: ${s.cells[0]} клеток, ${s.colours[0]} цв.`:`вне диапазона этапа L${p.forLevel}: ${s.cells.join('–')} клеток, ${s.colours.join('–')} цв.${s.compact?' (или компактная 200–399 / 3–6)':''}`);
 if(m.N>1400)issues.push('больше 1400 клеток — расширенный набор');if(m.colours>11)issues.push('больше 11 цветов — расширенный набор');
 if(m.colours>=10)warn.push('10–11 цветов: не больше 2 в любых 20 уровнях, цвет должен улучшать читаемость');
 for(const [c,v] of Object.entries(m.volumes))if(v<3)warn.push(`цвет ${c} (${PALETTE[c].name}): ${v} кл. — очень маленькая деталь`);
 if(p.w+4>48||p.h+4>48)issues.push('поле с рамкой больше 48×48');
 // every coloured cell can be opened: it is joined to the frame through cells that are not walls
 const W=p.w+2,H=p.h+2,cell=(x,y)=>x<1||y<1||x>p.w||y>p.h?'.':p.rows[y-1][x-1],seen=new Set(['0,0']),q=[[0,0]];
 while(q.length){const [x,y]=q.pop();for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=W||v>=H||seen.has(u+','+v))continue;const c=cell(u,v);if(c==='#'||c==='b')continue;seen.add(u+','+v);q.push([u,v]);}}
 let shut=0;for(let y=1;y<=p.h;y++)for(let x=1;x<=p.w;x++)if(CODES.includes(cell(x,y))&&!seen.has(x+','+y))shut++;if(shut)issues.push(`${shut} цветных клеток отрезаны стенами`);
 const sites=p.rows.join('').split('b').length-1;if(sites&&sites!==2)issues.push(`мест моста ${sites}, нужно ровно 2`);
 return {id:p.id,seq:p.seq,forLevel:p.forLevel,N:m.N,colours:m.colours,size:sizeBin(m.N),ok:!issues.length,issues,warn};}
function sheet(ids,out,cols){const pics=ids.map(picture).filter(Boolean),cs=Math.max(4,Math.floor(216/Math.max(...pics.map(p=>Math.max(p.w,p.h)))));
 const tw=Math.max(...pics.map(p=>p.w))*cs+16,lab=34,th=Math.max(...pics.map(p=>p.h))*cs+lab+16,C=Math.min(cols,pics.length),R=Math.ceil(pics.length/C),Wt=C*tw,Ht=R*th,px=Buffer.alloc(Wt*Ht*3,255);
 const put=(x,y,[r,g,b])=>{if(x<0||y<0||x>=Wt||y>=Ht)return;const i=(y*Wt+x)*3;px[i]=r;px[i+1]=g;px[i+2]=b;};
 const rect=(x0,y0,w,h,c)=>{for(let y=y0;y<y0+h;y++)for(let x=x0;x<x0+w;x++)put(x,y,c);};
 const text=(s,x,y,k,c)=>{for(const ch of s){const f=FONT[ch]||FONT[' '];for(let j=0;j<5;j++)for(let i=0;i<3;i++)if(f[j*3+i]==='1')rect(x+i*k,y+j*k,k,k,c);x+=4*k;}};
 pics.forEach((p,n)=>{const ox=(n%C)*tw+8,oy=Math.floor(n/C)*th+8,ink=p.status==='approved'?[30,130,60]:p.status==='rejected'?[190,40,40]:[30,30,30];
  text(String(p.seq),ox,oy,4,ink);text(`${p.N}/${p.colours}`,ox+4*4*(String(p.seq).length+1),oy+8,2,[90,90,90]);
  p.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++){const c=r[x],col=c==='.'?[238,236,230]:c==='#'?[70,70,70]:c==='b'?[160,110,60]:rgbOf(PALETTE[c].color);rect(ox+x*cs,oy+lab+y*cs,cs,cs,col);}});
  rect(ox-1,oy+lab-1,p.w*cs+2,1,[200,200,200]);rect(ox-1,oy+lab+p.h*cs,p.w*cs+2,1,[200,200,200]);});
 fs.mkdirSync(path.dirname(path.resolve(out)),{recursive:true});fs.writeFileSync(out,png(Wt,Ht,px));
 const md=out.replace(/\.png$/,'.md');fs.writeFileSync(md,`# Картинки на утверждение\n\nНомер на листе — порядковый номер картинки; рядом клетки/цвета. Ответь номерами: какие утвердить, какие переделать (и что поменять).\n\n| № | Название | Тема | Размер | Клеток | Цветов | Для уровня | Статус |\n|---:|---|---|---|---:|---:|---:|---|\n`+pics.map(p=>`| ${p.seq} | ${p.title} | ${p.theme||'—'} | ${p.w}×${p.h} | ${p.N} | ${p.colours} | ${p.forLevel} | ${p.status} |`).join('\n')+'\n');
 return {out,md,count:pics.length};}
if(cmd==='next'){console.log(nextSeq(manifest()));}
else if(cmd==='render'){const b=await load(process.argv[3]);const m=manifest();let seq=+arg('--start',nextSeq(m));
 if(m.pictures.some(p=>p.seq>=seq&&p.seq<seq+b.pictures.length))throw Error('номера заняты: используй next или replace');
 b.name=b.name||path.basename(process.argv[3],'.mjs');for(const e of b.pictures){const pic=record(e,seq++,b);m.pictures.push(entryOf(pic));console.log(pic.id,pic.title,`${pic.w}×${pic.h}`,pic.N,'кл.',pic.colours,'цв.');}saveManifest(m);}
else if(cmd==='replace'){const id=process.argv[3].startsWith('P')?process.argv[3]:pid(+process.argv[3]),b=await load(process.argv[4]),k=+arg('--index',0),old=picture(id);if(!old)throw Error('нет '+id);
 b.name=b.name||path.basename(process.argv[4],'.mjs');const pic=record({forLevel:old.forLevel,...b.pictures[k]},old.seq,b,old.version+1,[...(old.history||[]),{version:old.version,title:old.title,status:old.status,note:old.note||null,rows:old.rows}]);
 const m=manifest();m.pictures=m.pictures.filter(p=>p.id!==id).concat([entryOf(pic)]);saveManifest(m);console.log(pic.id,'v'+pic.version,pic.title,pic.N,'кл.',pic.colours,'цв.');}
else if(cmd==='check'){let bad=0;for(const id of idsFrom(process.argv.slice(3))){const p=picture(id);if(!p)continue;const r=check(p);if(!r.ok)bad++;
  console.log(`${r.ok?'OK ':'НЕТ'} ${r.id} L${r.forLevel} ${r.N} кл. ${r.colours} цв. (${r.size})${r.issues.length?' — '+r.issues.join('; '):''}${r.warn.length?' · '+r.warn.join('; '):''}`);}if(bad)process.exitCode=1;}
else if(cmd==='sheet'){const r=sheet(idsFrom(process.argv.slice(3)),arg('--out','content/rounds/pictures/sheets/sheet.png'),+arg('--columns',5));console.log(r.out,r.md,r.count,'картинок');}
else if(cmd==='approve'||cmd==='reject'){const m=manifest(),note=arg('--note');for(const id of idsFrom(process.argv.slice(3).filter(a=>a!=='--note'&&a!==note))){const p=picture(id);if(!p)continue;
  if(cmd==='approve'&&!check(p).ok)throw Error(id+' не проходит проверку правил — сначала исправить');
  p.status=cmd==='approve'?'approved':'rejected';p.note=note;p.decidedAt=new Date().toISOString().slice(0,10);writeJSON(path.join(PICTURES,id+'.json'),p);
  m.pictures=m.pictures.filter(x=>x.id!==id).concat([entryOf(p)]);console.log(id,p.status);}saveManifest(m);}
else if(cmd==='list'){const st=arg('--status');for(const p of manifest().pictures.filter(p=>!st||p.status===st))console.log(p.id,p.status.padEnd(8),`L${p.forLevel}`.padEnd(6),`${p.w}×${p.h}`.padEnd(6),String(p.N).padStart(5),'кл.',p.colours,'цв.',p.usedBy?'→ '+p.usedBy:'',p.title);}
else{console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));}
