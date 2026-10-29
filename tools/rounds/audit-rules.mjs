// Pure acceptance rules shared by the auditor and candidate placement. These checks deliberately use
// the board and counts, not the candidate's cached verdicts.
import {bridgeSide,sideMatches,slotOf} from './lib.mjs';

export function planIssues(level,plan){
 if(!plan)return [];
 const out=[],d=level.definition||level.level;
 if((level.recipe||'none')!==plan.recipe)out.push(`рецепт ${level.recipe||'none'}, план требует ${plan.recipe} (§13)`);
 if(plan.bridgeSide){
  const side=d?bridgeSide(d):null;
  if(!sideMatches(side,plan.bridgeSide))out.push(`сторона моста ${side||'нет'}, план требует ${plan.bridgeSide} (§13)`);
  const strip=d?.overlay?'overlay':'new';
  if(plan.strip&&strip!==plan.strip)out.push(`полоса ${strip}, план требует ${plan.strip} (§13)`);
  const kind=level.checks?.bridgeKind;
  if(plan.bridgeKind&&plan.bridgeKind!=='any'&&kind!==plan.bridgeKind)out.push(`вид моста ${kind||'не проверен'}, план требует ${plan.bridgeKind} (§13)`);
 }
 return out;
}

// §5: an uncertain class cannot silently become accepted. §8 contains an explicit owner decision for
// Normal with one winning bridge: at least 50% in 400 games, even if its Wilson interval crosses 50%.
export function borderlineDecision(level){
 if(!level.random?.borderline)return {ok:true};
 const r=level.random,bs=level.checks?.bridgeSingle;
 if(level.cls==='Normal'&&r.runs===400&&r.wins/r.runs>=.5&&level.checks?.bridgeKind==='single'&&bs?.ok===true&&bs.each?.filter(x=>x===true).length===1&&bs.each?.filter(x=>x===false).length===1)
  return {ok:true,note:'пограничный Normal: один решающий мост, ≥50 % в 400 партиях — решение владельца 28.10.2026 (§8)'};
 const c=level.classDecision;
 if(c?.decidedBy==='owner'&&c.cls===level.cls&&/^\d{4}-\d{2}-\d{2}$/.test(c.decidedAt||'')&&typeof c.reason==='string'&&c.reason.trim()&&typeof c.source==='string'&&c.source.trim())
  return {ok:true,note:`пограничный класс: решение владельца ${c.decidedAt}, ${c.source}`};
 return {ok:false,note:'класс на границе по Wilson — нет записанного решения владельца (§5)'};
}

export const isRegularNormal=L=>L&&L.cls==='Normal'&&L.level>3&&!L.intro&&slotOf(L.level-1).cls==='Normal';
export const median=xs=>{const a=xs.slice().sort((x,y)=>x-y),m=Math.floor(a.length/2);return a.length?(a.length%2?a[m]:(a[m-1]+a[m])/2):null;};

// levels must include neighbours: from−19 through to+19. Check only windows intersecting the changed
// range. Missing positions already fail the auditor; a median needs a complete 20-level window.
export function windowIssues(levels,from,to){
 const out=[],known=Object.values(levels).filter(Boolean),max=Math.max(to,...known.map(L=>L.level));
 for(let start=Math.max(1,from-19);start<=to&&start+19<=max;start++){
  const end=start+19,window=Array.from({length:20},(_,i)=>levels[start+i]).filter(Boolean);
  const rare=window.filter(L=>L.colours===11);
  if(rare.length>2)out.push(`L${start}–L${end}: картинок с 11 цветами ${rare.length} > 2 в 20 (§2)`);
  if(window.length!==20)continue;
  const regular=window.filter(isRegularNormal),m=median(regular.map(L=>L.random.share));
  if(m!==null&&(m<.5||m>.6))out.push(`L${start}–L${end}: медиана обычных Normal ${Math.round(m*100)} % вне 50–60 % (§5)`);
 }
 const regular=known.filter(isRegularNormal).sort((a,b)=>a.level-b.level);
 for(let i=0;i+10<=regular.length;i++){
  const w=regular.slice(i,i+10);if(w.at(-1).level<from||w[0].level>to)continue;
  const high=w.filter(L=>L.random.share>=.95);
  if(high.length>1)out.push(`L${w[0].level}–L${w.at(-1).level}: Normal на 95–100 % больше одного на десять обычных Normal: ${high.map(L=>'L'+L.level).join(', ')} (§5)`);
 }
 return out;
}

// Placement can reject a rare-palette candidate before it creates an invalid 20-level window.
export function rarePaletteFits(candidate,number,placed){
 if(candidate.colours!==11)return true;
 const all={...placed,[number]:{...candidate,level:number}};
 for(let start=Math.max(1,number-19);start<=number;start++)if(Object.values(all).filter(L=>L&&L.level>=start&&L.level<start+20&&L.colours===11).length>2)return false;
 return true;
}
