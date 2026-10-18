// Entry point for the four accepted V2 families. The game consumes 2D sprites only.
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
export const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');
const here=path.dirname(fileURLToPath(import.meta.url));
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const args=process.argv.slice(2),at=args.indexOf('--only'),families=at>=0?[args[at+1]]:['sprig','bloom','drop','twig'];
 if(families.some(f=>!['sprig','bloom','drop','twig'].includes(f)))throw Error('Expected --only sprig|bloom|drop|twig');
 for(const family of families){const r=spawnSync(process.execPath,[path.join(here,'bake-'+family+'-v2.mjs')],{stdio:'inherit'});if(r.status!==0)process.exit(r.status??1);}
 const home=spawnSync(process.execPath,[path.join(here,'bake-home-heroes.mjs')],{stdio:'inherit'});if(home.status!==0)process.exit(home.status??1);
 const eyewear=spawnSync(process.execPath,[path.join(here,'bake-eyewear-v1.mjs')],{stdio:'inherit'});if(eyewear.status!==0)process.exit(eyewear.status??1);
 const extras=spawnSync(process.execPath,[path.join(here,'bake-bloom-extras-v1.mjs')],{stdio:'inherit'});process.exit(extras.status??1);
}
