/* Search adapter: player waits for ALL work to settle between choices.
 * Sound winning witnesses for v6, NOT a complete solver of timed strategies.
 * A failed search here does not prove a live position irrecoverable. */
const R=require('./rules.cjs'),{Timeline}=require('./timeline.cjs');
function step(state,action){const t=new Timeline(state),result=t.commit(action);if(!result.accepted)return {...result,state};t.advance(1e9);return {...result,state:t.state};}
module.exports={...R,step};
