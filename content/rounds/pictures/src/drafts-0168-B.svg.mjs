// SVG drafts of batch 0168, group B: «Герои мультфильмов» (the owner, 2026-10-31: characters from cartoons). Every
// picture is a franchise reference (source 'сторонняя вселенная'), kept out of levels until the owner confirms rights.
// Drawn at cell scale (one viewBox unit = one cell, features on whole cells); convert with tools/rounds/svg2cells.mjs.
import {C} from '../../../../tools/rounds/svg-palette.mjs';
const U='сторонняя вселенная';
export default {theme:'Герои мультфильмов', pictures:[
 // 1 — Cheburashka: huge round ears, brown fur, big eyes; a bark outline round the figure, sky and grass
 {title:'Чебурашка', family:'Волшебные существа', subject:'Чебурашка: огромные круглые уши, коричневый мех, большие глаза',
  source:U, reference:'Крокодил Гена и Чебурашка — Чебурашка', forLevel:61, cells:980, colours:7,
  svg:`<svg viewBox="0 0 35 28" xmlns="http://www.w3.org/2000/svg">
   <rect width="35" height="28" fill="${C.S}"/><rect y="24" width="35" height="4" fill="${C.G}"/>
   <circle cx="7.6" cy="7.8" r="6.6" fill="${C.E}"/><circle cx="27.4" cy="7.8" r="6.6" fill="${C.E}"/>
   <ellipse cx="17.5" cy="21.8" rx="6.2" ry="4.4" fill="${C.E}"/>
   <ellipse cx="14" cy="25.4" rx="2.8" ry="1.5" fill="${C.E}"/><ellipse cx="21" cy="25.4" rx="2.8" ry="1.5" fill="${C.E}"/>
   <ellipse cx="11" cy="20.6" rx="2.2" ry="2.8" fill="${C.E}"/><ellipse cx="24" cy="20.6" rx="2.2" ry="2.8" fill="${C.E}"/>
   <circle cx="17.5" cy="12.4" r="8.6" fill="${C.E}"/>
   <circle cx="7.6" cy="7.8" r="5.6" fill="${C.A}"/><circle cx="27.4" cy="7.8" r="5.6" fill="${C.A}"/>
   <circle cx="7.9" cy="8.2" r="3.5" fill="${C.C}"/><circle cx="27.1" cy="8.2" r="3.5" fill="${C.C}"/>
   <ellipse cx="17.5" cy="21.8" rx="5.2" ry="3.4" fill="${C.A}"/>
   <ellipse cx="11.2" cy="20.8" rx="1.3" ry="1.9" fill="${C.A}"/><ellipse cx="23.8" cy="20.8" rx="1.3" ry="1.9" fill="${C.A}"/>
   <ellipse cx="17.5" cy="22.4" rx="3.2" ry="2.3" fill="${C.C}"/>
   <circle cx="17.5" cy="12.4" r="7.6" fill="${C.A}"/>
   <ellipse cx="17.5" cy="14" rx="6.5" ry="5.4" fill="${C.C}"/>
   <rect x="12" y="9" width="5" height="6" rx="2" fill="${C.D}"/><rect x="18" y="9" width="5" height="6" rx="2" fill="${C.D}"/>
   <rect x="13" y="10" width="2" height="2" fill="${C.C}"/><rect x="19" y="10" width="2" height="2" fill="${C.C}"/>
   <rect x="16" y="15" width="3" height="2" rx="0.7" fill="${C.D}"/>
   <rect x="15" y="18" width="5" height="1" fill="${C.D}"/><rect x="15" y="17" width="1" height="1" fill="${C.D}"/><rect x="19" y="17" width="1" height="1" fill="${C.D}"/>
  </svg>`},
 // 2 — Mickey Mouse waving: black round ears, red shorts with two white buttons, yellow shoes, white gloves; sky and grass
 {title:'Микки Маус', family:'Коллекционный хайп и маскоты', subject:'Микки Маус машет рукой: чёрные круглые уши, красные шорты с белыми пуговицами, жёлтые башмаки, белые перчатки',
  source:U, reference:'Disney — Mickey Mouse', forLevel:61, cells:1216, colours:7,
  svg:`<svg viewBox="0 0 32 38" xmlns="http://www.w3.org/2000/svg">
   <rect width="32" height="38" fill="${C.S}"/><rect y="33" width="32" height="5" fill="${C.G}"/>
   <circle cx="7.5" cy="6.5" r="5" fill="${C.D}"/><circle cx="24.5" cy="6.5" r="5" fill="${C.D}"/>
   <polygon points="10,20 12.6,21.6 7.4,14.6 5,15.8" fill="${C.D}"/><circle cx="5" cy="13.6" r="2.6" fill="${C.C}"/>
   <polygon points="21,20.5 25.5,23.6 24.2,25.6 20,22.8" fill="${C.D}"/><circle cx="25.6" cy="25.4" r="2.3" fill="${C.C}"/>
   <rect x="12" y="29" width="2.6" height="4" fill="${C.D}"/><rect x="17.4" y="29" width="2.6" height="4" fill="${C.D}"/>
   <ellipse cx="11.6" cy="33.6" rx="3.8" ry="1.9" fill="${C.Y}"/><ellipse cx="20.4" cy="33.6" rx="3.8" ry="1.9" fill="${C.Y}"/>
   <ellipse cx="16" cy="23.5" rx="5.4" ry="4.6" fill="${C.D}"/>
   <rect x="10.4" y="24.6" width="11.2" height="5.4" rx="1.6" fill="${C.R}"/>
   <rect x="12.6" y="25.6" width="2" height="3" rx="0.8" fill="${C.C}"/><rect x="17.4" y="25.6" width="2" height="3" rx="0.8" fill="${C.C}"/>
   <circle cx="16" cy="13" r="7.4" fill="${C.D}"/>
   <ellipse cx="16" cy="16" rx="6" ry="4.4" fill="${C.C}"/>
   <ellipse cx="13.4" cy="11.4" rx="2.6" ry="3.8" fill="${C.C}"/><ellipse cx="18.6" cy="11.4" rx="2.6" ry="3.8" fill="${C.C}"/>
   <rect x="12.6" y="9.6" width="2" height="3" rx="0.8" fill="${C.D}"/><rect x="17.4" y="9.6" width="2" height="3" rx="0.8" fill="${C.D}"/>
   <ellipse cx="16" cy="14.4" rx="2" ry="1.3" fill="${C.D}"/>
   <path d="M11.4 16.2 L20.6 16.2 Q19.8 20.2 16 20.2 Q12.2 20.2 11.4 16.2 Z" fill="${C.D}"/>
   <path d="M13.4 18.4 Q16 17.2 18.6 18.4 Q17.8 19.6 16 19.6 Q14.2 19.6 13.4 18.4 Z" fill="${C.R}"/>
  </svg>`},
 // 3 — Pikachu: yellow, long black-tipped ears, red cheeks, the lightning tail; sky and grass
 {title:'Пикачу', family:'Игровая интернет-культура', subject:'жёлтый Пикачу с длинными ушами с чёрными кончиками, красными щёчками и хвостом-молнией',
  source:U, reference:'Pokémon — Pikachu', forLevel:61, cells:896, colours:7,
  svg:`<svg viewBox="0 0 28 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="32" fill="${C.S}"/><rect y="27" width="28" height="5" fill="${C.G}"/>
   <polygon points="16,25 20,21.5 18.5,19.5 23,16 21.5,13.5 25.5,7 26.5,12 24.5,14.5 26,16.5 21.5,21.5 23,23.5 18,27" fill="${C.Y}"/>
   <ellipse cx="7.6" cy="7" rx="2" ry="5.6" transform="rotate(-28 7.6 7)" fill="${C.D}"/>
   <ellipse cx="18.4" cy="7" rx="2" ry="5.6" transform="rotate(28 18.4 7)" fill="${C.D}"/>
   <ellipse cx="9" cy="9.6" rx="2" ry="5.6" transform="rotate(-28 9 9.6)" fill="${C.Y}"/>
   <ellipse cx="17" cy="9.6" rx="2" ry="5.6" transform="rotate(28 17 9.6)" fill="${C.Y}"/>
   <ellipse cx="13" cy="25.2" rx="5.8" ry="4.6" fill="${C.Y}"/><ellipse cx="7.6" cy="23" rx="1.4" ry="2.2" transform="rotate(30 7.6 23)" fill="${C.Y}"/><ellipse cx="18.4" cy="23" rx="1.4" ry="2.2" transform="rotate(-30 18.4 23)" fill="${C.Y}"/>
   <ellipse cx="9.6" cy="29.2" rx="2.6" ry="1.4" fill="${C.Y}"/><ellipse cx="16.4" cy="29.2" rx="2.6" ry="1.4" fill="${C.Y}"/>
   <ellipse cx="13" cy="15" rx="8" ry="6.6" fill="${C.Y}"/>
   <rect x="8" y="13" width="3" height="3" rx="0.6" fill="${C.D}"/><rect x="15" y="13" width="3" height="3" rx="0.6" fill="${C.D}"/>
   <polygon points="18.2,22.6 21.2,20.4 23.2,23 19.8,26" fill="${C.A}"/>
   <rect x="5" y="16" width="3" height="3" rx="0.8" fill="${C.R}"/><rect x="18" y="16" width="3" height="3" rx="0.8" fill="${C.R}"/>
   <rect x="11" y="17" width="4" height="1" fill="${C.D}"/><rect x="12" y="18" width="2" height="1" fill="${C.D}"/>
  </svg>`},
 // 4 — Hello Kitty: white cat head with a black outline, red bow, no mouth, yellow nose; a pink ground, compact
 {title:'Хеллоу Китти', family:'Домашние животные', subject:'белая кошечка Хеллоу Китти с красным бантом и жёлтым носиком, без рта',
  source:U, reference:'Sanrio — Hello Kitty', forLevel:61, cells:396, colours:5,
  svg:`<svg viewBox="0 0 22 18" xmlns="http://www.w3.org/2000/svg">
   <rect width="22" height="18" fill="${C.P}"/>
   <polygon points="2,10 3,2 9.5,5.5" fill="${C.D}"/><polygon points="20,10 19,2 12.5,5.5" fill="${C.D}"/>
   <ellipse cx="11" cy="10.5" rx="9.6" ry="6.2" fill="${C.D}"/>
   <polygon points="3.8,8.8 4.4,4.2 8,6.2" fill="${C.C}"/>
   <ellipse cx="11" cy="10.5" rx="8.6" ry="5.2" fill="${C.C}"/>
   <rect x="6" y="10" width="2" height="3" fill="${C.D}"/><rect x="14" y="10" width="2" height="3" fill="${C.D}"/>
   <rect x="9" y="12" width="4" height="2" rx="0.8" fill="${C.Y}"/>
   <rect x="0" y="11" width="4" height="1" fill="${C.D}"/><rect x="0" y="13" width="4" height="1" fill="${C.D}"/>
   <rect x="18" y="11" width="4" height="1" fill="${C.D}"/><rect x="18" y="13" width="4" height="1" fill="${C.D}"/>
   <polygon points="11,0 16.5,2.6 22,0 22,8 16.5,5.4 11,8" fill="${C.D}"/>
   <polygon points="12,1.6 16,3.4 16,4.6 12,6.4" fill="${C.R}"/><polygon points="21,1.6 17,3.4 17,4.6 21,6.4" fill="${C.R}"/>
  </svg>`},
 // 5 — Mario jumping: red cap with a plain white disc, moustache, blue overalls with yellow buttons, white gloves; sky and grass
 {title:'Марио', family:'Игровая интернет-культура', subject:'Марио в красной кепке с белым кругом, с усами, в синем комбинезоне с жёлтыми пуговицами и белых перчатках',
  source:U, reference:'Super Mario — Mario', forLevel:61, cells:1216, colours:9,
  svg:`<svg viewBox="0 0 32 38" xmlns="http://www.w3.org/2000/svg">
   <rect width="32" height="38" fill="${C.S}"/><rect y="33" width="32" height="5" fill="${C.G}"/>
   <polygon points="9.6,18 12.4,19.6 8.4,11 5.6,12.2" fill="${C.R}"/><circle cx="6.6" cy="10" r="2.5" fill="${C.C}"/>
   <rect x="21" y="18.6" width="3.2" height="7" rx="1.2" fill="${C.R}"/><circle cx="22.6" cy="26.4" r="2.3" fill="${C.C}"/>
   <rect x="9.6" y="18" width="12.8" height="7" rx="2" fill="${C.R}"/>
   <rect x="10.6" y="21.4" width="10.8" height="8.6" rx="1.6" fill="${C.N}"/>
   <rect x="11.2" y="18" width="2.4" height="4" fill="${C.N}"/><rect x="18.4" y="18" width="2.4" height="4" fill="${C.N}"/>
   <circle cx="12.4" cy="21.8" r="1.2" fill="${C.Y}"/><circle cx="19.6" cy="21.8" r="1.2" fill="${C.Y}"/>
   <rect x="10.6" y="29" width="4.2" height="3" fill="${C.N}"/><rect x="17.2" y="29" width="4.2" height="3" fill="${C.N}"/>
   <ellipse cx="12" cy="32.8" rx="3.4" ry="1.7" fill="${C.E}"/><ellipse cx="20" cy="32.8" rx="3.4" ry="1.7" fill="${C.E}"/>
   <rect x="9.2" y="9.4" width="13.6" height="5" rx="2" fill="${C.E}"/>
   <circle cx="9.8" cy="12.6" r="1.4" fill="${C.C}"/><circle cx="22.2" cy="12.6" r="1.4" fill="${C.C}"/>
   <ellipse cx="16" cy="13" rx="5.6" ry="4.8" fill="${C.C}"/>
   <rect x="12.6" y="9.6" width="2" height="3" rx="0.8" fill="${C.N}"/><rect x="17.4" y="9.6" width="2" height="3" rx="0.8" fill="${C.N}"/>
   <polygon points="10.6,14.8 21.4,14.8 21.8,16.6 19.2,17.6 16,16.6 12.8,17.6 10.2,16.6" fill="${C.D}"/>
   <ellipse cx="16" cy="13.8" rx="2.2" ry="1.7" fill="${C.K}"/>
   <ellipse cx="16" cy="6.8" rx="6.4" ry="4.4" fill="${C.R}"/>
   <rect x="9.4" y="7.4" width="15" height="2.4" rx="1" fill="${C.R}"/>
   <circle cx="16" cy="5.6" r="2.3" fill="${C.C}"/>
  </svg>`},
 // 6 — Simba the cub on Pride Rock at sunrise: golden cub with a tuft on his head, the sun rising over the savanna
 {title:'Симба на скале', family:'Саванна и джунгли', subject:'золотистый львёнок Симба с хохолком на голове сидит на краю скалы на рассвете',
  source:U, reference:'The Lion King — Simba', forLevel:61, cells:1152, colours:8,
  svg:`<svg viewBox="0 0 36 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="36" height="32" fill="${C.K}"/><rect y="18" width="36" height="6" fill="${C.O}"/>
   <circle cx="7" cy="24" r="5.4" fill="${C.Y}"/><rect y="24" width="36" height="8" fill="${C.M}"/>
   <polygon points="36,32 36,19.5 14.6,19.5 11.6,20.6 13,22.6 18,24 22,27 21,32" fill="${C.D}"/>
   <path d="M28.4 17.6 Q33 17.6 32.4 12.4" fill="none" stroke="${C.Y}" stroke-width="2"/>
   <circle cx="32.4" cy="11.4" r="1.7" fill="${C.E}"/>
   <ellipse cx="24" cy="15.6" rx="5.2" ry="4.2" fill="${C.Y}"/>
   <rect x="20" y="14" width="3" height="6" fill="${C.Y}"/><rect x="25" y="14" width="3" height="6" fill="${C.Y}"/>
   <rect x="20" y="18" width="3" height="2" fill="${C.C}"/><rect x="25" y="18" width="3" height="2" fill="${C.C}"/>
   <rect x="23" y="14" width="2" height="4" fill="${C.C}"/>
   <circle cx="18.6" cy="4" r="2.1" fill="${C.Y}"/><circle cx="29.4" cy="4" r="2.1" fill="${C.Y}"/>
   <rect x="18" y="3" width="2" height="2" fill="${C.A}"/><rect x="28" y="3" width="2" height="2" fill="${C.A}"/>
   <circle cx="24" cy="8.6" r="6" fill="${C.Y}"/>
   <ellipse cx="24" cy="12" rx="3.4" ry="2.4" fill="${C.C}"/>
   <rect x="23" y="10" width="2" height="2" fill="${C.E}"/><rect x="22" y="13" width="4" height="1" fill="${C.E}"/>
   <rect x="20" y="7" width="2" height="2" fill="${C.E}"/><rect x="26" y="7" width="2" height="2" fill="${C.E}"/>
   <polygon points="21.4,3.6 22.4,0.8 24,2.4 25.6,0.8 26.6,3.6" fill="${C.A}"/>
  </svg>`},
 // 7 — Stitch: blue alien, huge ears, big black eyes, a wide grin; sunset sky and sand
 {title:'Стич', family:'Космос', subject:'синий ушастый Стич с огромными ушами, большими чёрными глазами и широкой улыбкой',
  source:U, reference:'Lilo & Stitch — Stitch', forLevel:61, cells:900, colours:8,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.O}"/><rect y="25" width="30" height="5" fill="${C.Y}"/>
   <ellipse cx="6.4" cy="8.6" rx="3.2" ry="6" transform="rotate(-48 6.4 8.6)" fill="${C.B}"/>
   <ellipse cx="23.6" cy="8.6" rx="3.2" ry="6" transform="rotate(48 23.6 8.6)" fill="${C.B}"/>
   <ellipse cx="6.6" cy="8.8" rx="1.6" ry="4.4" transform="rotate(-48 6.6 8.8)" fill="${C.K}"/>
   <ellipse cx="23.4" cy="8.8" rx="1.6" ry="4.4" transform="rotate(48 23.4 8.8)" fill="${C.K}"/>
   <ellipse cx="11.6" cy="27" rx="2.4" ry="1.4" fill="${C.B}"/><ellipse cx="18.4" cy="27" rx="2.4" ry="1.4" fill="${C.B}"/>
   <ellipse cx="8.4" cy="22.6" rx="1.7" ry="2.6" fill="${C.B}"/><ellipse cx="21.6" cy="22.6" rx="1.7" ry="2.6" fill="${C.B}"/>
   <ellipse cx="15" cy="23.4" rx="5.4" ry="3.8" fill="${C.B}"/>
   <ellipse cx="15" cy="24" rx="3" ry="2.6" fill="${C.S}"/>
   <ellipse cx="15" cy="13" rx="9.2" ry="7.6" fill="${C.B}"/>
   <ellipse cx="15" cy="7" rx="4" ry="2" fill="${C.N}"/>
   <ellipse cx="15" cy="17.4" rx="6.4" ry="3.2" fill="${C.S}"/>
   <ellipse cx="10.4" cy="11.8" rx="2.8" ry="3.4" fill="${C.D}"/><ellipse cx="19.6" cy="11.8" rx="2.8" ry="3.4" fill="${C.D}"/>
   <rect x="9" y="10" width="2" height="2" fill="${C.C}"/><rect x="18.2" y="10" width="2" height="2" fill="${C.C}"/>
   <ellipse cx="15" cy="13.8" rx="2.3" ry="1.5" fill="${C.N}"/>
   <path d="M9 16.4 L21 16.4 Q20 20.6 15 20.6 Q10 20.6 9 16.4 Z" fill="${C.D}"/>
   <rect x="10" y="16.4" width="10" height="1.4" fill="${C.C}"/>
  </svg>`},
 // 8 — Peppa Pig with her side-profile head like a hairdryer, red dress, jumping in a muddy puddle on the green hill
 {title:'Свинка Пеппа в луже', family:'Домашние животные', subject:'розовая свинка Пеппа в красном платье, голова в профиль, прыгает в грязную лужу',
  source:U, reference:'Peppa Pig — Peppa Pig', forLevel:61, cells:900, colours:7,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.S}"/><ellipse cx="15" cy="35" rx="24" ry="11" fill="${C.G}"/>
   <ellipse cx="13" cy="28.4" rx="9" ry="1.6" fill="${C.A}"/>
   <rect x="2" y="25" width="2" height="2" fill="${C.A}"/><rect x="23" y="24" width="2" height="2" fill="${C.A}"/><rect x="20" y="22" width="2" height="2" fill="${C.A}"/>
   <polygon points="7.6,6 8,1 11.4,5" fill="${C.K}"/><polygon points="11,5 13,0.8 14.4,5.4" fill="${C.K}"/>
   <ellipse cx="11.6" cy="10.4" rx="6.6" ry="5.6" fill="${C.K}"/>
   <ellipse cx="19" cy="7.4" rx="6.4" ry="3.4" transform="rotate(-20 19 7.4)" fill="${C.K}"/>
   <ellipse cx="24.6" cy="5.4" rx="1.8" ry="3.2" transform="rotate(-20 24.6 5.4)" fill="${C.P}"/>
   <rect x="15" y="5" width="2" height="2" fill="${C.D}"/><rect x="19" y="4" width="2" height="2" fill="${C.D}"/>
   <rect x="8" y="10" width="3" height="3" rx="1" fill="${C.P}"/>
   <rect x="14" y="12" width="4" height="1" fill="${C.D}"/><rect x="18" y="11" width="2" height="1" fill="${C.D}"/>
   <rect x="4" y="18" width="4" height="2" fill="${C.K}"/><rect x="3" y="16" width="2" height="3" fill="${C.K}"/>
   <rect x="17" y="18" width="4" height="2" fill="${C.K}"/><rect x="20" y="16" width="2" height="3" fill="${C.K}"/>
   <polygon points="9.4,16 15.6,16 19,25 6,25" fill="${C.R}"/>
   <rect x="9" y="25" width="2" height="2" fill="${C.K}"/><rect x="14" y="25" width="2" height="2" fill="${C.K}"/>
   <rect x="8" y="27" width="3" height="1" fill="${C.D}"/><rect x="14" y="27" width="3" height="1" fill="${C.D}"/>
  </svg>`},
 // 9 — Olaf: snowman with a carrot nose, twig arms and hair, one buck tooth; falling snow, compact
 {title:'Олаф', family:'Времена года и погода', direction:'Ожившие вещи', subject:'снеговик Олаф с носом-морковкой, руками-веточками и одним большим зубом, идёт снег',
  source:U, reference:'Frozen — Olaf', forLevel:61, cells:378, colours:5,
  svg:`<svg viewBox="0 0 18 21" xmlns="http://www.w3.org/2000/svg">
   <rect width="18" height="21" fill="${C.B}"/>
   <rect x="6" y="0" width="1" height="3" fill="${C.E}"/><rect x="8" y="0" width="1" height="3" fill="${C.E}"/><rect x="10" y="0" width="1" height="3" fill="${C.E}"/>
   <rect x="1" y="14" width="4" height="1" fill="${C.E}"/><rect x="1" y="12" width="1" height="2" fill="${C.E}"/>
   <rect x="13" y="14" width="4" height="1" fill="${C.E}"/><rect x="16" y="12" width="1" height="2" fill="${C.E}"/>
   <ellipse cx="9" cy="6.6" rx="5" ry="4" fill="${C.C}"/><ellipse cx="9" cy="9" rx="6" ry="3" fill="${C.C}"/>
   <ellipse cx="9" cy="14.6" rx="3.6" ry="2" fill="${C.C}"/>
   <ellipse cx="9" cy="18.8" rx="5" ry="2.2" fill="${C.C}"/>
   <rect x="6" y="4" width="2" height="2" fill="${C.D}"/><rect x="10" y="4" width="2" height="2" fill="${C.D}"/>
   <rect x="8" y="7" width="5" height="2" fill="${C.O}"/><rect x="13" y="7" width="2" height="1" fill="${C.O}"/>
   <rect x="4" y="9" width="10" height="1" fill="${C.D}"/><rect x="5" y="10" width="8" height="1" fill="${C.D}"/><rect x="7" y="11" width="4" height="1" fill="${C.D}"/>
   <rect x="8" y="9" width="2" height="2" fill="${C.C}"/>
   <rect x="8" y="14" width="2" height="2" fill="${C.D}"/><rect x="8" y="18" width="2" height="2" fill="${C.D}"/>
   <rect x="1" y="2" width="2" height="2" fill="${C.C}"/><rect x="15" y="2" width="2" height="2" fill="${C.C}"/><rect x="1" y="18" width="2" height="2" fill="${C.C}"/><rect x="15" y="17" width="2" height="2" fill="${C.C}"/>
  </svg>`},
 // 10 — Winnie the Pooh (Disney): golden bear cub in a short red shirt hugging a jar of honey; meadow
 {title:'Винни-Пух с мёдом', family:'Лесные и горные животные', subject:'золотистый медвежонок Винни-Пух в короткой красной футболке обнимает банку мёда',
  source:U, reference:'Winnie the Pooh (Disney) — Winnie the Pooh', forLevel:61, cells:900, colours:8,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.S}"/><rect y="25" width="30" height="5" fill="${C.G}"/>
   <circle cx="7.6" cy="4.2" r="2.4" fill="${C.Y}"/><circle cx="18.4" cy="4.2" r="2.4" fill="${C.Y}"/>
   <circle cx="7.6" cy="4.2" r="1.2" fill="${C.O}"/><circle cx="18.4" cy="4.2" r="1.2" fill="${C.O}"/>
   <ellipse cx="13" cy="20.6" rx="6.6" ry="5.6" fill="${C.Y}"/>
   <ellipse cx="9.4" cy="26" rx="3" ry="1.8" fill="${C.Y}"/><ellipse cx="16.6" cy="26" rx="3" ry="1.8" fill="${C.Y}"/>
   <rect x="6.4" y="13" width="13.2" height="5.6" rx="2" fill="${C.R}"/>
   <rect x="4.6" y="13.6" width="3" height="3.6" rx="1" fill="${C.R}"/><rect x="18.4" y="13.6" width="3" height="3.6" rx="1" fill="${C.R}"/>
   <ellipse cx="4.6" cy="19" rx="1.8" ry="2.6" fill="${C.Y}"/>
   <ellipse cx="13" cy="8.6" rx="6.4" ry="5.4" fill="${C.Y}"/>
   <rect x="9" y="6.6" width="2" height="2" fill="${C.D}"/><rect x="15" y="6.6" width="2" height="2" fill="${C.D}"/>
   <ellipse cx="13" cy="10.2" rx="2" ry="1.3" fill="${C.D}"/>
   <path d="M9.6 11.8 L16.4 11.8 Q15.8 14 13 14 Q10.2 14 9.6 11.8 Z" fill="${C.D}"/>
   <rect x="10.6" y="11.8" width="4.8" height="1" fill="${C.Y}"/>
   <ellipse cx="23" cy="21" rx="4.8" ry="4.6" fill="${C.A}"/>
   <rect x="19.8" y="15.2" width="6.4" height="2.6" rx="0.8" fill="${C.A}"/>
   <ellipse cx="23" cy="15.4" rx="3.6" ry="1.4" fill="${C.O}"/>
   <rect x="23.4" y="15.4" width="2" height="4" rx="0.8" fill="${C.O}"/>
   <rect x="20" y="20" width="6" height="3.4" rx="0.6" fill="${C.C}"/>
   <ellipse cx="18.8" cy="21" rx="2" ry="2.4" fill="${C.Y}"/>
  </svg>`},
 // 11 — Masha: girl in a pink headscarf and pink dress, big green eyes, blond fringe; sky and grass
 {title:'Маша', family:'Герои приключений', subject:'девочка Маша в розовом платке и розовом сарафане, большие глаза',
  source:U, reference:'Маша и Медведь — Маша', forLevel:61, cells:896, colours:8,
  svg:`<svg viewBox="0 0 28 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="32" fill="${C.S}"/><rect y="27" width="28" height="5" fill="${C.G}"/>
   <rect x="10.5" y="24" width="2.4" height="3.5" fill="${C.K}"/><rect x="15.1" y="24" width="2.4" height="3.5" fill="${C.K}"/>
   <ellipse cx="11.2" cy="27.6" rx="2.2" ry="1.2" fill="${C.R}"/><ellipse cx="16.8" cy="27.6" rx="2.2" ry="1.2" fill="${C.R}"/>
   <polygon points="11,18 17,18 21.5,25 6.5,25" fill="${C.P}"/>
   <polygon points="10.5,18.5 6.5,21.5 5.5,20 10,17.5" fill="${C.P}"/><circle cx="5.2" cy="21" r="1.5" fill="${C.K}"/>
   <polygon points="17.5,18.5 21.5,14.5 23,15.5 18,19.5" fill="${C.P}"/><circle cx="22.6" cy="14.2" r="1.5" fill="${C.K}"/>
   <circle cx="14" cy="10.5" r="8.4" fill="${C.P}"/>
   <polygon points="12,17 16,17 17.5,19.5 14,18.6 10.5,19.5" fill="${C.P}"/>
   <ellipse cx="14" cy="11.8" rx="6.2" ry="5.8" fill="${C.K}"/>
   <path d="M7.8,10 Q8.5,6 14,6 Q19.5,6 20.2,10 L18,8.8 L16,9.8 L14,8.6 L12,9.8 L10,8.8 Z" fill="${C.Y}"/>
   <rect x="9" y="10.6" width="4" height="4" rx="1.4" fill="${C.G}"/><rect x="15" y="10.6" width="4" height="4" rx="1.4" fill="${C.G}"/>
   <rect x="10" y="12" width="2" height="2" fill="${C.D}"/><rect x="16" y="12" width="2" height="2" fill="${C.D}"/>
   <rect x="12" y="16" width="4" height="1" fill="${C.R}"/><rect x="13" y="17" width="2" height="1" fill="${C.R}"/>
  </svg>`},
 // 12 — a Minion: yellow capsule body, one big goggle, blue overalls; sky and grass
 {title:'Миньон', family:'Вирусные анимационные персонажи и мини-вселенные', subject:'жёлтый миньон-капсула с одним большим глазом в очках и в синем комбинезоне',
  source:U, reference:'Гадкий я — Minion (Stuart)', forLevel:61, cells:896, colours:8,
  svg:`<svg viewBox="0 0 28 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="32" fill="${C.K}"/><rect y="28" width="28" height="4" fill="${C.G}"/>
   <rect x="10" y="25" width="3" height="3" fill="${C.N}"/><rect x="15" y="25" width="3" height="3" fill="${C.N}"/>
   <ellipse cx="10.8" cy="28.2" rx="2.6" ry="1.2" fill="${C.D}"/><ellipse cx="17.2" cy="28.2" rx="2.6" ry="1.2" fill="${C.D}"/>
   <rect x="4.6" y="17" width="2.6" height="6" rx="1.2" fill="${C.Y}"/><circle cx="5.9" cy="23.4" r="1.7" fill="${C.D}"/>
   <polygon points="20.8,18 22.6,17.2 25.4,12.6 23.4,11.8" fill="${C.Y}"/><circle cx="24.6" cy="11.6" r="1.7" fill="${C.D}"/>
   <rect x="7" y="3" width="14" height="24" rx="7" fill="${C.N}"/>
   <path d="M7,10 A7,7 0 0 1 21,10 L21,21 L7,21 Z" fill="${C.Y}"/>
   <rect x="10" y="19" width="8" height="2" fill="${C.N}"/>
   <polygon points="7.2,15.6 8.8,14.8 12,19.2 10,19.4" fill="${C.N}"/><polygon points="20.8,15.6 19.2,14.8 16,19.2 18,19.4" fill="${C.N}"/>
   <rect x="11" y="0.6" width="1" height="3" fill="${C.D}"/><rect x="14" y="0.2" width="1" height="3.4" fill="${C.D}"/><rect x="16" y="0.8" width="1" height="3" fill="${C.D}"/>
   <rect x="7" y="10" width="14" height="2" fill="${C.D}"/>
   <circle cx="14" cy="11" r="4.6" fill="${C.S}"/>
   <circle cx="14" cy="11" r="3.2" fill="${C.C}"/>
   <rect x="12" y="9" width="4" height="4" rx="0.5" fill="${C.A}"/>
   <rect x="13" y="10" width="2" height="2" fill="${C.D}"/>
   <rect x="11" y="16" width="6" height="1" fill="${C.D}"/><rect x="12" y="17" width="4" height="1" fill="${C.D}"/>
  </svg>`},
 // 13 — Shrek: green ogre with trumpet ears, white shirt and brown vest, waist-deep in his swamp
 {title:'Шрек в болоте', family:'Волшебные существа', subject:'зелёный огр Шрек с ушами-трубочками в белой рубахе и коричневом жилете',
  source:U, reference:'Шрек — Shrek', forLevel:61, cells:1224, colours:7,
  svg:`<svg viewBox="0 0 34 36" xmlns="http://www.w3.org/2000/svg">
   <rect width="34" height="36" fill="${C.S}"/>
   <rect x="4" y="22" width="26" height="12" rx="5" fill="${C.C}"/>
   <polygon points="4,25 12,22.4 13.5,32 4,32" fill="${C.E}"/><polygon points="30,25 22,22.4 20.5,32 30,32" fill="${C.E}"/>
   <rect x="13" y="19" width="8" height="5" fill="${C.G}"/>
   <polygon points="14,22.5 17,26 20,22.5" fill="${C.G}"/>
   <rect x="16" y="26" width="2" height="4" fill="${C.A}"/>
   <polygon points="9.6,7.4 5.2,4.4 3.6,6.2 8.8,10" fill="${C.G}"/><polygon points="24.4,7.4 28.8,4.4 30.4,6.2 25.2,10" fill="${C.G}"/>
   <circle cx="4.2" cy="4.6" r="2.3" fill="${C.G}"/><circle cx="29.8" cy="4.6" r="2.3" fill="${C.G}"/>
   <rect x="3" y="3.6" width="2" height="2" fill="${C.M}"/><rect x="29" y="3.6" width="2" height="2" fill="${C.M}"/>
   <ellipse cx="17" cy="11.5" rx="8" ry="8.4" fill="${C.G}"/>
   <ellipse cx="17" cy="15" rx="9.2" ry="5.6" fill="${C.G}"/>
   <rect x="10" y="9" width="6" height="1" fill="${C.M}"/><rect x="18" y="9" width="6" height="1" fill="${C.M}"/>
   <rect x="11" y="10" width="4" height="3" fill="${C.C}"/><rect x="19" y="10" width="4" height="3" fill="${C.C}"/>
   <rect x="13" y="11" width="2" height="2" fill="${C.A}"/><rect x="19" y="11" width="2" height="2" fill="${C.A}"/>
   <rect x="15" y="14" width="4" height="1" fill="${C.M}"/>
   <rect x="12" y="16" width="10" height="1" fill="${C.D}"/><rect x="12" y="15" width="1" height="1" fill="${C.D}"/><rect x="21" y="15" width="1" height="1" fill="${C.D}"/>
   <rect y="30" width="34" height="6" fill="${C.M}"/>
  </svg>`},
 // 14 — Woody: cowboy hat, yellow checked shirt, cow vest with the sheriff star, jeans; Andy's cloud wallpaper and floor
 {title:'Шериф Вуди', family:'Игрушки и настольные игры', direction:'Ожившие вещи', subject:'ковбой Вуди в шляпе, жёлтой клетчатой рубашке, коровьем жилете со звездой шерифа',
  source:U, reference:'История игрушек — Woody', forLevel:61, cells:1344, colours:9,
  svg:`<svg viewBox="0 0 32 42" xmlns="http://www.w3.org/2000/svg">
   <rect width="32" height="42" fill="${C.S}"/><rect y="40" width="32" height="2" fill="${C.A}"/>
   <ellipse cx="4.5" cy="22" rx="3" ry="1.5" fill="${C.C}"/><ellipse cx="27.5" cy="32" rx="3" ry="1.5" fill="${C.C}"/><ellipse cx="27" cy="13" rx="2.6" ry="1.4" fill="${C.C}"/>
   <rect x="11" y="30" width="4" height="7" fill="${C.N}"/><rect x="17" y="30" width="4" height="7" fill="${C.N}"/>
   <rect x="10" y="36" width="5" height="4" rx="1" fill="${C.E}"/><rect x="17" y="36" width="5" height="4" rx="1" fill="${C.E}"/>
   <rect x="6" y="18" width="20" height="10" rx="1.5" fill="${C.Y}"/>
   <rect x="6" y="21" width="3" height="1" fill="${C.R}"/><rect x="6" y="25" width="3" height="1" fill="${C.R}"/><rect x="7" y="18" width="1" height="10" fill="${C.R}"/>
   <rect x="23" y="21" width="3" height="1" fill="${C.R}"/><rect x="23" y="25" width="3" height="1" fill="${C.R}"/><rect x="24" y="18" width="1" height="10" fill="${C.R}"/>
   <rect x="9" y="18" width="14" height="11" fill="${C.Y}"/>
   <rect x="14" y="22" width="4" height="1" fill="${C.R}"/><rect x="14" y="26" width="4" height="1" fill="${C.R}"/>
   <rect x="6" y="28" width="3" height="2" rx="0.8" fill="${C.K}"/><rect x="23" y="28" width="3" height="2" rx="0.8" fill="${C.K}"/>
   <rect x="9" y="18" width="5" height="11" fill="${C.C}"/><rect x="18" y="18" width="5" height="11" fill="${C.C}"/>
   <rect x="9" y="20" width="2" height="3" fill="${C.D}"/><rect x="11" y="25" width="3" height="2" fill="${C.D}"/>
   <rect x="21" y="25" width="2" height="3" fill="${C.D}"/>
   <polygon points="20.5,18.8 21.3,20.9 23.4,20.9 21.7,22.2 22.4,24.4 20.5,23.1 18.6,24.4 19.3,22.2 17.6,20.9 19.7,20.9" fill="${C.Y}"/>
   <rect x="9" y="29" width="14" height="1" fill="${C.E}"/><rect x="15" y="29" width="2" height="2" fill="${C.Y}"/>
   <polygon points="12,17 20,17 16,21.4" fill="${C.R}"/>
   <rect x="14" y="16" width="4" height="2" fill="${C.K}"/>
   <ellipse cx="16" cy="11.6" rx="5" ry="5" fill="${C.K}"/>
   <rect x="11" y="7" width="10" height="2" fill="${C.E}"/><rect x="11" y="7" width="1" height="5" fill="${C.E}"/><rect x="20" y="7" width="1" height="5" fill="${C.E}"/>
   <rect x="12" y="10" width="3" height="3" fill="${C.C}"/><rect x="17" y="10" width="3" height="3" fill="${C.C}"/>
   <rect x="13" y="11" width="2" height="2" fill="${C.E}"/><rect x="17" y="11" width="2" height="2" fill="${C.E}"/>
   <rect x="13" y="14" width="6" height="1" fill="${C.R}"/><rect x="14" y="15" width="4" height="1" fill="${C.R}"/>
   <ellipse cx="16" cy="6" rx="10" ry="1.1" fill="${C.A}"/>
   <rect x="6" y="4" width="2" height="2" fill="${C.A}"/><rect x="24" y="4" width="2" height="2" fill="${C.A}"/>
   <rect x="10" y="0.8" width="12" height="5" rx="2" fill="${C.A}"/>
   <rect x="10" y="4" width="12" height="1" fill="${C.E}"/>
  </svg>`},
 // 15 — Sulley: big turquoise furry monster with purple spots and horns; a warm wall and floor
 {title:'Салли', family:'Волшебные существа', subject:'большой бирюзовый мохнатый монстр Салли с фиолетовыми пятнами и рожками',
  source:U, reference:'Корпорация монстров — James P. Sullivan (Sulley)', forLevel:61, cells:1224, colours:7,
  svg:`<svg viewBox="0 0 34 36" xmlns="http://www.w3.org/2000/svg">
   <rect width="34" height="36" fill="${C.Y}"/><rect y="32" width="34" height="4" fill="${C.A}"/>
   <polygon points="11.5,6.5 9.4,1.2 14,4.4" fill="${C.C}"/><polygon points="22.5,6.5 24.6,1.2 20,4.4" fill="${C.C}"/>
   <rect x="10" y="28" width="5" height="5" rx="1.5" fill="${C.B}"/><rect x="19" y="28" width="5" height="5" rx="1.5" fill="${C.B}"/>
   <rect x="10" y="31.6" width="5" height="1.4" fill="${C.C}"/><rect x="19" y="31.6" width="5" height="1.4" fill="${C.C}"/>
   <ellipse cx="17" cy="21" rx="11.4" ry="9.4" fill="${C.B}"/>
   <ellipse cx="17" cy="11" rx="8" ry="6.6" fill="${C.B}"/>
   <ellipse cx="5.6" cy="22" rx="3" ry="7" fill="${C.B}"/><ellipse cx="28.4" cy="22" rx="3" ry="7" fill="${C.B}"/>
   <rect x="4" y="27.6" width="4" height="1.4" fill="${C.C}"/><rect x="26" y="27.6" width="4" height="1.4" fill="${C.C}"/>
   <circle cx="10" cy="20" r="1.8" fill="${C.V}"/><circle cx="23.5" cy="20" r="2" fill="${C.V}"/><circle cx="13.5" cy="25.5" r="1.8" fill="${C.V}"/>
   <circle cx="21" cy="26" r="1.6" fill="${C.V}"/><circle cx="17" cy="21.5" r="1.4" fill="${C.V}"/><circle cx="12" cy="6.4" r="1.4" fill="${C.V}"/>
   <circle cx="22.2" cy="6.8" r="1.5" fill="${C.V}"/><circle cx="5.2" cy="18" r="1.4" fill="${C.V}"/><circle cx="28.8" cy="25" r="1.4" fill="${C.V}"/>
   <rect x="11" y="8" width="4" height="1" fill="${C.N}"/><rect x="19" y="8" width="4" height="1" fill="${C.N}"/>
   <rect x="12" y="9" width="3" height="3" fill="${C.C}"/><rect x="19" y="9" width="3" height="3" fill="${C.C}"/>
   <rect x="13" y="10" width="2" height="2" fill="${C.N}"/><rect x="19" y="10" width="2" height="2" fill="${C.N}"/>
   <ellipse cx="17" cy="13" rx="2.2" ry="1.2" fill="${C.N}"/>
   <path d="M10.5,15 L23.5,15 Q22.4,19 17,19 Q11.6,19 10.5,15 Z" fill="${C.D}"/>
   <rect x="11" y="15" width="12" height="1" fill="${C.C}"/>
  </svg>`},
 // 16 — Remy in a chef's toque: blue-grey rat, pink ears and nose; a warm kitchen wall and a wooden floor
 {title:'Реми в колпаке повара', family:'Профессии и увлечения', direction:'Обмен ролями', subject:'серо-голубой крысёнок Реми в белом поварском колпаке',
  source:U, reference:'Рататуй — Remy', forLevel:61, cells:896, colours:8,
  svg:`<svg viewBox="0 0 28 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="32" fill="${C.O}"/><rect y="28" width="28" height="4" fill="${C.A}"/>
   <polygon points="18,25 24,24 25.5,19 23.5,19 22.6,22.4 18,23" fill="${C.N}"/>
   <ellipse cx="11.5" cy="28.2" rx="2.4" ry="1.2" fill="${C.K}"/><ellipse cx="16.5" cy="28.2" rx="2.4" ry="1.2" fill="${C.K}"/>
   <ellipse cx="14" cy="23" rx="5.6" ry="5.4" fill="${C.N}"/>
   <ellipse cx="14" cy="23.6" rx="4.6" ry="4.4" fill="${C.S}"/>
   <ellipse cx="14" cy="24.4" rx="2.8" ry="3.2" fill="${C.C}"/>
   <circle cx="8.6" cy="22.6" r="1.4" fill="${C.K}"/><circle cx="19.4" cy="22.6" r="1.4" fill="${C.K}"/>
   <circle cx="7" cy="10" r="3.8" fill="${C.N}"/><circle cx="21" cy="10" r="3.8" fill="${C.N}"/>
   <circle cx="7" cy="10" r="2.9" fill="${C.S}"/><circle cx="21" cy="10" r="2.9" fill="${C.S}"/>
   <circle cx="7.2" cy="10.2" r="1.9" fill="${C.K}"/><circle cx="20.8" cy="10.2" r="1.9" fill="${C.K}"/>
   <path d="M8,13 Q8,7 14,7 Q20,7 20,13 Q19,17 14,20.6 Q9,17 8,13 Z" fill="${C.N}"/>
   <path d="M9,13 Q9,8 14,8 Q19,8 19,13 Q18,16.4 14,19.4 Q10,16.4 9,13 Z" fill="${C.S}"/>
   <path d="M10.6,15 Q14,13.6 17.4,15 Q16,17.6 14,19 Q12,17.6 10.6,15 Z" fill="${C.C}"/>
   <rect x="12" y="18" width="4" height="2" rx="0.6" fill="${C.P}"/>
   <rect x="11" y="11" width="2" height="3" fill="${C.D}"/><rect x="15" y="11" width="2" height="3" fill="${C.D}"/>
   <rect x="9.6" y="5.2" width="8.8" height="3" rx="0.8" fill="${C.C}"/>
   <circle cx="11" cy="3.6" r="2.4" fill="${C.C}"/><circle cx="14" cy="2.8" r="2.6" fill="${C.C}"/><circle cx="17" cy="3.6" r="2.4" fill="${C.C}"/>
  </svg>`},
 // 17 — Anger: small red brick with flames on his head, white shirt, dark tie; the emotions' headquarters
 {title:'Гнев', family:'Мемные реакции и типажи', direction:'Театр эмоций', subject:'маленький красный кирпичик Гнев с пламенем на голове, в белой рубашке и галстуке',
  source:U, reference:'Головоломка — Anger', forLevel:61, cells:784, colours:8,
  svg:`<svg viewBox="0 0 28 28" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="28" fill="${C.V}"/><rect y="25" width="28" height="3" fill="${C.N}"/>
   <polygon points="7.5,8 6.6,3.4 9.4,5.2 10.4,1 12.8,4.4 14,0.6 15.2,4.4 17.6,1 18.6,5.2 21.4,3.4 20.5,8" fill="${C.O}"/>
   <polygon points="9.6,8 9.8,5.8 11.4,6.8 12.4,3.6 14,6.2 15.6,3.6 16.6,6.8 18.2,5.8 18.4,8" fill="${C.Y}"/>
   <rect x="9" y="23" width="3" height="3" fill="${C.E}"/><rect x="16" y="23" width="3" height="3" fill="${C.E}"/>
   <ellipse cx="10" cy="25.8" rx="2.6" ry="1.2" fill="${C.D}"/><ellipse cx="18" cy="25.8" rx="2.6" ry="1.2" fill="${C.D}"/>
   <rect x="4" y="17" width="3" height="5" rx="1.2" fill="${C.R}"/><rect x="21" y="17" width="3" height="5" rx="1.2" fill="${C.R}"/>
   <rect x="4" y="17" width="3" height="2" fill="${C.C}"/><rect x="21" y="17" width="3" height="2" fill="${C.C}"/>
   <rect x="7" y="7" width="14" height="17" rx="1.2" fill="${C.R}"/>
   <rect x="7" y="17" width="14" height="5" fill="${C.C}"/>
   <rect x="7" y="22" width="14" height="2" fill="${C.E}"/>
   <polygon points="13,17 15,17 15,20.6 14,22 13,20.6" fill="${C.D}"/>
   <rect x="8" y="9" width="3" height="1" fill="${C.D}"/><rect x="17" y="9" width="3" height="1" fill="${C.D}"/>
   <rect x="9" y="10" width="4" height="1" fill="${C.D}"/><rect x="15" y="10" width="4" height="1" fill="${C.D}"/>
   <rect x="11" y="11" width="6" height="1" fill="${C.D}"/>
   <rect x="8" y="12" width="4" height="2" fill="${C.C}"/><rect x="16" y="12" width="4" height="2" fill="${C.C}"/>
   <rect x="10" y="12" width="2" height="2" fill="${C.D}"/><rect x="16" y="12" width="2" height="2" fill="${C.D}"/>
   <rect x="10" y="14" width="8" height="3" fill="${C.D}"/>
   <rect x="11" y="15" width="6" height="1" fill="${C.C}"/>
  </svg>`},
 // 18 — No-Face: black cloak, white mask with purple marks; compact, empty around
 {title:'Безликий', family:'Маски и костюмы', direction:'Фольклорная фантазия', subject:'чёрный безликий в белой маске с фиолетовыми отметинами',
  source:U, reference:'Унесённые призраками — Kaonashi (No-Face)', forLevel:61, cells:385, colours:3,
  svg:`<svg viewBox="0 0 16 28" xmlns="http://www.w3.org/2000/svg">
   <path d="M1,27.6 L2,9 Q2.4,0.6 8,0.6 Q13.6,0.6 14,9 L15,27.6 Z" fill="${C.D}"/>
   <ellipse cx="8" cy="9.4" rx="4.6" ry="6.4" fill="${C.C}"/>
   <rect x="5" y="5" width="2" height="3" fill="${C.V}"/><rect x="9" y="5" width="2" height="3" fill="${C.V}"/>
   <rect x="5" y="8" width="2" height="2" fill="${C.D}"/><rect x="9" y="8" width="2" height="2" fill="${C.D}"/>
   <rect x="5" y="10" width="2" height="3" fill="${C.V}"/><rect x="9" y="10" width="2" height="3" fill="${C.V}"/>
   <rect x="6" y="14" width="4" height="1" fill="${C.D}"/>
  </svg>`},
 // 19 — Calcifer: fire demon with two big eyes on his logs in a brick fireplace
 {title:'Кальцифер', family:'Свет и домашний декор', direction:'Домашняя магия', subject:'огненный демон Кальцифер с большими глазами на поленьях в камине',
  source:U, reference:'Ходячий замок — Calcifer', forLevel:61, cells:1156, colours:7,
  svg:`<svg viewBox="0 0 34 34" xmlns="http://www.w3.org/2000/svg">
   <rect width="34" height="34" fill="${C.A}"/>
   <path d="M4,31 L4,14 Q4,4 17,4 Q30,4 30,14 L30,31 Z" fill="${C.D}"/>
   <polygon points="6,28 27,25 28,28 7,31" fill="${C.E}"/><polygon points="6,25 28,28.5 27,31 5.5,28" fill="${C.E}"/>
   <circle cx="6.4" cy="26.6" r="1.6" fill="${C.A}"/><circle cx="27.5" cy="26.6" r="1.6" fill="${C.A}"/>
   <path d="M17,5 Q19,10 22,11 Q21,8 23,6.5 Q24,11 26.5,14 Q28,18 27.5,21 Q27,27 17,28 Q7,27 6.5,21 Q6,18 7.5,14 Q10,11 11,6.5 Q13,8 12,11 Q15,10 17,5 Z" fill="${C.R}"/>
   <path d="M17,9 Q19,13 21.5,13.6 Q23,15 24.5,17.5 Q25.5,22 22,25 Q20,26.4 17,26.4 Q14,26.4 12,25 Q8.5,22 9.5,17.5 Q11,15 12.5,13.6 Q15,13 17,9 Z" fill="${C.O}"/>
   <ellipse cx="17" cy="21" rx="6" ry="5" fill="${C.Y}"/>
   <ellipse cx="13.5" cy="18.4" rx="2.5" ry="3" fill="${C.C}"/><ellipse cx="20.5" cy="18.4" rx="2.5" ry="3" fill="${C.C}"/>
   <rect x="14" y="18" width="2" height="2" fill="${C.D}"/><rect x="18" y="18" width="2" height="2" fill="${C.D}"/>
   <path d="M14,22.6 L20,22.6 Q19.4,25 17,25 Q14.6,25 14,22.6 Z" fill="${C.D}"/>
  </svg>`},
 // 20 — Po the Kung Fu Panda in a kung-fu stance: green-gold shorts, one fist forward and one raised; sunset sky and a wooden court
 {title:'Кунг-фу Панда По', family:'Спорт и активные игры', subject:'панда По в зелёных шортах с золотым поясом в стойке кунг-фу',
  source:U, reference:'Kung Fu Panda — Po', forLevel:61, cells:1216, colours:6,
  svg:`<svg viewBox="0 0 38 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="38" height="32" fill="${C.O}"/><rect y="28" width="38" height="4" fill="${C.A}"/>
   <circle cx="13.6" cy="3.6" r="2.4" fill="${C.D}"/><circle cx="24.4" cy="3.6" r="2.4" fill="${C.D}"/>
   <polygon points="13.4,25 17.4,26.6 13.6,30.4 7.6,30.4 7.6,28.4" fill="${C.D}"/>
   <polygon points="24.6,25 20.6,26.6 24.4,30.4 30.4,30.4 30.4,28.4" fill="${C.D}"/>
   <rect x="5.4" y="14" width="8" height="3.6" rx="1.6" fill="${C.D}"/><circle cx="5.4" cy="15.8" r="2.4" fill="${C.D}"/>
   <rect x="24.6" y="13.4" width="6.6" height="3.6" rx="1.6" fill="${C.D}"/><rect x="28" y="7" width="3.6" height="9" rx="1.6" fill="${C.D}"/><circle cx="29.8" cy="6.4" r="2.4" fill="${C.D}"/>
   <ellipse cx="19" cy="16.4" rx="7.6" ry="3.8" fill="${C.D}"/>
   <ellipse cx="19" cy="20.4" rx="6.4" ry="5.6" fill="${C.C}"/>
   <rect x="12.4" y="22.4" width="13.2" height="5.4" rx="1.2" fill="${C.M}"/>
   <rect x="12.4" y="22.4" width="13.2" height="1.6" fill="${C.Y}"/><rect x="17.6" y="22.4" width="2.8" height="3.6" fill="${C.Y}"/>
   <circle cx="19" cy="8.6" r="6.6" fill="${C.C}"/>
   <ellipse cx="16.4" cy="8.6" rx="2" ry="2.6" transform="rotate(25 16.4 8.6)" fill="${C.D}"/>
   <ellipse cx="21.6" cy="8.6" rx="2" ry="2.6" transform="rotate(-25 21.6 8.6)" fill="${C.D}"/>
   <rect x="16" y="8" width="1.6" height="1.6" fill="${C.C}"/><rect x="20.4" y="8" width="1.6" height="1.6" fill="${C.C}"/>
   <ellipse cx="19" cy="11.2" rx="1.4" ry="1" fill="${C.D}"/>
   <path d="M16.4 12.4 L21.6 12.4 Q21 14.6 19 14.6 Q17 14.6 16.4 12.4 Z" fill="${C.D}"/>
  </svg>`},
]};
