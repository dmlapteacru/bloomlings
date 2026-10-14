/**
 * Public cosmetic IDs for the requested leaderboard players. Read protected appearance state rather than relying
 * on equal-score metadata updates under Keep Best. No private save, purchase or progression data is returned.
 */
const { DataApi } = require("@unity-services/cloud-save-1.4");
module.exports = async ({ params, context }) => {
  let players;
  try { players = JSON.parse(params.playerIdsJson); } catch { throw new Error("invalid-player-list"); }
  if (!Array.isArray(players) || players.length > 24 ||
      players.some(id => typeof id !== "string" || !/^[A-Za-z0-9_-]{1,128}$/.test(id))) throw new Error("invalid-player-list");
  const api = new DataApi({ accessToken: context.serviceToken });
  const result = Object.create(null);
  await Promise.all([...new Set(players)].map(async id => {
    const stored = await api.getProtectedItems(context.projectId, id, ["leaderboard_progress"]);
    const state = stored.data.results.find(item => item.key === "leaderboard_progress");
    if (state && state.value && state.value.appearance) {
      const appearance = state.value.appearance;
      result[id] = { avatar: appearance.avatar ?? null, frame: appearance.frame ?? null,
        badge: appearance.badge ?? null, marker: appearance.marker ?? null };
      if (appearance.name !== undefined) result[id].name = appearance.name;
    }
  }));
  return result;
};
module.exports.params = { playerIdsJson: { type: "STRING", required: true } };
