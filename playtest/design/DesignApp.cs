using System;
using System.Collections.Generic;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The full playtest's screens (spec 002 contracts/screen-map.md).</summary>
    public enum Screen
    {
        Splash,
        Home,
        Level,
        Wardrobe,
        Store,

        /// <summary>The Leaderboard page (spec 005 FR-030, contracts/look.md §6.8), a place of the bottom menu.</summary>
        Leaderboard,

        /// <summary>The Collection page (spec 005 FR-030, contracts/look.md §6.9), a place of the bottom menu.</summary>
        Collection,

        /// <summary>The profile page (spec 005 FR-037), opened from Home's avatar.</summary>
        Profile,
    }

    /// <summary>The cards shown over a screen.</summary>
    public enum Overlay
    {
        Pause,
        Settings,
        DailyReward,

        /// <summary>The Remove Ads card (spec 005 FR-033), opened from Home's No Ads scene at every level.</summary>
        RemoveAds,

        /// <summary>The profile's "Edit profile" card (spec 005 FR-037), over the profile page.</summary>
        ProfileEdit,

        /// <summary>The purchase confirmation (spec 005 FR-040), over whatever asked: nothing is spent until its Buy.</summary>
        Purchase,
    }

    /// <summary>
    /// The host's text entry (the profile's name, spec 005 FR-037): the APK asks with the system's text dialog and calls
    /// <c>done</c> with the typed text on the UI thread, or not at all when it is cancelled; the preview answers at once.
    /// </summary>
    public interface ITextPrompt
    {
        void Ask(string title, string text, int maxLength, Action<string> done);
    }

    /// <summary>
    /// The full playtest in the design board's look (spec 002 FR-003):
    /// <list type="bullet">
    /// <item><description>the splash;</description></item>
    /// <item><description>Home (frames 2 and 3);</description></item>
    /// <item><description>the level (frames 7–14);</description></item>
    /// <item><description>the Wardrobe (preview frame 27; spec 005 FR-025), opened from the bottom menu;</description></item>
    /// <item><description>the Store page (preview frames 17 and 26; spec 005 FR-029), opened from the bottom menu or a
    /// Petals pill's "+";</description></item>
    /// <item><description>the Leaderboard page (preview frames 5 and 31) and the Collection page (frames 6 and 20; spec
    /// 005 FR-030, the owner's request of 2026-10-04: "All the menu's places must be a separate page. Not popups."), opened
    /// from the bottom menu;</description></item>
    /// <item><description>the bottom menu on Home and the four pages (spec 005 FR-030, <see cref="Navigate"/>), its five
    /// places always shown: a locked one with a padlock, its page saying from which level it is available;</description></item>
    /// <item><description>the profile page (preview frame 39; spec 005 FR-037), opened from Home's avatar, and its edit
    /// card (frames 40 and 41);</description></item>
    /// <item><description>the cards over them (frames 4, 10, 11, 15 and 16; the Remove Ads card of Home's No Ads scene,
    /// preview frame 32).</description></item>
    /// </list>
    /// The very first launch goes straight into Level 1, and later launches open Home (spec 001 US2). Progress, Petals,
    /// booster charges, milestones, the Daily Reward and the Collection come from the Unity client's engine-free
    /// services (<see cref="PlaytestMeta"/>). This class only navigates and draws. Engine-free: the APK hosts it in a
    /// <c>DesignView</c>, and the preview tool renders it to PNG files.
    /// </summary>
    public sealed class DesignApp
    {
        /// <summary>
        /// When the splash's lotus iris started to open on the first screen (spec 005 FR-039), or −1 while it loads: the
        /// splash fills its ring of petals (the playtest has loaded everything before it starts), then the iris opens from
        /// the lotus. It never waits for a tap (spec 002 FR-016).
        /// </summary>
        private float _splashOpenSince = -1f;

        /// <summary>When the lotus iris between two levels started (the win's Next, spec 005 FR-039), or −1.</summary>
        private float _transitionSince = -1f;

        /// <summary>The level the transition shows ("Level N") and starts under its closed cover.</summary>
        private int _transitionLevel;

        /// <summary>Whether the transition's level has started under the cover.</summary>
        private bool _transitionSwitched;

        private readonly string _dataFolder;
        private readonly List<(Overlay Overlay, float OpenedAt)> _overlays = new List<(Overlay, float)>();
        private readonly bool _firstLaunch;

        public DesignApp(string dataFolder, ContentSet content, ISoundOut sound, bool showSplash = true)
        {
            _dataFolder = dataFolder;
            Content = content;
            Sound = sound;
            Meta = new PlaytestMeta(dataFolder);
            _firstLaunch = Meta.FirstLaunch;
            Screen = showSplash ? Screen.Splash : Screen.Home;
            StartHomeMotion();
            if (!showSplash)
            {
                AfterSplash();
            }
        }

        public ContentSet Content { get; }

        public ISoundOut Sound { get; }

        /// <summary>The host's text entry for the profile's name; none in a host without one (the name then stays).</summary>
        public ITextPrompt? TextPrompt { get; set; }

        /// <summary>The open edit card's state (spec 005 FR-037), made when the card opens.</summary>
        public ProfileEditor? ProfileEditor { get; private set; }

        public PlaytestMeta Meta { get; private set; }

        public Screen Screen { get; private set; }

        public LevelScreen? Level { get; private set; }

        /// <summary>Seconds since launch (menu motion and toasts).</summary>
        public float Now { get; private set; }

        /// <summary>
        /// Home's four heroes in motion (spec 005 FR-028), on <see cref="Now"/>'s clock: made when the splash or Home
        /// appears (the splash's carries on into Home, so nothing jumps), updated every drawn frame and tapped by Home's
        /// hero targets.
        /// </summary>
        public HomeMotion HomeMotion { get; private set; } = null!;

        /// <summary>
        /// Whether Home's last frame drew animated heroes or promo scenes (then Home keeps redrawing under a card too).
        /// </summary>
        public bool HomeMoving { get; set; }

        /// <summary>
        /// Seconds since Home was last shown (the clock of its promo scenes, spec 005 FR-032, <see cref="HomePromo.Layers"/>):
        /// from Home's first frame, also after the splash.
        /// </summary>
        public float PromoSeconds => Now - _promoOpenedAt;

        private float _promoOpenedAt;

        /// <summary>The Collection page's opened picture (an index into its entries), or −1 for its grid.</summary>
        public int CollectionDetail { get; set; } = -1;

        /// <summary>The Collection page's page of pictures.</summary>
        public int CollectionPage { get; set; }

        /// <summary>The Store's selected tab (0 shop, 1 cosmetics).</summary>
        public int StoreTab { get; set; }

        /// <summary>The Store cosmetics' selected family tab (an index into the four families, spec 005 §4.6).</summary>
        public int StoreFamily { get; set; }

        /// <summary>The Store cosmetics' page of outfit cards.</summary>
        public int StorePage { get; set; }

        /// <summary>The Store Shop's page of rows (when the rows take more than the page).</summary>
        public int StoreRowsPage { get; set; }

        /// <summary>
        /// The screen the Store page returns to: Home, or the page whose bottom menu or Petals "+" opened it (the Wardrobe,
        /// the Leaderboard or the Collection).
        /// </summary>
        public Screen StoreReturn { get; private set; } = Screen.Home;

        /// <summary>The Wardrobe's chosen family (an index into the four families, spec 005 §6.5).</summary>
        public int WardrobeFamily { get; set; }

        /// <summary>The Wardrobe's page of outfit cards.</summary>
        public int WardrobePage { get; set; }

        public IReadOnlyList<(Overlay Overlay, float OpenedAt)> Overlays => _overlays;

        /// <summary>
        /// The purchase waiting for the player's answer (spec 005 FR-040, the owner's request of 2026-10-06: "Every purchase
        /// needs a confirmation popup"), shown by <see cref="Overlay.Purchase"/>.
        /// </summary>
        public PurchaseConfirmation Purchase { get; } = new PurchaseConfirmation();

        /// <summary>The asked item's picture in the confirmation's well, drawn each frame; none for a plain card.</summary>
        public Action<IPainter, Box>? PurchasePicture { get; private set; }

        private Action? _purchaseCancelled;

        /// <summary>
        /// Whether the Store page's last frame drew the Animations tab, whose live previews loop all the time (spec 005
        /// FR-038 as amended on 2026-10-06): the host keeps drawing frames for them.
        /// </summary>
        public bool StoreMoving { get; set; }

        /// <summary>Whether the card being drawn lies under another open card (it then shows no close button of its own).</summary>
        public bool DrawingCovered { get; private set; }

        /// <summary>The close action of the card being drawn: none for a covered card, so only the top card has a ✕.</summary>
        public Action? CardClose => DrawingCovered ? null : CloseOverlay;

        /// <summary><paramref name="close"/>, or none when the card being drawn is covered by another card.</summary>
        public Action? CardCloseWith(Action close) => DrawingCovered ? null : close;

        /// <summary>
        /// Whether the host should draw another frame soon: animations, the splash, toasts, the one breathing button
        /// (PLAY on Home, CLAIM on the Daily Reward; spec 003 FR-019) and Home's animated heroes, which keep moving under
        /// a card too (spec 005 FR-028).
        /// </summary>
        public bool NeedsFrames =>
            Screen == Screen.Splash
            || SplashOpening
            || Transitioning
            || (Level != null && Screen == Screen.Level && Level.NeedsFrames)
            || (_overlays.Count > 0 && Now - _overlays[_overlays.Count - 1].OpenedAt < 0.4f)
            || (_homeToastUntil > Now)
            || (Screen == Screen.Home && (_overlays.Count == 0 || HomeMoving))
            || (Screen == Screen.Store && StoreMoving)
            || IsOpen(Overlay.DailyReward)
            || IsOpen(Overlay.Purchase);

        /// <summary>
        /// Whether the only motion left is slow: the open win or milestone card's (turning rays, falling petals, Next
        /// breathing, the hero's 24 fps frames), or Home's heroes and petals under a settled card (but the Daily Reward's
        /// breathing CLAIM). The host may then draw at about 30 frames a second instead of every display frame, to save
        /// battery.
        /// </summary>
        public bool Calm =>
            (_overlays.Count == 0 || Now - _overlays[_overlays.Count - 1].OpenedAt >= 0.4f)
            && _homeToastUntil <= Now
            && ((Screen == Screen.Level && Level != null && Level.OnlyCelebrating)
                || (Screen == Screen.Home && _overlays.Count > 0 && !IsOpen(Overlay.DailyReward))
                || (Screen == Screen.Store && StoreMoving && _overlays.Count == 0));

        private string? _homeToast;
        private float _homeToastUntil;

        /// <summary>A level number the playtest can play: its own content, repeating past the last level.</summary>
        public int Resolve(int levelNumber) =>
            Content.TryGetLevel(levelNumber, out _) ? levelNumber : Content.LevelNumbers[(levelNumber - 1) % Content.LevelCount];

        // ---- Navigation ----

        private void AfterSplash()
        {
            if (_firstLaunch)
            {
                StartLevel();
            }
            else
            {
                GoHome();
            }
        }

        public void StartLevel() => LoadLevel(Meta.CurrentLevel);

        /// <summary>
        /// The win's Next (spec 005 FR-039): the lotus iris closes over the win, the next level starts under its closed
        /// cover with "Level N" on it, and the iris opens on the level.
        /// </summary>
        public void NextLevel()
        {
            if (Transitioning)
            {
                return;
            }

            _transitionSince = Now;
            _transitionLevel = Meta.CurrentLevel;
            _transitionSwitched = false;
            Sound.Play(SoundCue.Click);
        }

        /// <summary>Whether the lotus iris between two levels shows.</summary>
        public bool Transitioning => _transitionSince >= 0f;

        /// <summary>Whether the splash's iris is opening on the first screen.</summary>
        public bool SplashOpening => _splashOpenSince >= 0f;

        public void LoadLevel(int levelNumber)
        {
            ClearOverlays();
            Level = new LevelScreen(this, levelNumber);
            Screen = Screen.Level;
        }

        public void GoHome()
        {
            // Home's heroes start over, unless Home follows the splash, whose heroes already move.
            if (Screen != Screen.Splash)
            {
                StartHomeMotion();
            }

            ClearOverlays();
            Level = null;
            Screen = Screen.Home;
            _promoOpenedAt = Now;
            if (Meta.DailyReward.CanClaim)
            {
                OpenOverlay(Overlay.DailyReward);
            }
        }

        /// <summary>Opens the Wardrobe over Home (spec 005 FR-025) on its first page.</summary>
        public void OpenWardrobe()
        {
            Sound.Play(SoundCue.Click);
            ClearOverlays();
            WardrobePage = 0;
            Screen = Screen.Wardrobe;
        }

        /// <summary>Back from the Wardrobe to Home (without reopening the Daily Reward).</summary>
        public void CloseWardrobe() => EnterHome(click: true);

        /// <summary>Home from a page (the Wardrobe, the Store, the Leaderboard or the Collection), without reopening the Daily Reward; its heroes start over.</summary>
        private void EnterHome(bool click)
        {
            if (click)
            {
                Sound.Play(SoundCue.Click);
            }

            ClearOverlays();
            StartHomeMotion();
            Screen = Screen.Home;
        }

        /// <summary>
        /// The bottom menu's place of the screen now (spec 005 FR-030): the Shop on the Store page, the Wardrobe on the
        /// Wardrobe, the Leaderboard and the Collection on their pages, else Home.
        /// </summary>
        public NavPlace ActivePlace => Screen switch
        {
            Screen.Store => NavPlace.Shop,
            Screen.Wardrobe => NavPlace.Wardrobe,
            Screen.Leaderboard => NavPlace.Leaderboard,
            Screen.Collection => NavPlace.Collection,
            _ => NavPlace.Home,
        };

        /// <summary>
        /// Whether a place of the bottom menu is open now (<see cref="BottomNav.IsOpen"/> with Home's look): a locked one
        /// still shows and opens its page, which says from which level it is available (<see cref="UnlockLevel"/>).
        /// </summary>
        public bool PlaceOpen(NavPlace place) => BottomNav.IsOpen(place, HomeScreen.Look(this));

        /// <summary>The level a locked place's page names: its unlock's level in the progression's own roadmap (<see cref="BottomNav.UnlockLevel"/>).</summary>
        public int UnlockLevel(NavPlace place) => BottomNav.UnlockLevel(place, Meta.Progression.Roadmap.LevelOf);

        /// <summary>
        /// A tap on a place of the bottom menu (spec 005 FR-030), with the click, straight from any page: the Shop opens the
        /// Store page (<see cref="OpenStore"/>, its back returning to the page it was opened from), the Wardrobe the
        /// Wardrobe, Home returns to Home, the Leaderboard and the Collection open their pages (<see cref="OpenLeaderboard"/>,
        /// <see cref="OpenCollection"/>; the owner's request of 2026-10-04: "All the menu's places must be a separate page.
        /// Not popups."). The active place does nothing. A locked place opens its page all the same, which then shows the
        /// locked notice instead of its content (<see cref="PlaceOpen"/>; the owner's request of 2026-10-04).
        /// </summary>
        public void Navigate(NavPlace place)
        {
            if (place == ActivePlace)
            {
                return;
            }

            switch (place)
            {
                case NavPlace.Shop:
                    OpenStore();
                    break;
                case NavPlace.Wardrobe:
                    OpenWardrobe();
                    break;
                case NavPlace.Leaderboard:
                    OpenLeaderboard();
                    break;
                case NavPlace.Collection:
                    OpenCollection();
                    break;
                default:
                    EnterHome(click: true);
                    break;
            }
        }

        /// <summary>Opens the Leaderboard page (spec 005 FR-030, contracts/look.md §6.8), locked before L10.</summary>
        public void OpenLeaderboard()
        {
            Sound.Play(SoundCue.Click);
            ClearOverlays();
            Screen = Screen.Leaderboard;
        }

        /// <summary>Back from the Leaderboard page to Home (without reopening the Daily Reward).</summary>
        public void CloseLeaderboard() => EnterHome(click: true);

        /// <summary>
        /// Opens the Collection page (spec 005 FR-030, contracts/look.md §6.9) on its grid's first page, locked before its
        /// first picture.
        /// </summary>
        public void OpenCollection()
        {
            Sound.Play(SoundCue.Click);
            ClearOverlays();
            CollectionDetail = -1;
            CollectionPage = 0;
            Screen = Screen.Collection;
        }

        /// <summary>
        /// Back on the Collection page (its back button and the system back): from a picture's detail to the grid, from the
        /// grid to Home (without reopening the Daily Reward).
        /// </summary>
        public void CollectionBack()
        {
            if (CollectionDetail >= 0)
            {
                Sound.Play(SoundCue.Click);
                CollectionDetail = -1;
                return;
            }

            EnterHome(click: true);
        }

        /// <summary>
        /// Opens the Store page (spec 005 FR-029) on its first pages: from Home (the bottom menu's Shop, the Petals pill's
        /// "+") or from the Wardrobe, the Leaderboard or the Collection page (the same two), where its back returns
        /// (<see cref="StoreReturn"/>).
        /// </summary>
        public void OpenStore()
        {
            Sound.Play(SoundCue.Click);
            StoreReturn = Screen == Screen.Wardrobe || Screen == Screen.Leaderboard || Screen == Screen.Collection || Screen == Screen.Profile ? Screen : Screen.Home;
            ClearOverlays();
            StorePage = 0;
            StoreRowsPage = 0;
            Screen = Screen.Store;
        }

        /// <summary>Back from the Store page to where it was opened (Home without reopening the Daily Reward, or the page).</summary>
        public void CloseStore()
        {
            Sound.Play(SoundCue.Click);
            ClearOverlays();
            if (StoreReturn == Screen.Home)
            {
                StartHomeMotion();
            }

            Screen = StoreReturn;
        }

        /// <summary>
        /// The system back (Android): closes the top card as its close does (the Remove Ads card, the Daily Reward, Settings,
        /// the pause); else does what a page's back button does, on the Store page, the Wardrobe, the Leaderboard and the
        /// Collection (a picture's detail back to the grid, the grid to Home). False anywhere else (the host then does what
        /// the system does).
        /// </summary>
        public bool Back()
        {
            if (_overlays.Count > 0)
            {
                CloseOverlay();
                return true;
            }

            switch (Screen)
            {
                case Screen.Store:
                    CloseStore();
                    return true;
                case Screen.Wardrobe:
                    CloseWardrobe();
                    return true;
                case Screen.Leaderboard:
                    CloseLeaderboard();
                    return true;
                case Screen.Collection:
                    CollectionBack();
                    return true;
                case Screen.Profile:
                    CloseProfile();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Home's heroes start their motion now (<see cref="HomeMotion"/>).</summary>
        private void StartHomeMotion()
        {
            HomeMotion = new HomeMotion(Now);
            _promoOpenedAt = Now;
        }

        public void OpenOverlay(Overlay overlay)
        {
            Sound.Play(SoundCue.Click);
            _overlays.Add((overlay, Now));
        }

        public void CloseOverlay()
        {
            if (_overlays.Count > 0)
            {
                bool purchase = _overlays[_overlays.Count - 1].Overlay == Overlay.Purchase;
                _overlays.RemoveAt(_overlays.Count - 1);
                Sound.Play(SoundCue.Click);
                if (purchase)
                {
                    // Closed without Buy (Cancel, the system back): nothing is bought.
                    DropPurchase();
                }
            }
        }

        /// <summary>Closes every card; a purchase still asking is dropped, nothing bought.</summary>
        private void ClearOverlays()
        {
            _overlays.Clear();
            Purchase.Cancel();
            PurchasePicture = null;
            _purchaseCancelled = null;
        }

        /// <summary>
        /// Asks to confirm a purchase (spec 005 FR-040): the confirmation card opens over everything with the item's
        /// <paramref name="picture"/>, its name and its price; <paramref name="buy"/> runs only on its Buy, and
        /// <paramref name="cancelled"/> when it is closed without (Cancel, the system back). Nothing is spent before.
        /// </summary>
        public void ConfirmPurchase(PurchaseOffer offer, Action<IPainter, Box>? picture, Action buy, Action? cancelled = null)
        {
            Purchase.Ask(offer, buy);
            PurchasePicture = picture;
            _purchaseCancelled = cancelled;
            if (_overlays.Count == 0 || _overlays[_overlays.Count - 1].Overlay != Overlay.Purchase)
            {
                OpenOverlay(Overlay.Purchase);
            }
        }

        /// <summary>The confirmation's Buy: the card closes, then the purchase runs once.</summary>
        public void BuyConfirmed()
        {
            if (_overlays.Count > 0 && _overlays[_overlays.Count - 1].Overlay == Overlay.Purchase)
            {
                _overlays.RemoveAt(_overlays.Count - 1);
            }

            PurchasePicture = null;
            _purchaseCancelled = null;
            Sound.Play(SoundCue.Click);
            Purchase.Confirm();
        }

        /// <summary>A purchase closed without Buy: nothing is bought, and whoever asked hears of it.</summary>
        private void DropPurchase()
        {
            Action? cancelled = _purchaseCancelled;
            Purchase.Cancel();
            PurchasePicture = null;
            _purchaseCancelled = null;
            cancelled?.Invoke();
        }

        public bool IsOpen(Overlay overlay)
        {
            foreach ((Overlay o, float _) in _overlays)
            {
                if (o == overlay)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A claimed reward (spec 003 FR-020): the Petals pill counts up from <paramref name="before"/> and sparkles burst
        /// around it.
        /// </summary>
        public void RewardBurst(long before)
        {
            _burstAt = Now;
            _burstFrom = before;
        }

        /// <summary>Seconds since the last <see cref="RewardBurst"/>.</summary>
        public float SinceRewardBurst => Now - _burstAt;

        /// <summary>The Petals balance as shown: counting up after a claim.</summary>
        public long ShownPetals
        {
            get
            {
                long now = Meta.Economy.Petals;
                return SinceRewardBurst < DesignTokens.Motion.CountUp.Seconds && now > _burstFrom
                    ? _burstFrom + GardenLook.CountUp(now - _burstFrom, SinceRewardBurst)
                    : now;
            }
        }

        private float _burstAt = -10f;
        private long _burstFrom;

        public void HomeToast(string message)
        {
            _homeToast = message;
            _homeToastUntil = Now + 1.6f;
        }

        public string? HomeToastText => _homeToastUntil > Now ? _homeToast : null;

        /// <summary>Home's profile avatar (spec 005 FR-037): opens the profile page.</summary>
        public void OpenProfile()
        {
            Sound.Play(SoundCue.Click);
            ClearOverlays();
            Screen = Screen.Profile;
        }

        /// <summary>Back from the profile page to Home (without reopening the Daily Reward).</summary>
        public void CloseProfile()
        {
            ProfileEditor = null;
            EnterHome(click: true);
        }

        /// <summary>Opens the edit card on <paramref name="tab"/> (the avatar's tap: Avatar; the pencil: Name).</summary>
        public void OpenProfileEdit(ProfileTab tab)
        {
            ProfileEditor = new ProfileEditor(Meta.Profile, Meta.Wardrobe, tab);
            OpenOverlay(Overlay.ProfileEdit);
        }

        /// <summary>
        /// The edit card's main button (<see cref="ProfileEditor.Confirm"/>): Save closes the card, a bought avatar shows
        /// "New avatar!", short Petals or an empty name say so. "Buy for N" asks the purchase confirmation first (spec 005
        /// FR-040): the avatar is bought only on its Buy.
        /// </summary>
        public void ConfirmProfileEdit()
        {
            ProfileEditor? editor = ProfileEditor;
            if (editor == null)
            {
                return;
            }

            if (editor.NeedsBuying && Meta.Economy.Petals >= editor.Price)
            {
                AvatarItem avatar = editor.Avatar;
                Outfit? outfit = Meta.Wardrobe.IsAvailable ? Meta.Wardrobe.OutfitOf(avatar.Family) : (Outfit?)null;
                ConfirmPurchase(
                    PurchaseOffer.ForPetals(PlaytestText.T("purchase.avatar"), editor.Price),
                    (q, well) => Kit.AvatarPicture(q, well.Inset(well.Width * 0.08f), avatar, outfit),
                    () => FinishProfileEdit(editor));
                return;
            }

            FinishProfileEdit(editor);
        }

        private void FinishProfileEdit(ProfileEditor editor)
        {
            switch (editor.Confirm())
            {
                case ProfileOutcome.Saved:
                    Sound.Play(SoundCue.Click);
                    CloseOverlay();
                    break;
                case ProfileOutcome.Bought:
                    Sound.Play(SoundCue.PodDone);
                    HomeToast(PlaytestText.T("profile.bought"));
                    break;
                case ProfileOutcome.NotEnoughPetals:
                    Sound.Play(SoundCue.Refused);
                    HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                    break;
                default:
                    Sound.Play(SoundCue.Refused);
                    HomeToast(PlaytestText.T("profile.name_empty"));
                    break;
            }
        }

        /// <summary>The Name tab's "Change name": asks the host for the name, then keeps it in the card until Save.</summary>
        public void AskProfileName()
        {
            ProfileEditor? editor = ProfileEditor;
            if (editor == null || TextPrompt == null)
            {
                return;
            }

            Sound.Play(SoundCue.Click);
            string current = editor.Name ?? PlaytestText.F("profile.default_name", Meta.Profile.DefaultNumber);
            TextPrompt.Ask(PlaytestText.T("profile.name_prompt"), current, ProfileService.MaxNameLength, text =>
            {
                if (!editor.EnterName(text))
                {
                    HomeToast(PlaytestText.T("profile.name_empty"));
                }
            });
        }

        /// <summary>Playtest control: a brand-new profile (Level 1, nothing unlocked).</summary>
        public void ResetProfile()
        {
            Meta = PlaytestMeta.ResetProfile(_dataFolder);
            HomeToast("New profile");
        }

        // ---- Frame ----

        /// <summary>Advances the clock and the level's animation, then draws the current screen and its cards.</summary>
        public void Draw(IPainter p, float dt)
        {
            Now += Math.Max(0f, Math.Min(0.1f, dt));
            if (Screen == Screen.Splash && LotusIris.SplashFull(Now, 1f))
            {
                // The ring is full: the first screen comes up under the cover and the iris opens on it.
                _splashOpenSince = Now;
                AfterSplash();
            }
            else if (SplashOpening && LotusIris.SplashDone(Now, _splashOpenSince))
            {
                _splashOpenSince = -1f;
            }

            if (Transitioning)
            {
                float since = Now - _transitionSince;
                if (!_transitionSwitched && since >= LotusIris.SwitchAt)
                {
                    _transitionSwitched = true;
                    LoadLevel(_transitionLevel);
                }
                else if (since >= LotusIris.TransitionSeconds)
                {
                    _transitionSince = -1f;
                }
            }

            if (Screen == Screen.Splash || Screen == Screen.Home)
            {
                HomeMotion.Update(Now);
            }

            switch (Screen)
            {
                case Screen.Splash:
                    SplashScreen.Draw(p, this);
                    return;
                case Screen.Home:
                    HomeScreen.Draw(p, this);
                    break;
                case Screen.Wardrobe:
                    WardrobeScreen.Draw(p, this);
                    break;
                case Screen.Store:
                    StoreScreen.Draw(p, this);
                    break;
                case Screen.Leaderboard:
                    LeaderboardScreen.Draw(p, this);
                    break;
                case Screen.Collection:
                    CollectionScreen.Draw(p, this);
                    break;
                case Screen.Profile:
                    ProfileScreen.Draw(p, this);
                    break;
                case Screen.Level:
                    Level!.Advance(dt);
                    Level.Draw(p);
                    break;
            }

            for (int i = 0; i < _overlays.Count; i++)
            {
                (Overlay overlay, float openedAt) = _overlays[i];
                float since = Now - openedAt;
                DrawingCovered = i < _overlays.Count - 1;
                switch (overlay)
                {
                    case Overlay.Pause:
                        MenuCards.Pause(p, this, since);
                        break;
                    case Overlay.Settings:
                        MenuCards.Settings(p, this, since);
                        break;
                    case Overlay.DailyReward:
                        MetaCards.DailyReward(p, this, since);
                        break;
                    case Overlay.RemoveAds:
                        MetaCards.RemoveAds(p, this, since);
                        break;
                    case Overlay.ProfileEdit:
                        ProfileScreen.Edit(p, this, since);

                        // A drag over the card's grid never taps a cell (spec 005 FR-041).
                        p.Scroll(ScreenLayout.ProfileEdit(p.Width, p.Height, p.Insets).Grid);
                        break;
                    case Overlay.Purchase:
                        MetaCards.Purchase(p, this, since);
                        break;
                }
            }

            DrawingCovered = false;
            if (SplashOpening)
            {
                LotusPainter.Draw(p, LotusIris.Splash(Now, 1f, _splashOpenSince), PlaytestText.T("splash.loading"), splash: true);
            }

            if (Transitioning)
            {
                LotusPainter.Draw(p, LotusIris.Transition(Now - _transitionSince), PlaytestText.F("common.level", _transitionLevel), splash: false);
            }
        }

        /// <summary>
        /// The garden backdrop of a level band's theme (FR-008, FR-066), cached by the painter. <paramref name="picture"/>
        /// names another owner picture than the scene's (the Wardrobe's <see cref="OwnerPictures.Wardrobe"/>), with the
        /// scene's drawn garden as its stand-in; <paramref name="place"/> places the owner's picture from its size (the
        /// win's top-anchored garden), else it is cover-fitted.
        /// </summary>
        public static void DrawBackdrop(IPainter p, BackdropScene scene, int level, string? picture = null, Func<int, int, Box>? place = null)
        {
            BackgroundTheme theme = ThemeRotation.Default.ThemeFor(Math.Max(1, level));
            if (picture == null)
            {
                // The scene's own slot, then the picture it shows: the splash shows the Home garden until its own exists.
                p.Mark(OwnerPictures.SlotOf(OwnerPictures.Background(scene, theme.Id)));
                picture = OwnerPictures.Resolve(scene, theme.Id, HasBackground(p));
            }

            p.Mark(OwnerPictures.SlotOf(picture));
            var screen = new Box(0f, 0f, p.Width, p.Height);
            // The owner's picture when it is embedded (spec 005 pictures.md B), else the code-drawn garden; Home and the
            // splash in the warmer garden colors (spec 005 §4.2); the win's garden is the level's lawn, blurred.
            BackdropColors colors = DesignTokens.Backdrop(theme.Background, theme.Accent);
            bool warm = !BackdropRaster.IsLawn(scene);
            if (warm)
            {
                colors = HomeStage.Garden(colors);
            }

            Visuals.Background(p, screen, picture, () =>
                p.Backdrop(screen, colors, scene, theme.Id + "/" + scene + (warm ? "/warm" : string.Empty)), place);
        }

        /// <summary>Whether the painter has an owner background of that name (pictures.md B).</summary>
        public static Func<string, bool> HasBackground(IPainter p) => name => p.HasSprite(PainterBase.BackgroundPrefix + name);
    }
}
