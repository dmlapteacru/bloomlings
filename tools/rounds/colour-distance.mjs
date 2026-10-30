// CIEDE2000 between two round colours (sRGB → CIELAB, D65), the same formula as the game's ColorScience and
// analysis/readability.mjs; used where the tools must keep two colours apart on the board (bridge scene strips).
import {createRequire} from 'node:module';
const req=createRequire(import.meta.url),P=req('./engine/palette.cjs'),PAL=P.palette('normal');
const lin=c=>c<=0.04045?c/12.92:((c+0.055)/1.055)**2.4,srgb=c=>c<=0.0031308?12.92*c:1.055*c**(1/2.4)-0.055,clamp=x=>Math.min(1,Math.max(0,x));
const hex=h=>[1,3,5].map(i=>parseInt(h.slice(i,i+2),16)/255);
const F=t=>t>0.008856?Math.cbrt(t):7.787*t+16/116;
const lab=rgb=>{const [r,g,b]=rgb.map(lin),x=(0.4124564*r+0.3575761*g+0.1804375*b)/0.95047,y=0.2126729*r+0.7151522*g+0.0721750*b,z=(0.0193339*r+0.1191920*g+0.9503041*b)/1.08883;return [116*F(y)-16,500*(F(x)-F(y)),200*(F(y)-F(z))];};
const rad=d=>d*Math.PI/180,hue=(b,a)=>{if(a===0&&b===0)return 0;const h=Math.atan2(b,a)*180/Math.PI;return h<0?h+360:h;};
export function de2000([L1,a1,b1],[L2,a2,b2]){const c1=Math.hypot(a1,b1),c2=Math.hypot(a2,b2),cm=(c1+c2)/2,g=0.5*(1-Math.sqrt(cm**7/(cm**7+25**7)));
 const ap1=(1+g)*a1,ap2=(1+g)*a2,cp1=Math.hypot(ap1,b1),cp2=Math.hypot(ap2,b2),hp1=hue(b1,ap1),hp2=hue(b2,ap2),dL=L2-L1,dC=cp2-cp1;
 let dh=0;if(cp1*cp2!==0)dh=Math.abs(hp2-hp1)<=180?hp2-hp1:(hp2-hp1>180?hp2-hp1-360:hp2-hp1+360);
 const dH=2*Math.sqrt(cp1*cp2)*Math.sin(rad(dh/2)),Lm=(L1+L2)/2,Cm=(cp1+cp2)/2;
 let hm;if(cp1*cp2===0)hm=hp1+hp2;else if(Math.abs(hp1-hp2)<=180)hm=(hp1+hp2)/2;else hm=hp1+hp2<360?(hp1+hp2+360)/2:(hp1+hp2-360)/2;
 const T=1-0.17*Math.cos(rad(hm-30))+0.24*Math.cos(rad(2*hm))+0.32*Math.cos(rad(3*hm+6))-0.20*Math.cos(rad(4*hm-63));
 const dTheta=30*Math.exp(-(((hm-275)/25)**2)),Rc=2*Math.sqrt(Cm**7/(Cm**7+25**7)),Sl=1+0.015*(Lm-50)**2/Math.sqrt(20+(Lm-50)**2),Sc=1+0.045*Cm,Sh=1+0.015*Cm*T,Rt=-Math.sin(rad(2*dTheta))*Rc;
 return Math.sqrt((dL/Sl)**2+(dC/Sc)**2+(dH/Sh)**2+Rt*(dC/Sc)*(dH/Sh));}
// the distance between two material codes under normal vision
export const deltaE=(a,b)=>de2000(lab(hex(PAL[a].color)),lab(hex(PAL[b].color)));
// the distance between a material code and a colour given as #RRGGBB (a split's gap strip in the board's colour, 2026-10-29)
export const deltaEHex=(a,h)=>de2000(lab(hex(PAL[a].color)),lab(hex(h)));
