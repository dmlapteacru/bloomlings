/**
 * SubmitProgress — UGS Cloud Code (contracts/backend-services.md, FR-062, FR-089; T140).
 *
 * Input:  level (highest completed level), contentVersion, commandLogHash (SHA-256 of the winning command log).
 * Output: { accepted, reason, rank }.
 *
 * Writes the player's score on the leaderboard "global_highest_level", encoded as
 *   level × 10_000_000 + (9_999_999 − minutesSince(2026-01-01T00:00Z))
 * so that, for the same level, the earlier completion ranks higher (UGS breaks ties by player id). The time is the
 * server's, taken when the submission arrives. The client mirror is Services/Backend/LeaderboardScore.cs.
 *
 * Sanity checks (rejections are logged, never banned; doc 15 §23):
 * - the level must increase monotonically;
 * - the jump must be plausible for the time since the last accepted submission
 *   (FIRST_ALLOWANCE for the first one, then BURST + LEVELS_PER_MINUTE × minutes);
 * - the content version must be one the game supports (raise MAX_CONTENT_VERSION with each content release).
 *
 * State per player (Cloud Save, key "leaderboard_progress"): { level, at, firstAttemptAt }.
 */
const { DataApi } = require("@unity-services/cloud-save-1.4");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

const LEADERBOARD_ID = "global_highest_level";
const STATE_KEY = "leaderboard_progress";
const EPOCH_MS = Date.UTC(2026, 0, 1, 0, 0, 0);
const LEVEL_FACTOR = 10000000;
const MAX_MINUTES = 9999999;

const MIN_CONTENT_VERSION = 1;
const MAX_CONTENT_VERSION = 1000;
const FIRST_ALLOWANCE = 200;
const BURST = 5;
const LEVELS_PER_MINUTE = 2;

module.exports = async ({ params, context, logger }) => {
  const level = Number(params.level);
  const contentVersion = Number(params.contentVersion);
  const reject = (reason, details) => {
    logger.warning(`SubmitProgress rejected (${reason}) for ${context.playerId}: ${JSON.stringify(details)}`);
    return { accepted: false, reason, rank: null };
  };

  if (!Number.isInteger(level) || level < 1) {
    return reject("invalid-level", { level: params.level });
  }

  if (!Number.isInteger(contentVersion) || contentVersion < MIN_CONTENT_VERSION || contentVersion > MAX_CONTENT_VERSION) {
    return reject("unsupported-content-version", { contentVersion: params.contentVersion });
  }

  const now = Date.now();
  const cloudSave = new DataApi(context);
  const stored = await cloudSave.getItems(context.projectId, context.playerId, [STATE_KEY]);
  const found = stored.data.results.find((item) => item.key === STATE_KEY);
  const state = found ? found.value : { level: 0, at: null, firstAttemptAt: now };

  if (level <= state.level) {
    return reject("not-monotonic", { level, previous: state.level });
  }

  const since = state.at !== null ? state.at : state.firstAttemptAt;
  const minutes = Math.max(0, (now - since) / 60000);
  const allowed = (state.at !== null ? BURST : FIRST_ALLOWANCE) + LEVELS_PER_MINUTE * minutes;
  if (level - state.level > allowed) {
    if (!found) {
      // Remember the first attempt, so a long offline run is accepted once enough time has passed.
      await cloudSave.setItem(context.projectId, context.playerId, { key: STATE_KEY, value: state });
    }

    return reject("implausible-jump", { level, previous: state.level, minutes: Math.round(minutes), allowed: Math.floor(allowed) });
  }

  const elapsed = Math.min(MAX_MINUTES, Math.max(0, Math.floor((now - EPOCH_MS) / 60000)));
  const score = level * LEVEL_FACTOR + (MAX_MINUTES - elapsed);
  const leaderboards = new LeaderboardsApi(context);
  const result = await leaderboards.addLeaderboardPlayerScore(context.projectId, LEADERBOARD_ID, context.playerId, { score });

  await cloudSave.setItem(context.projectId, context.playerId, {
    key: STATE_KEY,
    value: { level, at: now, firstAttemptAt: state.firstAttemptAt, contentVersion, commandLogHash: String(params.commandLogHash || "") },
  });

  const rank = result && result.data && typeof result.data.rank === "number" ? result.data.rank + 1 : null;
  return { accepted: true, reason: null, rank };
};

module.exports.params = {
  level: { type: "NUMERIC", required: true },
  contentVersion: { type: "NUMERIC", required: true },
  commandLogHash: { type: "STRING", required: false },
};
