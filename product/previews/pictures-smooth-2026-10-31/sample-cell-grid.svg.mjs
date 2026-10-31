import {C} from '../../../tools/rounds/svg-palette.mjs';
// 1 viewBox unit = 1 cell: big shapes as curves, features as cell-aligned blocks
export default {theme:'smooth sample', pictures:[
 {title:'Колобок на пеньке', cells:1225, colours:8, svg:`<svg viewBox="0 0 35 35" xmlns="http://www.w3.org/2000/svg">
  <rect width="35" height="35" fill="${C.S}"/>
  <ellipse cx="17.5" cy="37" rx="28" ry="9" fill="${C.G}"/>
  <path d="M8 26 Q7.5 32 6 34 L29 34 Q27.5 32 27 26 Z" fill="${C.E}"/>
  <ellipse cx="17.5" cy="26" rx="9.5" ry="2.6" fill="${C.A}"/>
  <ellipse cx="17.5" cy="26" rx="6" ry="1.4" fill="${C.C}"/>
  <circle cx="17.5" cy="15.5" r="10" fill="${C.O}"/>
  <circle cx="16.4" cy="14.4" r="9" fill="${C.Y}"/>
  <rect x="12" y="10" width="3" height="4" fill="${C.D}"/><rect x="19" y="10" width="3" height="4" fill="${C.D}"/>
  <rect x="9" y="15" width="3" height="2" fill="${C.K}"/><rect x="22" y="15" width="3" height="2" fill="${C.K}"/>
  <path d="M12 16 L22 16 L22 17 Q21 20 17 20 Q13 20 12 17 Z" fill="${C.R}"/>
 </svg>`},
 {title:'Чебурашка', cells:1332, colours:7, svg:`<svg viewBox="0 0 36 37" xmlns="http://www.w3.org/2000/svg">
  <rect width="36" height="37" fill="${C.S}"/>
  <ellipse cx="18" cy="39" rx="32" ry="7" fill="${C.G}"/>
  <circle cx="7" cy="10.5" r="7" fill="${C.E}"/><circle cx="7" cy="10.5" r="4.6" fill="${C.K}"/>
  <circle cx="29" cy="10.5" r="7" fill="${C.E}"/><circle cx="29" cy="10.5" r="4.6" fill="${C.K}"/>
  <ellipse cx="18" cy="28" rx="7" ry="6.4" fill="${C.E}"/>
  <ellipse cx="18" cy="29.4" rx="4.2" ry="4" fill="${C.C}"/>
  <ellipse cx="10.6" cy="28" rx="2.2" ry="3.4" fill="${C.E}" transform="rotate(25 10.6 28)"/>
  <ellipse cx="25.4" cy="28" rx="2.2" ry="3.4" fill="${C.E}" transform="rotate(-25 25.4 28)"/>
  <ellipse cx="14" cy="34.6" rx="3" ry="1.6" fill="${C.E}"/><ellipse cx="22" cy="34.6" rx="3" ry="1.6" fill="${C.E}"/>
  <circle cx="18" cy="15.5" r="9.2" fill="${C.E}"/>
  <ellipse cx="18" cy="17.4" rx="7" ry="6.4" fill="${C.C}"/>
  <rect x="13" y="13" width="3" height="4" fill="${C.D}"/><rect x="20" y="13" width="3" height="4" fill="${C.D}"/>
  <rect x="17" y="18" width="2" height="2" fill="${C.D}"/>
  <path d="M15 21 L21 21 Q20.5 23 18 23 Q15.5 23 15 21 Z" fill="${C.R}"/>
 </svg>`},
]};
