// v9 changes only four pictures; all other authored boards remain v8E controls.
// Rasterized at the actual cell grid, without filtering/downsampling or tiny decorative colors.
const oval=(cx,cy,rx,ry)=>(x,y)=>((x-cx)/rx)**2+((y-cy)/ry)**2<=1;
const box=(l,t,r,b)=>(x,y)=>x>=l&&x<=r&&y>=t&&y<=b;
const either=(...shapes)=>(x,y)=>shapes.some(f=>f(x,y));
const diamond=(cx,cy,rx,ry)=>(x,y)=>Math.abs((x-cx)/rx)+Math.abs((y-cy)/ry)<=1;
function raster(w,h,bg,layers){return Array.from({length:h},(_,y)=>Array.from({length:w},(_,x)=>{
 let c=typeof bg==='function'?bg(x,y):bg;
 for(const p of layers)if(p.shape(x+.5,y+.5)){
  const rim=p.outline&&[[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy])=>!p.shape(x+.5+dx,y+.5+dy));
  c=rim?p.outline:typeof p.color==='function'?p.color(x,y):p.color;
 }
 return c;
}).join(''));}
export function applyV9Scenes(S,BARE,BOARD){
 // L4: one readable butterfly, no 3x workload jump immediately after the ice cream.
 S[4]={title:'Бабочка',grid:raster(18,20,'.',[
  {shape:either(oval(4.8,6.8,4.2,5.2),oval(13.2,6.8,4.2,5.2),oval(5.7,13.6,3.7,4.1),oval(12.3,13.6,3.7,4.1)),color:(x,y)=>y<11?'P':'B',outline:'D'},
  ...[4.8,13.2].flatMap(x=>[{shape:oval(x,6.8,2.5,2.8),color:'Y'},{shape:oval(x,6.8,1.1,1.4),color:'C'}]),
  {shape:box(8,4,10,17),color:'D'},{shape:oval(9,3.9,1.5,1.6),color:'D'},
  {shape:(x,y)=>y<3&&((Math.abs(x-(7-y))<.65)||(Math.abs(x-(11+y))<.65)),color:'D'},
 ])};BARE.add(4);
 // L12: large pale facial discs, strong eyes, readable wings; two quiet sky zones rather than >50% single night color.
 S[12]={title:'Сова при луне',grid:raster(24,26,(x,y)=>y<12?'N':'B',[
  {shape:oval(20,3.5,2.5,2.5),color:'C'},
  {shape:oval(21.2,2.8,2.2,2.3),color:'N'},
  {shape:either(oval(12,16.5,9.4,8.7),diamond(6,4.7,3.3,4),diamond(18,4.7,3.3,4)),color:'A',outline:'D'},
  {shape:oval(12,13.2,9.1,8),color:'A',outline:'D'},
  {shape:either(oval(8,11,4.1,4.8),oval(16,11,4.1,4.8)),color:'C'},
  ...[8,16].flatMap(x=>[{shape:oval(x,11,1.8,2.4),color:'D'},{shape:oval(x-.5,10,0.7,0.8),color:'C'}]),
  {shape:diamond(12,15.5,1.8,2.2),color:'Y'},
  {shape:oval(12,20.3,5,4.2),color:'C'},
  {shape:either(oval(4.4,18,2.3,5.5),oval(19.6,18,2.3,5.5)),color:'D'},
  {shape:either(box(6,24,18,25),box(8,23,10,25),box(14,23,16,25)),color:'A'},
  // Lime arrives at L12: one coherent leaf cluster, not isolated speckles.
  {shape:either(oval(2.5,23.5,2.2,1.5),oval(21.5,23.5,2.2,1.5)),color:'L'},
 ])};
 // L13: larger hooked handle, solid canopy panels; no tiny flowers/rainbow noise. Bark introduced in handle.
 S[13]={title:'Зонтик в луже',grid:raster(22,26,(x,y)=>y<9?'N':'B',[
  {shape:(x,y)=>x>1&&x<21&&y>3&&y<13&&((x-11)/10)**2+((y-12)/9)**2<=1,color:(x,y)=>x<7?'R':x<15?'K':'R',outline:'D'},
  // Pearl occurs outside as rain AND behind the canopy: a readable repeated-color dependency.
  ...[[5.8,9.5],[11,7],[17,10]].map(([x,y])=>({shape:oval(x,y,1.35,1.35),color:'C'})),
  {shape:box(10,12,12,22),color:'E'},
  {shape:(x,y)=>oval(13,21.7,3,3)(x,y)&&y>21.3&&!oval(13,21.2,1.2,1.8)(x,y),color:'E'},
  {shape:oval(11,24.5,9,1.3),color:'S'},
  // A few broad two-cell rain drops read at phone scale.
  ...[[2,15],[18,16],[4,19]].map(([x,y])=>({shape:box(x,y,x+1,y+2),color:'C'})),
 ])};
 // L16 stays a relief, but a larger, symmetric kite now has a readable string and bows.
 S[16]={title:'Воздушный змей',grid:raster(24,30,'.',[
  {shape:diamond(12,11.7,10.7,10.6),color:(x,y)=>(x<12)===(y<12)?'Y':'R',outline:'D'},
  {shape:(x,y)=>diamond(12,11.7,10.7,10.6)(x,y)&&(Math.abs(x-12)<.6||Math.abs(y-12)<.6),color:'C'},
  {shape:(x,y)=>y>=22&&y<30&&Math.abs(x-(12+Math.sin((y-22)*.75)*1.5))<.7,color:'D'},
  ...[[13,25],[11,28]].map(([x,y])=>({shape:either(diamond(x-1.2,y,1.5,1),diamond(x+1.2,y,1.5,1)),color:'P'})),
 ])};BARE.add(16);
}
