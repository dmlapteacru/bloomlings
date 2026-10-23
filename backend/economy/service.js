'use strict';
const core = require('./core.js');
const catalog = require('./catalog.json');

// Atomic store contract: load returns protected state + a nonempty lock; replace must CAS that EXACT lock.
// No create/upsert method is exposed. Missing wallets must be provisioned by a separately verified bootstrap.
function createEconomyService(store, options = {}) {
  const clock = options.clock || Date.now;
  const attempts = options.attempts || 4;
  return async function handle(ownerId, operation = null, installationId = null) {
    try {
      if (!core.id(ownerId)) core.fail('unauthenticated');
      if (installationId !== null && !core.id(installationId)) core.fail('invalid-installation');
      const fingerprint = operation === null ? null : core.validateOperation(operation);
      for (let attempt = 0; attempt < attempts; attempt++) {
        const stored = await store.load(ownerId);
        if (!stored) core.fail('needs-initialization');
        core.validateState(stored.value, ownerId, catalog);
        if (typeof stored.writeLock !== 'string' || !stored.writeLock.length) core.fail('missing-write-lock');
        const state = stored.value;
        const view = () => core.snapshot(state);
        const now = clock();
        if (!core.integer(now, 0, 8640000000000000)) core.fail('invalid-server-clock');
        const serverUtcDate = new Date(now).toISOString().slice(0,10);
        if (operation === null) return {ok: true, code: 'loaded', serverUtcDate, consumed: false, retryable: false, state: view(), catalogVersion: catalog.version, catalog: {cosmetics:catalog.cosmetics,boosters:catalog.boosters}, nextSequence: installationId === null ? null : core.nextSequenceOf(state,installationId)};
        const previous = core.prior(state, operation, fingerprint);
        if (previous) return {...previous, consumed: true, retryable: false, state: view(), catalogVersion: catalog.version, nextSequence: core.nextSequenceOf(state,operation.installationId)};
        if (operation.kind !== 'cancel_purchase' && operation.catalogVersion !== catalog.version) core.fail('catalog-changed');
        let ticket = null;
        if (operation.ticketId !== undefined) ticket = await store.loadTicket(ownerId, operation.ticketId);
        const {next, result} = core.execute(state, operation, catalog, ticket, now);
        if (await store.replace(ownerId, stored.writeLock, next))
          return {...result, consumed: true, retryable: false, replayed: false, state: core.snapshot(next), catalogVersion: catalog.version, nextSequence: core.nextSequenceOf(next,operation.installationId)};
        // Another request won. Re-read and re-evaluate ownership, replay status, balance and eligibility.
      }
      core.fail('contention', true);
    } catch (error) {
      if (error instanceof core.EconomyError) return {ok: false, code: error.code, consumed: false, retryable: error.retryable, state: null, catalogVersion: catalog.version};
      // Includes an ambiguous write timeout: the client MUST retain its exact operation and retry it.
      return {ok: false, code: 'temporarily-unavailable', consumed: false, retryable: true, state: null};
    }
  };
}
module.exports = {createEconomyService};
