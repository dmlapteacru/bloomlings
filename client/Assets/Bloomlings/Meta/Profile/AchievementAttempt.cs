using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Meta.Profile
{
    /// <summary>Transient evidence from one attempt. Only a newly confirmed main-game win writes it to the save.</summary>
    public sealed class AchievementAttempt
    {
        private readonly HashSet<string> _keys = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _fountains = new HashSet<string>(StringComparer.Ordinal);
        private int _occupied;

        public int PeakSlots { get; private set; }
        public bool ChainReaction { get; private set; }
        public bool UsedBooster { get; private set; }
        public int Keys => _keys.Count;
        public int Fountains => _fountains.Count;

        public void Reset()
        {
            _keys.Clear();
            _fountains.Clear();
            _occupied = 0;
            PeakSlots = 0;
            ChainReaction = false;
            UsedBooster = false;
        }

        /// <param name="ordinaryTap">A direct tap, rather than a booster command.</param>
        public void Observe(IReadOnlyList<GameEvent> events, LevelDefinition definition, bool ordinaryTap)
        {
            if (!ordinaryTap) UsedBooster = true;
            var completed = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameEvent e in events)
            {
                switch (e)
                {
                    case PodCommitted _:
                        _occupied++;
                        PeakSlots = Math.Max(PeakSlots, _occupied);
                        break;
                    case SlotFreed _:
                    case PodReturned _:
                        _occupied = Math.Max(0, _occupied - 1);
                        break;
                    case PodCompleted pod:
                        completed.Add(pod.PodId);
                        break;
                    case KeyCollected key:
                        _keys.Add(key.KeyId);
                        break;
                    case SpecialTriggered special:
                        foreach (SpecialDef def in definition.Specials)
                            if (def.Id == special.SpecialId && def.Type == SpecialType.Fountain)
                                _fountains.Add(special.SpecialId);
                        break;
                }
            }
            if (ordinaryTap && completed.Count >= 3) ChainReaction = true;
        }

        /// <summary>Called before progression persists the new win, so evidence and level advancement are one save.</summary>
        public void RecordWin(PlayerSave save, int levelNumber, LevelDefinition level, BasePicture picture, int selfBoostersUsed)
        {
            if (levelNumber != save.Progression.CurrentLevel) return;
            string levelId = levelNumber.ToString(CultureInfo.InvariantCulture);
            void Fact(string id, long amount = 1)
            {
                if (amount < 1) return;
                string group = Achievements.FactPrefix + id;
                if (!save.Stats.Groups.TryGetValue(group, out SortedDictionary<string, long>? values))
                {
                    values = new SortedDictionary<string, long>(StringComparer.Ordinal);
                    save.Stats.Groups[group] = values;
                }
                values[levelId] = Math.Max(values.TryGetValue(levelId, out long old) ? old : 0, amount);
            }

            if (levelNumber >= 11 && selfBoostersUsed == 0) Fact("natural_talent");
            if (levelNumber >= 11 && level.Slots.Count == 5 && level.Slots.Locked == null
                && PeakSlots <= 3 && !UsedBooster) Fact("breathing_room");
            if (level.Difficulty.Class == DifficultyClass.Hard) Fact("tough_bloom");
            if (level.Difficulty.Class == DifficultyClass.SuperHard) Fact("lotus_mind");
            if (ChainReaction) Fact("chain_reaction");
            Fact("key_keeper", Keys);
            foreach (CellOverlay overlay in level.Overlays)
                if (overlay.LayersBelow.Count >= 2) { Fact("deep_roots"); break; }
            foreach (PodDef pod in level.Pods)
                if (pod.ConnectedGroupId != null) { Fact("better_together"); break; }
            Fact("spring_keeper", Fountains);

            bool newPicture = true;
            foreach (CollectionEntry entry in save.Collection)
                if (entry.PictureId == picture.Id) { newPicture = false; break; }
            if (newPicture)
            {
                foreach (string theme in picture.Tags.Themes)
                {
                    string group = Achievements.ThemePrefix + theme;
                    if (!save.Stats.Groups.TryGetValue(group, out SortedDictionary<string, long>? values))
                    {
                        values = new SortedDictionary<string, long>(StringComparer.Ordinal);
                        save.Stats.Groups[group] = values;
                    }
                    values[picture.Id] = 1;
                }
            }

            // The existing CollectionService.Add call after the win will see this entry and do nothing. Keeping the
            // collection entry here lets progression's next save persist it together with achievement facts and the win.
            bool levelCollected = false;
            foreach (CollectionEntry entry in save.Collection)
                if (entry.LevelNumber == levelNumber) { levelCollected = true; break; }
            if (!levelCollected)
                save.Collection.Add(new CollectionEntry(level.Picture.Id, level.Picture.Version,
                    CollectionService.MappingHash(level.Mapping), levelNumber));
        }
    }
}
