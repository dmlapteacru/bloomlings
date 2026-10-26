// Batch 0061, part 07: levels 91–95 (pirate ship on the sea, forest spirit with a lantern, knight's shield, frog under a leaf in the rain, carnival mask)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
// crop the empty rows and columns round the subject
const trim=rows=>{const r=rows.filter(l=>/[^.]/.test(l)),xs=r.flatMap(l=>[...l].map((c,i)=>c==='.'?-1:i).filter(i=>i>=0)),a=Math.min(...xs),b=Math.max(...xs);return r.map(l=>l.slice(a,b+1));};
const all=()=>-1;

function ship(){
 const W=38,H=34;
 const sea=box(19,30,30,6);
 const waves=(x,y)=>{const ph=(y===29?0:y===32?4:2);return (y===29||y===32)&&((x+ph)%8<3)?'N':(y===26&&(x+6)%8<3)?'S':'B';};
 const hull=poly([[3,17],[11,17],[11,20],[33,20],[36,17],[37,18],[32,28],[9,28],[5,24]]);
 const keel=meet(hull,box(19,27,20,1.6));
 const rail=meet(hull,box(19,20.5,20,0.5));
 const mainMast=box(17,11.5,1,9.5),foreMast=box(28,13.5,1,6.5),stern=box(6.5,14.5,1,3.5);
 const bowsprit=seg(33,19,37.5,15.5,0.75);
 const top=poly([[10,4],[24,4],[23,6],[24,10],[10,10],[11,7]]);
 const low=poly([[8,12],[26,12],[25,15],[26.5,19],[7.5,19],[9,15.5]]);
 const fore=poly([[22.5,8],[33.5,8],[32.5,12],[34,17],[22,17],[23,12.5]]);
 const flag=poly([[18,0],[26.5,0],[25,2.5],[26.5,5],[18,5]]);
 const g=raster(W,H,'.',[[all,'S'],[sea,waves],[mainMast,'E'],[foreMast,'E'],[bowsprit,'E'],[stern,'E'],
  [top,'C'],[low,'C'],[fore,'C'],[flag,'R'],[hull,'A'],[keel,'E']]);
 // patches on the sails
 put(g,'O',[[19,6],[20,6],[19,7],[20,7],[11,15],[12,15],[11,16],[12,16],[13,16],[28,13],[29,13],[28,14],[29,14]]);
 // star emblem on the flag
 put(g,'Y',[[21,1],[19,2],[20,2],[21,2],[22,2],[23,2],[20,3],[21,3],[22,3],[20,4],[22,4]]);
 // portholes
 for(const x of [13,18,23,28])put(g,'E',[[x,22],[x+1,22],[x,23],[x+1,23]]);
 put(g,'Y',[[14,22],[19,22],[24,22],[29,22]]);
 return str(g);}

function spirit(){
 const W=32,H=30;
 const ground=union(ellipse(16,31,22,5.5),box(16,29,16,1));
 const trunkL=box(1.8,15,2.2,16);
 const trunkR=box(30.2,15,2.2,16);
 const body=ellipse(14,18.5,6.5,6);
 const earL=poly([[9.5,14],[4,8],[5,7],[11,9.5],[12,13]]);
 const earR=poly([[18.5,14],[24,8],[23,7],[17,9.5],[16,13]]);
 const feet=union(ellipse(11.5,24.5,2,1.2),ellipse(16.5,24.5,2,1.2));
 const arm=seg(19,18.5,23,13.6,1);
 const handle=cut(circle(24,14.5,1.6),circle(24,14.5,0.6));
 const glow=circle(23.5,19.5,4.6);
 const lamp=box(24,19.5,2.5,3);
 const capT=box(24,16,3,1),capB=box(24,23,3,1);
 const g=raster(W,H,'.',[[all,'N'],[ground,'M'],[trunkL,'E'],[trunkR,'E'],[earL,'G'],[earR,'G'],[body,'C'],[feet,'C'],[arm,'C'],
  [handle,'D'],[lamp,'Y'],[capT,'D'],[capB,'D']]);
 put(g,'O',[[23,19],[24,19],[23,20],[24,20],[23,21],[24,21]]);
 // face
 put(g,'D',[[11,17],[11,18],[16,17],[16,18],[13,20],[14,20]]);
 // bark lines and two stars
 put(g,'D',[[2,4],[2,5],[2,6],[2,15],[2,16],[2,17],[29,7],[29,8],[29,9],[29,19],[29,20],[29,21]]);
 put(g,'Y',[[11,2],[10,3],[11,3],[12,3],[11,4],[19,4],[18,5],[19,5],[20,5],[19,6]]);
 // leaf veins
 put(g,'M',[[7,9],[8,10],[9,11],[20,9],[19,10],[18,11]]);
 return str(g);}

const star=(cx,cy,R,r,rot=-Math.PI/2)=>poly(Array.from({length:10},(_,i)=>{const a=rot+i*Math.PI/5,q=i%2?r:R;return [cx+q*Math.cos(a),cy+q*Math.sin(a)];}));
function shield(){
 const out=union(box(9.5,7,9.5,7,2.5),poly([[0,9],[19,9],[18,15],[14,20],[9.5,23.5],[5,20],[1,15]]));
 const inner=(x,y)=>out(x,y)+2;
 const g=raster(19,24,'.',[[out,'Y'],[inner,(x)=>x<9?'R':x>9?'N':'Y'],[star(9.5,10.5,6,2.6),'C']]);
 return trim(str(g));}

function frog(){
 const W=30,H=30;
 const puddle=ellipse(15,27,14,3.2);
 const leaf=cut(union(ellipse(14,10,13,7.5),poly([[24,6],[30,3.5],[27,9]])),ellipse(14,17.5,15,5.5));
 const stem=seg(14,10,15,19,0.8);
 const body=ellipse(14,23,6.5,4);
 const head=ellipse(14,19,5.5,3.2);
 const eyes=union(circle(10.5,16,2),circle(17.5,16,2));
 const belly=ellipse(14,24,3.8,2.6);
 const legs=union(ellipse(8,25.5,3,1.6),ellipse(20,25.5,3,1.6));
 const arm=seg(18,21.5,15.5,18,1);
 const rain=(x,y)=>{const sx=(Math.floor(y/4)%2)*3;return ((x+sx)%6===1&&y%4<2)?'B':'S';};
 const g=raster(W,H,'.',[[all,rain]]);
 // no rain under the leaf
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(y>=12&&x>=2&&x<=26&&g[y][x]==='B')g[y][x]='S';
 const g2=raster(W,H,'.',[[puddle,'B'],[leaf,'M'],[stem,'M'],[legs,'G'],[body,'G'],[belly,'L'],[head,'G'],[eyes,'G'],[arm,'G']]);
 g2.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.')g[y][x]=c;}));
 // leaf veins
 put(g,'G',[[14,4],[14,5],[14,6],[14,7],[14,8],[14,9],[10,6],[11,7],[12,8],[18,6],[17,7],[16,8],[6,8],[7,9],[22,8],[21,9]]);
 // eyes and mouth
 put(g,'C',[[10,15],[11,15],[10,16],[17,15],[18,15],[18,16]]);
 put(g,'D',[[11,16],[17,16],[12,20],[13,20],[14,20],[15,20],[16,20]]);
 return str(g);}

const rotE=(cx,cy,rx,ry,ang)=>{const c=Math.cos(ang),n=Math.sin(ang);return (x,y)=>{const u=(x-cx)*c+(y-cy)*n,v=-(x-cx)*n+(y-cy)*c;return ellipse(0,0,rx,ry)(u,v);};};
function mask(){
 const lobe=poly([[-1,-3],[5,1],[11,2],[16.5,2],[16.5,11],[12.5,12.5],[6,12.5],[1.5,10],[0,5]]);
 const mir=f=>(x,y)=>f(29-x,y);
 const face=cut(union(lobe,mir(lobe)),poly([[12,13.5],[14.5,10.5],[17,13.5]]));
 const holes=union(rotE(8,6.8,3.2,1.8,0.18),rotE(21,6.8,3.2,1.8,-0.18));
 const rim=cut(face,(x,y)=>face(x,y)+1.1);
 const plume=cut(rotE(28.5,-5,3.8,9,0.35),union(circle(32.6,-9,1.2),circle(32.8,-3.5,1.1),circle(24.6,-8.5,1.1)));
 const shaft=seg(26,3,31.5,-13,0.55);
 const stick=seg(4,11,0,21,1);
 const W=40,H=40,dx=4,dy=14;
 const k=1,mv=f=>(x,y)=>f((x-dx)/k,(y-dy)/k);
 const g=raster(W,H,'.',[[mv(stick),'A'],[mv(plume),'P'],[mv(meet(shaft,plume)),'K'],
  [mv(face),'V'],[mv(rim),'Y'],[mv(holes),'.'],[mv(box(14.5,5,1.2,1.2)),'R'],[mv(circle(26,3.2,1.6)),'R']]);
 return trim(str(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Пиратский корабль',forLevel:91,family:'Герои приключений',subject:'пиратский корабль с заплатанными парусами на волнах',source:'оригинальный',rows:ship()},
 {title:'Лесной дух с фонарём',forLevel:92,family:'Фольклор и легенды',direction:'Фольклорная фантазия',subject:'лесной дух с листьями-ушами и фонарём в ночном лесу',source:'оригинальный',rows:spirit()},
 {title:'Рыцарский щит',forLevel:93,family:'Фигуры, знаки и эмблемы',subject:'геральдический щит с цветком',source:'оригинальный',rows:shield()},
 {title:'Лягушка под листом',forLevel:94,family:'Времена года и погода',direction:'Перевёрнутый масштаб',subject:'лягушка под листом-зонтиком в дождь',source:'оригинальный',rows:frog()},
 {title:'Карнавальная маска',forLevel:95,family:'Маски и костюмы',subject:'карнавальная маска с пером на палочке',source:'оригинальный',rows:mask()},
]};
