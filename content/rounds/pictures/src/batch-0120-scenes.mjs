// batch-0120-scenes: batch-0120.mjs with a strip laid behind each bridge site and a divider between them (bridge-scene.mjs adapt,
// 17 §8): each one passed the builder's checks with its wave position's bridge kind (evidence in batch-0120-scenes.json). Drafts.
export default {
 "theme": "wave-0021-bridges",
 "name": "batch-0120-scenes",
 "pictures": [
  {
   "title": "Грибная поляна за ручьём",
   "forLevel": 23,
   "family": "Грибы и лесные находки",
   "device": "ручей поперёк, мосты вверх; полосы за местами (bridge-scene.mjs adapt)",
   "subject": "мухомор и подберёзовик на дальнем берегу лесного ручья; к левому мосту ведут камни, к правому мох",
   "source": "оригинальный",
   "rows": [
    "############################",
    "#GGGGGGGGGGLLGGGGGGGGGGGGGL#",
    "#GLLGGGGGGGLLGGGLLGGGGGGGGG#",
    "#GGGGGDDDDDDGGGGGGGGGGGGGGG#",
    "#GGDDDRRRRRCCDDGGGGGGGGGGGG#",
    "#GDRRCCRRRRCCRRDGGGGGGGGGGG#",
    "#DRRRCCRRRRRRRRRDGGGGGGGGGG#",
    "#DRRRRRRCCCRRRCRDGGGGGGGGGG#",
    "#DRCCEEECCCEEECEDGGGGGGGGGG#",
    "#DDDDDDDDDDDDDDDDGDDDDDDGGG#",
    "#GGGGGGDCCDGGGGGDDAAAAAADDG#",
    "#GGGGGDCCCADGGGGDAAAAAAAADG#",
    "#GGGGGDCCCADGGGGDDDDDDDDDDG#",
    "#GGGGGDCCCADGGGGGGGDCCDGGGG#",
    "#GGGGGDCCCADGGGGGGGDCADGGGG#",
    "#GGGGGDCCCADGGGGGGGDCADGGGG#",
    "#GGGGGDCCCADGGGGGGGDCADGGGG#",
    "#AAAAAAAAAAAA##DDDDDDDDDDDD#",
    "#AAAAAAAAAAAA##DDDDDDDDDDDD#",
    "#AAAAAAAAAAAA##DDDDDDDDDDDD#",
    "######b##############b######",
    "MMMMCCCCCMMMMMMMMMMLLLLLMMMM",
    "MMMMCCCCCMMMMMMMMMMLLLLLMMMM",
    "MMMMMCCCMMMMMMMMMMMMMMMMMMMM",
    "MMMMMMMMGGMMMMMMMMMMMMMMMMMM",
    "MMMMMMMGGMMMMMMMMMGGMMMMMMMM",
    "MMMMMMMGGMMMMMMMMMMMMMMMMMMM",
    "MMMMMMMMGGMMMMMMMMMMMMMMMMMM",
    "MMMGGMMMMMMMMMMMMMMMMMMMMMMM"
   ],
   "scene": {
    "from": "batch-0120.mjs #1",
    "side": "up",
    "kind": "better",
    "cls": "Normal",
    "recipe": "bridge",
    "win": [
     21,
     20
    ],
    "lose": [
     6,
     20
    ],
    "opts": {
     "a": "D",
     "b": "A",
     "depth": 3,
     "win": 1,
     "border": true
    }
   }
  }
 ]
};
