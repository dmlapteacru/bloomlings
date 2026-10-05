using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;

namespace Bloomlings.Playtest
{
    /// <summary>What a win paid, for the win card.</summary>
    public sealed record WinPayout(LevelReward? Reward, MilestoneGrant? Milestone);

    /// <summary>
    /// The playtest's meta layer on the Unity client's engine-free services (linked, not copied):
    /// <list type="bullet">
    /// <item><description>the save file;</description></item>
    /// <item><description>linear progression with the roadmap's unlocks;</description></item>
    /// <item><description>Petals and booster charges;</description></item>
    /// <item><description>milestone rewards;</description></item>
    /// <item><description>the Daily Reward;</description></item>
    /// <item><description>the Collection;</description></item>
    /// <item><description>the Wardrobe;</description></item>
    /// <item><description>the profile (avatars, name, joining day).</description></item>
    /// </list>
    /// The playtest only calls them; their rules live in <c>client/</c>.
    /// </summary>
    public sealed class PlaytestMeta
    {
        private readonly SaveService _saves;

        public PlaytestMeta(string dataFolder)
            : this(dataFolder, new SystemClock())
        {
        }

        public PlaytestMeta(string dataFolder, IClock clock)
        {
            _saves = new SaveService(Path.Combine(dataFolder, SaveService.FolderName), clock);
            Save = _saves.Load();
            FirstLaunch = _saves.IsFirstLaunch;
            var config = new BundledRemoteConfigService();
            Economy = new EconomyService(Save, EconomyConfig.Bundled, _saves.Save);
            Milestones = new MilestoneService(Save, MilestoneTable.Default, Economy, _saves.Save);
            DailyReward = new DailyRewardService(Save, clock, config, Economy, _saves.Save);
            Collection = new CollectionService(Save, _saves.Save);
            Wardrobe = new WardrobeService(Save, Cosmetics, config, _saves.Save, Economy);
            Profile = new ProfileService(Save, config, clock, _saves.Save, Economy);
            Progression = new ProgressionService(Save, UnlockRoadmap.Default, _saves.Save);
            Progression.UnlockReached += entry =>
            {
                Economy.OnUnlock(entry.UnlockId);
                Wardrobe.OnUnlock(entry.UnlockId);
                NewUnlocks.Add(entry);
            };
            Progression.LevelCompleted += level => Milestones.OnLevelCompleted(level);
            Progression.Initialize();
            NewUnlocks.Clear();
        }

        /// <summary>The cosmetic catalog the Unity client ships (embedded from <c>Meta/Wardrobe/Resources</c>).</summary>
        public static CosmeticCatalog Cosmetics { get; } = LoadCosmetics();

        public PlayerSave Save { get; }

        /// <summary>True on the very first launch: Level 1 starts directly, with no Home (US2).</summary>
        public bool FirstLaunch { get; }

        public ProgressionService Progression { get; }

        public EconomyService Economy { get; }

        public MilestoneService Milestones { get; }

        public DailyRewardService DailyReward { get; }

        public CollectionService Collection { get; }

        public WardrobeService Wardrobe { get; }

        public ProfileService Profile { get; }

        /// <summary>Unlocks reached by the last win (the Home and win card mention them).</summary>
        public List<UnlockEntry> NewUnlocks { get; } = new List<UnlockEntry>();

        public int CurrentLevel => Progression.CurrentLevel;

        /// <summary>Records a win of the current level and pays it; null when the level was not the current one.</summary>
        /// <param name="definition">The level played: its finished picture joins the Collection (FR-065).</param>
        public WinPayout? CompleteLevel(int level, DifficultyClass difficulty, int boostersUsed, LevelDefinition? definition = null)
        {
            NewUnlocks.Clear();
            if (!Progression.CompleteLevel(level))
            {
                return null;
            }

            if (definition != null)
            {
                Collection.Add(definition, level);
            }

            LevelReward reward = Economy.GrantLevelReward(level, difficulty, boostersUsed);
            MilestoneGrant? grant = Milestones.LastGrant != null && Milestones.LastGrant.Level == level ? Milestones.LastGrant : null;
            return new WinPayout(reward, grant);
        }

        public bool HasSeenDemo(string id) => Progression.HasSeenDemo(id);

        public void MarkDemoSeen(string id) => Progression.MarkDemoSeen(id);

        public void Persist() => _saves.Save();

        /// <summary>Tester control: completes levels up to <paramref name="highestCompleted"/>, with their unlocks and rewards.</summary>
        public void SkipTo(int highestCompleted)
        {
            NewUnlocks.Clear();
            Progression.FastForward(highestCompleted);
        }

        /// <summary>Tester control: one level back (unlocks and Petals stay).</summary>
        public void StepBack()
        {
            if (Save.Progression.HighestCompletedLevel > 0)
            {
                Save.Progression.HighestCompletedLevel--;
                _saves.Save();
            }
        }

        /// <summary>Tester control: deletes the save and starts a brand-new profile (Level 1, nothing unlocked).</summary>
        public static PlaytestMeta ResetProfile(string dataFolder)
        {
            string folder = Path.Combine(dataFolder, SaveService.FolderName);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }

            return new PlaytestMeta(dataFolder);
        }

        private static CosmeticCatalog LoadCosmetics()
        {
            using Stream? stream = typeof(PlaytestMeta).Assembly.GetManifestResourceStream("cosmetics/CosmeticCatalog.json");
            if (stream == null)
            {
                return new CosmeticCatalog(Array.Empty<CosmeticItem>());
            }

            using var reader = new StreamReader(stream);
            return CosmeticCatalog.Parse(reader.ReadToEnd());
        }
    }
}
