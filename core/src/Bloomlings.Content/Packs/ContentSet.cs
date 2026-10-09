using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Content.Packs
{
    /// <summary>
    /// The loaded levels and pictures of one content version, with lookups by level number and by picture
    /// <c>(id, version)</c>. Every level's picture must be present, so a missing picture fails at load time rather
    /// than when the level starts. A set made with a level reader knows its level numbers up front and reads each level
    /// when it is first asked for (the playtest APK, which carries all 5000), checking its picture then; one made with a
    /// picture reader too knows its picture ids up front and reads each picture when it is first asked for.
    /// </summary>
    public sealed class ContentSet
    {
        private readonly Dictionary<int, LevelDefinition> _levels = new Dictionary<int, LevelDefinition>();
        private readonly Dictionary<string, BasePicture> _pictures = new Dictionary<string, BasePicture>(StringComparer.Ordinal);
        private readonly int[] _levelNumbers;
        private readonly DailyPoolEntry[] _daily;
        private readonly Func<int, LevelDefinition>? _readLevel;
        private readonly HashSet<string>? _pictureIds;
        private readonly Func<string, BasePicture>? _readPicture;
        private readonly object _gate = new object();

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

                CheckPicture(level);
                _levels.Add(level.LevelNumber, level);
            }

            _levelNumbers = new int[_levels.Count];
            _levels.Keys.CopyTo(_levelNumbers, 0);
            Array.Sort(_levelNumbers);
            _daily = CheckDaily(daily);
        }

        /// <summary>A content set whose levels are read on first use, by number, with <paramref name="readLevel"/>.</summary>
        /// <param name="levelNumbers">Every level number the set has.</param>
        /// <param name="readLevel">Reads one of those levels; called at most once per level.</param>
        public ContentSet(
            int contentVersion,
            int shuffleNodeBudget,
            IEnumerable<int> levelNumbers,
            Func<int, LevelDefinition> readLevel,
            IEnumerable<BasePicture> pictures,
            IEnumerable<DailyPoolEntry>? daily = null)
            : this(contentVersion, shuffleNodeBudget, Array.Empty<LevelDefinition>(), pictures, daily)
        {
            var numbers = new List<int>(levelNumbers);
            numbers.Sort();
            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == numbers[i - 1])
                {
                    throw new ContentIntegrityException("levels", $"duplicate level {numbers[i]}");
                }
            }

            _levelNumbers = numbers.ToArray();
            _readLevel = readLevel;
        }

        /// <summary>
        /// A content set whose levels and pictures are both read on first use (the playtest APK: nothing is parsed at
        /// start), the levels by number with <paramref name="readLevel"/>, the pictures by id with
        /// <paramref name="readPicture"/>: a picture is found by its id and must have the version asked for.
        /// </summary>
        /// <param name="pictureIds">Every picture id the set has (one version of each).</param>
        /// <param name="readPicture">Reads the picture of one of those ids; called at most once per id.</param>
        public ContentSet(
            int contentVersion,
            int shuffleNodeBudget,
            IEnumerable<int> levelNumbers,
            Func<int, LevelDefinition> readLevel,
            IEnumerable<string> pictureIds,
            Func<string, BasePicture> readPicture)
            : this(contentVersion, shuffleNodeBudget, levelNumbers, readLevel, Array.Empty<BasePicture>())
        {
            _pictureIds = new HashSet<string>(pictureIds, StringComparer.Ordinal);
            _readPicture = readPicture;
        }

        private void CheckPicture(LevelDefinition level)
        {
            if (!HasPicture(level.Picture.Id, level.Picture.Version))
            {
                throw new ContentIntegrityException(
                    "levels",
                    $"level {level.LevelNumber} uses missing picture {level.Picture.Id} v{level.Picture.Version}");
            }
        }

        private DailyPoolEntry[] CheckDaily(IEnumerable<DailyPoolEntry>? daily)
        {
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

            return pool.ToArray();
        }

        public int ContentVersion { get; }

        public int ShuffleNodeBudget { get; }

        /// <summary>All level numbers, ascending.</summary>
        public IReadOnlyList<int> LevelNumbers => _levelNumbers;

        public int LevelCount => _levelNumbers.Length;

        public int PictureCount => _pictureIds?.Count ?? _pictures.Count;

        /// <summary>Every picture of the content set, in no particular order (a set that reads its pictures on first use reads them all).</summary>
        public IEnumerable<BasePicture> Pictures
        {
            get
            {
                if (_pictureIds == null)
                {
                    return _pictures.Values;
                }

                var all = new List<BasePicture>(_pictureIds.Count);
                foreach (string id in _pictureIds)
                {
                    all.Add(ReadPicture(id));
                }

                return all;
            }
        }

        /// <summary>The Daily Challenge pool by index (R19); empty when the content has no daily pack.</summary>
        public IReadOnlyList<DailyPoolEntry> DailyPool => _daily;

        /// <summary>The highest level number, or 0 when the set is empty.</summary>
        public int MaxLevel => _levelNumbers.Length == 0 ? 0 : _levelNumbers[_levelNumbers.Length - 1];

        public bool TryGetLevel(int levelNumber, out LevelDefinition level)
        {
            if (_readLevel == null)
            {
                return _levels.TryGetValue(levelNumber, out level!);
            }

            lock (_gate)
            {
                if (_levels.TryGetValue(levelNumber, out level!))
                {
                    return true;
                }

                if (Array.BinarySearch(_levelNumbers, levelNumber) < 0)
                {
                    return false;
                }

                LevelDefinition read = _readLevel(levelNumber);
                if (read.LevelNumber != levelNumber)
                {
                    throw new ContentIntegrityException("levels", $"level {levelNumber} reads as level {read.LevelNumber}");
                }

                CheckPicture(read);
                _levels.Add(levelNumber, read);
                level = read;
                return true;
            }
        }

        public LevelDefinition GetLevel(int levelNumber) =>
            TryGetLevel(levelNumber, out LevelDefinition level)
                ? level
                : throw new KeyNotFoundException($"Level {levelNumber} is not in content version {ContentVersion}.");

        public BasePicture GetPicture(PictureRef picture) => GetPicture(picture.Id, picture.Version);

        public BasePicture GetPicture(string id, int version) =>
            TryGetPicture(id, version, out BasePicture picture)
                ? picture
                : throw new KeyNotFoundException($"Picture {id} v{version} is not in content version {ContentVersion}.");

        private bool HasPicture(string id, int version) => TryGetPicture(id, version, out _);

        private bool TryGetPicture(string id, int version, out BasePicture picture)
        {
            if (_pictureIds == null)
            {
                return _pictures.TryGetValue(PictureKey(id, version), out picture!);
            }

            if (!_pictureIds.Contains(id))
            {
                picture = null!;
                return false;
            }

            picture = ReadPicture(id);
            return picture.Version == version;
        }

        /// <summary>The picture of an id the set has, read on first use (a set made with a picture reader).</summary>
        private BasePicture ReadPicture(string id)
        {
            lock (_gate)
            {
                if (_pictures.TryGetValue(id, out BasePicture? picture))
                {
                    return picture;
                }

                BasePicture read = _readPicture!(id);
                if (read.Id != id)
                {
                    throw new ContentIntegrityException("pictures", $"picture {id} reads as picture {read.Id}");
                }

                _pictures.Add(id, read);
                return read;
            }
        }

        private static string PictureKey(string id, int version) => id + "@" + version.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
