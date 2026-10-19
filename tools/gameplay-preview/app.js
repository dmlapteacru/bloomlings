(() => {
  'use strict';
  const assets = JSON.parse(document.getElementById('previewAssets').textContent);
  const planSource = JSON.parse(document.getElementById('planSource').textContent);
  const $ = id => document.getElementById(id);
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const COLORS = {
    G: {hex:'#91bd53', name:'Листва', hero:'sprig', mark:'◇'},
    P: {hex:'#ef91a7', name:'Лепесток', hero:'bloom', mark:'✦'},
    B: {hex:'#68bad0', name:'Вода', hero:'drop', mark:'●'},
    A: {hex:'#cd975a', name:'Янтарь', hero:'twig', mark:'◆'},
    C: {hex:'#f6e8bc', name:'Жемчужный', hero:'drop', mark:'○'},
    D: {hex:'#354c45', name:'Тёмный контур', hero:'twig', mark:'■'},
    V: {hex:'#9b8ab6', name:'Фиолетовый', hero:'bloom', mark:'✧'},
    M: {hex:'#d1e4a3', name:'Светлая листва', hero:'sprig', mark:'△'}
  };
  const HEROES = {
    sprig:{name:'Sprig',intro:'ЗНАКОМСТВО С L1',title:'Команда листвы',description:'Начинает с зелёных клеток. Позже появляются другие растительные оттенки: их можно использовать в красивой картинке, не придумывая ещё одного героя.',colors:['G','M'],tint:'#dfecc0',rule:'Зелёная и светлая капсулы Sprig — два разных материала. Совпадает образ героя, но не канал работы.',action:'Выращивает мостик',effect:'Открывает путь к дальней области. Оставшаяся работа капсулы становится доступной.',status:'Первый прототип · около L25'},
    bloom:{name:'Bloom',intro:'ЗНАКОМСТВО С L4',title:'Команда лепестков',description:'Розовый становится началом цветочной палитры. Позже здесь могут появиться коралловый, красный и фиолетовый — столько, сколько действительно нужно рисунку.',colors:['P','V'],tint:'#f4d9dd',rule:'На одном уровне держим небольшой набор различимых каналов. Герой помогает узнавать команду; точную краску показывает капсула.',action:'Обменивает две клумбы',effect:'Переставляет цвет двух равных отмеченных групп. Количество каждого цвета сохраняется.',status:'Эксперимент · не в первом срезе'},
    drop:{name:'Drop',intro:'ЗНАКОМСТВО С L1',title:'Команда воды и света',description:'Голубой и бирюзовый подходят воде и стеклу. Светлый жемчужный материал добавляет блики, чашки, лица и другие детали, которых не хватает старой палитре.',colors:['B','C'],tint:'#d8edf0',rule:'Кремовый — самостоятельный канал с собственным образцом и знаком. Его связь с командой задаётся явно в данных уровня.',action:'Смывает туман',effect:'Раскрывает отмеченный участок. Меняет доступную информацию, сохраняя клетки и их цвета.',status:'Гипотеза · обсудим после прототипа'},
    twig:{name:'Twig',intro:'ЗНАКОМСТВО С L7',title:'Команда дерева и контура',description:'Янтарь и коричневый дают тёплые предметы. Тёмный нейтральный канал помогает сделать читаемый контур, глаза и силуэты — без обязательной новой пары близких цветов.',colors:['A','D'],tint:'#f0dfbc',rule:'Герой не становится тёмным или кремовым вслед за материалом. Его принятый облик и выбранная одежда сохраняются.',action:'Сдвигает преграду',effect:'Открывает один проход и может закрыть другой. Порядок очистки становится важен.',status:'Гипотеза · обсудим после прототипа'}
  };
  const STAGES = [
    {level:1,w:11,h:12,art:'leaf',hero:'sprig',icons:true,title:'Листочек и капля',range:'L1–3 · ПЕРВЫЕ СОВПАДЕНИЯ',desc:'Крупные клетки и два хорошо различимых цвета. Sprig и Drop показывают связь между командой, капсулой и материалом.',lesson:'Выбрать подходящий цвет и увидеть, как заканчивается капсула. Пока без Hard и специальных препятствий.',line:'Зелёные клетки собирает команда Sprig. Рядом впервые появляется Drop.',note:'Один простой сюжет. На таком размере не пытаемся рисовать целую сцену.',difficulty:'Знакомство'},
    {level:4,w:12,h:14,art:'flower',hero:'bloom',icons:true,title:'Первый цветок Bloom',range:'L4–6 · ТРЕТИЙ ЦВЕТ',desc:'Розовый добавляется отдельным понятным шагом. Поле уже немного больше; цвет занимает главное место, символы ещё помогают узнавать команды.',lesson:'Очистка наружного участка даёт доступ к следующему. Новый цвет закрепляем на обычном уровне.',line:'Bloom знакомит с розовым материалом. Большой герой появляется ненадолго.',note:'Крупный цветок, стебель и вода. Детали читаются по нескольким большим пятнам.',difficulty:'Новый герой'},
    {level:7,w:14,h:15,art:'pot',hero:'twig',icons:true,title:'Сад в глиняном горшке',range:'L7–10 · ЧЕТЫРЕ КОМАНДЫ',desc:'Появляется Twig и четвёртый материал. У рисунка уже есть несколько связанных частей, но он всё ещё легко узнаётся на телефоне.',lesson:'Порядок капсул начинает влиять на освобождение мест. Ярлыки Hard/Super Hard в первой десятке не нужны.',line:'Янтарный горшок относится к команде Twig. Все четыре героя уже знакомы.',note:'Простые цветы и горшок вместо множества мелких предметов.',difficulty:'Новый герой'},
    {level:12,w:18,h:18,art:'cup',hero:'drop',icons:false,title:'Чашка с маленьким садом',range:'L11–15 · ПЕРВЫЙ СВЕТЛЫЙ МАТЕРИАЛ',desc:'Светлый канал помогает сделать настоящую кремовую чашку. Картинка больше не должна укладываться только в четыре цвета тел героев.',lesson:'Один герой может вести две команды разных материалов. Образец на капсуле определяет, что она собирает.',line:'Drop представляет жемчужный материал. Это отдельный канал, а не бесплатный перекрас голубого.',note:'Пять цветов. Символы можно включить кнопкой — доступность сохраняется.',difficulty:'Новый материал'},
    {level:20,w:22,h:22,art:'lantern',hero:'twig',icons:false,title:'Жёлудь-фонарь',range:'L16–20 · УЖЕ БОЛЬШАЯ КАРТИНКА',desc:'К L20 приходим к 22×22. Первый Hard предлагаем проверить около L16, а здесь знакомим с тёмным контуром на обычной задаче.',lesson:'Тёмный канал делает силуэт и детали читаемыми. Новую краску не совмещаем с первым сложным механизмом.',line:'Twig работает не только с янтарём: у контура есть собственная капсула и остаток.',note:'Шесть материалов, один главный предмет и маленькие цветовые акценты.',difficulty:'Новый материал'},
    {level:25,w:24,h:26,art:'umbrella',hero:'sprig',icons:false,title:'Лист-зонт после дождя',range:'L21–25 · ПЕРВЫЙ ЭПИЗОД ГЕРОЯ',desc:'К L25 поле достигает 24×26. Отдельно предлагаем простой эпизод Sprig: мостик открывает нужную область и помогает закончить ожидающую капсулу.',lesson:'Герой влияет на доступность клеток. Ниже можно попробовать схему мостика; эта мозаика показывает художественный масштаб этапа.',line:'Один заметный момент Sprig, заранее встроенный в задачу. Действие не покупается.',note:'Пример рисунка этапа; расположение мостика нужно проектировать вместе с будущей очередью.',difficulty:'Кандидат эпизода'},
    {level:40,w:26,h:28,art:'whale',hero:'bloom',icons:false,title:'Кит с садом на спине',range:'L26–50 · ХАРАКТЕР И НОВЫЕ ЦЕЛИ',desc:'Красивые крупные силуэты уже становятся обычной частью игры. Рядом идут спокойные задачи и настоящие Hard. На L40 открывается Wardrobe.',lesson:'Выигрыш даёт и новую картинку, и прогресс к вещам для любимого героя. Ключи появляются только там, где действительно меняют задачу.',line:'Герой сохраняет выбранный образ. Кастомизация даёт личную цель, не усиливая капсулу.',note:'Один необычный акцент — сад на ките. Никакой россыпи мелкой штриховки.',difficulty:'Wardrobe · L40'},
    {level:75,w:30,h:32,art:'bubble',hero:'drop',icons:false,title:'Луна в стеклянном саду',range:'L51–99 · БОЛЬШИЕ СИЛУЭТЫ И КОМБИНАЦИИ',desc:'Регулярно встречаются поля 24×26–28×30 и отдельные 30×32. Крупный рисунок оставляет место для сочетания известных зависимостей.',lesson:'Здесь можно строить Hard на порядке доступа и длительном занятии слотов. Длительность сама по себе не определяет класс.',line:'Новая краска не требует нового героя. Большую сцену ведут знакомые команды.',note:'Семь каналов в этом примере. Большинство обычных полей остаются с пятью-шестью.',difficulty:'Hard · пример'},
    {level:100,w:36,h:36,art:'portrait',hero:'sprig',icons:false,title:'Портрет Sprig как событие',range:'L100 · ПЕРВАЯ БОЛЬШАЯ ВЕХА',desc:'На значимом уровне — 36×36 и знакомый герой. Это отдельный праздничный рисунок, после которого размеры снова чередуются.',lesson:'Большое поле может быть зрелищным без Super Hard. Сохраняем уникальные мозаики, коллекцию и последовательную прогрессию.',line:'Герой, с которым началась игра, становится самой картинкой. Далее вехи Bloom, Drop и Twig.',note:'Это новый эскиз для обсуждения, не замена установленной мозаики. Twig L2000 не меняем.',difficulty:'Веха · 36×36'}
  ];
  const SIZES = [[11,12],[11,13],[12,13],[12,14],[13,14],[13,15],[14,15],[14,16],[15,17],[16,18],[17,18],[18,18],[18,19],[19,19],[19,20],[20,20],[20,21],[21,21],[21,22],[22,22],[22,23],[23,23],[23,24],[24,24],[24,26]];

  // Illustrative pixel art is authored directly as vector shapes sampled onto an exact grid.
  // It is separate from the game's catalog and does not edit any existing image asset.
  function makeArt(stage) {
    const {w,h,art}=stage;
    const cells=Array.from({length:h},()=>Array(w).fill(null));
    const fill=(test,c)=>{for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(test((x+.5)*100/w,(y+.5)*100/h))cells[y][x]=c;};
    const ellipse=(cx,cy,rx,ry,c)=>fill((x,y)=>((x-cx)/rx)**2+((y-cy)/ry)**2<=1,c);
    const rect=(l,t,r,b,c)=>fill((x,y)=>x>=l&&x<=r&&y>=t&&y<=b,c);
    const poly=(points,c)=>fill((x,y)=>{let inside=false;for(let i=0,j=points.length-1;i<points.length;j=i++){const [a,b]=points[i],[c,d]=points[j];if(((b>y)!==(d>y))&&(x<(c-a)*(y-b)/(d-b)+a))inside=!inside;}return inside;},c);
    const flower=(x,y,r,petal='P',center='C')=>{for(let i=0;i<5;i++){const a=i*Math.PI*2/5-Math.PI/2;ellipse(x+Math.cos(a)*r*.8,y+Math.sin(a)*r*.8,r*.58,r*.58,petal);}ellipse(x,y,r*.38,r*.38,center);};
    const leaf=(x,y,s,c='G')=>poly([[x-s,y+s*.2],[x-s*.8,y-s*.65],[x+s*.7,y-s],[x+s,y+s*.1],[x+s*.3,y+s*.75]],c);
    const drop=(x,y,s,c='B')=>{poly([[x,y-s],[x-s*.7,y+s*.2],[x+s*.7,y+s*.2]],c);ellipse(x,y+s*.15,s*.7,s*.65,c);};
    switch(art){
      case 'leaf':
        rect(45,45,54,88,'G');poly([[49,69],[23,60],[12,35],[24,12],[48,25],[59,49]],'G');poly([[51,62],[58,36],[79,22],[86,45],[72,64]],'G');drop(76,78,12);break;
      case 'flower':
        rect(47,43,54,92,'G');leaf(32,69,16);leaf(65,77,15);flower(50,33,23,'P','B');ellipse(50,94,28,3,'B');break;
      case 'pot':
        rect(32,23,38,69,'G');rect(62,25,68,69,'G');leaf(47,43,18);leaf(23,51,14);leaf(72,51,16);flower(33,22,16,'P','B');flower(67,24,16,'B','P');poly([[22,62],[78,62],[69,95],[31,95]],'A');rect(18,60,82,69,'A');break;
      case 'cup':
        ellipse(77,62,17,21,'A');ellipse(78,61,10,13,null);poly([[17,41],[77,41],[71,83],[26,83]],'A');poly([[23,47],[70,47],[65,76],[30,76]],'C');ellipse(48,83,30,5,'A');rect(46,24,52,48,'G');leaf(34,34,13);flower(52,23,16);rect(36,57,40,65,'B');rect(48,57,52,65,'B');rect(60,57,64,65,'B');break;
      case 'lantern':
        ellipse(50,60,32,34,'D');ellipse(50,59,28,30,'A');ellipse(50,31,36,14,'D');ellipse(50,29,33,11,'A');rect(46,7,54,22,'A');leaf(67,13,18);ellipse(50,56,12,13,'C');rect(38,55,62,78,'C');rect(48,46,52,78,'A');rect(38,60,62,64,'A');rect(30,89,69,94,'D');flower(17,81,9);drop(85,77,8);break;
      case 'umbrella':
        poly([[9,45],[15,29],[32,15],[51,9],[74,18],[91,39],[85,47]],'D');poly([[14,40],[21,29],[37,17],[51,13],[71,22],[85,39]],'G');rect(48,40,54,82,'D');rect(49,40,52,81,'A');ellipse(57,80,9,10,'A');ellipse(57,76,5,8,null);drop(18,62,7);drop(78,60,7);drop(34,84,6);rect(66,83,70,95,'G');flower(68,80,10);break;
      case 'whale':
        poly([[75,57],[88,35],[92,48],[98,43],[94,70],[76,78]],'D');poly([[78,58],[88,43],[89,56],[94,52],[90,66],[80,72]],'B');ellipse(46,62,36,25,'D');ellipse(45,60,33,22,'B');ellipse(43,72,27,10,'C');ellipse(32,56,3,4,'D');ellipse(32,54,1.2,1.4,'C');poly([[47,72],[66,70],[62,84],[52,80]],'B');rect(40,30,60,43,'A');rect(48,17,52,34,'G');leaf(61,23,13);leaf(39,28,10);flower(50,15,10);break;
      case 'bubble':
        ellipse(50,46,35,38,'D');ellipse(50,44,32,35,'B');ellipse(50,44,28,31,'V');ellipse(58,28,13,14,'C');ellipse(64,23,12,13,'V');rect(19,79,81,86,'D');rect(26,84,74,91,'A');rect(18,90,82,95,'D');ellipse(49,69,26,9,'G');rect(36,51,40,72,'G');leaf(32,51,11);flower(39,47,10);rect(57,55,61,72,'A');leaf(59,52,12);rect(29,28,32,36,'C');rect(26,31,35,34,'C');rect(73,48,76,54,'C');break;
      case 'portrait':
        ellipse(50,51,43,44,'B');ellipse(50,49,30,32,'D');ellipse(23,48,6,8,'M');ellipse(77,48,6,8,'M');rect(34,66,66,90,'G');ellipse(33,88,9,7,'G');ellipse(67,88,9,7,'G');ellipse(50,79,10,10,'C');ellipse(50,46,27,26,'M');leaf(29,27,16);leaf(47,15,20);leaf(64,21,18);leaf(76,31,12);ellipse(39,47,6,8,'D');ellipse(62,47,6,8,'D');ellipse(37,44,2,2.5,'C');ellipse(60,44,2,2.5,'C');ellipse(30,57,4,2.5,'P');ellipse(71,57,4,2.5,'P');poly([[44,58],[50,61],[57,58],[54,64],[48,65]],'D');ellipse(27,76,7,10,'M');ellipse(73,76,7,10,'M');rect(69,73,77,81,'A');break;
    }
    return cells;
  }
  const tint=(hex,amount)=>{const n=parseInt(hex.slice(1),16),f=v=>Math.max(0,Math.min(255,v+amount));return `rgb(${f(n>>16)},${f((n>>8)&255)},${f(n&255)})`;};
  function drawMark(ctx,x,y,size,c){
    ctx.save();ctx.translate(x+size/2,y+size/2);ctx.scale(size/24,size/24);
    ctx.fillStyle=c==='D'?'#f4ecc8cc':'#29483b9c';ctx.strokeStyle=ctx.fillStyle;ctx.lineWidth=1.5;
    if(c==='G'||c==='M'){
      ctx.beginPath();ctx.moveTo(-6,5);ctx.quadraticCurveTo(-9,-6,6,-7);ctx.quadraticCurveTo(9,7,-6,5);ctx.fill();
      ctx.strokeStyle='#ffffff77';ctx.beginPath();ctx.moveTo(-5,6);ctx.lineTo(5,-5);ctx.stroke();
    }else if(c==='P'||c==='V'){
      for(let i=0;i<5;i++){const a=i*Math.PI*2/5-Math.PI/2;ctx.beginPath();ctx.arc(Math.cos(a)*4,Math.sin(a)*4,2.8,0,Math.PI*2);ctx.fill();}
      ctx.fillStyle='#ffffff99';ctx.beginPath();ctx.arc(0,0,2,0,Math.PI*2);ctx.fill();
    }else if(c==='B'){
      ctx.beginPath();ctx.moveTo(0,-8);ctx.bezierCurveTo(-3,-4,-7,0,-5,5);ctx.bezierCurveTo(0,11,10,5,4,-2);ctx.closePath();ctx.fill();
    }else if(c==='A'){
      ctx.beginPath();ctx.ellipse(0,2,5,6,0,0,Math.PI*2);ctx.fill();ctx.fillRect(-6,-4,12,3);ctx.fillRect(-1,-8,2,4);
    }else if(c==='C'){
      ctx.lineWidth=2;ctx.beginPath();ctx.arc(0,0,5,0,Math.PI*2);ctx.stroke();ctx.beginPath();ctx.arc(-1,-1,1,0,Math.PI*2);ctx.fill();
    }else{ctx.fillRect(-4,-4,8,8);}
    ctx.restore();
  }
  function tile(ctx,x,y,size,c,icon=false){
    const color=COLORS[c],gap=Math.max(.8,size*.055),s=size-gap;
    ctx.fillStyle=tint(color.hex,-25);ctx.fillRect(x,y+2,s,s);
    ctx.fillStyle=color.hex;ctx.fillRect(x,y,s,s-1);
    ctx.fillStyle='rgba(255,255,255,.19)';ctx.fillRect(x+1,y+1,s-2,Math.max(1,size*.08));
    if(icon&&size>=14)drawMark(ctx,x,y,s,c);
  }
  function drawBoard(canvas,cells,{icons=false,grid=false}={}){
    const ctx=canvas.getContext('2d'),w=cells[0].length,h=cells.length,W=canvas.width,H=canvas.height;
    ctx.clearRect(0,0,W,H);const cell=Math.min((W-45)/w,(H-45)/h),ox=(W-w*cell)/2,oy=(H-h*cell)/2;
    if(grid){ctx.strokeStyle='#42583e22';ctx.lineWidth=1;for(let y=0;y<=h;y++){ctx.beginPath();ctx.moveTo(ox,oy+y*cell);ctx.lineTo(ox+w*cell,oy+y*cell);ctx.stroke();}for(let x=0;x<=w;x++){ctx.beginPath();ctx.moveTo(ox+x*cell,oy);ctx.lineTo(ox+x*cell,oy+h*cell);ctx.stroke();}}
    for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(cells[y][x])tile(ctx,ox+x*cell,oy+y*cell,cell,cells[y][x],icons);
  }
  const ART=STAGES.map(makeArt);
  let selectedStage=0,symbolPreference=null,showGrid=false,selectedHero='sprig';
  function selectStage(index){
    selectedStage=index;const s=STAGES[index],cells=ART[index],counts={};cells.flat().filter(Boolean).forEach(c=>counts[c]=(counts[c]||0)+1);
    $('levelLabel').textContent=`Уровень ${s.level}`;$('difficultyLabel').textContent=s.difficulty;$('difficultyLabel').classList.toggle('hard',s.difficulty.startsWith('Hard'));
    $('stageRange').textContent=s.range;$('stageTitle').textContent=s.title;$('stageDescription').textContent=s.desc;$('stageSize').textContent=`${s.w}×${s.h}`;$('stageColors').textContent=Object.keys(counts).length;$('stageCells').textContent=Object.values(counts).reduce((a,b)=>a+b,0);$('stageLesson').textContent=s.lesson;
    $('stageHeroName').textContent=HEROES[s.hero].name;$('stageHeroLine').textContent=s.line;$('stageHeroImage').src=assets[s.hero];$('stageArtNote').textContent=s.note;$('boardMilestone').hidden=s.level!==100;
    const symbols=symbolPreference??s.icons;
    $('symbolsToggle').setAttribute('aria-pressed',String(symbols));$('symbolsToggle').textContent=`Символы: ${symbols?'вкл.':'выкл.'}`;
    $('mainBoard').setAttribute('aria-label',`${s.title}, поле ${s.w} на ${s.h}, ${Object.keys(counts).length} цветов, ${$('stageCells').textContent} цветных клеток`);
    drawBoard($('mainBoard'),cells,{icons:symbols,grid:showGrid});
    document.querySelectorAll('[data-stage]').forEach(b=>{const active=Number(b.dataset.stage)===index;b.classList.toggle('selected',active);b.setAttribute('aria-pressed',String(active));});
    $('capsuleRow').innerHTML=Object.keys(counts).slice(0,5).map(c=>{const p=COLORS[c];return `<div class="capsule" style="background:${p.hex};${c==='C'||c==='M'?'color:#354c45;text-shadow:none':''}" aria-label="${HEROES[p.hero].name}, ${p.name}, пример капсулы"><img src="${assets[p.hero]}" alt=""><span class="capsule-mark">${p.mark}</span><strong>${Math.min(counts[c],25)}</strong></div>`;}).join('');
  }
  function selectHero(id){
    selectedHero=id;const h=HEROES[id];$('heroDetail').style.setProperty('--heroTint',h.tint);$('heroDetail').setAttribute('aria-labelledby',`hero-tab-${id}`);$('heroPortrait').src=assets[id];$('heroPortrait').alt=h.name;$('heroIntro').textContent=h.intro;$('heroTitle').textContent=h.title;$('heroDescription').textContent=h.description;$('heroRule').textContent=h.rule;
    $('heroSwatches').innerHTML=h.colors.map(c=>`<span class="swatch"><i style="background:${COLORS[c].hex}" aria-hidden="true"></i>${COLORS[c].name} ${COLORS[c].mark}</span>`).join('');
    document.querySelectorAll('[data-hero-tab]').forEach(b=>{const active=b.dataset.heroTab===id;b.classList.toggle('selected',active);b.setAttribute('aria-selected',String(active));b.tabIndex=active?0:-1;});
  }
  function rovingTabs(container,select,attribute){
    container.addEventListener('keydown',e=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;const buttons=[...container.querySelectorAll('[role=tab]')],i=buttons.indexOf(document.activeElement);if(i<0)return;e.preventDefault();const j=e.key==='Home'?0:e.key==='End'?buttons.length-1:(i+(e.key==='ArrowLeft'?-1:1)+buttons.length)%buttons.length;select(buttons[j].dataset[attribute]);buttons[j].focus();});
  }

  let demo={kind:'bridge',sent:false,acted:false};
  function drawDemo(){
    const c=$('actionBoard'),ctx=c.getContext('2d'),{kind,sent,acted}=demo;
    ctx.clearRect(0,0,c.width,c.height);
    ctx.fillStyle='#dce6c4';ctx.fillRect(22,40,274,360);ctx.fillRect(424,40,274,360);
    ctx.fillStyle='#a4d2d4';ctx.fillRect(306,0,108,540);
    ctx.strokeStyle='#ffffff66';ctx.lineWidth=3;for(let y=30;y<540;y+=42){ctx.beginPath();ctx.moveTo(325,y);ctx.quadraticCurveTo(345,y-8,365,y);ctx.quadraticCurveTo(382,y+8,398,y);ctx.stroke();}
    if(kind==='bridge'&&acted){ctx.fillStyle='#ad955b';ctx.fillRect(278,248,164,42);for(let i=0;i<7;i++){ctx.fillStyle='#88a655';ctx.fillRect(285+i*22,244+(i%2)*3,16,48);}ctx.strokeStyle='#79934b';ctx.lineWidth=5;ctx.beginPath();ctx.moveTo(278,244);ctx.lineTo(442,244);ctx.stroke();}
    else{ctx.strokeStyle='#c7bb8c';ctx.lineWidth=3;ctx.setLineDash([7,7]);ctx.strokeRect(278,246,164,45);ctx.setLineDash([]);}
    const left=kind==='bridge'?'G':acted?'G':'P',right=kind==='bridge'?'G':acted?'P':'G';
    const clearLeft=sent&&(kind==='bridge'||acted),clearRight=kind==='bridge'&&sent&&acted;
    for(let i=0;i<4;i++){const x=i%2,y=Math.floor(i/2);if(!clearLeft)tile(ctx,119+x*49,139+y*49,48,left);if(!clearRight)tile(ctx,526+x*49,139+y*49,48,right);}
    ctx.textAlign='center';ctx.textBaseline='middle';ctx.font='900 19px Nunito, sans-serif';ctx.fillStyle='#456045';ctx.fillText('Ближний участок',159,90);ctx.fillText('Дальний участок',563,90);
    ctx.font='600 17px Nunito, sans-serif';ctx.fillStyle='#6b7e65';ctx.fillText(clearLeft?'Собрано':`4 ${left==='G'?'зелёных':'розовых'}`,159,280);ctx.fillText(clearRight?'Собрано':`4 ${right==='G'?'зелёных':'розовых'}`,563,280);
    ctx.fillText(kind==='bridge'?(acted?'Маршрут открыт':'Канал без перехода'):'Обмен равных групп',360,465);
    $('actionBoard').setAttribute('aria-label',`${kind==='bridge'?'Мостик Sprig':'Обмен цвета Bloom'}. ${$('demoStatus').textContent}`);
  }
  function refreshDemo(){
    const {kind,sent,acted}=demo,bridge=kind==='bridge',total=bridge?8:4,remaining=sent?(acted?0:4):0,completed=sent&&acted;
    if(!acted||reducedMotion.matches){$('demoEffect').classList.remove('flash');$('actionHero').classList.remove('celebrate');}
    $('actionHero').src=assets[bridge?'sprig':'bloom'];$('actionHero').alt=bridge?'Sprig':'Bloom';
    $('demoKicker').textContent=bridge?'ПЕРВЫЙ ПРОТОТИП · КАНДИДАТ L25':'ЭКСПЕРИМЕНТ · НЕ В ПЕРВОМ СРЕЗЕ';
    $('demoTitle').textContent=bridge?'Мостик освобождает место':'Цвет меняет место, а не количество';
    $('demoDescription').textContent=bridge?'У капсулы 8 зелёных единиц работы. Четыре клетки рядом, ещё четыре — на островке за каналом. Попробуй запустить капсулу до мостика или после него.':'У зелёной капсулы 4 единицы работы. Рядом розовый участок, зелёный пока недоступен. Bloom может обменять цвета этих двух равных групп.';
    $('demoSteps').innerHTML=bridge?'<li>Без мостика капсула собирает только ближние 4 клетки.</li><li>Остаток 4 занимает одно из пяти мест.</li><li>Sprig открывает маршрут. Остаток заканчивается, место освобождается.</li>':'<li>Зелёная капсула ждёт: рядом нет нужного материала.</li><li>Обмен переносит 4 зелёных клетки ближе, а 4 розовых — дальше.</li><li>Зелёная работа завершается. Общее количество цвета от обмена не увеличилось.</li>';
    $('sendCapsule').textContent=sent?(completed?'Капсула закончилась':`Капсула ждёт · ${remaining}`):`Запустить зелёную капсулу · ${total}`;$('sendCapsule').disabled=sent;
    $('useHero').textContent=acted?(bridge?'Мостик готов':'Цвета обменяны'):(bridge?'Sprig · вырастить мостик':'Bloom · обменять цвета');$('useHero').disabled=acted;
    $('demoSlots').innerHTML=Array.from({length:5},(_,i)=>`<div class="demo-slot ${sent&&!completed&&i===0?'filled':''}" aria-label="Место ${i+1}: ${sent&&!completed&&i===0?'капсула, остаток '+remaining:'свободно'}">${sent&&!completed&&i===0?remaining:'·'}</div>`).join('');
    $('demoCounter').textContent=sent&&!completed?'Занято 1 из 5 мест':completed?'Все 5 мест свободны · капсула завершена':'Все 5 мест свободны';
    let status;
    if(completed)status=bridge?'Дальние 4 клетки доступны и собраны. Остаток стал 0 — место освободилось благодаря новому маршруту.':'Ближние 4 зелёные клетки собраны, место освободилось. Розовые остались на дальнем участке; для них ещё нужен маршрут.';
    else if(sent)status=bridge?'Собрано 4 из 8. Ещё 4 за каналом: капсула ждёт и занимает место. Попробуй действие Sprig.':'Зелёная капсула заняла место, но работа недоступна. Попробуй обмен Bloom: количество зелёного останется прежним.';
    else if(acted)status=bridge?'Мост уже открыт. Теперь зелёная капсула сможет собрать обе четвёрки без ожидания.':'Теперь зелёная четвёрка находится рядом. Запусти капсулу и проверь, что она завершится.';
    else status=bridge?'Начни с любого действия. Так можно сравнить, когда капсула занимает место в ожидании, а когда заканчивается сразу.':'Начни с капсулы или обмена. Здесь показываем последствие перестановки, а не произвольное создание новой краски.';
    $('demoStatus').textContent=status;drawDemo();
  }
  function selectDemo(kind){
    demo={kind,sent:false,acted:false};$('demoPanel').setAttribute('aria-labelledby',`demo-tab-${kind}`);
    document.querySelectorAll('[data-demo]').forEach(b=>{const active=b.dataset.demo===kind;b.classList.toggle('selected',active);b.setAttribute('aria-selected',String(active));b.tabIndex=active?0:-1;});refreshDemo();
  }
  document.querySelectorAll('[data-hero]').forEach(img=>{img.src=assets[img.dataset.hero];});
  document.documentElement.style.setProperty('--garden',`url("${assets.garden}")`);
  $('stagePicker').innerHTML=STAGES.map((s,i)=>`<button type="button" data-stage="${i}" aria-pressed="${i===0}">L${s.level}</button>`).join('');
  $('artGallery').innerHTML=STAGES.map((s,i)=>`<button class="art-card" type="button" data-stage="${i}" aria-pressed="${i===0}"><canvas width="240" height="254" id="mini-${i}" role="img" aria-label="${s.title}"></canvas><strong>${s.title}</strong><span>L${s.level} · ${s.w}×${s.h}</span></button>`).join('');
  document.querySelectorAll('[data-stage]').forEach(b=>b.addEventListener('click',()=>{selectStage(Number(b.dataset.stage));if(b.classList.contains('art-card'))$('journey').scrollIntoView({behavior:reducedMotion.matches?'auto':'smooth'});}));
  $('sizeLadder').innerHTML=SIZES.map(([w,h],i)=>`<span><b>L${i+1}</b>${w}×${h}</span>`).join('');
  $('heroTabs').innerHTML=Object.entries(HEROES).map(([id,h])=>`<button id="hero-tab-${id}" type="button" role="tab" aria-controls="heroDetail" aria-selected="${id==='sprig'}" data-hero-tab="${id}" tabindex="${id==='sprig'?0:-1}"><img src="${assets[id]}" alt="" width="70" height="80"><strong>${h.name}</strong></button>`).join('');
  document.querySelectorAll('[data-hero-tab]').forEach(b=>b.addEventListener('click',()=>selectHero(b.dataset.heroTab)));rovingTabs($('heroTabs'),selectHero,'heroTab');
  $('futureCards').innerHTML=Object.entries(HEROES).map(([id,h])=>`<article class="future-card"><img src="${assets[id]}" alt="${h.name}" width="72" height="90"><h3>${h.action}</h3><span class="tag">${h.status}</span><p>${h.effect}</p></article>`).join('');
  $('symbolsToggle').addEventListener('click',()=>{symbolPreference=!(symbolPreference??STAGES[selectedStage].icons);selectStage(selectedStage);});
  $('gridToggle').addEventListener('click',()=>{showGrid=!showGrid;$('gridToggle').setAttribute('aria-pressed',String(showGrid));$('gridToggle').textContent=showGrid?'Скрыть сетку':'Показать сетку';selectStage(selectedStage);});
  document.querySelectorAll('[data-demo]').forEach(b=>b.addEventListener('click',()=>selectDemo(b.dataset.demo)));rovingTabs(document.querySelector('.demo-tabs'),selectDemo,'demo');
  $('sendCapsule').addEventListener('click',()=>{demo.sent=true;refreshDemo();});
  $('useHero').addEventListener('click',()=>{demo.acted=true;refreshDemo();if(!reducedMotion.matches){for(const [id,cls] of [['demoEffect','flash'],['actionHero','celebrate']]){$(id).classList.remove(cls);void $(id).offsetWidth;$(id).classList.add(cls);}}});
  $('resetDemo').addEventListener('click',()=>{demo={kind:demo.kind,sent:false,acted:false};refreshDemo();});
  $('downloadPlan').addEventListener('click',()=>{const url=URL.createObjectURL(new Blob([planSource],{type:'text/markdown;charset=utf-8'})),a=document.createElement('a');a.href=url;a.download='bloomlings-gameplay-plan.md';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});
  $('printPage').addEventListener('click',()=>window.print());
  const printDetails=new Map();window.addEventListener('beforeprint',()=>{document.querySelectorAll('details').forEach(d=>{printDetails.set(d,d.open);d.open=true;});});window.addEventListener('afterprint',()=>{printDetails.forEach((open,d)=>d.open=open);printDetails.clear();});
  const navLinks=[...document.querySelectorAll('.section-nav a')];
  if('IntersectionObserver' in window){const observer=new IntersectionObserver(entries=>{entries.forEach(e=>{if(e.isIntersecting)navLinks.forEach(a=>a.classList.toggle('active',a.hash===`#${e.target.id}`));});},{rootMargin:'-10% 0px -65% 0px'});document.querySelectorAll('main>section').forEach(s=>observer.observe(s));}
  STAGES.forEach((s,i)=>drawBoard($(`mini-${i}`),ART[i]));selectStage(0);selectHero(selectedHero);refreshDemo();
  // Small read-only hook for browser checks; no production game engine is embedded here.
  window.bloomlingsPreview={stages:STAGES.map((s,i)=>({level:s.level,w:s.w,h:s.h,colors:[...new Set(ART[i].flat().filter(Boolean))],cells:ART[i].flat().filter(Boolean).length})),getDemo:()=>({...demo}),version:'2026-10-19'};
})();
