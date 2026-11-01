// Fit the share model on the calibration rows (README.md here) and report its accuracy and the mechanics' effects.
//   node tools/rounds/calib/fit.mjs     → content/work/calib/model.json, content/work/calib/fit.md (and fit.json)
// Accuracy is reported in-sample and by leave-pictures-out cross-validation (5 folds of 6 pictures: the picture is never
// seen in training), on the share scale (R², median and 80th percentile absolute error in points) and as the class zone
// hit rate; the noise floors are the binomial noise of 60 games and the spread between queue seeds at equal parameters.
import fs from 'node:fs';import path from 'node:path';
import {fit,predict,eta,logit,sigmoid,rawCoef,SPECS} from './model.mjs';
const HERE=path.dirname(new URL(import.meta.url).pathname),ROOT=path.resolve(HERE,'../../..'),W=path.join(ROOT,'content/work/calib'),RAW=path.join(W,'raw');
export function loadRows(){const pics=JSON.parse(fs.readFileSync(path.join(RAW,'pictures.json'),'utf8')),F=Object.fromEntries(pics.map(p=>[p.id,p.features])),rows=[];
 for(const f of fs.readdirSync(RAW).filter(f=>f.startsWith('rows-')))for(const l of fs.readFileSync(path.join(RAW,f),'utf8').split('\n'))if(l.trim()){const r=JSON.parse(l);if(!r.error&&F[r.pic])rows.push({...r,f:F[r.pic]});}
 return {rows,pics};}
const q=(a,p)=>{const s=a.slice().sort((x,y)=>x-y);return s.length?s[Math.min(s.length-1,Math.floor(p*(s.length-1)+.5))]:null;};
const median=a=>q(a,.5),meanOf=a=>a.reduce((x,y)=>x+y,0)/(a.length||1);
const clsOf=p=>p>=.5?'N':p>=.2?'H':p>=.05?'SH':'<5';
function metrics(obs,pred){const e=obs.map((o,i)=>Math.abs(o-pred[i])*100),m=meanOf(obs),sst=obs.reduce((s,o)=>s+(o-m)**2,0),sse=obs.reduce((s,o,i)=>s+(o-pred[i])**2,0);
 const cl=obs.filter((o,i)=>clsOf(o)===clsOf(pred[i])).length/obs.length;
 // the middle band only (5–95 %): where a class decision is made
 const mid=obs.map((o,i)=>[o,pred[i]]).filter(([o])=>o>=.05&&o<=.95),em=mid.map(([o,p])=>Math.abs(o-p)*100);
 return {n:obs.length,R2:1-sse/sst,medAbs:median(e),p80Abs:q(e,.8),classHit:cl,mid:{n:mid.length,medAbs:median(em),p80Abs:q(em,.8)}};}
// leave-pictures-out: fold k holds pictures k, k+5, … (pictures sorted by id)
export function crossValidate(rows,spec,folds=5,opts={}){const ids=[...new Set(rows.map(r=>r.pic))].sort(),pred=new Array(rows.length);
 for(let k=0;k<folds;k++){const test=new Set(ids.filter((_,i)=>i%folds===k)),m=fit(rows.filter(r=>!test.has(r.pic)),spec,opts);rows.forEach((r,i)=>{if(test.has(r.pic))pred[i]=predict(m,r);});}
 return pred;}

if(import.meta.url===new URL(process.argv[1],'file://'+process.cwd()+'/').href){
const {rows,pics}=loadRows(),obs=rows.map(r=>r.q),out={rows:rows.length,pictures:new Set(rows.map(r=>r.pic)).size,models:{}},lines=[];
lines.push(`# Калибровка: модель доли случайного теста`,'',`Строк ${rows.length}, картинок ${out.pictures}. Отклик — быстрая доля (60 партий политики §5).`,'');
// noise floor 1: the binomial noise of 60 games at the observed share
const binom=[];{let s=12345;const r=()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};for(const o of obs){let w=0;for(let i=0;i<60;i++)if(r()<o)w++;binom.push(Math.abs(w/60-o)*100);}}
// noise floor 2: part C, three queue seeds at equal parameters (the spread a formula of the parameters cannot remove)
const reps={};for(const r of rows.filter(r=>r.part==='C'))(reps[r.pic+'|'+r.t]=reps[r.pic+'|'+r.t]||[]).push(r.q);
const repDev=[];for(const a of Object.values(reps))if(a.length>=2){const m=meanOf(a);for(const x of a)repDev.push(Math.abs(x-m)*100*Math.sqrt(a.length/(a.length-1)));}
out.noise={binomial60:{medAbs:median(binom),p80Abs:q(binom,.8)},seedSpread:{groups:Object.keys(reps).length,medAbs:median(repDev),p80Abs:q(repDev,.8)}};
lines.push('## Пол шума','',`- биномиальный шум 60 партий: медиана ${median(binom).toFixed(1)} п.п., 80-й перцентиль ${q(binom,.8).toFixed(1)} п.п.`,`- разброс между сидами очереди при одинаковых параметрах (часть C, ${Object.keys(reps).length} групп по 3): медиана ${median(repDev).toFixed(1)} п.п., 80-й перцентиль ${q(repDev,.8).toFixed(1)} п.п. — это предел любой формулы от параметров`,'');
lines.push('## Точность моделей','','| Модель | Членов | R² (в выборке) | R² (CV по картинкам) | Медиана |ошибки| CV, п.п. | 80-й перц. CV, п.п. | В полосе 5–95 %: медиана / 80-й, п.п. | Класс угадан (CV) |','|---|---:|---:|---:|---:|---:|---:|---:|');
for(const spec of Object.keys(SPECS)){const m=fit(rows,spec),ins=metrics(obs,rows.map(r=>predict(m,r))),cvp=crossValidate(rows,spec),cv=metrics(obs,cvp);
 // residual spread on the logit scale (CV, rows with 3–57 wins), for the inverse's probability of landing in a zone
 const res=rows.map((r,i)=>[r,cvp[i]]).filter(([r])=>r.wins>=3&&r.wins<=57).map(([r,p])=>logit(Math.min(.99,Math.max(.01,r.q)))-logit(Math.min(.99,Math.max(.01,p))));
 const sdL=Math.sqrt(meanOf(res.map(x=>x*x)));out.models[spec]={terms:m.names.length,insample:ins,cv,logitResidualSd:sdL};
 lines.push(`| ${spec} | ${m.names.length} | ${ins.R2.toFixed(2)} | ${cv.R2.toFixed(2)} | ${cv.medAbs.toFixed(1)} | ${cv.p80Abs.toFixed(1)} | ${cv.mid.medAbs.toFixed(1)} / ${cv.mid.p80Abs.toFixed(1)} (${cv.mid.n}) | ${Math.round(cv.classHit*100)} % |`);
 if(spec==='params'||spec==='structure')fs.writeFileSync(path.join(W,`model-${spec}.json`),JSON.stringify({...m,logitResidualSd:sdL,cv,fitted:new Date().toISOString(),rows:rows.length},null,1));}
fs.copyFileSync(path.join(W,'model-params.json'),path.join(W,'model.json'));
// the controllable model's main coefficients (raw units: change of the logit per unit)
const mp=JSON.parse(fs.readFileSync(path.join(W,'model-params.json'),'utf8'));
lines.push('','## Коэффициенты модели params (стандартизованные, изменение logit на 1 σ признака), крупнейшие','','| Член | β (σ) |','|---|---:|');
mp.names.map((n,j)=>[n,mp.beta[j+1]]).sort((a,b)=>Math.abs(b[1])-Math.abs(a[1])).slice(0,20).forEach(([n,b])=>lines.push(`| ${n} | ${b.toFixed(2)} |`));
// share against t, pooled (part A): how steep the blend is
lines.push('','## Доля против t (часть A, все картинки)','','| t | медиана доли | 25–75 % | доля строк в зоне Normal / Hard / SH |','|---|---:|---:|---:|');
for(const t of [...new Set(rows.filter(r=>r.part==='A').map(r=>r.t))].sort((a,b)=>a-b)){const a=rows.filter(r=>r.part==='A'&&r.t===t).map(r=>r.q),z=(lo,hi)=>Math.round(a.filter(x=>x>=lo&&x<hi).length/a.length*100);
 lines.push(`| ${t.toFixed(2)} | ${Math.round(median(a)*100)} % | ${Math.round(q(a,.25)*100)}–${Math.round(q(a,.75)*100)} % | ${z(.5,.751)} / ${z(.2,.5)} / ${z(.05,.2)} % |`);}
// per picture strength: the t range that lands in each zone
lines.push('','## По силе картинки (часть A): медиана доли при t = 0 / ⅓ / ½ / ⅔ / 1','','| Картинки | n | t=0 | t=⅓ | t=½ | t=⅔ | t=1 |','|---|---:|---:|---:|---:|---:|---:|');
for(const s of ['weak','mid','strong']){const ids=new Set(pics.filter(p=>p.strength===s).map(p=>p.id)),at=t=>{const a=rows.filter(r=>r.part==='A'&&ids.has(r.pic)&&Math.abs(r.t-t)<.01).map(r=>r.q);return a.length?Math.round(median(a)*100)+' %':'—';};
 lines.push(`| ${s} | ${ids.size} | ${at(0)} | ${at(2/6)} | ${at(.5)} | ${at(4/6)} | ${at(1)} |`);}
// ---- the mechanics' effects: part B, each overlay against the base queue it was laid on (same picture, t, seed) ----
const B=rows.filter(r=>r.part==='B'),base={};for(const r of B)if(r.overlay==='base')base[r.pic+'|'+r.t]=r;
const groups={};for(const r of B){if(r.overlay==='base')continue;const b=base[r.pic+'|'+r.t];if(!b)continue;(groups[r.overlay]=groups[r.overlay]||[]).push([b,r]);}
const cl=x=>Math.min(.98,Math.max(.02,x));
lines.push('','## Влияние механик (часть B: та же очередь без механики и с ней)','',
 'Пары, где база в полосе 5–95 % (иначе 60 партий не видят разницы). Δ — с механикой минус без, в процентных пунктах; Δlogit — по всем парам (доли обрезаны 2–98 %). «Стоят без работы» и вынужденные — по парам, где замерены оба.','',
 '| Механика | пар | Δ доля, медиана (среднее ± 2 se) | Δ спокойно, медиана (среднее) | Δlogit доли | Δ «стоят без работы» | Δ вынужд. | жадная выигрывает: было → стало |','|---|---:|---:|---:|---:|---:|---:|---:|');
out.effects={};
const order=Object.keys(groups).sort();
for(const k of order){const ps=groups[k],inf=ps.filter(([b])=>b.q>=.05&&b.q<=.95),d=inf.map(([b,r])=>(r.q-b.q)*100),dc=inf.map(([b,r])=>(r.calm-b.calm)*100),dl=ps.map(([b,r])=>logit(cl(r.q))-logit(cl(b.q)));
 const se=a=>a.length>1?Math.sqrt(meanOf(a.map(x=>(x-meanOf(a))**2))/(a.length-1)):0,pp=ps.filter(([b,r])=>b.pressure!=null&&r.pressure!=null),dp=pp.map(([b,r])=>r.pressure-b.pressure),pf=ps.filter(([b,r])=>b.forced!=null&&r.forced!=null),df=pf.map(([b,r])=>(r.forced-b.forced)*100);
 const g0=ps.filter(([b])=>b.mostNow).length,g1=ps.filter(([,r])=>r.mostNow).length;
 out.effects[k]={pairs:ps.length,informative:inf.length,dShareMedian:median(d),dShareMean:meanOf(d),dShareSe:se(d),dCalmMedian:median(dc),dCalmMean:meanOf(dc),dLogitMean:meanOf(dl),dLogitSe:se(dl),dPressure:pp.length?meanOf(dp):null,dForced:pf.length?meanOf(df):null,greedy:[g0,g1]};
 const f1=x=>x==null?'—':(x>0?'+':'')+x.toFixed(1);
 lines.push(`| ${k} | ${ps.length} (${inf.length}) | ${f1(median(d))} (${f1(meanOf(d))} ± ${(2*se(d)).toFixed(1)}) | ${f1(median(dc))} (${f1(meanOf(dc))}) | ${f1(meanOf(dl))} ± ${(2*se(dl)).toFixed(2)} | ${pp.length?f1(meanOf(dp))+` (${pp.length})`:'—'} | ${pf.length?f1(meanOf(df)):'—'} | ${g0} → ${g1} из ${ps.length} |`);}
// the base level of the effect: the link and hidden effects by the base share band
lines.push('','### По уровню базы (Δ доля, медиана, п.п.)','','| Механика | база 75–95 % | база 50–75 % | база 20–50 % | база 5–20 % |','|---|---:|---:|---:|---:|');
for(const k of order){const ps=groups[k],band=(lo,hi)=>{const a=ps.filter(([b])=>b.q>=lo&&b.q<hi).map(([b,r])=>(r.q-b.q)*100);return a.length?`${(median(a)>0?'+':'')+median(a).toFixed(0)} (${a.length})`:'—';};
 lines.push(`| ${k} | ${band(.75,.951)} | ${band(.5,.75)} | ${band(.2,.5)} | ${band(.05,.2)} |`);}
// calm vs random: how the calm share follows the random share
const cr=rows.filter(r=>r.calm!=null),bands=[[0,.05],[.05,.2],[.2,.5],[.5,.75],[.75,.95],[.95,1.01]];
lines.push('','## Спокойная доля при данной случайной (все строки)','','| случайная | n | спокойная, медиана (25–75 %) | спокойная ≤ 50 % | жадная «больше работы» выигрывает |','|---|---:|---:|---:|---:|');
for(const [lo,hi] of bands){const a=cr.filter(r=>r.q>=lo&&r.q<hi);if(!a.length)continue;const c=a.map(r=>r.calm);lines.push(`| ${Math.round(lo*100)}–${Math.min(100,Math.round(hi*100))} % | ${a.length} | ${Math.round(median(c)*100)} % (${Math.round(q(c,.25)*100)}–${Math.round(q(c,.75)*100)} %) | ${Math.round(a.filter(r=>r.calm<=.5).length/a.length*100)} % | ${Math.round(a.filter(r=>r.mostNow).length/a.length*100)} % |`);}
// pressure against the share (rows where measured)
const pr=rows.filter(r=>r.pressure!=null);
lines.push('','## «Стоят без работы» при данной случайной доле (строки с замером)','','| случайная | n | стоят без работы, медиана (25–75 %) | вынужденных, медиана |','|---|---:|---:|---:|');
for(const [lo,hi] of bands){const a=pr.filter(r=>r.q>=lo&&r.q<hi);if(!a.length)continue;const p=a.map(r=>r.pressure),f=a.map(r=>r.forced);lines.push(`| ${Math.round(lo*100)}–${Math.min(100,Math.round(hi*100))} % | ${a.length} | ${median(p).toFixed(2)} (${q(p,.25).toFixed(2)}–${q(p,.75).toFixed(2)}) | ${Math.round(median(f)*100)} % |`);}
fs.writeFileSync(path.join(W,'fit.json'),JSON.stringify(out,null,1));fs.writeFileSync(path.join(W,'fit.md'),lines.join('\n')+'\n');console.log(lines.join('\n'));}
