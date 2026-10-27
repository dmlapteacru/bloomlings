const ROOT=new URL('../../../',import.meta.url).pathname;
// The game's readability check (core/src/Bloomlings.Pipeline/Readability: ColorScience, ReadabilityCommand) on the 16
// round colours: CIEDE2000 between each pair under normal vision and the protanopia, deuteranopia and tritanopia
// simulations, the smallest of the four; a pair fails under MinDeltaE 10 (spec 006 FR-047: on cubes without icons, colour
// alone must tell materials apart). Then lists which placed round levels and approved pictures hold a failing pair.
// node tools/rounds/analysis/readability.mjs [--min 10]
const fs=await import('node:fs');const {createRequire}=await import('node:module');const req=createRequire(import.meta.url);
const P=req(ROOT+'tools/rounds/engine/palette.cjs');const codes=P.codes,pal=P.palette('normal');
const M={protan:[[0.152286,1.052583,-0.204868],[0.114503,0.786281,0.099216],[-0.003882,-0.048116,1.051998]],deutan:[[0.367322,0.860646,-0.227968],[0.280085,0.672501,0.047413],[-0.011820,0.042940,0.968881]],tritan:[[1.255528,-0.076749,-0.178779],[-0.078411,0.930809,0.147602],[0.004733,0.691367,0.303900]]};
const lin=c=>c<=0.04045?c/12.92:((c+0.055)/1.055)**2.4,srgb=c=>c<=0.0031308?12.92*c:1.055*c**(1/2.4)-0.055,clamp=x=>Math.min(1,Math.max(0,x));
const hex=h=>[1,3,5].map(i=>parseInt(h.slice(i,i+2),16)/255);
const sim=(rgb,v)=>{if(v==='normal')return rgb;const m=M[v],l=rgb.map(lin);return m.map(r=>srgb(clamp(r[0]*l[0]+r[1]*l[1]+r[2]*l[2])));};
const F=t=>t>0.008856?Math.cbrt(t):7.787*t+16/116;
const lab=rgb=>{const [r,g,b]=rgb.map(lin),x=(0.4124564*r+0.3575761*g+0.1804375*b)/0.95047,y=0.2126729*r+0.7151522*g+0.0721750*b,z=(0.0193339*r+0.1191920*g+0.9503041*b)/1.08883;return [116*F(y)-16,500*(F(x)-F(y)),200*(F(y)-F(z))];};
const rad=d=>d*Math.PI/180,hue=(b,a)=>{if(a===0&&b===0)return 0;const h=Math.atan2(b,a)*180/Math.PI;return h<0?h+360:h;};
function de2000([L1,a1,b1],[L2,a2,b2]){const c1=Math.hypot(a1,b1),c2=Math.hypot(a2,b2),cm=(c1+c2)/2,g=0.5*(1-Math.sqrt(cm**7/(cm**7+25**7)));
 const ap1=(1+g)*a1,ap2=(1+g)*a2,cp1=Math.hypot(ap1,b1),cp2=Math.hypot(ap2,b2),hp1=hue(b1,ap1),hp2=hue(b2,ap2),dL=L2-L1,dC=cp2-cp1;
 let dh=0;if(cp1*cp2!==0)dh=Math.abs(hp2-hp1)<=180?hp2-hp1:(hp2-hp1>180?hp2-hp1-360:hp2-hp1+360);
 const dH=2*Math.sqrt(cp1*cp2)*Math.sin(rad(dh/2)),Lm=(L1+L2)/2,Cm=(cp1+cp2)/2;
 let hm;if(cp1*cp2===0)hm=hp1+hp2;else if(Math.abs(hp1-hp2)<=180)hm=(hp1+hp2)/2;else hm=hp1+hp2<360?(hp1+hp2+360)/2:(hp1+hp2-360)/2;
 const T=1-0.17*Math.cos(rad(hm-30))+0.24*Math.cos(rad(2*hm))+0.32*Math.cos(rad(3*hm+6))-0.20*Math.cos(rad(4*hm-63));
 const dTheta=30*Math.exp(-(((hm-275)/25)**2)),Rc=2*Math.sqrt(Cm**7/(Cm**7+25**7)),Sl=1+0.015*(Lm-50)**2/Math.sqrt(20+(Lm-50)**2),Sc=1+0.045*Cm,Sh=1+0.015*Cm*T,Rt=-Math.sin(rad(2*dTheta))*Rc;
 return Math.sqrt((dL/Sl)**2+(dC/Sc)**2+(dH/Sh)**2+Rt*(dC/Sc)*(dH/Sh));}
const min=+(process.argv.includes('--min')?process.argv[process.argv.indexOf('--min')+1]:10);
const pairs=[];for(let i=0;i<codes.length;i++)for(let j=i+1;j<codes.length;j++){const a=codes[i],b=codes[j];let m=Infinity,worst='';
 for(const v of ['normal','protan','deutan','tritan']){const d=de2000(lab(sim(hex(pal[a].color),v)),lab(sim(hex(pal[b].color),v)));if(d<m){m=d;worst=v;}}pairs.push({a,b,min:Math.round(m*10)/10,worst});}
const fail=pairs.filter(p=>p.min<min).sort((x,y)=>x.min-y.min);
console.log(`пар ${pairs.length}, не проходят (min ΔE2000 < ${min}): ${fail.length}`);
for(const p of fail)console.log(`  ${p.a} ${pal[p.a].name} / ${p.b} ${pal[p.b].name}: ${p.min} (${p.worst})`);
const has=(rows,p)=>{const s=rows.join('');return s.includes(p.a)&&s.includes(p.b);};
const lv=[];for(const f of fs.readdirSync(ROOT+'content/rounds/levels').filter(f=>/^L\d+\.json$/.test(f)).sort()){const L=JSON.parse(fs.readFileSync(ROOT+'content/rounds/levels/'+f));const bad=fail.filter(p=>has(L.definition.rowsBottomFirst,p));if(bad.length)lv.push(`${f.slice(0,5)} ${L.picture}: ${bad.map(p=>p.a+'/'+p.b).join(', ')}`);}
console.log(`уровни с такой парой: ${lv.length}`);lv.forEach(x=>console.log('  '+x));
const man=JSON.parse(fs.readFileSync(ROOT+'content/rounds/pictures/manifest.json'));let n=0,k=0;for(const e of man.pictures.filter(e=>e.status==='approved')){const p=JSON.parse(fs.readFileSync(ROOT+`content/rounds/pictures/${e.id}.json`));n++;if(fail.some(q=>has(p.rows,q)))k++;}
console.log(`утверждённых картинок с такой парой: ${k} из ${n}`);
