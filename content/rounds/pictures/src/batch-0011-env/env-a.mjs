// Batch 0011 with environment, part A: the whale in the sea, the lighthouse on a rock between sky and sea (levels 11, 14)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from '../helpers.mjs';
// a shape drawn in base units, scaled by k and moved to (dx,dy) on the grid
const T=(k,dx,dy,f)=>(x,y)=>f((x-dx)/k,(y-dy)/k)*k;
const all=()=>-1;

// lay hand-drawn rows over a grid; '.' keeps the cell underneath
const over=(g,x0,y0,rows)=>{rows.forEach((l,j)=>[...l].forEach((c,i)=>{if(c!=='.')g[y0+j][x0+i]=c;}));return g;};

function whale(){
 const W=32,H=28,sea=11;
 // sky above the sea line, sea below with rows of short light wave dashes
 const env=(x,y)=>{if(y<sea)return 'S';return ((y-sea)%5===3&&((x+3*(y-sea))%9<3))?'S':'B';};
 const g=raster(W,H,'.',[[all,env]]);
 over(g,0,0,[
'................................',//0
'..CCCC....CCCC..................',//1
'.CCCCCC..CCCCCC.................',//2
'.CC..CCCCCC..CC.................',//3
'.CC...CCCC...CC......NNN.....NNN',//4
'......CCCC............NNN...NNNN',//5
'.......CC..............NNNNNNNN.',//6
'.......CC...............NNNNNN..',//7
'.......CC................NNNN...',//8
'......NNNNNNNN...........NNNN...',//9
'....NNNNNNNNNNNNN.......NNNN....',//10
'...NNNNNNNNNNNNNNNNN....NNNN....',//11
'..NNNNNNNNNNNNNNNNNNNN.NNNNN....',//12
'.NNNNNNNNNNNNNNNNNNNNNNNNNN.....',//13
'.NNNNNNNNNNNNNNNNNNNNNNNNNN.....',//14
'.NNNNNNNNNNNNNNNNNNNNNNNNN......',//15
'.NNNNNNNNNNNNNNNNNNNNNNNN.......',//16
'.NCCCCCCCCCCNNNNNNNNNNNN........',//17
'.CCCCCCCCCCCCCCNNNNNNNN.........',//18
'..CCCCCCCCCCCCCCCNNNNN..........',//19
'...CCCCCCCCCCCCCCCNNN...........',//20
'.....CCCCCCCCCCCCC..............',//21
'........CCCCCCC.................',//22
]);
 put(g,'D',[[5,13],[6,13],[5,14],[6,14]]);put(g,'C',[[5,13]]);put(g,'K',[[8,15],[9,15],[10,15]]);
 put(g,'N',[[9,18],[10,18],[11,18],[10,19],[11,19]]);
 return str(g);}

function lighthouse(){
 const W=27,H=35,sea=30;
 const k=0.78,dx=1.8,dy=-1.4;
 const tower=poly([[7,38],[23,38],[20.5,17],[9.5,17]]);
 const stripe=(x,y)=>Math.floor(((y+0.5-dy)/k-17)/4.2)%2===0?'R':'C';
 const rock=poly([[0,48],[2,42],[5,38.5],[10,36.5],[20,36.5],[25,38],[28.5,42],[31,48]]);
 const shade=meet(rock,poly([[19,49],[24,40],[32,40],[32,49]]));
 // two light beams from the lamp, widening to the edges of the sky
 const beams=union(poly([[15,10.8],[-4,7.5],[-4,16.5],[15,13.2]]),poly([[15,10.8],[34,7.5],[34,16.5],[15,13.2]]));
 // sky above, sea below with rows of short cream foam dashes; the last rows wash over the foot of the rock
 const env=(x,y)=>{if(y<sea)return 'S';return ((y-sea)%3===1&&((x+3*(y-sea))%7<3))?'C':'B';};
 const g=raster(W,H,'.',[[all,env],...[[beams,'Y'],[rock,'A'],[shade,'E'],[tower,stripe],[box(15,35.5,1.6,2.5,1),'D'],[box(15,25,1,1.2),'D'],[box(15,16,8.5,1),'D'],[box(15,12.5,5.5,2.5),'D'],[box(15,12.5,3.5,1.5),'Y'],[poly([[8.5,10],[21.5,10],[15,4]]),'R'],[circle(15,4.5,1.2),'D']].map(([f,c])=>[T(k,dx,dy,f),c]),[(x,y)=>y>H-2.5?-1:1,env]]);
 return str(g);}

export default {theme:'batch-0011-env',pictures:[
 {title:'Кит',forLevel:11,family:'Морские и пресноводные обитатели',subject:'голубой кит с фонтаном',source:'оригинальный',rows:whale()},
 {title:'Маяк на скале',forLevel:14,family:'Дома и архитектура',subject:'полосатый маяк на скале',source:'оригинальный',rows:lighthouse()},
]};
