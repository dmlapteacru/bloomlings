// No dependencies. Generate a fixed server catalog and a standalone UGS JS script.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const read = p => JSON.parse(fs.readFileSync(path.join(root, p), 'utf8'));
const remote = read('backend/remote-config/defaults.json').keys;
const roadmap = read('content/roadmap/unlock-roadmap.json').entries;
const level = id => { const row = roadmap.find(e => e.unlockId === id); if (!row) throw Error(id); return row.level; };
const value = key => { const v = remote[key]?.default; if (!Number.isSafeInteger(v) || v < 0) throw Error(key); return v; };
const cosmetics = Object.fromEntries(read('client/Assets/Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json').items
  .filter(i => !i.retired && !i.defaultLook)
  .map(i => [i.id, { price: i.price || 0, free: i.free === true, minLevel: i.free && !i.metaOnly ? 1 : Math.max(level('system.wardrobe'), i.unlockLevel || 0) }]));
// Existing avatar/clearing catalogs are C# tables. Extract their finite declarations and fail if the
// expected shape changes; --check in tests prevents publishing a server/client price mismatch.
const avatarSource = fs.readFileSync(path.join(root,'client/Assets/Bloomlings/Meta/Profile/AvatarCatalog.cs'),'utf8');
const avatars = [...avatarSource.matchAll(/Item\("([a-z0-9_]+)", Family\.[A-Za-z]+, AvatarTier\.(Free|Common|Rare|Special)\)/g)];
if (!avatars.length) throw Error('Avatar table shape changed');
for (const [,name,tier] of avatars) cosmetics['avatar.'+name] = {price:tier==='Free'?0:value('economy.price.avatar'+tier),free:tier==='Free',minLevel:1};
const clearSource = fs.readFileSync(path.join(root,'client/Assets/Bloomlings/UI/Design/ClearStyles.cs'),'utf8');
const bought = clearSource.match(/IReadOnlyList<ClearStyle> Bought[^]*?new\[\][^]*?\{([^]*?)\};/);
if (!bought) throw Error('Clearing table shape changed');
for (const name of [...bought[1].matchAll(/ClearStyle\.([A-Za-z]+)/g)].map(m=>m[1])) {
  const tier = name==='Blossom'?'Intro':['Fireworks','Parade'].includes(name)?'Premium':'';
  cosmetics['clear.'+name.toLowerCase()]={price:value('economy.price.clearing'+tier),free:false,minLevel:level('system.wardrobe')};
}
cosmetics['clear.munchers']={price:0,free:true,minLevel:1};
const challengeSource=fs.readFileSync(path.join(root,'client/Assets/Bloomlings/Meta/DailyChallenge/DailyChallengeRules.cs'),'utf8');
const challengePetals=Number(challengeSource.match(/RewardPetals = (\d+)/)?.[1]);
if (!Number.isSafeInteger(challengePetals) || challengePetals<0) throw Error('Daily Challenge reward changed');
const boosters = Object.fromEntries([
  ['extraSlot', 'extra_slot', 3], ['shuffle', 'shuffle', 3], ['return', 'return', 3], ['bloomBurst', 'bloom_burst', 1]
].map(([kind, unlock, quantity]) => [kind, { price: value('economy.price.' + kind), quantity, minLevel: level('booster.' + unlock) }]));
const catalog = {
  version: 'economy-1', maxLevel: 2500, cosmetics, boosters,
  rewards: { base: value('economy.petals.base'), clean: value('economy.petals.cleanBonus'), hard: value('economy.petals.hardBonus'), superHard: value('economy.petals.superHardBonus'), unlockGrant: value('economy.unlockGrant'), dropEvery: value('economy.drop.everyLevels') },
  challenge: {minLevel:level('system.daily_challenge'),petals:challengePetals},
  daily: { minLevel: level('system.daily_reward'), petals: [1,2,3,4,5].map(n => value('daily.reward.step' + n)), ads: [false,true,true,false,true] },
  milestones: read('content/roadmap/milestones.json').cadences,
};
catalog.version = 'economy-1-' + crypto.createHash('sha256').update(JSON.stringify(catalog)).digest('hex').slice(0, 16);
const catalogJson = JSON.stringify(catalog, null, 2) + '\n';
const outputs = new Map([['backend/economy/catalog.json', catalogJson]]);
const publicCatalog={version:catalog.version,cosmetics:catalog.cosmetics,boosters:catalog.boosters};
outputs.set('client/Assets/Bloomlings/Services/Economy/Resources/ServerEconomyCatalog.json',JSON.stringify(publicCatalog,null,2)+'\n');
const core=require('../../backend/economy/core.js');
const initial=core.emptyState('alice',catalog);initial.petals=10000;initial.highestCompletedLevel=100;
const operation={kind:'buy_cosmetic',installationId:'fixture-phone',sequence:1,catalogVersion:catalog.version,itemId:'hat.twig_nightcap'};
const applied=core.execute(initial,operation,catalog,null,Date.UTC(2026,9,22));
outputs.set('backend/tests/fixtures/economy-client.json',JSON.stringify({
  catalog:publicCatalog,initialState:core.snapshot(initial),operation,
  read:{ok:true,code:'loaded',consumed:false,retryable:false,catalogVersion:catalog.version,catalog:{cosmetics:catalog.cosmetics,boosters:catalog.boosters},state:core.snapshot(initial),nextSequence:1},
  purchase:{...applied.result,consumed:true,retryable:false,replayed:false,catalogVersion:catalog.version,state:core.snapshot(applied.next),nextSequence:2}
},null,2)+'\n');

const files = ['core.js', 'service.js', 'ugs-store.js', 'endpoint.js'];
const modules = files.map(name => `${JSON.stringify('./' + name)}: function(module, exports, require) {\n${fs.readFileSync(path.join(root, 'backend/economy', name), 'utf8')}\n}`);
modules.push(`"./catalog.json": function(module) { module.exports = ${JSON.stringify(catalog)}; }`);
const bundle = `// GENERATED by node tools/backend/build-economy.cjs. Edit backend/economy sources.\n// Source fingerprint: ${crypto.createHash('sha256').update(modules.join('\n')).digest('hex')}\nmodule.exports = (() => {\nconst factories = {\n${modules.join(',\n')}\n};\nconst cache = {};\nfunction localRequire(id) {\n if (!Object.prototype.hasOwnProperty.call(factories, id)) return require(id);\n if (!cache[id]) { const m = {exports:{}}; cache[id]=m; factories[id](m,m.exports,localRequire); }\n return cache[id].exports;\n}\nreturn localRequire('./endpoint.js');\n})();\n`;
outputs.set('backend/cloud-code/ExecuteEconomy.js', bundle);
let stale = false;
for (const [file, content] of outputs) {
  if (process.argv.includes('--check')) {
    if (!fs.existsSync(path.join(root, file)) || fs.readFileSync(path.join(root, file), 'utf8') !== content) { console.error('Regenerate ' + file); stale = true; }
  } else fs.writeFileSync(path.join(root, file), content);
}
if (stale) process.exitCode = 1;
