# Contract: Board Frames → Game Screens

Each of the 17 board frames maps to the screen, component or state that implements it in each build (SC-001). The
level tester APK keeps its minimal view and is not listed (FR-003).

Unity paths are under `client/Assets/Bloomlings/`. Playtest paths are under `playtest/`: the engine-free screens in
`design/`, the Android host in `android/Design/`.

| # | Frame | Unity client | Full playtest | States to show |
|---|---|---|---|---|
| 1 | Splash | `UI/Screens/SplashScreen.cs`, shown by `App/Boot.cs` while loading | `design/HomeScreen.cs` (`SplashScreen`) | wordmark over the backdrop, the four families; no tap |
| 2 | Home (early levels) | `UI/Screens/HomeScreen.cs` | `design/HomeScreen.cs` | Petals pill, Settings, LEVEL N, PLAY; two Bloomlings on a stone |
| 3 | Home (progressed) | same, via `HomeLook` | same | hero, Wardrobe, Collection, "N levels to reward" + gift, "Rank #N >", Daily Challenge card |
| 4 | Daily Reward (popup) | `Meta/DailyReward/DailyRewardPopup.cs` | `design/MetaCards.cs` (`DailyReward`) | Day N, reward basket, +N Petals, CLAIM, ad bonus |
| 5 | Leaderboard (a page since spec 005, 2026-10-04) | `UI/Screens/LeaderboardScreen.cs` | `design/LeaderboardScreen.cs` | medals for 1–3, gap, neighbours, "You" row; offline notice; Refresh |
| 6 | Collection (a page since spec 005, 2026-10-04) | `UI/Screens/CollectionScreen.cs` | `design/CollectionScreen.cs` | framed grid with page arrows, detail with name and "Completed at Level N" |
| 7 | Gameplay (normal) | `UI/Screens/GameplayHud.cs` + `Gameplay/*` views | `design/LevelScreen.cs`, `design/BoardPainter.cs` | top bar, board, 5 slots, tray, booster bar |
| 8 | Gameplay (hard) | same + the HUD badge (`SetDifficulty`) and `DifficultyBanner.cs` | same | red HARD badge under the level pill |
| 9 | Gameplay (super hard) | same | same | SUPER HARD badge, lilac pill, stones, specials |
| 10 | Jam (bottom sheet) | `UI/Screens/JamScreen.cs` | `design/EndCards.cs` (`Jam`) | NO MOVES LEFT, booster tiles with costs, Free rescue, Restart |
| 11 | Pause menu | `UI/Screens/PauseScreen.cs` | `design/MenuCards.cs` (`Pause`, `Settings`) | RESUME, RESTART, SETTINGS, HOME, close |
| 12 | Pod states | `Gameplay/Tray/PodView.cs` | `design/PodPainter.cs` | exposed, next in stack, pressed, locked, mystery, connected |
| 13 | Waiting slot states | `Gameplay/Slots/SlotRowView.cs` | `design/SlotPainter.cs` | empty, working, stuck, locked, danger (4/5), extra |
| 14 | Booster bar | `UI/Gameplay/BoosterBar.cs` | `design/BoosterBarPainter.cs` | hidden < L3; appears per unlock; count badge; price when empty |
| 15 | Win screen | `UI/Screens/WinScreen.cs` | `design/EndCards.cs` (`Win`) | picture, +N Petals, NEXT, "×2 reward" |
| 16 | Milestone win | `UI/Screens/MilestoneCard.cs` | `design/EndCards.cs` (`Milestone`) | LEVEL N, "Milestone reached!", reward icons, CONTINUE |
| 17 | Store (a page since spec 005, 2026-10-04) | `UI/Screens/StoreScreen.cs` | `design/StoreScreen.cs` | Petals pill, rows: icon, name, price with the Petal symbol |

The preview tool renders each playtest frame to `playtest/preview/out/NN-<frame>-<shape>.png` and a contact sheet
(`board-sheet.png`) for the side-by-side review.

## Recorded deviations (FR-001)

1. **Illustrative numbers.**
   - The board's numbers are illustrative: "+12", "+35", "+200", ranks, scores and prices of 40/40/50/60.
   - The game shows its real values.
   - Return unlocks at L6, not L5 (spec 001 FR-042).
2. **Tiles keep variant symbols.** Frames 7–9 draw some tiles with Bloomling faces. Tiles keep variant symbols
   (FR-011, owner's answer); faces stay on pods, slots and walking Bloomlings.
3. **Tile colors.**
   - The board's pale tile tints are not used.
   - Tiles keep the variant colors that passed readability, so the picture-first mosaic shows (research R7).
   - The raised rounded style is applied.
4. **Merge wording.** The board's "Match and merge" is marketing wording. There is no merge mechanic.
5. **Booster bar.**
   - Frame 14 shows the bar's unlock steps.
   - The bar also keeps spec 001's disabled state: a booster the level cannot use now is greyed.
   - It also keeps the targeting highlight for Return and Bloom Burst.
6. **Jam sheet options.**
   - Frame 10 shows three booster options (Slot, Shuffle, Return).
   - The sheet shows only the recovery boosters that are unlocked and eligible (spec 001 FR-027).
   - Bloom Burst is included when eligible.
7. **Home extras.**
   - Frame 3 has no Store or Collection buttons.
   - The Store opens from the "+" on the Petals pill.
   - Collection sits as a small round button next to Wardrobe, in the same style (FR-017).
   - The free-booster ad offer (spec 001 FR-052) is a small secondary button under the rank row.
8. **Store tabs.**
   - Frame 17 shows one list.
   - The Store keeps spec 001's content: Petal packs, boosters, Remove Ads, the starter pack and cosmetics after L40.
   - It keeps two tabs, Shop and Cosmetics, in the board's row style.
9. **Tester.** The level tester APK keeps its minimal look (FR-003).
10. **Win card in Unity.** The Unity win card rises at the bottom, under the finished picture, which is revealed on the
    board itself (spec 001 FR-025). The playtest card shows a small copy of the picture, as frame 15 does.
11. **Playtest without a server, ads or real money.**
    - The Leaderboard shows placeholder rows and the player's own row, not invented players.
    - Ad and real-money buttons show as unavailable.
    - The jam rescue is granted without an ad.
    - The Wardrobe screen and the Daily Challenge are not in the playtest; their Home buttons say so.
12. **PLAY label.** Home shows PLAY at every level, as on the board (the earlier Continue label is gone).
13. **Hero family.** The Home hero is a Bloom Bloomling in the Unity client and a Sprig in the playtest, both in the
    player's outfit. Final art picks the hero.
