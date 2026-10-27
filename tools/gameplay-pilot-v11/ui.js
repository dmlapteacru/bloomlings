/* Pilot v11 (round levels of product/17_LEVEL_GENERATION_RULES.md); grew from the v8E/v9 pilot UI. 16 colours (4 per hero); a fence with gates (tunnels) and a path round the picture, drawn as solid strips: the
 * ants come in at a gate and walk the path to any side; the auto-tail plays at ×2.5. The tray shows 3 rows (no 4th-row edge since the v9 playtest); capsules are plain colour and number (symbols only in the
 * accessible palette); the auto-tail plays at ×2.5 (×4 in v8D). V8: logical outcomes + a physical, resumable work timeline. */
(() => {
 'use strict';
 const $=id=>document.getElementById(id),R=window.BloomPilotRules,T=window.BloomPilotTimeline;
 const data=JSON.parse($('pilotData').textContent),assets=JSON.parse($('pilotAssets').textContent);
 // v11 display tone (the owner, 2026-10-27: "цвета подкорректировать, чтоб картинки эффектнее смотрелись"): the normal palette
 // shown 20 % more saturated, hue and lightness kept, the same on the board, the capsules and the places; materials unchanged
 const toned=pal=>{const out={};for(const [k,v] of Object.entries(pal)){if(!v.color||k==='?'){out[k]=v;continue;}
   const n=parseInt(v.color.slice(1),16),[r,g,b]=[n>>16,(n>>8)&255,n&255].map(x=>x/255),mx=Math.max(r,g,b),mn=Math.min(r,g,b),l=(mx+mn)/2,d=mx-mn;
   let h=0,sat=d?d/(1-Math.abs(2*l-1)):0;if(d){h=mx===r?((g-b)/d)%6:mx===g?(b-r)/d+2:(r-g)/d+4;h*=60;if(h<0)h+=360;}
   sat=Math.min(1,sat*1.2);const c=(1-Math.abs(2*l-1))*sat,xx=c*(1-Math.abs((h/60)%2-1)),m=l-c/2,[r1,g1,b1]=h<60?[c,xx,0]:h<120?[xx,c,0]:h<180?[0,c,xx]:h<240?[0,xx,c]:h<300?[xx,0,c]:[c,0,xx];
   const hex='#'+[r1,g1,b1].map(t=>Math.round((t+m)*255).toString(16).padStart(2,'0')).join(''),mix=(f)=>'#'+[r1,g1,b1].map(t=>{const q=(t+m)*255,to=f>0?255:0;return Math.round(q+(to-q)*Math.abs(f)).toString(16).padStart(2,'0');}).join('');
   out[k]={...v,color:hex,dark:mix(-.28),light:mix(.36)};}return out;};
 const paletteOf=mode=>mode==='normal'?toned(window.BloomPilotPalette.palette(mode)):window.BloomPilotPalette.palette(mode);
 let palette=paletteOf('normal');
 const LESSONS=data.lessons; // built from the levels by build.mjs: L1, new colours, first introductions, first Hard and Super Hard

 const KEY=`bloomlings-pilot-v11:${data.hash}:${data.rulesHash}:${data.timelineHash}:${data.paletteHash}:${data.clockHash}`;
 const fresh=()=>({schema:9,catalogHash:data.hash,rulesHash:data.rulesHash,timelineHash:data.timelineHash,current:1,started:false,lessons:[],settings:{fast:false,symbols:false,palette:'normal'},attempts:[],activeByLevel:{},presentations:{},serial:0});
 let store=fresh(),storageProblem='',revealNotice='',revealUntil=0;
 try{const saved=localStorage.getItem(KEY);if(saved){const s=JSON.parse(saved);if(s.schema!==9||!s.presentations||!Array.isArray(s.attempts)||!['normal','accessible'].includes(s.settings?.palette))throw Error('Invalid save');store=s;}}catch(error){storageProblem='Сохранение недоступно. Скачай результаты перед закрытием.';}
 let state,visual,timeline,attempt,lastCheckpoint=performance.now(),drawing=null,needsRender=false;const activity=new window.BloomPilotClock.ActiveClock(performance.now());
 let topCards=[],backCards=[],columns=[],slotCards=[],bridgeCards=[],entryMarkers=[];
 const VISIBLE_ROWS=3; // v9 (the owner, 2026-10-24): 3 rows, the top one included, and nothing of the 4th
 const reduced=matchMedia('(prefers-reduced-motion: reduce)'),dialogs=Array.from(document.querySelectorAll('dialog'));
 const pictures={};for(const name of ['sprig','bloom','drop','twig']){const img=new Image();img.src=assets[name];img.onload=()=>draw();pictures[name]=img;}
 document.documentElement.style.setProperty('--garden',`url("${assets.garden}")`);
 palette=paletteOf(store.settings.palette);
 function save(){if(timeline&&attempt)store.presentations[attempt.id]=timeline.snapshot();try{localStorage.setItem(KEY,JSON.stringify(store));}catch(error){storageProblem='Сохранение недоступно. Скачай результаты перед закрытием.';}}
 function active(){return store.started&&!document.hidden&&!dialogs.some(d=>d.open);}
 function recordWorkEvents(){
  attempt.assignments.push(...timeline.lastAssignments);timeline.lastAssignments=[];
  const codes={depart:0,arrive:1,clear:2,return:3,complete:4,assign:5},released=timeline.lastEvents.filter(e=>e.type==='complete').map(e=>e.pod);
  for(const e of timeline.lastEvents){attempt.workEvents.push([Math.round(e.at*100)/100,codes[e.type],e.id??e.pod]);if(e.type==='depart')attempt.tripPlans.push(e.plan);}
  timeline.lastEvents=[];if(released.length)event('places-released',{pods:released});
 }
 function addTime(dt,bucket,working){
  if(dt<=0)return;attempt.elapsedMs+=dt;attempt.timeBuckets[bucket]+=dt;
  const last=attempt.intervals.at(-1);if(last?.kind===bucket)last.toMs=attempt.elapsedMs;else attempt.intervals.push({kind:bucket,fromMs:attempt.elapsedMs-dt,toMs:attempt.elapsedMs});
  if(working)attempt.workActiveMs+=dt;
 }
 function consumeTime(dt){
  let changed=false;
  while(dt>1e-7&&attempt?.outcome==='playing'&&(timeline.pending||state.status==='Playing')){
   const pending=timeline.pending,bucket=R.queueEmpty(state)&&pending?'autoTail':timeline.hasChoice?'choice':pending?'blockedWorking':'stalled',rate=speed();
   if(!pending){addTime(dt,bucket,false);break;}
   const next=Math.max(0,(timeline.steps[timeline.cursor].at-timeline.now)/rate),slice=Math.min(dt,next);
   addTime(slice,bucket,true);changed=timeline.advance(slice,rate)||changed;recordWorkEvents();dt-=slice;
   // advance(0) consumes equal-time events; every pass removes an event or time.
  }
  state=timeline?.state||state;visual=timeline?.shown||visual;return changed;
 }
 function syncTime(){const dt=activity.sample(performance.now()),changed=timeline&&attempt?consumeTime(dt):false;needsRender=needsRender||changed;return changed;}
 function setActivity(reason,force=null){
  const running=(force===null?active():force)&&attempt?.outcome==='playing'&&(timeline?.pending||state?.status==='Playing');
  if(activity.running!==Boolean(running)){const dt=activity.setRunning(Boolean(running),performance.now());if(timeline&&attempt)needsRender=consumeTime(dt)||needsRender;if(attempt&&timeline)event(running?'resume':'pause',{reason,effectiveSpeed:speed()});}
 }
 function modal(id){syncTime();$(id).showModal();setActivity('dialog:'+id);}
 function close(id){syncTime();$(id).close();setActivity('dialog:'+id);}
 function closeAll(){syncTime();dialogs.forEach(d=>{if(d.open)d.close();});setActivity('dialogs-closed');}
 function colour(element,code){const p=palette[code||'?'];element.style.setProperty('--color',p.color);element.style.setProperty('--text',p.text);element.style.setProperty('--text-shadow','none');}
 // v8D: symbols and hero portraits only in the accessible palette; the auto-tail runs at T.TAIL.speed.
 const symbols=()=>store.settings.palette==='accessible',speed=()=>timeline?.tail?T.TAIL.speed:store.settings.fast?2:1,fast=()=>speed()>1;
 function configuration(level){return 5;}
 function startAttempt(level,capacity){
  const a={id:++store.serial,level:level.id,capacity,config:{capacity,trips:T.PROFILE,visibility:'tray: top 3 rows, no edge row (v9 playtest); depth capsules reveal on top',tail:T.TAIL,initialSettings:{manualFast:store.settings.fast,effectiveSpeed:store.settings.fast?2:1,palette:store.settings.palette,symbols:symbols()}},startedAt:new Date().toISOString(),commands:[],inputEvents:[],telemetry:[],elapsedMs:0,outcome:'playing',logicalOutcome:'playing',timeBuckets:{choice:0,blockedWorking:0,autoTail:0,stalled:0},workActiveMs:0,assignments:[],tripPlans:[],workEvents:[],reveals:[],intervals:[],rating:null,interest:null,art:null,planning:null,depthNeed:null,pace:null,waiting:null,uncertainty:null,note:'',autoFastAtAction:null,autoFastAtMs:null,workFinishedAtMs:null};
  store.attempts.push(a);store.activeByLevel[`${level.id}:${capacity}`]=a.id;return a;
 }
 function measure(){return {logicalHeld:state.slots.filter(p=>p>=0).length,visualHeld:timeline.shown.slots.filter(p=>p>=0).length,visualSlots:Array.from(timeline.shown.slots),visualCounts:Array.from(timeline.shown.slots,p=>p<0?null:timeline.shown.remaining[p]),modelTimeMs:timeline.now,hasChoice:timeline.hasChoice,pendingTrips:timeline.jobs.size,pendingCells:timeline.pendingCells,pendingBeats:timeline.steps.length-timeline.cursor,sourceLeft:state.level.tray.reduce((n,s,c)=>n+s.length-state.heads[c],0)};}
 function event(type,extra={}){attempt.telemetry.push({type,atMs:attempt.elapsedMs,...extra,...measure()});}
 function loadLevel(id,{restart=false,lesson=true}={}){
  syncTime();save();setActivity('level-switch',false);closeAll();setActivity('level-switch',false);const level=data.levels.find(l=>l.id===id)||data.levels[0],capacity=configuration(level),key=`${level.id}:${capacity}`;
  const old=store.attempts.find(a=>a.id===store.activeByLevel[key]);if(restart&&old?.outcome==='playing'){old.outcome='abandoned';old.endedAt=new Date().toISOString();}
  attempt=(!restart&&old)||startAttempt(level,capacity);
  try{
   const saved=store.presentations[attempt.id];
   if(attempt.commands.length&&!saved)throw Error('Missing checkpoint');
   timeline=new T.Timeline(R.create(level,capacity),saved);
  }catch(error){storageProblem='Сохранение повреждено: начата новая попытка. Предыдущие результаты сохранены.';if(attempt.outcome==='playing')attempt.outcome='interrupted';attempt=startAttempt(level,capacity);timeline=new T.Timeline(R.create(level,capacity));}
  state=timeline.state;revealNotice='';revealUntil=0;
  visual=timeline.shown;store.current=level.id;activity.sample(performance.now());buildCards();render();event('attempt-open',{restored:Boolean(store.presentations[attempt.id]),manualFast:store.settings.fast,effectiveSpeed:speed(),palette:store.settings.palette});save();setActivity('level-open');
  if(timeline.status!=='Working'&&state.status!=='Playing')finishAttempt();else if(lesson&&store.started&&LESSONS[level.id]&&!store.lessons.includes(level.id))showLesson(level.id);
 }
 function buildCards(){
  const depth=Math.min(VISIBLE_ROWS,Math.max(...state.level.tray.map(c=>c.length)));$('game').style.setProperty('--tray-height',Math.max(174,Math.min(232,87+(depth-1)*18))+'px');
  entryMarkers.forEach(n=>n.remove());entryMarkers=[].map(()=>{const node=document.createElement('div');node.className='entry-marker';node.textContent='▲';node.setAttribute('aria-hidden','true');$('boardFrame').append(node);return node;});
  $('tray').replaceChildren();$('slots').replaceChildren();$('bridgeButtons').replaceChildren();$('tray').style.setProperty('--columns',state.level.tray.length);$('slots').style.setProperty('--places',attempt.capacity);
  columns=state.level.tray.map((_,c)=>{const node=document.createElement('div');node.className='column';node.setAttribute('aria-label',`Столбец ${c+1}`);$('tray').append(node);return node;});
  topCards=state.level.pods.map((pod,p)=>{const node=document.createElement('button');node.type='button';node.className='pod-top';node.dataset.pod=p;const image=document.createElement('img');image.alt='';image.draggable=false;const count=document.createElement('strong'),mark=document.createElement('span'),group=document.createElement('span');mark.className='channel-mark';group.className='pair-tag';node.append(image,count,mark,group);node.addEventListener('click',()=>act({type:'tap',pod:p}));return {node,image,count,mark,group};});
  backCards=state.level.pods.map((pod,p)=>{const node=document.createElement('div');node.className='pod-behind';node.dataset.futurePod=p;const mark=document.createElement('span'),count=document.createElement('strong'),group=document.createElement('span');group.className='pair-tag';node.append(mark,group,count);return {node,mark,count,group};});
  slotCards=Array.from({length:attempt.capacity},(_,i)=>{const node=document.createElement('div');node.className='slot empty';node.setAttribute('role','img');const image=document.createElement('img');image.alt='';const count=document.createElement('strong'),wait=document.createElement('span'),group=document.createElement('span');wait.className='slot-wait';group.className='pair-tag';node.append(image,count,wait,group);$('slots').append(node);return {node,image,count,wait,group};});
  $('heroBar').hidden=!state.level.hero;if(state.level.hero){$('heroActionPortrait').src=assets.sprig;bridgeCards=state.level.hero.choices.map((choice,i)=>{const button=document.createElement('button');button.type='button';button.dataset.bridge=i;button.addEventListener('click',()=>act({type:'bridge',choice:i}));$('bridgeButtons').append(button);return button;});}else bridgeCards=[];
 }
 // v8E (the owner, 2026-10-25): a linked pair is joined by a tube, in the tray and in the waiting places, not by a letter,
 // as in Colony Flow; a partner below the visible rows gets a tube running down into its column. A tapped capsule
 // flies from its column into its place (a pair together).
 const NS='http://www.w3.org/2000/svg',arriving=new Map();
 function tubes(container,links,k=1){let svg=container.querySelector(':scope > svg.tubes');if(!svg){svg=document.createElementNS(NS,'svg');svg.setAttribute('class','tubes');svg.setAttribute('aria-hidden','true');container.prepend(svg);}
  const box=container.getBoundingClientRect();svg.setAttribute('viewBox',`0 0 ${Math.max(1,box.width)} ${Math.max(1,box.height)}`);
  const centre=n=>{const r=n.getBoundingClientRect();return [r.left+r.width/2-box.left,r.top-box.top];};
  const parts=[];for(const [a,b] of links){const [x1,y1]=centre(a),[x2,y2]=Array.isArray(b)?b:centre(b);
   for(const [w0,c] of [[11,'#5d452c'],[7,'#b98d5a'],[2.2,'#f1d9a8']]){const w=w0*k;const l=document.createElementNS(NS,'line');l.setAttribute('x1',x1);l.setAttribute('y1',y1-(w0<3?1.5*k:0));l.setAttribute('x2',x2);l.setAttribute('y2',y2-(w0<3?1.5*k:0));l.setAttribute('stroke',c);l.setAttribute('stroke-width',w);l.setAttribute('stroke-linecap','round');parts.push(l);}}
  svg.replaceChildren(...parts);}
 function drawTubes(level,s){
  const shown=new Map();level.tray.forEach((stack,c)=>stack.slice(s.heads[c],s.heads[c]+VISIBLE_ROWS).forEach((p,k)=>shown.set(p,k===0?topCards[p].node:backCards[p].node)));
  const groups=new Map();level.pods.forEach((pod,p)=>{if(pod.group&&s.slots.indexOf(p)<0&&s.remaining[p]>0&&level.tray.some((st,c)=>st.indexOf(p)>=s.heads[c]))(groups.get(pod.group)||groups.set(pod.group,[]).get(pod.group)).push(p);});
  const trayLinks=[],box=$('tray').getBoundingClientRect();
  for(const members of groups.values()){const seen=members.filter(p=>shown.has(p));
   for(let i=1;i<seen.length;i++)trayLinks.push([shown.get(seen[i-1]),shown.get(seen[i])]);
   for(const p of members.filter(p=>!shown.has(p))){const c=level.tray.findIndex(st=>st.includes(p)),col=columns[c].getBoundingClientRect(),from=seen[0];if(from===undefined)continue;
    const stack=Array.from(columns[c].children),last=stack.length?stack.at(-1).getBoundingClientRect():col;trayLinks.push([shown.get(from),[col.left+col.width/2-box.left,Math.min(box.height-2,last.bottom-box.top+10)]]);}}
  tubes($('tray'),trayLinks);
  const placed=new Map(Array.from($('slots').children).filter(n=>n.dataset.slotPod!==undefined&&!n.classList.contains('arriving')).map(n=>[+n.dataset.slotPod,n])),slotLinks=[];
  const slotGroups=new Map();for(const [p,n] of placed){const g=level.pods[p].group;if(g)(slotGroups.get(g)||slotGroups.set(g,[]).get(g)).push(n);}
  for(const nodes of slotGroups.values())for(let i=1;i<nodes.length;i++)slotLinks.push([nodes[i-1],nodes[i]]);
  tubes($('slots'),slotLinks);}
 function fly(pods,from){if(reduced.matches)return;const until=performance.now()+340;
  for(const p of pods){const src=from.get(p),slot=$('slots').querySelector(`.slot[data-slot-pod="${p}"]`);if(!src||!slot)continue;const dst=slot.getBoundingClientRect(),pod=state.level.pods[p];
   const ghost=document.createElement('div');ghost.className='flying-pod';ghost.setAttribute('aria-hidden','true');colour(ghost,pod.color);ghost.textContent=visual.remaining[p];
   Object.assign(ghost.style,{left:src.left+'px',top:src.top+'px',width:src.width+'px',height:src.height+'px'});document.body.append(ghost);
   arriving.set(p,until);slot.classList.add('arriving');
   requestAnimationFrame(()=>{ghost.style.transform=`translate(${dst.left-src.left}px,${dst.top-src.top}px) scale(${dst.width/src.width},${dst.height/src.height})`;});
   setTimeout(()=>{ghost.remove();arriving.delete(p);render();},340);}}
 function render(){
  if(!state)return;needsRender=false;const s=timeline.shown,level=s.level,free=timeline.free,workers=timeline.workers;
  $('levelTitle').textContent=level.title;$('levelShort').textContent=`L${level.id}`;$('difficulty').textContent=level.difficulty;$('difficulty').className=level.difficulty==='Super Hard'?'hard super':level.difficulty==='Hard'?'hard':'';
  $('boardSize').textContent=`${level.w}×${level.h}`;$('workLabel').textContent=`Осталось ${s.board.reduce((n,c)=>n+(c>=0),0)}`;$('slotsLabel').textContent=`${free} свободно из ${attempt.capacity}`;$('movesLabel').textContent=`${attempt.commands.filter(a=>a.type==='tap').length} тапов`;
  $('speedButton').textContent=speed()+'×';$('speedButton').dataset.auto=String(timeline.tail);$('speedButton').setAttribute('aria-pressed',String(fast()));$('speedButton').setAttribute('aria-label',timeline.tail?`Авто ×${T.TAIL.speed}: очередь закончилась`:'Переключить скорость работы');
  // Slots stay fixed on release and single admission. Only an accepted pair compacts the model left.
  const placed=Array.from(s.slots);
  slotCards.forEach(({node,image,count,wait,group},i)=>{const p=placed[i],filled=p>=0;node.className=`slot ${filled?'filled':'empty'}`;image.hidden=count.hidden=wait.hidden=!filled;group.hidden=!filled||!level.pods[p]?.group;
   if(filled){const code=level.pods[p].color,ready=s.remaining[p]===0,paired=Boolean(level.pods[p].group),working=workers.has(p);colour(node,code);image.hidden=!symbols();if(image.dataset.hero!==palette[code].hero){image.src=assets[palette[code].hero];image.dataset.hero=palette[code].hero;}count.textContent=ready?'✓':s.remaining[p];wait.textContent=ready?(paired?'ждёт пару':'готово'):working?'в пути':'ждёт';group.textContent=level.pods[p].group||'';node.classList.toggle('pair-ready',ready&&paired);node.classList.toggle('arriving',arriving.has(p));group.hidden=true;node.classList.toggle('working',working&&!reduced.matches);node.dataset.slotPod=p;node.setAttribute('aria-label',`Место ${i+1}: ${palette[code].name}, ${ready?(paired?'готово, ждёт напарника':'готово'):'осталось '+s.remaining[p]}`);}else{delete node.dataset.slotPod;node.setAttribute('aria-label',`Место ${i+1}: свободно`);}
  });
  level.tray.forEach((stack,c)=>{const left=stack.slice(s.heads[c],s.heads[c]+VISIBLE_ROWS),node=columns[c];if(!left.length){node.replaceChildren();node.className='column empty';node.textContent='Пусто';return;}node.className='column';const nodes=left.map((p,k)=>{const card=k===0?topCards[p]:backCards[p],pod=level.pods[p],code=s.revealed[p]?pod.color:'?',unknown=code==='?';const edge=k===VISIBLE_ROWS;colour(card.node,code);card.node.classList.toggle('mystery',unknown);card.node.classList.toggle('pod-edge',edge);card.count.textContent=unknown?'':s.remaining[p];card.mark.textContent=palette[code].mark;card.mark.hidden=!(unknown||symbols()&&!edge);card.group.hidden=!pod.group;card.group.textContent=pod.group?`${pod.group} ↔`:'';card.node.dataset.group=pod.group||'';const reason=k===0?timeline.check({type:'tap',pod:p}):null;card.node.classList.toggle('pair-blocked',Boolean(reason));card.node.setAttribute('aria-label',unknown?'Скрытая капсула, дальше в очереди':edge?`${palette[code].name}: ${s.remaining[p]} клеток, край четвёртого ряда`:unknown?'Скрытая капсула, дальше в очереди':`${palette[code].name}: ${s.remaining[p]} клеток${pod.group?', пара '+pod.group+', нужны два места':''}${k?', дальше в очереди':''}`);card.group.hidden=true;
    if(k===0){card.image.hidden=unknown||!symbols();if(unknown){card.image.removeAttribute('src');delete card.image.dataset.hero;}if(!unknown&&card.image.dataset.hero!==palette[code].hero){card.image.src=assets[palette[code].hero];card.image.dataset.hero=palette[code].hero;}card.node.disabled=timeline.status!=='Working'&&state.status!=='Playing';card.node.setAttribute('aria-disabled',String(Boolean(reason)||card.node.disabled));}return card.node;});
   if(node.childNodes.length!==nodes.length||nodes.some((n,i)=>node.childNodes[i]!==n))node.replaceChildren(...nodes);const n=left.length-1;if(n){const css=getComputedStyle(node),height=topCards[left[0]].node.getBoundingClientRect().height,margin=parseFloat(getComputedStyle(backCards[left[1]].node).marginTop);node.style.setProperty('--peek-height',Math.max(15,Math.min(20,Math.floor((node.clientHeight-parseFloat(css.paddingTop)-height-n*margin)/n)))+'px');}
  });
  drawTubes(level,s);
  const ready=timeline.bridgeChoices().map(c=>c.choice);bridgeCards.forEach((b,i)=>{const used=state.bridge>=0,chosen=state.bridge===i;b.disabled=used||!ready.includes(i)||state.status!=='Playing';b.classList.toggle('chosen',chosen);b.textContent=chosen?'Мостик вырос ✓':used?'Второй недоступен':`Мостик${bridgeCards.length>1?i===0?' слева':' справа':''}${ready.includes(i)?' · готов':' · дойди до берега'}`;});
  $('statusLine').textContent=storageProblem||(attempt.elapsedMs<revealUntil?revealNotice:null)||(timeline.pending?(free?'Команды работают. Можно выбрать следующую.':'Места заняты, команды ещё работают.'):state.status==='Won'?'Картинка собрана!':state.status==='Jammed'?'Работа остановилась, все места заняты.':state.status==='Stuck'?'Следующей паре не хватает мест.':`${attempt.capacity-free} мест занято. Выбери верхнюю капсулу.`);
  $('board').setAttribute('aria-label',`${level.title}, поле ${level.w} на ${level.h}, осталось ${s.board.reduce((n,c)=>n+(c>=0),0)} клеток. Муравьи заходят через ворота и обходят картинку по дорожке.`);draw();
 }
 function act(action){
  if(dialogs.some(d=>d.open)||document.hidden)return false;syncTime();const flyFrom=new Map();if(action.type==='tap'){const g=state.level.pods[action.pod]?.group;for(const p of g?state.level.pods.map((q,i)=>q.group===g?i:-1).filter(i=>i>=0):[action.pod]){const n=topCards[p]?.node;if(n?.isConnected)flyFrom.set(p,n.getBoundingClientRect());else{const b=backCards[p]?.node;if(b?.isConnected)flyFrom.set(p,b.getBoundingClientRect());}}}
  const before=measure(),speedBefore=speed(),result=timeline.commit(action);
  attempt.inputEvents.push({atMs:attempt.elapsedMs,action:{...action},accepted:result.accepted,reason:result.reason||null,...before});
  if(!result.accepted){const messages={'visual-places-busy':'Плашки ещё заняты работой. Дождись пустого места.','partner-not-exposed':'Сначала открой напарника — капсулу на другом конце трубки.','pair-needs-two-slots':'Для пары нужны два свободных места.','no-slot':'Нет свободного места.','visible-bridge-not-ready':'Команды ещё добираются до берега.','finished':timeline.pending?'Команды заканчивают работу.':'Попытка закончилась.'};render();$('statusLine').textContent=messages[result.reason]||'Эта команда сейчас недоступна.';save();return false;}
  state=timeline.state;visual=timeline.shown;recordWorkEvents();attempt.commands.push({...action,modelTimeMs:timeline.now});attempt.updatedAt=new Date().toISOString();attempt.logicalOutcome=state.status.toLowerCase();event('commit',{action:{...action},command:attempt.commands.length});
  for(const r of result.events.filter(e=>e.type==='reveal')){
   const record={pod:r.pod,mode:r.mode,color:r.color,count:state.level.pods[r.pod].count,atMs:attempt.elapsedMs,modelTimeMs:timeline.now,freeBefore:attempt.capacity-before.visualHeld,freeAfter:timeline.free,clearedFraction:timeline.processedCells/state.level.pods.reduce((n,p)=>n+p.count,0),command:attempt.commands.length};attempt.reveals.push(record);event('reveal',record);
   if(r.mode){revealNotice=`Открыто: ${palette[r.color].name} · ${record.count}`;revealUntil=attempt.elapsedMs+2000;}
  }
  if(R.queueEmpty(state)&&attempt.autoFastAtAction===null){attempt.autoFastAtAction=attempt.commands.length;attempt.autoFastAtMs=attempt.elapsedMs;event('queue-exhausted',{effectiveSpeed:speed()});}
  if(speedBefore!==(speed()))event('effective-speed',{from:speedBefore,to:speed(),reason:'queue-exhausted'});
  save();render();fly([...flyFrom.keys()],flyFrom);finishAttempt();return true;
 }
 function finishAttempt(){
  if(!timeline||timeline.pending||state.status==='Playing')return;
  if(attempt.outcome==='playing'){syncTime();attempt.outcome=state.status.toLowerCase();attempt.logicalOutcome=attempt.outcome;attempt.endedAt=new Date().toISOString();attempt.workFinishedAtMs=attempt.elapsedMs;event('finished');setActivity('finished',false);save();}
  if(!dialogs.some(d=>d.open))showEnd();
 }
 function frame(now){
  const before=timeline?.free,wasPending=timeline?.pending,changed=syncTime();
  if(timeline&&(wasPending||needsRender)&&active()){if(changed||needsRender)render();else draw();if(!timeline.pending){event('work-idle');finishAttempt();save();}else if(now-lastCheckpoint>750){save();lastCheckpoint=now;}}
  if(timeline&&!timeline.pending&&state.status!=='Playing'&&attempt.outcome==='playing')finishAttempt();
  requestAnimationFrame(frame);
 }
  function showLesson(id){
    const lesson=LESSONS[id];if(!lesson)return;
    $('lessonTag').textContent=`L${id} · ${(data.lessonTags&&data.lessonTags[id])||'НОВЫЙ ШАГ'}`;$('lessonTitle').textContent=lesson[0];$('lessonPortrait').src=assets[lesson[1]];$('lessonPortrait').alt=lesson[1];
    $('lessonSwatch').className='lesson-material';$('lessonSwatch').style.background=palette[lesson[2]].color;$('lessonSwatch').textContent=palette[lesson[2]].mark;$('lessonText').textContent=lesson[3];
    modal('lessonDialog');
  }
  function showEnd(){
    if(timeline.pending||$('endDialog').open)return;
    const won=state.status==='Won';$('endTag').textContent=`L${state.level.id} · ${won?'ПРОЙДЕН':'ПОПЫТКА'}`;$('endTitle').textContent=won?'Отлично получилось!':state.status==='Stuck'?'Паре не хватает места':'Места закончились';
    $('endDescription').textContent=won?`${state.level.title}. ${attempt.commands.filter(a=>a.type==='tap').length} тапов. ${state.level.id===data.levels.length?`Все ${data.levels.length} доступны в выборе уровней.`+' Расскажи, как ощущается развитие.':'Можно оценить уровень и перейти дальше.'}`:'Остатки ждут закрытые клетки: '+Array.from(state.slots).filter(p=>p>=0&&state.remaining[p]>0).map(p=>palette[state.level.pods[p].color].name+' '+state.remaining[p]).join(', ')+'. Оставь место для команды, которая откроет путь. Ретрай бесплатный.';
    $('finishedPicture').hidden=!won;if(won)drawPicture($('finishedPicture'),true);
    $('nextButton').hidden=!won;$('nextButton').textContent=state.level.id===data.levels.length?'Посмотреть результаты':'Следующий уровень';
    $('uncertaintyFeedback').hidden=!state.level.pods.some(p=>p.mystery);$('levelNote').value=attempt.note||'';syncFeedback();modal('endDialog');save();
  }
  function syncFeedback(){for(const field of ['rating','interest','art','planning','depthNeed','pace','waiting','uncertainty'])document.querySelectorAll(`[data-${field.replace(/[A-Z]/g,c=>'-'+c.toLowerCase())}]`).forEach(b=>{const selected=b.dataset[field]===attempt[field];b.classList.toggle('selected',selected);b.setAttribute('aria-pressed',String(selected));});}
  function retry(){syncTime();save();if(attempt.commands.length&&state.status==='Playing')modal('retryDialog');else loadLevel(state.level.id,{restart:true,lesson:false});}
  function showLevels(){
    syncTime();save();closeAll();document.querySelectorAll('[data-palette]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.palette===store.settings.palette)));const grid=$('levelGrid');grid.replaceChildren();
    for(const level of data.levels){const button=document.createElement('button'),done=store.attempts.some(a=>a.level===level.id&&a.outcome==='won');button.type='button';button.className=`level-choice${level.difficulty==='Super Hard'?' hard super':level.difficulty==='Hard'?' hard':''}${done?' done':''}${level.id===state.level.id?' current':''}`;button.dataset.level=level.id;
      const strong=document.createElement('strong'),small=document.createElement('small');strong.textContent=`${level.id}${done?' ✓':''}`;small.textContent=level.difficulty==='Hard'||level.difficulty==='Super Hard'?level.difficulty:`${level.w}×${level.h}`;button.append(strong,small);if(data.changedLevels.includes(level.id)){const tag=document.createElement('span');tag.className='new-level';tag.textContent='ЧЕРНОВИК';button.append(tag);}button.setAttribute('aria-label',`Уровень ${level.id}, ${level.title}, ${level.difficulty}${data.changedLevels.includes(level.id)?', картинка-черновик':''}${done?', пройден':''}`);button.addEventListener('click',()=>loadLevel(level.id));grid.append(button);
    }modal('levelsDialog');
  }
  function showResults(){
    closeAll();syncTime();save();const won=new Set(store.attempts.filter(a=>a.outcome==='won').map(a=>a.level));$('resultsSummary').textContent=`Пройдено ${won.size} из ${data.levels.length}. Попыток: ${store.attempts.length}.`;
    $('resultsList').replaceChildren();
    for(const level of data.levels){const attempts=store.attempts.filter(a=>a.level===level.id),wins=attempts.filter(a=>a.outcome==='won'),last=attempts.at(-1),row=document.createElement('div');row.className='result-row';const label=document.createElement('strong'),details=document.createElement('div');label.textContent=`L${level.id}`;
      details.textContent=!attempts.length?'Ещё не пробовал':`${wins.length?'✓ Пройден':'В процессе'} · ${attempts.length} поп. · ${Math.round(attempts.reduce((sum,a)=>sum+a.elapsedMs,0)/1000)} сек.${last.rating?` · ${last.rating==='easy'?'легко':last.rating==='normal'?'нормально':'трудно'}`:''}${last.pace?' · темп: '+({fast:'быстро',okay:'хорошо',slow:'медленно'}[last.pace]):''}${last.note?' · '+last.note:''}`;
      row.append(label,details);$('resultsList').append(row);
    }$('versionLabel').textContent=`${data.version} · каталог ${data.hash.slice(0,12)} · правила ${data.rulesHash.slice(0,12)}`;modal('resultsDialog');
  }
  function exportResults(){
    syncTime();save();const report={format:'bloomlings-pilot-results-11',exportedAt:new Date().toISOString(),pilotVersion:data.version,baseVersion:data.baseVersion,changedLevels:data.changedLevels,catalogHash:data.hash,rulesHash:data.rulesHash,timelineHash:data.timelineHash,clockHash:data.clockHash,paletteHash:data.paletteHash,presentation:{trips:T.PROFILE,tail:T.TAIL,visibleRows:VISIBLE_ROWS,edgeRow:'none',symbols:'accessible palette only',planningAnswers:['whole-game','when-stuck','start-then-colours','colours'],topReveal:'depth-only',sealedReveal:'on-admission',releaseAfterReturn:false,releaseAfterLastDeparture:true,sameColourAllocation:'live-independent-cadence',workEventCodes:['depart','arrive','clear','return','release','assign'],tripPlanFields:['id','pod','cell','assignedMs','startMs','arriveMs','clearMs','returnMs','routeLength','physicalFrontier']},settings:store.settings,attempts:store.attempts};
    const blob=new Blob([JSON.stringify(report,null,2)+'\n'],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`Bloomlings-pilot-v11-results-${new Date().toISOString().slice(0,10)}.json`;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),3000);
  }
  function draw(){if(!state||drawing)return;drawing=requestAnimationFrame(()=>{drawing=null;drawPicture($('board'),false);});}
  // the soft cube of the game (spec 005 contracts/look.md §6.18): a clear margin of 3.5 %, a rounded body (corners 17 %),
  // the top ending 10 % above the bottom (the colour lightened 12 % to darkened 4 %), a cushion toward the colour lightened
  // 35 %, a white gloss dash at the top left, the front face darkened 20 % to 36 %, a soft deep edge darkened 55 %
  const cubes=new Map();
  const mixHex=(hex,f)=>{const n=parseInt(hex.slice(1),16),c=[n>>16,(n>>8)&255,n&255],t=f>0?255:0,k=Math.abs(f);return c.map(v=>Math.round(v+(t-v)*k));};
  const rgb=(hex,f,a=1)=>`rgba(${mixHex(hex,f).join(',')},${a})`;
  function cube(color,size){const key=color+':'+size;let c=cubes.get(key);if(c)return c;
    c=document.createElement('canvas');c.width=c.height=size;const x=c.getContext('2d'),m=size*.035,s=size-2*m,rad=s*.17,side=size*.10,top=s-side;
    let g=x.createLinearGradient(0,m,0,m+s);g.addColorStop(0,rgb(color,-.2));g.addColorStop(1,rgb(color,-.36));x.fillStyle=g;x.beginPath();x.roundRect(m,m,s,s,rad);x.fill();
    x.strokeStyle=rgb(color,-.55,.55);x.lineWidth=Math.max(.6,size*.025);x.beginPath();x.roundRect(m,m,s,s,rad);x.stroke();
    g=x.createLinearGradient(0,m,0,m+top);g.addColorStop(0,rgb(color,.12));g.addColorStop(1,rgb(color,-.04));x.fillStyle=g;x.beginPath();x.roundRect(m,m,s,top,rad);x.fill();
    const r=x.createRadialGradient(m+s*.45,m+top*.42,0,m+s*.5,m+top*.5,s*.6);r.addColorStop(0,rgb(color,.35,.32));r.addColorStop(1,rgb(color,.35,0));x.fillStyle=r;x.beginPath();x.roundRect(m,m,s,top,rad);x.fill();
    x.strokeStyle='rgba(255,255,255,.28)';x.lineWidth=Math.max(.5,size*.03);x.beginPath();x.moveTo(m+rad,m+size*.025);x.lineTo(m+s*.6,m+size*.025);x.stroke();
    x.lineCap='round';x.strokeStyle='rgba(255,255,255,.55)';x.lineWidth=Math.max(.7,size*.045);x.beginPath();x.moveTo(m+s*.17,m+top*.2);x.lineTo(m+s*.4,m+top*.13);x.stroke();
    cubes.set(key,c);return c;}
  function drawPicture(canvas,finished){
    const s=visual||state,l=s.level,rect=canvas.getBoundingClientRect();
    const width=finished?280:Math.max(1,rect.width),height=finished?280:Math.max(1,rect.height),scale=finished?1:Math.min(2,window.devicePixelRatio||1);
    if(canvas.width!==Math.round(width*scale)||canvas.height!==Math.round(height*scale)){canvas.width=Math.round(width*scale);canvas.height=Math.round(height*scale);}
    const ctx=canvas.getContext('2d');ctx.setTransform(scale,0,0,scale,0,0);ctx.clearRect(0,0,width,height);
    const cell=Math.min((width-8)/l.w,(height-10)/l.h),ox=(width-l.w*cell)/2,oy=(height-l.h*cell)/2,depth=Math.max(.8,cell*.14),gap=Math.max(.35,cell*.035);
    if(!finished)entryMarkers.forEach((node,i)=>{node.style.left=`${ox+((l.entries[i]%l.w)+.5)*cell}px`;});
    const original=s.meta.board;
    for(let y=l.h-1;y>=0;y--)for(let x=0;x<l.w;x++){
      const index=y*l.w+x,code=finished?original[index]:s.board[index],px=ox+x*cell,py=oy+(l.h-1-y)*cell;
      // v9 garden: the fence (board edge, and the top row where the path is closed) and the path are drawn as solid
      // strips, not tiles; a gate is a tunnel mouth in the fence. The stream keeps its water look.
      const yTop=l.h-1-y,edgeCell=x===0||y===0||x===l.w-1||y===l.h-1,fenceRow=yTop===1&&l.rowsBottomFirst[y].slice(1,-1).split('').every(c=>c==='#');
      if(code===R.WALL&&(edgeCell||fenceRow)){if(!finished){ctx.fillStyle='#5f8f52';ctx.fillRect(px-.3,py-.3,cell+.6,cell+.6);ctx.fillStyle='#4b7642';ctx.fillRect(px-.3,py+cell*.7,cell+.6,cell*.3+.3);}continue;}
      if(original[index]===R.OPEN&&!finished){ctx.fillStyle='#eadcb5';ctx.fillRect(px-.3,py-.3,cell+.6,cell+.6);
        if(l.entries.includes(index)){ctx.fillStyle='#3d5a37';ctx.beginPath();const r=cell*.42;ctx.arc(px+cell/2,py+cell*(y===0?.85:.5),r,0,Math.PI*2);ctx.fill();}
        continue;}
      if(code===R.WALL){ctx.fillStyle='#b6d6cc';ctx.fillRect(px,py,cell+.1,cell+.1);ctx.strokeStyle='#d6ece2';ctx.lineWidth=Math.max(.7,cell*.08);ctx.beginPath();ctx.moveTo(px+cell*.15,py+cell*.55);ctx.quadraticCurveTo(px+cell*.55,py+cell*.25,px+cell*.88,py+cell*.55);ctx.stroke();continue;}
      if(code===R.OPEN){
        // v9: cells that were open from the start (round an object-only picture) are not drawn: the card shows through.
        if(original[index]>=0){ctx.fillStyle=palette[R.CHANNELS[original[index]]].color+'22';ctx.fillRect(px+gap,py+gap,cell-gap*2,cell-gap*2);}
        if(!finished&&s.bridge>=0&&index===l.hero.choices[s.bridge].cell){ctx.fillStyle='#8eac62';ctx.fillRect(px+gap,py,cell-gap*2,cell);ctx.strokeStyle='#5b7d46';ctx.lineWidth=Math.max(.7,cell*.07);for(let k=1;k<4;k++){ctx.beginPath();ctx.moveTo(px+gap,py+cell*k/4);ctx.lineTo(px+cell-gap,py+cell*k/4);ctx.stroke();}}
        continue;
      }
      const p=palette[R.CHANNELS[code]];
      // v11: the game's soft cube (spec 005 §6.18), drawn from a cached picture per colour and size
      ctx.drawImage(cube(p.color,Math.max(4,Math.round(cell*scale))),px,py,cell,cell);
      if(symbols()&&!finished&&cell>=8){ctx.fillStyle=p.text;ctx.font=`900 ${Math.max(5,cell*.52)}px Nunito, sans-serif`;ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillText(p.mark,px+cell*.5,py+cell*.49);}
    }
    if(!finished&&l.hero&&s.bridge<0){
      const ready=timeline.bridgeChoices().map(c=>c.choice);
      l.hero.choices.forEach((choice,i)=>{const x=choice.anchor%l.w,y=Math.floor(choice.anchor/l.w),px=ox+(x+.5)*cell,py=oy+(l.h-y-.5)*cell;ctx.beginPath();ctx.arc(px,py,Math.max(2,cell*.34),0,Math.PI*2);ctx.fillStyle=ready.includes(i)?'#f3e4a7':'#fff7db99';ctx.fill();ctx.strokeStyle='#5b7d46';ctx.lineWidth=Math.max(1,cell*.08);ctx.stroke();});
    }
    if(!finished)for(const actor of timeline.couriers){
      const lane=((actor.id%3)-1)*Math.min(1.1,cell*.09),p=palette[l.pods[actor.pod].color],px=ox+(actor.x+.5)*cell+lane,py=oy+(l.h-actor.y-.5)*cell+lane,r=Math.max(2.8,Math.min(5,cell*.36));
      ctx.fillStyle='#253b3880';ctx.beginPath();ctx.ellipse(px,py+r*.65,r*1.1,r*.45,0,0,Math.PI*2);ctx.fill();
      ctx.fillStyle=p.color;ctx.strokeStyle=p.text;ctx.lineWidth=.8;ctx.beginPath();ctx.ellipse(px,py,r*.85,r,0,0,Math.PI*2);ctx.fill();ctx.stroke();
      ctx.fillStyle=p.text;ctx.beginPath();ctx.arc(px-r*.23,py-r*.22,.65,0,Math.PI*2);ctx.arc(px+r*.23,py-r*.22,.65,0,Math.PI*2);ctx.fill();
      if(actor.carrying){ctx.fillStyle=p.light;ctx.strokeStyle=p.dark;ctx.fillRect(px+r*.35,py-r*1.25,r,r);ctx.strokeRect(px+r*.35,py-r*1.25,r,r);}
    }
  }
  $('levelsButton').addEventListener('click',showLevels);
  $('retryButton').addEventListener('click',retry);
  $('confirmRetryButton').addEventListener('click',()=>loadLevel(state.level.id,{restart:true,lesson:false}));
  $('endRetryButton').addEventListener('click',()=>loadLevel(state.level.id,{restart:true,lesson:false}));
  $('endMapButton').addEventListener('click',showLevels);
  $('nextButton').addEventListener('click',()=>state.level.id===data.levels.length?showResults():loadLevel(state.level.id+1));
  $('speedButton').addEventListener('click',()=>{syncTime();const from=speed();store.settings.fast=!store.settings.fast;event('manual-speed',{manualFast:store.settings.fast,effectiveSpeed:speed()});if(from!==(speed()))event('effective-speed',{from,to:speed(),reason:'manual'});save();render();});
  $('resultsButton').addEventListener('click',showResults);$('exportButton').addEventListener('click',exportResults);
  $('helpButton').addEventListener('click',()=>{closeAll();modal('welcomeDialog');});
  $('startButton').addEventListener('click',()=>{store.started=true;save();close('welcomeDialog');if(LESSONS[state.level.id]&&!store.lessons.includes(state.level.id))showLesson(state.level.id);});
  $('lessonButton').addEventListener('click',()=>{if(!store.lessons.includes(state.level.id))store.lessons.push(state.level.id);save();close('lessonDialog');});
  document.querySelectorAll('[data-close]').forEach(b=>b.addEventListener('click',()=>close(b.dataset.close)));
  document.querySelectorAll('[data-rating]').forEach(b=>b.addEventListener('click',()=>{attempt.rating=b.dataset.rating;syncFeedback();save();}));
  document.querySelectorAll('[data-interest]').forEach(b=>b.addEventListener('click',()=>{attempt.interest=b.dataset.interest;syncFeedback();save();}));
  document.querySelectorAll('[data-art]').forEach(b=>b.addEventListener('click',()=>{attempt.art=b.dataset.art;syncFeedback();save();}));
  document.querySelectorAll('[data-planning]').forEach(b=>b.addEventListener('click',()=>{attempt.planning=b.dataset.planning;syncFeedback();save();}));
  document.querySelectorAll('[data-depth-need]').forEach(b=>b.addEventListener('click',()=>{attempt.depthNeed=b.dataset.depthNeed;syncFeedback();save();}));
  document.querySelectorAll('[data-pace]').forEach(b=>b.addEventListener('click',()=>{attempt.pace=b.dataset.pace;syncFeedback();save();}));
  document.querySelectorAll('[data-waiting]').forEach(b=>b.addEventListener('click',()=>{attempt.waiting=b.dataset.waiting;syncFeedback();save();}));
  document.querySelectorAll('[data-uncertainty]').forEach(b=>b.addEventListener('click',()=>{attempt.uncertainty=b.dataset.uncertainty;syncFeedback();save();}));
  $('levelNote').addEventListener('input',()=>{attempt.note=$('levelNote').value;save();});
  dialogs.forEach(d=>{d.addEventListener('cancel',()=>syncTime());d.addEventListener('close',()=>{syncTime();setActivity('dialog-close:'+d.id);});});
  document.addEventListener('visibilitychange',()=>{syncTime();event('visibility',{hidden:document.hidden});setActivity('visibility');save();});
  window.addEventListener('pagehide',()=>{syncTime();event('pagehide');setActivity('pagehide',false);save();});
  window.addEventListener('pageshow',()=>{syncTime();setActivity('pageshow');});
  window.addEventListener('resize',()=>{render();});new ResizeObserver(draw).observe($('boardFrame'));
  setInterval(()=>{syncTime();save();},15000);
  for(const name of ['sprig','bloom','drop','twig']){const img=document.createElement('img');img.src=assets[name];img.alt=name;img.width=90;img.height=110;$('welcomeHeroes').append(img);}
  loadLevel(store.current,{lesson:false});
  if(!store.started){closeAll();modal('welcomeDialog');}
  document.querySelectorAll('[data-palette]').forEach(b=>b.addEventListener('click',()=>{syncTime();store.settings.palette=b.dataset.palette;event('palette',{palette:store.settings.palette});palette=paletteOf(store.settings.palette);document.querySelectorAll('[data-palette]').forEach(n=>n.setAttribute('aria-pressed',String(n===b)));save();render();}));
  requestAnimationFrame(frame);
  // Read-only inspection: tests drive real DOM inputs and the browser clock.
  Object.defineProperty(window,'bloomlingsPilot',{value:Object.freeze({snapshot:()=>({level:state.level.id,capacity:attempt.capacity,status:state.status,displayStatus:timeline.status,signature:R.signature(state),visual:{board:Array.from(visual.board),remaining:Array.from(visual.remaining),slots:Array.from(visual.slots),heads:Array.from(visual.heads),revealed:Array.from(visual.revealed),bridge:visual.bridge},commands:attempt.commands.length,bridge:state.bridge,autoFast:R.queueEmpty(state),modelTimeMs:timeline.now,hasChoice:timeline.hasChoice,pendingTrips:timeline.jobs.size,pendingCells:timeline.pendingCells,pendingBeats:timeline.steps.length-timeline.cursor,phase:timeline.phase,physicalFree:timeline.free,elapsedMs:attempt.elapsedMs,timerRunning:activity.running,reservations:Array.from(timeline.reserved),inFlight:Array.from(timeline.inFlight),couriers:timeline.couriers,pendingTrips:timeline.jobs.size,palette:store.settings.palette,symbols:symbols()}),exportObject:()=>JSON.parse(JSON.stringify(store))})});
})();
