// Merges tools/level-stats/out/levels/*.json into one data file for the report and one table (CSV):
// - per level: the main measures, each marked with where it comes from (seen in a recording, the exact replay of a
//   recording, our engine's model, a strategy run);
// - per game (Colony Flow, v8E, v9) and difficulty: distributions for the comparison section;
// - similarity: nearest levels by logic (tray, links, hidden, risk) and by rhythm (buffer by stage), apart from the
//   picture (its title only);
// - completeness: what each level has and what is missing.
// node tools/level-stats/assemble.mjs → tools/level-stats/out/data.json, product/audits/level-stats/levels.csv
import fs from 'node:fs';import path from 'node:path';import {root} from './levels.mjs';
const dir=path.join(root,'tools/level-stats/out/levels'),files=fs.readdirSync(dir).filter(f=>f.endsWith('.json'));
const L=files.map(f=>JSON.parse(fs.readFileSync(path.join(dir,f),'utf8')));
const GAME_ORDER={CF:0,v8E:1,v9:2};L.sort((a,b)=>GAME_ORDER[a.game]-GAME_ORDER[b.game]||a.level-b.level);
const r2=x=>x==null||!Number.isFinite(x)?null:Math.round(x*100)/100;
const median=a=>{a=a.filter(x=>x!=null&&Number.isFinite(x)).sort((x,y)=>x-y);if(!a.length)return null;const m=a.length>>1;return a.length%2?a[m]:(a[m-1]+a[m])/2;};
const mean=a=>{a=a.filter(x=>x!=null&&Number.isFinite(x));return a.length?a.reduce((x,y)=>x+y,0)/a.length:null;};
// the attempt measures to quote for a level: the owner's won attempt seen (video) or replayed exactly (v8E); else the
// model's winning route played live; the source is kept with the number
function reference(l){
 const won=l.attempts.find(a=>a.outcome==='won');
 if(l.game==='v8E'&&won)return {source:'replay of the owner\'s won attempt (exact)',m:won.replay};
 if(l.game==='CF'&&won?.video)return {source:'video of the won attempt',m:won.video};
 if(l.game==='CF'&&won?.replay)return {source:'model replay of the won attempt (approximate)',m:won.replay};
 if(l.checks?.routeLive)return {source:'model: winning route played live',m:l.checks.routeLive};
 return {source:null,m:null};}
const STRATS=['left-to-right','right-to-left','top-down','bottom-up','diagonal','random','small-first','big-first','most-now','fast-release','planner','plannerPatient'];
const rows=L.map(l=>{const st=l.static,b=st.board,t=st.tray,ref=reference(l),m=ref.m,ch=l.checks;
 const attempts=l.attempts,firstWin=attempts.findIndex(a=>a.outcome==='won');
 const seconds=a=>a.realSeconds??(a.end!=null&&a.start!=null?a.end-a.start:null);
 return {key:l.key,game:l.game,set:l.set,level:l.level,difficulty:l.difficulty,title:l.title,
  cells:b?.cells??null,types:b?.types??null,mysteryTiles:b?.mysteryTiles??null,w:b?.w??null,h:b?.h??null,fill:b?.fill??null,patches:b?.patches??null,islands:b?.islands??null,largestPatch:b?.largestPatch??null,
  capsules:t.capsules,capsuleCells:t.cells,columns:t.columns,countMin:t.counts.min,countMean:t.counts.mean,countMedian:t.counts.median,countMax:t.counts.max,countSd:t.sd,countValues:t.values,countStep:t.step,smallVolumeShare:t.smallVolumeShare,
  linkSets:t.links.sets,linkCapsules:t.links.capsules,linkMaxSize:t.links.sizes.length?Math.max(...t.links.sizes):0,linkMaxLength:t.links.lengths.max??0,hidden:t.hidden.capsules,padlocks:t.padlocks,boxes:l.boxes||null,
  accountingOff:st.accounting?.off??null,model:l.model?(l.game==='CF'?'approximate':'exact'):'none',modelNote:l.modelNote,
  winnable:ch?.winnable??null,minCapacity:ch?.minCapacity?.min??null,minCapacityProven:ch?.minCapacity?.provenBelow??null,routeTaps:ch?.route?.taps??null,decisions:ch?.route?.decisions??null,risky:ch?.route?.risky??null,tight:ch?.route?.tight??null,
  sequencesLog10:ch?.sequences?.log10??null,routeFastWins:ch?.routeFast?(ch.routeFast.status==='won'?1:0):null,startTops:ch?.start?.tops??t.startTops,startUseful:ch?.start?.useful??null,
  ...Object.fromEntries(STRATS.map(k=>['s_'+k,l.strategies?.[k]?.rate??null])),
  attempts:attempts.length,outcomes:attempts.map(a=>a.outcome[0]).join(''),firstWinAttempt:firstWin>=0?firstWin+1:null,
  wonSeconds:firstWin>=0?r2(seconds(attempts[firstWin])):null,secondsToFirstWin:firstWin>=0?r2(attempts.slice(0,firstWin+1).reduce((n,a)=>n+(seconds(a)||0),0)):null,
  // o_: the owner's/player's won attempt (video for Colony Flow when measured, else the model replay of its taps; the
  // exact replay for v8E); none for v9. m_: the same model player on every level that runs on our engine (the
  // winning route found by search, each tap as soon as it is possible), so the three games compare on one basis.
  o_source:ref.source&&!/route/.test(ref.source)?ref.source:null,...obs(ref.source&&!/route/.test(ref.source)?m:null,'o_'),...obs(ch?.routeLive||null,'m_')};});
function obs(m,p){return {[p+'seconds']:m?.end??null,[p+'meanFree']:m?.buffer?.meanFree??null,[p+'minFree']:m?.buffer?.minFree??null,[p+'zeroFreeShare']:m?.buffer?.zeroFreeShare??null,[p+'oneFreeShare']:m?.buffer?.oneFreeShare??null,
 [p+'fullEpisodes']:m?.buffer?.fullEpisodes??null,[p+'tapGapMedian']:m?.time?.tapGaps?.median??null,[p+'stops']:m?.capsules?.stops??null,[p+'cascades']:m?.releases?.cascades??null,[p+'tail90']:m?.time?.tail90??null};}
// similarity: z-scored features, nearest three; logic and rhythm apart
const feats={logic:r=>[Math.log(r.cells||1),r.types,r.capsules,r.columns,r.countMean,r.countSd,r.linkSets,r.linkCapsules,r.hidden,r.risky,r.tight],
 rhythm:(r,l)=>{const ref=l.checks?.routeLive||reference(l).m;const seg=ref?.segmentsByTime||[];return [...[0,1,2,3,4].map(i=>seg[i]?.meanFree??null),...[0,1,2,3,4].map(i=>seg[i]?.zeroFreeShare??null),ref?.time?.tapGaps?.median??null];}};
const similar={};for(const [name,f] of Object.entries(feats)){const vecs=rows.map((r,i)=>f(r,L[i])),k=vecs[0].length;
 const mu=[...Array(k).keys()].map(j=>mean(vecs.map(v=>v[j]))),sd=[...Array(k).keys()].map(j=>Math.sqrt(mean(vecs.map(v=>v[j]==null?null:(v[j]-mu[j])**2))||1)||1);
 const z=vecs.map(v=>v.map((x,j)=>x==null?null:(x-mu[j])/sd[j]));
 for(let i=0;i<rows.length;i++){const d=[];for(let j=0;j<rows.length;j++){if(i===j)continue;let s=0,n=0;for(let q=0;q<k;q++)if(z[i][q]!=null&&z[j][q]!=null){s+=(z[i][q]-z[j][q])**2;n++;}if(n>=k*.6)d.push([rows[j].key,r2(Math.sqrt(s/n))]);}
  d.sort((a,b)=>a[1]-b[1]);(similar[rows[i].key]=similar[rows[i].key]||{})[name]=d.slice(0,3);}}
// comparison per game and difficulty: lists of values for the charts and medians for the table
const metrics=['cells','types','fill','patches','islands','capsules','columns','countMean','countMedian','countMax','countSd','smallVolumeShare','linkSets','linkCapsules','linkMaxLength','hidden','padlocks','minCapacity','risky','tight','decisions','startUseful','routeFastWins',
 ...['o_','m_'].flatMap(p=>['seconds','meanFree','zeroFreeShare','oneFreeShare','fullEpisodes','tapGapMedian','stops','cascades','tail90'].map(k=>p+k)),'wonSeconds','secondsToFirstWin','firstWinAttempt',...STRATS.map(k=>'s_'+k)];
const groupsOf=r=>[r.game,`${r.game}|${r.difficulty}`];const comparison={};
for(const r of rows)for(const g of groupsOf(r)){const c=comparison[g]=comparison[g]||{levels:0,values:{}};c.levels++;for(const m of metrics)(c.values[m]=c.values[m]||[]).push(r[m]);}
for(const c of Object.values(comparison)){c.median={};c.mean={};c.n={};for(const m of metrics){const v=c.values[m].filter(x=>x!=null);c.median[m]=r2(median(v));c.mean[m]=r2(mean(v));c.n[m]=v.length;}}
// mechanics by level: first appearance in each game
const firsts={};for(const r of rows){const f=firsts[r.game]=firsts[r.game]||{};for(const [k,on] of [['links',r.linkSets>0],['hidden',r.hidden>0],['padlocks',r.padlocks>0],['boxes',!!r.boxes],['boardMystery',r.mysteryTiles>0]])if(on&&f[k]==null)f[k]=r.level;}
// observed against model for Colony Flow: the video's free places against the model replay of the same taps
const crossCheck=L.filter(l=>l.game==='CF').flatMap(l=>l.attempts.filter(a=>a.video&&a.replay).map(a=>({key:l.key,attempt:a.attempt,outcome:a.outcome,videoMeanFree:a.video.buffer?.meanFree??null,modelMeanFree:a.replay.buffer?.meanFree??null,videoZero:a.video.buffer?.zeroFreeShare??null,modelZero:a.replay.buffer?.zeroFreeShare??null,videoEnd:a.video.end,modelEnd:a.replay.end,modelOutcome:a.replay.status})));
const completeness=L.map(l=>({key:l.key,board:l.board?(l.game==='CF'?(l.board.fromVideo?'video frame':l.set==='owner'?'start frame':'site cover'):'level definition'):'missing',tray:l.game==='CF'?'transcribed from video':'level definition',
 attempts:l.attempts.length,video:l.attempts.filter(a=>a.video).length,replay:l.attempts.filter(a=>a.replay).length,model:l.model?(l.game==='CF'?'approximate':'exact'):'none',modelNote:l.modelNote,unknown:[...(l.uncertain||[])]}));
const data={built:'tools/level-stats/assemble.mjs',date:new Date().toISOString().slice(0,10),rows,levels:L,similar,comparison,firsts,crossCheck,completeness,
 notes:{times:'Seconds. Model times are at ×1 game speed; Colony Flow video times are video seconds (the game may run at ×2 where noted); v8E real times are the owner\'s wall clock (×2 after the auto-fast or with fast on).',
  buffer:'Free places, weighted by how long each state lasted. Full = 0 free, nearly full = exactly 1 free (never added together).',
  segments:'Five stretches by the share of the board cleared (all three games win at a fully cleared board) and, separately, by the share of the attempt\'s time.',
  strategies:'Tapping as soon as possible on our engine (Colony Flow: approximate physics). Random: 100 runs; the others are deterministic. A strategy\'s loss does not prove a level hard for people.'}};
fs.mkdirSync(path.join(root,'tools/level-stats/out'),{recursive:true});fs.writeFileSync(path.join(root,'tools/level-stats/out/data.json'),JSON.stringify(data));
const cols=Object.keys(rows[0]),esc=v=>v==null?'':/[",\n;]/.test(String(v))?'"'+String(v).replace(/"/g,'""')+'"':String(v);
fs.mkdirSync(path.join(root,'product/audits/level-stats'),{recursive:true});
fs.writeFileSync(path.join(root,'product/audits/level-stats/levels.csv'),[cols.join(','),...rows.map(r=>cols.map(c=>esc(r[c])).join(','))].join('\n')+'\n');
console.log(rows.length,'levels;',Object.keys(comparison).length,'groups;',crossCheck.length,'cross-checks');
