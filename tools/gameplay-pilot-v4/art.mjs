// Bold silhouettes for the second playtest. Outlines follow the actual object;
// no rectangular difficulty frames. Each subject is rasterised at its own size.
export const SIZES=[[16,18],[18,20],[20,22],[24,26],[24,26],[24,26],[24,26],[24,26],[24,26],[26,28],
 [24,26],[26,28],[26,28],[26,28],[22,24],[24,26],[26,28],[26,28],[28,30],[24,26],
 [26,28],[25,27],[26,28],[28,30],[26,28],[28,30],[28,30],[26,28],[28,30],[28,30]];
export const SUBJECTS=[
 ['leaf','Большой лист'],['drop','Капелька'],['flower','Цветок Bloom'],['butterfly','Бабочка'],['acorn','Жёлудь Twig'],
 ['mushroom','Красный гриб'],['strawberry','Клубника'],['mug','Чашка с цветком'],['letter','Письмо'],['fish','Рыбка'],
 ['turtle','Черепаха'],['owl','Сова'],['umbrella','Зонтик'],['teapot','Чайник'],['cake','Клубничный торт'],
 ['kite','Воздушный змей'],['boot','Садовый сапог'],['mushroomhouse','Грибной домик'],['flowerpot','Цветок в горшке'],['penguin','Пингвин'],
 ['pear','Груша'],['cottage','Домик у ручья'],['balloon','Воздушный шар'],['snail','Улитка у ручья'],['cat','Котёнок'],
 ['sunflower','Подсолнух'],['whale','Кит с садом'],['apple','Яблоко'],['lantern','Садовый фонарь'],['bunny','Зайчик']
];
export const BRIDGES=new Set([20,21,22,24,29]);

export function makeLevelArt(id){
 const [w,h]=SIZES[id-1],[subject,title]=SUBJECTS[id-1],bridge=BRIDGES.has(id),background=subject==='drop'?'G':subject==='whale'?'C':'B';
 const free=[8,13,16,23,28,30].includes(id);
 const cells=Array.from({length:h},()=>Array(w).fill(free?'.':background)),allowed=id===1?'GBA':id===2?'GPBA':id===3?'GPBAC':id<11?'GPBACD':'GPBACDV';
 const outline=id>=4?'D':'A';
 const coordinates=(x,y)=>[(x+.5)*100/w,bridge?((y+.5)*100/h-3)/.62:(y+.5)*100/h];
 function paint(test,color,stroke=null){
  const mask=new Uint8Array(w*h);
  for(let y=0;y<h;y++)for(let x=0;x<w;x++){const [px,py]=coordinates(x,y);if(test(px,py))mask[y*w+x]=1;}
  if(stroke)for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(mask[y*w+x])for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)if(x+dx>=0&&x+dx<w&&y+dy>=0&&y+dy<h)cells[y+dy][x+dx]=stroke;
  for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(mask[y*w+x])cells[y][x]=color;
 }
 const el=(cx,cy,rx,ry,c,s=null)=>paint((x,y)=>((x-cx)/rx)**2+((y-cy)/ry)**2<=1,c,s);
 const rect=(l,t,r,b,c,s=null)=>paint((x,y)=>x>=l&&x<=r&&y>=t&&y<=b,c,s);
 function poly(points,c,s=null){paint((x,y)=>{let inside=false;for(let i=0,j=points.length-1;i<points.length;j=i++){const a=points[i],b=points[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])inside=!inside;}return inside;},c,s);}
 const leaf=(cx,cy,rx,ry,c='G',s=null)=>paint((x,y)=>{const u=(x-cx)/rx,v=(y-cy)/ry;return Math.abs(u+v)<1.25&&u*u+v*v<1.22;},c,s);
 const bloom=(cx,cy,r,c='P',center=id>=3?'C':'A')=>{for(let k=0;k<5;k++){const a=k*Math.PI*2/5-Math.PI/2;el(cx+Math.cos(a)*r*.62,cy+Math.sin(a)*r*.62,r*.52,r*.52,c);}el(cx,cy,r*.35,r*.35,center);};
 const stem=(x,top,bottom)=>rect(x-3,top,x+3,bottom,'G');
 switch(subject){
  case'leaf':poly([[15,87],[22,60],[18,37],[35,16],[80,9],[86,37],[75,62],[47,75]],'G',outline);poly([[14,90],[51,45],[71,24],[38,62],[22,94]],'G');poly([[29,48],[49,50],[69,27],[52,58],[31,70]],'B');break;
  case'drop':poly([[50,7],[42,27],[25,49],[18,68],[23,86],[39,94],[61,94],[77,84],[82,67],[74,48],[59,27]],'B');el(37,64,6,13,'G');el(43,68,3,8,'B');bloom(71,80,12,'P',outline);leaf(30,88,15,7,'G');break;
  case'flower':stem(50,44,91);leaf(32,73,18,9);leaf(70,82,18,9);bloom(50,34,27,'P','C');el(50,34,4,4,'A');break;
  case'butterfly':el(28,35,23,26,'P');el(73,35,23,26,'P');el(31,72,20,20,'P');el(70,72,20,20,'P');el(50,54,6,35,'D');poly([[48,24],[36,8],[31,11],[45,30]],'G');poly([[52,24],[64,8],[69,11],[55,30]],'G');el(27,32,9,12,'C');el(74,32,9,12,'C');el(31,72,7,8,'G');el(70,72,7,8,'G');rect(46,12,54,22,'A');break;
  case'acorn':poly([[23,35],[77,35],[74,69],[64,87],[50,96],[36,87],[26,69]],'A');el(50,34,34,15,'A');rect(45,9,54,25,'A');leaf(71,14,18,8);rect(29,31,71,38,'P');el(38,59,5,14,'C');poly([[22,40],[25,70],[35,88],[29,66],[28,40]],'D');break;
  case'mushroom':rect(42,48,60,87,'A');el(51,87,14,6,'A');paint((x,y)=>y>=14&&y<=54&&((x-50)/41)**2+((y-54)/40)**2<=1,'P',outline);rect(10,48,90,55,'P');el(31,39,6,6,'B');el(61,29,7,7,'B');el(77,44,4,4,'B');leaf(24,89,16,7);break;
  case'strawberry':poly([[15,34],[26,20],[74,20],[85,34],[80,60],[62,84],[50,95],[36,84],[20,59]],'P',outline);poly([[19,23],[35,27],[34,11],[50,23],[64,10],[65,28],[82,23],[64,39],[50,33],[36,39]],'G');el(34,49,3,5,'A');el(64,46,3,5,'A');el(48,65,3,5,'A');el(64,71,3,5,'A');break;
  case'mug':el(76,48,16,21,'A');el(77,47,9,13,'B');rect(17,23,70,79,'C',outline);el(43,79,26,10,'C',outline);rect(18,24,69,72,'C');bloom(43,48,16);rect(14,18,74,26,'A');break;
  case'letter':rect(12,25,88,80,'C',outline);poly([[12,25],[50,55],[88,25],[50,62]],'A');poly([[13,78],[39,56],[43,60],[18,80]],'A');poly([[87,78],[61,56],[57,60],[82,80]],'A');el(50,57,9,9,'P');leaf(76,13,12,7);break;
  case'fish':el(49,51,30,27,'P',outline);poly([[72,50],[93,25],[93,76]],'P',outline);poly([[38,27],[50,10],[60,29]],'P',outline);poly([[37,76],[51,91],[61,74]],'P',outline);el(32,43,6,7,'C');el(32,43,3,4,'A');poly([[13,49],[27,55],[13,61]],'B');stem(11,72,94);leaf(20,81,10,5);break;
  case'turtle':el(50,51,30,28,'A');el(49,48,26,24,'G',outline);poly([[49,27],[65,38],[63,58],[49,69],[35,57],[34,39]],'C');el(86,53,11,13,'G',outline);el(88,48,2,3,'A');el(26,78,10,8,'G',outline);el(67,78,10,8,'G',outline);poly([[20,51],[8,43],[9,58]],'G');break;
  case'owl':poly([[20,33],[19,9],[39,25],[64,25],[82,9],[80,37]],'A');el(50,56,31,35,'A');el(38,45,14,16,'C');el(64,45,14,16,'C');el(38,46,5,7,'A');el(64,46,5,7,'A');poly([[43,61],[58,61],[51,71]],'P');el(51,78,14,10,'C');rect(12,91,89,96,'A');leaf(83,84,12,6);break;
  case'umbrella':paint((x,y)=>y>=12&&y<=53&&((x-50)/40)**2+((y-52)/39)**2<=1,'P',outline);poly([[9,53],[23,44],[36,53],[50,43],[64,53],[78,44],[92,53]],'P');rect(48,49,53,85,'A');el(40,85,12,11,'A');el(41,81,7,7,'B');rect(27,71,34,84,'A');poly([[47,14],[41,34],[40,45],[46,42],[50,16]],'C');break;
  case'teapot':el(53,59,29,28,'C',outline);el(83,54,15,20,'A');el(83,54,8,13,'B');poly([[26,49],[12,36],[5,34],[7,56],[32,73]],'C',outline);el(51,32,27,7,'A');rect(45,20,58,30,'A');el(51,87,30,5,'A');bloom(52,57,14);break;
  case'cake':rect(20,41,80,83,'C',outline);rect(19,38,81,52,'P');el(50,37,31,8,'P');el(50,83,36,7,'A');rect(21,71,79,77,'P');el(50,25,10,12,'P',outline);poly([[40,17],[47,19],[50,9],[54,19],[62,15],[55,25],[46,25]],'G');bloom(50,62,15,'P','C');break;
  case'kite':poly([[50,8],[85,39],[50,71],[15,39]],'P',outline);poly([[50,8],[50,71],[54,67],[54,13]],'C');poly([[15,38],[84,38],[80,43],[21,43]],'C');poly([[49,72],[55,79],[43,89],[49,96],[45,97],[38,88],[50,79]],'A');poly([[41,80],[29,76],[31,88],[42,84],[54,79],[55,90]],'G');break;
  case'boot':poly([[24,14],[57,14],[57,58],[65,67],[79,67],[90,76],[89,87],[17,87],[17,74],[24,62]],'G',outline);rect(20,10,61,22,'C',outline);rect(16,87,91,94,'A');rect(28,28,34,60,'C');bloom(44,42,10,'P','C');break;
  case'mushroomhouse':rect(27,42,73,88,'C',outline);paint((x,y)=>y>=10&&y<=49&&((x-50)/42)**2+((y-49)/40)**2<=1,'P',outline);rect(10,42,90,50,'P');el(30,31,7,8,'C');el(61,24,8,8,'C');el(77,37,6,7,'C');rect(44,63,60,90,'A');rect(32,57,40,68,'B','A');leaf(82,84,14,6);break;
  case'flowerpot':stem(50,40,76);leaf(31,58,18,8);leaf(70,67,18,8);bloom(50,29,25);poly([[22,72],[78,72],[70,96],[30,96]],'A');rect(18,69,82,77,'C',outline);break;
  case'penguin':el(50,51,30,39,'D');el(50,57,22,29,'C');el(38,35,9,12,'C');el(64,35,9,12,'C');el(39,36,3,5,'D');el(63,36,3,5,'D');poly([[42,47],[59,47],[51,57]],'A');el(30,91,13,6,'A');el(72,91,13,6,'A');poly([[23,50],[8,71],[13,80],[29,65]],'D');poly([[78,50],[92,71],[87,80],[72,65]],'D');rect(27,62,75,70,'G');rect(65,68,74,88,'G');break;
  case'pear':el(50,66,31,27,'G','D');el(50,40,19,24,'G');rect(48,8,54,27,'A');leaf(70,17,18,8,'G','D');el(34,63,6,13,'C');break;
  case'cottage':rect(21,38,79,91,'C','D');poly([[10,42],[50,5],[90,42]],'A','D');rect(44,61,61,92,'P','D');rect(29,51,39,65,'B','D');rect(66,51,75,65,'B','D');rect(68,14,77,31,'A','D');leaf(13,80,11,8,'G');break;
  case'balloon':el(50,34,33,28,'P','D');el(50,34,12,28,'C');rect(27,64,32,83,'A');rect(69,64,74,83,'A');rect(29,81,73,96,'A','D');rect(36,86,65,90,'C');break;
  case'snail':el(55,45,29,29,'A','D');el(55,45,21,21,'C');el(55,45,13,13,'A');el(55,45,5,5,'C');el(48,84,39,9,'G','D');el(17,69,10,17,'G','D');rect(9,43,13,61,'G');rect(22,42,26,61,'G');el(11,43,4,5,'D');el(24,42,4,5,'D');break;
  case'cat':el(72,80,19,12,'A','D');el(77,76,10,6,'B');el(49,79,20,16,'A','D');el(49,80,11,12,'C');poly([[23,31],[19,7],[40,24],[64,24],[82,7],[78,33]],'A','D');el(50,43,29,24,'A','D');poly([[23,14],[34,26],[25,31]],'P');poly([[78,14],[67,26],[76,31]],'P');el(38,39,4,6,'D');el(63,39,4,6,'D');el(49,52,15,9,'C');poly([[45,47],[55,47],[50,54]],'P');rect(48,54,52,59,'D');rect(7,48,31,51,'D');rect(69,48,93,51,'D');rect(10,57,31,60,'D');rect(69,57,90,60,'D');el(35,93,12,5,'C','D');el(62,93,12,5,'C','D');rect(31,66,69,71,'G');break;
  case'sunflower':stem(50,47,94);leaf(29,77,20,9);leaf(72,87,19,9);for(let k=0;k<10;k++){const a=k*Math.PI/5-Math.PI/2,dx=Math.cos(a),dy=Math.sin(a),cx=50+dx*21,cy=35+dy*21;paint((x,y)=>(((x-cx)*dx+(y-cy)*dy)/12)**2+((-(x-cx)*dy+(y-cy)*dx)/7.5)**2<=1,'A');}el(50,35,15,15,'D');el(45,30,6,5,'C');break;
  case'whale':poly([[74,48],[88,20],[88,39],[98,29],[92,61],[75,69]],'B','D');el(45,61,35,26,'B','D');el(44,74,24,9,'C');el(31,53,3,4,'D');poly([[50,73],[68,69],[63,89],[53,84]],'B','D');stem(49,20,41);leaf(65,29,13,6);bloom(46,18,11);break;
  case'apple':el(34,57,25,32,'P','D');el(65,57,25,32,'P','D');poly([[16,52],[83,52],[76,79],[62,94],[49,88],[37,94],[23,81]],'P');rect(47,10,55,31,'A');leaf(71,18,19,8,'G','D');el(27,48,5,12,'C');break;
  case'lantern':rect(22,31,78,85,'A','D');rect(30,39,70,78,'C','D');rect(47,34,53,82,'A');rect(26,55,75,61,'A');poly([[14,32],[29,17],[70,17],[87,32]],'A','D');el(50,15,12,12,'D');el(50,14,6,7,'B');rect(20,86,80,93,'A','D');leaf(80,9,15,6);break;
  case'bunny':el(34,27,10,21,'C','D');el(68,27,10,21,'C','D');el(34,25,4,13,'P');el(68,25,4,13,'P');el(51,52,27,25,'C','D');el(51,80,22,16,'C','D');el(31,94,12,5,'C','D');el(72,94,12,5,'C','D');el(40,49,3,5,'D');el(63,49,3,5,'D');el(33,61,5,3,'P');el(70,61,5,3,'P');poly([[47,57],[56,57],[51,62]],'A');rect(49,62,53,68,'D');poly([[49,75],[32,68],[32,82],[49,78],[68,69],[68,83]],'G');break;
 }
 if(id>=11){
  if(subject==='turtle'){poly([[49,28],[63,40],[61,56],[49,66],[37,55],[36,41]],'V');}
  if(subject==='owl')el(51,78,14,10,'V');
  if(subject==='umbrella')poly([[50,14],[58,32],[62,48],[50,43]],'V');
  if(subject==='teapot')bloom(52,57,14,'V','P');
  if(subject==='kite')poly([[50,9],[83,38],[54,38]],'V');
  if(subject==='boot')bloom(44,42,10,'V','P');
  if(subject==='mushroomhouse')rect(44,63,60,90,'V');
  if(subject==='flowerpot')rect(26,79,74,88,'V');
  if(subject==='balloon')el(50,34,12,28,'V');
  if(subject==='snail')el(55,45,13,13,'V');
  if(subject==='sunflower')bloom(83,67,12,'V','P');
  if(subject==='whale')bloom(46,18,11,'V','P');
  if(subject==='lantern')rect(31,40,46,54,'V');
 }
 let rowsBottomFirst=cells.map(row=>row.join('')).reverse(),hero;
 if(bridge){
  const y=Math.floor(h*.20),mid=Math.floor(w/2);
  const grid=rowsBottomFirst.map(r=>r.split(''));
  for(let yy=0;yy<y;yy++)for(let x=0;x<w;x++)grid[yy][x]=background;
  for(let yy=2;yy<y;yy++)for(let x=2;x<w-2;x++)grid[yy][x]=x<mid-1?'G':x>mid?'A':background;
  grid[y].fill('#');
  for(let yy=y+1;yy<=y+3;yy++)for(let x=0;x<w;x++)grid[yy][x]=x<mid-1?'A':x>mid?'G':'#';
  const xs=id===20?[Math.floor(w*.25)]:[Math.floor(w*.25),Math.floor(w*.75)];
  hero={id:'sprig-bridge',choices:xs.map((x,i)=>({cell:y*w+x,anchor:(y-1)*w+x,label:i?'Правый мостик':'Левый мостик'}))};
  rowsBottomFirst=grid.map(r=>r.join(''));
 }
 for(const c of rowsBottomFirst.join(''))if(c!=='#'&&c!=='.'&&!allowed.includes(c))throw new Error(`L${id}: premature ${c}`);
 return {id,w,h,subject,title,rowsBottomFirst,entries:id===27?[Math.floor(w/3),Math.floor(w*2/3)]:[Math.floor(w/2)],...(hero?{hero}:{})};
}
