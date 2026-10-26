// Batch 0061, part 01: levels 61–65 (puppy with a bone, tulips in a vase, cherries, giraffe at an acacia, geometric butterfly)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
// crop the empty rows and columns round the subject
const trim=rows=>{const r=rows.filter(l=>/[^.]/.test(l)),xs=r.flatMap(l=>[...l].map((c,i)=>c==='.'?-1:i).filter(i=>i>=0)),a=Math.min(...xs),b=Math.max(...xs);return r.map(l=>l.slice(a,b+1));};
// stamp hand-drawn rows onto a grid at (x0,y0); '.' leaves the cell as it is
const stamp=(g,x0,y0,rows)=>{rows.forEach((l,j)=>[...l].forEach((c,i)=>{if(c!=='.')g[y0+j][x0+i]=c;}));return g;};
// mirror a shape round the vertical line x=cx (the left half is drawn)
const mir=(cx,f)=>(x,y)=>f(cx-Math.abs(x-cx),y);
const all=()=>-1;

function puppy(){
 // the left half by hand, mirrored; the shading, the tail and the bone are added after
 const half=[
"..........OOOOOOO",
"....EEEE.OOOOOOOO",
"...EEEEEOOOOOOOOO",
"..EEEEEEOOOOOOOOO",
"..EEEEEEOOOOOOOOO",
"..EEEEEOOOOOOOOOO",
"..EEEEEOOOODDOOOO",
"..EEEEEOOOODDOOOO",
"..EEEEEOOOOOOOOOO",
"..EEEEEOOOOOOCCCC",
"...EEEEOOOOOCCCDD",
"...EEEEOOOOCCCCCD",
"...EEEE.OOOCCCCCD",
"....EE...OOCCDDDP",
".........OOOCCCPP",
".........RRRRRRRR",
".........RRRRRRRR",
"........OOOOOOCCC",
".......OOOOOOOCCC",
"......OOOOOOOOCCC",
"......OOOOOOOOCCC",
"......OOOOAOOOCCC",
".....OOOOOAOOOCCC",
".....OOOOOAOOOCCC",
"....OOOOOOAOOOCCC",
"....OOOOOOAOOOCCC",
"....OOOOOOAOOOAAA",
"....OOOOOOAOOOAAA",
"...OOOOOOOAOOOAAA",
"...OOOOOOACCCCAAA",
"....OOOOOACCCCAAA",
];
 const W=37,g=half.map(r=>[...(r+[...r].reverse().join('')+'...')]);
 while(g.length<36)g.push(Array(W).fill('.'));
 put(g,'C',[[11,6],[21,6]]);
 // the far side of the body in shadow
 for(let y=19;y<=30;y++){const row=g[y];let n=0;for(let x=W-1;x>=17&&n<2;x--)if(row[x]==='O'){row[x]='A';n++;}else if(n>0)break;}
 // the wagging tail with a cream tip
 const tail=union(seg(27,25,32,19.5,1.5),seg(32,19.5,33.5,15,1.4));
 for(let y=0;y<36;y++)for(let x=0;x<W;x++)if(g[y][x]==='.'&&tail(x+.5,y+.5)<0)g[y][x]=y<17?'C':'O';
 stamp(g,0,30,[
"......DDDD..............DDDD......",
".....DCCCCDDDDDDDDDDDDDDCCCCD.....",
".....DCCCCCCCCCCCCCCCCCCCCCCD.....",
".....DCCCCCCCCCCCCCCCCCCCCCCD.....",
".....DCCCCDDDDDDDDDDDDDDCCCCD.....",
"......DDDD..............DDDD......"]);
 return trim(str(g));}

function tulips(){
 // a tulip head: the front petal in the main colour, the two side petals behind it in the second colour
 const head=(c,d)=>[
"dd.cc.dd",
"dddccddd",
"ddccccdd",
"dccccccd",
"dccccccd",
"dccccccd",
"dccccccd",
".cccccc.",
"..cccc.."].map(l=>l.replace(/c/g,c).replace(/d/g,d));
 // axis x=19; heads by their centre column and top row
 const heads=[[19,0,'R','P'],[10,4,'Y','O'],[28,4,'Y','O'],[4,12,'R','P'],[34,12,'R','P']];
 const stems=union(...heads.map(([x,y])=>seg(x,y+8,19,26,1)));
 const leaves=union(poly([[18,26.5],[11,21.5],[6.5,21.8],[9.5,25.5],[16.5,28.5]]),poly([[20,26.5],[27,21.5],[31.5,21.8],[28.5,25.5],[21.5,28.5]]));
 const vase=union(ellipse(19,35,10.6,8.4),box(19,26,4.2,1.8,.6));
 const water=meet(vase,box(19,40,12,5));
 const rim=box(19,25,5,1,.4);
 const g=raster(38,44,'.',[[stems,'M'],[leaves,'G'],[vase,'S'],[water,'B'],[meet(vase,stems),'M'],[rim,'B']]);
 for(const [x,y,c,d] of heads)stamp(g,x-4,y,head(c,d));
 return trim(str(g));}

function cherries(){
 const g=draw(20,22,1.2,[[seg(6,13,11,3.5,.85),'M'],[seg(14.5,14,11,3.5,.85),'M'],[poly([[11,3.5],[14,1],[18.5,1.2],[16,4],[12,4.5]]),'G'],[seg(12,3.8,17,1.8,.45),'M'],
  [circle(6,16,4.8),'R'],[circle(14.5,17,4.8),'R'],[circle(4.3,14.6,1.1),'C'],[circle(12.8,15.6,1.1),'C']]);
 return trim(str(g));}

function giraffe(){
 const W=34,H=37,ground=31;
 // warm blush sky over a strip of grass
 const env=(x,y)=>y>=ground?'G':'K';
 // a flat acacia crown, dark underneath
 const canopy=union(ellipse(8.5,6.2,8.8,2.4),ellipse(5,4.8,5.5,2),ellipse(12,4.4,5.5,2),ellipse(1.5,6.8,3,1.5));
 const under=meet(canopy,box(9,8.8,13,1.4));
 const trunk=union(seg(7,33,7.5,18,1.2),seg(7.5,18,4,7,.8),seg(7.5,19,12,7,.8));
 const body=ellipse(25.5,21,7,4.2);
 const neck=poly([[19,21.5],[23.8,18.5],[23.8,5.5],[20.5,6]]);
 const head=union(ellipse(21.5,5.3,3,2),ellipse(18.3,6.4,2.4,1.7));
 const horns=union(box(20.5,2.2,.6,1.4),box(22.5,2.4,.6,1.4));
  const legX=[20,23,28,31];
 const legs=union(...legX.map(x=>box(x,28.5,1,5.6)));
 const hooves=union(...legX.map(x=>box(x,34,1,.6)));
 const tail=seg(32.3,19.5,33.5,26,.5);
 const gir=union(body,neck,legs);
 // a net of patches on a 3-cell lattice, away from the edges
 const spot=(x,y)=>(x%3<2&&y%3<2&&y<25&&gir(x+.5,y+.5)<-.5)?'A':'Y';
 const g=raster(W,H,'.',[[all,env],[trunk,'E'],[canopy,'G'],[under,'M'],[tail,'E'],[circle(33.5,26.5,.9),'E'],[gir,spot],[head,'Y'],[horns,'E'],[hooves,'E']]);
 put(g,'Y',[[23,3],[24,3],[25,3],[26,3],[24,4],[25,4]]);put(g,'D',[[21,4]]);
 return str(g);}

function butterfly(){
 const cx=17;
 const up=poly([[16,12],[4,1.5],[1,3.5],[1.5,13],[6,16.5],[16,15]]);
 const upIn=poly([[14.5,12],[5,4],[3,5],[3.5,12.5],[7,15],[14.5,14]]);
 const lo=poly([[16,15.5],[6,17.5],[3.5,22],[6,27],[11,27.5],[16,20]]);
 const loIn=poly([[14.5,16.8],[7,18.8],[5.5,22],[7,25.5],[10.5,25.8],[14.5,19.8]]);
 const dot=circle(7.5,8.5,2.6);
 const tri=poly([[10,13.5],[13.5,13.5],[11.8,9.8]]);
 const dot2=circle(9,22.5,1.8);
 const dot3=circle(4.6,12.3,1.3);
 const tri2=poly([[9.5,19],[13.5,19],[13.5,23]]);
 const body=union(box(cx,18,1,8.5,1),circle(cx,8.5,1.6));
 const ant=union(seg(16.4,7.5,13,2.5,.5),seg(17.6,7.5,21,2.5,.5));
 const tips=union(circle(12.7,2.2,1),circle(21.3,2.2,1));
 const g=draw(34,29,1.15,[[mir(cx,up),'N'],[mir(cx,upIn),'B'],[mir(cx,dot),'Y'],[mir(cx,tri),'P'],[mir(cx,lo),'V'],[mir(cx,loIn),'P'],[mir(cx,dot2),'Y'],[mir(cx,dot3),'P'],[mir(cx,tri2),'V'],[body,'D'],[ant,'D'],[tips,'Y']]);
 return trim(str(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Щенок с косточкой',forLevel:61,family:'Домашние животные',subject:'весёлый щенок сидит с косточкой, висячие уши, виляющий хвост',source:'оригинальный',rows:puppy()},
 {title:'Букет тюльпанов в вазе',forLevel:62,family:'Цветы и букеты',subject:'пять тюльпанов двух цветов в круглой стеклянной вазе с листьями',source:'оригинальный',rows:tulips()},
 {title:'Вишенки',forLevel:63,family:'Фрукты и ягоды',subject:'пара вишенок на сросшихся черешках с листиком и бликом',source:'оригинальный',rows:cherries()},
 {title:'Жираф у дерева',forLevel:64,family:'Саванна и джунгли',subject:'жираф тянется к листьям акации, тёплое небо и трава',source:'оригинальный',rows:giraffe()},
 {title:'Бабочка',forLevel:65,family:'Насекомые и маленькие существа',direction:'Геометрическая фантазия',subject:'бабочка с симметричными узорными крыльями из кругов и треугольников',source:'оригинальный',rows:butterfly()},
]};
