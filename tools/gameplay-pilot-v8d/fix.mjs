// v8D = v8C with the owner's proposals 1-5 of 2026-10-22 (product/playtests/pilot-v8c-owner-2026-10-22).
// The levels keep v8C's boards, capsules and order, so the bots' role gates do not move (bots only ever look at the
// top). What changes: the tray shows 3 rows and the 4th row's edge, and every level must be won by the planner that
// sees only that (horizon.mjs); "?" capsules are rarer but important: 1-3 on L6, L12, L19, L25 and L30, below the
// 3 full rows (they first show as a grey "?"), at least one of a key colour, revealing in the middle of the level.
import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;
import {report,patternWins} from './botsc.mjs';import {playPlanner,ROWS} from './horizon.mjs';
import {MYSTERY,PLANNERS,REVEAL_WINDOW,REVEAL_OUTER,planner,withMysteries,strip} from './fix-lib.mjs';
const v8c=JSON.parse(fs.readFileSync('content/pilot-v8c/levels.json')),v8cq=JSON.parse(fs.readFileSync('content/pilot-v8c/quality.json'));
const out='content/pilot-v8d';fs.mkdirSync(out,{recursive:true});
const levels=[],certs=[],quality=[];
for(const original of v8c.levels){const id=original.id,role=v8cq.roleOf[id]||'tutorial',plain=strip(original);let level=plain,won,mystery=null;
 if(MYSTERY[id]){const m=withMysteries(plain,MYSTERY[id]);level=m.level;won=m.planner;mystery={hidden:m.hidden,keys:m.keys,reveals:m.reveals};}
 else{won=planner(level);if(!won)throw Error('The planner that sees '+ROWS+' rows loses L'+id);}
 // Comparison: the same planner seeing only the top row, and seeing the whole tray.
 const top=playPlanner(level,{...won.opts,rows:1}).won,full=playPlanner(level,{...won.opts,rows:99}).won;
 const bots=role==='tutorial'?null:{...report(level,300),patternWins:patternWins(level)};
 levels.push(level);
 certs.push({id,actions:won.actions,schedule:'wait until work idle between commands',planner:won.opts,fairness:{scope:`visible-horizon planner: plans only with the ${ROWS} visible rows (the 4th is the edge), never a hidden colour or count; its route is the certificate`}});
 quality.push({id,role,source:JSON.stringify(level)===JSON.stringify(original)?'v8C':'v8C + v8D mysteries',changed:JSON.stringify(level)!==JSON.stringify(original),planner:{...won.opts,commands:won.actions.length,winsSeeingTopRowOnly:top,winsSeeingWholeTray:full},mystery,bots});
 console.log('L'+id,role,'planner depth',won.opts.depth,'top-only',top,'full',full,mystery?'? '+mystery.hidden.join(',')+' keys '+mystery.keys.join(',')+' at '+mystery.reveals.map(e=>e.clearedFraction).join('/'):'',bots?`random ${(bots.random*100).toFixed(0)}% patterns ${bots.patternWins.length}/9`:'');
}
const hash=crypto.createHash('sha256').update(JSON.stringify(levels)).digest('hex');
fs.writeFileSync(out+'/levels.json',JSON.stringify({version:'pilot-30-v8d',hash,levels},null,2)+'\n');fs.writeFileSync(out+'/certificates.json',JSON.stringify(certs,null,2)+'\n');
fs.writeFileSync(out+'/quality.json',JSON.stringify({version:'pilot-30-v8d',catalogHash:hash,sourceHash:v8c.hash,complete:levels.length===30,changedLevels:quality.filter(q=>q.changed).map(q=>q.id),controls:v8cq.controls,roles:v8cq.roles,roleOf:v8cq.roleOf,patternShare:v8cq.patternShare,hardPatternLevels:v8cq.hardPatternLevels,visibleRows:ROWS,mysteryLevels:MYSTERY,revealWindow:{key:REVEAL_WINDOW,any:REVEAL_OUTER},planners:PLANNERS,tail:T.TAIL,
 limits:['Bot rates are construction diagnostics, not human win rates.','The planner is one honest strategy (it never looks deeper than the player), not the best possible one; its win proves a level can be won from what is visible.','Patient player: every command settles before the next.','Controls L10, L22, L29 keep their v8C capsules.'],levels:quality},null,2)+'\n');
console.log('done',hash);
