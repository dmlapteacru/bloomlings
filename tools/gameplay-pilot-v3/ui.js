/* V3: logical outcomes + a physical, resumable work timeline. */
(() => {
 'use strict';
 const $=id=>document.getElementById(id),R=window.BloomPilotRules,T=window.BloomPilotTimeline;
 const data=JSON.parse($('pilotData').textContent),assets=JSON.parse($('pilotAssets').textContent);
  const palette = {
    G:{color:'#91bd53',dark:'#658e3d',light:'#b2d77d',name:'Лист',mark:'♧',hero:'sprig'},
    P:{color:'#ef91a7',dark:'#c76681',light:'#ffc1cd',name:'Лепесток',mark:'✿',hero:'bloom'},
    B:{color:'#68bad0',dark:'#3b8fa6',light:'#a0ddec',name:'Вода',mark:'◆',hero:'drop'},
    A:{color:'#cd975a',dark:'#a16c38',light:'#eac18b',name:'Дерево',mark:'●',hero:'twig'},
    C:{color:'#f6e8bc',dark:'#d2be87',light:'#fff5da',name:'Светлый песок',mark:'◇',hero:'drop'},
    D:{color:'#354c45',dark:'#243830',light:'#627c68',name:'Тёмная земля',mark:'▰',hero:'twig'},
    '?':{color:'#d8d4c5',name:'Скрытый цвет',mark:'?'}
  };
  const LESSONS = {
    1:['Новый сад · итерация 3','sprig','G','Sprig собирает зелёные клетки, Drop — голубые. Тапай верхние капсулы: команда собирает только свой цвет от входа снизу. Начинаем с 3 мест. Можно отправить несколько команд сразу, пока есть пустые плашки. В выборе уровней можно попробовать тот же ранний уровень с 4 местами. Когда очередь закончится, автоматически включится ×2.'],
    3:['Знакомимся с Bloom','bloom','P','Bloom добавляет розовый цвет. Число на капсуле — количество клеток, которое она соберёт. Если путь к ним закрыт другими цветами, остаток займёт место и будет ждать.'],
    5:['В гости к Twig','twig','A','Появляется коричневый цвет Twig. Смотри и на поле, и на следующие капсулы: важен путь к работе, а не только совпадение цвета.'],
    6:['Что дальше в очереди?','drop','B','В глубине очереди встречаются закрытые капсулы. Когда такая капсула становится верхней, её цвет открывается автоматически — до твоего выбора. Содержимое при повторе не меняется.'],
    8:['Один герой, разные материалы','drop','C','Светлый песок — ещё один материал Drop. Кремовая команда собирает песок, голубая — воду. Совпадать должен точный цвет капсулы.'],
    10:['Вместе до конца','sprig','G','Одинаковая буква ↔ обозначает пару. Один тап отправляет обе капсулы, нужны 2 свободных места и обе капсулы наверху. Оба места остаются заняты, пока не закончат обе команды. Галочка означает: свою работу сделал, жду напарника. На уровнях с парами в этом тесте пять мест.'],
    15:['Первый Hard','twig','A','Здесь пары могут надолго занять места. Смотри, какие цвета откроют следующую работу, и сравнивай капсулы одного цвета по объёму и тому, что за ними. Простой выбор по цвету уже может завести в тупик. Ретрай свободный.'],
    20:['Ещё один материал','twig','D','Тёмная земля — второй материал Twig. Она не собирается коричневой командой. Разные цвета на капсулах обозначают разные задачи, даже когда герой один.'],
    18:['Действие Sprig: мостик','sprig','G','Ручей закрывает проход к рисунку. Доберись до отмеченного берега, затем нажми кнопку под полем: Sprig вырастит мостик. Он открывает путь, но сам не убирает клетки. Мостик может помочь даже при пяти занятых местах.'],
    19:['Один мостик, два места','sprig','G','За разными берегами доступны разные цвета. На уровень есть только один мостик: учитывай ожидающие команды и очередь. Подготовка команд и выбранный берег влияют на доступное продолжение.']
  };

 const KEY=`bloomlings-pilot-v3:${data.hash}:${data.rulesHash}:${data.timelineHash}`;
 const fresh=()=>({schema:3,catalogHash:data.hash,rulesHash:data.rulesHash,timelineHash:data.timelineHash,current:1,started:false,lessons:[],settings:{fast:false,symbols:false,earlyCapacity:3},attempts:[],activeByLevel:{},presentations:{},serial:0});
 let store=fresh(),storageProblem='';
 try{const saved=localStorage.getItem(KEY);if(saved){const s=JSON.parse(saved);if(s.schema!==3||!s.presentations||!Array.isArray(s.attempts)||![3,4].includes(s.settings?.earlyCapacity))throw Error('Invalid save');store=s;}}catch(error){storageProblem='Сохранение недоступно. Скачай результаты перед закрытием.';}
 let state,visual,timeline,attempt,clock=performance.now(),lastFrame=clock,lastCheckpoint=clock,drawing=null;
 let topCards=[],backCards=[],columns=[],slotCards=[],bridgeCards=[];
 const reduced=matchMedia('(prefers-reduced-motion: reduce)'),dialogs=Array.from(document.querySelectorAll('dialog'));
 const pictures={};for(const name of ['sprig','bloom','drop','twig']){const img=new Image();img.src=assets[name];img.onload=()=>draw();pictures[name]=img;}
 document.documentElement.style.setProperty('--garden',`url("${assets.garden}")`);
 function save(){if(timeline&&attempt)store.presentations[attempt.id]=timeline.snapshot();try{localStorage.setItem(KEY,JSON.stringify(store));}catch(error){storageProblem='Сохранение недоступно. Скачай результаты перед закрытием.';}}
 function active(){return !document.hidden&&!dialogs.some(d=>d.open);}
 function clockTick(allowHidden=false){const now=performance.now();if(attempt&&['Playing','Working'].includes(timeline?.status)&&(!document.hidden||allowHidden)&&!dialogs.some(d=>d.open))attempt.elapsedMs+=Math.max(0,now-clock);clock=now;}
 function syncTime(){const now=performance.now();clockTick();const changed=Boolean(timeline&&active()&&timeline.advance(Math.max(0,now-lastFrame),fast()?2:1));if(changed){state=timeline.state;visual=timeline.shown;}lastFrame=now;return changed;}
 function modal(id){syncTime();$(id).showModal();lastFrame=clock=performance.now();}
 function close(id){clockTick();$(id).close();lastFrame=clock=performance.now();}
 function closeAll(){clockTick();dialogs.forEach(d=>{if(d.open)d.close();});lastFrame=clock=performance.now();}
 function colour(element,code){const p=palette[code||'?'];element.style.setProperty('--color',p.color);element.style.setProperty('--text',code==='D'?'#fff7df':'#314332');element.style.setProperty('--text-shadow',code==='D'?'0 1px 1px #0003':'none');}
 const symbols=()=>Boolean(store.settings.symbols),fast=()=>store.settings.fast||R.queueEmpty(state);
 function configuration(level){return level.id<=9?store.settings.earlyCapacity:5;}
 function startAttempt(level,capacity){
  const a={id:++store.serial,level:level.id,capacity,config:{capacity,workMs:T.WORK_MS,visibility:'reveal-on-top'},startedAt:new Date().toISOString(),commands:[],inputEvents:[],telemetry:[],elapsedMs:0,outcome:'playing',logicalOutcome:'playing',rating:null,interest:null,art:null,planning:null,note:'',autoFastAtAction:null,autoFastAtMs:null,workFinishedAtMs:null};
  store.attempts.push(a);store.activeByLevel[`${level.id}:${capacity}`]=a.id;return a;
 }
 function replay(level,a){let s=R.create(level,a.capacity);for(const action of a.commands){const n=R.step(s,action);if(!n.accepted)throw Error('Invalid saved action');s=n.state;}return s;}
 function measure(){return {logicalHeld:state.slots.filter(p=>p>=0).length,visualHeld:timeline.shown.slots.filter(p=>p>=0).length,visualSlots:Array.from(timeline.shown.slots),visualCounts:Array.from(timeline.shown.slots,p=>p<0?null:timeline.shown.remaining[p]),pendingCells:timeline.pendingCells,pendingBeats:timeline.steps.length-timeline.cursor,sourceLeft:state.level.tray.reduce((n,s,c)=>n+s.length-state.heads[c],0)};}
 function event(type,extra={}){attempt.telemetry.push({type,atMs:attempt.elapsedMs,...extra,...measure()});}
 function loadLevel(id,{restart=false,lesson=true}={}){
  syncTime();save();closeAll();const level=data.levels.find(l=>l.id===id)||data.levels[0],capacity=configuration(level),key=`${level.id}:${capacity}`;
  const old=store.attempts.find(a=>a.id===store.activeByLevel[key]);if(restart&&old?.outcome==='playing'){old.outcome='abandoned';old.endedAt=new Date().toISOString();}
  attempt=(!restart&&old)||startAttempt(level,capacity);
  try{state=replay(level,attempt);}catch(error){storageProblem='Повтор не восстановился, начата новая попытка.';attempt=startAttempt(level,capacity);state=R.create(level,capacity);}
  try{timeline=new T.Timeline(state,store.presentations[attempt.id]);}catch(error){timeline=new T.Timeline(state);storageProblem='Ходы восстановлены; отображение синхронизировано с результатом.';}
  visual=timeline.shown;store.current=level.id;lastFrame=clock=performance.now();buildCards();render();save();
  if(timeline.status!=='Working'&&state.status!=='Playing')finishAttempt();else if(lesson&&store.started&&LESSONS[level.id]&&!store.lessons.includes(level.id))showLesson(level.id);
 }
 function buildCards(){
  $('tray').replaceChildren();$('slots').replaceChildren();$('bridgeButtons').replaceChildren();$('tray').style.setProperty('--columns',state.level.tray.length);$('slots').style.setProperty('--places',attempt.capacity);
  columns=state.level.tray.map((_,c)=>{const node=document.createElement('div');node.className='column';node.setAttribute('aria-label',`Столбец ${c+1}`);$('tray').append(node);return node;});
  topCards=state.level.pods.map((pod,p)=>{const node=document.createElement('button');node.type='button';node.className='pod-top';node.dataset.pod=p;const image=document.createElement('img');image.alt='';image.draggable=false;const count=document.createElement('strong'),mark=document.createElement('span'),group=document.createElement('span');mark.className='channel-mark';group.className='pair-tag';node.append(image,count,mark,group);node.addEventListener('click',()=>act({type:'tap',pod:p}));return {node,image,count,mark,group};});
  backCards=state.level.pods.map((pod,p)=>{const node=document.createElement('div');node.className='pod-behind';node.dataset.futurePod=p;const mark=document.createElement('span'),count=document.createElement('strong'),group=document.createElement('span');group.className='pair-tag';node.append(mark,group,count);return {node,mark,count,group};});
  slotCards=Array.from({length:attempt.capacity},(_,i)=>{const node=document.createElement('div');node.className='slot empty';node.setAttribute('role','img');const image=document.createElement('img');image.alt='';const count=document.createElement('strong'),wait=document.createElement('span'),group=document.createElement('span');wait.className='slot-wait';group.className='pair-tag';node.append(image,count,wait,group);$('slots').append(node);return {node,image,count,wait,group};});
  $('heroBar').hidden=!state.level.hero;if(state.level.hero){$('heroActionPortrait').src=assets.sprig;bridgeCards=state.level.hero.choices.map((choice,i)=>{const button=document.createElement('button');button.type='button';button.dataset.bridge=i;button.addEventListener('click',()=>act({type:'bridge',choice:i}));$('bridgeButtons').append(button);return button;});}else bridgeCards=[];
 }
 function render(){
  if(!state)return;const s=timeline.shown,level=s.level,free=timeline.free,workers=timeline.workers;
  $('levelTitle').textContent=level.title;$('levelShort').textContent=`L${level.id}`;$('difficulty').textContent=level.difficulty;$('difficulty').className=level.difficulty==='Hard'?'hard':'';
  $('boardSize').textContent=`${level.w}×${level.h}`;$('workLabel').textContent=`Осталось ${s.board.reduce((n,c)=>n+(c>=0),0)}`;$('slotsLabel').textContent=`${free} свободно из ${attempt.capacity}`;$('movesLabel').textContent=`${attempt.commands.filter(a=>a.type==='tap').length} тапов`;
  $('speedButton').textContent=fast()?'2×':'1×';$('speedButton').dataset.auto=String(R.queueEmpty(state));$('speedButton').setAttribute('aria-pressed',String(fast()));$('speedButton').setAttribute('aria-label',R.queueEmpty(state)?'Авто ×2: очередь закончилась':'Переключить скорость работы');$('symbolsButton').setAttribute('aria-pressed',String(symbols()));
  slotCards.forEach(({node,image,count,wait,group},i)=>{const p=s.slots[i],filled=p>=0;node.className=`slot ${filled?'filled':'empty'}`;image.hidden=count.hidden=wait.hidden=!filled;group.hidden=!filled||!level.pods[p]?.group;
   if(filled){const code=level.pods[p].color,ready=s.remaining[p]===0,paired=Boolean(level.pods[p].group),working=workers.has(p);colour(node,code);if(image.dataset.hero!==palette[code].hero){image.src=assets[palette[code].hero];image.dataset.hero=palette[code].hero;}count.textContent=ready?'✓':s.remaining[p];wait.textContent=ready?(paired?'ждёт пару':'готово'):working?'собирает':'ждёт';group.textContent=level.pods[p].group||'';node.classList.toggle('pair-ready',ready&&paired);node.classList.toggle('working',working&&!reduced.matches);node.dataset.slotPod=p;node.setAttribute('aria-label',`Место ${i+1}: ${palette[code].name}, ${ready?(paired?'готово, ждёт напарника':'готово'):'осталось '+s.remaining[p]}`);}else{delete node.dataset.slotPod;node.setAttribute('aria-label',`Место ${i+1}: свободно`);}
  });
  level.tray.forEach((stack,c)=>{const left=stack.slice(s.heads[c]),node=columns[c];if(!left.length){node.replaceChildren();node.className='column empty';node.textContent='Пусто';return;}node.className='column';const nodes=left.map((p,k)=>{const card=k===0?topCards[p]:backCards[p],pod=level.pods[p],code=s.revealed[p]?pod.color:'?',unknown=code==='?';colour(card.node,code);card.node.classList.toggle('mystery',unknown);card.count.textContent=s.remaining[p];card.mark.textContent=palette[code].mark;card.mark.hidden=!unknown&&!symbols();card.group.hidden=!pod.group;card.group.textContent=pod.group?`${pod.group} ↔`:'';card.node.dataset.group=pod.group||'';const reason=k===0?timeline.check({type:'tap',pod:p}):null;card.node.classList.toggle('pair-blocked',Boolean(reason));card.node.setAttribute('aria-label',`${palette[code].name}: ${s.remaining[p]} клеток${pod.group?', пара '+pod.group+', нужны два места':''}${k?', дальше в очереди':''}`);
    if(k===0){if(unknown)throw Error('Selectable top has not been revealed');card.image.hidden=false;if(card.image.dataset.hero!==palette[code].hero){card.image.src=assets[palette[code].hero];card.image.dataset.hero=palette[code].hero;}card.node.disabled=timeline.status!=='Working'&&state.status!=='Playing';card.node.setAttribute('aria-disabled',String(Boolean(reason)||card.node.disabled));}return card.node;});
   if(node.childNodes.length!==nodes.length||nodes.some((n,i)=>node.childNodes[i]!==n))node.replaceChildren(...nodes);const n=left.length-1;if(n){const css=getComputedStyle(node),height=topCards[left[0]].node.getBoundingClientRect().height,margin=parseFloat(getComputedStyle(backCards[left[1]].node).marginTop);node.style.setProperty('--peek-height',Math.max(12,Math.min(20,Math.floor((node.clientHeight-parseFloat(css.paddingTop)-height-n*margin)/n)))+'px');}
  });
  const ready=timeline.bridgeChoices().map(c=>c.choice);bridgeCards.forEach((b,i)=>{const used=state.bridge>=0,chosen=state.bridge===i;b.disabled=used||!ready.includes(i)||state.status!=='Playing';b.classList.toggle('chosen',chosen);b.textContent=chosen?'Мостик вырос ✓':used?'Второй недоступен':`Мостик${bridgeCards.length>1?i===0?' слева':' справа':''}${ready.includes(i)?' · готов':' · дойди до берега'}`;});
  $('statusLine').textContent=storageProblem||(timeline.pending?(free?'Команды работают. Можно выбрать следующую.':'Места заняты, команды ещё работают.'):state.status==='Won'?'Картинка собрана!':state.status==='Jammed'?'Работа остановилась, все места заняты.':state.status==='Stuck'?'Следующей паре не хватает мест.':`${attempt.capacity-free} мест занято. Выбери верхнюю капсулу.`);
  $('board').setAttribute('aria-label',`${level.title}, поле ${level.w} на ${level.h}, осталось ${s.board.reduce((n,c)=>n+(c>=0),0)} клеток. Вход снизу.`);draw();
 }
 function act(action){
  if(dialogs.some(d=>d.open))return false;syncTime();const before=measure(),result=timeline.commit(action);
  attempt.inputEvents.push({atMs:attempt.elapsedMs,action:{...action},accepted:result.accepted,reason:result.reason||null,...before});
  if(!result.accepted){const messages={'visual-places-busy':'Плашки ещё заняты работой. Дождись пустого места.','partner-not-exposed':'Сначала открой напарника с такой же буквой.','pair-needs-two-slots':'Для пары нужны два свободных места.','no-slot':'Нет свободного места.','visible-bridge-not-ready':'Команды ещё добираются до берега.','finished':timeline.pending?'Команды заканчивают работу.':'Попытка закончилась.'};render();$('statusLine').textContent=messages[result.reason]||'Эта команда сейчас недоступна.';save();return false;}
  state=timeline.state;visual=timeline.shown;attempt.commands.push({...action});attempt.updatedAt=new Date().toISOString();attempt.logicalOutcome=state.status.toLowerCase();event('commit',{action:{...action},command:attempt.commands.length});
  if(R.queueEmpty(state)&&attempt.autoFastAtAction===null){attempt.autoFastAtAction=attempt.commands.length;attempt.autoFastAtMs=attempt.elapsedMs;event('auto-fast');}
  save();render();finishAttempt();return true;
 }
 function finishAttempt(){
  if(!timeline||timeline.pending||state.status==='Playing')return;
  if(attempt.outcome==='playing'){clockTick();attempt.outcome=state.status.toLowerCase();attempt.endedAt=new Date().toISOString();attempt.workFinishedAtMs=attempt.elapsedMs;event('finished');save();}
  if(!dialogs.some(d=>d.open))showEnd();
 }
 function frame(now){
  const before=timeline?.free,wasPending=timeline?.pending,changed=syncTime();
  if(timeline&&wasPending&&active()){if(before!==timeline.free)event('places-released');if(changed)render();if(!timeline.pending){event('work-idle');finishAttempt();save();}else if(now-lastCheckpoint>750){save();lastCheckpoint=now;}}
  if(timeline&&!timeline.pending&&state.status!=='Playing'&&attempt.outcome==='playing')finishAttempt();
  requestAnimationFrame(frame);
 }
  function showLesson(id){
    const lesson=LESSONS[id];if(!lesson)return;
    $('lessonTag').textContent=`L${id} · ${id===15?'ПЕРВЫЙ HARD':'НОВЫЙ ШАГ'}`;$('lessonTitle').textContent=lesson[0];$('lessonPortrait').src=assets[lesson[1]];$('lessonPortrait').alt=lesson[1];
    $('lessonSwatch').className='lesson-material';$('lessonSwatch').style.background=palette[lesson[2]].color;$('lessonSwatch').textContent=palette[lesson[2]].mark;$('lessonText').textContent=lesson[3];
    modal('lessonDialog');
  }
  function showEnd(){
    if(timeline.pending||$('endDialog').open)return;
    const won=state.status==='Won';$('endTag').textContent=`L${state.level.id} · ${won?'ПРОЙДЕН':'ПОПЫТКА'}`;$('endTitle').textContent=won?'Отлично получилось!':state.status==='Stuck'?'Паре не хватает места':'Места закончились';
    $('endDescription').textContent=won?`${state.level.title}. ${attempt.commands.filter(a=>a.type==='tap').length} тапов. ${state.level.id===30?'Все 30 доступны в выборе уровней. Расскажи, как ощущается развитие.':'Можно оценить уровень и перейти дальше.'}`:'Команды ждут клетки, к которым пока не добраться. Попробуй другой порядок капсул. Ретрай ничего не стоит.';
    $('finishedPicture').hidden=!won;if(won)drawPicture($('finishedPicture'),true);
    $('nextButton').hidden=!won;$('nextButton').textContent=state.level.id===30?'Посмотреть результаты':'Следующий уровень';
    $('levelNote').value=attempt.note||'';syncFeedback();modal('endDialog');save();
  }
  function syncFeedback(){for(const field of ['rating','interest','art','planning'])document.querySelectorAll(`[data-${field}]`).forEach(b=>{const selected=b.dataset[field]===attempt[field];b.classList.toggle('selected',selected);b.setAttribute('aria-pressed',String(selected));});}
  function retry(){syncTime();save();if(attempt.commands.length&&state.status==='Playing')modal('retryDialog');else loadLevel(state.level.id,{restart:true,lesson:false});}
  function showLevels(){
    syncTime();save();closeAll();document.querySelectorAll('[data-capacity]').forEach(b=>b.setAttribute('aria-pressed',String(+b.dataset.capacity===store.settings.earlyCapacity)));const grid=$('levelGrid');grid.replaceChildren();
    for(const level of data.levels){const button=document.createElement('button'),done=store.attempts.some(a=>a.level===level.id&&a.outcome==='won');button.type='button';button.className=`level-choice${level.difficulty==='Hard'?' hard':''}${done?' done':''}${level.id===state.level.id?' current':''}`;button.dataset.level=level.id;
      const strong=document.createElement('strong'),small=document.createElement('small');strong.textContent=`${level.id}${done?' ✓':''}`;small.textContent=level.difficulty==='Hard'?'Hard':`${level.w}×${level.h}`;button.append(strong,small);button.setAttribute('aria-label',`Уровень ${level.id}, ${level.title}, ${level.difficulty}${done?', пройден':''}`);button.addEventListener('click',()=>loadLevel(level.id));grid.append(button);
    }modal('levelsDialog');
  }
  function showResults(){
    closeAll();clockTick();save();const won=new Set(store.attempts.filter(a=>a.outcome==='won').map(a=>a.level));$('resultsSummary').textContent=`Пройдено ${won.size} из 30. Попыток: ${store.attempts.length}.`;
    $('resultsList').replaceChildren();
    for(const level of data.levels){const attempts=store.attempts.filter(a=>a.level===level.id),wins=attempts.filter(a=>a.outcome==='won'),last=attempts.at(-1),row=document.createElement('div');row.className='result-row';const label=document.createElement('strong'),details=document.createElement('div');label.textContent=`L${level.id}`;
      details.textContent=!attempts.length?'Ещё не пробовал':`${wins.length?'✓ Пройден':'В процессе'} · ${attempts.length} поп. · ${Math.round(attempts.reduce((sum,a)=>sum+a.elapsedMs,0)/1000)} сек.${last.rating?` · ${last.rating==='easy'?'легко':last.rating==='normal'?'нормально':'трудно'}`:''}${last.note?' · '+last.note:''}`;
      row.append(label,details);$('resultsList').append(row);
    }$('versionLabel').textContent=`${data.version} · каталог ${data.hash.slice(0,12)} · правила ${data.rulesHash.slice(0,12)}`;modal('resultsDialog');
  }
  function exportResults(){
    clockTick();save();const report={format:'bloomlings-pilot-results-3',exportedAt:new Date().toISOString(),pilotVersion:data.version,catalogHash:data.hash,rulesHash:data.rulesHash,timelineHash:data.timelineHash,presentation:{workMs:T.WORK_MS,topReveal:true},settings:store.settings,attempts:store.attempts};
    const blob=new Blob([JSON.stringify(report,null,2)+'\n'],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`Bloomlings-pilot-v3-results-${new Date().toISOString().slice(0,10)}.json`;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),3000);
  }
  function draw(){if(!state||drawing)return;drawing=requestAnimationFrame(()=>{drawing=null;drawPicture($('board'),false);});}
  function drawPicture(canvas,finished){
    const s=visual||state,l=s.level,rect=canvas.getBoundingClientRect();
    const width=finished?280:Math.max(1,rect.width),height=finished?280:Math.max(1,rect.height),scale=finished?1:Math.min(2,window.devicePixelRatio||1);
    if(canvas.width!==Math.round(width*scale)||canvas.height!==Math.round(height*scale)){canvas.width=Math.round(width*scale);canvas.height=Math.round(height*scale);}
    const ctx=canvas.getContext('2d');ctx.setTransform(scale,0,0,scale,0,0);ctx.clearRect(0,0,width,height);
    const cell=Math.min((width-8)/l.w,(height-10)/l.h),ox=(width-l.w*cell)/2,oy=(height-l.h*cell)/2,depth=Math.max(.8,cell*.14),gap=Math.max(.35,cell*.035);
    if(!finished)$('entryMarker').style.left=`${ox+((l.entries[0]%l.w)+.5)*cell}px`;
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
      if(symbols()&&!finished&&cell>=8){ctx.fillStyle=R.CHANNELS[code]==='D'?'#d5e4cd':'#344d385e';ctx.font=`900 ${Math.max(5,cell*.44)}px Nunito, sans-serif`;ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillText(p.mark,px+cell*.5,py+cell*.49);}
    }
    if(!finished&&l.hero&&s.bridge<0){
      const ready=timeline.bridgeChoices().map(c=>c.choice);
      l.hero.choices.forEach((choice,i)=>{const x=choice.anchor%l.w,y=Math.floor(choice.anchor/l.w),px=ox+(x+.5)*cell,py=oy+(l.h-y-.5)*cell;ctx.beginPath();ctx.arc(px,py,Math.max(2,cell*.34),0,Math.PI*2);ctx.fillStyle=ready.includes(i)?'#f3e4a7':'#fff7db99';ctx.fill();ctx.strokeStyle='#5b7d46';ctx.lineWidth=Math.max(1,cell*.08);ctx.stroke();});
    }
  }
  $('levelsButton').addEventListener('click',showLevels);
  $('retryButton').addEventListener('click',retry);
  $('confirmRetryButton').addEventListener('click',()=>loadLevel(state.level.id,{restart:true,lesson:false}));
  $('endRetryButton').addEventListener('click',()=>loadLevel(state.level.id,{restart:true,lesson:false}));
  $('endMapButton').addEventListener('click',showLevels);
  $('nextButton').addEventListener('click',()=>state.level.id===30?showResults():loadLevel(state.level.id+1));
  $('speedButton').addEventListener('click',()=>{syncTime();store.settings.fast=!store.settings.fast;event('manual-speed',{fast:store.settings.fast});save();render();});
  $('symbolsButton').addEventListener('click',()=>{store.settings.symbols=!symbols();save();render();});
  $('resultsButton').addEventListener('click',showResults);$('exportButton').addEventListener('click',exportResults);
  $('helpButton').addEventListener('click',()=>{closeAll();modal('welcomeDialog');});
  $('startButton').addEventListener('click',()=>{store.started=true;save();close('welcomeDialog');if(LESSONS[state.level.id]&&!store.lessons.includes(state.level.id))showLesson(state.level.id);});
  $('lessonButton').addEventListener('click',()=>{if(!store.lessons.includes(state.level.id))store.lessons.push(state.level.id);save();close('lessonDialog');});
  document.querySelectorAll('[data-close]').forEach(b=>b.addEventListener('click',()=>close(b.dataset.close)));
  document.querySelectorAll('[data-rating]').forEach(b=>b.addEventListener('click',()=>{attempt.rating=b.dataset.rating;syncFeedback();save();}));
  document.querySelectorAll('[data-interest]').forEach(b=>b.addEventListener('click',()=>{attempt.interest=b.dataset.interest;syncFeedback();save();}));
  document.querySelectorAll('[data-art]').forEach(b=>b.addEventListener('click',()=>{attempt.art=b.dataset.art;syncFeedback();save();}));
  document.querySelectorAll('[data-planning]').forEach(b=>b.addEventListener('click',()=>{attempt.planning=b.dataset.planning;syncFeedback();save();}));
  $('levelNote').addEventListener('input',()=>{attempt.note=$('levelNote').value;save();});
  dialogs.forEach(d=>{d.addEventListener('cancel',()=>{clockTick();clock=performance.now();});d.addEventListener('close',()=>{clock=performance.now();});});
  document.addEventListener('visibilitychange',()=>{clockTick(true);save();lastFrame=clock=performance.now();});
  window.addEventListener('pagehide',()=>{syncTime();save();});
  window.addEventListener('resize',()=>{render();});new ResizeObserver(draw).observe($('boardFrame'));
  setInterval(()=>{clockTick();save();},15000);
  for(const name of ['sprig','bloom','drop','twig']){const img=document.createElement('img');img.src=assets[name];img.alt=name;img.width=90;img.height=110;$('welcomeHeroes').append(img);}
  loadLevel(store.current,{lesson:false});
  if(!store.started){closeAll();modal('welcomeDialog');}
  document.querySelectorAll('[data-capacity]').forEach(b=>b.addEventListener('click',()=>{syncTime();save();store.settings.earlyCapacity=+b.dataset.capacity;save();loadLevel(state.level.id,{lesson:false});}));
  requestAnimationFrame(frame);
  // Read-only inspection: tests drive real DOM inputs and the browser clock.
  Object.defineProperty(window,'bloomlingsPilot',{value:Object.freeze({snapshot:()=>({level:state.level.id,capacity:attempt.capacity,status:state.status,displayStatus:timeline.status,signature:R.signature(state),visual:{board:Array.from(visual.board),remaining:Array.from(visual.remaining),slots:Array.from(visual.slots),heads:Array.from(visual.heads),revealed:Array.from(visual.revealed),bridge:visual.bridge},commands:attempt.commands.length,bridge:state.bridge,autoFast:R.queueEmpty(state),pendingCells:timeline.pendingCells,pendingBeats:timeline.steps.length-timeline.cursor,phase:timeline.phase,physicalFree:timeline.free}),exportObject:()=>JSON.parse(JSON.stringify(store))})});
})();
