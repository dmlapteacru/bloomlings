// split-L0035: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0035.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0035",
 "pictures": [
  {
   "title": "Карусель — мост",
   "family": "Ярмарки и праздники",
   "subject": "карусель с полосатой крышей и лошадками",
   "source": "оригинальный",
   "forLevel": 35,
   "scene": {
    "from": "P0052",
    "mode": "split",
    "side": "right",
    "at": 25,
    "sites": [
     [
      25,
      14
     ],
     [
      25,
      18
     ]
    ],
    "share": 0.375,
    "trim": {
     "left": 1,
     "top": 0,
     "cut": 1
    },
    "kind": "better",
    "cls": "Hard",
    "recipe": "bridge+links+hidden",
    "win": [
     25,
     18
    ],
    "lose": [
     25,
     14
    ],
    "queue": {
     "gate": 0,
     "delay": 2
    },
    "rules": {
     "gapTolerance": 0.15,
     "gapLow": [
      6,
      51
     ]
    }
   },
   "rows": [
    "....................DDYY.#..................",
    "....................DDYYY#YYY...............",
    "....................DDYYY#..................",
    "....................DD...#..................",
    "...................YYYY..#..................",
    "...................YYYY..#..................",
    ".................RCYYYYCR#..................",
    "................RCCYYYYCC#R.................",
    "..............RRCCRRCCRRC#CRR...............",
    ".............RRCCRRRCCRRR#CCRR..............",
    "...........RRRCCCRRRCCRRR#CCCRRR............",
    "..........RRRCCCRRRCCCCRR#RCCCRRR...........",
    "........RRRRCCCCRRRCCCCRR#RCCCCRRRR.........",
    ".......RRRRCCCCRRRRCCCCRR#RRCCCCRRRR........",
    ".....RRRRRCCCCRRRRRCCCCRRbRRRCCCCRRRRR......",
    "....RRRRRCCCCCRRRRRCCCCRR#RRRCCCCCRRRRR.....",
    "..RRRRRRCCCCCRRRRRCCCCCCR#RRRRCCCCCRRRRRR...",
    ".RRRRRRCCCCCRRRRRRCCCCCCR#RRRRRCCCCCRRRRRR..",
    "YYYYYYYYYYYYYYYYYYYYYYYYYbYYYYYYYYYYYYYYYYY.",
    "YYYYYYYYYYYYYYYYYYYYYYYYY#YYYYYYYYYYYYYYYYY.",
    "RRRRCCC.RRRYCCC.RRRCCCCRR#RRCCCYRRR.CCCRRRR.",
    ".RR.CCC.RRRYCCC.RR.VCCV.R#R.CCCYRRR.CCC.RR..",
    ".RR..CC..RYY.C..RR.VCCV.R#R..C.YYR...C..RR..",
    ".....C...RYY.C...R.VPPV..#...C.YYR...C......",
    "...CCA....YY.......VPPV..#.....YY....ACC....",
    ".CCCCAA...YY.......VPPV..#.....YY...AACCCC..",
    "CCDCCCA...YY.......VPPV..#.....YY...ACCCDCC.",
    "CCCCCCAA..YY.......VPPV..#.....YY..AACCCCCC.",
    "...CCCCA..YY.......VPPV..#.....YY..ACCCC....",
    "....CCCAA.YY.......VPPV..#.....YY.AACCC.....",
    "....CCCCA.YY....A..VPPV..#A....YY.ACCCC.....",
    "....CCCCA.YY...AA..VPPV..#AA...YY.ACCCC.....",
    "....CCCCCPPPPCCAA..VPPV..#AACCPPPPCCCCC.....",
    "...CCCCCCPPPPCCAA..VPPV..#AACCPPPPCCCCCC....",
    "..CCCCCCCCCCCCCA...VPPV..#.ACCCCCCCCCCCCC...",
    "..CCCCC...YYCCC....VPPV..#..CCCYY...CCCCC...",
    "..CC......YY.CCC...VPPV..#.CCC.YY......CC...",
    "...CC.....YY...CC..VPPV..#CC...YY.....CC....",
    "..........YY.......VPPV..#.....YY...........",
    "BBBBBBBBBBBBBBBBBBBBBBBBB#BBBBBBBBBBBBBBBBB.",
    "BBBBBBBBBBBBBBBBBBBBBBBBB#BBBBBBBBBBBBBBBBB.",
    "NNNNNNNNNNNNNNNNNNNNNNNNN#NNNNNNNNNNNNNNNNN.",
    ".NNNNNNNNNNNNNNNNNNNNNNNN#NNNNNNNNNNNNNNNN..",
    "..NNNNNNNNNNNNNNNNNNNNNNN#NNNNNNNNNNNNNNN..."
   ]
  }
 ]
};
