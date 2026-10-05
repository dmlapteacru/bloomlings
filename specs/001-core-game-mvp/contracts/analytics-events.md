# Contract: Analytics Events

These events feed difficulty tuning and business metrics (FR-086, SC-006, SC-007, SC-010) through `IAnalyticsService`.

**Common parameters** on every event: `app_version`, `content_version`, `player_level` (highest completed + 1),
`session_id`.

**Level parameters**, attached where a level is involved: `level_number`, `definition_version`, `difficulty_class`,
`picture_id`.

## Gameplay

| Event | When | Extra parameters |
|---|---|---|
| `level_start` | Level loaded and playable | `attempt_index` |
| `level_win` | `LevelWon` | `duration_ms`, `taps`, `boosters_used` (without the free guided use), `clean_clear`, `peak_slots`, `attempt_index` |
| `level_jam` | `LevelJammed` or `LevelStuck` | `kind` (`jam`/`stuck`), `duration_ms`, `taps`, `slots_used`, `remaining_work` |
| `level_recover` | Recovery used at a jam | `method` (`extra_slot`/`shuffle`/`return`/`bloom_burst`/`ad_rescue`; Shuffle recovers a stuck board) |
| `level_restart` | Restart | `from` (`pause`/`jam`) |
| `level_quit` | Leave the level from pause | `duration_ms` |
| `booster_use` | Booster command accepted | `booster`, `source` (`charge`/`petals`/`ad`/`demo`: the free guided use at its unlock, spec 005 FR-035) |
| `tutorial_step` | Demonstration step shown or completed | `unlock_id`, `step`, `completed` |

## Progression and meta

| Event | When | Extra parameters |
|---|---|---|
| `unlock` | Roadmap unlock reached | `unlock_id`, `kind` |
| `milestone_claim` | Milestone reward granted | `milestone_level`, `bundle_id` |
| `daily_reward_claim` | Daily reward claimed | `streak` |
| `daily_challenge_complete` | Daily challenge won | `utc_date` |
| `leaderboard_view` | Leaderboard opened | `rank` |
| `cosmetic_equip` | Skin equipped | `family`, `skin_id` |
| `collection_open` | Collection opened | `entries` |

## Monetization

| Event | When | Extra parameters |
|---|---|---|
| `store_open` | Store opened | `from` |
| `purchase` | Validated purchase | `product_id`, `price_micros`, `currency`, `transaction_id` |
| `ad_rewarded` | Rewarded ad completed | `placement` (`rescue`/`free_booster`/`double_reward`/`daily`) |
| `ad_interstitial` | Interstitial shown | `levels_since_last`, `seconds_since_last` |
| `consent` | Consent result | `gdpr`, `att` |

## Content quality signals

| Event | When | Extra parameters |
|---|---|---|
| `content_update` | New manifest activated | `from_version`, `to_version` |
| `content_error` | Pack hash mismatch or level load failure | `pack_id`, `level_number`, `error` |

Crash reports carry the custom keys `app_version`, `content_version`, `level_number`, `definition_version` and
`picture_id` (R14).
