using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Rounds;

namespace Bloomlings.Client.Meta.Album
{
    /// <summary>An object of a scene as the album shows it now (spec 007).</summary>
    /// <param name="Item">The catalog's object.</param>
    /// <param name="Level">The campaign level holding its picture, or null when no published level holds it yet.</param>
    /// <param name="Open">Whether that level is won: the object shows in colour.</param>
    /// <param name="New">Open but not yet seen in colour: the colour's return plays when its scene is next shown.</param>
    public sealed record AlbumObjectView(AlbumObject Item, int? Level, bool Open, bool New);

    /// <summary>A scene as the album shows it now (spec 007): its objects, how many are open and its reward's state.</summary>
    public sealed record AlbumSceneView(AlbumScene Scene, IReadOnlyList<AlbumObjectView> Objects, bool RewardClaimed)
    {
        public int OpenCount
        {
            get
            {
                int open = 0;
                foreach (AlbumObjectView item in Objects)
                {
                    open += item.Open ? 1 : 0;
                }

                return open;
            }
        }

        public int Total => Objects.Count;

        /// <summary>Every object is open ("Scene complete").</summary>
        public bool Complete => OpenCount == Total;

        /// <summary>Complete and its one-time reward not claimed yet: the reward strip's Claim.</summary>
        public bool RewardWaiting => Complete && !RewardClaimed;

        /// <summary>An opened object not seen yet, or a reward waiting: the menu's "!" and the album's opening priority.</summary>
        public bool HasNews
        {
            get
            {
                if (RewardWaiting)
                {
                    return true;
                }

                foreach (AlbumObjectView item in Objects)
                {
                    if (item.New)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>The view of that object, or null.</summary>
        public AlbumObjectView? Find(string id)
        {
            foreach (AlbumObjectView item in Objects)
            {
                if (item.Item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The Scene Album (spec 007-scene-album, the owner's UX of 2026-11-02; it replaced the Collection in the bottom menu):
    /// <list type="bullet">
    /// <item><description>It opens at <see cref="UnlockId"/> (L27, the unlock roadmap).</description></item>
    /// <item><description>An object is open once the campaign level holding its picture is won
    /// (<see cref="LevelOf"/>: the published round levels' pictures, the explicit binding by picture code, never by title),
    /// so wins before the album opened count at once; nothing about it is stored.</description></item>
    /// <item><description>Future scenes show gradually: a scene shows once one of its objects is within
    /// <see cref="PreviewWindow"/> levels of the current one (or open), and every scene before it shows
    /// (<see cref="Shown"/>).</description></item>
    /// <item><description>The album opens on (<see cref="OpeningIndex"/>): a scene with a new reveal or a waiting reward, else
    /// the last viewed incomplete one, else the nearest incomplete one.</description></item>
    /// <item><description>A complete scene's reward (<see cref="RewardPetals"/>, Remote Config
    /// <c>economy.album.sceneReward</c>) is claimed once (<see cref="Claim"/>), a gift through the economy service with no
    /// purchase confirmation.</description></item>
    /// <item><description>The save's <c>album</c> section keeps the claimed rewards, the reveals seen
    /// (<see cref="Viewed"/>) and the scene viewed last.</description></item>
    /// </list>
    /// Engine-free: the playtest and the Unity client call it.
    /// </summary>
    public sealed class AlbumService
    {
        /// <summary>The roadmap's unlock of the album: <c>system.album</c> at L27 (the owner, 2026-11-01).</summary>
        public const string UnlockId = "system.album";

        /// <summary>How far ahead of the current level a scene's first object may lie for the scene to show (spec 007 FR-006).</summary>
        public const int PreviewWindow = 30;

        private readonly PlayerSave _save;
        private readonly Func<string, int?> _levelOfPicture;
        private readonly IRemoteConfigService _config;
        private readonly EconomyService _economy;
        private readonly Action _persist;

        /// <param name="levelOfPicture">The campaign level holding a picture code, or null (<see cref="PictureLevels"/>).</param>
        public AlbumService(PlayerSave save, AlbumCatalog catalog, Func<string, int?> levelOfPicture, IRemoteConfigService config, EconomyService economy, Action persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _levelOfPicture = levelOfPicture ?? throw new ArgumentNullException(nameof(levelOfPicture));
            _config = config;
            _economy = economy;
            _persist = persist;
        }

        /// <summary>A reward was claimed or a reveal seen.</summary>
        public event Action? Changed;

        public AlbumCatalog Catalog { get; }

        /// <summary>Whether the album is open (the roadmap's <see cref="UnlockId"/> reached).</summary>
        public bool IsUnlocked => _save.Unlocks.IsSet(UnlockId);

        /// <summary>The level Play starts ("Next level: M").</summary>
        public int NextLevel => _save.Progression.CurrentLevel;

        /// <summary>A finished scene's one-time reward in Petals (Remote Config <c>economy.album.sceneReward</c>, 50).</summary>
        public int RewardPetals => _config.Get(RemoteConfigKeys.AlbumSceneReward);

        /// <summary>
        /// Whether a claim can pay here: the server-authoritative economy has no album grant yet, so there the Claim stays
        /// greyed (spec 007 open point) and nothing is marked claimed.
        /// </summary>
        public bool CanPay => !_economy.IsAuthoritative;

        /// <summary>
        /// Picture code → the lowest published round level holding it (any version of the picture), for the catalog's
        /// pictures. Without round levels (<paramref name="rounds"/> null: the preview's legacy frames) each object's
        /// <see cref="AlbumObject.LevelHint"/>, the level the owner's pack was drawn for.
        /// </summary>
        public static Func<string, int?> PictureLevels(AlbumCatalog catalog, IRoundLevelSource? rounds)
        {
            var levels = new Dictionary<string, int>(StringComparer.Ordinal);
            if (rounds == null)
            {
                foreach (AlbumScene scene in catalog.Scenes)
                {
                    foreach (AlbumObject item in scene.Objects)
                    {
                        levels[item.Picture] = item.LevelHint;
                    }
                }
            }
            else
            {
                var wanted = new HashSet<string>(StringComparer.Ordinal);
                foreach (AlbumScene scene in catalog.Scenes)
                {
                    foreach (AlbumObject item in scene.Objects)
                    {
                        wanted.Add(item.Picture);
                    }
                }

                for (int n = 1; n <= rounds.LevelCount; n++)
                {
                    string picture = rounds.Listed(n).PictureId;
                    if (wanted.Contains(picture) && !levels.ContainsKey(picture))
                    {
                        levels[picture] = n;
                    }
                }
            }

            return picture => levels.TryGetValue(picture, out int level) ? level : (int?)null;
        }

        /// <summary>The campaign level holding <paramref name="item"/>'s picture, or null when no published level holds it yet.</summary>
        public int? LevelOf(AlbumObject item) => _levelOfPicture(item.Picture);

        /// <summary>Whether <paramref name="item"/> is open: the level holding its picture is won (earlier wins count).</summary>
        public bool IsOpen(AlbumObject item)
        {
            int? level = LevelOf(item);
            return level.HasValue && level.Value <= _save.Progression.HighestCompletedLevel;
        }

        /// <summary>The scene as it shows now.</summary>
        public AlbumSceneView View(AlbumScene scene)
        {
            var objects = new List<AlbumObjectView>();
            foreach (AlbumObject item in scene.Objects)
            {
                int? level = LevelOf(item);
                bool open = level.HasValue && level.Value <= _save.Progression.HighestCompletedLevel;
                objects.Add(new AlbumObjectView(item, level, open, open && !_save.Album.HasSeen(scene.Id, item.Id)));
            }

            return new AlbumSceneView(scene, objects, _save.Album.Claimed.Contains(scene.Id));
        }

        /// <summary>
        /// The scenes that show, in album order: the first always; then each scene while one of its objects is open or lies
        /// within <see cref="PreviewWindow"/> levels of the current one; the first that does not ends the list, so the
        /// album never reveals a far scene before a near one.
        /// </summary>
        public IReadOnlyList<AlbumScene> Shown
        {
            get
            {
                var shown = new List<AlbumScene>();
                int horizon = _save.Progression.CurrentLevel + PreviewWindow;
                foreach (AlbumScene scene in Catalog.Scenes)
                {
                    bool near = shown.Count == 0;
                    foreach (AlbumObject item in scene.Objects)
                    {
                        int? level = LevelOf(item);
                        near |= level.HasValue && level.Value <= horizon;
                    }

                    if (!near)
                    {
                        break;
                    }

                    shown.Add(scene);
                }

                return shown;
            }
        }

        /// <summary>
        /// The scene the album opens on, an index into <see cref="Shown"/> (spec 007 FR-004): the first with a new reveal or
        /// a waiting reward; else the last viewed one while incomplete; else the first incomplete one; else the last viewed
        /// one, or the last scene. −1 for an album without scenes.
        /// </summary>
        public int OpeningIndex()
        {
            IReadOnlyList<AlbumScene> shown = Shown;
            if (shown.Count == 0)
            {
                return -1;
            }

            int last = -1;
            int incomplete = -1;
            for (int i = 0; i < shown.Count; i++)
            {
                AlbumSceneView view = View(shown[i]);
                if (view.HasNews)
                {
                    return i;
                }

                if (shown[i].Id == _save.Album.LastViewed)
                {
                    last = i;
                }

                if (incomplete < 0 && !view.Complete)
                {
                    incomplete = i;
                }
            }

            if (last >= 0 && !View(shown[last]).Complete)
            {
                return last;
            }

            if (incomplete >= 0)
            {
                return incomplete;
            }

            return last >= 0 ? last : shown.Count - 1;
        }

        /// <summary>Whether the bottom menu's Album wears the "!": the album is open and a shown scene has a new reveal or a waiting reward.</summary>
        public bool HasNews
        {
            get
            {
                if (!IsUnlocked)
                {
                    return false;
                }

                foreach (AlbumScene scene in Shown)
                {
                    if (View(scene).HasNews)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// The ids of <paramref name="scene"/>'s objects that are open but not seen yet: the page takes them before
        /// <see cref="Viewed"/> to play their colour's return.
        /// </summary>
        public IReadOnlyList<string> NewReveals(AlbumScene scene)
        {
            var ids = new List<string>();
            foreach (AlbumObjectView item in View(scene).Objects)
            {
                if (item.New)
                {
                    ids.Add(item.Item.Id);
                }
            }

            return ids;
        }

        /// <summary>
        /// The album shows <paramref name="scene"/>: its open objects count as seen (their colour's return has started) and it
        /// becomes the scene viewed last. Saved only when something changed.
        /// </summary>
        public void Viewed(AlbumScene scene)
        {
            bool changed = _save.Album.LastViewed != scene.Id;
            _save.Album.LastViewed = scene.Id;
            foreach (AlbumObjectView item in View(scene).Objects)
            {
                if (item.Open)
                {
                    changed |= _save.Album.MarkSeen(scene.Id, item.Item.Id);
                }
            }

            if (changed)
            {
                _persist();
                Changed?.Invoke();
            }
        }

        /// <summary>Whether <paramref name="scene"/>'s reward can be claimed now: the album is open, the scene complete and not claimed, and a claim pays here.</summary>
        public bool CanClaim(AlbumScene scene) => IsUnlocked && CanPay && View(scene).RewardWaiting;

        /// <summary>
        /// Claims <paramref name="scene"/>'s one-time reward: <see cref="RewardPetals"/> through the economy service and the
        /// scene marked claimed in the save. Returns the Petals paid, 0 when nothing was claimable (claimed already,
        /// incomplete, the album closed, or a server economy).
        /// </summary>
        public int Claim(AlbumScene scene)
        {
            if (!CanClaim(scene) || !_save.Album.Claimed.Add(scene.Id))
            {
                return 0;
            }

            int petals = RewardPetals;
            _economy.Grant(petals, null);
            _persist();
            Changed?.Invoke();
            return petals;
        }
    }
}
