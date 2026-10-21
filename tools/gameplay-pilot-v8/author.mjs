import R from './rules.cjs';
const MYSTERY=new Set([6,7,9,12,14,17,19,25,26,30]),HARD=new Set([10,15,22,25,29]),RELIEF=new Set([11,16,23,28,30]);
const pick=(a,r)=>a[Math.floor(r()*a.length)];
const shuffle=(a,r)=>{a=a.slice();for(let i=a.length-1;i>0;i--){const j=Math.floor(r()*(i+1));[a[i],a[j]]=[a[j],a[i]];}return a;};
function groupCount(id){return ({8:1,9:2,10:3,12:2,13:1,14:2,17:1,18:2,19:2,21:1,24:2,25:3,27:2,29:3,30:1})[id]||0;}
function podCount(id){const changed={4:22,5:22,6:16,7:22,8:18,9:20,13:22,15:22,16:20,18:25,19:26,23:22,25:24,26:24,28:22};if(changed[id])return changed[id];return [9,10,12,15,16,14,16,14,16,18,15,18,17,19,20,16,18,20,21,14,18,22,16,22,22,22,22,18,24,22][id-1];}

export function definition(art,r){
 const id=art.id,n=podCount(id),counts=Object.fromEntries(R.CHANNELS.split('').map(c=>[c,0]));
 for(const c of art.rowsBottomFirst.join(''))if(c!=='#'&&c!=='.')counts[c]++;
 const colours=R.CHANNELS.split('').filter(c=>counts[c]>0),alloc=Object.fromEntries(colours.map(c=>[c,1])),minimum=4;
 while(Object.values(alloc).reduce((a,b)=>a+b,0)<n){
  const options=colours.filter(c=>counts[c]>=minimum*(alloc[c]+1));if(!options.length)throw new Error('Not enough work for capsule budget');
  const weights=options.map(c=>counts[c]/(alloc[c]+.4)**1.5),total=weights.reduce((a,b)=>a+b,0);let point=r()*total,chosen=options.at(-1);
  for(let i=0;i<options.length;i++){point-=weights[i];if(point<=0){chosen=options[i];break;}}alloc[chosen]++;
 }
 let mysteryColours=[],mysteryCount=0;
 if(MYSTERY.has(id)){
  mysteryCount=(id===6?18:id<10?24:id===25?32:28)+Math.floor(r()*(id===6?7:15));
  const options=colours.filter(c=>alloc[c]>=2&&counts[c]>=mysteryCount+minimum*(alloc[c]-1));
  if(options.length<2)return null;
  mysteryColours=shuffle(options,r).slice(0,2);
 }
 const pods=[];
 for(const color of colours){
  let total=counts[color],number=alloc[color];
  if(mysteryColours.includes(color)){pods.push({color,count:mysteryCount,mystery:'sealed'});total-=mysteryCount;number--;}
  const floor=number?Math.min(minimum,Math.floor(total/number)):0;
  const weights=Array.from({length:number},()=>id<5?.7+r()*.6:(HARD.has(id)||[5,7].includes(id))?.18+r()**2*4:.35+r()*1.8),sum=weights.reduce((a,b)=>a+b,0);let remaining=total;
  for(let i=0;i<number;i++){const count=i===number-1?remaining:floor+Math.floor((total-floor*number)*weights[i]/sum);pods.push({color,count});remaining-=count;}
 }
 if(pods.length!==n||pods.some(p=>p.count<1))return null;
 const candidates=shuffle(pods.map((p,i)=>(!p.mystery||id===25)?i:-1).filter(i=>i>=0),r),groups=[];
 for(let g=0;g<groupCount(id);g++){
  if(candidates.length<2)return null;const same=(id===8||id===18)&&g===0;const aIndex=id===25&&g===0?candidates.findIndex(i=>pods[i].mystery):same?candidates.findIndex(a=>candidates.some(b=>a!==b&&pods[a].color===pods[b].color)):candidates.length-1;if(aIndex<0)return null;const [a]=candidates.splice(aIndex,1),bIndex=candidates.findIndex(p=>(id!==25||!pods[p].mystery)&&(same?pods[p].color===pods[a].color:pods[p].color!==pods[a].color));if(bIndex<0)return null;
  const [b]=candidates.splice(bIndex,1),name=String.fromCharCode(65+g);pods[a].group=pods[b].group=name;groups.push([a,b].sort((x,y)=>x-y));
 }
 const singles=shuffle(pods.map((p,i)=>p.group?null:i).filter(i=>i!==null),r),pendingGroups=shuffle(groups,r);

 const columns=3,tray=Array.from({length:columns},()=>[]);
 // Fill complete rows. Native Core requires both pair members at the same
 // original depth; after play begins their heads can advance independently.
 while(singles.length||pendingGroups.length){
  const target=Math.min(columns,singles.length+pendingGroups.length*2),patterns=[];
  for(let g=0;g<=Math.min(pendingGroups.length,Math.floor(target/2));g++)if(target-g*2<=singles.length)patterns.push(g);
  if(!patterns.length)return null;const g=pick(patterns,r),row=new Array(target).fill(-1);
  if(g===2){const a=pendingGroups.pop(),b=pendingGroups.pop();row.splice(0,4,...a,...b);}
  else if(g===1){const at=Math.floor(r()*(target-1)),pair=pendingGroups.pop();row[at]=pair[0];row[at+1]=pair[1];}
  for(let c=0;c<target;c++){if(row[c]<0)row[c]=singles.pop();tray[c].push(row[c]);}
 }
 for(const stack of tray)if(pods[stack[0]]?.mystery){const at=stack.findIndex((p,i)=>i>0&&!pods[p].group&&!pods[p].mystery);if(at<0)return null;[stack[0],stack[at]]=[stack[at],stack[0]];}
 return {...art,capacity:5,pods,tray,difficulty:HARD.has(id)?'Hard':'Normal'};
}
