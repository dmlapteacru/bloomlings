'use strict';
const crypto = require('crypto');
const KINDS = ['extraSlot', 'shuffle', 'return', 'bloomBurst'];
const LIMITS = Object.freeze({ petals: 2147483647, charges: 999999, installations: 32, receipts: 128, owned: 4096, bytes: 200000 });
class EconomyError extends Error {
  constructor(code, retryable = false) { super(code); this.code = code; this.retryable = retryable; }
}
const fail = (code, retryable = false) => { throw new EconomyError(code, retryable); };
const plain = x => x !== null && typeof x === 'object' && !Array.isArray(x) && (Object.getPrototypeOf(x) === Object.prototype || Object.getPrototypeOf(x) === null);
const integer = (x, min = 0, max = Number.MAX_SAFE_INTEGER) => Number.isSafeInteger(x) && x >= min && x <= max;
const id = x => typeof x === 'string' && /^[a-zA-Z0-9_.:-]{1,128}$/.test(x) && !['__proto__','constructor','prototype'].includes(x);
function exact(x, keys, code = 'invalid-operation') {
  if (!plain(x) || Object.keys(x).some(k => !keys.includes(k))) fail(code);
}
function canonical(x) {
  if (Array.isArray(x)) return '[' + x.map(canonical).join(',') + ']';
  if (plain(x)) return '{' + Object.keys(x).sort().map(k => JSON.stringify(k) + ':' + canonical(x[k])).join(',') + '}';
  return JSON.stringify(x);
}
const hash = x => crypto.createHash('sha256').update(canonical(x)).digest('hex');
const clone = x => JSON.parse(JSON.stringify(x));
const sequenceOf = (s, installation) => Object.prototype.hasOwnProperty.call(s.installations, installation) ? s.installations[installation] : 0;
const nextSequenceOf = (s, installation) => { const n=sequenceOf(s,installation); return n===Number.MAX_SAFE_INTEGER ? null : n+1; };
function emptyState(ownerId, catalog) {
  if (!id(ownerId)) fail('invalid-owner');
  return { schemaVersion: 1, ownerId, catalogVersion: catalog.version, revision: 0, petals: 0, highestCompletedLevel: 0,
    boosters: Object.fromEntries(KINDS.map(k => [k, 0])), owned: [], milestones: [], challengeDate: null, boosterAdDate: null, daily: {date: null, step: 0}, installations: {}, receipts: [] };
}
function validateState(s, ownerId, catalog) {
  const bad = () => fail('invalid-server-state');
  exact(s, ['schemaVersion','ownerId','catalogVersion','revision','petals','highestCompletedLevel','boosters','owned','milestones','daily','challengeDate','boosterAdDate','installations','receipts'], 'invalid-server-state');
  if (s.schemaVersion !== 1 || s.ownerId !== ownerId || !id(s.ownerId) || !id(s.catalogVersion)
    || !integer(s.revision) || !integer(s.petals, 0, LIMITS.petals) || !integer(s.highestCompletedLevel, 0, catalog.maxLevel)) bad();
  exact(s.boosters, KINDS, 'invalid-server-state');
  if (KINDS.some(k => !integer(s.boosters[k], 0, LIMITS.charges))) bad();
  if (!Array.isArray(s.owned) || s.owned.length > LIMITS.owned || s.owned.some(x => !id(x)) || new Set(s.owned).size !== s.owned.length) bad();
  if (!Array.isArray(s.milestones) || s.milestones.length > catalog.maxLevel || s.milestones.some(n => !integer(n, 1, s.highestCompletedLevel)) || new Set(s.milestones).size !== s.milestones.length) bad();
  for (const date of [s.challengeDate,s.boosterAdDate]) if (date !== null && (typeof date !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(date))) bad();
  exact(s.daily, ['date','step'], 'invalid-server-state');
  if (!integer(s.daily.step, 0, 5) || (s.daily.date === null ? s.daily.step !== 0 : !/^\d{4}-\d{2}-\d{2}$/.test(s.daily.date))) bad();
  if (!plain(s.installations) || Object.keys(s.installations).length > LIMITS.installations || Object.entries(s.installations).some(([k,v]) => !id(k) || !integer(v, 1))) bad();
  if (!Array.isArray(s.receipts) || s.receipts.length > LIMITS.receipts) bad();
  const seen = new Set();
  for (const r of s.receipts) {
    exact(r, ['installationId','sequence','hash','outcome'], 'invalid-server-state');
    if (!id(r.installationId) || !integer(r.sequence, 1, s.installations[r.installationId] || 0) || !/^[a-f0-9]{64}$/.test(r.hash)) bad();
    exact(r.outcome, ['ok','code','effects'], 'invalid-server-state');
    if (typeof r.outcome.ok !== 'boolean' || !id(r.outcome.code) || !plain(r.outcome.effects)) bad();
    const key = r.installationId + ':' + r.sequence;
    if (seen.has(key)) bad(); seen.add(key);
  }
  if (Buffer.byteLength(JSON.stringify(s)) > LIMITS.bytes) bad();
}
function validateOperation(op) {
  exact(op, ['installationId','sequence','catalogVersion','kind','itemId','boosterKind','level','step','ticketId','autoBuy']);
  if (!id(op.installationId) || !integer(op.sequence, 1) || !id(op.catalogVersion)) fail('invalid-operation');
  const fields = {buy_cosmetic: ['itemId'], buy_booster: ['boosterKind'], consume_booster: ['boosterKind','autoBuy'], claim_level: ['level','ticketId'], claim_milestone: ['level'], claim_daily: ['step','ticketId'], claim_challenge: ['ticketId'], claim_booster_ad: ['ticketId']};
  if (!Object.prototype.hasOwnProperty.call(fields, op.kind)) fail('invalid-operation');
  const common = ['installationId','sequence','catalogVersion','kind'];
  if (Object.keys(op).some(k => !common.concat(fields[op.kind]).includes(k))) fail('invalid-operation');
  if (op.kind === 'buy_cosmetic' && !id(op.itemId)) fail('invalid-operation');
  if (['buy_booster','consume_booster'].includes(op.kind) && !KINDS.includes(op.boosterKind)) fail('invalid-operation');
  if (['claim_level','claim_milestone'].includes(op.kind) && !integer(op.level, 1, 2500)) fail('invalid-operation');
  if (op.kind === 'claim_daily' && !integer(op.step, 1, 5)) fail('invalid-operation');
  if (op.autoBuy !== undefined && typeof op.autoBuy !== 'boolean') fail('invalid-operation');
  if (['claim_level','claim_challenge','claim_booster_ad'].includes(op.kind) && !id(op.ticketId)) fail('invalid-operation');
  if (op.ticketId !== undefined && !id(op.ticketId)) fail('invalid-operation');
  return hash(op);
}
function snapshot(s) {
  // Never return receipt/installation registries, server tickets or write locks to the client.
  return clone({ schemaVersion: s.schemaVersion, ownerId: s.ownerId, catalogVersion: s.catalogVersion, revision: s.revision,
    petals: s.petals, highestCompletedLevel: s.highestCompletedLevel, boosters: s.boosters, owned: s.owned, milestones: s.milestones, daily: s.daily, challengeDate: s.challengeDate, boosterAdDate: s.boosterAdDate });
}
function prior(s, op, fingerprint) {
  const high = sequenceOf(s, op.installationId);
  if (op.sequence <= high) {
    const receipt = s.receipts.find(r => r.installationId === op.installationId && r.sequence === op.sequence);
    if (!receipt) return {ok: false, code: 'receipt-expired', effects: {}, replayed: true};
    if (receipt.hash !== fingerprint) fail('sequence-reused');
    return {...clone(receipt.outcome), replayed: true};
  }
  if (op.sequence !== high + 1) fail('sequence-gap');
  if (!high && Object.keys(s.installations).length >= LIMITS.installations) fail('installation-limit');
  return null;
}
const outcome = (ok, code, effects = {}) => ({ok, code, effects});
function unlocked(s, booster, catalog) { return s.highestCompletedLevel + 1 >= catalog.boosters[booster].minLevel; }
function addPetals(s, amount) { if (!integer(amount, 0, LIMITS.petals) || s.petals > LIMITS.petals - amount) fail('balance-limit'); s.petals += amount; }
function addBooster(s, kind, amount) { if (!integer(amount, 0, LIMITS.charges) || s.boosters[kind] > LIMITS.charges - amount) fail('booster-limit'); s.boosters[kind] += amount; }
function own(s, item) {
  if (!s.owned.includes(item)) { if (s.owned.length >= LIMITS.owned) fail('inventory-limit'); s.owned.push(item); s.owned.sort(); }
}
function authorize(ticket, owner, kind, now) {
  // Authorizations are read ONLY with a service token from protected server data. This check does not trust a
  // client boolean, command hash or a claimed ad completion. Issuers must verify actual gameplay/SSV evidence.
  if (!plain(ticket) || ticket.ownerId !== owner || ticket.kind !== kind || !Number.isSafeInteger(ticket.expiresAt) || ticket.expiresAt <= now) return false;
  return true;
}
function apply(s, op, catalog, ticket, now) {
  if (op.kind === 'buy_cosmetic') {
    const item = Object.prototype.hasOwnProperty.call(catalog.cosmetics, op.itemId) ? catalog.cosmetics[op.itemId] : null;
    if (!item) return outcome(false, 'unknown-item');
    if (item.free || s.owned.includes(op.itemId)) return outcome(false, 'already-owned');
    if (s.highestCompletedLevel + 1 < item.minLevel) return outcome(false, 'item-locked');
    if (!item.free && item.price <= 0) return outcome(false, 'not-for-sale');
    const price = item.free ? 0 : item.price;
    if (s.petals < price) return outcome(false, 'insufficient-petals');
    own(s, op.itemId); s.petals -= price;
    return outcome(true, 'purchased', {petals: -price, itemId: op.itemId});
  }
  if (op.kind === 'buy_booster' || op.kind === 'consume_booster') {
    const kind = op.boosterKind, pack = catalog.boosters[kind];
    if (!unlocked(s, kind, catalog)) return outcome(false, 'booster-locked');
    if (op.kind === 'consume_booster') {
      let price = 0, added = 0;
      if (s.boosters[kind] <= 0) {
        if (!op.autoBuy) return outcome(false, 'no-charges');
        if (s.petals < pack.price) return outcome(false, 'insufficient-petals');
        price = pack.price; added = pack.quantity;
        addBooster(s, kind, added); s.petals -= price;
      }
      s.boosters[kind]--; return outcome(true, 'consumed', {petals: -price, boosterKind: kind, charges: added - 1});
    }
    if (s.petals < pack.price) return outcome(false, 'insufficient-petals');
    addBooster(s, kind, pack.quantity); s.petals -= pack.price;
    return outcome(true, 'purchased', {petals: -pack.price, boosterKind: kind, charges: pack.quantity});
  }
  if (op.kind === 'claim_level') {
    if (op.level <= s.highestCompletedLevel) return outcome(false, 'already-claimed');
    if (op.level !== s.highestCompletedLevel + 1) return outcome(false, 'level-gap');
    if (!authorize(ticket, s.ownerId, 'level', now) || ticket.level !== op.level || ticket.catalogVersion !== catalog.version
      || !['Normal','Hard','SuperHard'].includes(ticket.difficulty) || !integer(ticket.boostersUsed)) return outcome(false, 'reward-not-authorized');
    const reward = catalog.rewards;
    const petals = reward.base + (ticket.boostersUsed === 0 ? reward.clean : 0) + (ticket.difficulty === 'Hard' ? reward.hard : ticket.difficulty === 'SuperHard' ? reward.superHard : 0);
    addPetals(s, petals); s.highestCompletedLevel = op.level;
    // Unlocks happen at the start of the next level, as in Progression.Initialize in the client.
    for (const kind of KINDS) if (kind !== 'bloomBurst' && catalog.boosters[kind].minLevel === op.level + 1) addBooster(s, kind, reward.unlockGrant);
    let drop = null;
    if (op.level >= 20 && op.level % 50 !== 0 && (op.level - 20) % reward.dropEvery === 0) {
      const kinds = KINDS.filter(k => k !== 'bloomBurst' && unlocked(s, k, catalog));
      let earlier = 0;
      for (let l = 20; l < op.level; l += reward.dropEvery) if (l % 50 !== 0) earlier++;
      if (kinds.length) { drop = kinds[earlier % kinds.length]; addBooster(s, drop, 1); }
    }
    return outcome(true, 'rewarded', {petals, level: op.level, droppedBooster: drop});
  }
  if (op.kind === 'claim_milestone') {
    if (op.level > s.highestCompletedLevel) return outcome(false, 'level-not-completed');
    if (s.milestones.includes(op.level)) return outcome(false, 'already-claimed');
    const cadence = catalog.milestones.filter(c => op.level % c.every === 0).sort((a,b) => b.every-a.every)[0];
    if (!cadence) return outcome(false, 'not-a-milestone');
    addPetals(s, cadence.petals);
    for (const kind of KINDS) addBooster(s, kind, cadence.eachBooster);
    if (cadence.boosterCharges) {
      const kinds = KINDS.filter(k => unlocked(s,k,catalog));
      kinds.sort((a,b) => s.boosters[a]-s.boosters[b] || KINDS.indexOf(a)-KINDS.indexOf(b));
      addBooster(s, kinds[0] || 'extraSlot', cadence.boosterCharges);
    }
    let itemId = cadence.items.find(id => !s.owned.includes(id)) || null;
    if (!itemId && cadence.items.length) itemId = (cadence.tier === 'prestige' ? 'marker' : 'badge') + '.level_' + op.level;
    if (itemId) own(s, itemId);
    s.milestones.push(op.level); s.milestones.sort((a,b)=>a-b);
    return outcome(true, 'rewarded', {petals: cadence.petals, level: op.level, itemId});
  }
  if (op.kind === 'claim_challenge' || op.kind === 'claim_booster_ad') {
    const challenge = op.kind === 'claim_challenge';
    const today = new Date(now).toISOString().slice(0,10);
    // A verified offline challenge may finish after its puzzle's day. Its trusted ticket retains that date,
    // matching DailyChallengeService.Complete; free ad rewards remain bound to the current server day.
    if (challenge && (!authorize(ticket,s.ownerId,'challenge',now) || ticket.catalogVersion !== catalog.version
      || typeof ticket.date !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(ticket.date) || ticket.date > today)) return outcome(false,'reward-not-authorized');
    const date = challenge ? ticket.date : today;
    const previous = challenge ? s.challengeDate : s.boosterAdDate;
    if (previous !== null && previous >= date) return outcome(false, previous === date ? 'already-claimed' : 'older-date-already-superseded');
    if (challenge && s.highestCompletedLevel + 1 < catalog.challenge.minLevel) return outcome(false,'challenge-locked');
    const kinds = KINDS.filter(k => k !== 'bloomBurst' && unlocked(s,k,catalog));
    if (!challenge && !kinds.length) return outcome(false,'booster-locked');
    if (!challenge && (!authorize(ticket,s.ownerId,'free-booster-ad',now) || ticket.date !== date)) return outcome(false,'reward-not-authorized');
    if (challenge) {
      addPetals(s,catalog.challenge.petals); s.challengeDate=date;
      return outcome(true,'rewarded',{date,petals:catalog.challenge.petals});
    }
    kinds.sort((a,b) => s.boosters[a]-s.boosters[b] || KINDS.indexOf(a)-KINDS.indexOf(b));
    addBooster(s,kinds[0],1); s.boosterAdDate=date;
    return outcome(true,'rewarded',{date,boosterKind:kinds[0],charges:1});
  }
  if (op.kind === 'claim_daily') {
    if (s.highestCompletedLevel + 1 < catalog.daily.minLevel) return outcome(false, 'daily-locked');
    const date = new Date(now).toISOString().slice(0,10);
    if (s.daily.date !== null && date < s.daily.date) return outcome(false, 'server-date-regressed');
    const previous = s.daily.date === date ? s.daily.step : 0;
    if (op.step <= previous) return outcome(false, 'already-claimed');
    if (op.step !== previous + 1) return outcome(false, 'daily-step-gap');
    if (catalog.daily.ads[op.step-1] && (!authorize(ticket,s.ownerId,'daily-ad',now) || ticket.date !== date || ticket.step !== op.step)) return outcome(false, 'reward-not-authorized');
    const petals = catalog.daily.petals[op.step-1];
    addPetals(s, petals); s.daily = {date, step: op.step};
    return outcome(true, 'rewarded', {petals, date, step: op.step});
  }
  fail('invalid-operation');
}
function execute(state, op, catalog, ticket, now) {
  // Pure function: errors, retries and failed CAS never mutate the read snapshot.
  let next = clone(state);
  let result;
  try { result = apply(next, op, catalog, ticket, now); }
  catch (error) {
    if (!(error instanceof EconomyError) || !['balance-limit','booster-limit','inventory-limit'].includes(error.code)) throw error;
    // A failed bundle cannot leave a partial grant. Its terminal rejection is still remembered atomically.
    next = clone(state);
    result = outcome(false, error.code);
  }
  if (!integer(next.revision + 1)) fail('revision-limit');
  next.revision++;
  next.catalogVersion = catalog.version;
  next.installations[op.installationId] = op.sequence;
  next.receipts.push({installationId: op.installationId, sequence: op.sequence, hash: hash(op), outcome: result});
  next.receipts = next.receipts.slice(-LIMITS.receipts);
  if (Buffer.byteLength(JSON.stringify(next)) > LIMITS.bytes) fail('state-size-limit');
  return {next, result};
}
module.exports = {KINDS, LIMITS, EconomyError, fail, id, integer, clone, hash, sequenceOf, nextSequenceOf, emptyState, validateState, validateOperation, prior, snapshot, execute};
