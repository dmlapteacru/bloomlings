/* The fast twin of tools/rounds/engine/idle-rules.cjs (tools/rounds/perf/README.md): the search adapter on the fast rules
 * and timeline, the same code. The player waits for ALL work to settle between choices. */
const R=require('./fast-rules.cjs'),{Timeline}=require('./fast-timeline.cjs');
function step(state,action){const t=new Timeline(state),result=t.commit(action);if(!result.accepted)return {...result,state};t.advance(1e9);return {...result,state:t.state};}
module.exports={...R,step};
