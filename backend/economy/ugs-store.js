'use strict';
const {DataApi} = require('@unity-services/cloud-save-1.4');
const {id, fail} = require('./core.js');
const crypto = require('crypto');
const KEY = 'economy_v1';
function ticketKey(ticketId) {
  if (!id(ticketId)) fail('invalid-ticket');
  // No sanitization/truncation collisions: issuer uses this same digest for the protected authorization key.
  return 'economy_auth_' + crypto.createHash('sha256').update(ticketId).digest('hex');
}
function createUgsStore(context) {
  if (!context || !id(context.playerId) || !context.projectId || !context.serviceToken) fail('unauthenticated');
  const api = new DataApi({accessToken: context.serviceToken});
  const assertOwner = owner => { if (owner !== context.playerId) fail('owner-mismatch'); };
  return {
    async load(owner) {
      assertOwner(owner);
      const response = await api.getProtectedItems(context.projectId, owner, [KEY]);
      const item = response.data.results.find(i => i.key === KEY);
      return item ? {value: item.value, writeLock: item.writeLock} : null;
    },
    async loadTicket(owner, ticketId) {
      assertOwner(owner);
      const key = ticketKey(ticketId);
      const response = await api.getProtectedItems(context.projectId, owner, [key]);
      const item = response.data.results.find(i => i.key === key);
      return item ? item.value : null;
    },
    async replace(owner, writeLock, value) {
      assertOwner(owner);
      if (typeof writeLock !== 'string' || !writeLock.length) fail('missing-write-lock');
      if (value.ownerId !== owner) fail('owner-mismatch');
      try {
        await api.setProtectedItem(context.projectId, owner, {key: KEY, value, writeLock});
        return true;
      } catch (error) {
        if (error?.response?.status === 409) return false;
        throw error;
      }
    }
  };
}
module.exports = {KEY, ticketKey, createUgsStore};
