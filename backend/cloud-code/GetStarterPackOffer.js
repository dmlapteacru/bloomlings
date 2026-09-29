/**
 * GetStarterPackOffer — UGS Cloud Code (contracts/backend-services.md, research R13; T132).
 *
 * Output: { eligible }. The starter pack is a one-time offer; the flag lives server-side in the player's Cloud Save
 * data ("starter_pack_used", set by ValidatePurchase), so reinstalling or changing device does not bring it back.
 */
const { DataApi } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ context }) => {
  const cloudSave = new DataApi(context);
  const result = await cloudSave.getItems(context.projectId, context.playerId, ["starter_pack_used"]);
  const used = result.data.results.some((item) => item.key === "starter_pack_used" && item.value === true);
  return { eligible: !used };
};
