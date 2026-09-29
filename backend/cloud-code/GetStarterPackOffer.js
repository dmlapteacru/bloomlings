/**
 * GetStarterPackOffer — UGS Cloud Code (contracts/backend-services.md, research R13; T132).
 *
 * Output: { eligible }. The starter pack is a one-time offer; the flag lives server-side in the player's protected
 * Cloud Save data ("starter_pack_used", set by ValidatePurchase with the service token; players can read but not write
 * protected data), so reinstalling, changing device or editing the save does not bring it back. The client asks before
 * showing the offer.
 */
const { DataApi } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ context }) => {
  const server = new DataApi({ accessToken: context.serviceToken });
  const result = await server.getProtectedItems(context.projectId, context.playerId, ["starter_pack_used"]);
  const used = result.data.results.some((item) => item.key === "starter_pack_used" && item.value === true);
  return { eligible: !used };
};
