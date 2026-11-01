// The share model of the calibration (README.md here): a binomial logistic regression (IRLS, a light ridge on
// standardized columns) of the quick share on picture features, queue parameters and mechanic doses. Shared by fit.mjs
// (fitting, cross-validation, the effects) and plan.mjs (the inverse). Pure functions, no engine.
export const sigmoid=x=>1/(1+Math.exp(-x)),logit=p=>Math.log(p/(1-p));
// the feature sets: name → value of a row (row = a measured configuration joined with its picture's features f)
const lin={
 // picture
 logN:r=>r.f.logN,colours:r=>r.f.colours,hiddenShare:r=>r.f.hiddenShare,visible:r=>r.f.visible,openShare:r=>r.f.openShare,shallowShare:r=>r.f.shallowShare,
 layers:r=>r.f.layers/10,deepWork:r=>r.f.deepWork,meanFirst:r=>r.f.meanFirst,sdFirst:r=>r.f.sdFirst,meanDepth:r=>r.f.meanDepth,volCV:r=>r.f.volCV,maxShare:r=>r.f.maxShare,smallColours:r=>r.f.smallColours,
 // queue parameters (controllable)
 t:r=>r.t,t2:r=>r.t*r.t,sigma:r=>r.sigma,cols:r=>r.cols,podMean:r=>r.f.N/r.P/10,hold:r=>r.hold,
 // mechanics (controllable doses)
 linkShare:r=>r.qf.linkShare,linkTrap:r=>r.qf.linkShare*(r.linkStyle==='trap'?1:0),hidden:r=>r.qf.hidden/4,hiddenRun:r=>r.qf.hidden/4*(r.hiddenStyle==='run'?1:0),
 cellsShare:r=>r.qf.cellsHidden/r.f.N*10,latch:r=>r.qf.latches,accents:r=>r.accentsPainted?1:0,
 // structure of the built queue (no simulation)
 rankCorr:r=>r.qf.rankCorr,deepTop:r=>r.qf.deepTop,frontTop:r=>r.qf.frontTop/r.cols,
};
const PIC=['logN','colours','hiddenShare','visible','openShare','shallowShare','layers','deepWork','meanFirst','sdFirst','meanDepth','volCV','maxShare','smallColours'];
const MECH=['linkShare','linkTrap','hidden','hiddenRun','cellsShare','latch','accents'];
const inter=(a,b)=>[`${a}*${b}`,r=>lin[a](r)*lin[b](r)];
export const SPECS={
 // picture only (how much the picture alone tells)
 picture:{terms:[...PIC]},
 // the controllable model: picture + queue parameters + doses, t interacting with the picture's access structure
 params:{terms:[...PIC,'t','t2','sigma','cols','podMean','hold',...MECH],inter:[['t','hiddenShare'],['t','openShare'],['t','layers'],['t','logN'],['t','visible'],['t','deepWork'],['t','meanFirst'],['t','shallowShare'],['t2','hiddenShare'],['t2','openShare'],['t','sigma'],['t','podMean'],['linkShare','t'],['linkTrap','t'],['hidden','t'],['cellsShare','t'],['t','cols']]},
 // + the structure of the built queue (rank correlation with the access order, deep pods on top): still no simulation
 structure:{terms:[...PIC,'t','t2','sigma','cols','podMean','hold',...MECH,'rankCorr','deepTop','frontTop'],inter:[['t','hiddenShare'],['t','openShare'],['t','layers'],['t','logN'],['t','visible'],['t','deepWork'],['t','meanFirst'],['t2','hiddenShare'],['t2','openShare'],['t','podMean'],['linkShare','t'],['linkTrap','t'],['rankCorr','hiddenShare'],['rankCorr','openShare'],['deepTop','hiddenShare'],['rankCorr','layers'],['deepTop','podMean']]},
};
export function featuresOf(spec){const s=SPECS[spec]||spec;return [...s.terms.map(n=>[n,lin[n]]),...(s.inter||[]).map(([a,b])=>inter(a,b))];}
// Gaussian elimination with partial pivoting
function solve(A,b){const n=b.length,M=A.map((r,i)=>[...r,b[i]]);for(let c=0;c<n;c++){let p=c;for(let r=c+1;r<n;r++)if(Math.abs(M[r][c])>Math.abs(M[p][c]))p=r;[M[c],M[p]]=[M[p],M[c]];const d=M[c][c]||1e-12;
 for(let r=0;r<n;r++){if(r===c)continue;const k=M[r][c]/d;if(k)for(let j=c;j<=n;j++)M[r][j]-=k*M[c][j];}}return M.map((r,i)=>r[n]/(r[i]||1e-12));}
// fit: rows with wins/games; returns {spec, names, mu, sd, beta (on standardized columns, beta[0] the intercept)}
export function fit(rows,spec,{lambda=1,iters=30}={}){const F=featuresOf(spec),names=F.map(x=>x[0]),raw=rows.map(r=>F.map(([,fn])=>fn(r)));
 const mu=names.map((_,j)=>raw.reduce((s,x)=>s+x[j],0)/raw.length),sd=names.map((_,j)=>Math.sqrt(raw.reduce((s,x)=>s+(x[j]-mu[j])**2,0)/raw.length)||1);
 const X=raw.map(x=>[1,...x.map((v,j)=>(v-mu[j])/sd[j])]),k=X[0].length;let beta=new Array(k).fill(0);
 for(let it=0;it<iters;it++){const A=Array.from({length:k},()=>new Array(k).fill(0)),b=new Array(k).fill(0);
  rows.forEach((r,i)=>{const x=X[i],eta=x.reduce((s,v,j)=>s+v*beta[j],0),p=Math.min(1-1e-6,Math.max(1e-6,sigmoid(eta))),n=r.games,w=n*p*(1-p),z=eta+(r.wins/n-p)/(p*(1-p));
   for(let a=0;a<k;a++){if(!x[a])continue;b[a]+=w*x[a]*z;for(let c=a;c<k;c++)A[a][c]+=w*x[a]*x[c];}});
  for(let a=0;a<k;a++)for(let c=0;c<a;c++)A[a][c]=A[c][a];for(let a=1;a<k;a++)A[a][a]+=lambda;
  const nb=solve(A,b),dm=Math.max(...nb.map((v,j)=>Math.abs(v-beta[j])));beta=nb;if(dm<1e-7)break;}
 return {spec:typeof spec==='string'?spec:'custom',names,mu,sd,beta};}
export function eta(model,row){const F=featuresOf(model.spec);let e=model.beta[0];F.forEach(([,fn],j)=>{e+=model.beta[j+1]*(fn(row)-model.mu[j])/model.sd[j];});return e;}
export const predict=(model,row)=>sigmoid(eta(model,row));
// the raw-unit coefficient of a term (d eta / d term), for the report
export const rawCoef=(model,name)=>{const j=model.names.indexOf(name);return j<0?null:model.beta[j+1]/model.sd[j];};
