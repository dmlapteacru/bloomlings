# Backend (Unity Gaming Services)

The game is offline-first: everything here is optional at runtime (FR-074), and the client falls back to its bundled
defaults and local save (contracts/backend-services.md).

| Path | What it is |
|---|---|
| `remote-config/defaults.json` | Every Remote Config key with its default and range, mirrored from `RemoteConfigKeys.cs` (a client test keeps them in step). Import these into the UGS Remote Config environment. |
| `cloud-code/ExecuteEconomy.js` | Generated authoritative wallet operations; protected snapshot, conditional writes and replay protection. See [economy/README.md](economy/README.md) for supported operations, verification and rollout gates. |
| `cloud-code/ValidatePurchase.js` | Validates a store receipt and claims its transaction once for the whole game, so a receipt cannot be replayed on another account (FR-089). |
| `cloud-code/GetStarterPackOffer.js` | The one-time starter pack offer flag, kept in protected player data. |
| `cloud-code/SubmitProgress.js` | Leaderboard submission with the sanity checks (monotonic level, plausible jump, supported content version) and the time-encoded score (FR-062). Rejections are logged, not banned. |

Create the leaderboard `global_highest_level` in the UGS dashboard: sort **descending**, update type **keep best**,
no reset. Players write their scores only through `SubmitProgress` (the client never writes the score itself); add a
project **Access Control** policy that denies the `Player` principal writes to the Leaderboards scores, so a modified
client cannot post a score directly. The player save lives in Cloud Save player data under `player_save_v1`.

The new server economy is prepared but not enabled in either mobile host. Its wallet must remain authoritative once
connected; a network failure must not fall back to a local grant. Existing `ValidatePurchase` global receipt claims
use non-atomic get→set and must not be treated as safe credits for the new wallet before the billing work described
in [economy/README.md](economy/README.md).

Server-owned state is never in player-writable data:

| Where | Key | Written by |
|---|---|---|
| Custom data `purchases` (game-wide) | `tx_<transactionId>` | `ValidatePurchase` (service token) |
| Protected player data | `economy_v1` | `ExecuteEconomy`, initialized through a separately verified provisioning path |
| Protected player data | `economy_auth_<SHA256(ticketId)>` | Trusted gameplay/ad validators (issuers pending integration) |
| Protected player data | `starter_pack_used` | `ValidatePurchase` (service token) |
| Protected player data | `leaderboard_progress` | `SubmitProgress` (service token) |

Raise `MAX_CONTENT_VERSION` in `SubmitProgress.js` before publishing content beyond it.

`node --test backend/tests/*.test.js` runs the scripts against in-memory stand-ins for the UGS modules (CI runs it too).

Deploy the Cloud Code scripts with the UGS CLI (`ugs deploy backend/cloud-code`) and set the secrets named in
`ValidatePurchase.js` in the UGS Secret Manager. These scripts have not been run against a live UGS project yet; check
the module names against the current UGS Cloud Code documentation when deploying.


### Leaderboard profile presentation

Deploy both `SubmitProgress` and `GetLeaderboardAppearances` when enabling the new honor board. `SubmitProgress` accepts optional `appearanceJson` with avatar/frame/badge/marker IDs and the chosen profile name. A submission at the already accepted level changes the presentation while preserving the stored score and completion timestamp. Old clients without appearance metadata retain their original progress behavior.

`GetLeaderboardAppearances` returns only public cosmetic identifiers/name for up to 24 requested player IDs. Its data comes from protected `leaderboard_progress`, so profile edits remain current even when the leaderboard's Keep Best strategy retains the metadata of an equal score. No private save or purchase details are exposed. The client requests top three separately from the neighbour window, joins their saved appearance by player ID and falls back to existing score metadata when the appearance endpoint is unavailable. Legacy rows with no known avatar show a neutral portrait.

The code and in-memory backend tests have been checked locally; live UGS deployment/SDK execution still needs the configured UGS project. Run the backend checks with `node --test backend/tests/*.test.js`.
