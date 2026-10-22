# Authoritative economy — prepared backend

Status: implemented and tested against a strict conditional-write stand-in; **not enabled in the game or verified
against live UGS**. No credentials or dashboard setup are needed to build or test this code.

## Operations

`ExecuteEconomy` accepts `action=read|execute` and `expectedOwnerId`. The actual owner always comes from authenticated
`context.playerId`; a mismatch fails before storage access. There is no player-selected target, wallet upload,
price override, arbitrary grant, currency adjustment, refund or public initialization operation.

| kind | Operation-specific fields | Result |
|---|---|---|
| `buy_cosmetic` | `itemId` | Server catalog price/level eligibility; includes wardrobe items, avatars and bought clearing styles. Already owned/free/milestone-only/retired/default duplicates cannot be bought again. |
| `buy_booster` | `boosterKind` | Server pack price and quantity: three basic charges or one Bloom Burst. |
| `consume_booster` | `boosterKind`, optional `autoBuy` boolean | Spend one charge. Explicit `autoBuy:true` may buy the pack and consume one charge atomically if empty. Client must obtain purchase consent first. |
| `claim_level` | `level`, `ticketId` | Sequential verified completion. Server calculates normal/clean/hard rewards, unlock charges and regular booster drops. |
| `claim_milestone` | `level` | Completed eligible milestone, largest cadence, current server inventory and charge counts; claimed once permanently. |
| `claim_daily` | `step`, optional `ticketId` | Five ordered steps on the server UTC date; steps 2, 3, 5 require a verified ad ticket. |
| `claim_challenge` | `ticketId` | Verified Daily Challenge: current reward 50 Petals. A verified earlier puzzle may finish offline/overnight; dates only move forward, matching the existing client. |
| `claim_booster_ad` | `ticketId` | Verified daily free-booster ad, once per server day; one of the first three unlocked boosters with fewest charges. |

`boosterKind`: `extraSlot`, `shuffle`, `return`, `bloomBurst`.

Every operation also carries `installationId`, positive safe-integer `sequence` and `catalogVersion`. IDs are bounded
ASCII identifiers. Unknown fields, unsafe numbers and prototype keys are rejected. An installation ID is a correlation
channel, **not authentication**. A fresh install must use a fresh ID and must not reset an existing ID's sequence.

## Atomicity, acknowledgement and replay

One **protected** Cloud Save key, `economy_v1`, holds wallet, boosts, explicitly acquired inventory, verified completion,
permanent milestone/date claims, installation high-water marks and recent receipts. One successful CAS commits all
of them together. Client-writable `player_save_v1` is never imported or consulted for currency/eligibility.

Each initialized mutation loads its write lock and passes that exact nonempty lock to `setProtectedItem`. A 409
causes a fresh read and full evaluation; after four contested attempts the response is retryable `contention`.
A timeout is ambiguous: return retryable and require the same request to be retried. Never mint a new ID to recover
an unknown write result.

New work must have `sequence == installationHighWater + 1`. Same sequence + same canonical payload returns its
original outcome and a **current** authoritative snapshot, without repeating effects. Different payload reusing a
retained sequence is refused. Recent receipts are bounded to 128, but installation high-water marks are permanent:
an evicted receipt returns `receipt-expired` and never re-executes. One-time level/milestone/date eligibility is
independent of the request channel, so another installation cannot reclaim a reward.

Terminal business rejections (insufficient Petals, already owned/claimed, cap failures, etc.) are committed with a
receipt too. If a later reward changes the balance, retrying an earlier failed purchase still returns that failure.
A new purchase needs a new sequence. Failed bundles roll back all provisional effects before remembering rejection.

Responses explicitly distinguish:

- `consumed:true`: the sequence was processed before or in this request. This includes successful operations,
  remembered business rejections, and evicted receipts. Consume that queue entry; use `nextSequence`.
- `consumed:false, retryable:true`: retain and retry the exact pending entry, including after timeouts.
- `consumed:false, retryable:false`: protocol/configuration/input problem, not an acknowledged operation. Do not
  advance the channel merely because `ok` or `retryable` is false. Resolve it deliberately.

Clients apply the returned **snapshot**, scoped to owner/session and monotonic revision; they must never add
`effects` to an existing wallet. Effects are explanatory receipt data, not an additional grant. Retain local
provisional changes separately until acknowledgement, and ignore late responses from a retired profile/session.

## Catalog and economy parity

Build with:

```bash
node tools/backend/build-economy.cjs
node tools/backend/build-economy.cjs --check
node --test backend/tests/*.test.js
```

`catalog.json` and standalone `cloud-code/ExecuteEconomy.js` are generated. The source catalog comes from current
client cosmetics, avatar/clearing declarations, `remote-config/defaults.json`, unlock roadmap, milestone table and
Daily Challenge rules. Build/check fails if the source declaration shape is unsupported or generated files are
stale. Current balance/price values are preserved, not redesigned here. Live Remote Config price overrides do not
silently alter this fixed server catalog; publish an updated catalog and refresh the purchase UI from it.

A catalog content hash identifies the rules used for a new operation. `read` returns the **current** top-level
`catalogVersion` and server item/booster price sheet. Its snapshot `state.catalogVersion` identifies the last applied
catalog. Schema-compatible older state remains readable and old receipts remain replayable. A new operation must
use the current catalog; the successful CAS (including a remembered domain rejection) adopts it without dropping
inventory, money, receipts or claims. A changed price therefore requires refreshing/reconfirming the offer, not
silently executing an old quoted operation at a different price. Changes to the state schema require an explicit
migration and are not covered by this catalog policy.

Free catalog items are implicit entitlements (`catalog.cosmetics[id].free || state.owned.includes(id)`), matching
WardrobeService/ProfileService. They do not have to occupy `owned`; a zero-cost buy request reports already owned.
Presentation filters can still hide locked or default shelf entries.

## Trusted reward authorization

Level/ad/challenge evidence is **not** a client boolean or the current leaderboard's stored command hash. The
endpoint only reads owner-bound protected authorization records created by trusted server issuers after validation.
The issuer is a separate integration, not a public grant endpoint in this implementation.

Key: `economy_auth_<SHA256(ticketId)>`. Use `ticketKey()` from `ugs-store.js` on the issuer side. Hashing avoids
sanitization/truncation collisions. Common fields: `ownerId`, `kind`, `expiresAt` (server milliseconds).

| kind | Additional trusted fields |
|---|---|
| `level` | `level`, `difficulty` (`Normal|Hard|SuperHard`), nonnegative integer `boostersUsed`, current `catalogVersion` |
| `daily-ad` | server UTC `date`, `step` (2/3/5), bound to a verified provider SSV callback and its game placement |
| `challenge` | puzzle UTC `date` no later than server today, current `catalogVersion`, verified win for that puzzle |
| `free-booster-ad` | current server UTC `date`, verified SSV for that specific free-booster placement |

Gameplay validation must bind content/level identity and difficulty and reconcile paid booster usage with actual
server spends, rather than trusting a client-reported zero to obtain the clean bonus. Challenge proof retains the
puzzle date; an unexpired verified ticket can settle an earlier offline attempt. As in the current client, once a
newer challenge date is claimed, an older date cannot be claimed afterward. Dates/ticket expiration come from
trusted server records. Reward eligibility remains in the wallet snapshot even if an issuer accidentally produces
two tickets for the same level/day.

Ad validation may arrive after the client ad callback. An unauthorized claim is a remembered rejection; after actual
authorization arrives, submit a **new** operation, not a changed payload under the rejected sequence. Issuers must
bound/clean up expired ticket records so they do not accumulate indefinitely or exhaust Cloud Save key limits.

## Storage/provider boundary and rollout

The UGS adapter uses the current repository's `@unity-services/cloud-save-1.4` API surface. Exact live SDK behavior,
error shape and deployment permissions still need verification in the configured dev project. A strict stand-in
checks owner scoping, service-token use, protected keys and stale-lock rejection; that is not a live service test.

Missing state or a missing/empty lock fails closed (`needs-initialization` / `missing-write-lock`). There is deliberately
**no null-lock upsert** and no bootstrap endpoint in this script. A separately verified atomic create/provisioning
path must initialize each owner. `emptyState()` is an internal model/test helper, not authority to overwrite an
existing wallet or import an unverified local balance. Retrying provisioning must never reset a funded account.

Before enabling in a public build:

1. Verify provisioning, conditional writes and protected permissions against real UGS, including simultaneous first
   requests. Players must not have a way to set/delete server economy state or issue authorizations.
2. Connect trusted gameplay/ad ticket issuers and defined legacy progress/economy migration. The local APK and current
   client EconomyService remain in the existing offline/dev mode; do not advertise this wallet as connected.
3. Wire a durable per-owner sequential client queue and authoritative snapshot application. On network failure,
   purchases must not fall back to local granting once server authority is enabled. Offline spends remain provisional
   until reconciled; never upload/union the local wallet into `economy_v1`.
4. Implement paid receipt ownership/crediting. Existing `ValidatePurchase` still uses non-atomic global get→set and
   separate starter/claim writes. It does **not** credit this new wallet. Store API verification, unique global claims,
   recoverable crediting/refunds and consumable receipt ownership must be fixed before funded economy rollout.
5. Verify actual billing/devices and reconcile booster refunds rejected by gameplay. No arbitrary client refund is
   exposed here; validated compensation must be a controlled server operation.

Limits: 32 permanent installation cursors, 128 recent receipts, 4096 explicit items, 200 KB per economic snapshot,
nonnegative 32-bit Petals and at most 999999 charges per kind. Caps fail safely; installation retirement or state/schema
migration must preserve replay tombstones/claims, not erase them to regain space. The 2500-level launch and current
catalog fit comfortably; limits are storage safeguards, not new player-facing progression rules.
