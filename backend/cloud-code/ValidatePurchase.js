/**
 * ValidatePurchase — UGS Cloud Code (contracts/backend-services.md, FR-089, research R13; T132).
 *
 * Input:  platform ("google" | "apple"), receipt (the Unity IAP receipt JSON string), productId.
 * Output: { valid, transactionId, grants }.
 *
 * - Google Play: the purchase data is verified against its signature with the app's Play license public key.
 * - Apple: the transaction is looked up with the App Store Server API, signed with the in-app purchase key.
 * - Idempotent by transactionId: the first answer for a transaction is stored in the player's Cloud Save data and
 *   returned again for repeats. The client ledger is keyed by the same id, so a repeat never grants twice.
 * - The starter pack is offered once: buying it records the offer as used (see GetStarterPackOffer).
 *
 * Secrets (UGS Secret Manager): GOOGLE_PLAY_LICENSE_KEY, APPLE_ISSUER_ID, APPLE_KEY_ID, APPLE_PRIVATE_KEY,
 * APPLE_BUNDLE_ID. The product grants mirror client/Assets/Bloomlings/Services/Purchases/ProductCatalog.json.
 */
const crypto = require("crypto");
const axios = require("axios-0.21");
const { DataApi } = require("@unity-services/cloud-save-1.4");
const { SecretManagerClient } = require("@unity-services/secret-manager-1.0");

const PRODUCTS = {
  petals_s: { petals: 120 },
  petals_m: { petals: 400 },
  petals_l: { petals: 1000 },
  boosters_bundle_small: { boosters: { extraSlot: 2, shuffle: 2, return: 2, bloomBurst: 1 } },
  boosters_bundle_large: { boosters: { extraSlot: 5, shuffle: 5, return: 5, bloomBurst: 3 } },
  starter_pack: { petals: 300, boosters: { extraSlot: 2, shuffle: 2, return: 2, bloomBurst: 2 }, offeredOnce: true },
  remove_ads: { removeAds: true },
};

const INVALID = { valid: false, transactionId: null, grants: null };

module.exports = async ({ params, context, logger }) => {
  const product = PRODUCTS[params.productId];
  if (!product) {
    logger.warning(`Unknown product ${params.productId}`);
    return INVALID;
  }

  const secrets = new SecretManagerClient(context);
  let transactionId;
  try {
    const receipt = JSON.parse(params.receipt);
    transactionId = params.platform === "apple"
      ? await verifyApple(receipt, params.productId, secrets)
      : await verifyGoogle(receipt, params.productId, secrets);
  } catch (error) {
    logger.error(`Receipt check failed: ${error.message}`);
    return INVALID;
  }

  if (!transactionId) {
    return INVALID;
  }

  // Idempotency: the first answer for this transaction is kept and returned again.
  const cloudSave = new DataApi(context);
  const key = `purchase_tx_${transactionId.replace(/[^A-Za-z0-9_-]/g, "_")}`;
  const existing = await cloudSave.getItems(context.projectId, context.playerId, [key]);
  const stored = existing.data.results.find((item) => item.key === key);
  if (stored) {
    return stored.value;
  }

  const answer = { valid: true, transactionId, grants: product };
  const items = [{ key, value: answer }];
  if (product.offeredOnce) {
    items.push({ key: "starter_pack_used", value: true });
  }

  for (const item of items) {
    await cloudSave.setItem(context.projectId, context.playerId, item);
  }

  return answer;
};

/** Google Play: the purchase JSON must carry this product, be purchased, and match its signature. */
async function verifyGoogle(receipt, productId, secrets) {
  const payload = JSON.parse(receipt.Payload);
  const purchase = JSON.parse(payload.json);
  const licenseKey = (await secrets.getSecret("GOOGLE_PLAY_LICENSE_KEY")).value;
  const publicKey = `-----BEGIN PUBLIC KEY-----\n${licenseKey.match(/.{1,64}/g).join("\n")}\n-----END PUBLIC KEY-----`;
  const signed = crypto.verify("RSA-SHA1", Buffer.from(payload.json), publicKey, Buffer.from(payload.signature, "base64"));
  if (!signed || purchase.productId !== productId || purchase.purchaseState !== 0) {
    return null;
  }

  return purchase.orderId || purchase.purchaseToken;
}

/** Apple: the App Store Server API confirms the transaction for this bundle and product. */
async function verifyApple(receipt, productId, secrets) {
  const transactionId = receipt.TransactionID;
  const issuer = (await secrets.getSecret("APPLE_ISSUER_ID")).value;
  const keyId = (await secrets.getSecret("APPLE_KEY_ID")).value;
  const privateKey = (await secrets.getSecret("APPLE_PRIVATE_KEY")).value;
  const bundleId = (await secrets.getSecret("APPLE_BUNDLE_ID")).value;
  const token = appStoreToken(issuer, keyId, privateKey, bundleId);
  const response = await axios.get(`https://api.storekit.itunes.apple.com/inApps/v1/transactions/${encodeURIComponent(transactionId)}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  // signedTransactionInfo is a JWS whose payload was signed by Apple and fetched over TLS from Apple's API.
  const info = JSON.parse(Buffer.from(response.data.signedTransactionInfo.split(".")[1], "base64url").toString("utf8"));
  if (info.bundleId !== bundleId || info.productId !== productId || info.revocationDate) {
    return null;
  }

  return info.transactionId;
}

/** An ES256 JWT for the App Store Server API. */
function appStoreToken(issuer, keyId, privateKey, bundleId) {
  const now = Math.floor(Date.now() / 1000);
  const header = { alg: "ES256", kid: keyId, typ: "JWT" };
  const claims = { iss: issuer, iat: now, exp: now + 600, aud: "appstoreconnect-v1", bid: bundleId };
  const encode = (value) => Buffer.from(JSON.stringify(value)).toString("base64url");
  const unsigned = `${encode(header)}.${encode(claims)}`;
  const signature = crypto.sign("sha256", Buffer.from(unsigned), { key: privateKey, dsaEncoding: "ieee-p1363" });
  return `${unsigned}.${signature.toString("base64url")}`;
}

module.exports.params = {
  platform: { type: "String", required: true },
  receipt: { type: "String", required: true },
  productId: { type: "String", required: true },
};
