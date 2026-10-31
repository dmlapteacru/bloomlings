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
 // 2 — Crocodile Gena: green crocodile in profile, black hat, red coat, white shirt with a bow tie; sky and a path
 {title:'Крокодил Гена', family:'Саванна и джунгли', subject:'крокодил Гена в шляпе и красном пальто с бабочкой',
  source:U, reference:'Крокодил Гена и Чебурашка — Крокодил Гена', forLevel:61, cells:1216, colours:8,
  svg:`<svg viewBox="0 0 32 38" xmlns="http://www.w3.org/2000/svg">
   <rect width="32" height="38" fill="${C.S}"/><rect y="34" width="32" height="4" fill="${C.A}"/>
   <polygon points="9,24 3.6,28.4 2.2,33 6.4,33.4 10,30" fill="${C.G}"/>
   <rect x="10" y="30" width="3" height="4" fill="${C.G}"/><rect x="17" y="30" width="3" height="4" fill="${C.G}"/>
   <ellipse cx="11" cy="34" rx="3" ry="1.4" fill="${C.D}"/><ellipse cx="19.5" cy="34" rx="3" ry="1.4" fill="${C.D}"/>
   <polygon points="10,17 20,17 22,19 23,31 7,31 8,19" fill="${C.R}"/>
   <polygon points="11,17 19,17 15,25" fill="${C.C}"/>
   <polygon points="12,17.2 15,18.6 18,17.2 18,20.2 15,18.8 12,20.2" fill="${C.D}"/>
   <rect x="14" y="22" width="2" height="2" fill="${C.D}"/>
   <polygon points="19,18 22,18 26,23 24,25.5 19,21" fill="${C.R}"/>
   <circle cx="25.4" cy="24.4" r="1.8" fill="${C.G}"/>
   <ellipse cx="14" cy="12" rx="6" ry="5.2" fill="${C.G}"/>
   <rect x="13" y="9.5" width="16" height="6.5" rx="3" fill="${C.G}"/>
   <rect x="15" y="14" width="13.4" height="2.2" rx="1" fill="${C.L}"/>
   <rect x="15.5" y="13" width="12" height="1" fill="${C.D}"/>
   <rect x="26" y="10" width="2" height="2" fill="${C.D}"/>
   <circle cx="21" cy="9" r="3" fill="${C.G}"/><circle cx="21" cy="9" r="2.2" fill="${C.C}"/>
   <rect x="21" y="8" width="2" height="2" fill="${C.D}"/>
   <ellipse cx="12" cy="7.6" rx="6.6" ry="1.3" fill="${C.D}"/>
   <rect x="7.5" y="1.5" width="9" height="6.5" rx="1.5" fill="${C.D}"/>
  </svg>`},
 // 3 — the Wolf of «Nu, pogodi!»: grey wolf (slate), cream muzzle, striped shirt, bell-bottoms; no cigarette
 {title:'Волк из «Ну, погоди!»', family:'Лесные и горные животные', subject:'серый волк в полосатой тельняшке и брюках клёш, хитрая ухмылка',
  source:U, reference:'Ну, погоди! — Волк', forLevel:61, cells:1280, colours:8,
  svg:`<svg viewBox="0 0 32 40" xmlns="http://www.w3.org/2000/svg">
   <rect width="32" height="40" fill="${C.S}"/><rect y="36" width="32" height="4" fill="${C.G}"/>
   <polygon points="7.6,6 8.2,0.6 12.6,4.4" fill="${C.D}"/><polygon points="14,4 17.6,0.6 18.8,6.4" fill="${C.D}"/>
   <polygon points="7.5,7.5 3.4,9.8 6,11 3.4,13.4 7.5,13.4" fill="${C.D}"/>
   <ellipse cx="13" cy="8.6" rx="6.2" ry="5.8" fill="${C.D}"/>
   <rect x="10.5" y="13" width="4.5" height="3" fill="${C.D}"/>
   <rect x="15" y="9.6" width="12.6" height="5.4" rx="2.4" fill="${C.C}"/>
   <rect x="13.4" y="11" width="4" height="4" fill="${C.C}"/>
   <circle cx="26.6" cy="10.6" r="1.8" fill="${C.D}"/>
   <rect x="16" y="12" width="10" height="1" fill="${C.D}"/>
   <rect x="18" y="13" width="6" height="2" rx="0.6" fill="${C.R}"/>
   <rect x="12" y="4" width="3" height="4" rx="1" fill="${C.C}"/><rect x="16" y="4" width="3" height="4" rx="1" fill="${C.C}"/>
   <rect x="13" y="5" width="2" height="2" fill="${C.D}"/><rect x="17" y="5" width="2" height="2" fill="${C.D}"/>
   <rect x="5.5" y="16" width="2.6" height="10" rx="1.2" fill="${C.D}"/><rect x="19" y="16" width="2.6" height="10" rx="1.2" fill="${C.D}"/>
   <rect x="8" y="15" width="11" height="12" rx="1" fill="${C.C}"/>
   <rect x="8" y="15" width="11" height="2" fill="${C.N}"/><rect x="8" y="19" width="11" height="2" fill="${C.N}"/><rect x="8" y="23" width="11" height="2" fill="${C.N}"/>
   <rect x="5.5" y="15" width="3" height="2" fill="${C.N}"/><rect x="5.5" y="17" width="3" height="2" fill="${C.C}"/>
   <rect x="18.5" y="15" width="3" height="2" fill="${C.N}"/><rect x="18.5" y="17" width="3" height="2" fill="${C.C}"/>
   <rect x="8" y="27" width="11" height="1.6" fill="${C.E}"/>
   <polygon points="8,28.5 13,28.5 12.6,31 13.8,36 6.2,36 8.4,31" fill="${C.B}"/>
   <polygon points="14,28.5 19,28.5 18.6,31 20.8,36 13.2,36 14.4,31" fill="${C.B}"/>
   <rect x="8" y="28.5" width="11" height="2" fill="${C.B}"/>
   <ellipse cx="10" cy="36.4" rx="3.2" ry="1.3" fill="${C.D}"/><ellipse cx="17.4" cy="36.4" rx="3.2" ry="1.3" fill="${C.D}"/>
  </svg>`},
 // 4 — Leopold: kind orange-yellow cat, red bow tie, white collar; a bust against the sky
 {title:'Кот Леопольд', family:'Домашние животные', subject:'добрый рыжий Леопольд с красной бабочкой',
  source:U, reference:'Приключения кота Леопольда — Леопольд', forLevel:61, cells:900, colours:7,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.S}"/>
   <ellipse cx="15" cy="31" rx="11.5" ry="7" fill="${C.Y}"/>
   <rect x="11" y="18" width="8" height="7" fill="${C.Y}"/>
   <polygon points="10.5,22.5 15,26 19.5,22.5 19.5,25.5 15,29 10.5,25.5" fill="${C.C}"/>
   <polygon points="9.5,21.6 15,24 20.5,21.6 20.5,26.6 15,24.4 9.5,26.6" fill="${C.R}"/>
   <rect x="13.5" y="22.5" width="3" height="3" rx="0.6" fill="${C.R}"/>
   <polygon points="5.5,9 6,1 12.5,4.8" fill="${C.Y}"/><polygon points="17.5,4.8 24,1 24.5,9" fill="${C.Y}"/>
   <polygon points="6.8,8 7.2,2.6 11.2,5.2" fill="${C.K}"/><polygon points="18.8,5.2 22.8,2.6 23.2,8" fill="${C.K}"/>
   <ellipse cx="15" cy="11.8" rx="10" ry="8.4" fill="${C.Y}"/>
   <ellipse cx="15" cy="15" rx="11" ry="5.4" fill="${C.Y}"/>
   <ellipse cx="15" cy="16.4" rx="5" ry="3.4" fill="${C.C}"/>
   <ellipse cx="11" cy="10.4" rx="2.4" ry="2.6" fill="${C.C}"/><ellipse cx="19" cy="10.4" rx="2.4" ry="2.6" fill="${C.C}"/>
   <rect x="10" y="9" width="2" height="3" fill="${C.D}"/><rect x="18" y="9" width="2" height="3" fill="${C.D}"/>
   <rect x="13" y="13" width="4" height="2" rx="0.6" fill="${C.P}"/>
   <rect x="14" y="15" width="2" height="1" fill="${C.D}"/><rect x="12" y="16" width="6" height="1" fill="${C.D}"/>
   <rect x="12" y="15" width="1" height="1" fill="${C.D}"/><rect x="17" y="15" width="1" height="1" fill="${C.D}"/>
   <rect x="2" y="14" width="6" height="1" fill="${C.D}"/><rect x="22" y="14" width="6" height="1" fill="${C.D}"/>
   <rect x="2.5" y="16" width="5.5" height="1" fill="${C.D}"/><rect x="22" y="16" width="5.5" height="1" fill="${C.D}"/>
  </svg>`},
 // 5 — Karlsson: round little man with red hair, propeller on his back, checked shirt, one strap; flying in the sky
 {title:'Карлсон с пропеллером', family:'Герои приключений', direction:'Невесомый мир', subject:'толстячок Карлсон с пропеллером за спиной, рыжий, в клетчатой рубашке, летит',
  source:U, reference:'Малыш и Карлсон — Карлсон', forLevel:61, cells:900, colours:9,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <defs><pattern id="chk5" width="4" height="4" patternUnits="userSpaceOnUse"><rect width="4" height="4" fill="${C.B}"/><rect width="2" height="2" fill="${C.C}"/><rect x="2" y="2" width="2" height="2" fill="${C.C}"/></pattern></defs>
   <rect width="30" height="30" fill="${C.S}"/>
   <ellipse cx="4.5" cy="25.5" rx="3.2" ry="1.6" fill="${C.C}"/><ellipse cx="26" cy="10.5" rx="3" ry="1.5" fill="${C.C}"/>
   <rect x="14" y="2.5" width="2" height="5" fill="${C.D}"/>
   <polygon points="15,2.6 4.2,0.8 3.2,2.2 4.2,3.8" fill="${C.D}"/><polygon points="15,3.4 25.8,5.2 26.8,3.8 25.8,2.2" fill="${C.D}"/>
   <circle cx="15" cy="3" r="1.6" fill="${C.D}"/>
   <rect x="10" y="25" width="3.5" height="3" fill="${C.N}"/><rect x="16.5" y="25" width="3.5" height="3" fill="${C.N}"/>
   <ellipse cx="11.4" cy="28.2" rx="2.4" ry="1.2" fill="${C.E}"/><ellipse cx="18.6" cy="28.2" rx="2.4" ry="1.2" fill="${C.E}"/>
   <polygon points="9,15.5 3.6,17 3.6,19.8 9.4,19.4" fill="url(#chk5)"/><polygon points="21,15.5 26.4,17 26.4,19.8 20.6,19.4" fill="url(#chk5)"/>
   <circle cx="3.6" cy="18.4" r="1.8" fill="${C.K}"/><circle cx="26.4" cy="18.4" r="1.8" fill="${C.K}"/>
   <ellipse cx="15" cy="20" rx="7.8" ry="6.6" fill="url(#chk5)"/>
   <path d="M7.3,22 L22.7,22 A7.8,6.6 0 0 1 7.3,22 Z" fill="${C.N}"/>
   <polygon points="9.2,15.4 11.2,14.6 14.8,22.2 12.8,22.2" fill="${C.N}"/>
   <rect x="17" y="23" width="3" height="3" rx="1" fill="${C.Y}"/>
   <ellipse cx="15" cy="9.6" rx="6.4" ry="4.4" fill="${C.O}"/>
   <polygon points="8.8,9.2 9.2,5.8 11.6,7 12.8,4.8 15,6.4 17.2,4.8 18.4,7 20.8,5.8 21.2,9.2" fill="${C.O}"/>
   <ellipse cx="15" cy="12" rx="5" ry="4.4" fill="${C.K}"/>
   <rect x="12" y="10" width="2" height="2" fill="${C.D}"/><rect x="16" y="10" width="2" height="2" fill="${C.D}"/>
   <rect x="13" y="12" width="4" height="2" rx="0.9" fill="${C.P}"/>
   <rect x="12" y="14" width="6" height="1" fill="${C.D}"/>
  </svg>`},
 // 6 — the Hedgehog in the fog with his bundle on a stick; pale fog and dark grass
 {title:'Ёжик в тумане с узелком', family:'Лесные и горные животные', direction:'Уютная мистика', subject:'маленький ёжик с узелком на палочке идёт в тумане',
  source:U, reference:'Ёжик в тумане — Ёжик', forLevel:61, cells:864, colours:7,
  svg:`<svg viewBox="0 0 32 27" xmlns="http://www.w3.org/2000/svg">
   <rect width="32" height="27" fill="${C.S}"/><rect y="23" width="32" height="4" fill="${C.M}"/>
   <rect x="12" y="20" width="2.2" height="4" fill="${C.C}"/><rect x="17" y="20" width="2.2" height="4" fill="${C.C}"/>
   <polygon points="6,19 2,17.5 4.8,15.5 1.8,12.5 6,12.2 4.6,8.6 8.6,9.4 9.2,5.8 12,8.4 13.6,5 15.6,8.4 18.2,6.4 18.6,10 22,10.6 20,14 21,20 7.5,21.5" fill="${C.E}"/>
   <ellipse cx="13" cy="15.2" rx="7.6" ry="6" fill="${C.E}"/>
   <ellipse cx="20" cy="15.5" rx="4.6" ry="4.4" fill="${C.C}"/>
   <polygon points="21,12.5 29,15.4 21,18.6" fill="${C.C}"/>
   <circle cx="28.6" cy="15.4" r="1.4" fill="${C.D}"/>
   <rect x="20" y="12" width="2" height="3" rx="0.8" fill="${C.D}"/>
   <polygon points="20.4,18.6 19,20 5.8,6.4 7.2,5" fill="${C.A}"/>
   <ellipse cx="19.6" cy="19" rx="1.8" ry="1.4" fill="${C.C}"/>
   <circle cx="6" cy="6" r="3.8" fill="${C.R}"/>
   <polygon points="3.8,3 4.6,0.6 6.2,2.4 7.8,0.6 8.4,3" fill="${C.R}"/>
   <rect x="3.5" y="4.5" width="2" height="2" fill="${C.C}"/><rect x="6.5" y="6.5" width="2" height="2" fill="${C.C}"/>
  </svg>`},
 // 7 — Matroskin: striped cat in a cap, standing; compact, empty around
 {title:'Кот Матроскин', family:'Домашние животные', subject:'полосатый Матроскин в кепке, усатый и довольный',
  source:U, reference:'Трое из Простоквашино — Кот Матроскин', forLevel:61, cells:380, colours:5,
  svg:`<svg viewBox="0 0 20 28" xmlns="http://www.w3.org/2000/svg">
   <defs><clipPath id="b7"><ellipse cx="10" cy="19.5" rx="5.6" ry="6"/></clipPath></defs>
   <path d="M15,22 Q19.4,21 18.6,15 L16.6,15.4 Q17,19.4 14.6,20" fill="${C.C}"/>
   <rect x="16.4" y="16.4" width="2.4" height="1" fill="${C.D}"/>
   <rect x="6" y="23" width="3" height="4" rx="1" fill="${C.C}"/><rect x="11" y="23" width="3" height="4" rx="1" fill="${C.C}"/>
   <ellipse cx="10" cy="19.5" rx="5.6" ry="6" fill="${C.C}"/>
   <g clip-path="url(#b7)"><rect x="3" y="16" width="14" height="1" fill="${C.D}"/><rect x="3" y="19" width="14" height="1" fill="${C.D}"/><rect x="3" y="22" width="14" height="1" fill="${C.D}"/></g>
   <polygon points="3.4,8 3.2,1.8 8,5" fill="${C.C}"/><polygon points="16.6,8 16.8,1.8 12,5" fill="${C.C}"/>
   <ellipse cx="10" cy="10.2" rx="7" ry="5.6" fill="${C.C}"/>
   <ellipse cx="10" cy="4.8" rx="5.6" ry="3" fill="${C.N}"/>
   <rect x="3.6" y="5" width="12.8" height="2" rx="1" fill="${C.N}"/>
   <rect x="6" y="8" width="3" height="3" fill="${C.G}"/><rect x="11" y="8" width="3" height="3" fill="${C.G}"/>
   <rect x="7" y="8" width="1" height="3" fill="${C.D}"/><rect x="12" y="8" width="1" height="3" fill="${C.D}"/>
   <rect x="8" y="11" width="4" height="2" rx="0.6" fill="${C.P}"/>
   <rect x="8" y="14" width="4" height="1" fill="${C.D}"/><rect x="8" y="13" width="1" height="1" fill="${C.D}"/><rect x="11" y="13" width="1" height="1" fill="${C.D}"/>
   <rect x="0.6" y="11" width="5" height="1" fill="${C.D}"/><rect x="14.4" y="11" width="5" height="1" fill="${C.D}"/>
   <rect x="1" y="13" width="4.6" height="1" fill="${C.D}"/><rect x="14.4" y="13" width="4.6" height="1" fill="${C.D}"/>
  </svg>`},
 // 8 — Luntik: purple, long ears, big eyes, a pink belly; compact, empty around
 {title:'Лунтик', family:'Космос', subject:'фиолетовый Лунтик с длинными ушами и большими глазами',
  source:U, reference:'Лунтик и его друзья — Лунтик', forLevel:61, cells:375, colours:5,
  svg:`<svg viewBox="0 0 20 31" xmlns="http://www.w3.org/2000/svg">
   <ellipse cx="6" cy="7" rx="2.3" ry="6.5" fill="${C.V}" transform="rotate(-12 6 7)"/>
   <ellipse cx="14" cy="7" rx="2.3" ry="6.5" fill="${C.V}" transform="rotate(12 14 7)"/>
   <ellipse cx="6.2" cy="7.5" rx="1.1" ry="4.5" fill="${C.K}" transform="rotate(-12 6.2 7.5)"/>
   <ellipse cx="13.8" cy="7.5" rx="1.1" ry="4.5" fill="${C.K}" transform="rotate(12 13.8 7.5)"/>
   <ellipse cx="6.5" cy="28.8" rx="2.6" ry="1.5" fill="${C.V}"/><ellipse cx="13.5" cy="28.8" rx="2.6" ry="1.5" fill="${C.V}"/>
   <ellipse cx="1.8" cy="21.5" rx="1.6" ry="2.4" fill="${C.V}"/><ellipse cx="18.2" cy="21.5" rx="1.6" ry="2.4" fill="${C.V}"/>
   <circle cx="10" cy="20" r="8" fill="${C.V}"/>
   <ellipse cx="10" cy="24.6" rx="4.6" ry="3" fill="${C.K}"/>
   <ellipse cx="6.4" cy="16.8" rx="2.6" ry="3.2" fill="${C.C}"/><ellipse cx="13.6" cy="16.8" rx="2.6" ry="3.2" fill="${C.C}"/>
   <ellipse cx="7" cy="17.2" rx="1.6" ry="2.1" fill="${C.D}"/><ellipse cx="13" cy="17.2" rx="1.6" ry="2.1" fill="${C.D}"/>
   <rect x="8" y="20" width="4" height="1" fill="${C.D}"/>
  </svg>`},
 // 9 — Krosh: round blue rabbit ball, long ears, buck teeth, wide grin; compact, empty around
 {title:'Крош', family:'Домашние животные', subject:'круглый голубой кролик Крош с длинными ушами и зубастой улыбкой',
  source:U, reference:'Смешарики — Крош', forLevel:61, cells:360, colours:5,
  svg:`<svg viewBox="0 0 24 28" xmlns="http://www.w3.org/2000/svg">
   <ellipse cx="7.5" cy="7" rx="2.4" ry="6.6" fill="${C.B}" transform="rotate(-20 7.5 7)"/>
   <ellipse cx="16.5" cy="7" rx="2.4" ry="6.6" fill="${C.B}" transform="rotate(20 16.5 7)"/>
   <ellipse cx="7.6" cy="7.4" rx="1.1" ry="4.6" fill="${C.K}" transform="rotate(-20 7.6 7.4)"/>
   <ellipse cx="16.4" cy="7.4" rx="1.1" ry="4.6" fill="${C.K}" transform="rotate(20 16.4 7.4)"/>
   <ellipse cx="8" cy="26.5" rx="2.8" ry="1.4" fill="${C.B}"/><ellipse cx="16" cy="26.5" rx="2.8" ry="1.4" fill="${C.B}"/>
   <circle cx="12" cy="18.5" r="8.2" fill="${C.B}"/>
   <ellipse cx="9.6" cy="14.5" rx="2.2" ry="2.8" fill="${C.C}"/><ellipse cx="14.4" cy="14.5" rx="2.2" ry="2.8" fill="${C.C}"/>
   <rect x="10" y="14" width="1.6" height="2.2" fill="${C.D}"/><rect x="12.4" y="14" width="1.6" height="2.2" fill="${C.D}"/>
   <path d="M6.5,18.5 L17.5,18.5 Q17,24 12,24 Q7,24 6.5,18.5 Z" fill="${C.D}"/>
   <path d="M8.5,21.4 Q12,19.6 15.5,21.4 Q14.6,23.2 12,23.2 Q9.4,23.2 8.5,21.4 Z" fill="${C.R}"/>
   <rect x="10" y="18.5" width="1.8" height="2" fill="${C.C}"/><rect x="12.2" y="18.5" width="1.8" height="2" fill="${C.C}"/>
  </svg>`},
 // 10 — Winnie the Pooh (Soyuzmultfilm) with a jar of honey; bark outline, sky and grass
 {title:'Винни-Пух с мёдом', family:'Лесные и горные животные', subject:'круглый коричневый Винни-Пух с широкой улыбкой обнимает банку мёда',
  source:U, reference:'Винни-Пух (Союзмультфильм) — Винни-Пух', forLevel:61, cells:900, colours:8,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.S}"/><rect y="25" width="30" height="5" fill="${C.G}"/>
   <circle cx="7.3" cy="3.8" r="2.8" fill="${C.E}"/><circle cx="18.7" cy="3.8" r="2.8" fill="${C.E}"/>
   <ellipse cx="13" cy="20" rx="7.8" ry="6.6" fill="${C.E}"/>
   <ellipse cx="9" cy="26" rx="3" ry="1.8" fill="${C.E}"/><ellipse cx="17" cy="26" rx="3" ry="1.8" fill="${C.E}"/>
   <circle cx="13" cy="9.5" r="7.4" fill="${C.E}"/>
   <circle cx="7.3" cy="3.8" r="1.7" fill="${C.A}"/><circle cx="18.7" cy="3.8" r="1.7" fill="${C.A}"/>
   <ellipse cx="13" cy="20" rx="6.7" ry="5.5" fill="${C.A}"/>
   <circle cx="13" cy="9.5" r="6.4" fill="${C.A}"/>
   <rect x="9" y="7" width="2" height="2" fill="${C.D}"/><rect x="15" y="7" width="2" height="2" fill="${C.D}"/>
   <ellipse cx="13" cy="10.6" rx="1.8" ry="1.1" fill="${C.D}"/>
   <path d="M8.4,12 L17.6,12 Q16.8,15.8 13,15.8 Q9.2,15.8 8.4,12 Z" fill="${C.D}"/>
   <path d="M9.6,13 L16.4,13 Q15.8,14.9 13,14.9 Q10.2,14.9 9.6,13 Z" fill="${C.R}"/>
   <ellipse cx="22.5" cy="20.5" rx="5.8" ry="5.6" fill="${C.E}"/>
   <rect x="18.4" y="12.4" width="8.2" height="4.4" rx="1" fill="${C.E}"/>
   <ellipse cx="22.5" cy="20.5" rx="4.8" ry="4.6" fill="${C.O}"/>
   <rect x="19.6" y="15" width="5.8" height="2" fill="${C.O}"/>
   <ellipse cx="22.5" cy="13" rx="3.5" ry="1.5" fill="${C.Y}"/>
   <rect x="23" y="13" width="2" height="4.4" rx="0.8" fill="${C.Y}"/>
   <circle cx="17.6" cy="20" r="2.2" fill="${C.E}"/><circle cx="17.6" cy="20" r="1.4" fill="${C.A}"/>
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
 // 20 — Dug from «Up»: golden retriever with the red collar and its translator, tongue out; jungle green behind
 {title:'Даг с ошейником', family:'Домашние животные', subject:'золотистый ретривер Даг с красным говорящим ошейником, высунул язык',
  source:U, reference:'Вверх — Dug', forLevel:61, cells:900, colours:8,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.G}"/>
   <ellipse cx="15" cy="31" rx="10.5" ry="9.5" fill="${C.Y}"/>
   <ellipse cx="15" cy="12" rx="8" ry="7.6" fill="${C.Y}"/>
   <ellipse cx="7.4" cy="13.4" rx="2.8" ry="6" fill="${C.O}" transform="rotate(12 7.4 13.4)"/>
   <ellipse cx="22.6" cy="13.4" rx="2.8" ry="6" fill="${C.O}" transform="rotate(-12 22.6 13.4)"/>
   <ellipse cx="15" cy="16" rx="4.8" ry="3.6" fill="${C.C}"/>
   <ellipse cx="15" cy="13.6" rx="2.1" ry="1.3" fill="${C.D}"/>
   <rect x="12" y="16" width="6" height="1" fill="${C.D}"/>
   <rect x="13" y="16.6" width="4" height="5" rx="1.5" fill="${C.P}"/>
   <rect x="11" y="9" width="2" height="2" fill="${C.D}"/><rect x="17" y="9" width="2" height="2" fill="${C.D}"/>
   <rect x="8" y="20.6" width="14" height="2.6" fill="${C.R}"/>
   <rect x="13" y="18.4" width="4" height="2.4" rx="1" fill="${C.P}"/>
   <circle cx="15" cy="24.6" r="2.4" fill="${C.S}"/>
  </svg>`},
]};
