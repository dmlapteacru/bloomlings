// A small JSON Schema (draft 2020-12) checker for the round formats of spec 006 (no dependency): the keywords those
// schemas use — type, const, enum, pattern, minimum, maximum, minLength, minItems, maxItems, uniqueItems, items,
// properties, required, additionalProperties: false, $ref to #/$defs, oneOf, format: date. An unknown keyword fails, so a
// schema change is not checked silently.
import fs from 'node:fs';import path from 'node:path';import {root} from './lib.mjs';

export const SCHEMAS=path.join(root,'specs/006-round-levels-in-game/contracts');
const cache=new Map();
export function schema(name){if(!cache.has(name))cache.set(name,JSON.parse(fs.readFileSync(path.join(SCHEMAS,name+'.schema.json'),'utf8')));return cache.get(name);}

const KNOWN=new Set(['$schema','$id','title','description','$defs','type','const','enum','pattern','minimum','maximum','minLength','minItems','maxItems','uniqueItems','items','properties','required','additionalProperties','$ref','oneOf','format']);
const typeOf=v=>v===null?'null':Array.isArray(v)?'array':Number.isInteger(v)?'integer':typeof v;
const same=(a,b)=>JSON.stringify(a)===JSON.stringify(b);

// errors of `value` against the named schema (empty when valid): ["/pods/3/count: above maximum 60", …]
export function validate(name,value){const root=schema(name),errors=[];
 const check=(s,v,at)=>{
  for(const k of Object.keys(s))if(!KNOWN.has(k))throw Error(`schema ${name}: keyword ${k} is not supported by tools/rounds/schema.mjs`);
  if(s.$ref){const m=/^#\/\$defs\/(.+)$/.exec(s.$ref);if(!m)throw Error(`schema ${name}: $ref ${s.$ref}`);check(root.$defs[m[1]],v,at);}
  if(s.oneOf){const ok=s.oneOf.filter(x=>{const before=errors.length;check(x,v,at);const pass=errors.length===before;errors.length=before;return pass;}).length;if(ok!==1)errors.push(`${at||'/'}: matches ${ok} of oneOf`);}
  if(s.type){const t=typeOf(v),want=[].concat(s.type);if(!want.includes(t)&&!(t==='integer'&&want.includes('number'))){errors.push(`${at||'/'}: ${t}, expected ${want.join('|')}`);return;}}
  if('const' in s&&!same(s.const,v))errors.push(`${at||'/'}: not ${JSON.stringify(s.const)}`);
  if(s.enum&&!s.enum.some(e=>same(e,v)))errors.push(`${at||'/'}: ${JSON.stringify(v)} not in ${JSON.stringify(s.enum)}`);
  if(typeof v==='string'){if(s.pattern&&!new RegExp(s.pattern,'u').test(v))errors.push(`${at||'/'}: ${JSON.stringify(v.slice(0,40))} does not match ${s.pattern}`);
   if(s.minLength!=null&&[...v].length<s.minLength)errors.push(`${at||'/'}: shorter than ${s.minLength}`);
   if(s.format==='date'&&!/^\d{4}-\d{2}-\d{2}$/.test(v))errors.push(`${at||'/'}: not a date`);}
  if(typeof v==='number'){if(s.minimum!=null&&v<s.minimum)errors.push(`${at||'/'}: below minimum ${s.minimum}`);if(s.maximum!=null&&v>s.maximum)errors.push(`${at||'/'}: above maximum ${s.maximum}`);}
  if(Array.isArray(v)){if(s.minItems!=null&&v.length<s.minItems)errors.push(`${at||'/'}: fewer than ${s.minItems} items`);if(s.maxItems!=null&&v.length>s.maxItems)errors.push(`${at||'/'}: more than ${s.maxItems} items`);
   if(s.uniqueItems&&new Set(v.map(x=>JSON.stringify(x))).size!==v.length)errors.push(`${at||'/'}: items not unique`);
   if(s.items)v.forEach((x,i)=>check(s.items,x,`${at}/${i}`));}
  if(v&&typeof v==='object'&&!Array.isArray(v)){
   for(const k of s.required||[])if(!(k in v))errors.push(`${at||'/'}: missing ${k}`);
   for(const [k,x] of Object.entries(v)){if(s.properties&&k in s.properties)check(s.properties[k],x,`${at}/${k}`);else if(s.additionalProperties===false)errors.push(`${at||'/'}: unexpected ${k}`);}}};
 check(root,value,'');return errors;}
