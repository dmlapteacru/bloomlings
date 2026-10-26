// Batch 113: one dense full-rectangle picture for level 5 (Hard without mechanics): night sky > tree trunk > hollow > owl.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,str,put} from './helpers.mjs';

// a sprite of rows ('.' transparent) laid on the grid with its top-left cell at (x0,y0)
const sprite=(g,x0,y0,rows)=>rows.forEach((r,y)=>[...r].forEach((c,x)=>{if(c!=='.')g[y0+y][x0+x]=c;}));

function owl(){const W=24,H=24;
 const trunk=union(box(12,15.5,8.6,9.5),poly([[0.5,24.5],[3.6,20.4],[20.4,20.4],[23.5,24.5]]),
  poly([[3.4,7],[4.8,4.4],[7,5.4],[9.4,2.4],[11.8,4.2],[14.6,1.8],[16.8,3.8],[19,2.6],[20.6,4.6],[20.6,7]]));
 const g=raster(W,H,'N',[
  [trunk,'A'],
  // shadow side of the trunk and the roots
  [meet(trunk,union(box(21.4,14,1.4,12),poly([[19.6,20],[24,24.5],[18.6,24.5]]))),'E'],
  // the hollow's dark lip and the hollow itself
  [ellipse(12,14.4,7.4,8.4),'E'],
  [ellipse(12,14.4,6.2,7.2),'D'],
 ]);
 sprite(g,7,9,[
  '.OO....OO.',
  '.OOO..OOO.',
  'OOOOOOOOOO',
  'OCCCOOCCCO',
  'OCDCOOCDCO',
  'OCCCYYCCCO',
  'OOOOYYOOOO',
  'AOOCCCCOOA',
  'AOCCCCCCOA',
  'AOCCCCCCOA',
  'AOCCCCCCOA',
  '.AOCCCCOA.',
  '..YY..YY..',
 ]);
 sprite(g,1,1,['.CC.','CCCC','CCCC','.CC.']);
 const star=(x,y)=>put(g,'Y',[[x,y],[x-1,y],[x+1,y],[x,y-1],[x,y+1]]);
 star(21,1);star(1,10);star(22,12);star(1,17);
 return str(g);}

export default {theme:'batch-0113',pictures:[
 {title:'Сова в дупле',forLevel:5,family:'Птицы',subject:'сова выглядывает из дупла старого дерева ночью',source:'оригинальный',rows:owl()},
]};
