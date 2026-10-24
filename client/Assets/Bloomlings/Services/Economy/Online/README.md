# Cached economy and durable client outbox

Prepared C# client component for `backend/cloud-code/ExecuteEconomy.js`. Runtime activation is deliberately **opt-in**:
no boot hook or SDK detection switches the current local APK to a server wallet.
The native .NET Android transport and in-game purchase UI are now prepared; see
[the native-host integration notes](../../../../../../product/plans/native-ugs-purchases-2026-10-23.md). Live UGS provisioning, trusted reward
issuers, billing and host/profile migration are still prerequisites for enabling authoritative economy.

## Offline behavior

- `Catalog` starts from the generated bundled `Resources/ServerEconomyCatalog.json`. A successful read replaces and
  persists the catalog. It remains usable for browsing/quotes across restart and network loss; it has no hard expiry
  that could blank the wardrobe. It contains price/eligibility metadata; existing game assets remain bundled locally.
- `Confirmed` is the last validated authoritative snapshot for this exact owner. `Owns` combines its explicitly owned
  IDs with free catalog entitlements. `Equip` checks the existing cosmetic definitions/family and stores the selection
  locally, including offline. Owned-item switching never needs a new purchase. Previewing an unowned item must stay
  in the existing temporary `WardrobeBrowseState`; it must not call `Equip` or change permanent ownership.
- Purchases (including `consume_booster` with `autoBuy:true`) can start only following an online catalog/state read in
  the current authenticated session and explicit confirmation of an immutable `EconomyQuote`. A cached catalog alone
  cannot authorize a purchase. Offline attempts are refused **before** they receive a queue entry or sequence.
- Cached booster charges can be reserved offline with `QueueBoosterUse`. The read model exposes `AvailableCharges`
  minus pending local reservations. This never auto-buys a pack, changes confirmed Petals or invents booster charges.
  Final spending remains subject to server reconciliation across devices.
- `RecordWin` persists immutable evidence before indexing a reward intent. It does not increase confirmed money.
  `UnsettledRewardIds`/`RewardEvidence` expose pending work to the future trusted verifier. Only after the server
  issuer supplies a ticket does `QueueAuthorizedWin` create the corresponding level/challenge request. This API
  accepts a ticket ID, not player-selected grant amounts; the server validates it.

## Crash/retry protocol

`EconomyJournal` uses one hashed directory and exclusive writer lock per owner. Installation ID, next sequence,
exact serialized requests, attempted/uncertain flags, prior payload alternatives, cached snapshot/catalog and outfit
choices are stored in a checksum envelope via flushed temp file and atomic replacement, with a backup.

A request is durable **before** dispatch, and an attempted flag is durable before the network coroutine runs.
`Flush` retries an attempted request byte-for-byte after a timeout/restart. It never creates a new identity or sequence
to recover an unknown result. Network coroutines are bounded and failed nested coroutines become retryable outcomes.

If an app closes before dispatch, automatic `Flush` stops at `confirmation-required`; a fresh online read and
`ConfirmPendingPurchase` are required. A purchase intent is not silently executed hours later simply because the
connection returned. After a known dispatch and lost reply, exact replay is allowed without another confirmation.

A `consumed:true` response must contain a valid owner-scoped snapshot and server cursor. Cached revisions never
move backward; equal revisions must have identical state. Snapshot replacement, queue removal and cursor update
are one journal commit. The client never adds response `effects` to a wallet; those effects are already in the
snapshot. Failure to persist acknowledgement leaves the same replayable request in place.

A retryable/unavailable response leaves the queue unchanged. A definitive unconsumed error stores a block reason
and **does not advance** the channel. A read never drops pending operations or rewinds a local cursor. The next
purchase stays unavailable until preceding work is reconciled.

## Catalog changes and blocked work

- After `catalog-changed`, refresh the server catalog, show its new price, and call `ConfirmPendingPurchase` only
  following new player confirmation. It reuses the unconsumed sequence but keeps prior serialized payloads.
- For nonpurchase work, `ReconcileNonPurchase` allows rebinding after a successful current-session read. Level and
  challenge claims require a fresh trusted ticket; a booster reservation needs no paid purchase confirmation.
- Unknown outcomes and recovered uncertain entries are replayed exactly until a definitive rejection permits a
  replacement. If an earlier payload won a race, old payload alternatives recover its receipt instead of discarding
  an acknowledged transaction. At most four prior payloads are retained; further unusual divergence blocks for
  manual reconciliation.
- A recovered backup cannot prove a request was never sent. Its pending entries are atomically marked uncertain
  before recovery returns. Paid entries need reconfirmation unless a fresh server cursor proves they were already
  processed (in that case replay retrieves the result without a new purchase). Recovery never exposes a raw,
  apparently trusted backup as main before these flags are durable.

Missing/corrupt journals in an existing owner directory fail closed. They never regenerate installation identity.
A first initialization writes identity before any read/execute. Two writers for one owner are refused. The owner
folder/lock is therefore part of durable identity: do not delete it as a generic cache-clearing operation. Cached
price metadata is replaceable; the outbox is not disposable cache.

Reward evidence lives in separate immutable SHA-256-named files (up to 1 MiB each). Pending evidence is not deleted on
transport failures or an unauthorized-ticket rejection, so a later verified ticket can settle the same win. The index
is limited to 2 MiB, 128 pending server operations and 10000 reward intents. Existing evidence is retained at this
stage; rollout should add explicit retention/pruning of settled records without deleting pending work or receipt
recovery data. No proof upload/verification is faked by this component.

## Composition / remaining host work

Construct `EconomyJournal` with the bound/previously bound **UGS owner**, generated bundled catalog and an app-data
root; then construct `OnlineEconomyClient` with an `IEconomyTransport`. `OfflineEconomyTransport` supports cached
browsing. The guarded Unity `UgsEconomyTransport` and engine-free `UgsHttpEconomyTransport` target the real endpoint but are not registered
by default. The .NET APK compiles the HTTP transport directly, without Unity SDK packages. Its authenticated memory-only
`UgsPlayerSession` must come from a native authentication provider; it cannot be created from a local profile ID. No Unity/Google credentials are embedded.

A new guest without a backend owner stays in the existing local/dev game. On first real binding, preserve guest
progress/evidence through the explicit account migration; do not copy a journal into arbitrary platform identities.
An active client's owner is immutable. On profile changes call `InvalidateSession`, dispose it, and compose a client
for the new owner. The transport must check both owner and auth session before dispatch and before callbacks;
late responses (including A → B → A) cannot change the new profile.

For server mode, views use the cached authoritative catalog/balance/ownership and local outfit selection; purchase
callbacks run `Purchase` and observe pending/confirmed state. Preserve existing temporary wardrobe previews and game
art/layout. Do **not** route failed server purchases into current local `EconomyService.TrySpend` or apply a whole
legacy cloud-save wallet over this snapshot. All spending/grant flows must be migrated consistently before enabling
the mode, including daily/milestone/ad/billing flows; the current typed outbox implements purchases, booster use and
verified level/challenge claims. It is not a replacement boot/gameplay integration by itself.

Call `Refresh`/`Flush` at controlled lifecycle points after sign-in, on returning to Home and on connectivity recovery;
no tight automatic retry loop. If `Flush` ends `queued` after its 32-request budget, schedule a subsequent pass outside
the current coroutine. Receipt-expired rewards remain unsettled unless their result is known; retain evidence and
reconcile with the verifier rather than inventing a local grant.

## Verification

The generator emits the bundled public catalog and a C# fixture from the real JS economy reducer. Node's existing
build-consistency test checks them along with the deployable backend. .NET tests parse that fixture and exercise an
owner/sequence-aware transport, network loss, crash before dispatch/ACK, reprice confirmation, partial/corrupt storage,
exclusive writers, stale snapshots, exact replay, separate owners and offline reward evidence.

```bash
node tools/backend/build-economy.cjs --check
node --test backend/tests/*.test.js
DOTNET_CLI_HOME=/workspace/.dotnet-home /workspace/.dotnet/dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj --no-restore --filter 'FullyQualifiedName~OnlineEconomyClientTests|FullyQualifiedName~AccountCloudSyncTests|FullyQualifiedName~EconomyServiceTests|FullyQualifiedName~Wardrobe|FullyQualifiedName~SaveServiceTests'
```

The .NET suite compiles engine-free client logic against Unity stubs. It does not compile the conditionally enabled
UGS adapter against real SDK packages or prove live UGS/device behavior. No APK is built for this task.

Validation result for this revision: **106 client tests passed, 45 backend tests passed**, no failures/skips; generated files and `git diff --check` are current. Astra independently reviewed ACK/reprice/recovery behavior; reported issues were fixed and regression-tested.


## In-game purchase UI (2026-10-23)

`OnlinePurchaseOperation` implements `IPurchaseOperation` for the shared `PurchasePanel`. It refreshes prices, requires
explicit confirmation, identifies a consumed result by sequence and success flag, and replays only the exact pending
request on Check. An operation is bound to its construction session; a coroutine queued before a session change may
not spend under the new session. No legacy purchase callback is retained by this adapter.

`PendingPurchase` describes the head purchase for a host's recovery entry. Dismissing the panel preserves the outbox;
it does not refund or discard a request. `cancel_purchase` now atomically competes for the original sequence; it
never rewinds the cursor or refunds an already completed purchase. `UiRoutineQueue` supplies a bounded UI-thread runner for the
native host. The game's existing local/dev mode uses `LocalPurchaseOperation` explicitly, with real network gating,
live local prices and explicit success/failure; it is never selected as a fallback from this adapter.

## Authoritative host mode and evidence (2026-10-23)

`ServerEconomy` composes one authoritative projection for the native host: prices, wallet, reservations, inventory,
daily and milestone states. Local grants/spends and tester reset are disabled for that profile. A separate durable
save seal prevents a legacy backup or corrupt save from silently reopening server money as a local wallet. First
activation requires a verified provisioned snapshot and an explicit approved-migration host gate; this does not
implement bootstrap or migrate a legacy wallet. See [current rollout and unresolved live gates](../../../../../../product/plans/server-economy-and-ranking-2026-10-23.md).

Pending purchase review can be opened without replay, checked or cancelled. Cancellation preserves the original
booster reservation until its terminal acknowledgement. Review/confirmation/cancellation stop at their own
sequence so later queued operations cannot overwrite the result presented by that window.

`WinEvidence` snapshots accepted commands and trusted-content fingerprints, and can replay them against caller-supplied
trusted content. It is bounded preflight, not authorization or proof of paid booster ownership. Native first wins
are archived before progression; server mode stores reward intents without minting money. `GuestWinArchive` keeps
immutable checksum-checked first wins for future migration. Earlier guest wins do not acquire evidence retroactively.
Actual replay/ticket issuance, attempt-bound booster recovery, billing/SSV and non-economic progress sync remain off.
