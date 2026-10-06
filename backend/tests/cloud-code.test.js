// Tests for the Cloud Code scripts with in-memory stand-ins for the UGS modules: `node --test backend/tests`.
const test = require("node:test");
const assert = require("node:assert/strict");
const crypto = require("crypto");
const Module = require("module");
const path = require("path");

// ---- In-memory UGS stand-ins ----
const store = { custom: new Map(), protected: new Map(), player: new Map(), scores: [] };
const results = (map, keys) => ({ data: { results: keys.filter((k) => map.has(k)).map((k) => ({ key: k, value: map.get(k) })) } });
const scoped = (map, id) => {
  if (!map.has(id)) map.set(id, new Map());
  return map.get(id);
};

class DataApi {
  constructor(arg) {
    this.service = Boolean(arg && arg.accessToken === "service-token");
  }
  async getCustomItems(projectId, customId, keys) { return results(scoped(store.custom, customId), keys); }
  async setCustomItem(projectId, customId, item) { this.requireService(); scoped(store.custom, customId).set(item.key, item.value); }
  async getProtectedItems(projectId, playerId, keys) { return results(scoped(store.protected, playerId), keys); }
  async setProtectedItem(projectId, playerId, item) { this.requireService(); scoped(store.protected, playerId).set(item.key, item.value); }
  async getItems(projectId, playerId, keys) { return results(scoped(store.player, playerId), keys); }
  async setItem(projectId, playerId, item) { scoped(store.player, playerId).set(item.key, item.value); }
  requireService() { if (!this.service) throw new Error("server data needs the service token"); }
}

const keys = crypto.generateKeyPairSync("rsa", { modulusLength: 2048 });
const licenseKey = keys.publicKey.export({ type: "spki", format: "der" }).toString("base64");
class SecretManagerClient {
  async getSecret(name) { return { value: name === "GOOGLE_PLAY_LICENSE_KEY" ? licenseKey : "" }; }
}
class LeaderboardsApi {
  async addLeaderboardPlayerScore(projectId, id, playerId, body) { store.scores.push({ playerId, score: body.score }); return { data: { rank: 0 } }; }
}

const fakes = {
  "@unity-services/cloud-save-1.4": { DataApi },
  "@unity-services/secret-manager-1.0": { SecretManagerClient },
  "@unity-services/leaderboards-1.1": { LeaderboardsApi },
  "axios-0.21": { get: async () => { throw new Error("no network in tests"); } },
};
const load = Module._load;
Module._load = function (request, parent, isMain) {
  return Object.prototype.hasOwnProperty.call(fakes, request) ? fakes[request] : load.call(this, request, parent, isMain);
};

const validate = require(path.join(__dirname, "..", "cloud-code", "ValidatePurchase.js"));
const offer = require(path.join(__dirname, "..", "cloud-code", "GetStarterPackOffer.js"));
const progress = require(path.join(__dirname, "..", "cloud-code", "SubmitProgress.js"));

// ---- Helpers ----
const logger = { warning() {}, info() {} };
const context = (playerId) => ({ projectId: "p", playerId, serviceToken: "service-token", accessToken: "player-token" });

function receipt(productId, orderId) {
  const json = JSON.stringify({ productId, orderId, purchaseState: 0, purchaseToken: "token-" + orderId });
  const signature = crypto.sign("RSA-SHA1", Buffer.from(json), keys.privateKey).toString("base64");
  return JSON.stringify({ Payload: JSON.stringify({ json, signature }) });
}

const buy = (playerId, productId, orderId) =>
  validate({ params: { platform: "google", productId, receipt: receipt(productId, orderId) }, context: context(playerId), logger });

// ---- ValidatePurchase ----
test("a receipt is granted once, and again only to the same player", async () => {
  const first = await buy("alice", "petals_m", "GPA.1");
  assert.equal(first.valid, true);
  assert.equal(first.transactionId, "GPA.1");
  assert.deepEqual(first.grants, { petals: 400 });

  assert.deepEqual(await buy("alice", "petals_m", "GPA.1"), first, "a repeat gets the same answer");

  const replay = await buy("mallory", "petals_m", "GPA.1");
  assert.equal(replay.valid, false);
  assert.equal(replay.reason, "claimed-by-another-player");
});

test("Remove Ads can be restored on another game account of the same store account", async () => {
  assert.equal((await buy("bob", "remove_ads", "GPA.2")).valid, true);
  assert.equal((await buy("bob-new-device", "remove_ads", "GPA.2")).valid, true);
});

test("a tampered receipt or the wrong product is refused", async () => {
  const good = JSON.parse(JSON.parse(receipt("petals_s", "GPA.3")).Payload);
  const tampered = JSON.stringify({ Payload: JSON.stringify({ json: good.json.replace("petals_s", "petals_l"), signature: good.signature }) });
  const answer = await validate({ params: { platform: "google", productId: "petals_l", receipt: tampered }, context: context("carol"), logger });
  assert.equal(answer.valid, false);
  const other = await validate({ params: { platform: "google", productId: "petals_l", receipt: receipt("petals_s", "GPA.3") }, context: context("carol"), logger });
  assert.equal(other.valid, false, "the signed purchase is for petals_s");
});

test("the starter pack is once per player, remembered server-side", async () => {
  assert.deepEqual(await offer({ context: context("dave") }), { eligible: true });
  assert.equal((await buy("dave", "starter_pack", "GPA.4")).valid, true);
  assert.deepEqual(await offer({ context: context("dave") }), { eligible: false });

  const again = await buy("dave", "starter_pack", "GPA.5");
  assert.equal(again.valid, false);
  assert.equal(again.reason, "offer-already-used");
  assert.equal(store.player.get("dave"), undefined, "nothing lives in player-writable data");
});

// ---- SubmitProgress ----
test("progress state is protected and cannot be reset by the player", async () => {
  const submit = (level) => progress({ params: { level, contentVersion: 1, commandLogHash: "h" }, context: context("erin"), logger });
  assert.equal((await submit(10)).accepted, true);
  assert.equal(store.protected.get("erin").get("leaderboard_progress").level, 10);
  assert.equal((await submit(5)).reason, "not-monotonic");

  // The player wipes its own (player-writable) data: the allowance does not come back.
  store.player.delete("erin");
  assert.equal((await submit(500)).reason, "implausible-jump");
  assert.equal(store.scores.length, 1);
});
