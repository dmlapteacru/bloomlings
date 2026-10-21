/* V7: logical outcomes + a physical, resumable work timeline. */
(() => {
 'use strict';
 const $=id=>document.getElementById(id),R=window.BloomPilotRules,T=window.BloomPilotTimeline;
 const data=JSON.parse($('pilotData').textContent),assets=JSON.parse($('pilotAssets').textContent);
 let palette=window.BloomPilotPalette.palette('normal');
 const LESSONS={
  1:['Три героя, три материала','sprig','G','Sprig собирает лист, Drop — воду, Twig — янтарь. Отправляй верхние капсулы: курьеры идут от входа к доступным клеткам своего точного цвета. Есть пять мест. Можно выбирать следующие команды во время работы; число показывает, сколько героев ещё не вышло. Место освобождается, когда вышла вся команда — курьеры могут ещё идти по полю.'],
  2:['Привет, Bloom!','bloom','P','Розовый лепесток собирает Bloom. Смотри не только на цвет: число показывает объём команды, а другие цвета могут закрывать путь к её клеткам.'],
  3:['Жемчужный материал Drop','drop','C','Кремовый жемчуг и голубая вода — разные задачи одного героя. На капсулах всегда есть свой символ материала. В выборе уровней можно включить цвета с узорами.'],
  4:['Большой цветной сад','twig','D','Тёмная земля Twig рисует контур, янтарь — светлые детали. Уже шесть материалов: сравнивай доступные края рисунка с очередью. На каждую доступную клетку выходит отдельный курьер с небольшим интервалом. Чем шире фронт, тем больше героев в пути; следующий выход не ждёт возвращения предыдущего. Скорость можно переключить вручную.'],
  5:['Подготовь следующую команду','twig','A','Иногда полезно заранее отправить небольшую команду, чтобы открыть следующую капсулу в её столбце. Если своего материала пока не достать, она подождёт и займёт место. Оставь место для команды, которая откроет путь.'],
  6:['Скрытая глубина','drop','B','В глубине очереди цвет закрыт. Когда капсула становится верхней, он откроется до твоего выбора. При повторе содержимое остаётся тем же.'],
  8:['Две команды одного цвета','sprig','G','Одинаковая буква ↔ связывает две капсулы. Тап отправляет обе, нужны два свободных места и обе капсулы наверху. Даже готовая команда ждёт напарника: оба места освободятся вместе. Начинаем с пары одного цвета.'],
  9:['Разные материалы в паре','bloom','P','Теперь две связки объединяют разные материалы. Обе пары вместе займут четыре места. Одна команда может закончить раньше, но её место останется занято пока обе команды не отправят всех героев. Подготовь путь для обеих.'],
  10:['Первый Hard','twig','A','Здесь знакомые связки требуют планировать свободные места. Большая капсула доступного цвета не всегда лучший выбор. Ретрай свободный.'],
  11:['Сливовый материал Bloom','bloom','V','Слива — второй материал Bloom. Фиолетовые и розовые команды собирают разные клетки. Теперь знакомы все семь материалов этой главы.'],
  20:['Мостик Sprig','sprig','G','Доберись до отмеченного берега, затем нажми кнопку под полем. Sprig вырастит мостик через ручей. Он открывает путь к клеткам и может помочь даже при пяти занятых местах.'],
  21:['Выбери берег','sprig','G','Мостик один, а берега два. За ними доступны разные материалы. Учти ожидающие команды и очередь: подготовка и выбор берега меняют продолжение.']
 };

 const KEY=`bloomlings-pilot-v7:${data.hash}:${data.rulesHash}:${data.timelineHash}:${data.paletteHash}:${data.clockHash}`;
 const fresh=()=>({schema:7,catalogHash:data.hash,rulesHash:data.rulesHash,timelineHash:data.timelineHash,current:1,started:false,lessons:[],settings:{fast:false,symbols:false,palette:'normal'},attempts:[],activeByLevel:{},presentations:{},serial:0});
 let store=fresh(),storageProblem='';
 try{const saved=localStorage.getItem(KEY);if(saved){const s=JSON.parse(saved);if(s.schema!==7||!s.presentations||!Array.isArray(s.attempts)||!['normal','accessible'].includes(s.settings?.palette))throw Error('Invalid save');store=s;}}catch(error){storageProblem='Сохранение недоступно. Скачай результаты перед закрытием.';}
 let state,visual,timeline,attempt,lastCheckpoint=performance.now(),drawing=null,needsRender=false;const activity=new window.BloomPilotClock.ActiveClock(performance.now());
 let topCards=[],backCards=[],columns=[],slotCards=[],bridgeCards=[],entryMarkers=[];
 const reduced=matchMedia('(prefers-reduced-motion: reduce)'),dialogs=Array.from(document.querySelectorAll('dialog'));
 const pictures={};for(const name of ['sprig','bloom','drop','twig']){const img=new Image();img.src=assets[name];img.onload=()=>draw();pictures[name]=img;}
 document.documentElement.style.setProperty('--garden',`url("${assets.garden}")`);
 palette=window.BloomPilotPalette.palette(store.settings.palette);
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
   const pending=timeline.pending,bucket=R.queueEmpty(state)&&pending?'autoTail':timeline.hasChoice?'choice':pending?'blockedWorking':'stalled',speed=fast()?2:1;
   if(!pending){addTime(dt,bucket,false);break;}
   const next=Math.max(0,(timeline.steps[timeline.cursor].at-timeline.now)/speed),slice=Math.min(dt,next);
   addTime(slice,bucket,true);changed=timeline.advance(slice,speed)||changed;recordWorkEvents();dt-=slice;
   // advance(0) consumes equal-time events; every pass removes an event or time.
  }
  state=timeline?.state||state;visual=timeline?.shown||visual;return changed;
 }
 function syncTime(){const dt=activity.sample(performance.now()),changed=timeline&&attempt?consumeTime(dt):false;needsRender=needsRender||changed;return changed;}
 function setActivity(reason,force=null){
  const running=(force===null?active():force)&&attempt?.outcome==='playing'&&(timeline?.pending||state?.status==='Playing');
  if(activity.running!==Boolean(running)){const dt=activity.setRunning(Boolean(running),performance.now());if(timeline&&attempt)needsRender=consumeTime(dt)||needsRender;if(attempt&&timeline)event(running?'resume':'pause',{reason,effectiveSpeed:fast()?2:1});}
 }
 function modal(id){syncTime();$(id).showModal();setActivity('dialog:'+id);}
 function close(id){syncTime();$(id).close();setActivity('dialog:'+id);}
 function closeAll(){syncTime();dialogs.forEach(d=>{if(d.open)d.close();});setActivity('dialogs-closed');}
 function colour(element,code){const p=palette[code||'?'];element.style.setProperty('--color',p.color);element.style.setProperty('--text',p.text);element.style.setProperty('--text-shadow','none');}
 const symbols=()=>Boolean(store.settings.symbols||store.settings.palette==='accessible'),fast=()=>store.settings.fast||R.queueEmpty(state);
 function configuration(level){return 5;}
 function startAttempt(level,capacity){
  const a={id:++store.serial,level:level.id,capacity,config:{capacity,trips:T.PROFILE,visibility:'reveal-on-top',initialSettings:{manualFast:store.settings.fast,effectiveSpeed:store.settings.fast?2:1,palette:store.settings.palette,symbols:symbols()}},startedAt:new Date().toISOString(),commands:[],inputEvents:[],telemetry:[],elapsedMs:0,outcome:'playing',logicalOutcome:'playing',timeBuckets:{choice:0,blockedWorking:0,autoTail:0,stalled:0},workActiveMs:0,assignments:[],tripPlans:[],workEvents:[],intervals:[],rating:null,interest:null,art:null,planning:null,pace:null,note:'',autoFastAtAction:null,autoFastAtMs:null,workFinishedAtMs:null};
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
  state=timeline.state;
  visual=timeline.shown;store.current=level.id;activity.sample(performance.now());buildCards();render();event('attempt-open',{restored:Boolean(store.presentations[attempt.id]),manualFast:store.settings.fast,effectiveSpeed:fast()?2:1,palette:store.settings.palette});save();setActivity('level-open');
  if(timeline.status!=='Working'&&state.status!=='Playing')finishAttempt();else if(lesson&&store.started&&LESSONS[level.id]&&!store.lessons.includes(level.id))showLesson(level.id);
 }
 function buildCards(){
  const depth=Math.max(...state.level.tray.map(c=>c.length));$('game').style.setProperty('--tray-height',Math.max(174,Math.min(232,87+(depth-1)*18))+'px');
  entryMarkers.forEach(n=>n.remove());entryMarkers=state.meta.entries.map(()=>{const node=document.createElement('div');node.className='entry-marker';node.textContent='▲';node.setAttribute('aria-hidden','true');$('boardFrame').append(node);return node;});
  $('tray').replaceChildren();$('slots').replaceChildren();$('bridgeButtons').replaceChildren();$('tray').style.setProperty('--columns',state.level.tray.length);$('slots').style.setProperty('--places',attempt.capacity);
  columns=state.level.tray.map((_,c)=>{const node=document.createElement('div');node.className='column';node.setAttribute('aria-label',`Столбец ${c+1}`);$('tray').append(node);return node;});
  topCards=state.level.pods.map((pod,p)=>{const node=document.createElement('button');node.type='button';node.className='pod-top';node.dataset.pod=p;const image=document.createElement('img');image.alt='';image.draggable=false;const count=document.createElement('strong'),mark=document.createElement('span'),group=document.createElement('span');mark.className='channel-mark';group.className='pair-tag';node.append(image,count,mark,group);node.addEventListener('click',()=>act({type:'tap',pod:p}));return {node,image,count,mark,group};});
  backCards=state.level.pods.map((pod,p)=>{const node=document.createElement('div');node.className='pod-behind';node.dataset.futurePod=p;const mark=document.createElement('span'),count=document.createElement('strong'),group=document.createElement('span');group.className='pair-tag';node.append(mark,group,count);return {node,mark,count,group};});
  slotCards=Array.from({length:attempt.capacity},(_,i)=>{const node=document.createElement('div');node.className='slot empty';node.setAttribute('role','img');const image=document.createElement('img');image.alt='';const count=document.createElement('strong'),wait=document.createElement('span'),group=document.createElement('span');wait.className='slot-wait';group.className='pair-tag';node.append(image,count,wait,group);$('slots').append(node);return {node,image,count,wait,group};});
  $('heroBar').hidden=!state.level.hero;if(state.level.hero){$('heroActionPortrait').src=assets.sprig;bridgeCards=state.level.hero.choices.map((choice,i)=>{const button=document.createElement('button');button.type='button';button.dataset.bridge=i;button.addEventListener('click',()=>act({type:'bridge',choice:i}));$('bridgeButtons').append(button);return button;});}else bridgeCards=[];
 }
 function render(){
  if(!state)return;needsRender=false;const s=timeline.shown,level=s.level,free=timeline.free,workers=timeline.workers;
  $('levelTitle').textContent=level.title;$('levelShort').textContent=`L${level.id}`;$('difficulty').textContent=level.difficulty;$('difficulty').className=level.difficulty==='Hard'?'hard':'';
  $('boardSize').textContent=`${level.w}×${level.h}`;$('workLabel').textContent=`Осталось ${s.board.reduce((n,c)=>n+(c>=0),0)}`;$('slotsLabel').textContent=`${free} свободно из ${attempt.capacity}`;$('movesLabel').textContent=`${attempt.commands.filter(a=>a.type==='tap').length} тапов`;
  $('speedButton').textContent=fast()?'2×':'1×';$('speedButton').dataset.auto=String(R.queueEmpty(state));$('speedButton').setAttribute('aria-pressed',String(fast()));$('speedButton').setAttribute('aria-label',R.queueEmpty(state)?'Авто ×2: очередь закончилась':'Переключить скорость работы');$('symbolsButton').setAttribute('aria-pressed',String(symbols()));
  slotCards.forEach(({node,image,count,wait,group},i)=>{const p=s.slots[i],filled=p>=0;node.className=`slot ${filled?'filled':'empty'}`;image.hidden=count.hidden=wait.hidden=!filled;group.hidden=!filled||!level.pods[p]?.group;
   if(filled){const code=level.pods[p].color,ready=s.remaining[p]===0,paired=Boolean(level.pods[p].group),working=workers.has(p);colour(node,code);if(image.dataset.hero!==palette[code].hero){image.src=assets[palette[code].hero];image.dataset.hero=palette[code].hero;}count.textContent=ready?'✓':s.remaining[p];wait.textContent=ready?(paired?'ждёт пару':'готово'):working?'в пути':'ждёт';group.textContent=level.pods[p].group||'';node.classList.toggle('pair-ready',ready&&paired);node.classList.toggle('working',working&&!reduced.matches);node.dataset.slotPod=p;node.setAttribute('aria-label',`Место ${i+1}: ${palette[code].name}, ${ready?(paired?'готово, ждёт напарника':'готово'):'осталось '+s.remaining[p]}`);}else{delete node.dataset.slotPod;node.setAttribute('aria-label',`Место ${i+1}: свободно`);}
  });
  level.tray.forEach((stack,c)=>{const left=stack.slice(s.heads[c]),node=columns[c];if(!left.length){node.replaceChildren();node.className='column empty';node.textContent='Пусто';return;}node.className='column';const nodes=left.map((p,k)=>{const card=k===0?topCards[p]:backCards[p],pod=level.pods[p],code=s.revealed[p]?pod.color:'?',unknown=code==='?';colour(card.node,code);card.node.classList.toggle('mystery',unknown);card.count.textContent=s.remaining[p];card.mark.textContent=palette[code].mark;card.mark.hidden=false;card.group.hidden=!pod.group;card.group.textContent=pod.group?`${pod.group} ↔`:'';card.node.dataset.group=pod.group||'';const reason=k===0?timeline.check({type:'tap',pod:p}):null;card.node.classList.toggle('pair-blocked',Boolean(reason));card.node.setAttribute('aria-label',`${palette[code].name}: ${s.remaining[p]} клеток${pod.group?', пара '+pod.group+', нужны два места':''}${k?', дальше в очереди':''}`);
    if(k===0){if(unknown)throw Error('Selectable top has not been revealed');card.image.hidden=false;if(card.image.dataset.hero!==palette[code].hero){card.image.src=assets[palette[code].hero];card.image.dataset.hero=palette[code].hero;}card.node.disabled=timeline.status!=='Working'&&state.status!=='Playing';card.node.setAttribute('aria-disabled',String(Boolean(reason)||card.node.disabled));}return card.node;});
   if(node.childNodes.length!==nodes.length||nodes.some((n,i)=>node.childNodes[i]!==n))node.replaceChildren(...nodes);const n=left.length-1;if(n){const css=getComputedStyle(node),height=topCards[left[0]].node.getBoundingClientRect().height,margin=parseFloat(getComputedStyle(backCards[left[1]].node).marginTop);node.style.setProperty('--peek-height',Math.max(15,Math.min(20,Math.floor((node.clientHeight-parseFloat(css.paddingTop)-height-n*margin)/n)))+'px');}
  });
  const ready=timeline.bridgeChoices().map(c=>c.choice);bridgeCards.forEach((b,i)=>{const used=state.bridge>=0,chosen=state.bridge===i;b.disabled=used||!ready.includes(i)||state.status!=='Playing';b.classList.toggle('chosen',chosen);b.textContent=chosen?'Мостик вырос ✓':used?'Второй недоступен':`Мостик${bridgeCards.length>1?i===0?' слева':' справа':''}${ready.includes(i)?' · готов':' · дойди до берега'}`;});
  $('statusLine').textContent=storageProblem||(timeline.pending?(free?'Команды работают. Можно выбрать следующую.':'Места заняты, команды ещё работают.'):state.status==='Won'?'Картинка собрана!':state.status==='Jammed'?'Работа остановилась, все места заняты.':state.status==='Stuck'?'Следующей паре не хватает мест.':`${attempt.capacity-free} мест занято. Выбери верхнюю капсулу.`);
  $('board').setAttribute('aria-label',`${level.title}, поле ${level.w} на ${level.h}, осталось ${s.board.reduce((n,c)=>n+(c>=0),0)} клеток. Вход снизу.`);draw();
 }
 function act(action){
  if(dialogs.some(d=>d.open)||document.hidden)return false;syncTime();const before=measure(),speedBefore=fast()?2:1,result=timeline.commit(action);
  attempt.inputEvents.push({atMs:attempt.elapsedMs,action:{...action},accepted:result.accepted,reason:result.reason||null,...before});
  if(!result.accepted){const messages={'visual-places-busy':'Плашки ещё заняты работой. Дождись пустого места.','partner-not-exposed':'Сначала открой напарника с такой же буквой.','pair-needs-two-slots':'Для пары нужны два свободных места.','no-slot':'Нет свободного места.','visible-bridge-not-ready':'Команды ещё добираются до берега.','finished':timeline.pending?'Команды заканчивают работу.':'Попытка закончилась.'};render();$('statusLine').textContent=messages[result.reason]||'Эта команда сейчас недоступна.';save();return false;}
  state=timeline.state;visual=timeline.shown;recordWorkEvents();attempt.commands.push({...action,modelTimeMs:timeline.now});attempt.updatedAt=new Date().toISOString();attempt.logicalOutcome=state.status.toLowerCase();event('commit',{action:{...action},command:attempt.commands.length});
  if(R.queueEmpty(state)&&attempt.autoFastAtAction===null){attempt.autoFastAtAction=attempt.commands.length;attempt.autoFastAtMs=attempt.elapsedMs;event('queue-exhausted',{effectiveSpeed:fast()?2:1});}
  if(speedBefore!==(fast()?2:1))event('effective-speed',{from:speedBefore,to:fast()?2:1,reason:'queue-exhausted'});
  save();render();finishAttempt();return true;
 }
 function finishAttempt(){
  if(!timeline||timeline.pending||state.status==='Playing')return;
  if(attempt.outcome==='playing'){syncTime();attempt.outcome=state.status.toLowerCase();attempt.endedAt=new Date().toISOString();attempt.workFinishedAtMs=attempt.elapsedMs;event('finished');setActivity('finished',false);save();}
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
    $('lessonTag').textContent=`L${id} · ${id===10?'ПЕРВЫЙ HARD':'НОВЫЙ ШАГ'}`;$('lessonTitle').textContent=lesson[0];$('lessonPortrait').src=assets[lesson[1]];$('lessonPortrait').alt=lesson[1];
    $('lessonSwatch').className='lesson-material';$('lessonSwatch').style.background=palette[lesson[2]].color;$('lessonSwatch').textContent=palette[lesson[2]].mark;$('lessonText').textContent=lesson[3];
    modal('lessonDialog');
  }
  function showEnd(){
    if(timeline.pending||$('endDialog').open)return;
    const won=state.status==='Won';$('endTag').textContent=`L${state.level.id} · ${won?'ПРОЙДЕН':'ПОПЫТКА'}`;$('endTitle').textContent=won?'Отлично получилось!':state.status==='Stuck'?'Паре не хватает места':'Места закончились';
    $('endDescription').textContent=won?`${state.level.title}. ${attempt.commands.filter(a=>a.type==='tap').length} тапов. ${state.level.id===30?'Все 30 доступны в выборе уровней. Расскажи, как ощущается развитие.':'Можно оценить уровень и перейти дальше.'}`:'Остатки ждут закрытые клетки: '+Array.from(state.slots).filter(p=>p>=0&&state.remaining[p]>0).map(p=>palette[state.level.pods[p].color].name+' '+state.remaining[p]).join(', ')+'. Оставь место для команды, которая откроет путь. Ретрай бесплатный.';
    $('finishedPicture').hidden=!won;if(won)drawPicture($('finishedPicture'),true);
    $('nextButton').hidden=!won;$('nextButton').textContent=state.level.id===30?'Посмотреть результаты':'Следующий уровень';
    $('levelNote').value=attempt.note||'';syncFeedback();modal('endDialog');save();
  }
  function syncFeedback(){for(const field of ['rating','interest','art','planning','pace'])document.querySelectorAll(`[data-${field}]`).forEach(b=>{const selected=b.dataset[field]===attempt[field];b.classList.toggle('selected',selected);b.setAttribute('aria-pressed',String(selected));});}
  function retry(){syncTime();save();if(attempt.commands.length&&state.status==='Playing')modal('retryDialog');else loadLevel(state.level.id,{restart:true,lesson:false});}
  function showLevels(){
    syncTime();save();closeAll();document.querySelectorAll('[data-palette]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.palette===store.settings.palette)));const grid=$('levelGrid');grid.replaceChildren();
    for(const level of data.levels){const button=document.createElement('button'),done=store.attempts.some(a=>a.level===level.id&&a.outcome==='won');button.type='button';button.className=`level-choice${level.difficulty==='Hard'?' hard':''}${done?' done':''}${level.id===state.level.id?' current':''}`;button.dataset.level=level.id;
      const strong=document.createElement('strong'),small=document.createElement('small');strong.textContent=`${level.id}${done?' ✓':''}`;small.textContent=level.difficulty==='Hard'?'Hard':`${level.w}×${level.h}`;button.append(strong,small);button.setAttribute('aria-label',`Уровень ${level.id}, ${level.title}, ${level.difficulty}${done?', пройден':''}`);button.addEventListener('click',()=>loadLevel(level.id));grid.append(button);
    }modal('levelsDialog');
  }
  function showResults(){
    closeAll();syncTime();save();const won=new Set(store.attempts.filter(a=>a.outcome==='won').map(a=>a.level));$('resultsSummary').textContent=`Пройдено ${won.size} из 30. Попыток: ${store.attempts.length}.`;
    $('resultsList').replaceChildren();
    for(const level of data.levels){const attempts=store.attempts.filter(a=>a.level===level.id),wins=attempts.filter(a=>a.outcome==='won'),last=attempts.at(-1),row=document.createElement('div');row.className='result-row';const label=document.createElement('strong'),details=document.createElement('div');label.textContent=`L${level.id}`;
      details.textContent=!attempts.length?'Ещё не пробовал':`${wins.length?'✓ Пройден':'В процессе'} · ${attempts.length} поп. · ${Math.round(attempts.reduce((sum,a)=>sum+a.elapsedMs,0)/1000)} сек.${last.rating?` · ${last.rating==='easy'?'легко':last.rating==='normal'?'нормально':'трудно'}`:''}${last.pace?' · темп: '+({fast:'быстро',okay:'хорошо',slow:'медленно'}[last.pace]):''}${last.note?' · '+last.note:''}`;
      row.append(label,details);$('resultsList').append(row);
    }$('versionLabel').textContent=`${data.version} · каталог ${data.hash.slice(0,12)} · правила ${data.rulesHash.slice(0,12)}`;modal('resultsDialog');
  }
  function exportResults(){
    syncTime();save();const report={format:'bloomlings-pilot-results-7',exportedAt:new Date().toISOString(),pilotVersion:data.version,catalogHash:data.hash,rulesHash:data.rulesHash,timelineHash:data.timelineHash,clockHash:data.clockHash,paletteHash:data.paletteHash,presentation:{trips:T.PROFILE,topReveal:true,releaseAfterReturn:false,releaseAfterLastDeparture:true,sameColourAllocation:'live-independent-cadence',workEventCodes:['depart','arrive','clear','return','release','assign'],tripPlanFields:['id','pod','cell','assignedMs','startMs','arriveMs','clearMs','returnMs','routeLength','physicalFrontier']},settings:store.settings,attempts:store.attempts};
    const blob=new Blob([JSON.stringify(report,null,2)+'\n'],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`Bloomlings-pilot-v7-results-${new Date().toISOString().slice(0,10)}.json`;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),3000);
  }
  function draw(){if(!state||drawing)return;drawing=requestAnimationFrame(()=>{drawing=null;drawPicture($('board'),false);});}
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
      if(code===R.WALL){ctx.fillStyle='#b6d6cc';ctx.fillRect(px,py,cell+.1,cell+.1);ctx.strokeStyle='#d6ece2';ctx.lineWidth=Math.max(.7,cell*.08);ctx.beginPath();ctx.moveTo(px+cell*.15,py+cell*.55);ctx.quadraticCurveTo(px+cell*.55,py+cell*.25,px+cell*.88,py+cell*.55);ctx.stroke();continue;}
      if(code===R.OPEN){
        ctx.fillStyle=original[index]>=0?palette[R.CHANNELS[original[index]]].color+'22':'#e0e8cf';ctx.fillRect(px+gap,py+gap,cell-gap*2,cell-gap*2);
        if(!finished&&s.bridge>=0&&index===l.hero.choices[s.bridge].cell){ctx.fillStyle='#8eac62';ctx.fillRect(px+gap,py,cell-gap*2,cell);ctx.strokeStyle='#5b7d46';ctx.lineWidth=Math.max(.7,cell*.07);for(let k=1;k<4;k++){ctx.beginPath();ctx.moveTo(px+gap,py+cell*k/4);ctx.lineTo(px+cell-gap,py+cell*k/4);ctx.stroke();}}
        continue;
      }
      const p=palette[R.CHANNELS[code]],a=cell-gap*2;
      ctx.fillStyle=p.dark;ctx.fillRect(px+gap,py+gap+depth,a,a-depth*.25);
      ctx.fillStyle=p.color;ctx.beginPath();ctx.roundRect(px+gap,py+gap,a,a-depth,Math.min(2,cell*.16));ctx.fill();
      ctx.fillStyle=p.light;ctx.fillRect(px+gap+cell*.13,py+gap+cell*.10,a-cell*.26,Math.max(.5,cell*.085));
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
  $('nextButton').addEventListener('click',()=>state.level.id===30?showResults():loadLevel(state.level.id+1));
  $('speedButton').addEventListener('click',()=>{syncTime();const from=fast()?2:1;store.settings.fast=!store.settings.fast;event('manual-speed',{manualFast:store.settings.fast,effectiveSpeed:fast()?2:1});if(from!==(fast()?2:1))event('effective-speed',{from,to:fast()?2:1,reason:'manual'});save();render();});
  $('symbolsButton').addEventListener('click',()=>{syncTime();store.settings.symbols=!symbols();event('symbols',{enabled:symbols()});save();render();});
  $('resultsButton').addEventListener('click',showResults);$('exportButton').addEventListener('click',exportResults);
  $('helpButton').addEventListener('click',()=>{closeAll();modal('welcomeDialog');});
  $('startButton').addEventListener('click',()=>{store.started=true;save();close('welcomeDialog');if(LESSONS[state.level.id]&&!store.lessons.includes(state.level.id))showLesson(state.level.id);});
  $('lessonButton').addEventListener('click',()=>{if(!store.lessons.includes(state.level.id))store.lessons.push(state.level.id);save();close('lessonDialog');});
  document.querySelectorAll('[data-close]').forEach(b=>b.addEventListener('click',()=>close(b.dataset.close)));
  document.querySelectorAll('[data-rating]').forEach(b=>b.addEventListener('click',()=>{attempt.rating=b.dataset.rating;syncFeedback();save();}));
  document.querySelectorAll('[data-interest]').forEach(b=>b.addEventListener('click',()=>{attempt.interest=b.dataset.interest;syncFeedback();save();}));
  document.querySelectorAll('[data-art]').forEach(b=>b.addEventListener('click',()=>{attempt.art=b.dataset.art;syncFeedback();save();}));
  document.querySelectorAll('[data-planning]').forEach(b=>b.addEventListener('click',()=>{attempt.planning=b.dataset.planning;syncFeedback();save();}));
  document.querySelectorAll('[data-pace]').forEach(b=>b.addEventListener('click',()=>{attempt.pace=b.dataset.pace;syncFeedback();save();}));
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
  document.querySelectorAll('[data-palette]').forEach(b=>b.addEventListener('click',()=>{syncTime();store.settings.palette=b.dataset.palette;event('palette',{palette:store.settings.palette});palette=window.BloomPilotPalette.palette(store.settings.palette);document.querySelectorAll('[data-palette]').forEach(n=>n.setAttribute('aria-pressed',String(n===b)));save();render();}));
  requestAnimationFrame(frame);
  // Read-only inspection: tests drive real DOM inputs and the browser clock.
  Object.defineProperty(window,'bloomlingsPilot',{value:Object.freeze({snapshot:()=>({level:state.level.id,capacity:attempt.capacity,status:state.status,displayStatus:timeline.status,signature:R.signature(state),visual:{board:Array.from(visual.board),remaining:Array.from(visual.remaining),slots:Array.from(visual.slots),heads:Array.from(visual.heads),revealed:Array.from(visual.revealed),bridge:visual.bridge},commands:attempt.commands.length,bridge:state.bridge,autoFast:R.queueEmpty(state),modelTimeMs:timeline.now,hasChoice:timeline.hasChoice,pendingTrips:timeline.jobs.size,pendingCells:timeline.pendingCells,pendingBeats:timeline.steps.length-timeline.cursor,phase:timeline.phase,physicalFree:timeline.free,elapsedMs:attempt.elapsedMs,timerRunning:activity.running,reservations:Array.from(timeline.reserved),inFlight:Array.from(timeline.inFlight),couriers:timeline.couriers,pendingTrips:timeline.jobs.size,palette:store.settings.palette,symbols:symbols()}),exportObject:()=>JSON.parse(JSON.stringify(store))})});
})();
