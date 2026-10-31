// SVG drafts of batch 0168, group A: «Герои мультсериалов» (the owner, 2026-10-31: characters from cartoons). Every
// picture is a franchise reference (`source: 'сторонняя вселенная'`), not for release until the owner confirms the rights.
// Drawn in cell units: the viewBox is the intended grid, so `cells` is the coloured cell count at that grid.
// Convert: node tools/rounds/svg2cells.mjs content/rounds/pictures/src/drafts-0168-A.svg.mjs --out
//   content/rounds/pictures/src/batch-0168-A.mjs --sheet content/work/tmp/pics/A/sheet.png --level 61
import {C} from '../../../../tools/rounds/svg-palette.mjs';
// a silhouette in one colour with a one-cell rim of the same colour round it (drawn under the coloured parts: an outline)
const rim=(col,shapes,w=2)=>`<g fill="${col}" stroke="${col}" stroke-width="${w}" stroke-linejoin="round" stroke-linecap="round">${shapes}</g>`;
const fill=(col,shapes)=>`<g fill="${col}">${shapes}</g>`;
const line=(col,d,w=2)=>`<path d="${d}" fill="none" stroke="${col}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"/>`;
const svg=(w,h,body)=>`<svg viewBox="0 0 ${w} ${h}" xmlns="http://www.w3.org/2000/svg">${body}</svg>`;
const ref=(universe,name)=>({source:'сторонняя вселенная',reference:`${universe} — ${name}`});
// a pixel layer: ops in order; a block {x,y,rows} paints its non-'.' cells ('_' erases), {mirror:true} doubles each row
// (left half + its reverse, {odd:true} shares the middle column), {outline:'D'} rings every painted cell with an
// eight-neighbour rim on empty cells (so the rim stays one connected line); the result is flat one-cell rects, one per run of a row
const pix=(w,h,ops)=>{const g=Array.from({length:h},()=>Array(w).fill('.'));
 for(const o of ops){if(o.outline){const add=[];for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(g[y][x]==='.'&&[[1,0],[-1,0],[0,1],[0,-1],[1,1],[1,-1],[-1,1],[-1,-1]].some(([dx,dy])=>{const c=g[y+dy]?.[x+dx];return c&&c!=='.';}))add.push([x,y]);for(const [x,y] of add)g[y][x]=o.outline;continue;}
  const rows=o.mirror?o.rows.map(r=>r+[...r].reverse().join('').slice(o.odd?1:0)):o.rows;
  rows.forEach((r,j)=>[...r].forEach((c,i)=>{const y=(o.y||0)+j,x=(o.x||0)+i;if(c!=='.'&&c!==' '&&y<h&&x<w&&y>=0&&x>=0)g[y][x]=c==='_'?'.':c;}));}
 let s='';g.forEach((r,y)=>{let x=0;while(x<w){const c=r[x];if(c==='.'){x++;continue;}let e=x;while(e<w&&r[e]===c)e++;s+=`<rect x="${x}" y="${y}" width="${e-x}" height="1" fill="${C[c]}"/>`;x=e;}});return s;};

// 1 Gumball: a blue cat with huge touching eyes, a white muzzle, an open smile, a tail; a yellow room with a wooden floor
const gumball=svg(28,34,`<rect width="28" height="34" fill="${C.Y}"/><rect y="31" width="28" height="3" fill="${C.A}"/>${pix(28,34,[{rows:[
 '............................',
 '............................',
 '......BB............BB......',
 '......BBB..........BBB......',
 '.....BBBBB........BBBBB.....',
 '.....BBBBBB......BBBBBB.....',
 '....BBBBBBBBBBBBBBBBBBBB....',
 '....BBBBBCCCCBBCCCCBBBBB....',
 '...BBBBBCCCCCCCCCCCCBBBBB...',
 '...BBBBCCCCCCCCCCCCCCBBBB...',
 '...BBBBCCCCCCCCCCCCCCBBBB...',
 '...BBBBCCCDDCCCCDDCCCBBBB...',
 '...BBBBCCCDDCCCCDDCCCBBBB...',
 '...BBBBCCCDDCCCCDDCCCBBBB...',
 '...BBBBCCCCCCCCCCCCCCBBBB...',
 '...BBBBBCCCCCCCCCCCCBBBBB...',
 '...BBBBBBCCCCBBCCCCBBBBBB...',
 '...BBBBBBBBBBBBBBBBBBBBBB...',
 '...BBBBBBBBCCCCCCBBBBBBBB...',
 '...BBBBBBBCCCDDCCCBBBBBBB...',
 '...BBBBBBBCCCDDCCCBBBBBBB...',
 '....BBBBBBBCCCCCCBBBBBBB....',
 '....BBBBBBDDDDDDDDBBBBBB....',
 '.....BBBBBDPPPPPPDBBBBB.....',
 '......BBBBBDPPPPDBBBBB......',
 '.......BBBBBDDDDBBBBB.......',
 '.........BBBBBBBBBB.........',
 '.......BBBBBBBBBBBBBB.......',
 '......BB.BBBBBBBBBB.BB..BB..',
 '......BB.BBBBBBBBBB.BB..BB..',
 '......BB.BBBBBBBBBB.BB.BB...',
 '.........BBBB..BBBBBBBBB....']},{outline:'D'}])}`);

// 2 Jake: a yellow dog with big jowls split by a seam, a black nose, round eyes, floppy ears; sky and grass
const jake=svg(32,27,`<rect width="32" height="27" fill="${C.S}"/><rect y="21" width="32" height="6" fill="${C.G}"/>${pix(32,27,[
 {y:17,rows:[
 '..........YYYYYYYYYYYY..........',
 '.........YYYYYYYYYYYYYY.........',
 '.........YYYYYYYYYYYYYY.........',
 '.........YYYYYYYYYYYYYY...YY....',
 '.........YYYYYYYYYYYYYY..YY.....',
 '.........YYYYYYYYYYYYYYYYY......',
 '..........YYY......YYY..........',
 '..........YYY......YYY..........']},
 {y:3,mirror:true,rows:[
 '..........YYYYYY',
 '........YYYYYYYY',
 '...OOOOYYYYYYYYY',
 '..OOOOOYYYCCCYYY',
 '..OOOOOYYCCCCCYY',
 '..OOOOOYYCCDDCYY',
 '..OOOOOYYCCDDCYY',
 '..OOOOOYYCCCCCYY',
 '..OOOOOYYYCCCYYD',
 '..OOOOOYYYYYYYDD',
 '..OOOOOYYYYYYYYD',
 '...OOOYYYYYYYYYA',
 '...OOOYYYYYYYYYA',
 '....OOYYYYYYYYYA',
 '......YYYYYYYYYA',
 '.......YYYYYYYAA',
 '........AAAAAA..']},{outline:'A'}])}`);

// 3 Ice King: a blue face framed by long white hair and beard, a long drooping nose, a gold crown with red gems; icy sky
const iceKing=svg(32,38,`<rect width="32" height="38" fill="${C.S}"/>
 ${fill(C.N,`<path d="M1 38 L2 27 Q3 21 9 20 L23 20 Q29 21 30 27 L31 38 Z"/>`)}
 ${fill(C.C,`<rect x="3" y="20" width="4" height="9" rx="1.5"/><rect x="25" y="20" width="4" height="9" rx="1.5"/><path d="M6 21 L26 21 Q25.5 30 16 36.5 Q6.5 30 6 21 Z"/>`)}
 ${pix(32,38,[{mirror:true,rows:[
 '................',
 '...............Y',
 '........Y.....YY',
 '........YY...YYR',
 '.......YRR..YYYR',
 '.......YRRYYYYYY',
 '.......YYYYYYYYY',
 '.......YYYYYYYYY',
 '....CCBBBBBBBBBB',
 '....CCBBBBBBBBBB',
 '....CCBBBCCCCBBB',
 '....CCBBBCDDCBBB',
 '....CCBBBCDDCBBB',
 '....CCBBBBCCBBNB',
 '....CCBBBBBBBNBB',
 '....CCCBBBBBBNBB',
 '....CCCCCCCCCNBB',
 '....CCCCCCCCCNBB',
 '....CCCCCCCCCNBB',
 '....CCCCCCCCCCNB',
 '....CCCCCCCCCCCN',
 '....CCCCCCCCCCCC']}])}`);

// 4 Mordecai: a blue jay with a swept crest, big white eyes, a wide black beak, a white belly; sunset and grass
const mordecai=svg(26,34,`<rect width="26" height="34" fill="${C.O}"/><rect y="30" width="26" height="4" fill="${C.G}"/>${pix(26,34,[{rows:[
 '..........................',
 '...............BB.........',
 '..............BBBB........',
 '.............BBBBB........',
 '........BBBBBBBBBB........',
 '.......BBBBBBBBBBBB.......',
 '......BBBCCCBBCCCBBB......',
 '......BBCCCCCCCCCCBB......',
 '......BBCCDDCCDDCCBB......',
 '......BBCCDDCCDDCCBB......',
 '......BBCCCCCCCCCCBB......',
 '......BBBCCCDDCCCBBB......',
 '......BBDDDDDDDDDDBB......',
 '......BBBDDDDDDDDBBB......',
 '.......BBBDDDDDDBBB.......',
 '........BBBDDDDBBB........',
 '.........BBBBBBBB.........',
 '.........BBBBBBBB.........',
 '........BBBCCCCBBB........',
 '......BBBBCCCCCCBBBB......',
 '.....BBBBBCCCCCCBBBBB.....',
 '.....BB.BBCCCCCCBB.BB.....',
 '.....BB.BBCCCCCCBB.BB.....',
 '.....BB.BBCCCCCCBB.BB.....',
 '.....NN.BBCCCCCCBB.NN.....',
 '.....NN.BBBCCCCBBB.NN.....',
 '........BBBBBBBBBB........',
 '..........BBBBBB..........',
 '..........DD..DD..........',
 '..........DD..DD..........',
 '........DDDD..DDDD........']}])}`);

// 5 Blossom: a huge head with giant pink-iris eyes, a red bow, long orange hair, a pink dress with a black belt
const blossom=svg(28,32,`<rect width="28" height="32" fill="${C.S}"/><rect y="29" width="28" height="3" fill="${C.G}"/>${pix(28,32,[{mirror:true,rows:[
 '..............',
 '...RRR........',
 '...RRRRR....RR',
 '...RRRRRRRRRRR',
 '...RRRRRRRRRRR',
 '...RRRRR.OORRR',
 '....OOOOOOOOOO',
 '...OOOOOOOOOOO',
 '..OOOOOOOOOOOO',
 '..OOOOOOOOOOOO',
 '..OOKKKKKKKKKK',
 '.OOOKKKCCCCKKK',
 '.OOOKKCCCCCCKK',
 '.OOOKCCCPPPPCK',
 '.OOOKCCPPPPPCK',
 '.OOOKCCPDDPPCK',
 '.OOOKCCPDDPPCK',
 '.OOOKCCPPPPPCK',
 '.OOOKKCCPPPCKK',
 '.OOOKKKCCCCKKK',
 '.OOOKKKKKKKKDD',
 '.OOOOKKKKKKKKK',
 '.OOOOOOKKKKKKK',
 '.OOOOOOOOKPPPP',
 '.OOOOOOOKKPPPP',
 '.OOOOOOOKKDDDD',
 '..OOOOOOOPPPPP',
 '..OOOOOOPPPPPP',
 '...OOOOO..CC..',
 '..........DDD.']}])}`);

// 6 Ben 10: brown hair, green eyes, a white T-shirt with a black stripe, the raised fist with the green Omnitrix; night
const ben=svg(28,30,`<rect width="28" height="30" fill="${C.N}"/>${pix(28,30,[{rows:[
 '............................',
 '............................',
 '..........AAAAAA............',
 '........AAAAAAAAAA..........',
 '.......AAAAAAAAAAAA.........',
 '.......AAAAAAAAAAAAA........',
 '.......AAAAAAAAKKAAA........',
 '.......AAAAAAKKKKKAA.KKKK...',
 '.......AAAAAKKKKKKKA.KKKK...',
 '.......AAKAKKKKKKKKA.KKKK...',
 '.......AKKKKKKKKKKKA.DDDD...',
 '.......KKCCCKKKCCCKKDGGGGD..',
 '.......KKCGGKKKGGCKKDDGGDD..',
 '.......KKCGGKKKGGCKKDGGGGD..',
 '........KKKKKKKKKKK..DDDD...',
 '........KKKKKKKKKKK..KKK....',
 '........KKKDDDDDKKK..KKK....',
 '.........KKKKKKKKK...KKK....',
 '...........KKKKK.....KKK....',
 '.......CCCCCDDDCCCCC.CCCC...',
 '.....CCCCCCCDDDCCCCCCCCCC...',
 '....CCCCCCCCDDDCCCCCCCCCC...',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....',
 '...CCCCCCCCCDDDCCCCCCCCC....']}])}`);

// 7 Bugs Bunny: grey rabbit with long pink-lined ears, white muzzle, a carrot in the white glove, out of his burrow
const bugs=svg(30,42,`<rect width="30" height="42" fill="${C.M}"/><rect y="35" width="30" height="7" fill="${C.G}"/>
 ${fill(C.A,`<ellipse cx="15" cy="36.5" rx="11.5" ry="3.5"/>`)}${fill(C.E,`<ellipse cx="15" cy="35.6" rx="8" ry="1.8"/>`)}
 ${pix(30,42,[{rows:[
 '..............................',
 '..........SS......SS..........',
 '.........SSSS....SSSS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SKKS....SKKS.........',
 '.........SSSS....SSSS.........',
 '........SSSSSSSSSSSSSS........',
 '.......SSSSSSSSSSSSSSSS.......',
 '.......SSSCCCSSSSCCCSSS.......',
 '......SSSCCCCSSSSCCCCSSS......',
 '......SSSCCDDSSSSDDCCSSS......',
 '......SSSCCDDSSSSDDCCSSS......',
 '......SSSSSSSSDDSSSSSSSS......',
 '......SSCCCCCCDDCCCCCCSS......',
 '......SCCCCCCCCCCCCCCCCS......',
 '......SCCCDDDDDDDDDDCCCS......',
 '.......SCCCCCCCCCCCCCCS.......',
 '........SSCCCCCCCCCCSS........',
 '..........SSSSSSSSSS..........',
 '.........SSSSSSSSSSSS.........',
 '........SSSSCCCCCCSSSS........',
 '........SSSCCCCCCCCSSS........',
 '........SSSCCCCCCCCSSS........',
 '........SSSCCCCCCCCSSS........',
 '........SSSCCCCCCCCSSS........',
 '........SSSCCCCCCCCSSS........',
 '........SSSCCCCCCCCSSS........',
 '........SSSCCCCCCCCSSS........']},
 {x:0,y:20,rows:[
 '..G.......',
 '.GGG.O....',
 '..GOOOOO..',
 '.GGOOOOOOO',
 '...OCCCO..',
 '....CCCC..',
 '....CCCS..',
 '.....SSSSS',
 '......SSSS']}])}
 ${fill(C.A,`<path d="M4.5 36.6 Q15 38.6 25.5 36.6 L26.5 38.2 Q15 41.2 3.5 38.2 Z"/>`)}`);

// 8 Taz: a brown Tasmanian devil, angry brows, the huge open mouth with square teeth and a red tongue; desert
const taz=svg(32,28,`<rect width="32" height="28" fill="${C.S}"/><rect y="21" width="32" height="7" fill="${C.Y}"/>${pix(32,28,[{mirror:true,rows:[
 '................',
 '.....AA.........',
 '.....AAA........',
 '......AAA.AAAAAA',
 '......AAAAAAAAAA',
 '.AA..AAEEEAAAAAA',
 '.AA.AAAAAEEEEAAA',
 '..AAAAAACCCCAAAA',
 '...AAAAACCDDAAAA',
 '...AAAAACCDDAAAD',
 '...AAAAAACCAADDD',
 '...AAAAAACCCCCDD',
 '..AAAAACCCCCCCCC',
 '..AAAACDDDDDDDDD',
 '.AAAAACDCCDCCDCC',
 '.AAAAACDCCDCCDCC',
 '.AAAAACDDDDDDDDD',
 '.AAAAACDDDDDDDRR',
 '..AAACDDCCDDRRRR',
 '..AAACCDCCDDDDDD',
 '..AAAACCCCCCCCCC',
 '...AAAACCCCCCCCC',
 '....AAAACCCCCCCC',
 '.....AAAAAAAAAAA',
 '......AAAA......',
 '.....EEEEE......']}])}`);

// 9 Tweety: a yellow canary, the huge round head, big blue eyes, an orange beak and feet
const tweety=svg(20,25,pix(20,25,[{mirror:true,rows:[
 '.........Y',
 '........YY',
 '......YYYY',
 '....YYYYYY',
 '...YYYYYYY',
 '..YYYYYYYY',
 '.YYYYYYYYY',
 '.YYYYCCCYY',
 '.YYYCCCCCY',
 'YYYYCBBBCY',
 'YYYYCBDDCY',
 'YYYYCBDDCY',
 'YYYYCBBBCY',
 'YYYYYCCCYY',
 'YYYYYYYYOO',
 '.YYYYYYYOO',
 '.YYYYYYYYO',
 '..YYYYYYYY',
 '...YYYYYYY',
 '....YYYYYY',
 '.....YYYYY',
 '....YYYYYY',
 '.....YYYYY',
 '.....OOO..',
 '....OOOO..']}]));

// 10 Mr. Meeseeks: a tall light-blue figure, thin arms raised, wide eyes, the open mouth; a yellow room
const meeseeks=svg(24,36,`<rect width="24" height="36" fill="${C.Y}"/><rect y="32" width="24" height="4" fill="${C.O}"/>${pix(24,36,[{rows:[
 '........................',
 '...........B............',
 '.BB........BB........BB.',
 '.BBB.......B........BBB.',
 '.BBB.....BBBBBB.....BBB.',
 '..BB...BBBBBBBBBB...BB..',
 '..BB..BBBBBBBBBBBB..BB..',
 '...BB.BBCCCBBCCCBB.BB...',
 '...BB.BCCCCBBCCCCB.BB...',
 '...BB.BCCDDBBDDCCB.BB...',
 '...BB.BCCDDBBDDCCB.BB...',
 '...BB.BCCCCBBCCCCB.BB...',
 '...BB.BBCCCBBCCCBB.BB...',
 '...BB.BBBBBBBBBBBB.BB...',
 '...BB.BBDDDDDDDDBB.BB...',
 '...BB.BBDPPPPPPDBB.BB...',
 '...BB..BDPPPPPPDB..BB...',
 '...BBB.BBDDDDDDBB.BBB...',
 '....BBBBBBBBBBBBBBBB....',
 '......BBBBBBBBBBBB......',
 '......BBBBBBBBBBBB......',
 '......BBBBBBBBBBBB......',
 '......BBBBBBBBBBBB......',
 '......BBBBBBBBBBBB......',
 '......BBBBBBBBBBBB......',
 '.......BBBBBBBBBB.......',
 '........BBBBBBBB........',
 '........BB....BB........',
 '........BB....BB........',
 '........BB....BB........',
 '........BB....BB........',
 '.......BBB....BBB.......']}])}`);

// 11 Waddles: a pink piglet, the head turned to us with its big snout, a curly tail, on a strip of grass
const waddles=svg(27,19,pix(27,19,[{rows:[
 '............KK.........KK..',
 '............KKK.......KKK..',
 'KKKK........KPPK.....KPPK..',
 'K..K........KPPKKKKKKKPPK..',
 'KK.K.KKKKKKKKKKKKKKKKKKKKK.',
 '..KKKKKKKKKKKKKKKKKKKKKKKK.',
 '...KKKKKKKKKKKKKDDKKKDDKKK.',
 '...KKKKKKKKKKKKKDDKKKDDKKK.',
 '...KKKKKKKKKKKKKKKKKKKKKKK.',
 '...KKKKKKKKKKKKKKPPPPPPKKK.',
 '...KKKKKKKKKKKKKPPPPPPPPKK.',
 '...KKKKKKKKKKKKKPPDDPDDPKK.',
 '...KKKKKKKKKKKKKPPDDPDDPKK.',
 '...KKKKKKKKKKKKKKPPPPPPKKK.',
 '....KKKKKKKKKKKKKKKKKKKKK..',
 '....KK..KK.....KKKKKKKKK...',
 '....KK..KK.....KK....KK....',
 '....KK..KK.....KK....KK....',
 '..GGGGGGGGGGGGGGGGGGGGGGG..']}]));

// 12 Perry
const perry=svg(32,36,`<rect width="32" height="36" fill="${C.V}"/><rect y="31" width="32" height="5" fill="${C.D}"/>
 ${fill(C.O,`<ellipse cx="26" cy="27.5" rx="4.5" ry="2.2" transform="rotate(-20 26 27.5)"/>`)}
 ${fill(C.B,`<rect x="9" y="8" width="14" height="22" rx="6"/><rect x="6.5" y="17" width="3" height="6" rx="1.4"/><rect x="22.5" y="17" width="3" height="6" rx="1.4"/>`)}
 ${fill(C.O,`<ellipse cx="12.5" cy="30.5" rx="3.5" ry="1.4"/><ellipse cx="19.5" cy="30.5" rx="3.5" ry="1.4"/><ellipse cx="16" cy="17" rx="7.5" ry="2.5"/>`)}
 ${fill(C.C,`<rect x="11" y="11" width="4" height="3"/><rect x="17" y="11" width="4" height="3"/>`)}
 ${fill(C.D,`<rect x="12" y="12" width="2" height="2"/><rect x="18" y="12" width="2" height="2"/>`)}
 ${fill(C.A,`<ellipse cx="16" cy="8.5" rx="9" ry="1.6"/><path d="M10 8.5 L10.5 3.5 Q16 1.5 21.5 3.5 L22 8.5 Z"/>`)}
 ${fill(C.E,`<rect x="10.2" y="6" width="11.6" height="2"/>`)}`);

// 13 King: a small dark creature in a white bone skull mask with curved horns, inside an orange round
const king=svg(24,24,`${fill(C.O,`<circle cx="12" cy="12" r="10.6"/>`)}${pix(24,24,[{mirror:true,rows:[
 '............',
 '............',
 '............',
 '....CC......',
 '....CC......',
 '.....CC..CCC',
 '.....CCCCCCC',
 '......CCCCCC',
 '......CDDDCC',
 '......CDDDCC',
 '......CDDDCC',
 '......CCCCCC',
 '.......CCCCC',
 '........CCCC',
 '.........CCC',
 '.........DDD',
 '........DDDD',
 '.......DDDDD',
 '.......DDDDD',
 '........DDDD',
 '........DD..']}])}`);

// 14 Patrick: a pink starfish with thick brows and a wide smile, green shorts with purple flowers, on sand under the sky
const patrick=svg(30,30,`<rect width="30" height="30" fill="${C.S}"/><rect y="22" width="30" height="8" fill="${C.Y}"/>${pix(30,30,[{mirror:true,rows:[
 '...............',
 '..............P',
 '.............PP',
 '............PPP',
 '...........PPPP',
 '..........DDDPP',
 '.........PPCCCP',
 '.........PPCDDP',
 '........PPPCDDP',
 '........PPPCCCP',
 '.......PPPPPPPP',
 '......PPPDPPPPP',
 '...PPPPPPPDDDDD',
 '.PPPPPPPPPPPPPP',
 '.PPPPPPPPPPPPPP',
 '...PPPPPPPPPPPP',
 '.....PPPPPPPPPP',
 '......PPPPPPPPP',
 '......PPPPPPPPP',
 '......GGGGGGGGG',
 '......GGVVGGGGG',
 '......GGVVGGGGG',
 '......GGGGGGGGV',
 '......GGGGGGGGV',
 '......GGGGGG...',
 '.......PPPP....',
 '........PPP....',
 '.........PP....']}])}`);

// 15 Aang: the bald head with the blue arrow down to the forehead, orange-and-yellow robes, the staff; sky and meadow
const aang=svg(32,38,`<rect width="32" height="38" fill="${C.S}"/>
 ${fill(C.A,`<rect x="26" y="5" width="2" height="30"/>`)}
 ${fill(C.O,`<path d="M3 38 L4 29 Q5 24 11 23 L21 23 Q27 24 28 29 L29 38 Z"/>`)}
 ${fill(C.Y,`<polygon points="12,23 20,23 19,38 13,38"/>`)}
 ${fill(C.C,`<rect x="13" y="21" width="6" height="3"/><circle cx="27" cy="24" r="1.8"/>`)}
 ${pix(32,38,[{mirror:true,rows:[
 '................','................','................','................',
 '............CCCB',
 '..........CCCCCB',
 '.........CCCCCCB',
 '........CCCCCCCB',
 '........CCCCCCCB',
 '........CCCCCCCB',
 '........CCCCBBBB',
 '........CCCCCBBB',
 '........CCCCCCBB',
 '.......CCCCCCCCB',
 '.......CCCCDDCCC',
 '.......CCCCDDCCC',
 '........CCCCCCCC',
 '........CCCCCCCC',
 '........CCCDCCCC',
 '........CCCCDDDD',
 '.........CCCCCCC',
 '..........CCCCCC',
 '............CCCC']}])}
 ${fill(C.G,`<path d="M0 38 L0 33 Q16 29 32 33 L32 38 Z"/>`)}`);

// 16 Appa: the huge white flying bison, the brown arrow on its brow, horns, a big dark nose, six legs, the saddle
const appa=svg(40,30,`<rect width="40" height="30" fill="${C.B}"/>
 ${fill(C.C,`<ellipse cx="36" cy="11" rx="2.5" ry="4.5"/>
   <rect x="11" y="18" width="3" height="6" rx="1"/><rect x="15" y="18" width="3" height="6" rx="1"/><rect x="19" y="18" width="3" height="6" rx="1"/>
   <rect x="23" y="18" width="3" height="6" rx="1"/><rect x="27" y="18" width="3" height="6" rx="1"/><rect x="31" y="17" width="3" height="6" rx="1"/>
   <ellipse cx="22.5" cy="13.5" rx="13.5" ry="7.5"/>`)}
 ${fill(C.R,`<rect x="15" y="6.5" width="14" height="3.5" rx="1"/>`)}${fill(C.E,`<rect x="16" y="4" width="12" height="3"/>`)}
 ${pix(40,30,[{x:1,rows:[
 '................','................',
 '..AA.........AA.',
 '..AA.........AA.',
 '...AA.......AA..',
 '...AA.......AA..',
 '....AA.....AA...',
 '...CCCCCCCCCC...',
 '..CCCCCAACCCCC..',
 '.CCCCCCAACCCCCC.',
 '.CCCAAAAAAACCCC.',
 'CCCCCAAAAACCCCCC',
 'CDDCCCAAACCCDDCC',
 'CDDCCCCACCCCDDCC',
 'CCCCCCCCCCCCCCCC',
 'CCCCDDDDDDDCCCCC',
 'CCCCDDDDDDDCCCCC',
 '.CCCCDDDDDCCCCC.',
 '.CCCCCCCCCCCCCC.',
 '..CCDDDDDDDDCCC.',
 '...CCCCCCCCCCC..',
 '.....CCCCCCC....']}])}
 ${fill(C.G,`<path d="M0 30 L0 26 Q10 23.5 20 26 Q30 28.5 40 25 L40 30 Z"/>`)}`);

// 17 GIR in his dog suit: a green hood with darker floppy ears, round cyan eyes in dark rims, the grin and the tongue,
// the zipper down the front; a dark blue night
const gir=svg(26,32,`<rect width="26" height="32" fill="${C.N}"/><rect y="29" width="26" height="3" fill="${C.D}"/>${pix(26,32,[{mirror:true,rows:[
 '.............',
 '.............',
 '........GGGGG',
 '......GGGGGGG',
 '.....GGGGGGGG',
 '..MMMGGGGGGGG',
 '.MMMMGDDDDGGG',
 '.MMMMDDBBDDGG',
 '.MMMMDBBBBDGG',
 '.MMMMDBBBBDGG',
 '.MMMMDDBBDDGG',
 '.MMMMGDDDDGGG',
 '.MMMMGGGGGGGG',
 '..MMGGGGGGGDD',
 '...MGGGGGGGGD',
 '....GGDDDDDDD',
 '....GGGDDDDDD',
 '.....GGGGDDDD',
 '......GGGGGGG',
 '........GGGGD',
 '......GGGGGGD',
 '.....GGGGGGGD',
 '.....GG.GGGGD',
 '.....GG.GGGGD',
 '......G.GGGGD',
 '........GGGGD',
 '........GGGGG',
 '........GG...',
 '........GG...',
 '.......GGG...']},
 {x:15,y:16,rows:['PPP','PPP','PPP','.PP']}])}`);

// 18 CatDog: one long yellow body, the cat's head with pointed ears at one end, the dog's with brown ears at the other
const catdog=svg(38,27,`<rect width="38" height="27" fill="${C.S}"/><rect y="23" width="38" height="4" fill="${C.G}"/>${pix(38,27,[
 {x:9,y:10,rows:[
 '..YYYYYYYYYYYYYYYYYYY..',
 '.YYYYYYYYYYYYYYYYYYYYY.',
 'YYYYYYYYYYYYYYYYYYYYYYY',
 'YYYYYYYYYYYYYYYYYYYYYYY',
 'YYYYYYYYYYYYYYYYYYYYYYY',
 'YYYYYYYYYYYYYYYYYYYYYYY',
 'YYYYYYYYYYYYYYYYYYYYYYY',
 '.YYYYYYYYYYYYYYYYYYYYY.',
 '..YYYYYYYYYYYYYYYYYYY..']},
 {x:10,y:19,rows:['YY..YY.........YY..YY','YY..YY.........YY..YY','YY..YY.........YY..YY','YY..YY.........YY..YY']},
 {x:2,y:2,rows:[
 '..YY....YY..',
 '..YKY..YKY..',
 '.YYKKYYKKYY.',
 '.YYYYYYYYYY.',
 'YYYYYYYYYYYY',
 'YYCCCYYCCCYY',
 'YYCDDYYDDCYY',
 'YYYCCYYCCYYY',
 'YYYYYKKYYYYY',
 'YYYYYKKYYYYY',
 'YYYDDDDDDYYY',
 '.YYYYYYYYYY.',
 '..YYYYYYYY..']},
 {x:24,y:2,rows:[
 '...YYYYYY...',
 '..YYYYYYYY..',
 '.AYYYYYYYYA.',
 'AAYYYYYYYYAA',
 'AAYDDYYDDYAA',
 'AAYDDYYDDYAA',
 'AAYYYYYYYYAA',
 'AAYYDDDDYYAA',
 'AAYYYDDYYYAA',
 '.AYYYYYYYYA.',
 '..YDDDDDDY..',
 '..YYYPPYYY..',
 '...YPPPPY...',
 '.....PP.....']},
 {outline:'D'}])}`);

// 19 Cosmo: green swept hair, a floating crown, the wand with a star, small white wings; inside a dark blue round
const cosmo=svg(24,24,`${fill(C.N,`<circle cx="12" cy="12" r="10.6"/>`)}${pix(24,24,[{rows:[
 '........................',
 '........................',
 '..........Y.Y.Y.........',
 '..........YYYYY.........',
 '........................',
 '.........GGGGGG.........',
 '.......GGGGGGGGG....Y...',
 '.....GGGGGGGGGGG...YYY..',
 '.....GG.GGGGGGGG..YYYYY.',
 '........GKKKKKG....YYY..',
 '.......KKKKKKKKK...Y.Y..',
 '.......KCCCKCCCK...D....',
 '.......KCDDKDDCK...D....',
 '.......KCCCKCCCK...D....',
 '.......KKKKKKKKK..DD....',
 '...CC...KDDDDDK..KK.CC..',
 '..CCCC...KKKKK...KK.CCC.',
 '..CCC..CCCCCCCCCCC..CCC.',
 '...CC.CCCCCCCCCCCCC.CC..',
 '......CCCCCCCCCCCCC.....',
 '.......CCCCCCCCCCC......']}])}`);
// 20 Michelangelo
const mikey=svg(32,38,`<rect width="32" height="38" fill="${C.N}"/>
 ${fill(C.A,`<ellipse cx="16" cy="31" rx="13" ry="9"/>`)}
 ${fill(C.G,`<path d="M6 38 L7 28 Q9 23 16 23 Q23 23 25 28 L26 38 Z"/><rect x="13" y="19" width="6" height="5"/><ellipse cx="16" cy="12.5" rx="7.5" ry="7.5"/>`)}
 ${fill(C.Y,`<path d="M10 38 L10.5 28 Q12 25 16 25 Q20 25 21.5 28 L22 38 Z"/>`)}
 ${fill(C.A,`<rect x="10.5" y="30" width="11" height="1"/><rect x="10.5" y="34" width="11" height="1"/>`)}
 ${fill(C.O,`<rect x="8.5" y="9.5" width="15" height="4.5"/><polygon points="8.5,10 3,8 4,12 8.5,12.5"/><polygon points="8.5,12.5 2.5,14.5 5,16 9,13.5"/>`)}
 ${fill(C.C,`<rect x="11" y="10" width="4" height="3"/><rect x="17" y="10" width="4" height="3"/>`)}
 ${fill(C.D,`<rect x="12" y="11" width="2" height="2"/><rect x="18" y="11" width="2" height="2"/><path d="M11 16 Q16 20.5 21 16 Z"/>`)}
 ${fill(C.C,`<rect x="12.5" y="16" width="7" height="1"/>`)}
 ${fill(C.G,`<circle cx="28" cy="26" r="2"/><path d="M26 34 L26.5 27 L29.5 27 L29 34 Z"/>`)}
 ${fill(C.Y,`<polygon points="21,18 30,13 30,24"/>`)}
 ${fill(C.A,`<polygon points="28.5,13.6 30.5,12.5 30.5,24.5 28.5,23.5"/>`)}
 ${fill(C.R,`<circle cx="26" cy="16.5" r="1.4"/><circle cx="25.5" cy="21" r="1.4"/>`)}`);

const T='Герои мультсериалов';
export default {theme:T, pictures:[
 {title:'Гамбол',family:'Домашние животные',subject:'Гамбол Уоттерсон, голубой мультяшный котяра с огромными глазами и белой мордочкой',...ref('Удивительный мир Гамбола','Gumball Watterson'),forLevel:61,cells:952,colours:7,svg:gumball},
 {title:'Джейк-пёс',family:'Домашние животные',subject:'Джейк, жёлто-оранжевый пёс с большими брылями и круглыми глазами',...ref('Время приключений','Jake the Dog'),forLevel:61,cells:864,colours:7,svg:jake},
 {title:'Ледяной король',family:'Герои приключений',subject:'Ледяной король: голубая кожа, длинная белая борода, золотая корона с красными камнями',...ref('Время приключений','Ice King'),forLevel:61,cells:1216,colours:8,svg:iceKing},
 {title:'Мордекай',family:'Птицы',subject:'голубая сойка Мордекай с белым животом, клювом и хохолком',...ref('Обычный мультик','Mordecai'),forLevel:61,cells:884,colours:7,svg:mordecai},
 {title:'Цветик',family:'Герои приключений',subject:'Цветик из Суперкрошек: огромные глаза, длинные рыжие волосы, красный бант, розовое платье',...ref('Суперкрошки','Blossom'),forLevel:61,cells:896,colours:8,svg:blossom},
 {title:'Бен Тен с омнитриксом',family:'Герои приключений',subject:'Бен Теннисон: каштановые волосы, белая футболка с чёрной полосой, зелёные часы-омнитрикс',...ref('Бен 10','Ben Tennyson'),forLevel:61,cells:840,colours:7,svg:ben},
 {title:'Багз Банни с морковкой',family:'Лесные и горные животные',subject:'серый кролик Багз Банни с длинными ушами и белой мордочкой грызёт морковку у норы',...ref('Looney Tunes','Bugs Bunny'),forLevel:61,cells:1260,colours:9,svg:bugs},
 {title:'Таз',family:'Лесные и горные животные',subject:'тасманийский дьявол Таз: коричневое тело, огромная открытая пасть с зубами, светлое брюшко',direction:'Театр эмоций',...ref('Looney Tunes','Taz (Tasmanian Devil)'),forLevel:61,cells:896,colours:7,svg:taz},
 {title:'Твити',family:'Птицы',subject:'жёлтая канарейка Твити с огромной головой и голубыми глазами',...ref('Looney Tunes','Tweety'),forLevel:61,cells:354,colours:5,svg:tweety},
 {title:'Мистер Мисикс',family:'Волшебные существа',subject:'Мистер Мисикс: высокий голубой человечек с поднятыми тонкими руками и широко раскрытыми глазами',direction:'Театр эмоций',...ref('Рик и Морти','Mr. Meeseeks'),forLevel:61,cells:864,colours:6,svg:meeseeks},
 {title:'Пухля',family:'Домашние животные',subject:'розовый поросёнок Пухля с закрученным хвостиком',...ref('Гравити Фолз','Waddles'),forLevel:61,cells:342,colours:4,svg:waddles},
 {title:'Перри-утконос',family:'Профессии и увлечения',subject:'утконос Перри, агент П: бирюзовое тело, коричневая шляпа, оранжевые клюв и хвост',direction:'Обмен ролями',...ref('Финес и Ферб','Perry the Platypus (Agent P)'),forLevel:61,cells:1152,colours:7,svg:perry},
 {title:'Кинг',family:'Волшебные существа',subject:'Кинг: маленькое тёмное существо в белой костяной маске-черепе с рогами',...ref('The Owl House','King'),forLevel:61,cells:353,colours:3,svg:king},
 {title:'Патрик Стар',family:'Морские и пресноводные обитатели',subject:'розовая морская звезда Патрик в зелёных шортах с фиолетовыми цветами на песке под небом',...ref('Губка Боб Квадратные Штаны','Patrick Star'),forLevel:61,cells:900,colours:7,svg:patrick},
 {title:'Аанг',family:'Герои приключений',subject:'Аанг: лысая голова с голубой стрелой-татуировкой, оранжево-жёлтые одежды, посох',...ref('Аватар: Легенда об Аанге','Aang'),forLevel:61,cells:1216,colours:8,svg:aang},
 {title:'Аппа',family:'Волшебные существа',subject:'огромный белый летающий бизон Аппа с коричневой стрелой на голове, шестью лапами и седлом',direction:'Невесомый мир',...ref('Аватар: Легенда об Аанге','Appa'),forLevel:61,cells:1200,colours:7,svg:appa},
 {title:'ГИР в зелёном костюме',family:'Маски и костюмы',subject:'ГИР в зелёном костюме собачки с молнией, голубыми глазами и высунутым языком',direction:'Визуальные каламбуры',...ref('Захватчик Зим','GIR (dog suit)'),forLevel:61,cells:832,colours:7,svg:gir},
 {title:'Котопёс',family:'Домашние животные',subject:'Котопёс: одно тело с головой кота с одной стороны и головой пса с другой',direction:'Визуальные каламбуры',...ref('CatDog','CatDog'),forLevel:61,cells:1026,colours:8,svg:catdog},
 {title:'Космо',family:'Волшебные существа',subject:'крёстный фей Космо: зелёные волосы, корона, палочка со звездой, маленькие крылышки',...ref('Волшебные покровители','Cosmo'),forLevel:61,cells:360,colours:6,svg:cosmo},
 {title:'Микеланджело с пиццей',family:'Герои приключений',subject:'черепашка-ниндзя Микеланджело: зелёная кожа, оранжевая повязка, панцирь, кусок пиццы',...ref('Черепашки-ниндзя','Michelangelo'),forLevel:61,cells:1216,colours:9,svg:mikey},
]};
