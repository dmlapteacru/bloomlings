// Stream variants of two approved environment pictures for the Sprig bridge introductions (the owner, 2026-10-27:
// "ни одна картинка не подошла с бэкграундом? Если их чуть дополнить?"). The originals stay approved in the pool; a
// variant is a new picture. The stream crosses the ground strip, the near bank below is a few rows of grass, so each
// bridge's approach must be cleared first (17 §8; the owner: "мосты хоть чуть-чуть должны быть скрыты").
import fs from 'node:fs';
const src=id=>JSON.parse(fs.readFileSync(new URL(`../${id}.json`,import.meta.url),'utf8'));
// keep `keep` rows from the top, then the stream (two bridge sites), then the near-bank rows
function withStream(id,keep,sites,near){const p=src(id),w=p.w,rows=p.rows.slice(0,keep);
 rows.push([...'#'.repeat(w)].map((c,x)=>sites.includes(x)?'b':c).join(''));
 for(const r of near){if(r.length!==w)throw Error(`${id}: near-bank row ${r.length} ≠ ${w}`);rows.push(r);}
 return rows;}
const tent=withStream('P0023',27,[9,23],[
 'MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM',
 'MMMCCMMMMMMMMMMMMMMMMMMMMMCCMMMM',
 'MMMMMMMMMMMMMMCCCMMMMMMMMMMMMMMM']);
const spirit=withStream('P0092',28,[8,23],[
 'MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM',
 'MMCCMMMMMMMMMMMMMMMMMMMMMMMCCMMM']);
export default {theme:'variants-0114',pictures:[
 {title:'Палатка у ручья',forLevel:16,family:'Походы и отдых на природе',subject:'вариант P0023: палатка и костёр на дальнем берегу ручья, ближний берег с травой и камешками',source:'оригинальный',rows:tent},
 {title:'Лесной дух у ручья',forLevel:18,family:'Фольклор и легенды',direction:'Фольклорная фантазия',subject:'вариант P0092: лесной дух с фонарём на дальнем берегу лесного ручья',source:'оригинальный',rows:spirit}]};
