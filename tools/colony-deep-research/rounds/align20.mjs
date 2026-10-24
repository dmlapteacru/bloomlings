// L20 tap times: the owner's 36 taps (sim/l20.json ownerColumns) replayed on the reconstructed tray give each tap's
// capsules and how many slots it fills (2 for a linked pair); the detected events (slottaps.py rises with their size,
// plus traytaps.py bursts no slot rise explains, which are a tap into a slot freed in the same frame) are consumed in
// order.
// node rounds/align20.mjs rounds/detected/l20-traytaps.txt rounds/detected/l20-slottaps.txt > rounds/l20-taps.json
import fs from 'node:fs';
const L=JSON.parse(fs.readFileSync(new URL('../sim/l20.json',import.meta.url))).L20;
const read=f=>fs.readFileSync(f,'utf8').trim().split('\n').map(l=>l.trim().split(/\s+/));
const tray=read(process.argv[2]).map(([t])=>parseFloat(t)).filter(t=>t<397);
const slot=read(process.argv[3]).map(([t,a,,b])=>({t:parseFloat(t),n:+b-+a})).filter(e=>e.t<397);
// merge slot rises 0.25 s apart (a pair shows over two frames)
const rises=[];for(const e of slot){const last=rises.at(-1);if(last&&e.t-last.t2<=.3){last.n+=e.n;last.t2=e.t;}else rises.push({t:e.t,t2:e.t,n:e.n});}
const events=[...rises.map(r=>({t:r.t,n:r.n,src:'slots'})),...tray.filter(t=>!rises.some(r=>Math.abs(r.t-t)<=.5)).map(t=>({t,n:1,src:'tray'}))].sort((a,b)=>a.t-b.t);
// replay the taps on the tray
const cols=Object.values(L.pods).map(c=>c.map(s=>{const m=/^([A-Za-z#])(\d+)(\?)?(?::([a-z]))?$/.exec(s);return {token:s,color:m[1],count:+m[2],mystery:!!m[3],link:m[4]||null};}));
const heads=cols.map(()=>0),taps=[];
for(const c0 of L.ownerColumns){const c=c0-1,p=cols[c][heads[c]];const group=p.link?cols.flatMap((col,ci)=>col.map((q,qi)=>({q,ci,qi}))).filter(x=>x.q.link===p.link):[{q:p,ci:c,qi:heads[c]}];
 for(const g of group){if(g.qi!==heads[g.ci])throw Error('linked capsule not on top: '+g.q.token);heads[g.ci]++;}
 taps.push({column:c0,pods:group.map(g=>`c${g.ci+1}:${g.q.token}`),slots:group.length});}
// consume events
let e=0;for(const tap of taps){let need=tap.slots;const ev=events[e];if(!ev){tap.t=null;continue;}tap.t=ev.t;tap.from=ev.src;
 if(ev.n>need){ev.n-=need;tap.shared=true;}else{e++;need-=ev.n;while(need>0&&events[e]&&events[e].t-ev.t<1.5){need-=events[e].n;e++;}}}
console.log(JSON.stringify({events:events.length,leftover:events.slice(e),taps},null,1));
