// Colony Flow L1-L5, L10 and L20, transcribed from the owner's recordings, played by the unchanged v8D bots.
// The engine and bot files are copied to a temporary folder with two rule lines changed, both needed only by CF levels:
// - the colour alphabet: v8D knows 9 colours, CF uses up to 11 (the 9 letters keep their places, more letters follow);
// - CF's stacked links (L20): a linked capsule right under its partner in the same column comes out with it. v8D links
//   join capsules in different columns, so for v8D levels the rule is unchanged.
// The bots themselves are not touched.
// Usage (from the repo root): node tools/colony-deep-research/v8d-bots/run.mjs
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import assert from 'node:assert/strict';
const root=path.resolve(path.dirname(new URL(import.meta.url).pathname),'../../..');
const src=path.join(root,'tools/gameplay-pilot-v8d'),tmp=fs.mkdtempSync(path.join(os.tmpdir(),'cf-v8d-'));
const FILES=['rules.cjs','timeline.cjs','idle-rules.cjs','botsc.mjs','horizon.mjs','clock.cjs'];
for(const f of FILES)fs.copyFileSync(path.join(src,f),path.join(tmp,f));
{const p=path.join(tmp,'rules.cjs'),a=fs.readFileSync(p,'utf8'),b=a.replace("const CHANNELS = 'GPBACDVYO';","const CHANNELS = 'GPBACDVYOEFHIJKLMNQRSTUWXZ';");
 assert.notEqual(a,b);assert.equal(a.split('\n').filter((l,i)=>l!==b.split('\n')[i]).length,1);
 const old="if(group.some(p=>!heads.includes(p)))return 'partner-not-exposed';",
  cf="if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';";
 assert.equal(b.split(old).length,2);const d=b.replace(old,cf);assert.equal(b.split('\n').filter((l,i)=>l!==d.split('\n')[i]).length,1);fs.writeFileSync(p,d);}
const R=(await import(path.join(tmp,'rules.cjs'))).default,bots=await import(path.join(tmp,'botsc.mjs')),{playPlanner}=await import(path.join(tmp,'horizon.mjs'));
const PLANNERS=[{rows:4,depth:4,weight:12},{rows:4,depth:8,weight:12},{rows:4,depth:6,weight:30}]; // as fix-lib.mjs

// Levels: boards from the recordings' covers, trays from the played queues (tools/colony-deep-research/sim).
const G=JSON.parse(fs.readFileSync(path.join(root,'product/audits/colony-flow-2026-10-20/owner-video/boards.json'))),
 T=JSON.parse(fs.readFileSync(path.join(root,'tools/colony-deep-research/sim/levels.json')));
// L10's ten "?" capsules (column, index from the top), from the recording: colony-flow-levels-deep-dive-2026-10-20.md §9.
const HIDDEN={L10:[[0,2],[0,5],[0,6],[0,7],[1,1],[1,4],[1,5],[2,2],[2,6],[2,7]]};
const LABEL={L1:'Normal',L2:'Normal',L3:'Normal',L4:'Normal',L5:'Hard',L10:'Super Hard',L20:'Super Hard'};
// L20 (sim/l20.json): the whole board and the queue with links ("S35:a") and "?" capsules ("Y30?").
const L20=JSON.parse(fs.readFileSync(path.join(root,'tools/colony-deep-research/sim/l20.json'))).L20;
function buildL20(){
 const rows=L20.board.rows.map(r=>r.split('')),need={};
 for(const col of Object.values(L20.pods))for(const p of col){const [k]=p.split(':');need[k[0]]=(need[k[0]]||0)+parseInt(k.slice(1));}
 const syms=Object.keys(need),map=Object.fromEntries(syms.map((s,i)=>[s,R.CHANNELS[i]]));
 const W=rows[0].length+2,H=rows.length+2,blank='.'.repeat(W),padded=[blank,...rows.map(r=>'.'+r.map(c=>map[c]).join('')+'.'),blank];
 const pods=[],tray=Object.values(L20.pods).map(col=>col.map(p=>{const [k,link]=p.split(':');
  pods.push({color:map[k[0]],count:parseInt(k.slice(1)),label:p,...(link?{group:link}:{}),...(k.endsWith('?')?{mystery:'depth'}:{})});return pods.length-1;}));
 const level={id:20,title:'Colony Flow L20',subject:'cf-L20',w:W,h:H,rowsBottomFirst:padded.slice().reverse(),entries:[Math.floor(W/2)],capacity:L20.capacity,pods,tray,difficulty:'Super Hard'};
 // validate only knows 9 letters, 36 cells and v8D's pairs (same depth, two columns); everything else must hold.
 const errors=R.validate(level).filter(e=>!['two equal-count mystery colours required','at most three depth mysteries','grid','dimensions/id','pair in same column','pair at different initial depths'].includes(e));assert.deepEqual(errors,[],'L20');
 return {level,fixes:[]};
}
function build(name){
 const g=G[name],t=T[name],rows=g.rows.map(r=>r.split(''));
 // The same single-cell recognition fixes as sim/run.mjs, so that the board's cells equal the capsules' work.
 const need={},have={};for(const col of Object.values(t.pods))for(const p of col)need[p[0]]=(need[p[0]]||0)+ +p.slice(1);
 rows.flat().forEach(c=>{if(c!=='.')have[c]=(have[c]||0)+1;});
 const over=Object.keys(have).filter(c=>have[c]>(need[c]||0)),under=Object.keys(need).filter(c=>need[c]>(have[c]||0)),fixes=[];
 for(const u of under)for(const o of over)while(have[o]>need[o]&&need[u]>have[u]){let done=false;
  for(let y=0;y<rows.length&&!done;y++)for(let x=0;x<rows[0].length&&!done;x++){if(rows[y][x]!==o)continue;
   if([[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy])=>rows[y+dy]?.[x+dx]===u)){rows[y][x]=u;have[o]--;have[u]++;fixes.push(`${o}->${u}@${x},${y}`);done=true;}}
  if(!done)break;}
 const syms=Object.keys(need),map=Object.fromEntries(syms.map((s,i)=>[s,R.CHANNELS[i]]));
 // (validate's grid check only knows the 9 v8D letters, so it is skipped.)
 // A ring of open cells: CF's ants run round the frame and reach the picture from every side.
 const W=rows[0].length+2,H=rows.length+2,blank='.'.repeat(W);
 const padded=[blank,...rows.map(r=>'.'+r.map(c=>c==='.'?'.':map[c]).join('')+'.'),blank];
 const pods=[],tray=Object.values(t.pods).map((col,c)=>col.map((p,k)=>{const hidden=(HIDDEN[name]||[]).some(([hc,hk])=>hc===c&&hk===k);
  pods.push({color:map[p[0]],count:+p.slice(1),label:p,...(hidden?{mystery:'depth'}:{})});return pods.length-1;}));
 const level={id:+name.slice(1),title:'Colony Flow '+name,subject:'cf-'+name,w:W,h:H,rowsBottomFirst:padded.slice().reverse(),entries:[Math.floor(W/2)],capacity:t.capacity,pods,tray,difficulty:LABEL[name]};
 const errors=R.validate(level).filter(e=>!['two equal-count mystery colours required','at most three depth mysteries','grid'].includes(e));assert.deepEqual(errors,[],name);
 return {level,fixes};
}
const out={source:'owner recordings (Pixel 9, Colony Flow v1.0.79, 2026-10-20 and L20 on 2026-10-23)',engine:'tools/gameplay-pilot-v8d; colour alphabet widened and CF stacked links added, bots unchanged',levels:{}};
// The owner's winning order on L20 (columns 1-4, from the recording), replayed patiently in our rules.
const OWNER_L20=L20.ownerColumns,idle=(await import(path.join(tmp,'idle-rules.cjs'))).default;
function ownerReplay(level){let st=idle.create(level),n=0;for(const c of OWNER_L20){const pod=level.tray[c-1][st.heads[c-1]],first=st.meta.groups[pod][0];const r=idle.step(st,{type:'tap',pod:first});if(!r.accepted)return {won:false,refusedAt:n+1,reason:r.reason};st=r.state;n++;}return {won:st.status==='Won',status:st.status,taps:n};}
for(const name of [...Object.keys(T),'L20']){const {level,fixes}=name==='L20'?buildL20():build(name);
 const t0=Date.now(),rep=bots.report(level,300),patterns=bots.patternWins(level);
 let planner=null;for(const opts of PLANNERS){const r=playPlanner(level,opts);if(r.won){planner={...opts,commands:r.actions.length};break;}}
 const plannerOpts=planner||PLANNERS[0],top=playPlanner(level,{...plannerOpts,rows:1}).won,full=playPlanner(level,{...plannerOpts,rows:99}).won;
 const plain={...level,pods:level.pods.map(({mystery,...p})=>p)},fullIfKnown=level.pods.some(p=>p.mystery)?PLANNERS.some(o=>playPlanner(plain,o).won):null;
 const ef=bots.earlyFatal(level,30),owner=name==='L20'?ownerReplay(level):null;
 out.levels[name]={label:LABEL[name],size:`${level.w-2}x${level.h-2}`,cells:level.pods.reduce((n,p)=>n+p.count,0),colours:new Set(level.pods.map(p=>p.color)).size,pods:level.pods.length,
  podMedian:level.pods.map(p=>p.count).sort((a,b)=>a-b)[Math.floor(level.pods.length/2)],mysteries:level.pods.filter(p=>p.mystery).length,rows:Math.max(...level.tray.map(s=>s.length)),fixes,
  bots:{...rep,patternWins:patterns},planner:{wins:Boolean(planner),settings:planner,winsSeeingTopRowOnly:top,winsSeeingWholeTray:full,winsIfMysteriesWereKnown:fullIfKnown},earlyFatal:ef,...(owner?{ownerOrder:owner}:{}),seconds:(Date.now()-t0)/1000};
 console.log(name,LABEL[name],JSON.stringify({random:rep.random,visible:rep.visible,patterns:patterns.length,sensible:rep.sensibleWins,planner:planner?.depth??'loses',top,full,ef}));
}
// The same measures on our v8D levels: the first five, the Hard and the Super Hard ones.
const ours=JSON.parse(fs.readFileSync(path.join(root,'content/pilot-v8d/levels.json'))).levels;out.v8d={};
for(const id of [1,2,3,4,5,10,15,22,25,29]){const level=ours[id-1],rep=bots.report(level,300),patterns=bots.patternWins(level);
 let planner=null;for(const opts of PLANNERS){const r=playPlanner(level,opts);if(r.won){planner={...opts,commands:r.actions.length};break;}}
 const top=playPlanner(level,{...(planner||PLANNERS[0]),rows:1}).won;
 out.v8d['L'+id]={label:level.difficulty,size:`${level.w}x${level.h}`,cells:level.pods.reduce((n,p)=>n+p.count,0),colours:new Set(level.pods.map(p=>p.color)).size,pods:level.pods.length,
  podMedian:level.pods.map(p=>p.count).sort((a,b)=>a-b)[Math.floor(level.pods.length/2)],mysteries:level.pods.filter(p=>p.mystery).length,rows:Math.max(...level.tray.map(s=>s.length)),
  bots:{...rep,patternWins:patterns},planner:{wins:Boolean(planner),settings:planner,winsSeeingTopRowOnly:top},earlyFatal:bots.earlyFatal(level,30)};
 console.log('v8D L'+id,JSON.stringify({random:rep.random,visible:rep.visible,patterns:patterns.length,top,ef:out.v8d['L'+id].earlyFatal}));}
const file=path.join(root,'product/audits/colony-flow-vs-v8d-bots-2026-10-23.json');fs.writeFileSync(file,JSON.stringify(out,null,2)+'\n');console.log('wrote',path.relative(root,file));
fs.rmSync(tmp,{recursive:true,force:true});
