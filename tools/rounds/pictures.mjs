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
// node tools/rounds/pictures.mjs publish   rebuild content/rounds/approved/ (approve, reject, replace and levels.mjs place
//   run it too): locked/P####.* the pictures locked to levels 1–3, pool/P####.* every other approved picture, README.md
//   the index with each code's state, free or in a round (read from content/rounds/levels/)
// node tools/rounds/pictures.mjs tag <ids…> [--family …] [--direction …] [--device …] [--subject …] [--source …] [--reference …]
//   set a picture's theme catalog tags (metadata only, the matrix stays)
// node tools/rounds/pictures.mjs status [--free|--used]   approved pictures by code: free, or the level that uses them
// node tools/rounds/pictures.mjs motifs [--status approved] [--max 3]   recurring motifs (fish, lighthouse, watering a
//   flower …) in titles and subjects, main or secondary; a motif at --max or more is not drawn again unless the owner asks
// node tools/rounds/pictures.mjs verify <batch.mjs> [--cells a-b] [--colours a-b] [--target n] [--level n] [--out <png>]
//   count and check a module's matrices without recording them (skill bloomlings-board-picture), sheet from the matrices
// A batch module: export default {theme, pictures:[{title, theme?, forLevel?, family, direction?, device?, subject?, source?,
// reference?, rows:[…top first…]}]} (tags: product/design/Bloomlings-picture-theme-catalog.md); rows use the
// palette letters GPBACDVYOLMRKNSE, '.' for empty, '#' for an impassable strip and 'b' for a bridge site on it.
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
import {usage,FIXED_VOLUMES,APPROVED,PICTURES,CODES,PALETTE,manifest,saveManifest,nextSeq,pid,picture,writeJSON,measure,stageOf,fitsStage,sizeBin,png,FONT,rgbOf,arg,flag} from './lib.mjs';
// recurring motifs for the repeat guard (the owner, 2026-10-26: "не сильно повторяться, а то уже сколько рыб"): main or
// secondary, matched in a picture's title and subject
const E='(?![а-яё])';const re=x=>new RegExp(x.replace(/\$/g,E));
const MOTIFS=[['рыбы',re('рыб(?!ак)|кит$|клоун')],['подводный мир',re('под вод|подвод|батискаф|водолаз|осьминог|аквариум|губка боб')],['маяки',re('маяк')],['космонавты и ракеты',re('космонавт|астероид|ракет|инопланет')],
 ['полив цветка',re('полива|лейк')],['роботы',re('робот|валл|бимо')],['чайники',re('чайник')],['чашки и кружки',re('чашк|кружк|какао')],['совы',re('сов[аыу]$')],['пингвины',re('пингвин')],
 ['кошки',re('(^| )(кот|кота|котик|котёнок)$|кошк')],['собаки',re('щен|собак')],['лисы',re('(^| )лис[аиёы]?$|лисён')],['медведи',re('медвед|мишк|тоторо')],['драконы',re('дракон')],['динозавры',re('динозавр')],
 ['привидения и духи',re('привиден|дух$')],['единороги',re('единорог')],['лампы и фонари',re('лампа|лампочк|фонар')],['книги',re('книг')],['ретро-техника',re('ретро|автомат|фотоаппарат|телефон|радио|проигрыват')],
 ['инструменты музыки',re('гитар|барабан|пластинк')],['торты и пончики',re('торт|пончик|капкейк')],['печенье и выпечка',re('печень|круассан|выпечк')],['сундуки',re('сундук')],['зонты',re('зонт')],
 ['воздушные шары',re('воздушн[а-яё]* шар')],['корабли и лодки',re('корабл|лодк|парусник')],['облака с лицом',re('облако спит|облачко')],['цветок в горшке',re('в горшке|горшок')],['домики-сказки',re('домик|дом в|дом на')]];
const cmd=process.argv[2];
const idsFrom=args=>{const m=manifest(),st=arg('--status');let ids=args.filter((a,i)=>/^P?\d+$/.test(a)&&!(args[i-1]||'').startsWith('--')).map(a=>a.startsWith('P')?a:pid(+a));
 if(!ids.length&&st)ids=m.pictures.filter(p=>p.status===st).map(p=>p.id);if(!ids.length)ids=m.pictures.map(p=>p.id);return ids;};
async function load(file){return (await import(pathToFileURL(path.resolve(file)).href+'?t='+Date.now())).default;}
function validRows(rows){if(!Array.isArray(rows)||!rows.length)return 'нет строк';const w=rows[0].length;if(rows.some(r=>r.length!==w))return 'строки разной длины';
 const bad=new Set(rows.join('').replace(new RegExp('['+CODES+'.#b]','g'),''));return bad.size?'чужие символы: '+[...bad].join(''):null;}
function record(entry,seq,batch,version=1,history=[]){const err=validRows(entry.rows);if(err)throw Error(`#${seq} «${entry.title}»: ${err}`);
 const pic={id:pid(seq),seq,version,title:entry.title,theme:entry.theme||batch.theme||null,...tagsOf(entry,batch),forLevel:entry.forLevel||seq,w:entry.rows[0].length,h:entry.rows.length,rows:entry.rows,...measure(entry),status:'draft',batch:batch.name||null,createdAt:new Date().toISOString().slice(0,10),history};
 writeJSON(path.join(PICTURES,pic.id+'.json'),pic);return pic;}
// the theme catalog's tags (product/design/Bloomlings-picture-theme-catalog.md): family (one main theme family),
// direction (creative direction), device (composition device), subject, source (оригинальный / Bloomlings / сторонняя
// вселенная / интернет-мем/тренд, with the reference for the last two)
const THEMES=JSON.parse(fs.readFileSync(new URL('./themes.json',import.meta.url),'utf8'));
const FAMILIES=new Set([...THEMES.groups.flatMap(g=>g.families.map(f=>f.name)),...THEMES.internet.families]),DIRECTIONS=new Set(THEMES.directions.map(d=>d.name));
const TAGS=['family','direction','device','subject','source','reference'];
const tagsOf=(e,b={})=>Object.fromEntries(TAGS.map(k=>[k,e[k]??b[k]??null]).filter(([,v])=>v!==null));
const entryOf=p=>({id:p.id,seq:p.seq,version:p.version,title:p.title,theme:p.theme,family:p.family||null,direction:p.direction||null,forLevel:p.forLevel,w:p.w,h:p.h,N:p.N,colours:p.colours,status:p.status,batch:p.batch,note:p.note||null,usedBy:p.usedBy||null});
function check(p){const issues=[],warn=[];const s=stageOf(p.forLevel),m=measure(p);
 if(!fitsStage(m,p.forLevel))issues.push(s.fixed?`уровень ${p.forLevel} требует ${s.fixed}: ${s.cells[0]} клеток, ${s.colours[0]} цв.`:`вне диапазона этапа L${p.forLevel}: ${s.cells.join('–')} клеток, ${s.colours.join('–')} цв.${s.compact?' (или компактная 200–399 / 3–6)':''}`);
 if(m.N>1400)issues.push('больше 1400 клеток — расширенный набор');if(m.colours>11)issues.push('больше 11 цветов — расширенный набор');
 if(!s.fixed&&m.N>=s.cells[0]&&m.N<s.cells[0]+(s.cells[1]-s.cells[0])/4)warn.push(`у нижней границы этапа (${s.cells.join('–')}): целиться ближе к середине`);
 if(!p.family)warn.push('нет семейства каталога тем (family)');else if(!FAMILIES.has(p.family))warn.push(`семейство «${p.family}» не из каталога тем`);
 if(p.direction&&!DIRECTIONS.has(p.direction))warn.push(`направление «${p.direction}» не из каталога тем`);
 if(p.source&&/сторонн|мем|тренд/.test(p.source)&&!p.reference)warn.push('сторонний источник или мем без reference: нужна проверка прав (каталог тем §4, §10)');
 if(m.colours>=10)warn.push('10–11 цветов: не больше 2 в любых 20 уровнях, цвет должен улучшать читаемость');
 for(const [c,v] of Object.entries(m.volumes))if(v<3)warn.push(`цвет ${c} (${PALETTE[c].name}): ${v} кл. — очень маленькая деталь`);
 if(p.w+4>48||p.h+4>48)issues.push('поле с рамкой больше 48×48');
 // every coloured cell can be opened: it is joined to the frame through cells that are not walls
 // a stream row across the whole width closes the ring too (boardOf); a bridge site counts as passable, since one is built
 const W=p.w+2,H=p.h+2,stream=y=>y>=1&&y<=p.h&&/^[#b]+$/.test(p.rows[y-1]),cell=(x,y)=>x<1||y<1||x>p.w||y>p.h?(stream(y)?'#':'.'):p.rows[y-1][x-1],start=(p.h+1)+'',seen=new Set(['0,'+start]),q=[[0,p.h+1]];
 while(q.length){const [x,y]=q.pop();for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=W||v>=H||seen.has(u+','+v))continue;const c=cell(u,v);if(c==='#')continue;seen.add(u+','+v);q.push([u,v]);}}
 let shut=0;for(let y=1;y<=p.h;y++)for(let x=1;x<=p.w;x++)if(CODES.includes(cell(x,y))&&!seen.has(x+','+y))shut++;if(shut)issues.push(`${shut} цветных клеток отрезаны стенами`);
 const sites=p.rows.join('').split('b').length-1;if(sites&&sites!==2)issues.push(`мест моста ${sites}, нужно ровно 2`);
 return {id:p.id,seq:p.seq,forLevel:p.forLevel,N:m.N,colours:m.colours,size:sizeBin(m.N),ok:!issues.length,issues,warn};}
function sheet(pics,out,cols){const cs=Math.max(5,Math.floor(320/Math.max(...pics.map(p=>Math.max(p.w,p.h)))));
 const tw=Math.max(...pics.map(p=>p.w))*cs+16,lab=34,th=Math.max(...pics.map(p=>p.h))*cs+lab+16,C=Math.min(cols,pics.length),R=Math.ceil(pics.length/C),Wt=C*tw,Ht=R*th,px=Buffer.alloc(Wt*Ht*3,255);
 const put=(x,y,[r,g,b])=>{if(x<0||y<0||x>=Wt||y>=Ht)return;const i=(y*Wt+x)*3;px[i]=r;px[i+1]=g;px[i+2]=b;};
 const rect=(x0,y0,w,h,c)=>{for(let y=y0;y<y0+h;y++)for(let x=x0;x<x0+w;x++)put(x,y,c);};
 const text=(s,x,y,k,c)=>{for(const ch of s){const f=FONT[ch]||FONT[' '];for(let j=0;j<5;j++)for(let i=0;i<3;i++)if(f[j*3+i]==='1')rect(x+i*k,y+j*k,k,k,c);x+=4*k;}};
 pics.forEach((p,n)=>{const ox=(n%C)*tw+8,oy=Math.floor(n/C)*th+8,ink=p.inUse?[150,150,150]:p.status==='approved'?[30,130,60]:p.status==='rejected'?[190,40,40]:[30,30,30];
  const name=String(p.label||p.seq);text(name,ox,oy,4,ink);text(p.inUse?`#${+p.inUse.slice(1)}`:`${p.N}/${p.colours}`,ox+4*4*(name.length+1),oy+8,2,[90,90,90]);
  p.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++){const c=r[x],col=c==='.'?[248,247,243]:c==='#'?[70,70,70]:c==='b'?[160,110,60]:rgbOf(PALETTE[c].color);rect(ox+x*cs,oy+lab+y*cs,cs,cs,col);}});
  rect(ox-1,oy+lab-1,p.w*cs+2,1,[200,200,200]);rect(ox-1,oy+lab+p.h*cs,p.w*cs+2,1,[200,200,200]);});
 fs.mkdirSync(path.dirname(path.resolve(out)),{recursive:true});fs.writeFileSync(out,png(Wt,Ht,px));
 const md=out.replace(/\.png$/,'.md');fs.writeFileSync(md,`# Картинки на утверждение\n\nНомер на листе — порядковый номер картинки; рядом клетки/цвета. Ответь номерами: какие утвердить, какие переделать (и что поменять).\n\n| № | Название | Тема | Размер | Клеток | Цветов | Объёмы | Для уровня | Статус |\n|---:|---|---|---|---:|---:|---|---:|---|\n`+pics.map(p=>`| ${p.label||p.seq} | ${p.title} | ${p.theme||'—'} | ${p.w}×${p.h} | ${p.N} | ${p.colours} | ${Object.entries(p.volumes||{}).sort((a,b)=>b[1]-a[1]).map(([c,v])=>`${c} ${v}`).join(' · ')} | ${p.forLevel} | ${p.status||'—'} |`).join('\n')+'\n');
 return {out,md,count:pics.length};}
// ---- the approved folder: one place for every approved picture. Levels 1–3 are locked to their level; all others form
// one pool that the level skill places by its rules (a picture's number or forLevel is no level assignment) ----
const BANDS=[['4–9',4],['10–24',10],['25–60',25],['61+',61]];
function picturePng(p,cs=12){const W=p.w*cs,H=p.h*cs,px=Buffer.alloc(W*H*3,255);
 p.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++){const c=r[x],col=c==='.'?[255,255,255]:rgbOf(PALETTE[c].color);
  for(let j=0;j<cs;j++)for(let i=0;i<cs;i++){const k=((y*cs+j)*W+x*cs+i)*3,edge=c!=='.'&&(i===cs-1||j===cs-1);px[k]=edge?Math.round(col[0]*.85):col[0];px[k+1]=edge?Math.round(col[1]*.85):col[1];px[k+2]=edge?Math.round(col[2]*.85):col[2];}}});
 return png(W,H,px);}
// usedBy comes from the placed levels; the manifest and picture files are brought in line with them
function syncUsage(){const u=usage(),m=manifest();let changed=false;
 for(const e of m.pictures){const by=u[e.id]||null;if((e.usedBy||null)!==by){e.usedBy=by;changed=true;const p=picture(e.id);p.usedBy=by;writeJSON(path.join(PICTURES,e.id+'.json'),p);}}
 if(changed)saveManifest(m);return u;}
function publish(){const u=syncUsage(),m=manifest(),pics=m.pictures.filter(e=>e.status==='approved').map(e=>picture(e.id));
 for(const d of ['locked','pool']){const dir=path.join(APPROVED,d);fs.rmSync(dir,{recursive:true,force:true});fs.mkdirSync(dir,{recursive:true});}
 const rows=[];
 for(const p of pics){const mm=measure(p),name=p.id,by=u[p.id]||null,dir=path.join(APPROVED,p.locked?'locked':'pool');
  const stages=p.locked?['L'+p.locked]:BANDS.filter(([,l])=>fitsStage(mm,l)||(stageOf(l).compact&&mm.N>=200&&mm.N<=399)).map(([b])=>b);
  writeJSON(path.join(dir,name+'.json'),{code:p.id,state:by?'used':'free',usedBy:by,version:p.version,title:p.title,theme:p.theme,...tagsOf(p),locked:p.locked||null,stages,w:p.w,h:p.h,N:mm.N,colours:mm.colours,
   volumes:Object.fromEntries(Object.entries(mm.volumes).sort((a,b)=>b[1]-a[1]).map(([c,v])=>[c,{cells:v,name:PALETTE[c].name,hex:PALETTE[c].color}])),approvedAt:p.decidedAt||null,rows:p.rows});
  fs.writeFileSync(path.join(dir,name+'.png'),picturePng(p));
  rows.push({key:(p.locked?0:1)+p.id,row:`| **${p.id}** | ${by?'в раунде '+by:'свободна'} | ${p.locked?'закреплена за L'+p.locked:'пул'} | [${p.title}](${p.locked?'locked':'pool'}/${name}.png) | ${p.family||'—'} | ${p.direction||'—'} | v${p.version} | ${p.w}×${p.h} | ${mm.N} | ${mm.colours} | ${stages.join(', ')||'—'} |`});}
 rows.sort((a,b)=>a.key.localeCompare(b.key));const nUsed=pics.filter(p=>u[p.id]).length;
 fs.writeFileSync(path.join(APPROVED,'README.md'),`# Approved round pictures\n\nGenerated by \`node tools/rounds/pictures.mjs publish\` (approve, reject, replace and \`levels.mjs place\` run it). Do not edit by\nhand.\n\nEvery picture has a code, \`P####\`: its file name, its JSON's \`code\` and the number on \`sheet.png\`. A code is **free** until a placed\nlevel in \`content/rounds/levels/\` uses it, then **in a round** (\`usedBy\`: the level). Free now: **${pics.length-nUsed}**, in rounds: **${nUsed}**.\n\n- \`locked/P####.*\`: the pictures locked to levels 1–3 (rule 17 §4); only that level may use them.\n- \`pool/P####.*\`: every other approved picture. They are interchangeable between rounds: a code does not assign a\n  level. \`bloomlings-round-levels\` places each one by its measured class and the stage ranges it fits (Stages).\n- \`sheet.png\`: all of them; a green number is free, a grey one is in a round (#N: its level).\n\nEach JSON holds the code, state, matrix (\`rows\`, top first, \`.\` empty), size, cell count, colours and the volume of each\nmaterial.\n\n| Code | State | Use | Picture | Family | Direction | Version | Size | Cells | Colours | Stages |\n|---|---|---|---|---|---|---|---|---:|---:|---|\n${rows.map(r=>r.row).join('\n')}\n`);
 if(pics.length){sheet(pics.map(p=>({...p,label:String(p.seq),inUse:u[p.id]||null})),path.join(APPROVED,'sheet.png'),5);fs.rmSync(path.join(APPROVED,'sheet.md'),{force:true});}
 return pics.length;}
if(cmd==='next'){console.log(nextSeq(manifest()));}
else if(cmd==='render'){const b=await load(process.argv[3]);const m=manifest();let seq=+arg('--start',nextSeq(m));
 if(m.pictures.some(p=>p.seq>=seq&&p.seq<seq+b.pictures.length))throw Error('номера заняты: используй next или replace');
 b.name=b.name||path.basename(process.argv[3],'.mjs');for(const e of b.pictures){const pic=record(e,seq++,b);m.pictures.push(entryOf(pic));console.log(pic.id,pic.title,`${pic.w}×${pic.h}`,pic.N,'кл.',pic.colours,'цв.');}saveManifest(m);}
else if(cmd==='replace'){const id=process.argv[3].startsWith('P')?process.argv[3]:pid(+process.argv[3]),b=await load(process.argv[4]),k=+arg('--index',0),old=picture(id);if(!old)throw Error('нет '+id);
 b.name=b.name||path.basename(process.argv[4],'.mjs');const pic=record({forLevel:old.forLevel,...tagsOf(old),...b.pictures[k]},old.seq,b,old.version+1,[...(old.history||[]),{version:old.version,title:old.title,status:old.status,note:old.note||null,rows:old.rows}]);
 const m=manifest();m.pictures=m.pictures.filter(p=>p.id!==id).concat([entryOf(pic)]);saveManifest(m);console.log(pic.id,'v'+pic.version,pic.title,pic.N,'кл.',pic.colours,'цв.');publish();}
else if(cmd==='check'){let bad=0;for(const id of idsFrom(process.argv.slice(3))){const p=picture(id);if(!p)continue;const r=check(p);if(!r.ok)bad++;
  console.log(`${r.ok?'OK ':'НЕТ'} ${r.id} L${r.forLevel} ${r.N} кл. ${r.colours} цв. (${r.size})${r.issues.length?' — '+r.issues.join('; '):''}${r.warn.length?' · '+r.warn.join('; '):''}`);}if(bad)process.exitCode=1;}
else if(cmd==='sheet'){const r=sheet(idsFrom(process.argv.slice(3)).map(picture).filter(Boolean),arg('--out','content/rounds/pictures/sheets/sheet.png'),+arg('--columns',5));console.log(r.out,r.md,r.count,'картинок');}
else if(cmd==='approve'||cmd==='reject'){const m=manifest(),note=arg('--note');for(const id of idsFrom(process.argv.slice(3).filter(a=>a!=='--note'&&a!==note))){const p=picture(id);if(!p)continue;
  if(cmd==='approve'&&!check(p).ok)throw Error(id+' не проходит проверку правил — сначала исправить');
  p.status=cmd==='approve'?'approved':'rejected';p.note=note;p.decidedAt=new Date().toISOString().slice(0,10);
  if(cmd==='approve'&&p.forLevel<=3){const other=m.pictures.find(x=>x.id!==id&&x.status==='approved'&&picture(x.id).locked===p.forLevel);if(other)throw Error(`L${p.forLevel} уже закреплён за ${other.id}`);p.locked=p.forLevel;}
  if(cmd==='reject')delete p.locked;writeJSON(path.join(PICTURES,id+'.json'),p);
  m.pictures=m.pictures.filter(x=>x.id!==id).concat([entryOf(p)]);console.log(id,p.status);}saveManifest(m);console.log('approved/:',publish(),'утверждённых');}
else if(cmd==='verify'){const b=await load(process.argv[3]),range=v=>v?v.split(/[-–]/).map(Number):null;
 const cells=range(arg('--cells')),cols=range(arg('--colours')),target=arg('--target')?+arg('--target'):null,level=arg('--level')?+arg('--level'):null;let bad=0;
 const pics=b.pictures.map((e,k)=>{const err=validRows(e.rows);if(err){bad++;console.log(`НЕТ #${k+1} «${e.title}»: ${err}`);return null;}
  const p={...e,label:e.label||null,seq:e.forLevel||level||k+1,forLevel:e.forLevel||level||k+1,w:e.rows[0].length,h:e.rows.length,...measure(e)};
  const r=check(p),issues=[...r.issues],sum=Object.values(p.volumes).reduce((a,v)=>a+v,0),occupied=e.rows.join('').replace(/[.#b]/g,'').length;
  if(sum!==occupied)issues.push(`сумма объёмов ${sum} ≠ занятым клеткам ${occupied}`);
  if(cells&&(p.N<cells[0]||p.N>cells[1]))issues.push(`клеток ${p.N}, задано ${cells.join('–')}`);
  if(cols&&(p.colours<cols[0]||p.colours>cols[1]))issues.push(`цветов ${p.colours}, задано ${cols.join('–')}`);
  const fixed=FIXED_VOLUMES[p.forLevel],have=Object.values(p.volumes).sort((a,b)=>b-a);
  if(fixed&&have.join('/')!==fixed.join('/'))issues.push(`объёмы ${have.join('/')}, по §4 нужно ${fixed.join('/')}`);
  if(issues.length)bad++;
  console.log(`${issues.length?'НЕТ':'OK '} ${p.label||'#'+(k+1)} «${p.title}» L${p.forLevel} ${p.w}×${p.h}: ${p.N} кл., ${p.colours} цв.${target?` (цель ${target}, ${p.N-target>=0?'+':''}${p.N-target})`:''}${issues.length?' — '+issues.join('; '):''}${r.warn.length?' · '+r.warn.join('; '):''}`);
  console.log('    '+Object.entries(p.volumes).sort((a,b)=>b[1]-a[1]).map(([c,v])=>`${c} ${PALETTE[c].name} ${PALETTE[c].color} ${v}`).join(' · '));
  return p;}).filter(Boolean);
 const out=arg('--out');if(out&&pics.length)console.log(sheet(pics,out,+arg('--columns',5)).out);if(bad)process.exitCode=1;}
else if(cmd==='status'){const u=publish()&&usage(),want=flag('--free')?'free':flag('--used')?'used':null;
 for(const e of manifest().pictures.filter(e=>e.status==='approved')){const p=picture(e.id),by=u[e.id]||null;if(want&&(want==='free')===!!by)continue;
  console.log(e.id,(by?'в раунде '+by:'свободна').padEnd(16),(p.locked?'L'+p.locked+' закреплена':'пул').padEnd(13),String(e.N).padStart(5),'кл.',e.colours,'цв.',e.title);}}
else if(cmd==='tag'){const ids=idsFrom(process.argv.slice(3).filter((a,i,all)=>!a.startsWith('--')&&!(all[i-1]||'').startsWith('--')));const m=manifest();
 for(const id of ids){const p=picture(id);if(!p)continue;for(const k of TAGS){const v=arg('--'+k);if(v!==null)p[k]=v;}writeJSON(path.join(PICTURES,id+'.json'),p);
  m.pictures=m.pictures.filter(x=>x.id!==id).concat([entryOf(p)]);console.log(id,TAGS.map(k=>p[k]?`${k}=${p[k]}`:'').filter(Boolean).join(' · '));}
 saveManifest(m);publish();}
else if(cmd==='motifs'){const st=arg('--status','approved'),max=+arg('--max',3),pics=manifest().pictures.filter(e=>!st||st==='all'||e.status===st).map(e=>picture(e.id)).filter(Boolean);
 const hits={};for(const p of pics){const text=`${p.title} ${p.subject||''}`.toLowerCase();for(const [m,re] of MOTIFS)if(re.test(text))(hits[m]=hits[m]||[]).push(p.id);}
 const rows=Object.entries(hits).sort((a,b)=>b[1].length-a[1].length);for(const [m,ids] of rows)console.log((ids.length>=max?'ХВАТИТ ':'       ')+m.padEnd(18),String(ids.length).padStart(3),ids.join(' '));
 console.log(`${pics.length} картинок (${st}); «ХВАТИТ» — мотив уже есть ${max}+ раз: не рисовать снова ни главным, ни заметной второй деталью, пока владелец не попросит`);}
else if(cmd==='publish'){console.log('approved/:',publish(),'утверждённых');}
else if(cmd==='list'){const st=arg('--status');for(const p of manifest().pictures.filter(p=>!st||p.status===st))console.log(p.id,p.status.padEnd(8),`L${p.forLevel}`.padEnd(6),`${p.w}×${p.h}`.padEnd(6),String(p.N).padStart(5),'кл.',p.colours,'цв.',p.usedBy?'→ '+p.usedBy:'',p.title);}
else{console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));}
