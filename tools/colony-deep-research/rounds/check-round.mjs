// Checks one transcribed Colony Flow round file (the colony-flow.com videos, 2026-10-25): every tap takes the capsules
// on top of their columns (a linked group whole), a won round empties the tray; prints the capsule totals per colour.
// node tools/colony-deep-research/rounds/check-round.mjs <L<n>.json>
// File: {level, difficulty, video, legend:{X:"name"}, columns:{c1:[tokens top first],...}, rounds:[{attempt, outcome:
// "won"|"lost"|"unfinished", start, end, taps:[[t, "c2", "note"?], ...], events:[[t, "text"], ...]}], mechanics:[...]}
// Token: colour letter + count, then "?" (shown as "?" until it reaches the top), ":x" (linked group x), "!flag"
// (as named in mechanics), e.g. "R20", "Y8?:a", "D15:b!flag"; a padlock is "#<colour>" ("#yellow"): it takes a place
// in its column, is never tapped and leaves the column when its key on the board is reached, listed per attempt in
// "unlocks": [[t, "c2"], ...] (the time it vanished from the top of that column).
import fs from 'node:fs';
const file=process.argv[2],L=JSON.parse(fs.readFileSync(file,'utf8'));
const parse=t=>{const k=/^#([a-z]+)$/.exec(t);if(k)return {lock:k[1],n:0,token:t};const m=/^([A-Za-z])(\d+)(\?)?(?::([a-z]))?(?:!(\w+))?$/.exec(t);if(!m)throw Error('bad token '+t);return {c:m[1],n:+m[2],mystery:!!m[3],link:m[4]||null,flag:m[5]||null,token:t};};
const errors=[],cols=Object.fromEntries(Object.entries(L.columns).map(([c,col])=>[c,col.map(parse)]));
const totals={};for(const col of Object.values(cols))for(const p of col){if(p.lock)continue;totals[p.c]=(totals[p.c]||0)+p.n;if(!L.legend?.[p.c])errors.push(`colour ${p.c} not in legend`);}
for(const col of Object.values(cols))for(const p of col)if(p.lock&&!(L.mechanics||[]).some(m=>/padlock/i.test(m)))errors.push('padlock without a mechanics note');
const groups={};for(const [c,col] of Object.entries(cols))col.forEach((p,i)=>{if(p.link)(groups[p.link]=groups[p.link]||[]).push({c,i,p});});
for(const [g,m] of Object.entries(groups))if(m.length<2)errors.push(`link ${g} has one capsule`);
for(const r of L.rounds||[]){const heads=Object.fromEntries(Object.keys(cols).map(c=>[c,0]));let last=-1;
 for(const [t,c] of r.taps){if(t<last)errors.push(`attempt ${r.attempt}: tap at ${t} before ${last}`);last=t;}
 const steps=[...r.taps.map(x=>({t:x[0],c:x[1]})),...(r.unlocks||[]).map(x=>({t:x[0],c:x[1],unlock:true}))].sort((a,b)=>a.t-b.t||!!b.unlock-!!a.unlock);
 for(const {t,c,unlock} of steps){
  const top=cols[c]?.[heads[c]];if(!top){errors.push(`attempt ${r.attempt} at ${t}: ${c} is empty or unknown`);continue;}
  if(unlock){if(!top.lock)errors.push(`attempt ${r.attempt} at ${t}: unlock but the top of ${c} is ${top.token}`);else heads[c]++;continue;}
  if(top.lock){errors.push(`attempt ${r.attempt} at ${t}: tap on ${c} while its padlock #${top.lock} is on top`);continue;}
  const group=top.link?groups[top.link]:[{c,i:heads[c],p:top}];
  for(const g of group.slice().sort((a,b)=>a.i-b.i)){if(g.i!==heads[g.c]){errors.push(`attempt ${r.attempt} at ${t}: ${g.c} row ${g.i+1} (${g.p.token}, link ${top.link}) is not on top (top is row ${heads[g.c]+1})`);continue;}heads[g.c]++;}}
 const left=Object.entries(cols).reduce((s,[c,col])=>s+col.length-heads[c],0);
 if(r.outcome==='won'&&left)errors.push(`attempt ${r.attempt}: won but ${left} capsules left in the tray`);
 console.log(`attempt ${r.attempt} ${r.outcome}: ${r.taps.length} taps, ${left} capsules left`);}
console.log('capsules',Object.values(cols).flat().filter(p=>!p.lock).length,'padlocks',Object.values(cols).flat().filter(p=>p.lock).length,'columns',Object.keys(cols).length,'totals',JSON.stringify(totals));
if(errors.length){console.log('ERRORS\n'+errors.join('\n'));process.exit(1);}else console.log('OK');
