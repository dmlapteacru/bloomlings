using System;
using System.Collections.Generic;
using System.Text;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Content.Rounds
{
    /// <summary>
    /// Where a client's round levels come from (spec 006 FR-060): the levels 1..<see cref="LevelCount"/> of one published
    /// content version, each with its picture. The level screen asks it by number; a number past
    /// <see cref="LevelCount"/> has no level yet (the "new levels soon" card).
    /// </summary>
    public interface IRoundLevelSource
    {
        /// <summary>The published content version (<c>rounds publish</c>).</summary>
        int ContentVersion { get; }

        /// <summary>How many round levels there are; they are numbered 1..<see cref="LevelCount"/> without gaps.</summary>
        int LevelCount { get; }

        /// <summary>Whether level <paramref name="levelNumber"/> exists (1..<see cref="LevelCount"/>).</summary>
        bool Has(int levelNumber);

        /// <summary>The level as the manifest lists it (class, definition version, picture), without reading a pack.</summary>
        RoundManifestLevel Listed(int levelNumber);

        /// <summary>The level's definition, read on first use.</summary>
        RoundLevel GetLevel(int levelNumber);

        /// <summary>The level's picture, read on first use.</summary>
        RoundPicture GetPicture(RoundLevel level);
    }

    /// <summary>
    /// A published round content version (<c>rounds publish</c>, contracts/pipeline-rounds-cli.md) as an
    /// <see cref="IRoundLevelSource"/>: <c>manifest.json</c> is read at once, and each pack when one of its levels or a
    /// picture is first asked for, checked against the manifest's SHA-256 and length (as <see cref="ContentSet"/> reads the
    /// legacy levels on first use). <paramref name="readFile"/> gives a file's bytes by its path relative to the manifest
    /// (<c>manifest.json</c>, <c>levels/round_levels_0001_0250.jsonl.gz</c>, …), or null when the build does not carry it.
    /// Engine-free: the playtest and the Unity client read their packs through it.
    /// </summary>
    public sealed class RoundContentSet : IRoundLevelSource
    {
        private readonly Func<string, byte[]?> _readFile;
        private readonly Dictionary<int, RoundLevel> _levels = new Dictionary<int, RoundLevel>();
        private readonly HashSet<string> _readPacks = new HashSet<string>(StringComparer.Ordinal);
        private readonly RoundPackEntry[] _packOfLevel;
        private readonly RoundPackEntry _picturePack;
        private readonly HashSet<string> _listedPictures = new HashSet<string>(StringComparer.Ordinal);
        private readonly object _gate = new object();
        private Dictionary<string, RoundPicture>? _pictures;

        /// <exception cref="ContentIntegrityException">The manifest's levels are not 1..N, a level's picture or pack is not listed.</exception>
        public RoundContentSet(RoundContentManifest manifest, Func<string, byte[]?> readFile)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _readFile = readFile ?? throw new ArgumentNullException(nameof(readFile));

            foreach ((string id, int version) in manifest.Pictures)
            {
                _listedPictures.Add(RoundPicture.KeyOf(id, version));
            }

            _packOfLevel = new RoundPackEntry[manifest.Levels.Count + 1];
            RoundPackEntry? picturePack = null;
            foreach (RoundPackEntry pack in manifest.Packs)
            {
                if (pack.Kind == RoundPackKind.Pictures)
                {
                    picturePack = picturePack == null ? pack : throw new ContentIntegrityException("manifest", "more than one picture pack");
                    continue;
                }

                if (!pack.FirstLevel.HasValue || !pack.LastLevel.HasValue)
                {
                    throw new ContentIntegrityException(pack.Id, "a level pack needs its level range");
                }

                for (int n = pack.FirstLevel.Value; n <= pack.LastLevel.Value; n++)
                {
                    if (n >= _packOfLevel.Length)
                    {
                        throw new ContentIntegrityException(pack.Id, $"level {n} is not in the manifest's levels");
                    }

                    if (_packOfLevel[n] != null)
                    {
                        throw new ContentIntegrityException(pack.Id, $"level {n} is in two packs");
                    }

                    _packOfLevel[n] = pack;
                }
            }

            _picturePack = picturePack ?? throw new ContentIntegrityException("manifest", "no picture pack");
            for (int i = 0; i < manifest.Levels.Count; i++)
            {
                RoundManifestLevel level = manifest.Levels[i];
                if (level.Number != i + 1)
                {
                    throw new ContentIntegrityException("manifest", $"round levels must be numbered 1..{manifest.Levels.Count} in order; found {level.Number} at position {i + 1}");
                }

                if (!_listedPictures.Contains(RoundPicture.KeyOf(level.PictureId, level.PictureVersion)))
                {
                    throw new ContentIntegrityException("manifest", $"level {level.Number} uses the unlisted picture {level.PictureId} v{level.PictureVersion}");
                }

                if (_packOfLevel[level.Number] == null)
                {
                    throw new ContentIntegrityException("manifest", $"level {level.Number} is in no pack");
                }
            }
        }

        /// <summary>
        /// Reads the published content whose files <paramref name="readFile"/> gives: <c>manifest.json</c> now, the packs on
        /// first use.
        /// </summary>
        /// <exception cref="ContentIntegrityException">There is no manifest (nothing published), or it is inconsistent.</exception>
        public static RoundContentSet Read(Func<string, byte[]?> readFile)
        {
            byte[] manifest = (readFile ?? throw new ArgumentNullException(nameof(readFile)))(RoundContentManifest.FileName)
                ?? throw new ContentIntegrityException(
                    "rounds",
                    "no published round content (" + RoundContentManifest.FileName + " is missing): publish it with `rounds publish --out <dir>`");
            return new RoundContentSet(RoundContentManifest.Read(Encoding.UTF8.GetString(manifest)), readFile);
        }

        public RoundContentManifest Manifest { get; }

        /// <summary>The engine fingerprints every level of this version was proven on.</summary>
        public RoundEngineVersion Engine => Manifest.Engine;

        public int ContentVersion => Manifest.ContentVersion;

        public int LevelCount => Manifest.Levels.Count;

        public bool Has(int levelNumber) => levelNumber >= 1 && levelNumber <= LevelCount;

        public RoundManifestLevel Listed(int levelNumber) =>
            Has(levelNumber) ? Manifest.Levels[levelNumber - 1] : throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber, $"There are {LevelCount} round levels.");

        public RoundLevel GetLevel(int levelNumber) =>
            TryGetLevel(levelNumber, out RoundLevel? level) ? level! : throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber, $"There are {LevelCount} round levels.");

        /// <summary>The level, reading its pack on first use; false for a number past the last level.</summary>
        public bool TryGetLevel(int levelNumber, out RoundLevel? level)
        {
            level = null;
            if (!Has(levelNumber))
            {
                return false;
            }

            lock (_gate)
            {
                if (!_levels.TryGetValue(levelNumber, out level))
                {
                    ReadLevelPack(_packOfLevel[levelNumber]);
                    level = _levels[levelNumber];
                }
            }

            return true;
        }

        public RoundPicture GetPicture(RoundLevel level)
        {
            if (level.PictureId == null)
            {
                throw new ContentIntegrityException("levels", $"level {level.Number} has no picture");
            }

            return GetPicture(level.PictureId, level.PictureVersion);
        }

        /// <summary>A listed picture by id and version, reading the picture pack on first use.</summary>
        public RoundPicture GetPicture(string id, int version)
        {
            string key = RoundPicture.KeyOf(id, version);
            lock (_gate)
            {
                _pictures ??= ReadPicturePack();
                return _pictures.TryGetValue(key, out RoundPicture? picture)
                    ? picture
                    : throw new ContentIntegrityException(_picturePack.Id, $"picture {id} v{version} is not published");
            }
        }

        /// <summary>The packs read so far, by id (a pack is read once, when first needed).</summary>
        public IReadOnlyCollection<string> ReadPacks
        {
            get
            {
                lock (_gate)
                {
                    return new List<string>(_readPacks);
                }
            }
        }

        private byte[] ReadFile(RoundPackEntry pack) =>
            _readFile(pack.Path) ?? throw new ContentIntegrityException(pack.Id, "the build does not carry " + pack.Path);

        private void ReadLevelPack(RoundPackEntry pack)
        {
            IReadOnlyList<RoundLevel> levels = RoundPacks.ReadLevels(pack, ReadFile(pack));
            _readPacks.Add(pack.Id);
            foreach (RoundLevel level in levels)
            {
                if (level.Number < pack.FirstLevel!.Value || level.Number > pack.LastLevel!.Value || _levels.ContainsKey(level.Number))
                {
                    throw new ContentIntegrityException(pack.Id, $"level {level.Number} does not belong in the pack");
                }

                RoundManifestLevel listed = Manifest.Levels[level.Number - 1];
                if (level.DefinitionVersion != listed.DefinitionVersion || level.Class != listed.Class
                    || !string.Equals(level.PictureId, listed.PictureId, StringComparison.Ordinal) || level.PictureVersion != listed.PictureVersion)
                {
                    throw new ContentIntegrityException(pack.Id, $"level {level.Number} differs from the manifest's entry");
                }

                _levels.Add(level.Number, level);
            }

            for (int n = pack.FirstLevel!.Value; n <= pack.LastLevel!.Value; n++)
            {
                if (!_levels.ContainsKey(n))
                {
                    throw new ContentIntegrityException(pack.Id, $"level {n} is missing from the pack");
                }
            }
        }

        private Dictionary<string, RoundPicture> ReadPicturePack()
        {
            var pictures = new Dictionary<string, RoundPicture>(StringComparer.Ordinal);
            foreach (RoundPicture picture in RoundPacks.ReadPictures(_picturePack, ReadFile(_picturePack)))
            {
                if (!pictures.ContainsKey(picture.Key))
                {
                    pictures.Add(picture.Key, picture);
                }
            }

            _readPacks.Add(_picturePack.Id);
            foreach (string key in _listedPictures)
            {
                if (!pictures.ContainsKey(key))
                {
                    throw new ContentIntegrityException(_picturePack.Id, $"the listed picture {key} is missing from the pack");
                }
            }

            return pictures;
        }
    }
}
