using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>
    /// The loaded levels and pictures of one content version, with lookups by level number and by picture
    /// <c>(id, version)</c>. Every level's picture must be present, so a missing picture fails at load time rather
    /// than when the level starts.
    /// </summary>
    public sealed class ContentSet
    {
        private readonly Dictionary<int, LevelDefinition> _levels = new Dictionary<int, LevelDefinition>();
        private readonly Dictionary<string, BasePicture> _pictures = new Dictionary<string, BasePicture>(StringComparer.Ordinal);
        private readonly int[] _levelNumbers;
        private readonly DailyPoolEntry[] _daily;

        /// <param name="contentVersion">The manifest's content version; 0 for loose development content.</param>
        /// <param name="shuffleNodeBudget">Fixed per content version (research R10).</param>
        /// <param name="daily">The Daily Challenge pool (R19), if the content has one.</param>
        public ContentSet(
            int contentVersion,
            int shuffleNodeBudget,
            IEnumerable<LevelDefinition> levels,
            IEnumerable<BasePicture> pictures,
            IEnumerable<DailyPoolEntry>? daily = null)
        {
            ContentVersion = contentVersion;
            ShuffleNodeBudget = shuffleNodeBudget;

            foreach (BasePicture picture in pictures)
            {
                string key = PictureKey(picture.Id, picture.Version);
                if (_pictures.ContainsKey(key))
                {
                    throw new ContentIntegrityException("pictures", $"duplicate picture {picture.Id} v{picture.Version}");
                }

                _pictures.Add(key, picture);
            }

            foreach (LevelDefinition level in levels)
            {
                if (_levels.ContainsKey(level.LevelNumber))
                {
                    throw new ContentIntegrityException("levels", $"duplicate level {level.LevelNumber}");
                }

                if (!_pictures.ContainsKey(PictureKey(level.Picture.Id, level.Picture.Version)))
                {
                    throw new ContentIntegrityException(
                        "levels",
                        $"level {level.LevelNumber} uses missing picture {level.Picture.Id} v{level.Picture.Version}");
                }

                _levels.Add(level.LevelNumber, level);
            }

            _levelNumbers = new int[_levels.Count];
            _levels.Keys.CopyTo(_levelNumbers, 0);
            Array.Sort(_levelNumbers);

            var pool = new List<DailyPoolEntry>(daily ?? Array.Empty<DailyPoolEntry>());
            pool.Sort((a, b) => a.Index.CompareTo(b.Index));
            for (int i = 0; i < pool.Count; i++)
            {
                // Indices are 0..n-1 without gaps, so every device maps a date to the same entry (R19).
                if (pool[i].Index != i)
                {
                    throw new ContentIntegrityException("daily", $"the daily pool indices must be 0..{pool.Count - 1}; found {pool[i].Index} at position {i}");
                }

                PictureRef picture = pool[i].Level.Picture;
                if (!_pictures.ContainsKey(PictureKey(picture.Id, picture.Version)))
                {
                    throw new ContentIntegrityException("daily", $"daily entry {i} uses missing picture {picture.Id} v{picture.Version}");
                }
            }

            _daily = pool.ToArray();
        }

        public int ContentVersion { get; }

        public int ShuffleNodeBudget { get; }

        /// <summary>All level numbers, ascending.</summary>
        public IReadOnlyList<int> LevelNumbers => _levelNumbers;

        public int LevelCount => _levelNumbers.Length;

        public int PictureCount => _pictures.Count;

        /// <summary>Every picture of the content set, in no particular order.</summary>
        public IEnumerable<BasePicture> Pictures => _pictures.Values;

        /// <summary>The Daily Challenge pool by index (R19); empty when the content has no daily pack.</summary>
        public IReadOnlyList<DailyPoolEntry> DailyPool => _daily;

        /// <summary>The highest level number, or 0 when the set is empty.</summary>
        public int MaxLevel => _levelNumbers.Length == 0 ? 0 : _levelNumbers[_levelNumbers.Length - 1];

        public bool TryGetLevel(int levelNumber, out LevelDefinition level) => _levels.TryGetValue(levelNumber, out level!);

        public LevelDefinition GetLevel(int levelNumber) =>
            _levels.TryGetValue(levelNumber, out LevelDefinition? level)
                ? level
                : throw new KeyNotFoundException($"Level {levelNumber} is not in content version {ContentVersion}.");

        public BasePicture GetPicture(PictureRef picture) => GetPicture(picture.Id, picture.Version);

        public BasePicture GetPicture(string id, int version) =>
            _pictures.TryGetValue(PictureKey(id, version), out BasePicture? picture)
                ? picture
                : throw new KeyNotFoundException($"Picture {id} v{version} is not in content version {ContentVersion}.");

        private static string PictureKey(string id, int version) => id + "@" + version.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
