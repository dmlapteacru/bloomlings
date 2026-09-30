using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;

namespace Bloomlings.Playtest
{
    /// <summary>What a win paid, for the win card.</summary>
    public sealed record WinPayout(LevelReward? Reward, MilestoneGrant? Milestone);

    /// <summary>
    /// The playtest's meta layer on the Unity client's engine-free services (linked, not copied): the save file,
    /// linear progression with the roadmap's unlocks, Petals and booster charges, and milestone rewards. The playtest
    /// only calls them; their rules live in <c>client/</c>.
    /// </summary>
    public sealed class PlaytestMeta
    {
        private readonly SaveService _saves;

        public PlaytestMeta(string dataFolder)
        {
            _saves = new SaveService(Path.Combine(dataFolder, SaveService.FolderName), new SystemClock());
            Save = _saves.Load();
            FirstLaunch = _saves.IsFirstLaunch;
            Economy = new EconomyService(Save, EconomyConfig.Bundled, _saves.Save);
            Milestones = new MilestoneService(Save, MilestoneTable.Default, Economy, _saves.Save);
            Progression = new ProgressionService(Save, UnlockRoadmap.Default, _saves.Save);
            Progression.UnlockReached += entry =>
            {
                Economy.OnUnlock(entry.UnlockId);
                NewUnlocks.Add(entry);
            };
            Progression.LevelCompleted += level => Milestones.OnLevelCompleted(level);
            Progression.Initialize();
            NewUnlocks.Clear();
        }

        public PlayerSave Save { get; }

        /// <summary>True on the very first launch: Level 1 starts directly, with no Home (US2).</summary>
        public bool FirstLaunch { get; }

        public ProgressionService Progression { get; }

        public EconomyService Economy { get; }

        public MilestoneService Milestones { get; }

        /// <summary>Unlocks reached by the last win (the Home and win card mention them).</summary>
        public List<UnlockEntry> NewUnlocks { get; } = new List<UnlockEntry>();

        public int CurrentLevel => Progression.CurrentLevel;

        /// <summary>Records a win of the current level and pays it; null when the level was not the current one.</summary>
        public WinPayout? CompleteLevel(int level, DifficultyClass difficulty, int boostersUsed)
        {
            NewUnlocks.Clear();
            if (!Progression.CompleteLevel(level))
            {
                return null;
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
    }
}
