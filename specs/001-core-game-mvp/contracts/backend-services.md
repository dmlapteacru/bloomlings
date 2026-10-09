# Contract: Backend and Platform Services

Each online dependency sits behind a client interface, so that providers stay replaceable (doc 15). The initial
providers follow research R11–R14. The core game never calls these services; they are used by the App/Services layer
only.

## Client interfaces

| Interface | Initial provider | Offline behaviour | Spec |
|---|---|---|---|
| `IAuthService` | UGS Authentication: anonymous, with optional Apple / Google Play Games link | Local profile only; retries later | FR-087 |
| `ICloudSaveService` | UGS Cloud Save (key `player_save_v1`, value = player-save.v1) | Queue until online; merge on reconnect (R15) | FR-087, FR-088 |
| `ILeaderboardService` | UGS Leaderboards (`global_highest_level`) | Hidden or stale label; submit on reconnect | FR-062 |
| `IRemoteConfigService` | UGS Remote Config | Bundled defaults, clamped | FR-085 |
| `IPurchaseService` | Unity IAP v5 + Cloud Code validation | Store unavailable, clearly marked | FR-051, FR-054, FR-089 |
| `IAdsService` | Google Mobile Ads + mediation (vendor deferred) | Ads unavailable; ad rescue hidden | FR-052, FR-053 |
| `IConsentService` | Google UMP + Apple ATT | Most restrictive defaults | FR-090 |
| `IAnalyticsService` | Firebase Analytics | Buffered by the SDK | FR-086 |
| `ICrashReporter` | Firebase Crashlytics | Buffered by the SDK | FR-086 |
| `IContentUpdateService` | HTTPS CDN + manifest (R6) | Bundled catalog | FR-078 |

## Leaderboard

- **Id**: `global_highest_level`. It is sorted descending and ranks the whole world (FR-062).
- **Score encoding**: `level × 10_000_000 + (9_999_999 − minutesSince(2026-01-01T00:00Z))`. UGS breaks ties by
  PlayerID, so the earlier completion must be encoded in the score to rank higher. The value fits exactly in a double
  up to about level 900 million. Two completions of the same level within the same minute still tie and fall back to
  the PlayerID order; this is accepted.
- **Submission** happens through Cloud Code function `SubmitProgress(level, contentVersion, commandLogHash)`. The
  sanity checks reject a submission when:
  - the level does not increase monotonically;
  - the level jump is too large for the elapsed time;
  - the content version is not supported.

  Rejected submissions are logged, not banned (doc 15 §23).
- The board unlocks at L10. The client shows the player's rank and a small window of neighbours.

## Cloud Code functions

| Function | Input | Output | Notes |
|---|---|---|---|
| `ValidatePurchase` | platform, receipt, productId | `{valid, transactionId, grants, reason}` | Idempotent by `transactionId` across all players (a consumable's receipt cannot be replayed on another account; Remove Ads may be restored on one), which the client ledger also uses (R13). The client grants `grants`. A second starter pack is refused (`offer-already-used`). Server state lives in custom data and protected player data, written with the service token |
| `SubmitProgress` | level, contentVersion, commandLogHash | `{accepted, rank?}` | Sanity checks as above |
| `GetStarterPackOffer` | – | `{eligible}` | One-time offer flag, in protected player data; asked when the store connects |

## Remote Config keys (defaults bundled; the client clamps to the ranges)

| Key | Default | Range | Spec |
|---|---|---|---|
| `economy.petals.base` | 12 | 5–50 | FR-041 |
| `economy.petals.cleanBonus` | 8 | 0–50 | FR-041 |
| `economy.petals.hardBonus` / `superHardBonus` | 10 / 20 | 0–100 | FR-041 |
| `economy.price.extraSlot` / `shuffle` / `return` / `bloomBurst` | 1200 / 1200 / 1500 / 1800 (40 / 40 / 50 / 60 before 2026-10-05) | 10–5000 (Bloom Burst 10–10000; the client keeps it above the other three, FR-048) | FR-047, FR-048 |
| `economy.price.avatarCommon` / `avatarRare` / `avatarSpecial` | 300 / 600 / 1200 (the owner, 2026-10-05) | 10–10000 | spec 005 FR-037 (the profile's avatar pictures) |
| `economy.price.clearing` | 5000 (the owner, 2026-10-06: every bought clearing style, for now) | 10–50000 | spec 005 FR-038 (the board's clearing styles) |
| `economy.unlockGrant` | 1 | 1–3 | FR-042 |
| `economy.drop.everyLevels` | 5 | 2–20 | FR-047: every Nth completed level grants 1 charge, rotating through the unlocked boosters (no randomness) |
| `daily.reward.step1` / `step2` / `step3` / `step4` / `step5` | 20 / 30 / 40 / 50 / 80 (the owner, 2026-10-09) | 5–500 | FR-055 as amended: the Daily Reward's five steps a day, 2, 3 and 5 after a rewarded ad (they replace `daily.reward.petals` 20 and the streak bonus `streakBonusPetals` / `streakMaxDays` 5 / 7) |
| `ads.interstitial.firstLevel` | 11 | 11–100 | FR-053, SC-013 |
| `ads.interstitial.minSeconds` / `minLevels` | 180 / 3 | 60–1800 / 1–10 | FR-053 |
| `ads.rescue.perAttempt` | 1 | 0–1 | FR-027, FR-048 |
| `feature.dailyChallenge` / `feature.wardrobe` / `feature.leaderboard` | true | bool | FR-062 to FR-064 |
| `content.manifestUrl` | "" | URL | FR-078 |
| `fx.backlogThresholdMs` | 60000 (the owner's calm pace, 2026-10-06; 12000 from 2026-10-05, 6000 from 2026-10-03, 1500 before; R4 amendments) | 2000–120000 | R4 |

Core puzzle rules and level definitions are **not** remotely configurable (FR-085).
