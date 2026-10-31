// SVG drafts of batch 0168, letter E (theme «Мемные и абсурдные герои», 2026-10-31). Pictures 1–18 are meme references
// (source «интернет-мем/тренд», not for release until the owner confirms the rights; catalog §4, §10); 19–20 are our own
// heroes in a meme situation. One viewBox unit = one cell (the owner's «плавнее», 2026-10-31): big shapes are curves,
// small features cell-aligned blocks. Convert with tools/rounds/svg2cells.mjs into batch-0168-E.mjs.
import {C} from '../../../../tools/rounds/svg-palette.mjs';
const M='интернет-мем/тренд',BR='AI-brainrot и абсурдные гибриды',ANI='Мемные животные и бытовые ситуации',MIX='Мемные ремиксы Bloomlings';
export default {theme:'Мемные и абсурдные герои', pictures:[
 // 1 — blue shark standing on three legs in blue sneakers, on a beach
 {title:'Тралалеро Тралала', family:BR, direction:'Предметные гибриды', subject:'синяя акула на трёх ногах в синих кроссовках на пляже',
  source:M, reference:'Italian brainrot — Tralalero Tralala', forLevel:61, cells:1360, colours:8,
  svg:`<svg viewBox="0 0 40 34" xmlns="http://www.w3.org/2000/svg">
  <rect width="40" height="34" fill="${C.S}"/>
  <rect y="19" width="40" height="6" fill="${C.B}"/>
  <path d="M0 25 Q20 22.6 40 25 L40 34 L0 34 Z" fill="${C.Y}"/>
  <path d="M11 8.6 Q7.5 4.5 3.2 1.6 Q4.6 6.5 7.6 11 L12.6 13.2 Z" fill="${C.N}"/>
  <path d="M12 11.6 Q8.4 15.5 4 19.6 Q9.4 19.4 13.6 14.6 Z" fill="${C.N}"/>
  <path d="M17 7.5 Q20 2 25.5 0.6 Q23.6 4 24.6 7.5 Z" fill="${C.N}"/>
  <path d="M9.5 12 C 12 6.5, 25 5, 32 7.2 C 35.5 8.3, 38.5 10.5, 38.5 12.2 C 38 14.8, 32 17.5, 23 17.8 C 15.5 18, 11.5 15.5, 9.5 12 Z" fill="${C.N}"/>
  <path d="M10.6 13.2 C 15 15.2, 27 15.6, 38.3 12.6 C 37 15.2, 31.5 17.6, 23 17.8 C 16 18, 12 15.7, 10.6 13.2 Z" fill="${C.C}"/>
  <path d="M29.5 12.4 Q34 14.4 38.4 11.8 L38.3 13.4 Q34 16.2 29.5 14.6 Z" fill="${C.D}"/>
  <rect x="31" y="8.6" width="2" height="2" fill="${C.D}"/>
  <path d="M11 16 L14 16.6 Q13.7 22 13.6 27 L11.6 27 Q11.4 22 11 16 Z" fill="${C.K}"/>
  <path d="M19 17.4 L22 17.6 Q21.7 22.5 21.6 27 L19.6 27 Q19.4 22.5 19 17.4 Z" fill="${C.K}"/>
  <path d="M27 16.8 L30 16.2 Q29.7 21.5 29.6 27 L27.6 27 Q27.4 21.5 27 16.8 Z" fill="${C.K}"/>
  <path d="M10.6 26 L14.2 26 Q14.8 27.2 16 27.8 Q17 28.3 17 29.6 L10.4 29.6 Q10.2 27.8 10.6 26 Z" fill="${C.B}"/>
  <path d="M18.6 26 L22.2 26 Q22.8 27.2 24 27.8 Q25 28.3 25 29.6 L18.4 29.6 Q18.2 27.8 18.6 26 Z" fill="${C.B}"/>
  <path d="M26.6 26 L30.2 26 Q30.8 27.2 32 27.8 Q33 28.3 33 29.6 L26.4 29.6 Q26.2 27.8 26.6 26 Z" fill="${C.B}"/>
  <rect x="10.2" y="29.4" width="7" height="2" rx="0.8" fill="${C.C}"/>
  <rect x="18.2" y="29.4" width="7" height="2" rx="0.8" fill="${C.C}"/>
  <rect x="26.2" y="29.4" width="7" height="2" rx="0.8" fill="${C.C}"/>
 </svg>`},
 // 2 — ballerina in a pink tutu whose head is a cappuccino with a foam dome, on a stage
 {title:'Балерина Капучина', family:BR, direction:'Предметные гибриды', subject:'балерина в розовой пачке с головой-капучино на сцене',
  source:M, reference:'Italian brainrot — Ballerina Cappuccina', forLevel:61, cells:1360, colours:8,
  svg:`<svg viewBox="0 0 34 40" xmlns="http://www.w3.org/2000/svg">
  <rect width="34" height="40" fill="${C.V}"/>
  <rect y="35" width="34" height="5" fill="${C.E}"/>
  <ellipse cx="16.5" cy="36.6" rx="10" ry="1.9" fill="${C.A}"/>
  <path d="M13.4 19.4 Q8.5 19.6 3.6 22.2" stroke="${C.K}" stroke-width="2.2" fill="none" stroke-linecap="round"/>
  <path d="M19.6 19.4 Q24.5 19.6 29.4 22.2" stroke="${C.K}" stroke-width="2.2" fill="none" stroke-linecap="round"/>
  <path d="M13.4 27 L16.4 27 Q16.2 31 15.8 34.5 L13.8 34.5 Q13.6 31 13.4 27 Z" fill="${C.K}"/>
  <path d="M16 26.5 L19 26.5 Q22.5 28.5 22.2 30 Q21.6 31.4 15.8 31.6 L15.8 29.6 Q18.6 29.6 19.2 29.4 Q18 28.6 16 28.4 Z" fill="${C.K}"/>
  <path d="M13.6 34 L16 34 L15.6 36.6 L14 36.6 Z" fill="${C.R}"/>
  <path d="M12.8 18.6 Q16.5 17.4 20.2 18.6 Q19.6 21.6 19.2 24 L13.8 24 Q13.4 21.6 12.8 18.6 Z" fill="${C.P}"/>
  <path d="M4 25.4 Q16.5 19.4 29 25.4 Q27.6 26.4 26 26.2 Q24.6 27.6 22.6 27 Q20.6 28.4 18.4 27.6 Q16.5 28.8 14.6 27.6 Q12.4 28.4 10.4 27 Q8.4 27.6 7 26.2 Q5.4 26.4 4 25.4 Z" fill="${C.P}"/>
  <rect x="15.5" y="16" width="2" height="2.6" fill="${C.K}"/>
  <ellipse cx="16.5" cy="16.4" rx="7.6" ry="1.5" fill="${C.C}"/>
  <circle cx="26.6" cy="9.6" r="3.8" fill="${C.C}"/><circle cx="27" cy="9.6" r="1.7" fill="${C.V}"/>
  <path d="M6.6 4.6 L26.4 4.6 L25 11 Q23.8 15.4 19.6 15.6 L13.4 15.6 Q9.2 15.4 8 11 Z" fill="${C.C}"/>
  <ellipse cx="16.5" cy="4.6" rx="9.9" ry="3.2" fill="${C.A}"/>
  <path d="M16.5 7.2 L13.4 4.8 Q12.6 2.8 14.4 2.6 Q15.8 2.5 16.5 3.8 Q17.2 2.5 18.6 2.6 Q20.4 2.8 19.6 4.8 Z" fill="${C.C}"/>
  <rect x="12" y="9" width="2" height="3" fill="${C.D}"/><rect x="19" y="9" width="2" height="3" fill="${C.D}"/>
  <path d="M14.5 12.4 L18.5 12.4 Q18 14.4 16.5 14.4 Q15 14.4 14.5 12.4 Z" fill="${C.R}"/>
 </svg>`},
 // 3 — a wooden log with a face holding a baseball bat
 {title:'Тунг Тунг Тунг Сахур', family:BR, direction:'Ожившие вещи', subject:'деревянное полено с лицом и бейсбольной битой',
  source:M, reference:'brainrot — Tung Tung Tung Sahur', forLevel:61, cells:1292, colours:8,
  svg:`<svg viewBox="0 0 34 38" xmlns="http://www.w3.org/2000/svg">
  <rect width="34" height="38" fill="${C.K}"/>
  <path d="M0 32 Q17 29.5 34 32 L34 38 L0 38 Z" fill="${C.M}"/>
  <path d="M11.5 18 Q8 20.5 7 25" stroke="${C.E}" stroke-width="2.2" fill="none" stroke-linecap="round"/>
  <circle cx="7" cy="25.6" r="1.8" fill="${C.E}"/>
  <path d="M23 18.5 Q26.5 18.5 27.2 15.4" stroke="${C.E}" stroke-width="2.2" fill="none" stroke-linecap="round"/>
  <g transform="translate(27.4 16) rotate(18)">
   <path d="M-1.1 1 L1.1 1 L2.2 -8 Q2.3 -14.6 0 -15 Q-2.3 -14.6 -2.2 -8 Z" fill="${C.Y}"/>
   <ellipse cx="0" cy="1.6" rx="1.8" ry="1.1" fill="${C.Y}"/>
  </g>
  <circle cx="27.4" cy="14.4" r="1.9" fill="${C.E}"/>
  <path d="M14 29 L13.4 33.4" stroke="${C.E}" stroke-width="2.2" fill="none"/>
  <path d="M20.5 29 L21.1 33.4" stroke="${C.E}" stroke-width="2.2" fill="none"/>
  <ellipse cx="12.2" cy="34" rx="2.8" ry="1.3" fill="${C.E}"/><ellipse cx="22.3" cy="34" rx="2.8" ry="1.3" fill="${C.E}"/>
  <rect x="10" y="5" width="14.5" height="25" rx="3.4" fill="${C.A}"/>
  <path d="M21.4 6 L24.5 6 L24.5 26.6 Q24.5 30 21.6 30 L21.2 30 Q22.2 18 21.4 6 Z" fill="${C.E}"/>
  <ellipse cx="17.25" cy="5.6" rx="7.25" ry="2.6" fill="${C.Y}"/>
  <rect x="11.5" y="10" width="4" height="5" rx="1" fill="${C.C}"/><rect x="17.5" y="10" width="4" height="5" rx="1" fill="${C.C}"/>
  <rect x="13" y="11.5" width="2" height="3" fill="${C.D}"/><rect x="18" y="11.5" width="2" height="3" fill="${C.D}"/>
  <rect x="13.5" y="18" width="6" height="3" rx="1.2" fill="${C.D}"/>
 </svg>`},
 // 4 — a monkey made of a tree trunk with big feet and a small hat
 {title:'Брр Брр Патапим', family:BR, direction:'Предметные гибриды', subject:'обезьяна из древесного ствола с большими ступнями и маленькой шляпой',
  source:M, reference:'Italian brainrot — Brr Brr Patapim', forLevel:61, cells:1292, colours:9,
  svg:`<svg viewBox="0 0 34 38" xmlns="http://www.w3.org/2000/svg">
  <rect width="34" height="38" fill="${C.S}"/>
  <path d="M0 31 Q17 28.6 34 31 L34 38 L0 38 Z" fill="${C.G}"/>
  <path d="M11.5 19 Q6.5 17 5 10.5" stroke="${C.E}" stroke-width="2.4" fill="none" stroke-linecap="round"/>
  <path d="M22.5 19 Q27.5 17 29 10.5" stroke="${C.E}" stroke-width="2.4" fill="none" stroke-linecap="round"/>
  <path d="M7 14.5 L3.6 13.4" stroke="${C.E}" stroke-width="2" fill="none" stroke-linecap="round"/>
  <path d="M27 14.5 L30.4 13.4" stroke="${C.E}" stroke-width="2" fill="none" stroke-linecap="round"/>
  <circle cx="4.6" cy="8.4" r="3" fill="${C.G}"/><circle cx="29.4" cy="8.4" r="3" fill="${C.G}"/>
  <path d="M11 18 Q10.6 26 9.4 30.4 L15.6 30.4 L17 26.4 L18.4 30.4 L24.6 30.4 Q23.4 26 23 18 Z" fill="${C.E}"/>
  <rect x="13" y="20" width="2" height="7" rx="1" fill="${C.A}"/><rect x="19" y="20" width="2" height="7" rx="1" fill="${C.A}"/>
  <path d="M10.4 29.4 L15.6 29.4 Q16.4 31.6 14.6 33.4 Q11 34.8 7 34 Q4.8 33.2 5.8 31.6 Q7.8 30.6 10.4 29.4 Z" fill="${C.K}"/>
  <path d="M23.6 29.4 L18.4 29.4 Q17.6 31.6 19.4 33.4 Q23 34.8 27 34 Q29.2 33.2 28.2 31.6 Q26.2 30.6 23.6 29.4 Z" fill="${C.K}"/>
  <ellipse cx="17" cy="12.4" rx="8" ry="7.2" fill="${C.A}"/>
  <path d="M12 10 Q12 7 14.5 7.4 Q17 8.4 19.5 7.4 Q22 7 22 10 Q23 14 21 16.6 Q17 19 13 16.6 Q11 14 12 10 Z" fill="${C.C}"/>
  <rect x="13.5" y="9.4" width="2" height="3" fill="${C.D}"/><rect x="18.5" y="9.4" width="2" height="3" fill="${C.D}"/>
  <path d="M15.4 11.6 L18.6 11.6 Q20 14.4 19 15.6 Q17 16.6 15 15.6 Q14 14.4 15.4 11.6 Z" fill="${C.P}"/>
  <rect x="14.5" y="16.6" width="5" height="1.6" rx="0.8" fill="${C.D}"/>
  <rect x="11.6" y="5" width="10.8" height="2" rx="1" fill="${C.M}"/>
  <path d="M13.6 5.4 Q13.6 1.8 17 1.8 Q20.4 1.8 20.4 5.4 Z" fill="${C.M}"/>
 </svg>`},
 // 5 — a crocodile head on a bomber plane among clouds, seen from above
 {title:'Бомбардиро Крокодило', family:BR, direction:'Предметные гибриды', subject:'крокодил-бомбардировщик летит среди облаков',
  source:M, reference:'Italian brainrot — Bombardiro Crocodilo', forLevel:61, cells:1280, colours:8,
  svg:`<svg viewBox="0 0 40 32" xmlns="http://www.w3.org/2000/svg">
  <rect width="40" height="32" fill="${C.S}"/>
  <circle cx="2" cy="3" r="3.6" fill="${C.C}"/><circle cx="7" cy="1.6" r="3.4" fill="${C.C}"/><circle cx="10.6" cy="0" r="2.6" fill="${C.C}"/>
  <circle cx="38" cy="29" r="3.8" fill="${C.C}"/><circle cx="33" cy="31" r="3.4" fill="${C.C}"/><circle cx="28.6" cy="32.4" r="2.6" fill="${C.C}"/>
  <path d="M3.6 14 L8.4 13.8 L7 6.6 Q5.4 6 3.6 6.6 Z" fill="${C.M}"/>
  <path d="M3.6 18 L8.4 18.2 L7 25.4 Q5.4 26 3.6 25.4 Z" fill="${C.M}"/>
  <rect x="20" y="3.4" width="5.6" height="2.8" rx="1.2" fill="${C.D}"/><rect x="20" y="8.4" width="5.6" height="2.8" rx="1.2" fill="${C.D}"/>
  <rect x="20" y="20.8" width="5.6" height="2.8" rx="1.2" fill="${C.D}"/><rect x="20" y="25.8" width="5.6" height="2.8" rx="1.2" fill="${C.D}"/>
  <rect x="25" y="2" width="2" height="5.6" rx="0.8" fill="${C.D}"/><rect x="25" y="7" width="2" height="5.6" rx="0.8" fill="${C.D}"/>
  <rect x="25" y="19.4" width="2" height="5.6" rx="0.8" fill="${C.D}"/><rect x="25" y="24.4" width="2" height="5.6" rx="0.8" fill="${C.D}"/>
  <path d="M14.6 13 L22.4 13 L21.2 2 Q19 1 16.4 2 Z" fill="${C.M}"/>
  <path d="M14.6 19 L22.4 19 L21.2 30 Q19 31 16.4 30 Z" fill="${C.M}"/>
  <path d="M2.6 15 Q2.6 13.4 4.6 13.4 L28 12.8 L28 19.2 L4.6 18.6 Q2.6 18.6 2.6 17 Z" fill="${C.G}"/>
  <path d="M26 11.6 Q29 10.8 31.2 12.2 L38 13.2 Q40 13.8 40 16 Q40 18.2 38 18.8 L31.2 19.8 Q29 21.2 26 20.4 Z" fill="${C.G}"/>
  <rect x="32" y="11" width="2" height="2" fill="${C.C}"/><rect x="35" y="11.6" width="2" height="2" fill="${C.C}"/>
  <rect x="32" y="19" width="2" height="2" fill="${C.C}"/><rect x="35" y="18.4" width="2" height="2" fill="${C.C}"/>
  <circle cx="29.4" cy="12" r="2.4" fill="${C.G}"/><circle cx="29.4" cy="20" r="2.4" fill="${C.G}"/>
  <rect x="28" y="10.6" width="3" height="3" rx="0.6" fill="${C.Y}"/><rect x="29" y="11" width="2" height="2" fill="${C.D}"/>
  <rect x="28" y="18.4" width="3" height="3" rx="0.6" fill="${C.Y}"/><rect x="29" y="19" width="2" height="2" fill="${C.D}"/>
  <rect x="37" y="14" width="2" height="1.6" fill="${C.D}"/><rect x="37" y="16.4" width="2" height="1.6" fill="${C.D}"/>
  <rect x="22.6" y="14.4" width="3.4" height="3.2" rx="1" fill="${C.B}"/>
 </svg>`},
 // 6 — an elephant that is a cactus, in sandals (side view)
 {title:'Лирили Ларила', family:BR, direction:'Визуальные каламбуры', subject:'слон-кактус в сандалиях',
  source:M, reference:'Italian brainrot — Lirilì Larilà', forLevel:61, cells:1368, colours:9,
  svg:`<svg viewBox="0 0 38 36" xmlns="http://www.w3.org/2000/svg">
  <rect width="38" height="36" fill="${C.V}"/>
  <path d="M0 31 Q19 28.6 38 31 L38 36 L0 36 Z" fill="${C.Y}"/>
  <path d="M33 15 Q36 17 35.6 21" stroke="${C.G}" stroke-width="1.8" fill="none" stroke-linecap="round"/>
  <path d="M18.8 22 L18.8 30.4" stroke="${C.M}" stroke-width="3.4" fill="none"/>
  <path d="M31.8 21 L31.8 30.4" stroke="${C.M}" stroke-width="3.4" fill="none"/>
  <ellipse cx="23" cy="17.6" rx="11" ry="8.6" fill="${C.G}"/>
  <rect x="17" y="10" width="2" height="15" rx="1" fill="${C.M}"/><rect x="22" y="9" width="2" height="17" rx="1" fill="${C.M}"/>
  <rect x="27" y="9.6" width="2" height="15.6" rx="1" fill="${C.M}"/>
  <path d="M12.4 22 L12.4 30.4" stroke="${C.G}" stroke-width="3.4" fill="none"/>
  <path d="M26.2 22 L26.2 30.4" stroke="${C.G}" stroke-width="3.4" fill="none"/>
  <circle cx="10.4" cy="13" r="6.8" fill="${C.G}"/>
  <path d="M6 16 Q3 19.6 3.4 24.6 Q3.8 27.8 6.8 27" stroke="${C.G}" stroke-width="3.4" fill="none" stroke-linecap="round"/>
  <ellipse cx="14.4" cy="14" rx="3.8" ry="5.4" fill="${C.M}"/>
  <path d="M7.6 17.6 Q8.6 21.4 11.4 21.8 L11.6 20 Q9.6 19.6 9.4 17.4 Z" fill="${C.C}"/>
  <rect x="7" y="10" width="2" height="3" fill="${C.D}"/>
  <rect x="20" y="13" width="2" height="2" fill="${C.C}"/><rect x="25" y="17" width="2" height="2" fill="${C.C}"/>
  <rect x="30" y="13" width="2" height="2" fill="${C.C}"/><rect x="20" y="20" width="2" height="2" fill="${C.C}"/>
  <rect x="25" y="11" width="2" height="2" fill="${C.C}"/><rect x="30" y="20" width="2" height="2" fill="${C.C}"/>
  <rect x="11" y="7" width="2" height="2" fill="${C.C}"/><rect x="4" y="13" width="2" height="2" fill="${C.C}"/>
  <circle cx="10.4" cy="5.6" r="2.4" fill="${C.P}"/><rect x="9.4" y="4.6" width="2" height="2" fill="${C.Y}"/>
  <rect x="16.6" y="30" width="4.6" height="2.4" rx="1.1" fill="${C.A}"/><rect x="29.6" y="30" width="4.6" height="2.4" rx="1.1" fill="${C.A}"/>
  <rect x="9.8" y="30" width="5" height="2.4" rx="1.1" fill="${C.A}"/><rect x="23.6" y="30" width="5" height="2.4" rx="1.1" fill="${C.A}"/>
  <rect x="10.6" y="28.4" width="3.6" height="2" fill="${C.E}"/><rect x="24.4" y="28.4" width="3.6" height="2" fill="${C.E}"/>
 </svg>`},
 // 7 — a chimp's head inside a peeled banana
 {title:'Шимпанзини Бананини', family:BR, direction:'Кулинарная фантастика', subject:'голова шимпанзе в очищенном банане',
  source:M, reference:'Italian brainrot — Chimpanzini Bananini', forLevel:61, cells:1292, colours:8,
  svg:`<svg viewBox="0 0 34 38" xmlns="http://www.w3.org/2000/svg">
  <rect width="34" height="38" fill="${C.G}"/>
  <path d="M0 33.6 Q17 31.4 34 33.6 L34 38 L0 38 Z" fill="${C.M}"/>
  <path d="M10.4 18.6 Q9.4 28 13 32.8 Q16 36.4 19.8 36.4 Q23.6 31.4 24 26 Q24.2 22 23.6 18.6 Z" fill="${C.Y}"/>
  <path d="M21.4 19.6 Q22.4 27 20.6 31.6 Q19.4 34.8 17.6 36.2 Q18.6 36.4 19.8 36.4 Q23.6 31.4 24 26 Q24.2 22 23.6 18.6 Z" fill="${C.A}"/>
  <rect x="18" y="35" width="2.6" height="2.6" rx="0.6" fill="${C.E}"/>
  <path d="M11.4 18.8 Q6 20.2 4.2 27 Q3.8 30.4 5.4 31.4 L7.8 31 Q7.6 26 9.6 23.6 Q11 22 12.6 21.2 Z" fill="${C.Y}"/>
  <path d="M22.6 18.8 Q28 20.2 29.8 27 Q30.2 30.4 28.6 31.4 L26.2 31 Q26.4 26 24.4 23.6 Q23 22 21.4 21.2 Z" fill="${C.Y}"/>
  <path d="M10.6 19.4 Q7.4 21.4 6.4 26 L8.4 25.4 Q9.4 22.8 11.8 21.2 Z" fill="${C.C}"/>
  <path d="M23.4 19.4 Q26.6 21.4 27.6 26 L25.6 25.4 Q24.6 22.8 22.2 21.2 Z" fill="${C.C}"/>
  <rect x="4.4" y="30" width="2.6" height="2.4" rx="0.8" fill="${C.E}"/><rect x="27" y="30" width="2.6" height="2.4" rx="0.8" fill="${C.E}"/>
  <circle cx="7.6" cy="12" r="2.9" fill="${C.E}"/><circle cx="26.4" cy="12" r="2.9" fill="${C.E}"/>
  <rect x="6.6" y="11" width="2" height="2" fill="${C.K}"/><rect x="25.4" y="11" width="2" height="2" fill="${C.K}"/>
  <ellipse cx="17" cy="12" rx="9.2" ry="8.4" fill="${C.E}"/>
  <path d="M10 11 Q10 6.4 13.4 6.4 Q15.6 6.4 17 8.2 Q18.4 6.4 20.6 6.4 Q24 6.4 24 11 Q25.4 14 24.4 17 Q22.4 20.4 17 20.4 Q11.6 20.4 9.6 17 Q8.6 14 10 11 Z" fill="${C.C}"/>
  <rect x="12.6" y="9" width="3" height="3" rx="0.6" fill="${C.D}"/><rect x="18.4" y="9" width="3" height="3" rx="0.6" fill="${C.D}"/>
  <rect x="15" y="13.2" width="4" height="2" rx="0.8" fill="${C.E}"/>
  <path d="M12.6 16 Q17 19.2 21.4 16 L21.4 17.6 Q17 20.6 12.6 17.6 Z" fill="${C.E}"/>
 </svg>`},
 // 8 — a camel whose body is a fridge, in the desert
 {title:'Фриго Камело', family:BR, direction:'Предметные гибриды', subject:'верблюд с туловищем-холодильником в пустыне',
  source:M, reference:'Italian brainrot — Frigo Camelo', forLevel:61, cells:1368, colours:8,
  svg:`<svg viewBox="0 0 38 36" xmlns="http://www.w3.org/2000/svg">
  <rect width="38" height="36" fill="${C.S}"/>
  <circle cx="5" cy="5" r="3.4" fill="${C.Y}"/>
  <path d="M0 29 Q10 25.4 20 28 Q29 30 38 27 L38 36 L0 36 Z" fill="${C.Y}"/>
  <path d="M6.6 14 Q3.4 16 3.2 20" stroke="${C.A}" stroke-width="2" fill="none" stroke-linecap="round"/>
  <path d="M8.6 26 L8.2 32.4" stroke="${C.A}" stroke-width="2.4" fill="none"/>
  <path d="M12.6 26 L12.4 32.4" stroke="${C.A}" stroke-width="2.4" fill="none"/>
  <path d="M19.6 26 L19.8 32.4" stroke="${C.A}" stroke-width="2.4" fill="none"/>
  <path d="M23.6 26 L24 32.4" stroke="${C.A}" stroke-width="2.4" fill="none"/>
  <ellipse cx="8.2" cy="32.8" rx="1.8" ry="1.1" fill="${C.E}"/><ellipse cx="12.4" cy="32.8" rx="1.8" ry="1.1" fill="${C.E}"/>
  <ellipse cx="19.8" cy="32.8" rx="1.8" ry="1.1" fill="${C.E}"/><ellipse cx="24" cy="32.8" rx="1.8" ry="1.1" fill="${C.E}"/>
  <path d="M10 12.6 Q12 6.6 16 6.6 Q20 6.6 22 12.6 Z" fill="${C.A}"/>
  <path d="M23 15 Q26 13 27 8 Q27.6 4.6 30 4.2" stroke="${C.A}" stroke-width="3.6" fill="none" stroke-linecap="round"/>
  <path d="M27.6 5.6 Q28 2.2 31.4 2.2 Q34 2.2 35.6 3.6 Q37.2 4.6 37 6.4 Q36.6 8 34.6 8 L30.6 8.2 Q28 8 27.6 5.6 Z" fill="${C.A}"/>
  <path d="M28.6 3 L28.2 0.8 L30 1.8 Z" fill="${C.A}"/>
  <rect x="31" y="3.6" width="2" height="2" fill="${C.D}"/>
  <path d="M34.6 6.4 Q35.8 6.6 36.6 6" stroke="${C.E}" stroke-width="1" fill="none"/>
  <rect x="6" y="12" width="20" height="15" rx="2.4" fill="${C.C}"/>
  <rect x="6" y="17" width="20" height="1.4" fill="${C.D}"/>
  <rect x="22" y="13.6" width="2" height="2.6" rx="0.6" fill="${C.D}"/>
  <rect x="22" y="19.6" width="2" height="5" rx="0.6" fill="${C.D}"/>
  <rect x="9" y="20" width="4" height="3" rx="0.8" fill="${C.R}"/>
 </svg>`},
 // 9 — a frog head on a tyre with human legs
 {title:'Бонека Амбалабу', family:BR, direction:'Предметные гибриды', subject:'голова лягушки на шине с человеческими ногами',
  source:M, reference:'Italian brainrot — Boneca Ambalabu', forLevel:61, cells:1292, colours:8,
  svg:`<svg viewBox="0 0 34 38" xmlns="http://www.w3.org/2000/svg">
  <rect width="34" height="38" fill="${C.S}"/>
  <path d="M0 33 Q17 30.6 34 33 L34 38 L0 38 Z" fill="${C.M}"/>
  <path d="M13 29 L12.4 34" stroke="${C.K}" stroke-width="2.8" fill="none"/>
  <path d="M21 29 L21.6 34" stroke="${C.K}" stroke-width="2.8" fill="none"/>
  <path d="M10 33.4 L14 33.4 Q16.4 33.6 16.4 35.4 L16.4 36 L9.6 36 Q9.4 34.4 10 33.4 Z" fill="${C.R}"/>
  <path d="M24 33.4 L20 33.4 Q17.6 33.6 17.6 35.4 L17.6 36 L24.4 36 Q24.6 34.4 24 33.4 Z" fill="${C.R}"/>
  <circle cx="17" cy="22" r="9.4" fill="${C.D}"/>
  <circle cx="17" cy="22" r="4.6" fill="${C.C}"/>
  <rect x="16" y="21" width="2" height="2" fill="${C.D}"/>
  <ellipse cx="17" cy="11.6" rx="11" ry="6.6" fill="${C.G}"/>
  <path d="M8.4 13.4 Q17 18.6 25.6 13.4 Q24 17.4 17 18 Q10 17.4 8.4 13.4 Z" fill="${C.L}"/>
  <circle cx="10.6" cy="6.4" r="4" fill="${C.G}"/><circle cx="23.4" cy="6.4" r="4" fill="${C.G}"/>
  <circle cx="10.6" cy="6" r="2.7" fill="${C.C}"/><circle cx="23.4" cy="6" r="2.7" fill="${C.C}"/>
  <rect x="10" y="5" width="2" height="2" fill="${C.D}"/><rect x="22" y="5" width="2" height="2" fill="${C.D}"/>
  <path d="M8.6 12.4 Q17 16.4 25.4 12.4 L25.4 13.8 Q17 18 8.6 13.8 Z" fill="${C.D}"/>
 </svg>`},
 // 10 — Labubu: a furry monster figure with tall rabbit ears and a jagged toothy grin
 {title:'Лабубу', family:'Коллекционный хайп и маскоты', subject:'пушистый монстрик с заячьими ушами и зубастой улыбкой',
  source:M, reference:'Pop Mart / The Monsters — Labubu', forLevel:61, cells:1280, colours:6,
  svg:`<svg viewBox="0 0 32 40" xmlns="http://www.w3.org/2000/svg">
  <rect width="32" height="40" fill="${C.L}"/>
  <path d="M8.6 12 Q4.6 6 6 1.2 Q8.2 0.4 10.2 2.8 Q12.6 6.4 13.4 10 Z" fill="${C.P}"/>
  <path d="M23.4 12 Q27.4 6 26 1.2 Q23.8 0.4 21.8 2.8 Q19.4 6.4 18.6 10 Z" fill="${C.P}"/>
  <path d="M9.4 10.4 Q7.4 6.4 7.8 3.4 Q9.4 4.4 10.4 6.4 Q11.4 8.4 11.8 10 Z" fill="${C.K}"/>
  <path d="M22.6 10.4 Q24.6 6.4 24.2 3.4 Q22.6 4.4 21.6 6.4 Q20.6 8.4 20.2 10 Z" fill="${C.K}"/>
  <path d="M10 28 Q6.6 29 5.6 33" stroke="${C.P}" stroke-width="3" fill="none" stroke-linecap="round"/>
  <path d="M22 28 Q25.4 29 26.4 33" stroke="${C.P}" stroke-width="3" fill="none" stroke-linecap="round"/>
  <ellipse cx="16" cy="32" rx="7.4" ry="6" fill="${C.P}"/>
  <ellipse cx="12" cy="37.6" rx="3.4" ry="1.8" fill="${C.P}"/><ellipse cx="20" cy="37.6" rx="3.4" ry="1.8" fill="${C.P}"/>
  <ellipse cx="16" cy="18" rx="12" ry="10" fill="${C.P}"/>
  <ellipse cx="16" cy="19.4" rx="9.2" ry="7.6" fill="${C.C}"/>
  <rect x="9" y="13" width="4" height="5" rx="1.4" fill="${C.D}"/><rect x="19" y="13" width="4" height="5" rx="1.4" fill="${C.D}"/>
  <rect x="9" y="13" width="2" height="2" fill="${C.C}"/><rect x="19" y="13" width="2" height="2" fill="${C.C}"/>
  <path d="M7.6 19 L24.4 19 Q24 27 16 27 Q8 27 7.6 19 Z" fill="${C.D}"/>
  <rect x="9" y="20" width="2" height="2" fill="${C.C}"/><rect x="12" y="20" width="2" height="3" fill="${C.C}"/>
  <rect x="15" y="20" width="2" height="3" fill="${C.C}"/><rect x="18" y="20" width="2" height="3" fill="${C.C}"/>
  <rect x="21" y="20" width="2" height="2" fill="${C.C}"/>
 </svg>`},
 // 11 — Oren from Sprunki: an orange round character with headphones
 {title:'Орен из Спрунки', family:'Игровая интернет-культура', subject:'оранжевый круглый персонаж в наушниках',
  source:M, reference:'Sprunki — Oren', forLevel:61, cells:1292, colours:7,
  svg:`<svg viewBox="0 0 34 38" xmlns="http://www.w3.org/2000/svg">
  <rect width="34" height="38" fill="${C.B}"/>
  <path d="M0 33 Q17 30.6 34 33 L34 38 L0 38 Z" fill="${C.N}"/>
  <path d="M11 27 Q7 28 6 31" stroke="${C.O}" stroke-width="2.6" fill="none" stroke-linecap="round"/>
  <path d="M23 27 Q27 28 28 31" stroke="${C.O}" stroke-width="2.6" fill="none" stroke-linecap="round"/>
  <path d="M13.6 32 L13.4 35" stroke="${C.O}" stroke-width="2.8" fill="none"/>
  <path d="M20.4 32 L20.6 35" stroke="${C.O}" stroke-width="2.8" fill="none"/>
  <ellipse cx="12.8" cy="35.4" rx="2.6" ry="1.2" fill="${C.D}"/><ellipse cx="21.2" cy="35.4" rx="2.6" ry="1.2" fill="${C.D}"/>
  <ellipse cx="17" cy="28.6" rx="6.6" ry="5" fill="${C.O}"/>
  <circle cx="17" cy="14.4" r="10.6" fill="${C.O}"/>
  <path d="M5.6 14 Q5.6 1.8 17 1.8 Q28.4 1.8 28.4 14" stroke="${C.D}" stroke-width="2.4" fill="none"/>
  <rect x="3.4" y="9.6" width="5.4" height="9.6" rx="2.4" fill="${C.D}"/><rect x="25.2" y="9.6" width="5.4" height="9.6" rx="2.4" fill="${C.D}"/>
  <rect x="5.4" y="11.6" width="2" height="5.6" rx="1" fill="${C.R}"/><rect x="26.6" y="11.6" width="2" height="5.6" rx="1" fill="${C.R}"/>
  <rect x="11" y="9.6" width="4" height="5" rx="1.4" fill="${C.C}"/><rect x="19" y="9.6" width="4" height="5" rx="1.4" fill="${C.C}"/>
  <rect x="12.6" y="11" width="2" height="3" fill="${C.D}"/><rect x="20.6" y="11" width="2" height="3" fill="${C.D}"/>
  <path d="M12 17.6 L22 17.6 Q21.4 22.4 17 22.4 Q12.6 22.4 12 17.6 Z" fill="${C.D}"/>
  <path d="M14 20.4 Q17 19 20 20.4 Q19 22.2 17 22.2 Q15 22.2 14 20.4 Z" fill="${C.R}"/>
 </svg>`},
 // 12 — Doge: a Shiba Inu's side-eye face
 {title:'Доге', family:ANI, direction:'Театр эмоций', subject:'морда сиба-ину с косым взглядом',
  source:M, reference:'Doge — Kabosu', forLevel:61, cells:1368, colours:7,
  svg:`<svg viewBox="0 0 36 38" xmlns="http://www.w3.org/2000/svg">
  <rect width="36" height="38" fill="${C.K}"/>
  <path d="M2 38 Q3 28 10 26 L26 26 Q33 28 34 38 Z" fill="${C.O}"/>
  <path d="M12 27 Q18 25 24 27 Q24 33 18 38 Q12 33 12 27 Z" fill="${C.C}"/>
  <path d="M6.6 12 Q6 4.6 8.2 0.8 Q12.4 3 15.4 7.6 Z" fill="${C.O}"/>
  <path d="M29.4 12 Q30 4.6 27.8 0.8 Q23.6 3 20.6 7.6 Z" fill="${C.O}"/>
  <path d="M8.4 9.6 Q8.2 6 9.2 4 Q11.6 5.6 13 7.8 Z" fill="${C.C}"/>
  <path d="M27.6 9.6 Q27.8 6 26.8 4 Q24.4 5.6 23 7.8 Z" fill="${C.C}"/>
  <path d="M18 5.6 Q29.6 5.6 30.6 15.6 Q31.4 22.6 27.4 26.4 Q23 29.4 18 29.4 Q13 29.4 8.6 26.4 Q4.6 22.6 5.4 15.6 Q6.4 5.6 18 5.6 Z" fill="${C.O}"/>
  <path d="M5.6 18.4 Q9 18 12 20.6 Q14.4 22 14.4 24.6 L14.4 28.8 Q11 28.6 8.6 26.4 Q5.2 23 5.6 18.4 Z" fill="${C.C}"/>
  <path d="M30.4 18.4 Q27 18 24 20.6 Q21.6 22 21.6 24.6 L21.6 28.8 Q25 28.6 27.4 26.4 Q30.8 23 30.4 18.4 Z" fill="${C.C}"/>
  <ellipse cx="18" cy="23.4" rx="6.4" ry="5.2" fill="${C.C}"/>
  <rect x="11" y="10" width="3" height="2" rx="0.8" fill="${C.C}"/><rect x="22" y="10" width="3" height="2" rx="0.8" fill="${C.C}"/>
  <path d="M9.6 15.4 Q12.4 13 15.6 14.4 Q15.4 17.4 12.6 17.6 Q10.4 17.4 9.6 15.4 Z" fill="${C.C}"/>
  <path d="M20.4 14.4 Q23.6 13 26.4 15.4 Q25.6 17.4 23.4 17.6 Q20.6 17.4 20.4 14.4 Z" fill="${C.C}"/>
  <rect x="10" y="14.4" width="2.4" height="3" fill="${C.D}"/><rect x="20.6" y="14.4" width="2.4" height="3" fill="${C.D}"/>
  <path d="M15.4 19.4 L20.6 19.4 Q20.4 22 18 22 Q15.6 22 15.4 19.4 Z" fill="${C.D}"/>
  <path d="M14.4 24.6 Q16.4 26 18 24.6 Q19.6 26 21.6 24.6 L21.6 25.8 Q19.6 27.4 18 26 Q16.4 27.4 14.4 25.8 Z" fill="${C.E}"/>
  <rect x="17" y="21.6" width="2" height="3" fill="${C.E}"/>
 </svg>`},
 // 13 — Nyan Cat: a cat with a pop-tart body flying with a rainbow trail across a night sky
 {title:'Нян Кэт', family:'Вирусные анимационные персонажи и мини-вселенные', direction:'Невесомый мир',
  subject:'кот с туловищем-тостом в розовой глазури летит с радугой по ночному небу',
  source:M, reference:'Nyan Cat — Nyan Cat', forLevel:61, cells:1320, colours:9,
  svg:`<svg viewBox="0 0 44 30" xmlns="http://www.w3.org/2000/svg">
  <rect width="44" height="30" fill="${C.N}"/>
  <rect x="3" y="1" width="2" height="4" fill="${C.Y}"/><rect x="2" y="2" width="4" height="2" fill="${C.Y}"/><rect x="13" y="24" width="2" height="4" fill="${C.Y}"/><rect x="12" y="25" width="4" height="2" fill="${C.Y}"/><rect x="39" y="2" width="2" height="4" fill="${C.Y}"/><rect x="38" y="3" width="4" height="2" fill="${C.Y}"/><rect x="40" y="23" width="2" height="4" fill="${C.Y}"/><rect x="39" y="24" width="4" height="2" fill="${C.Y}"/><rect x="25" y="25" width="2" height="4" fill="${C.Y}"/><rect x="24" y="26" width="4" height="2" fill="${C.Y}"/>
  <rect x="0" y="8" width="4.5" height="2" fill="${C.R}"/> <rect x="4.5" y="9" width="4.5" height="2" fill="${C.R}"/> <rect x="9" y="8" width="4.5" height="2" fill="${C.R}"/> <rect x="13.5" y="9" width="4.5" height="2" fill="${C.R}"/>
  <rect x="0" y="10" width="4.5" height="2" fill="${C.O}"/> <rect x="4.5" y="11" width="4.5" height="2" fill="${C.O}"/> <rect x="9" y="10" width="4.5" height="2" fill="${C.O}"/> <rect x="13.5" y="11" width="4.5" height="2" fill="${C.O}"/>
  <rect x="0" y="12" width="4.5" height="2" fill="${C.Y}"/> <rect x="4.5" y="13" width="4.5" height="2" fill="${C.Y}"/> <rect x="9" y="12" width="4.5" height="2" fill="${C.Y}"/> <rect x="13.5" y="13" width="4.5" height="2" fill="${C.Y}"/>
  <rect x="0" y="14" width="4.5" height="2" fill="${C.G}"/> <rect x="4.5" y="15" width="4.5" height="2" fill="${C.G}"/> <rect x="9" y="14" width="4.5" height="2" fill="${C.G}"/> <rect x="13.5" y="15" width="4.5" height="2" fill="${C.G}"/>
  <rect x="0" y="16" width="4.5" height="2" fill="${C.B}"/> <rect x="4.5" y="17" width="4.5" height="2" fill="${C.B}"/> <rect x="9" y="16" width="4.5" height="2" fill="${C.B}"/> <rect x="13.5" y="17" width="4.5" height="2" fill="${C.B}"/>
  <rect x="0" y="18" width="4.5" height="2" fill="${C.V}"/> <rect x="4.5" y="19" width="4.5" height="2" fill="${C.V}"/> <rect x="9" y="18" width="4.5" height="2" fill="${C.V}"/> <rect x="13.5" y="19" width="4.5" height="2" fill="${C.V}"/>
  <path d="M12 12.6 Q14 14.6 17 14.6" stroke="${C.C}" stroke-width="2.2" fill="none" stroke-linecap="round"/>
  <rect x="18" y="20" width="2.4" height="3" rx="0.8" fill="${C.C}"/><rect x="22" y="20" width="2.4" height="3" rx="0.8" fill="${C.C}"/>
  <rect x="27" y="20" width="2.4" height="3" rx="0.8" fill="${C.C}"/><rect x="31" y="20" width="2.4" height="3" rx="0.8" fill="${C.C}"/>
  <rect x="16" y="5" width="16" height="16" rx="2.6" fill="${C.Y}"/>
  <rect x="18" y="7" width="12" height="12" rx="2.4" fill="${C.P}"/>
  <rect x="20" y="9" width="2" height="2" fill="${C.R}"/><rect x="25" y="8.6" width="2" height="2" fill="${C.R}"/>
  <rect x="22" y="13" width="2" height="2" fill="${C.R}"/><rect x="19.6" y="16" width="2" height="2" fill="${C.R}"/>
  <rect x="25" y="15.6" width="2" height="2" fill="${C.R}"/>
  <path d="M28.4 12 L29.4 7.4 L32.6 10.6 L35.4 10.6 L38.4 7.4 L39.4 12 Q40.6 14 40.4 17 Q40 21.4 34 21.4 Q28 21.4 27.6 17 Q27.4 14 28.4 12 Z" fill="${C.C}"/>
  <rect x="30" y="13" width="2" height="2" fill="${C.N}"/><rect x="36" y="13" width="2" height="2" fill="${C.N}"/>
  <rect x="28.6" y="16.4" width="2" height="2" fill="${C.P}"/><rect x="37.4" y="16.4" width="2" height="2" fill="${C.P}"/>
  <path d="M31.6 17 L36.4 17 L36.4 18 Q35.2 19.4 34 18.4 Q32.8 19.4 31.6 18 Z" fill="${C.N}"/>
 </svg>`},
 // 14 — a calm capybara with a tangerine on its head in warm water
 {title:'Капибара с мандарином на голове', family:ANI, direction:'Уютный сюрреализм', subject:'спокойная капибара с мандарином на голове в тёплой воде',
  source:M, reference:'мем «спокойная капибара» — капибара в источнике с мандарином', forLevel:61, cells:1368, colours:8,
  svg:`<svg viewBox="0 0 38 36" xmlns="http://www.w3.org/2000/svg">
  <rect width="38" height="36" fill="${C.S}"/>
  <path d="M0 14 Q8 9.6 17 12.4 Q27 8.6 38 12.6 L38 22 L0 22 Z" fill="${C.M}"/>
  <ellipse cx="25" cy="22" rx="11.4" ry="7" fill="${C.A}"/>
  <path d="M21 10 Q14 8.6 9.4 9 Q6.4 9.4 6 12 L5.8 15.6 Q6 18 8.4 18.4 Q12 19.6 16 20.4 L24 22 L25 14 Q24.6 10.6 21 10 Z" fill="${C.A}"/>
  <path d="M5.8 12 Q6 9.4 8.6 9.2 L9.6 9.2 L9.6 15.6 L5.8 15.6 Z" fill="${C.E}"/>
  <rect x="6.6" y="10.6" width="2" height="2" fill="${C.D}"/>
  <path d="M8 16.6 Q10.6 17.6 13 17.2" stroke="${C.E}" stroke-width="1.4" fill="none"/>
  <circle cx="20.6" cy="9.4" r="1.9" fill="${C.E}"/>
  <rect x="14" y="11" width="3" height="2" rx="0.8" fill="${C.D}"/>
  <circle cx="15" cy="5" r="3.6" fill="${C.O}"/>
  <ellipse cx="17" cy="1.4" rx="2.2" ry="1.1" transform="rotate(-20 17 1.4)" fill="${C.M}"/>
  <rect y="22" width="38" height="14" fill="${C.B}"/>
  <ellipse cx="7" cy="25" rx="4.6" ry="1.2" fill="${C.C}"/><ellipse cx="31" cy="27" rx="5" ry="1.2" fill="${C.C}"/>
  <ellipse cx="17" cy="31" rx="6" ry="1.2" fill="${C.C}"/>
 </svg>`},
 // 15 — Moo Deng: a wet baby pygmy hippo with its mouth wide open
 {title:'Му Дэнг', family:ANI, direction:'Театр эмоций', subject:'мокрый детёныш карликового бегемота с открытым ртом',
  source:M, reference:'Moo Deng — Moo Deng', forLevel:61, cells:1296, colours:8,
  svg:`<svg viewBox="0 0 36 36" xmlns="http://www.w3.org/2000/svg">
  <rect width="36" height="36" fill="${C.L}"/>
  <path d="M0 30 Q18 27.4 36 30 L36 36 L0 36 Z" fill="${C.G}"/>
  <ellipse cx="11.6" cy="32.4" rx="3.4" ry="2.2" fill="${C.D}"/><ellipse cx="24.4" cy="32.4" rx="3.4" ry="2.2" fill="${C.D}"/>
  <ellipse cx="18" cy="27" rx="12" ry="6.4" fill="${C.D}"/>
  <circle cx="8.4" cy="6.6" r="2.6" fill="${C.D}"/><circle cx="27.6" cy="6.6" r="2.6" fill="${C.D}"/>
  <rect x="7.4" y="5.6" width="2" height="2" fill="${C.K}"/><rect x="26.6" y="5.6" width="2" height="2" fill="${C.K}"/>
  <ellipse cx="18" cy="15" rx="12.4" ry="10.4" fill="${C.D}"/>
  <ellipse cx="18" cy="19.4" rx="9.6" ry="6.6" fill="${C.K}"/>
  <path d="M11.6 17.6 Q18 15.4 24.4 17.6 Q24 25.4 18 25.6 Q12 25.4 11.6 17.6 Z" fill="${C.R}"/>
  <ellipse cx="18" cy="23.2" rx="4" ry="2" fill="${C.P}"/>
  <rect x="13" y="18" width="2" height="2" fill="${C.C}"/><rect x="21" y="18" width="2" height="2" fill="${C.C}"/>
  <rect x="14" y="14" width="2" height="2" fill="${C.D}"/><rect x="20" y="14" width="2" height="2" fill="${C.D}"/>
  <rect x="9" y="9" width="3" height="3" rx="0.8" fill="${C.C}"/><rect x="24" y="9" width="3" height="3" rx="0.8" fill="${C.C}"/>
  <rect x="10" y="10" width="2" height="2" fill="${C.D}"/><rect x="24" y="10" width="2" height="2" fill="${C.D}"/>
  <rect x="13" y="6" width="4" height="2" rx="1" fill="${C.S}"/><rect x="8" y="24" width="2" height="3" rx="1" fill="${C.S}"/>
  <rect x="26" y="24" width="3" height="2" rx="1" fill="${C.S}"/>
 </svg>`},
]};
