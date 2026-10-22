'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const cp = require('node:child_process');
const Module = require('node:module');
const core = require('../economy/core.js');
const catalog = require('../economy/catalog.json');
const {createEconomyService} = require('../economy/service.js');
const NOW = Date.UTC(2026,9,22,12);
const copy = core.clone;
class Store {
  constructor() { this.documents = new Map(); this.tickets = new Map(); this.writes = 0; this.conflict = false; this.afterWrite = null; this.beforeWrite = null; this.lastWrite = null; }
  seed(owner, overrides = {}) { const state = {...core.emptyState(owner,catalog), ...overrides}; this.documents.set(owner,{value:copy(state),writeLock:'v0'}); return state; }
  async load(owner) { return this.documents.has(owner) ? copy(this.documents.get(owner)) : null; }
  async loadTicket(owner,ticket) { return copy(this.tickets.get(owner+':'+ticket) || null); }
  async replace(owner,lock,value) {
    if (this.beforeWrite) await this.beforeWrite(owner,lock,value);
    const current = this.documents.get(owner);
    if (this.conflict || !current || current.writeLock !== lock) return false;
    assert.ok(lock); this.writes++;
    this.documents.set(owner, {value:copy(value),writeLock:'v'+this.writes});
    this.lastWrite = copy(value);
    if (this.afterWrite) await this.afterWrite();
    return true;
  }
  value(owner='alice') { return this.documents.get(owner).value; }
  ticket(level, overrides={}) { const name='level-'+level; this.tickets.set('alice:'+name, {ownerId:'alice',kind:'level',level,expiresAt:NOW+60000,catalogVersion:catalog.version,difficulty:'Normal',boostersUsed:0,...overrides}); return name; }
}
const service = store => createEconomyService(store,{clock:()=>NOW});
const op = (kind, fields={}, sequence=1, installationId='phone-a') => ({kind,installationId,sequence,catalogVersion:catalog.version,...fields});
const hat = 'hat.twig_nightcap';
const hatPrice = catalog.cosmetics[hat].price;
const initialized = (overrides={}) => { const s = new Store(); s.seed('alice',{petals:10000,highestCompletedLevel:100,...overrides}); return s; };

test('generated deployment/catalog exactly match current client prices, unlocks and sources', () => {
  cp.execFileSync(process.execPath,['tools/backend/build-economy.cjs','--check'],{cwd:path.resolve(__dirname,'../..')});
  const root = path.resolve(__dirname,'../..');
  const client = JSON.parse(fs.readFileSync(path.join(root,'client/Assets/Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json')));
  for (const item of client.items) {
    if (item.retired || item.defaultLook) assert.equal(catalog.cosmetics[item.id],undefined);
    else { assert.equal(catalog.cosmetics[item.id].price,item.price||0); assert.equal(catalog.cosmetics[item.id].free,item.free===true); }
  }
  assert.equal(catalog.boosters.shuffle.price,1200);
  assert.equal(catalog.boosters.bloomBurst.quantity,1);
});

test('wallet, item and receipt commit together; retry returns original result without another debit', async () => {
  const store=initialized(); const handle=service(store); const buy=op('buy_cosmetic',{itemId:hat});
  const first=await handle('alice',buy); const again=await handle('alice',buy);
  assert.equal(first.ok,true); assert.equal(again.replayed,true);
  assert.deepEqual(again.effects,first.effects);
  assert.equal(store.value().petals,10000-hatPrice);
  assert.deepEqual(store.value().owned,[hat]); assert.equal(store.writes,1);
  assert.equal(store.value().receipts[0].sequence,1);
});

test('same request racing on two servers executes once', async () => {
  const store=initialized(); const handle=service(store); const buy=op('buy_booster',{boosterKind:'return'});
  const results=await Promise.all([handle('alice',buy),handle('alice',copy(buy))]);
  assert.ok(results.every(r=>r.ok)); assert.equal(store.value().petals,8500);
  assert.equal(store.value().boosters.return,3); assert.equal(store.writes,1);
});

test('different operations racing under the same sequence cannot both apply', async () => {
  const store=initialized(); const handle=service(store);
  const results=await Promise.all([handle('alice',op('buy_booster',{boosterKind:'shuffle'})),handle('alice',op('buy_booster',{boosterKind:'return'}))]);
  assert.equal(results.filter(r=>r.ok).length,1);
  assert.equal(results.filter(r=>r.code==='sequence-reused').length,1); assert.equal(store.writes,1);
});

test('two devices cannot spend the same petals twice', async () => {
  const store=initialized({petals:1500}); const handle=service(store);
  const results=await Promise.all(['a','b'].map(device=>handle('alice',op('buy_booster',{boosterKind:'shuffle'},1,device))));
  assert.equal(results.filter(r=>r.ok).length,1); assert.equal(results.filter(r=>r.code==='insufficient-petals').length,1);
  assert.equal(store.value().petals,300); assert.equal(store.value().boosters.shuffle,3);
  assert.equal(store.value().receipts.length,2,'terminal rejection is committed too');
});

test('duplicate purchase of one cosmetic on different installations is charged once', async () => {
  const store=initialized(); const handle=service(store);
  const results=await Promise.all(['a','b'].map(device=>handle('alice',op('buy_cosmetic',{itemId:hat},1,device))));
  assert.equal(results.filter(r=>r.ok).length,1); assert.equal(results.filter(r=>r.code==='already-owned').length,1);
  assert.equal(store.value().petals,10000-hatPrice);
});

test('business rejection stays rejected even after a subsequent reward adds money', async () => {
  const store=initialized({petals:0,highestCompletedLevel:24}); const handle=service(store);
  const buy=op('buy_booster',{boosterKind:'shuffle'});
  assert.equal((await handle('alice',buy)).code,'insufficient-petals');
  store.ticket(25);
  await handle('alice',op('claim_level',{level:25,ticketId:'level-25'},1,'reward-phone'));
  const before=copy(store.value());
  const again=await handle('alice',buy);
  assert.equal(again.code,'insufficient-petals'); assert.equal(again.replayed,true); assert.deepEqual(store.value(),before);
});

test('missing, empty or stale write lock never falls back to unconditional writes', async () => {
  for (const lock of [undefined,null,'']) {
    const store=initialized(); store.documents.get('alice').writeLock=lock;
    assert.equal((await service(store)('alice',op('buy_cosmetic',{itemId:hat}))).code,'missing-write-lock');
    assert.equal(store.writes,0);
  }
  const empty=new Store(); assert.equal((await service(empty)('alice',op('buy_cosmetic',{itemId:hat}))).code,'needs-initialization'); assert.equal(empty.writes,0);
  const contended=initialized(); contended.conflict=true;
  const answer=await service(contended)('alice',op('buy_cosmetic',{itemId:hat}));
  assert.equal(answer.code,'contention'); assert.equal(answer.retryable,true); assert.equal(contended.writes,0);
});

test('ambiguous write timeout is resolved by retrying the exact operation', async () => {
  const store=initialized(); const handle=service(store); const buy=op('buy_cosmetic',{itemId:hat});
  store.afterWrite=async()=>{throw Error('timeout after commit');};
  assert.equal((await handle('alice',buy)).retryable,true);
  store.afterWrite=null; const retry=await handle('alice',buy);
  assert.equal(retry.ok,true); assert.equal(retry.replayed,true); assert.equal(store.writes,1);
});

test('read snapshots cannot mutate stored state or expose internal registries', async () => {
  const store=initialized(); const handle=service(store);
  const read=await handle('alice',null,'phone-a'); read.state.petals=999999;
  assert.equal(store.value().petals,10000); assert.equal(read.nextSequence,1);
  assert.equal(read.state.receipts,undefined); assert.equal(read.state.installations,undefined);
});

test('read snapshot precedes concurrent state change; CAS retry re-evaluates all conditions', async () => {
  const store=initialized({petals:1200}); const handle=service(store);
  store.beforeWrite=async()=>{store.beforeWrite=null; await handle('alice',op('buy_booster',{boosterKind:'shuffle'},1,'other'));};
  const answer=await handle('alice',op('buy_cosmetic',{itemId:hat}));
  assert.equal(answer.code,'insufficient-petals'); assert.equal(store.value().petals,0);
  assert.deepEqual(store.value().owned,[]);
});

test('evicted receipts never permit old work to execute again', async () => {
  const store=initialized(); const handle=service(store); const first=op('buy_cosmetic',{itemId:hat});
  await handle('alice',first);
  for (let sequence=2;sequence<=core.LIMITS.receipts+2;sequence++) await handle('alice',op('buy_cosmetic',{itemId:hat},sequence));
  const before=copy(store.value()); const old=await handle('alice',first);
  assert.equal(old.code,'receipt-expired'); assert.equal(old.replayed,true); assert.deepEqual(store.value(),before);
  assert.equal(store.value().installations['phone-a'],130);
});

test('sequence gaps and installation limit are rejected without losing previous cursors', async () => {
  const store=initialized(); const handle=service(store);
  assert.equal((await handle('alice',op('buy_cosmetic',{itemId:hat},2))).code,'sequence-gap');
  for(let i=0;i<core.LIMITS.installations;i++) await handle('alice',op('buy_cosmetic',{itemId:hat},1,'device-'+i));
  assert.equal((await handle('alice',op('buy_cosmetic',{itemId:hat},1,'extra'))).code,'installation-limit');
  assert.equal(Object.keys(store.value().installations).length,32);
});

test('malicious operation fields and numeric/identifier edge cases fail before any mutation', async () => {
  const invalid=[
    op('buy_cosmetic',{itemId:hat,price:1}),op('buy_cosmetic',{itemId:hat,petals:90000}),op('claim_level',{level:101,ticketId:'x',grants:{petals:900}}),
    {...op('buy_cosmetic',{itemId:hat}),sequence:1.5},{...op('buy_cosmetic',{itemId:hat}),sequence:Infinity},
    {...op('buy_cosmetic',{itemId:hat}),installationId:'__proto__'},op('buy_cosmetic',{itemId:'x'.repeat(129)}),
    op('refund_booster',{boosterKind:'shuffle'}),op('buy_booster',{boosterKind:'__proto__'}),
    JSON.parse('{"installationId":"phone","sequence":1,"catalogVersion":"'+catalog.version+'","kind":"buy_cosmetic","itemId":"'+hat+'","__proto__":{"petals":99999}}')
  ];
  for (const request of invalid) { const store=initialized(); const result=await service(store)('alice',request); assert.equal(result.code,'invalid-operation'); assert.equal(store.writes,0); }
});

test('outdated operation catalog, future schema and malformed state fail closed', async () => {
  const corrupt=[s=>s.schemaVersion=2,s=>s.ownerId='bob',s=>s.petals=-1,s=>s.petals=Number.MAX_SAFE_INTEGER,s=>s.boosters.shuffle=NaN,s=>s.installations.bad=-1,s=>s.catalogVersion=null];
  for(const alter of corrupt){const store=initialized();alter(store.value());const response=await service(store)('alice',op('buy_cosmetic',{itemId:hat}));assert.equal(response.code,'invalid-server-state');assert.equal(store.writes,0);}
  const store=initialized();const request={...op('buy_cosmetic',{itemId:hat}),catalogVersion:'outdated'};
  assert.equal((await service(store)('alice',request)).code,'catalog-changed'); assert.equal(store.writes,0);
});

test('retired, default, unknown, locked and milestone-only items cannot be bought', async () => {
  for (const [itemId,code,highest] of [['hat.acorn_cap','unknown-item',100],['not.real','unknown-item',100],['frame.daisy','not-for-sale',100],[hat,'item-locked',10]]) {
    const store=initialized({highestCompletedLevel:highest});assert.equal((await service(store)('alice',op('buy_cosmetic',{itemId}))).code,code);assert.equal(store.value().petals,10000);
  }
});

test('booster packs use server quantities and spending never goes negative', async () => {
  const store=initialized(); const handle=service(store);
  await handle('alice',op('buy_booster',{boosterKind:'bloomBurst'}));
  assert.equal(store.value().boosters.bloomBurst,1); assert.equal(store.value().petals,9000);
  assert.equal((await handle('alice',op('consume_booster',{boosterKind:'bloomBurst'},2))).ok,true);
  assert.equal((await handle('alice',op('consume_booster',{boosterKind:'bloomBurst'},3))).code,'no-charges');
  assert.equal(store.value().boosters.bloomBurst,0);
});

test('trusted level reward determines clean/hard bonuses and grants once under any sequence/device', async () => {
  const store=initialized({petals:0,highestCompletedLevel:0}); const handle=service(store); store.ticket(1,{difficulty:'Hard'});
  const first=await handle('alice',op('claim_level',{level:1,ticketId:'level-1'}));
  assert.equal(first.ok,true);assert.equal(store.value().petals,30);
  assert.equal((await handle('alice',op('claim_level',{level:1,ticketId:'level-1'},1,'other'))).code,'already-claimed');
  assert.equal(store.value().petals,30);
});

test('untrusted, expired, wrong-owner and wrong-level tickets cannot create rewards', async () => {
  for(const overrides of [{ownerId:'bob'},{level:9},{expiresAt:NOW-1},{kind:'client-win'},{boostersUsed:-1},{difficulty:'Impossible'},{catalogVersion:'old'}]) {
    const store=initialized({highestCompletedLevel:0});store.ticket(1,overrides);
    const answer=await service(store)('alice',op('claim_level',{level:1,ticketId:'level-1'}));
    assert.equal(answer.code,'reward-not-authorized');assert.equal(store.value().petals,10000);
  }
  const store=initialized({highestCompletedLevel:0});
  assert.equal((await service(store)('alice',op('claim_level',{level:1,ticketId:'missing'}))).code,'reward-not-authorized');
  assert.equal((await service(store)('alice',op('claim_level',{level:10,ticketId:'missing'},2))).code,'level-gap');
});

test('basic booster unlocks and regular drops mirror the current roadmap', async () => {
  const store=initialized({petals:0,highestCompletedLevel:1});store.ticket(2);
  await service(store)('alice',op('claim_level',{level:2,ticketId:'level-2'}));assert.equal(store.value().boosters.extraSlot,1);
  const drop=initialized({highestCompletedLevel:19});drop.ticket(20);await service(drop)('alice',op('claim_level',{level:20,ticketId:'level-20'}));
  assert.equal(drop.value().boosters.extraSlot,1);
});

test('milestone bundles are chosen on server, highest cadence wins and repeated claims do not pay', async () => {
  const store=initialized({petals:0});const handle=service(store);
  const result=await handle('alice',op('claim_milestone',{level:100}));
  assert.equal(result.ok,true);assert.equal(store.value().petals,300);assert.deepEqual(store.value().boosters,{extraSlot:1,shuffle:1,return:1,bloomBurst:1});assert.deepEqual(store.value().owned,['frame.ivy']);
  assert.equal((await handle('alice',op('claim_milestone',{level:100},2))).code,'already-claimed');
  assert.equal((await handle('alice',op('claim_milestone',{level:125},3))).code,'level-not-completed');
});

test('two concurrent distinct milestone rewards are both retained by CAS retry', async () => {
  const store=initialized({petals:0});const handle=service(store);
  const result=await Promise.all([handle('alice',op('claim_milestone',{level:25},1,'a')),handle('alice',op('claim_milestone',{level:50},1,'b'))]);
  assert.ok(result.every(r=>r.ok)); assert.equal(store.value().petals,150); assert.deepEqual(store.value().milestones,[25,50]);
});

test('daily uses server UTC and strict step order; ad steps require protected authorization', async () => {
  const store=initialized({petals:0});const handle=service(store);
  assert.equal((await handle('alice',op('claim_daily',{step:1}))).ok,true);
  assert.equal((await handle('alice',op('claim_daily',{step:1},2))).code,'already-claimed');
  assert.equal((await handle('alice',op('claim_daily',{step:4},3))).code,'daily-step-gap');
  assert.equal((await handle('alice',op('claim_daily',{step:2},4))).code,'reward-not-authorized');
  store.tickets.set('alice:ad',{ownerId:'alice',kind:'daily-ad',date:'2026-10-22',step:2,expiresAt:NOW+60000});
  assert.equal((await handle('alice',op('claim_daily',{step:2,ticketId:'ad'},5))).ok,true);
  assert.equal(store.value().petals,50);assert.deepEqual(store.value().daily,{date:'2026-10-22',step:2});
  const tomorrow=createEconomyService(store,{clock:()=>NOW+86400000});
  assert.equal((await tomorrow('alice',op('claim_daily',{step:1},6))).ok,true);
  assert.equal(store.value().petals,70);
  assert.equal((await handle('alice',op('claim_daily',{step:1},7))).code,'server-date-regressed');
});

test('a capped reward bundle rolls back every effect and keeps a stable rejection', async () => {
  const store=initialized({petals:0});store.value().boosters.bloomBurst=core.LIMITS.charges;
  const result=await service(store)('alice',op('claim_milestone',{level:100}));
  assert.equal(result.code,'booster-limit');assert.equal(store.value().petals,0);assert.deepEqual(store.value().milestones,[]);assert.deepEqual(store.value().owned,[]);assert.equal(store.value().installations['phone-a'],1);
});

// Exercise the generated deployable endpoint and the actual UGS adapter against a strict REST stand-in.
const apiState={store:null,serviceOnly:true,calls:[],noLock:false};
class DataApi {
  constructor(options){assert.equal(options.accessToken,'service-token');}
  async getProtectedItems(projectId,owner,keys){apiState.calls.push(['load',owner]);const results=[];for(const key of keys){if(key==='economy_v1'){const v=await apiState.store.load(owner);if(v)results.push({key,value:v.value,writeLock:apiState.noLock?null:v.writeLock});}else if(apiState.store.tickets.has(owner+':'+key))results.push({key,value:copy(apiState.store.tickets.get(owner+':'+key))});}return {data:{results}};}
  async setProtectedItem(projectId,owner,item){apiState.calls.push(['write',owner,item.writeLock]);if(!item.writeLock)throw Error('unconditional write forbidden');if(!await apiState.store.replace(owner,item.writeLock,item.value)){const error=Error('conflict');error.response={status:409};throw error;}}
}
const original=Module._load;
Module._load=function(request,parent,main){return request==='@unity-services/cloud-save-1.4'?{DataApi}:original.call(this,request,parent,main);};
const endpoint=require('../cloud-code/ExecuteEconomy.js');
Module._load=original;
const call=(params,owner='alice')=>endpoint({params,context:{projectId:'p',playerId:owner,serviceToken:'service-token'}});

test('standalone UGS endpoint scopes identity, applies CAS and rejects client identity/amount overrides', async () => {
  apiState.store=initialized();apiState.calls=[];
  const params={action:'execute',expectedOwnerId:'alice',operationJson:JSON.stringify(op('buy_cosmetic',{itemId:hat}))};
  assert.equal((await call(params,'bob')).code,'owner-mismatch');assert.deepEqual(apiState.calls,[]);
  assert.equal((await call(params)).ok,true);assert.equal(apiState.store.value().petals,10000-hatPrice);
  assert.equal(apiState.calls.find(c=>c[0]==='write')[2],'v0');
  const retry=await call(params);assert.equal(retry.replayed,true);
  const read=await call({action:'read',expectedOwnerId:'alice',installationId:'phone-a'});assert.equal(read.nextSequence,2);
});

test('deployable endpoint cannot turn malformed JSON or empty execute into a state read/write', async () => {
  apiState.store=initialized();apiState.calls=[];
  for(const operationJson of ['{bad','null','{}','"text"','x'.repeat(2049)]) assert.equal((await call({action:'execute',expectedOwnerId:'alice',operationJson})).code,'invalid-operation');
  assert.equal(apiState.store.writes,0);
  assert.equal((await call({action:'init',expectedOwnerId:'alice'})).code,'invalid-action');
});

test('UGS missing-record initialization and missing-lock writes are blocked', async () => {
  apiState.store=new Store();apiState.calls=[];
  assert.equal((await call({action:'read',expectedOwnerId:'alice'})).code,'needs-initialization');
  apiState.store=initialized();apiState.noLock=true;
  assert.equal((await call({action:'execute',expectedOwnerId:'alice',operationJson:JSON.stringify(op('buy_cosmetic',{itemId:hat}))})).code,'missing-write-lock');
  assert.equal(apiState.calls.filter(c=>c[0]==='write').length,0);apiState.noLock=false;
});

test('catalog rollout preserves wallet reads, old receipts, ownership and permanent claims', async () => {
  const store=initialized();const handle=service(store);const request=op('buy_cosmetic',{itemId:hat});
  await handle('alice',request);
  const state=store.value();state.catalogVersion='economy-previous';state.milestones=[25];
  const read=await handle('alice');assert.equal(read.ok,true);assert.equal(read.catalogVersion,catalog.version);
  const retry=await handle('alice',request);assert.equal(retry.replayed,true);assert.equal(store.writes,1);
  const next=await handle('alice',op('buy_booster',{boosterKind:'shuffle'},2));
  assert.equal(next.ok,true);assert.equal(store.value().catalogVersion,catalog.version);
  assert.deepEqual(store.value().owned,[hat]);assert.deepEqual(store.value().milestones,[25]);
  assert.equal(store.value().petals,10000-hatPrice-1200);
});

test('installation IDs matching inherited object names still have integer sequence cursors', async () => {
  const store=initialized();const handle=service(store);
  for(const name of ['toString','valueOf','hasOwnProperty']) {
    assert.equal((await handle('alice',null,name)).nextSequence,1);
    assert.equal((await handle('alice',op('buy_booster',{boosterKind:'shuffle'},1,name))).nextSequence,2);
  }
});

test('automatic booster purchase and consumption are one atomic operation with explicit consent', async () => {
  const store=initialized({petals:1200});const handle=service(store);
  assert.equal((await handle('alice',op('consume_booster',{boosterKind:'shuffle'}))).code,'no-charges');
  const request=op('consume_booster',{boosterKind:'shuffle',autoBuy:true},2);
  assert.equal((await handle('alice',request)).ok,true);assert.equal(store.value().petals,0);assert.equal(store.value().boosters.shuffle,2);
  assert.equal((await handle('alice',request)).replayed,true);assert.equal(store.value().boosters.shuffle,2);
  const poor=initialized({petals:1000});
  assert.equal((await service(poor)('alice',op('consume_booster',{boosterKind:'shuffle',autoBuy:true}))).code,'insufficient-petals');
  assert.equal(poor.value().petals,1000);assert.equal(poor.value().boosters.shuffle,0);
});

test('avatar and clearing prices match current tiers; free defaults are implicit entitlements', async () => {
  const store=initialized();const handle=service(store);
  assert.equal((await handle('alice',op('buy_cosmetic',{itemId:'avatar.twig_glasses_scarf_clay'}))).ok,true);
  assert.equal(store.value().petals,9700);
  assert.equal((await handle('alice',op('buy_cosmetic',{itemId:'clear.blossom'},2))).ok,true);assert.equal(store.value().petals,7200);
  assert.equal((await handle('alice',op('buy_cosmetic',{itemId:'avatar.twig_default'},3))).code,'already-owned');
  assert.equal(store.value().petals,7200);assert.ok(!store.value().owned.includes('avatar.twig_default'));
});

test('Daily Challenge and free booster ads require evidence and cannot be claimed twice per server day', async () => {
  const store=initialized({petals:0});const handle=service(store);
  assert.equal((await handle('alice',op('claim_challenge',{ticketId:'challenge'}))).code,'reward-not-authorized');
  store.tickets.set('alice:challenge',{ownerId:'alice',kind:'challenge',date:'2026-10-22',expiresAt:NOW+60000,catalogVersion:catalog.version});
  assert.equal((await handle('alice',op('claim_challenge',{ticketId:'challenge'},2))).ok,true);assert.equal(store.value().petals,50);
  assert.equal((await handle('alice',op('claim_challenge',{ticketId:'challenge'},1,'other'))).code,'already-claimed');
  store.tickets.set('alice:booster-ad',{ownerId:'alice',kind:'free-booster-ad',date:'2026-10-22',expiresAt:NOW+60000});
  assert.equal((await handle('alice',op('claim_booster_ad',{ticketId:'booster-ad'},3))).ok,true);
  assert.equal(store.value().boosters.extraSlot,1);
  assert.equal((await handle('alice',op('claim_booster_ad',{ticketId:'booster-ad'},4))).code,'already-claimed');
});

test('inventory and total-document limits cannot produce partial purchases or erase replay protection', async () => {
  const full=initialized();full.value().owned=Array.from({length:core.LIMITS.owned},(_,i)=>'hat.fixture_'+i);
  const response=await service(full)('alice',op('buy_cosmetic',{itemId:hat}));
  assert.equal(response.code,'inventory-limit');assert.equal(full.value().petals,10000);assert.equal(full.value().owned.length,core.LIMITS.owned);
  const big=initialized();const state=big.value();state.installations.seed=1;
  state.receipts=[{installationId:'seed',sequence:1,hash:'a'.repeat(64),outcome:{ok:true,code:'seeded',effects:{padding:''}}}];
  state.receipts[0].outcome.effects.padding='x'.repeat(core.LIMITS.bytes-Buffer.byteLength(JSON.stringify(state))-30);
  const before=copy(state);
  assert.equal((await service(big)('alice',op('buy_cosmetic',{itemId:hat}))).code,'state-size-limit');
  assert.deepEqual(big.value(),before);assert.equal(big.writes,0);
});

test('final representable sequence never returns an unsafe next number', async () => {
  const store=initialized();store.value().installations['phone-a']=Number.MAX_SAFE_INTEGER-1;
  const response=await service(store)('alice',op('buy_cosmetic',{itemId:hat},Number.MAX_SAFE_INTEGER));
  assert.equal(response.ok,true);assert.equal(response.nextSequence,null);
});

test('endpoint rejects extra parameters instead of treating a client balance as server input', async () => {
  apiState.store=initialized();apiState.calls=[];
  assert.equal((await call({action:'read',expectedOwnerId:'alice',petals:999999})).code,'invalid-parameters');
  assert.deepEqual(apiState.calls,[]);
});

test('verified offline Daily Challenge can finish after its puzzle date without duplicate or future claims', async () => {
  const store=initialized({petals:0});const handle=service(store);
  store.tickets.set('alice:yesterday',{ownerId:'alice',kind:'challenge',date:'2026-10-21',expiresAt:NOW+60000,catalogVersion:catalog.version});
  const request=op('claim_challenge',{ticketId:'yesterday'});
  assert.equal((await handle('alice',request)).ok,true);assert.equal(store.value().petals,50);assert.equal(store.value().challengeDate,'2026-10-21');
  assert.equal((await handle('alice',request)).replayed,true);
  assert.equal((await handle('alice',op('claim_challenge',{ticketId:'yesterday'},2))).code,'already-claimed');
  store.tickets.set('alice:future',{ownerId:'alice',kind:'challenge',date:'2026-10-23',expiresAt:NOW+60000,catalogVersion:catalog.version});
  assert.equal((await handle('alice',op('claim_challenge',{ticketId:'future'},3))).code,'reward-not-authorized');assert.equal(store.value().petals,50);
});

test('consumed explicitly distinguishes remembered business rejection from unprocessed transport/protocol failure', async () => {
  const store=initialized({petals:0});const handle=service(store);
  assert.equal((await handle('alice',op('buy_booster',{boosterKind:'shuffle'}))).consumed,true);
  assert.equal((await handle('alice',op('buy_booster',{boosterKind:'shuffle'},3))).consumed,false);
  store.conflict=true;
  assert.equal((await handle('alice',op('buy_booster',{boosterKind:'shuffle'},2))).consumed,false);
});
