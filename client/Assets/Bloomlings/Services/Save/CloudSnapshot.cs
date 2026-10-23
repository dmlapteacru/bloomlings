using System.Text;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>One indivisible snapshot: never combine an inventory with another snapshot's wallet.</summary>
    public static class CloudSnapshot
    {
        public static string Fingerprint(PlayerSave save)
        {
            JObject json = SaveSerializer.ToJObject(save);
            // These describe the local installation/sync, not gameplay changes. Timestamps must not
            // turn a harmless local persist into a two-device conflict or select a winning wallet.
            foreach (string field in new[] { "localPlayerId", "deviceId", "updatedAt", "cloudBaseHash", "linkedIdentity" })
                json.Remove(field);
            return PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(CanonicalJson.Write(json, false)));
        }

        public static void Restore(PlayerSave local, PlayerSave remote)
        {
            if (local.ServerEconomyOwnerId != null || remote.ServerEconomyOwnerId != null)
                throw new System.InvalidOperationException("Authoritative economy profiles cannot use legacy whole-save restore.");
            string id = local.LocalPlayerId;
            string? device = local.DeviceId;
            string? linked = local.LinkedIdentity;
            local.Assign(remote);
            local.LocalPlayerId = id;
            local.DeviceId = device;
            local.LinkedIdentity = linked;
            local.CloudBaseHash = Fingerprint(local);
        }
    }
}
