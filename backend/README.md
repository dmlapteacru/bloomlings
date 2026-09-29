# Backend (Unity Gaming Services)

The game is offline-first: everything here is optional at runtime (FR-074), and the client falls back to its bundled
defaults and local save (contracts/backend-services.md).

| Path | What it is |
|---|---|
| `remote-config/defaults.json` | Every Remote Config key with its default and range, mirrored from `RemoteConfigKeys.cs` (a client test keeps them in step). Import these into the UGS Remote Config environment. |
| `cloud-code/ValidatePurchase.js` | Validates a store receipt and answers idempotently by transaction id (FR-089). |
| `cloud-code/GetStarterPackOffer.js` | The one-time starter pack offer flag. |

Deploy the Cloud Code scripts with the UGS CLI (`ugs deploy backend/cloud-code`) and set the secrets named in
`ValidatePurchase.js` in the UGS Secret Manager. These scripts have not been run against a live UGS project yet; check
the module names against the current UGS Cloud Code documentation when deploying.
