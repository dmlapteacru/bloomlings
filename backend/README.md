# Backend (Unity Gaming Services)

The game is offline-first: everything here is optional at runtime (FR-074), and the client falls back to its bundled
defaults and local save (contracts/backend-services.md).

| Path | What it is |
|---|---|
| `remote-config/defaults.json` | Every Remote Config key with its default and range, mirrored from `RemoteConfigKeys.cs` (a client test keeps them in step). Import these into the UGS Remote Config environment. |
| `cloud-code/ValidatePurchase.js` | Validates a store receipt and claims its transaction once for the whole game, so a receipt cannot be replayed on another account (FR-089). |
| `cloud-code/GetStarterPackOffer.js` | The one-time starter pack offer flag, kept in protected player data. |
| `cloud-code/SubmitProgress.js` | Leaderboard submission with the sanity checks (monotonic level, plausible jump, supported content version) and the time-encoded score (FR-062). Rejections are logged, not banned. |

Create the leaderboard `global_highest_level` in the UGS dashboard: sort **descending**, update type **keep best**,
no reset. Players write their scores only through `SubmitProgress` (the client never writes the score itself); add a
project **Access Control** policy that denies the `Player` principal writes to the Leaderboards scores, so a modified
client cannot post a score directly. The player save lives in Cloud Save player data under `player_save_v1`.

Server-owned state is never in player-writable data:

| Where | Key | Written by |
|---|---|---|
| Custom data `purchases` (game-wide) | `tx_<transactionId>` | `ValidatePurchase` (service token) |
| Protected player data | `starter_pack_used` | `ValidatePurchase` (service token) |
| Protected player data | `leaderboard_progress` | `SubmitProgress` (service token) |

Raise `MAX_CONTENT_VERSION` in `SubmitProgress.js` before publishing content beyond it.

`node --test backend/tests/*.test.js` runs the scripts against in-memory stand-ins for the UGS modules (CI runs it too).

Deploy the Cloud Code scripts with the UGS CLI (`ugs deploy backend/cloud-code`) and set the secrets named in
`ValidatePurchase.js` in the UGS Secret Manager. These scripts have not been run against a live UGS project yet; check
the module names against the current UGS Cloud Code documentation when deploying.
