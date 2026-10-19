/* Standalone test UI. Only rules.cjs changes the logical board. */
(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const R = window.BloomPilotRules;
  const data = JSON.parse($('pilotData').textContent);
  const assets = JSON.parse($('pilotAssets').textContent);
  const palette = {
    G:{color:'#91bd53',dark:'#658e3d',light:'#b2d77d',name:'Лист',mark:'♧',hero:'sprig'},
    P:{color:'#ef91a7',dark:'#c76681',light:'#ffc1cd',name:'Лепесток',mark:'✿',hero:'bloom'},
    B:{color:'#68bad0',dark:'#3b8fa6',light:'#a0ddec',name:'Вода',mark:'◆',hero:'drop'},
    A:{color:'#cd975a',dark:'#a16c38',light:'#eac18b',name:'Дерево',mark:'●',hero:'twig'},
    C:{color:'#f6e8bc',dark:'#d2be87',light:'#fff5da',name:'Светлый песок',mark:'◇',hero:'drop'},
    D:{color:'#354c45',dark:'#243830',light:'#627c68',name:'Тёмная земля',mark:'▰',hero:'twig'}
  };
  const LESSONS = {
    1:['Первые ростки','sprig','G','Sprig собирает зелёные клетки, Drop — голубые. Тапни верхнюю капсулу: команда займет одно из пяти мест и соберёт свой материал от входа снизу. Соседние клетки открываются постепенно.'],
    4:['Знакомимся с Bloom','bloom','P','Появляются розовые лепестки. У каждой капсулы свой материал и количество работы. Если часть клеток закрыта другими цветами, команда подождёт в одном из пяти мест.'],
    7:['В гости к Twig','twig','A','Теперь в рисунке есть дерево. Смотри и на поле, и на следующие капсулы в столбцах: иногда нужно освободить путь к нужной команде. Нажимать можно только верхние.'],
    12:['Один герой, разные материалы','drop','C','У Drop появляется светлый песок — отдельный материал. Голубая команда собирает воду, кремовая — песок. Один герой может работать с несколькими цветами, но капсула всегда собирает только свой точный цвет.'],
    16:['Первый Hard','twig','A','Теперь важны количество работы и порядок капсул. Большой остаток может занять место надолго. Смотри, какие команды понадобятся дальше. Если все пять мест заняты, попробуй другой порядок. Ретрай свободный.'],
    20:['Ещё один материал','twig','D','Тёмная земля — второй материал Twig. Она не собирается коричневой командой. Разные цвета на капсулах обозначают разные задачи, даже когда герой один.'],
    25:['Действие Sprig: мостик','sprig','G','Грядку пересекает ручей. Когда доберёшься до отмеченного берега, Sprig сможет вырастить один мостик. Нажми кнопку под полем. Это открывает путь, но не убирает клетки и не освобождает места само по себе.'],
    28:['Один мостик, два места','sprig','G','У Sprig по-прежнему одно действие на уровень. Теперь для мостика есть два берега. Выбери, где открыть путь, учитывая рисунок и очередь капсул. Второй мостик в этой попытке построить нельзя.']
  };
  const KEY = `bloomlings-pilot:${data.hash}:${data.rulesHash}`;
  const fresh = () => ({schema:1,catalogHash:data.hash,rulesHash:data.rulesHash,current:1,started:false,lessons:[],settings:{fast:false,symbols:null},attempts:[],activeByLevel:{},serial:0});
  let store=fresh(), storageProblem='';
  try {
    const saved=localStorage.getItem(KEY);
    if(saved){const value=JSON.parse(saved);if(value.schema!==1||!Array.isArray(value.attempts)||!value.activeByLevel||!value.settings||!Array.isArray(value.lessons))throw new Error('Неверный формат сохранения.');store=value;}
  } catch(error){storageProblem='Браузер не смог прочитать сохранение. Эта попытка начнётся заново.';}
  let state,visual,attempt,busy=false,animation=null,raf=0,clock=performance.now(),drawing=null;
  let topCards=[],backCards=[],columns=[],slotCards=[],bridgeCards=[];
  const reduced=matchMedia('(prefers-reduced-motion: reduce)');
  const pictures={};
  for(const name of ['sprig','bloom','drop','twig']){const img=new Image();img.src=assets[name];img.onload=()=>draw();pictures[name]=img;}
  document.documentElement.style.setProperty('--garden',`url("${assets.garden}")`);
  function save(){try{localStorage.setItem(KEY,JSON.stringify(store));}catch(error){storageProblem='Сохранение в этом браузере недоступно. Скачай результаты перед закрытием.';}}
  const dialogs=Array.from(document.querySelectorAll('dialog'));
  function clockTick(allowHidden=false){const now=performance.now();if(attempt&&state?.status==='Playing'&&(!document.hidden||allowHidden)&&!dialogs.some(d=>d.open))attempt.elapsedMs+=Math.max(0,now-clock);clock=now;}
  function modal(id){clockTick();$(id).showModal();clock=performance.now();}
  function close(id){clockTick();$(id).close();clock=performance.now();}
  function closeAll(){clockTick();dialogs.forEach(d=>{if(d.open)d.close();});clock=performance.now();}
  function colour(element,code){const p=palette[code];element.style.setProperty('--color',p.color);element.style.setProperty('--text',code==='D'?'#fff7df':'#314332');element.style.setProperty('--text-shadow',code==='D'?'0 1px 1px #0003':'none');}
  const symbols = () => store.settings.symbols ?? state.level.id<=7;
  function startAttempt(level){
    const item={id:++store.serial,level:level.id,startedAt:new Date().toISOString(),commands:[],elapsedMs:0,outcome:'playing',rating:null,art:null,note:''};
    store.attempts.push(item);store.activeByLevel[level.id]=item.id;return item;
  }
  function replay(level,record){let next=R.create(level);for(const action of record.commands){const move=R.step(next,action);if(!move.accepted)throw new Error('Invalid saved action.');next=move.state;}return next;}
  function loadLevel(id,{restart=false,lesson=true}={}){
    finishAnimation();clockTick();closeAll();
    const level=data.levels.find(l=>l.id===id)||data.levels[0];
    const old=store.attempts.find(a=>a.id===store.activeByLevel[level.id]);
    if(restart&&old?.outcome==='playing'){old.outcome='abandoned';old.endedAt=new Date().toISOString();}
    attempt=(!restart&&old)||startAttempt(level);
    try{state=replay(level,attempt);}catch(error){storageProblem='Не удалось восстановить эту попытку. Начата новая.';attempt=startAttempt(level);state=R.create(level);}
    visual=state;store.current=level.id;buildCards();render();save();clock=performance.now();
    if(state.status!=='Playing')showEnd();
    else if(lesson&&store.started&&LESSONS[level.id]&&!store.lessons.includes(level.id))showLesson(level.id);
  }
  function buildCards(){
    $('tray').replaceChildren();$('slots').replaceChildren();$('bridgeButtons').replaceChildren();
    $('tray').style.setProperty('--columns',state.level.tray.length);
    columns=state.level.tray.map((_,c)=>{const node=document.createElement('div');node.className='column';node.setAttribute('aria-label',`Столбец ${c+1}`);$('tray').append(node);return node;});
    topCards=state.level.pods.map((pod,p)=>{
      const node=document.createElement('button');node.type='button';node.className='pod-top';node.dataset.pod=p;colour(node,pod.color);
      const image=document.createElement('img');image.src=assets[palette[pod.color].hero];image.alt='';image.draggable=false;
      const count=document.createElement('strong'),mark=document.createElement('span');mark.className='channel-mark';mark.textContent=palette[pod.color].mark;
      node.append(image,count,mark);node.setAttribute('aria-label',`${palette[pod.color].name}: ${pod.count} клеток`);node.addEventListener('click',()=>act({type:'tap',pod:p}));
      return {node,count,mark};
    });
    backCards=state.level.pods.map((pod,p)=>{
      const node=document.createElement('div');node.className='pod-behind';node.dataset.futurePod=p;colour(node,pod.color);
      const mark=document.createElement('span'),count=document.createElement('strong');mark.textContent=palette[pod.color].mark;count.textContent=pod.count;node.append(mark,count);
      node.setAttribute('aria-label',`${palette[pod.color].name}: ${pod.count}, дальше в очереди`);return {node,mark};
    });
    slotCards=Array.from({length:5},(_,i)=>{
      const node=document.createElement('div');node.className='slot empty';node.setAttribute('role','img');
      const image=document.createElement('img');image.alt='';const count=document.createElement('strong'),wait=document.createElement('span');wait.className='slot-wait';wait.textContent='ждёт';
      node.append(image,count,wait);$('slots').append(node);return {node,image,count,wait};
    });
    $('heroBar').hidden=!state.level.hero;
    if(state.level.hero){$('heroActionPortrait').src=assets.sprig;bridgeCards=state.level.hero.choices.map((choice,i)=>{
      const button=document.createElement('button');button.type='button';button.dataset.bridge=i;button.addEventListener('click',()=>act({type:'bridge',choice:i}));$('bridgeButtons').append(button);return button;
    });}else bridgeCards=[];
  }
  function render(){
    if(!state)return;
    const s=visual||state,level=s.level,free=Array.from(s.slots).filter(p=>p<0).length;
    $('game').classList.toggle('game-busy',busy);
    $('levelTitle').textContent=level.title;$('levelShort').textContent=`L${level.id}`;
    $('difficulty').textContent=level.difficulty==='Hard'?'Hard':'Normal';$('difficulty').className=level.difficulty==='Hard'?'hard':'';
    $('boardSize').textContent=`${level.w}×${level.h}`;
    $('workLabel').textContent=`Осталось ${s.board.reduce((n,c)=>n+(c>=0),0)}`;
    $('slotsLabel').textContent=`${free} свободно из 5`;
    $('movesLabel').textContent=`${attempt.commands.filter(a=>a.type==='tap').length} тапов`;
    $('speedButton').textContent=store.settings.fast?'2×':'1×';$('speedButton').setAttribute('aria-pressed',String(store.settings.fast));
    $('symbolsButton').setAttribute('aria-pressed',String(symbols()));
    slotCards.forEach(({node,image,count,wait},i)=>{
      const p=s.slots[i],filled=p>=0;node.className=`slot ${filled?'filled':'empty'}`;image.hidden=count.hidden=wait.hidden=!filled;
      if(filled){const code=level.pods[p].color;colour(node,code);if(image.dataset.hero!==palette[code].hero){image.src=assets[palette[code].hero];image.dataset.hero=palette[code].hero;}count.textContent=s.remaining[p];node.setAttribute('aria-label',`Место ${i+1}: ${palette[code].name}, осталось ${s.remaining[p]}`);}
      else node.setAttribute('aria-label',`Место ${i+1}: свободно`);
    });
    level.tray.forEach((stack,c)=>{
      const remaining=stack.slice(s.heads[c]),node=columns[c];
      if(!remaining.length){node.replaceChildren();node.className='column empty';node.textContent='Пусто';return;}
      node.className='column';
      const nodes=remaining.map((p,k)=>{const card=k===0?topCards[p]:backCards[p];if(k===0){card.count.textContent=s.remaining[p];card.mark.hidden=!symbols();card.node.disabled=busy||state.status!=='Playing'||free===0;}else card.mark.hidden=!symbols();return card.node;});
      if(node.childNodes.length!==nodes.length||nodes.some((n,i)=>node.childNodes[i]!==n))node.replaceChildren(...nodes);
      const futureCount=remaining.length-1;
      if(futureCount){const css=getComputedStyle(node),topHeight=topCards[remaining[0]].node.getBoundingClientRect().height,margin=parseFloat(getComputedStyle(backCards[remaining[1]].node).marginTop);const peek=Math.min(20,Math.floor((node.clientHeight-parseFloat(css.paddingTop)-topHeight-futureCount*margin)/futureCount));node.style.setProperty('--peek-height',Math.max(12,peek)+'px');}
    });
    const ready=R.bridgeChoices(state).map(b=>b.choice);
    bridgeCards.forEach((button,i)=>{
      const chosen=state.bridge===i,used=state.bridge>=0;
      button.disabled=busy||used||!ready.includes(i)||state.status!=='Playing';button.classList.toggle('chosen',chosen);
      button.textContent=chosen?'Мостик вырос ✓':used?'Второй недоступен':`Мостик${bridgeCards.length>1?i===0?' слева':' справа':''}${ready.includes(i)?' · готов':' · дойди до берега'}`;
      button.setAttribute('aria-label',button.textContent);
    });
    $('statusLine').textContent=storageProblem|| (busy?'Команды собирают клетки…':state.status==='Won'?'Картинка собрана!':state.status==='Jammed'?'Все места заняты. Попробуем иначе?':state.status==='Stuck'?'Не осталось доступных команд.':free<3?`${5-free} команды ждут. Учитывай очередь.`:'Выбери верхнюю капсулу.');
    $('board').setAttribute('aria-label',`${level.title}, поле ${level.w} на ${level.h}, осталось ${s.board.reduce((n,c)=>n+(c>=0),0)} клеток. Вход снизу.`);
    draw();
  }
  function act(action){
    if(busy||dialogs.some(d=>d.open))return false;
    clockTick();const before=state,result=R.step(state,action,true);if(!result.accepted)return false;
    state=result.state;attempt.commands.push({...action});attempt.updatedAt=new Date().toISOString();
    if(state.status!=='Playing'){attempt.outcome=state.status.toLowerCase();attempt.endedAt=new Date().toISOString();}
    save();
    if(reduced.matches||document.hidden){visual=state;render();if(state.status!=='Playing')showEnd();return true;}
    busy=true;visual=R.clone(before);let index=0;const events=result.events;
    const duration=store.settings.fast?140:Math.min(850,320+events.filter(e=>e.type==='clear').length*1.2);
    animation={start:performance.now(),duration,events,after:()=>{if(state.status!=='Playing')showEnd();}};
    render();
    const frame=now=>{
      if(!animation)return;
      const fraction=Math.min(1,(now-animation.start)/duration),until=Math.ceil(events.length*fraction);
      while(index<until){applyVisualEvent(visual,events[index++]);}
      render();
      if(fraction<1)raf=requestAnimationFrame(frame);else finishAnimation();
    };
    raf=requestAnimationFrame(frame);return true;
  }
  function applyVisualEvent(s,e){
    if(e.type==='commit'){s.slots[e.slot]=e.pod;s.ages[e.pod]=++s.serial;const column=s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===e.pod);s.heads[column]++;}
    if(e.type==='clear'){s.board[e.cell]=R.OPEN;s.remaining[e.pod]--;}
    if(e.type==='complete')s.slots[e.slot]=-1;
    if(e.type==='bridge'){s.board[e.cell]=R.OPEN;s.bridge=state.bridge;}
    s._reach=null;
  }
  function finishAnimation(){
    if(!animation)return;
    const after=animation.after;animation=null;cancelAnimationFrame(raf);busy=false;visual=state;render();after();
  }
  function showLesson(id){
    const lesson=LESSONS[id];if(!lesson)return;
    $('lessonTag').textContent=`L${id} · ${id===16?'ПЕРВЫЙ HARD':'НОВЫЙ ШАГ'}`;$('lessonTitle').textContent=lesson[0];$('lessonPortrait').src=assets[lesson[1]];$('lessonPortrait').alt=lesson[1];
    $('lessonSwatch').className='lesson-material';$('lessonSwatch').style.background=palette[lesson[2]].color;$('lessonSwatch').textContent=palette[lesson[2]].mark;$('lessonText').textContent=lesson[3];
    modal('lessonDialog');
  }
  function showEnd(){
    if($('endDialog').open)return;
    const won=state.status==='Won';$('endTag').textContent=`L${state.level.id} · ${won?'ПРОЙДЕН':'ПОПЫТКА'}`;$('endTitle').textContent=won?'Отлично получилось!':'Места закончились';
    $('endDescription').textContent=won?`${state.level.title}. ${attempt.commands.filter(a=>a.type==='tap').length} тапов. ${state.level.id===30?'Все 30 доступны в выборе уровней. Расскажи, как ощущается развитие.':'Можно оценить уровень и перейти дальше.'}`:'Команды ждут клетки, к которым пока не добраться. Попробуй другой порядок капсул. Ретрай ничего не стоит.';
    $('finishedPicture').hidden=!won;if(won)drawPicture($('finishedPicture'),true);
    $('nextButton').hidden=!won;$('nextButton').textContent=state.level.id===30?'Посмотреть результаты':'Следующий уровень';
    $('levelNote').value=attempt.note||'';syncFeedback();modal('endDialog');save();
  }
  function syncFeedback(){document.querySelectorAll('[data-rating]').forEach(b=>{b.classList.toggle('selected',b.dataset.rating===attempt.rating);b.setAttribute('aria-pressed',String(b.dataset.rating===attempt.rating));});document.querySelectorAll('[data-art]').forEach(b=>{b.classList.toggle('selected',b.dataset.art===attempt.art);b.setAttribute('aria-pressed',String(b.dataset.art===attempt.art));});}
  function retry(){finishAnimation();if(attempt.commands.length&&state.status==='Playing')modal('retryDialog');else loadLevel(state.level.id,{restart:true,lesson:false});}
  function showLevels(){
    finishAnimation();closeAll();const grid=$('levelGrid');grid.replaceChildren();
    for(const level of data.levels){const button=document.createElement('button'),done=store.attempts.some(a=>a.level===level.id&&a.outcome==='won');button.type='button';button.className=`level-choice${level.difficulty==='Hard'?' hard':''}${done?' done':''}${level.id===state.level.id?' current':''}`;button.dataset.level=level.id;
      const strong=document.createElement('strong'),small=document.createElement('small');strong.textContent=`${level.id}${done?' ✓':''}`;small.textContent=level.difficulty==='Hard'?'Hard':`${level.w}×${level.h}`;button.append(strong,small);button.setAttribute('aria-label',`Уровень ${level.id}, ${level.title}, ${level.difficulty}${done?', пройден':''}`);button.addEventListener('click',()=>loadLevel(level.id));grid.append(button);
    }modal('levelsDialog');
  }
  function showResults(){
    closeAll();clockTick();save();const won=new Set(store.attempts.filter(a=>a.outcome==='won').map(a=>a.level));$('resultsSummary').textContent=`Пройдено ${won.size} из 30. Попыток: ${store.attempts.length}.`;
    $('resultsList').replaceChildren();
    for(const level of data.levels){const attempts=store.attempts.filter(a=>a.level===level.id),wins=attempts.filter(a=>a.outcome==='won'),last=attempts.at(-1),row=document.createElement('div');row.className='result-row';const label=document.createElement('strong'),details=document.createElement('div');label.textContent=`L${level.id}`;
      details.textContent=!attempts.length?'Ещё не пробовал':`${wins.length?'✓ Пройден':'В процессе'} · ${attempts.length} поп. · ${Math.round(attempts.reduce((sum,a)=>sum+a.elapsedMs,0)/1000)} сек.${last.rating?` · ${last.rating==='easy'?'легко':last.rating==='good'?'хорошо':'трудно'}`:''}${last.note?' · '+last.note:''}`;
      row.append(label,details);$('resultsList').append(row);
    }$('versionLabel').textContent=`${data.version} · каталог ${data.hash.slice(0,12)} · правила ${data.rulesHash.slice(0,12)}`;modal('resultsDialog');
  }
  function exportResults(){
    clockTick();save();const report={format:'bloomlings-pilot-results-1',exportedAt:new Date().toISOString(),pilotVersion:data.version,catalogHash:data.hash,rulesHash:data.rulesHash,settings:store.settings,attempts:store.attempts};
    const blob=new Blob([JSON.stringify(report,null,2)+'\n'],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`Bloomlings-pilot-results-${new Date().toISOString().slice(0,10)}.json`;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),3000);
  }
  function draw(){if(!state||drawing)return;drawing=requestAnimationFrame(()=>{drawing=null;drawPicture($('board'),false);});}
  function drawPicture(canvas,finished){
    const s=visual||state,l=s.level,rect=canvas.getBoundingClientRect();
    const width=finished?280:Math.max(1,rect.width),height=finished?280:Math.max(1,rect.height),scale=finished?1:Math.min(2,window.devicePixelRatio||1);
    if(canvas.width!==Math.round(width*scale)||canvas.height!==Math.round(height*scale)){canvas.width=Math.round(width*scale);canvas.height=Math.round(height*scale);}
    const ctx=canvas.getContext('2d');ctx.setTransform(scale,0,0,scale,0,0);ctx.clearRect(0,0,width,height);
    const cell=Math.min((width-8)/l.w,(height-10)/l.h),ox=(width-l.w*cell)/2,oy=(height-l.h*cell)/2,depth=Math.max(.8,cell*.14),gap=Math.max(.35,cell*.035);
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
      const ready=R.bridgeChoices(state).map(c=>c.choice);
      l.hero.choices.forEach((choice,i)=>{const x=choice.anchor%l.w,y=Math.floor(choice.anchor/l.w),px=ox+(x+.5)*cell,py=oy+(l.h-y-.5)*cell;ctx.beginPath();ctx.arc(px,py,Math.max(2,cell*.34),0,Math.PI*2);ctx.fillStyle=ready.includes(i)?'#f3e4a7':'#fff7db99';ctx.fill();ctx.strokeStyle='#5b7d46';ctx.lineWidth=Math.max(1,cell*.08);ctx.stroke();});
    }
  }
  $('levelsButton').addEventListener('click',showLevels);
  $('retryButton').addEventListener('click',retry);
  $('confirmRetryButton').addEventListener('click',()=>loadLevel(state.level.id,{restart:true,lesson:false}));
  $('endRetryButton').addEventListener('click',()=>loadLevel(state.level.id,{restart:true,lesson:false}));
  $('endMapButton').addEventListener('click',showLevels);
  $('nextButton').addEventListener('click',()=>state.level.id===30?showResults():loadLevel(state.level.id+1));
  $('speedButton').addEventListener('click',()=>{store.settings.fast=!store.settings.fast;save();render();});
  $('symbolsButton').addEventListener('click',()=>{store.settings.symbols=!symbols();save();render();});
  $('resultsButton').addEventListener('click',showResults);$('exportButton').addEventListener('click',exportResults);
  $('helpButton').addEventListener('click',()=>{closeAll();modal('welcomeDialog');});
  $('startButton').addEventListener('click',()=>{store.started=true;save();close('welcomeDialog');if(LESSONS[state.level.id]&&!store.lessons.includes(state.level.id))showLesson(state.level.id);});
  $('lessonButton').addEventListener('click',()=>{if(!store.lessons.includes(state.level.id))store.lessons.push(state.level.id);save();close('lessonDialog');});
  document.querySelectorAll('[data-close]').forEach(b=>b.addEventListener('click',()=>close(b.dataset.close)));
  document.querySelectorAll('[data-rating]').forEach(b=>b.addEventListener('click',()=>{attempt.rating=b.dataset.rating;syncFeedback();save();}));
  document.querySelectorAll('[data-art]').forEach(b=>b.addEventListener('click',()=>{attempt.art=b.dataset.art;syncFeedback();save();}));
  $('levelNote').addEventListener('input',()=>{attempt.note=$('levelNote').value;save();});
  dialogs.forEach(d=>{d.addEventListener('cancel',()=>{clockTick();clock=performance.now();});d.addEventListener('close',()=>{clock=performance.now();});});
  document.addEventListener('visibilitychange',()=>{if(document.hidden){clockTick(true);finishAnimation();save();}clock=performance.now();});
  window.addEventListener('pagehide',()=>{clockTick();save();});
  window.addEventListener('resize',()=>{render();});new ResizeObserver(draw).observe($('boardFrame'));
  setInterval(()=>{clockTick();save();},15000);
  for(const name of ['sprig','bloom','drop','twig']){const img=document.createElement('img');img.src=assets[name];img.alt=name;img.width=90;img.height=110;$('welcomeHeroes').append(img);}
  loadLevel(store.current,{lesson:false});
  if(!store.started){closeAll();modal('welcomeDialog');}
  // Read-only inspection for reproducible browser QA, without solutions or cheats.
  Object.defineProperty(window,'bloomlingsPilot',{value:Object.freeze({snapshot:()=>({level:state.level.id,status:state.status,signature:R.signature(state),busy,commands:attempt.commands.length,bridge:state.bridge}),exportObject:()=>JSON.parse(JSON.stringify(store))})});
})();
