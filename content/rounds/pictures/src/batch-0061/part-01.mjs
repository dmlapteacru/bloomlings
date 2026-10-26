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
 // a tulip cup with three points, centred at (cx,cy), tilted by a (radians)
 const cup=(cx,cy,a,s=1)=>{const c=Math.cos(a),sn=Math.sin(a);const f=poly([[-2.6,-0.5],[-2.8,-4.2],[-1.3,-2.2],[0,-4.6],[1.3,-2.2],[2.8,-4.2],[2.6,-0.5],[1.8,1.8],[0,2.5],[-1.8,1.8]]);
  return (x,y)=>{const dx=(x-cx)/s,dy=(y-cy)/s;return f(dx*c+dy*sn,-dx*sn+dy*c)*s;};};
 const heads=[[15,6,0],[8.5,9,-.35],[21.5,9,.35],[4,15,-.6],[26,15,.6]];
 const stems=union(...heads.map(([x,y])=>seg(x,y+1,15,27,.8)));
 const leaves=union(poly([[14,27],[6,20],[4.5,19.5],[7,23],[13,28.5]]),poly([[16,27],[24,20],[25.5,19.5],[23,23],[17,28.5]]));
 const vase=union(circle(15,35,8.5),box(15,26.5,4.2,1.6,.6));
 const water=meet(vase,box(15,40,12,6));
 const glint=meet(cut(circle(15,35,7),circle(16.3,36.3,7)),box(10,32,4,5));
 const rim=box(15,25.6,4.8,0.9,.4);
 const colourOf=i=>i%2===0?'R':'Y';
 const layers=[[stems,'M'],[leaves,'G'],[vase,'S'],[water,'B'],[meet(vase,stems),'M'],[meet(water,stems),'M'],[rim,'B']];
 heads.forEach(([x,y,a],i)=>{layers.push([cup(x,y,a,1.15),colourOf(i)]);layers.push([meet(cup(x,y,a,1.15),poly([[x-.6,y-6],[x+.6,y-6],[x+.6,y+3],[x-.6,y+3]].map(([u,v])=>[x+(u-x)*Math.cos(a)-(v-y)*Math.sin(a),y+(u-x)*Math.sin(a)+(v-y)*Math.cos(a)]))),colourOf(i)==='R'?'P':'O']);});
 const g=draw(31,45,1.3,layers);
 return trim(str(g));}

function cherries(){
 const g=draw(20,22,1.2,[[seg(6,13,11,3.5,.85),'M'],[seg(14.5,14,11,3.5,.85),'M'],[poly([[11,3.5],[14,1],[18.5,1.2],[16,4],[12,4.5]]),'G'],[seg(12,3.8,17,1.8,.45),'M'],
  [circle(6,16,4.8),'R'],[circle(14.5,17,4.8),'R'],[circle(4.3,14.6,1.1),'C'],[circle(12.8,15.6,1.1),'C']]);
 return trim(str(g));}

function giraffe(){
 const W=34,H=37,ground=31;
 const env=(x,y)=>y>=ground?'G':'K';
 const canopy=union(ellipse(10,6.5,10,2.6),ellipse(6,5,5.5,2),ellipse(14,4.6,6,2.1),ellipse(3,7.2,4,1.6));
 const under=meet(canopy,box(10,9,12,1.6));
 const trunk=union(seg(7,33,8,16,1.2),seg(8,16,5,8,.8),seg(8,17,12,8,.8),seg(8,22,8,8,.7));
 const body=ellipse(24,22.5,6.5,4);
 const neck=poly([[17.8,22.5],[21,20.5],[15.2,8.5],[13,9.7]]);
 const head=union(ellipse(13.5,9,3,2),ellipse(11,10.2,2.2,1.6));
 const horns=union(box(13.8,6.3,.5,1.2),box(15.4,6.6,.5,1.2));
 const legs=union(box(19.5,28,.95,4.6),box(22,28,.95,4.6),box(26.5,28,.95,4.6),box(29,28,.95,4.6));
 const hooves=union(box(19.5,33,.95,.6),box(22,33,.95,.6),box(26.5,33,.95,.6),box(29,33,.95,.6));
 const tail=seg(30.3,21,32,27,.5);
 const gir=union(body,neck,head,legs);
 const spot=(x,y)=>{const i=Math.floor(x/3),j=Math.floor(y/3);return ((i+j)%2===0&&x%3<2&&y%3<2)?'A':'Y';};
 const g=raster(W,H,'.',[[all,env],[trunk,'E'],[canopy,'G'],[under,'M'],[tail,'E'],[gir,spot],[horns,'E'],[hooves,'E']]);
 put(g,'D',[[13,8]]);
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
 const body=union(box(cx,18,1,8.5,1),circle(cx,8.5,1.6));
 const ant=union(seg(16.4,7.5,13,2.5,.5),seg(17.6,7.5,21,2.5,.5));
 const tips=union(circle(12.7,2.2,1),circle(21.3,2.2,1));
 const g=draw(34,29,1.15,[[mir(cx,up),'N'],[mir(cx,upIn),'B'],[mir(cx,dot),'Y'],[mir(cx,tri),'P'],[mir(cx,lo),'V'],[mir(cx,loIn),'P'],[mir(cx,dot2),'Y'],[body,'D'],[ant,'D'],[tips,'Y']]);
 return trim(str(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Щенок с косточкой',forLevel:61,family:'Домашние животные',subject:'весёлый щенок сидит с косточкой, висячие уши, виляющий хвост',source:'оригинальный',rows:puppy()},
 {title:'Букет тюльпанов в вазе',forLevel:62,family:'Цветы и букеты',subject:'пять тюльпанов двух цветов в круглой стеклянной вазе с листьями',source:'оригинальный',rows:tulips()},
 {title:'Вишенки',forLevel:63,family:'Фрукты и ягоды',subject:'пара вишенок на сросшихся черешках с листиком и бликом',source:'оригинальный',rows:cherries()},
 {title:'Жираф у дерева',forLevel:64,family:'Саванна и джунгли',subject:'жираф тянется к листьям акации, тёплое небо и трава',source:'оригинальный',rows:giraffe()},
 {title:'Бабочка',forLevel:65,family:'Насекомые и маленькие существа',direction:'Геометрическая фантазия',subject:'бабочка с симметричными узорными крыльями из кругов и треугольников',source:'оригинальный',rows:butterfly()},
]};
