'use strict';
const {createEconomyService} = require('./service.js');
const {createUgsStore} = require('./ugs-store.js');
const {EconomyError} = require('./core.js');
module.exports = async ({params, context}) => {
  try {
    if (!params || !context?.playerId || params.expectedOwnerId !== context.playerId)
      return {ok:false, code:'owner-mismatch', consumed:false, retryable:false, state:null};
    if (Object.keys(params).some(k=>!['action','expectedOwnerId','operationJson','installationId'].includes(k)))
      return {ok:false,code:'invalid-parameters',consumed:false,retryable:false,state:null};
    if (!['read','execute'].includes(params.action)) return {ok:false, code:'invalid-action', consumed:false, retryable:false, state:null};
    let operation = null;
    if (params.action === 'execute') {
      if (typeof params.operationJson !== 'string' || Buffer.byteLength(params.operationJson) > 4096)
        return {ok:false, code:'invalid-operation', consumed:false, retryable:false, state:null};
      try { operation = JSON.parse(params.operationJson); }
      catch { return {ok:false, code:'invalid-operation', consumed:false, retryable:false, state:null}; }
      if (operation === null) return {ok:false, code:'invalid-operation', consumed:false, retryable:false, state:null};
    }
    return await createEconomyService(createUgsStore(context))(context.playerId, operation, params.installationId ?? null);
  } catch (error) {
    return {ok:false, code: error instanceof EconomyError ? error.code : 'temporarily-unavailable', consumed:false, retryable: !(error instanceof EconomyError), state:null};
  }
};
module.exports.params = {
  action: {type:'String', required:true},
  expectedOwnerId: {type:'String', required:true},
  operationJson: {type:'String', required:false},
  installationId: {type:'String', required:false},
};
