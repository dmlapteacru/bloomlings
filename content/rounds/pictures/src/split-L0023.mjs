// split-L0023: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0023.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0023",
 "pictures": [
  {
   "title": "Фонтан в парке — мост",
   "family": "Городские пространства",
   "subject": "круглый каменный фонтан со струями воды, газон и два куста вокруг",
   "source": "оригинальный",
   "forLevel": 23,
   "scene": {
    "from": "P0074",
    "mode": "split",
    "side": "up",
    "at": 10,
    "sites": [
     [
      2,
      10
     ],
     [
      14,
      10
     ]
    ],
    "share": 0.333,
    "kind": "better",
    "cls": "Normal",
    "recipe": "bridge+hidden",
    "win": [
     2,
     10
    ],
    "lose": [
     14,
     10
    ],
    "queue": {
     "gate": 2,
     "delay": 2
    }
   },
   "rows": [
    "SSSSSSSSSSSSSSSSSSSSSSSSSSSSSS",
    "SSSSSSSSSSSSSSBBSSSSSSSSSSSSSS",
    "SSSSSSSSSSSSSSBBSSSSSSSSSSSSSS",
    "SSSSSSSSSSSSSSBBSSSSSSSSSSSSSS",
    "SSSSSSSSSSSSSSBBSSSSSSSSSSSSSS",
    "SSSSSSSSSBBBBSBBSBBBBSSSSSSSSS",
    "SSSSSSSSBBSSBBBBBBSSBBSSSSSSSS",
    "SSSSSSSSBSSSSBBBBSSSSBSSSSSSSS",
    "SSSSSSSBBSSSSBBBBSSSSBBSSSSSSS",
    "SSSSSSSBSSSSSSBBSSSSSSBSSSSSSS",
    "##b###########b###############",
    "SSSSSSSSSSSSSSBBSSSSSSSSSSSSSS",
    "SSSSSSSSCBBBBBBBBBBBBASSSSSSSS",
    "SSSSSSSSCCCCCCCCCCCCAASSSSSSSS",
    "SSSSSSSSSACCCCCCCCAAASSSSSSSSS",
    "MMMMMSSSSSSSAAAAAASSSSSSSMMMMM",
    "MMMMMGBGGGGGGCCAAGGGGGGBGMMMMM",
    "MMMMMMBGGGGGGCCAAGGGGGGBMMMMMM",
    "MMMMMBBGGGGGGCCAAGGGGGGBBMMMMM",
    "MMMMMBMGGGGCCCCAACCGGGGMBMMMMM",
    "MMMMBBCCCCCCCCCAACCCCCCCBBMMMM",
    "MMCCBBBBBBBBBCCAABBBBBBBBBCCMM",
    "MMCCBBBBBBBBBBBBBBBBBBBBBBCCMM",
    "GMMCCCCCCCCCCCCCCCCCCCCCCCCMMG",
    "GGACCCCACCCCCCCCCCCCCCACCCCAGG",
    "GGACCCCACCCCACCCCACCCCACCCCAGG",
    "GGACCCCACCCCACCCCACCCCACCCCAGG",
    "GGACCCCACCCCACCCCACCCCACCCCAGG",
    "GGGAAAAAAAAAAAAAAAAAAAAAAAAGGG",
    "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGG",
    "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGG"
   ]
  }
 ]
};
