# Release Checklist

Steps to finish before the game goes to the stores, kept here as the owner asks for them.

## Daily Challenge (the owner, 2026-10-08: "before the release the Daily Challenge must be turned on")

The client shows the Daily Challenge only when all three hold (`DailyChallengeService.IsAvailable`): the player has
unlocked it at L50 (`system.daily_challenge`), the Remote Config flag `feature.dailyChallenge` is on, and the content
has a daily pool.

- [ ] The release content is published with the pool: `publish --catalog content/catalog --daily content/daily`. Today
  the Unity APK workflow (`unity-apk.yml`) publishes only `content/curated`, without `--daily`, so the Daily Challenge
  never appears in that build.
- [ ] `feature.dailyChallenge` is `true` in the live Remote Config environment (the default in
  `backend/remote-config/defaults.json` and `RemoteConfigKeys.DailyChallengeEnabled` is `true`).
- [ ] On a release build, a save past L50 shows the Daily Challenge on Home, plays the day's entry
  (`DailyChallengeService.PoolIndex`) and pays its reward once a day.
