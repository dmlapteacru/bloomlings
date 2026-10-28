// Bridge scenes for wave L21-L50 (17 §8, the owner's scene design of 2026-10-28): one per bridge position, picked from
// the scene generator's drafts (scenes-L00NN.mjs, evidence in the .json next to each) so a subject comes back no sooner
// than 10 levels later and the Normal shares stay near the median (L26 68 %, L29 58 %, L41 77 %, L44 81 %); L23 is the drawn mushroom glade (batch-0120-scenes.mjs #1, the owner's choice).
import mush from './batch-0120-scenes.mjs';
import s26 from './scenes-L0026.mjs';import s29 from './scenes-L0029.mjs';import s33 from './scenes-L0033.mjs';
import s35 from './scenes-L0035.mjs';import s41 from './scenes-L0041.mjs';import s44 from './scenes-L0044.mjs';
import s48 from './scenes-L0048.mjs';import s50 from './scenes-L0050.mjs';
const pick=(m,title)=>{const p=m.default?m.default:m;const f=p.pictures.find(x=>x.title===title);if(!f)throw Error('no '+title);return f;};
export default {theme:'мосты волны L21–L50',pictures:[
 pick(mush,'Грибная поляна за ручьём'),
 pick(s26,'Вишенки за мостом'),pick(s29,'Жёлудь за мостом'),pick(s33,'Медальон за мостом'),
 pick(s35,'Печенье с шоколадом за мостом'),pick(s41,'Вишенки за мостом'),pick(s44,'Жёлудь за мостом'),
 pick(s48,'Компас за мостом'),pick(s50,'Клубок с вязанием за мостом')]};
