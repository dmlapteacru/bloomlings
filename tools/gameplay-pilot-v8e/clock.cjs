/* Capture the PREVIOUS activity state: a visibility return never charges hidden
 * dwell. The UI consumes slices through the same timer for work and telemetry. */
(function(root,factory){const api=factory();if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.BloomPilotClock=api;})(globalThis,function(){
 class ActiveClock{
  constructor(now=0){this.last=now;this.running=false;}
  sample(now){if(!Number.isFinite(now)||now<this.last)throw Error('Clock regressed');const dt=this.running?now-this.last:0;this.last=now;return dt;}
  setRunning(running,now){const dt=this.sample(now);this.running=Boolean(running);return dt;}
 }
 return {ActiveClock};
});
