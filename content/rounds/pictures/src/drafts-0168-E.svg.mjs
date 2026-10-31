// SVG drafts of batch 0168, letter E (theme «Мемные и абсурдные герои», 2026-10-31). Pictures 1–18 are meme references
// (source «интернет-мем/тренд», not for release until the owner confirms the rights; catalog §4, §10); 19–20 are our own
// heroes in a meme situation. Drawn at cell scale like the first drafts of groups B and C (one viewBox unit = one cell,
// features on whole cells, bold and chunky); convert with tools/rounds/svg2cells.mjs into batch-0168-E.mjs.
import {C} from '../../../../tools/rounds/svg-palette.mjs';
const M='интернет-мем/тренд',BR='AI-brainrot и абсурдные гибриды',ANI='Мемные животные и бытовые ситуации',MIX='Мемные ремиксы Bloomlings';
const line=(d,c,w=2)=>`<path d="${d}" fill="none" stroke="${c}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"/>`;
export default {theme:'Мемные и абсурдные герои', pictures:[
 // 1 — blue shark on three legs in blue sneakers, on a beach
 {title:'Тралалеро Тралала', family:BR, direction:'Предметные гибриды', subject:'синяя акула на трёх ногах в синих кроссовках на пляже',
  source:M, reference:'Italian brainrot — Tralalero Tralala', forLevel:61, cells:1280, colours:8,
  svg:`<svg viewBox="0 0 40 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="40" height="32" fill="${C.S}"/><rect y="27" width="40" height="5" fill="${C.Y}"/>
   <polygon points="15,8.5 25,8.5 18.5,1 16,1" fill="${C.N}"/>
   <polygon points="4,10 10,9 10,15 4,14" fill="${C.N}"/>
   <polygon points="4,11 1,1 4,1 8,10" fill="${C.N}"/>
   <polygon points="4,13 2,20 5,20 8,14" fill="${C.N}"/>
   <ellipse cx="21" cy="12.5" rx="14" ry="5.5" fill="${C.N}"/>
   <polygon points="29,8 39,11.5 38,13.5 29,17" fill="${C.N}"/>
   <polygon points="8,12.5 15,15 24,15.2 31,14 39,12.2 37,15 30,17.4 21,18.2 13,17.4 9,14.5" fill="${C.C}"/>
   <polygon points="29,14 38.6,12.3 38,13.4 30,15.2" fill="${C.D}"/>
   <rect x="31" y="9" width="2" height="2" fill="${C.D}"/>
   <rect x="25" y="10" width="1" height="3" fill="${C.D}"/><rect x="27" y="10" width="1" height="3" fill="${C.D}"/>
   <rect x="12" y="17" width="3" height="9" fill="${C.K}"/><rect x="19" y="17" width="3" height="9" fill="${C.K}"/><rect x="27" y="16" width="3" height="10" fill="${C.K}"/>
   <rect x="11" y="25" width="5" height="3" rx="1" fill="${C.B}"/><rect x="11" y="26" width="7" height="2" rx="1" fill="${C.B}"/><rect x="11" y="28" width="7" height="1" fill="${C.C}"/>
   <rect x="18" y="25" width="5" height="3" rx="1" fill="${C.B}"/><rect x="18" y="26" width="7" height="2" rx="1" fill="${C.B}"/><rect x="18" y="28" width="7" height="1" fill="${C.C}"/>
   <rect x="26" y="25" width="5" height="3" rx="1" fill="${C.B}"/><rect x="26" y="26" width="7" height="2" rx="1" fill="${C.B}"/><rect x="26" y="28" width="7" height="1" fill="${C.C}"/>
  </svg>`},
 // 2 — ballerina in a pink tutu with a cappuccino for a head, on a stage between red curtains
 {title:'Балерина Капучина', family:BR, direction:'Предметные гибриды', subject:'балерина в розовой пачке с головой-капучино на сцене',
  source:M, reference:'Italian brainrot — Ballerina Cappuccina', forLevel:61, cells:952, colours:7,
  svg:`<svg viewBox="0 0 28 34" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="34" fill="${C.V}"/><rect y="30" width="28" height="4" fill="${C.E}"/>
   <polygon points="0,0 4,0 3,30 0,30" fill="${C.R}"/><polygon points="24,0 28,0 28,30 25,30" fill="${C.R}"/><rect width="28" height="2" fill="${C.R}"/>
   <circle cx="21" cy="8.5" r="3" fill="${C.C}"/><circle cx="21.3" cy="8.5" r="1.3" fill="${C.V}"/>
   <path d="M7.5 5 L20.5 5 L19.5 11 Q19 13 17 13 L11 13 Q9 13 8.5 11 Z" fill="${C.C}"/>
   <ellipse cx="14" cy="5" rx="6.6" ry="2.6" fill="${C.C}"/>
   <ellipse cx="14" cy="5" rx="5.4" ry="1.7" fill="${C.A}"/>
   <rect x="12" y="4" width="4" height="2" fill="${C.C}"/>
   <rect x="13" y="13" width="2" height="2" fill="${C.K}"/>
   ${line('M11.5 15.5 L7 13.5 L4 11',C.K)}${line('M16.5 15.5 L21 13.5 L24 11',C.K)}
   <polygon points="11,14.5 17,14.5 16.5,20 11.5,20" fill="${C.P}"/>
   <ellipse cx="14" cy="21" rx="9.5" ry="2.4" fill="${C.P}"/>
   <ellipse cx="14" cy="22.4" rx="8.5" ry="1.3" fill="${C.K}"/>
   ${line('M13.5 23.5 L13.5 29.5',C.K)}${line('M15 23.5 L18.5 26 L15.2 28',C.K)}
   <rect x="12.5" y="29" width="2" height="2" fill="${C.P}"/><rect x="13.5" y="27" width="2" height="2" fill="${C.P}"/>
  </svg>`},
 // 3 — a wooden log with a face holding a baseball bat, at night
 {title:'Тунг Тунг Тунг Сахур', family:BR, direction:'Ожившие вещи', subject:'деревянное полено с лицом и бейсбольной битой ночью',
  source:M, reference:'brainrot — Tung Tung Tung Sahur', forLevel:61, cells:952, colours:6,
  svg:`<svg viewBox="0 0 28 34" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="34" fill="${C.N}"/><rect y="31" width="28" height="3" fill="${C.D}"/>
   <circle cx="4.5" cy="4.5" r="2.8" fill="${C.Y}"/><circle cx="5.8" cy="3.6" r="2.4" fill="${C.N}"/>
   ${line('M11.5 27 L11.5 30.5',C.E)}${line('M16.5 27 L16.5 30.5',C.E)}
   <rect x="9" y="30" width="4" height="1.5" fill="${C.E}"/><rect x="15" y="30" width="4" height="1.5" fill="${C.E}"/>
   ${line('M8.5 15 L5 22',C.E)}
   <rect x="8" y="4" width="12" height="24" rx="2" fill="${C.E}"/>
   <rect x="9" y="4" width="10" height="23" fill="${C.A}"/>
   <ellipse cx="14" cy="4.5" rx="6" ry="2" fill="${C.C}"/><ellipse cx="14" cy="4.5" rx="3" ry="0.8" fill="${C.A}"/>
   <circle cx="11.5" cy="11" r="2.3" fill="${C.C}"/><circle cx="16.5" cy="11" r="2.3" fill="${C.C}"/>
   <rect x="11" y="11" width="2" height="2" fill="${C.D}"/><rect x="16" y="11" width="2" height="2" fill="${C.D}"/>
   <rect x="12" y="16" width="4" height="1" fill="${C.E}"/><rect x="11" y="15" width="1" height="1" fill="${C.E}"/><rect x="16" y="15" width="1" height="1" fill="${C.E}"/>
   <rect x="11" y="20" width="1" height="4" fill="${C.E}"/><rect x="16" y="21" width="1" height="4" fill="${C.E}"/>
   ${line('M19.5 15 L22.5 18.5',C.E)}
   ${line('M22.5 21 L24.6 9',C.C,2)}${line('M24.4 11 L25.6 4',C.C,4)}
   <rect x="21" y="21" width="3" height="1.4" fill="${C.C}"/>
  </svg>`},
 // 4 — a monkey made of a tree trunk with a huge nose, big feet and a small hat
 {title:'Брр Брр Патапим', family:BR, direction:'Предметные гибриды', subject:'обезьяна из древесного ствола с большим носом, огромными ступнями и маленькой шляпой',
  source:M, reference:'Italian brainrot — Brr Brr Patapim', forLevel:61, cells:952, colours:8,
  svg:`<svg viewBox="0 0 28 34" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="34" fill="${C.S}"/><rect y="30" width="28" height="4" fill="${C.G}"/>
   ${line('M9 19 L4.5 15 L3.5 10',C.E)}${line('M19 19 L23.5 15 L24.5 10',C.E)}
   <circle cx="3.5" cy="8.5" r="2.4" fill="${C.G}"/><circle cx="24.5" cy="8.5" r="2.4" fill="${C.G}"/>
   <circle cx="5.5" cy="13.5" r="1.6" fill="${C.G}"/><circle cx="22.5" cy="13.5" r="1.6" fill="${C.G}"/>
   <path d="M9 7 Q9 5 11 5 L17 5 Q19 5 19 7 L19.5 27 L8.5 27 Z" fill="${C.A}"/>
   <rect x="11" y="19" width="1" height="6" fill="${C.E}"/><rect x="16" y="18" width="1" height="7" fill="${C.E}"/>
   <ellipse cx="14" cy="11" rx="4.5" ry="4" fill="${C.K}"/>
   <rect x="11" y="8" width="2" height="2" fill="${C.D}"/><rect x="15" y="8" width="2" height="2" fill="${C.D}"/>
   <rect x="13" y="9" width="2" height="3" fill="${C.O}"/><ellipse cx="14" cy="14" rx="2.6" ry="2.4" fill="${C.O}"/>
   <rect x="12" y="17" width="4" height="1" fill="${C.E}"/>
   <rect x="11" y="2" width="6" height="2.5" fill="${C.M}"/><rect x="9.5" y="4" width="9" height="1.2" fill="${C.M}"/>
   <rect x="10" y="26" width="3" height="2" fill="${C.A}"/><rect x="15" y="26" width="3" height="2" fill="${C.A}"/>
   <ellipse cx="9" cy="29" rx="5" ry="2" fill="${C.K}"/><ellipse cx="19" cy="29" rx="5" ry="2" fill="${C.K}"/>
  </svg>`},
 // 5 — a crocodile that is a bomber plane, in the sky among clouds
 {title:'Бомбардиро Крокодило', family:BR, direction:'Предметные гибриды', subject:'крокодил-бомбардировщик с крыльями и пропеллерами летит среди облаков',
  source:M, reference:'Italian brainrot — Bombardiro Crocodilo', forLevel:61, cells:1280, colours:8,
  svg:`<svg viewBox="0 0 40 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="40" height="32" fill="${C.S}"/>
   <ellipse cx="6" cy="27" rx="4" ry="2" fill="${C.C}"/><ellipse cx="9" cy="25.8" rx="3" ry="2" fill="${C.C}"/>
   <ellipse cx="33" cy="4" rx="4" ry="1.8" fill="${C.C}"/><ellipse cx="35.5" cy="3" rx="2.5" ry="1.8" fill="${C.C}"/>
   <polygon points="15,12.5 21,12.5 17,3 12,3" fill="${C.M}"/>
   <rect x="11.5" y="5.5" width="7" height="3" rx="1" fill="${C.D}"/><rect x="18.5" y="3.5" width="1.5" height="7" fill="${C.C}"/>
   <polygon points="3,9 7,9 9,14.5 4,14.5" fill="${C.M}"/>
   <polygon points="1,15.5 8,13 8,18 " fill="${C.G}"/>
   <ellipse cx="18" cy="15.5" rx="11" ry="3.8" fill="${C.G}"/>
   <polygon points="26,12 37,13 39,14.5 26,15.5" fill="${C.G}"/>
   <polygon points="26,15.5 38,16.5 37,18.5 26,19" fill="${C.G}"/>
   <polygon points="27,15 38,15.6 38,16.4 27,16.4" fill="${C.R}"/>
   <rect x="29" y="14" width="2" height="1" fill="${C.C}"/><rect x="33" y="14.2" width="2" height="1" fill="${C.C}"/>
   <rect x="31" y="16.4" width="2" height="1" fill="${C.C}"/><rect x="35" y="16.4" width="2" height="1" fill="${C.C}"/>
   <ellipse cx="17" cy="18" rx="10" ry="1.4" fill="${C.L}"/>
   <circle cx="28.5" cy="11.5" r="2.2" fill="${C.G}"/><circle cx="28.7" cy="11.3" r="1.5" fill="${C.Y}"/><rect x="28" y="10.5" width="1.5" height="1.5" fill="${C.D}"/>
   <polygon points="10,12 11,10.5 12,12" fill="${C.M}"/><polygon points="14,12 15,10.5 16,12" fill="${C.M}"/><polygon points="18,12 19,10.5 20,12" fill="${C.M}"/>
   <polygon points="14,16 22,16 17,27 9,27" fill="${C.M}"/>
   <rect x="11" y="21" width="8" height="3.2" rx="1" fill="${C.D}"/><rect x="19" y="18" width="1.6" height="9.5" fill="${C.C}"/>
   <ellipse cx="25.5" cy="22" rx="2.5" ry="1.3" fill="${C.D}"/><polygon points="22,21 23.5,22 22,23" fill="${C.D}"/>
  </svg>`},
 // 6 — an elephant that is a cactus, in sandals, in the desert
 {title:'Лирили Ларила', family:BR, direction:'Визуальные каламбуры', subject:'слон-кактус в сандалиях в пустыне',
  source:M, reference:'Italian brainrot — Lirilì Larilà', forLevel:61, cells:900, colours:8,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.S}"/><rect y="25" width="30" height="5" fill="${C.Y}"/>
   <rect x="6" y="3" width="3.5" height="10" rx="1.7" fill="${C.G}"/><rect x="3" y="6" width="3" height="5" rx="1.5" fill="${C.G}"/><rect x="3" y="9" width="5" height="2.5" fill="${C.G}"/>
   <rect x="6" y="19" width="3" height="7" fill="${C.G}"/><rect x="10" y="19" width="3" height="7" fill="${C.G}"/>
   <rect x="15" y="19" width="3" height="7" fill="${C.G}"/><rect x="19" y="19" width="3" height="7" fill="${C.G}"/>
   <ellipse cx="14" cy="15.5" rx="9" ry="6" fill="${C.G}"/>
   <rect x="9" y="11" width="1" height="9" fill="${C.M}"/><rect x="13" y="10" width="1" height="11" fill="${C.M}"/><rect x="17" y="10" width="1" height="11" fill="${C.M}"/>
   <rect x="7" y="5" width="1" height="7" fill="${C.M}"/>
   <rect x="5.5" y="26" width="4" height="1.5" fill="${C.A}"/><rect x="9.5" y="26" width="4" height="1.5" fill="${C.A}"/>
   <rect x="14.5" y="26" width="4" height="1.5" fill="${C.A}"/><rect x="18.5" y="26" width="4" height="1.5" fill="${C.A}"/>
   <rect x="6" y="23.5" width="3" height="1" fill="${C.A}"/><rect x="10" y="23.5" width="3" height="1" fill="${C.A}"/>
   <rect x="15" y="23.5" width="3" height="1" fill="${C.A}"/><rect x="19" y="23.5" width="3" height="1" fill="${C.A}"/>
   <ellipse cx="18.5" cy="10" rx="3.8" ry="5" fill="${C.D}"/><ellipse cx="18.5" cy="10.5" rx="2.2" ry="3.4" fill="${C.K}"/>
   <circle cx="23" cy="9.5" r="4.5" fill="${C.D}"/>
   ${line('M26 11 Q28.5 15 27.5 19.5 Q27.2 21.5 29 21.5',C.D,3)}
   <polygon points="24,13 27,14.5 24,15" fill="${C.C}"/>
   <rect x="23" y="7" width="2" height="2" fill="${C.C}"/>
  </svg>`},
 // 7 — a chimp's head inside a peeled banana
 {title:'Шимпанзини Бананини', family:BR, direction:'Предметные гибриды', subject:'голова шимпанзе внутри очищенного банана',
  source:M, reference:'Italian brainrot — Chimpanzini Bananini', forLevel:61, cells:360, colours:5,
  svg:`<svg viewBox="0 0 20 27" xmlns="http://www.w3.org/2000/svg">
   <polygon points="4,13 16,13 15,21 12,25 8,25 5,21" fill="${C.Y}"/>
   <polygon points="3.5,12 6,14 3,22 0.5,21 1,15" fill="${C.Y}"/><polygon points="16.5,12 14,14 17,22 19.5,21 19,15" fill="${C.Y}"/>
   <polygon points="3.6,13.5 5,14.5 2.6,20 1.6,19.6" fill="${C.C}"/><polygon points="16.4,13.5 15,14.5 17.4,20 18.4,19.6" fill="${C.C}"/>
   <rect x="8.5" y="24.5" width="3" height="2.5" fill="${C.D}"/>
   <circle cx="3" cy="8.5" r="2" fill="${C.E}"/><circle cx="17" cy="8.5" r="2" fill="${C.E}"/>
   <circle cx="10" cy="8" r="6.6" fill="${C.E}"/>
   <circle cx="8" cy="7.2" r="2.3" fill="${C.A}"/><circle cx="12" cy="7.2" r="2.3" fill="${C.A}"/>
   <ellipse cx="10" cy="10.8" rx="4" ry="2.8" fill="${C.A}"/>
   <rect x="7" y="6" width="2" height="2" fill="${C.D}"/><rect x="11" y="6" width="2" height="2" fill="${C.D}"/>
   <rect x="8" y="11.5" width="4" height="1" fill="${C.D}"/>
   <polygon points="6.5,14 13.5,14 12,19 10,20 8,19" fill="${C.Y}"/>
  </svg>`},
 // 8 — a camel whose body is a fridge, in the desert
 {title:'Фриго Камело', family:BR, direction:'Предметные гибриды', subject:'верблюд с туловищем-холодильником в пустыне',
  source:M, reference:'Italian brainrot — Frigo Camelo', forLevel:61, cells:900, colours:6,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.S}"/><rect y="26" width="30" height="4" fill="${C.Y}"/>
   <circle cx="14.5" cy="8.5" r="3" fill="${C.A}"/><circle cx="20.5" cy="8.5" r="3" fill="${C.A}"/>
   ${line('M25 12 L27 18',C.A,1.6)}<rect x="26" y="17" width="2" height="3" fill="${C.E}"/>
   <rect x="11" y="22" width="2" height="5" fill="${C.A}"/><rect x="14" y="22" width="2" height="5" fill="${C.A}"/>
   <rect x="19" y="22" width="2" height="5" fill="${C.A}"/><rect x="22" y="22" width="2" height="5" fill="${C.A}"/>
   <rect x="10.5" y="26" width="3" height="1.5" fill="${C.E}"/><rect x="13.5" y="26" width="3" height="1.5" fill="${C.E}"/>
   <rect x="18.5" y="26" width="3" height="1.5" fill="${C.E}"/><rect x="21.5" y="26" width="3" height="1.5" fill="${C.E}"/>
   ${line('M11 13 Q6 11.5 6 5',C.A,3)}
   <rect x="9" y="9" width="17" height="14" rx="1" fill="${C.D}"/><rect x="10" y="10" width="15" height="12" fill="${C.C}"/>
   <rect x="10" y="14" width="15" height="1" fill="${C.D}"/>
   <rect x="11" y="11" width="1" height="2" fill="${C.D}"/><rect x="11" y="16" width="1" height="4" fill="${C.D}"/>
   <ellipse cx="4.5" cy="4.5" rx="4" ry="2.6" fill="${C.A}"/><rect x="6" y="1" width="2" height="2" fill="${C.A}"/>
   <rect x="5" y="3" width="2" height="2" fill="${C.D}"/><rect x="0.5" y="5.5" width="3" height="1" fill="${C.E}"/>
  </svg>`},
 // 9 — a frog head on a tyre with human legs
 {title:'Бонека Амбалабу', family:BR, direction:'Предметные гибриды', subject:'голова лягушки на автомобильной шине с человеческими ногами',
  source:M, reference:'Italian brainrot — Boneca Ambalabu', forLevel:61, cells:896, colours:8,
  svg:`<svg viewBox="0 0 28 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="32" fill="${C.S}"/><rect y="29" width="28" height="3" fill="${C.G}"/>
   <rect x="10" y="24" width="3" height="5" fill="${C.K}"/><rect x="15" y="24" width="3" height="5" fill="${C.K}"/>
   <rect x="8" y="27.5" width="5" height="2" rx="0.8" fill="${C.R}"/><rect x="15" y="27.5" width="5" height="2" rx="0.8" fill="${C.R}"/>
   <circle cx="14" cy="18.5" r="7.5" fill="${C.D}"/><circle cx="14" cy="18.5" r="3.6" fill="${C.C}"/><rect x="13" y="17.5" width="2" height="2" fill="${C.D}"/>
   <ellipse cx="14" cy="8.5" rx="8.5" ry="4.8" fill="${C.G}"/>
   <circle cx="9.5" cy="4.5" r="2.8" fill="${C.G}"/><circle cx="18.5" cy="4.5" r="2.8" fill="${C.G}"/>
   <circle cx="9.5" cy="4.3" r="1.9" fill="${C.C}"/><circle cx="18.5" cy="4.3" r="1.9" fill="${C.C}"/>
   <rect x="9" y="4" width="2" height="2" fill="${C.D}"/><rect x="18" y="4" width="2" height="2" fill="${C.D}"/>
   <ellipse cx="14" cy="11.3" rx="6.5" ry="1.8" fill="${C.L}"/>
   <rect x="7" y="9" width="14" height="1" fill="${C.D}"/>
  </svg>`},
 // 10 — Labubu: rabbit-eared furry monster figure with a jagged toothy grin
 {title:'Лабубу', family:'Коллекционный хайп и маскоты', subject:'пушистый монстрик с заячьими ушами и зубастой улыбкой',
  source:M, reference:'Pop Mart, The Monsters — Labubu', forLevel:61, cells:360, colours:5,
  svg:`<svg viewBox="0 0 18 28" xmlns="http://www.w3.org/2000/svg">
   <rect x="2.5" y="0.5" width="4.5" height="12" rx="2.2" fill="${C.A}"/><rect x="11" y="0.5" width="4.5" height="12" rx="2.2" fill="${C.A}"/>
   <rect x="4" y="2" width="2" height="8" rx="1" fill="${C.K}"/><rect x="12" y="2" width="2" height="8" rx="1" fill="${C.K}"/>
   <ellipse cx="9" cy="24" rx="5.5" ry="4" fill="${C.A}"/>
   <circle cx="9" cy="14.5" r="8" fill="${C.A}"/>
   <ellipse cx="9" cy="16" rx="6" ry="4.8" fill="${C.C}"/>
   <rect x="5" y="12" width="2.5" height="3" rx="1" fill="${C.D}"/><rect x="10.5" y="12" width="2.5" height="3" rx="1" fill="${C.D}"/>
   <polygon points="4,16.5 14,16.5 13,19.5 5,19.5" fill="${C.D}"/>
   <polygon points="5,16.5 6.5,18 8,16.5" fill="${C.C}"/><polygon points="8,16.5 9.5,18 11,16.5" fill="${C.C}"/><polygon points="11,16.5 12.5,18 14,16.5" fill="${C.C}"/>
   <polygon points="6.5,19.5 8,18.2 9.5,19.5" fill="${C.C}"/><polygon points="9.5,19.5 11,18.2 12.5,19.5" fill="${C.C}"/>
  </svg>`},
 // 11 — Oren of Sprunki: orange round character in headphones
 {title:'Орен из Спрунки', family:'Игровая интернет-культура', subject:'оранжевый круглый персонаж в больших наушниках',
  source:M, reference:'Sprunki — Oren', forLevel:61, cells:360, colours:5,
  svg:`<svg viewBox="0 0 21 22" xmlns="http://www.w3.org/2000/svg">
   <rect x="5" y="15" width="11" height="7" rx="2" fill="${C.O}"/>
   <circle cx="10.5" cy="9.5" r="7.6" fill="${C.O}"/>
   ${line('M2.2 9 A8.4 8.4 0 0 1 18.8 9',C.D,2)}
   <rect x="0.5" y="6.5" width="3.5" height="7" rx="1.2" fill="${C.D}"/><rect x="17" y="6.5" width="3.5" height="7" rx="1.2" fill="${C.D}"/>
   <circle cx="7.8" cy="8.5" r="2.2" fill="${C.C}"/><circle cx="13.2" cy="8.5" r="2.2" fill="${C.C}"/>
   <rect x="7.5" y="8" width="2" height="2" fill="${C.D}"/><rect x="12.5" y="8" width="2" height="2" fill="${C.D}"/>
   <path d="M7 12.5 L14 12.5 Q13.5 15 10.5 15 Q7.5 15 7 12.5 Z" fill="${C.D}"/><rect x="9" y="14" width="3" height="1" fill="${C.R}"/>
  </svg>`},
 // 12 — Doge: a Shiba Inu's face with the side-eye
 {title:'Доге', family:ANI, direction:'Театр эмоций', subject:'морда сиба-ину с косым взглядом на диване',
  source:M, reference:'Doge — Kabosu', forLevel:61, cells:900, colours:7,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.B}"/>
   <ellipse cx="15" cy="27" rx="10" ry="6" fill="${C.O}"/><ellipse cx="14" cy="26" rx="4.5" ry="3.5" fill="${C.C}"/>
   <rect y="27" width="30" height="3" fill="${C.R}"/>
   <polygon points="6,11 7.5,2 13,7" fill="${C.O}"/><polygon points="17,7 22.5,2 24,11" fill="${C.O}"/>
   <polygon points="7.5,9 8.3,4.5 11,7" fill="${C.K}"/><polygon points="19,7 21.7,4.5 22.5,9" fill="${C.K}"/>
   <ellipse cx="15" cy="13.5" rx="9.2" ry="7.6" fill="${C.O}"/>
   <ellipse cx="12" cy="17.2" rx="5.4" ry="3.4" fill="${C.C}"/><ellipse cx="20.5" cy="16.5" rx="3.6" ry="3" fill="${C.C}"/>
   <rect x="9" y="10" width="4" height="2" fill="${C.C}"/><rect x="17" y="10" width="4" height="2" fill="${C.C}"/>
   <rect x="11" y="10" width="2" height="2" fill="${C.D}"/><rect x="19" y="10" width="2" height="2" fill="${C.D}"/>
   <rect x="9" y="7" width="2" height="2" fill="${C.C}"/><rect x="17" y="7" width="2" height="2" fill="${C.C}"/>
   <rect x="7" y="14" width="4" height="2" rx="0.5" fill="${C.D}"/>
   <rect x="9" y="18" width="5" height="1" fill="${C.D}"/>
  </svg>`},
 // 13 — Nyan Cat: a cat with a pop-tart body flying with a rainbow trail across a starry night sky
 {title:'Нян Кэт', family:'Вирусные анимационные персонажи и мини-вселенные', direction:'Невесомый мир', subject:'кот с телом-печенькой с глазурью летит с радужным следом по ночному небу',
  source:M, reference:'Nyan Cat — Nyan Cat', forLevel:61, cells:1232, colours:10,
  svg:`<svg viewBox="0 0 44 28" xmlns="http://www.w3.org/2000/svg">
   <rect width="44" height="28" fill="${C.N}"/>
   ${[0,1,2,3].map(i=>{const x=i*5,d=i%2;return [C.R,C.O,C.Y,C.G,C.B,C.V].map((c,k)=>`<rect x="${x}" y="${7+d+k*2}" width="${i===3?5:5}" height="2" fill="${c}"/>`).join('');}).join('')}
   ${[[4,3],[12,24],[24,2],[40,24],[41,4],[3,22]].map(([x,y])=>`<rect x="${x-1}" y="${y}" width="3" height="1" fill="${C.C}"/><rect x="${x}" y="${y-1}" width="1" height="3" fill="${C.C}"/>`).join('')}
   <rect x="19" y="20" width="2" height="3" fill="${C.S}"/><rect x="23" y="20" width="2" height="3" fill="${C.S}"/><rect x="29" y="20" width="2" height="3" fill="${C.S}"/><rect x="33" y="20" width="2" height="3" fill="${C.S}"/>
   ${line('M19 15 L15 13.5',C.S,2)}
   <rect x="18" y="5" width="17" height="16" rx="2" fill="${C.C}"/>
   <rect x="19.5" y="6.5" width="14" height="13" rx="1.5" fill="${C.P}"/>
   <rect x="21" y="8" width="2" height="2" fill="${C.R}"/><rect x="26" y="7.5" width="2" height="2" fill="${C.R}"/><rect x="22" y="14" width="2" height="2" fill="${C.R}"/><rect x="25" y="17" width="2" height="2" fill="${C.R}"/><rect x="29" y="9" width="2" height="2" fill="${C.R}"/>
   <polygon points="29,11 31,7 33.5,11" fill="${C.S}"/><polygon points="36.5,11 39,7 41,11" fill="${C.S}"/>
   <rect x="29" y="10" width="12" height="10" rx="2.5" fill="${C.S}"/>
   <rect x="31" y="13" width="2" height="2" fill="${C.N}"/><rect x="37" y="13" width="2" height="2" fill="${C.N}"/>
   <rect x="34" y="16" width="2" height="1" fill="${C.N}"/><rect x="32" y="17" width="6" height="1" fill="${C.N}"/>
   <rect x="29.5" y="15.5" width="2" height="2" fill="${C.P}"/><rect x="38.5" y="15.5" width="2" height="2" fill="${C.P}"/>
  </svg>`},
 // 14 — a calm capybara with a tangerine on its head in a warm spring, tangerines floating round it
 {title:'Капибара с мандарином на голове', family:ANI, direction:'Уютный сюрреализм', subject:'спокойная капибара с мандарином на голове в тёплом источнике',
  source:M, reference:'мем «спокойная капибара» — капибара с мандарином в источнике', forLevel:61, cells:1292, colours:7,
  svg:`<svg viewBox="0 0 38 34" xmlns="http://www.w3.org/2000/svg">
   <rect width="38" height="34" fill="${C.S}"/>
   ${line('M5 15 Q3.5 12.5 5 10 Q6.5 7.5 5 5',C.C,1.2)}${line('M34 14 Q32.5 11.5 34 9 Q35.5 6.5 34 4',C.C,1.2)}
   <ellipse cx="13" cy="21" rx="10.5" ry="7" fill="${C.A}"/>
   <rect x="16" y="8" width="18" height="12" rx="4.5" fill="${C.A}"/>
   <circle cx="19.5" cy="8.5" r="2" fill="${C.E}"/>
   <rect x="30.5" y="9" width="4" height="4" rx="1" fill="${C.E}"/>
   <rect x="23" y="11" width="3" height="1" fill="${C.E}"/>
   <rect x="29" y="16" width="4" height="1" fill="${C.E}"/>
   <circle cx="24.5" cy="5" r="3.3" fill="${C.O}"/>
   <ellipse cx="27.5" cy="2" rx="2.2" ry="1.1" fill="${C.G}"/>
   <rect y="20" width="38" height="14" fill="${C.B}"/>
   <rect x="3" y="23" width="5" height="1" fill="${C.S}"/><rect x="27" y="22" width="6" height="1" fill="${C.S}"/><rect x="14" y="26" width="7" height="1" fill="${C.S}"/><rect x="28" y="30" width="5" height="1" fill="${C.S}"/><rect x="4" y="30" width="4" height="1" fill="${C.S}"/>
   <circle cx="9" cy="27" r="1.8" fill="${C.O}"/><circle cx="33" cy="26" r="1.8" fill="${C.O}"/><circle cx="22" cy="30.5" r="1.8" fill="${C.O}"/>
  </svg>`},
 // 15 — Moo Deng: a wet baby pygmy hippo with its mouth wide open
 {title:'Му Дэнг', family:ANI, direction:'Театр эмоций', subject:'мокрый детёныш карликового бегемота с широко открытой пастью',
  source:M, reference:'Moo Deng — Moo Deng', forLevel:61, cells:900, colours:8,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.G}"/>
   <circle cx="7.5" cy="6" r="2.2" fill="${C.D}"/><circle cx="22.5" cy="6" r="2.2" fill="${C.D}"/>
   <circle cx="7.5" cy="6" r="1" fill="${C.K}"/><circle cx="22.5" cy="6" r="1" fill="${C.K}"/>
   <ellipse cx="15" cy="13" rx="10" ry="7" fill="${C.D}"/>
   <ellipse cx="15" cy="20" rx="11" ry="6.5" fill="${C.D}"/>
   <path d="M7 14 Q15 11.5 23 14 L22 21 Q15 23 8 21 Z" fill="${C.P}"/>
   <ellipse cx="15" cy="19.5" rx="4.5" ry="1.8" fill="${C.R}"/>
   <rect x="8" y="14" width="2" height="2" fill="${C.C}"/><rect x="20" y="14" width="2" height="2" fill="${C.C}"/>
   <rect x="10" y="8" width="2" height="2" fill="${C.C}"/><rect x="18" y="8" width="2" height="2" fill="${C.C}"/>
   <rect x="12" y="5" width="4" height="1" fill="${C.S}"/><rect x="21" y="9" width="2" height="2" fill="${C.S}"/><rect x="5" y="17" width="1" height="3" fill="${C.S}"/>
   <ellipse cx="6" cy="12.5" rx="1.5" ry="1.2" fill="${C.K}"/><ellipse cx="24" cy="12.5" rx="1.5" ry="1.2" fill="${C.K}"/>
   <rect y="24" width="30" height="6" fill="${C.B}"/>
   <rect x="2" y="26" width="5" height="1" fill="${C.S}"/><rect x="21" y="27" width="6" height="1" fill="${C.S}"/>
  </svg>`},
 // 16 — Pedro the raccoon spinning in a dance
 {title:'Енот Педро', family:ANI, direction:'Театр эмоций', subject:'енот кружится в танце, лапы вверх',
  source:M, reference:'мем Pedro — танцующий енот', forLevel:61, cells:896, colours:6,
  svg:`<svg viewBox="0 0 28 32" xmlns="http://www.w3.org/2000/svg">
   <rect width="28" height="32" fill="${C.V}"/><rect y="29" width="28" height="3" fill="${C.N}"/>
   ${line('M3 6 Q1.5 13 4 18',C.Y,1.4)}${line('M25 18 Q27 24 23 28',C.Y,1.4)}
   <rect x="2" y="22" width="3" height="3" fill="${C.D}"/><rect x="5" y="22" width="3" height="3" fill="${C.S}"/><rect x="8" y="21" width="3" height="3" fill="${C.D}"/>
   ${line('M12.5 24 L12.5 28.5',C.S)}${line('M16 24 L19.5 25.5 L19 28',C.S)}
   <rect x="11" y="28" width="3" height="1.5" fill="${C.D}"/><rect x="18" y="27.5" width="3" height="1.5" fill="${C.D}"/>
   ${line('M17.5 15.5 L22 11 L23 6',C.S)}${line('M10.5 15.5 L6 14 L4.5 10',C.S)}
   <rect x="22" y="4.5" width="2.2" height="2" fill="${C.D}"/><rect x="3.5" y="8.5" width="2.2" height="2" fill="${C.D}"/>
   <ellipse cx="14" cy="19.5" rx="5" ry="6" fill="${C.S}"/><ellipse cx="14" cy="20" rx="3" ry="4" fill="${C.C}"/>
   <polygon points="8.5,6 9.5,1.5 12.5,4" fill="${C.S}"/><polygon points="15.5,4 18.5,1.5 19.5,6" fill="${C.S}"/>
   <polygon points="9.5,5 10,3 11.5,4.2" fill="${C.D}"/><polygon points="16.5,4.2 18,3 18.5,5" fill="${C.D}"/>
   <ellipse cx="14" cy="8.5" rx="6.3" ry="5" fill="${C.S}"/>
   <ellipse cx="14" cy="10.6" rx="3.6" ry="2.6" fill="${C.C}"/>
   <polygon points="8,7 13,7.5 13,10 9,10.5" fill="${C.D}"/><polygon points="15,7.5 20,7 19,10.5 15,10" fill="${C.D}"/>
   <rect x="10" y="8" width="2" height="2" fill="${C.C}"/><rect x="16" y="8" width="2" height="2" fill="${C.C}"/>
   <rect x="13" y="11" width="2" height="2" fill="${C.D}"/>
  </svg>`},
 // 17 — Zhdun waiting on a bench in a clinic corridor
 {title:'Ждун', family:'Мемные реакции и типажи', direction:'Театр эмоций', subject:'серое тюленеподобное существо с длинным носом сложило руки и ждёт на скамейке',
  source:M, reference:'Ждун — Homunculus loxodontus', forLevel:61, cells:1224, colours:8,
  svg:`<svg viewBox="0 0 36 34" xmlns="http://www.w3.org/2000/svg">
   <rect width="36" height="34" fill="${C.L}"/><rect y="20" width="36" height="9" fill="${C.G}"/><rect y="29" width="36" height="5" fill="${C.A}"/>
   <rect x="4" y="26" width="2" height="5" fill="${C.E}"/><rect x="30" y="26" width="2" height="5" fill="${C.E}"/>
   <ellipse cx="18" cy="10.5" rx="8" ry="8" fill="${C.D}"/><ellipse cx="18" cy="20" rx="11" ry="7.5" fill="${C.D}"/>
   <ellipse cx="18" cy="10.5" rx="7" ry="7" fill="${C.S}"/><ellipse cx="18" cy="20" rx="10" ry="6.5" fill="${C.S}"/>
   <rect x="2" y="24" width="32" height="3" fill="${C.E}"/>
   <rect x="12" y="9" width="4" height="1" fill="${C.D}"/><rect x="20" y="9" width="4" height="1" fill="${C.D}"/>
   <rect x="12" y="10" width="4" height="1" fill="${C.C}"/><rect x="20" y="10" width="4" height="1" fill="${C.C}"/>
   <rect x="16" y="8" width="4" height="10" rx="2" fill="${C.D}"/><rect x="17" y="8" width="2" height="9" rx="1" fill="${C.S}"/>
   ${line('M10 16 Q12 20.5 15.5 20.5',C.D,1)}${line('M26 16 Q24 20.5 20.5 20.5',C.D,1)}
   <ellipse cx="18" cy="21" rx="3.6" ry="1.8" fill="${C.D}"/><ellipse cx="18" cy="21" rx="2.6" ry="1" fill="${C.K}"/>
  </svg>`},
 // 18 — Big Floppa: a caracal with tall black ear tufts and a calm face
 {title:'Шлёпа', family:ANI, direction:'Театр эмоций', subject:'каракал с высокими чёрными кисточками на ушах и невозмутимой мордой',
  source:M, reference:'Big Floppa — Шлёпа (каракал)', forLevel:61, cells:1224, colours:7,
  svg:`<svg viewBox="0 0 34 36" xmlns="http://www.w3.org/2000/svg">
   <rect width="34" height="36" fill="${C.B}"/>
   <ellipse cx="17" cy="33" rx="12" ry="6" fill="${C.O}"/><ellipse cx="17" cy="32" rx="5" ry="4" fill="${C.C}"/>
   <rect y="32" width="34" height="4" fill="${C.R}"/>
   ${line('M9 7 L8 2',C.D,2)}${line('M25 7 L26 2',C.D,2)}
   <polygon points="6,17 9,5.5 15,11" fill="${C.D}"/><polygon points="19,11 25,5.5 28,17" fill="${C.D}"/>
   <polygon points="8,15 9.6,9 13.5,12" fill="${C.C}"/><polygon points="20.5,12 24.4,9 26,15" fill="${C.C}"/>
   <ellipse cx="17" cy="19.5" rx="10.5" ry="8.5" fill="${C.O}"/>
   <circle cx="15" cy="24" r="3" fill="${C.C}"/><circle cx="19" cy="24" r="3" fill="${C.C}"/>
   <rect x="10" y="17" width="5" height="1" fill="${C.D}"/><rect x="10" y="18" width="5" height="2" fill="${C.Y}"/><rect x="12" y="18" width="1" height="2" fill="${C.D}"/>
   <rect x="19" y="17" width="5" height="1" fill="${C.D}"/><rect x="19" y="18" width="5" height="2" fill="${C.Y}"/><rect x="21" y="18" width="1" height="2" fill="${C.D}"/>
   <rect x="14" y="20" width="1" height="2" fill="${C.D}"/><rect x="19" y="20" width="1" height="2" fill="${C.D}"/>
   <rect x="11" y="15" width="3" height="1" fill="${C.C}"/><rect x="20" y="15" width="3" height="1" fill="${C.C}"/>
   <polygon points="15,21 19,21 17,23" fill="${C.P}"/>
   <rect x="16.5" y="23" width="1" height="1" fill="${C.D}"/><rect x="14.5" y="24" width="5" height="1" fill="${C.D}"/>
  </svg>`},
 // 19 — our Twig calm at a table in a room on fire («This is fine»)
 {title:'Twig: «Всё нормально»', family:MIX, direction:'Театр эмоций', subject:'Твиг в шапочке-жёлуде спокойно сидит за столом в горящей комнате',
  source:'Bloomlings', reference:'мем «This is fine» — Twig в горящей комнате', forLevel:61, cells:900, colours:9,
  svg:`<svg viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
   <rect width="30" height="30" fill="${C.C}"/><rect y="27" width="30" height="3" fill="${C.A}"/>
   <path d="M0 0 L30 0 L30 3 Q27 5 24 3.5 Q21 5.5 18 3.5 Q15 5.5 12 3.5 Q9 5.5 6 3.5 Q3 5 0 3.5 Z" fill="${C.D}"/>
   <polygon points="0,27 0,9 2,13 3.5,6 5.5,13 7,9 8,16 9,27" fill="${C.R}"/>
   <polygon points="1,27 2,18 3.5,21 4.5,15 6,21 7,27" fill="${C.Y}"/>
   <polygon points="30,27 30,10 28,14 26.5,7 24.5,13 23,10 22,16 21,27" fill="${C.R}"/>
   <polygon points="29,27 28,19 26.5,22 25.5,16 24,22 23,27" fill="${C.Y}"/>
   <rect x="9" y="20" width="12" height="3" fill="${C.E}"/>
   <ellipse cx="15" cy="9.5" rx="7" ry="4.6" fill="${C.E}"/><rect x="9" y="12" width="12" height="9" rx="2" fill="${C.E}"/>
   <ellipse cx="15" cy="9" rx="6" ry="3.8" fill="${C.O}"/><rect x="8.5" y="10.5" width="13" height="2" rx="0.8" fill="${C.O}"/>
   <rect x="14" y="3.5" width="2" height="2.5" fill="${C.E}"/><ellipse cx="18.5" cy="4.2" rx="2.4" ry="1.2" fill="${C.G}"/>
   <rect x="10" y="12.5" width="10" height="5.5" rx="1.5" fill="${C.C}"/>
   <rect x="12" y="13.5" width="2" height="2" fill="${C.D}"/><rect x="16" y="13.5" width="2" height="2" fill="${C.D}"/>
   <rect x="13" y="16.5" width="4" height="1" fill="${C.D}"/>
   <rect x="10" y="18" width="10" height="3" fill="${C.O}"/>
   <rect x="2" y="21" width="26" height="2" fill="${C.E}"/><rect x="4" y="23" width="2" height="5" fill="${C.E}"/><rect x="24" y="23" width="2" height="5" fill="${C.E}"/>
   <ellipse cx="9.5" cy="20.5" rx="2" ry="1.2" fill="${C.O}"/><ellipse cx="20.5" cy="20.5" rx="2" ry="1.2" fill="${C.O}"/>
  </svg>`},
 // 20 — our Bloom with a suspicious squint
 {title:'Bloom с подозрительным прищуром', family:MIX, direction:'Театр эмоций', subject:'Блум с лепестками вокруг лица подозрительно щурится',
  source:'Bloomlings', reference:'мем-реакция «подозрительный прищур» — Bloom', forLevel:61, cells:360, colours:5,
  svg:`<svg viewBox="0 0 22 22" xmlns="http://www.w3.org/2000/svg">
   ${[[-90],[-30],[30],[90],[150],[210]].map(([a])=>{const r=a*Math.PI/180;return `<circle cx="${(11+6.6*Math.cos(r)).toFixed(2)}" cy="${(11+6.6*Math.sin(r)).toFixed(2)}" r="4.3" fill="${C.R}"/>`;}).join('')}
   <circle cx="11" cy="11.5" r="6.4" fill="${C.K}"/>
   <circle cx="9.5" cy="5.6" r="1.5" fill="${C.Y}"/><circle cx="12.5" cy="5.6" r="1.5" fill="${C.Y}"/><circle cx="11" cy="4.4" r="1.5" fill="${C.Y}"/>
   <polygon points="6,8.5 10,9.8 10,10.8 6,9.5" fill="${C.D}"/><polygon points="12,9.2 16,8 16,9 12,10.2" fill="${C.D}"/>
   <rect x="6.5" y="12" width="3.5" height="1" fill="${C.D}"/><rect x="12" y="12" width="3.5" height="1" fill="${C.D}"/>
   <rect x="11" y="15" width="3" height="1" fill="${C.D}"/>
   <rect x="6" y="14" width="2" height="1" fill="${C.P}"/><rect x="14" y="14" width="2" height="1" fill="${C.P}"/>
  </svg>`},
]};
