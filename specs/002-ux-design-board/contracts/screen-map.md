# Contract: Board Frames → Game Screens

Each of the 17 board frames maps to the screen, component or state that implements it in each build (SC-001). The
level tester APK keeps its minimal view and is not listed (FR-003).

Unity paths are under `client/Assets/Bloomlings/`. Playtest paths are under `playtest/`: the engine-free screens in
`design/`, the Android host in `android/Design/`.

| # | Frame | Unity client | Full playtest | States to show |
|---|---|---|---|---|
| 1 | Splash | `UI/Screens/SplashScreen.cs` (new), shown by `App/Boot.cs` while loading | `design/SplashScreen.cs` | wordmark over the backdrop; no tap |
| 2 | Home (early levels) | `UI/Screens/HomeScreen.cs` | `design/HomeScreen.cs` | Petals pill, Settings, LEVEL N, PLAY; two Bloomlings on a stone |
| 3 | Home (progressed) | same, `HomeLook` with the unlocked features | same | hero, Wardrobe, "N levels to reward" + gift, "Rank #N >", Daily Challenge card |
| 4 | Daily Reward (popup) | `Meta/DailyReward/DailyRewardPopup.cs` | `design/DailyRewardCard.cs` | Day N, reward basket, +N Petals, CLAIM, "Get +N" (ad) |
| 5 | Leaderboard | `UI/Screens/LeaderboardScreen.cs` | `design/LeaderboardCard.cs` | top 5 with medals, gap, neighbours, "You" row; offline notice |
| 6 | Collection | `UI/Screens/CollectionScreen.cs` | `design/CollectionCard.cs` | framed grid, detail with name and "Completed at Level N" |
| 7 | Gameplay (normal) | `UI/Screens/GameplayHud.cs` + `Gameplay/*` views | `design/LevelScreen.cs` | top bar, board, 5 slots, tray, booster bar |
| 8 | Gameplay (hard) | same + `UI/Screens/DifficultyBanner.cs` → badge | same | red HARD badge under the level pill |
| 9 | Gameplay (super hard) | same | same | SUPER HARD badge, purple pill, stones, specials, locks |
| 10 | Jam (bottom sheet) | `UI/Screens/JamScreen.cs` | `design/JamSheet.cs` | NO MOVES LEFT, booster options with costs, Free rescue, Restart |
| 11 | Pause menu | `UI/Screens/PauseScreen.cs` | `design/PauseCard.cs` | RESUME, RESTART, SETTINGS, HOME, close |
| 12 | Pod states | `Gameplay/Tray/PodView.cs` | `design/PodPainter.cs` | exposed, next in stack, pressed, locked, mystery, connected |
| 13 | Waiting slot states | `Gameplay/Slots/SlotRowView.cs` | `design/SlotPainter.cs` | empty, working, stuck, locked, danger (4/5), extra |
| 14 | Booster bar | `UI/Gameplay/BoosterBar.cs` | `design/BoosterBarPainter.cs` | hidden < L3; appears per unlock; count badge; price when empty |
| 15 | Win screen | `UI/Screens/WinScreen.cs` | `design/WinCard.cs` | picture, +N Petals, NEXT, "×2 reward" |
| 16 | Milestone win | `UI/Screens/MilestoneCard.cs` (new) | `design/MilestoneCard.cs` | LEVEL N, "Milestone reached!", reward icons, CONTINUE |
| 17 | Store | `UI/Screens/StoreScreen.cs` | `design/StoreCard.cs` | Petals pill with +, rows: icon, name, price with Petal |

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
