using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Bloomlings.Core.Random;
using Bloomlings.Generator.Profiles;

namespace Bloomlings.Generator
{
    /// <summary>
    /// R9 step 1 (T084): picks an approved base picture that fits the profile (size, themes, structure targets and
    /// mechanics) and is not blocked by the FR-083 windows: Levels 1–100 each use a different picture, and a picture
    /// never repeats within 50 consecutive levels.
    /// </summary>
    public sealed class PicturePicker
    {
        public const int UniqueUpToLevel = 100;
        public const int RepeatWindow = 50;

        private readonly List<BasePicture> _library;
        private readonly bool _allowDraft;

        /// <param name="allowDraft">Development previews only: also use pictures that are not approved yet.</param>
        public PicturePicker(IEnumerable<BasePicture> library, bool allowDraft = false)
        {
            _library = new List<BasePicture>(library);
            _library.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            _allowDraft = allowDraft;
        }

        /// <summary>Whether any usable picture has a role of this color group (a variant can only map onto its own group).</summary>
        public bool HasColorGroup(ColorGroup group)
        {
            foreach (BasePicture picture in _library)
            {
                foreach (PictureRole role in picture.Roles)
                {
                    if (role.ColorGroup == group)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IReadOnlyList<BasePicture> Candidates(GenerationProfile profile, int level, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            var blocked = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<int, LevelDefinition> entry in history)
            {
                bool inWindow = entry.Key < level && entry.Key > level - RepeatWindow;
                bool uniqueTier = level <= UniqueUpToLevel && entry.Key <= UniqueUpToLevel;
                if (inWindow || uniqueTier)
                {
                    blocked.Add(entry.Value.Picture.Id);
                }
            }

            var result = new List<BasePicture>();
            foreach (BasePicture picture in _library)
            {
                if (blocked.Contains(picture.Id)
                    || (!_allowDraft && picture.Review.Status != ReviewStatus.Approved)
                    || !profile.BoardWidth.Contains(picture.Width)
                    || !profile.BoardHeight.Contains(picture.Height)
                    || !MatchesThemes(picture, profile)
                    || (HasStones(picture) && !profile.Allows("stone")))
                {
                    continue;
                }

                if (picture.Structure != null
                    && (!profile.StructureTargets.NestingDepth.Contains(picture.Structure.NestingDepth)
                        || !profile.StructureTargets.BackgroundSharePermille.Contains(picture.Structure.BackgroundSharePermille)))
                {
                    continue;
                }

                result.Add(picture.Review.Status == ReviewStatus.Approved ? picture : AsPreview(picture));
            }

            return result;
        }

        public BasePicture? Pick(GenerationProfile profile, int level, IReadOnlyDictionary<int, LevelDefinition> history, ref Xoshiro256StarStar rng)
        {
            IReadOnlyList<BasePicture> candidates = Candidates(profile, level, history);
            return candidates.Count == 0 ? null : candidates[rng.NextInt(candidates.Count)];
        }

        /// <summary>
        /// A draft picture treated as approved in memory, so a development preview can build its board. The library file
        /// is unchanged, so <c>validate</c> still fails such levels on <c>picture-approved</c>.
        /// </summary>
        public static BasePicture AsPreview(BasePicture picture) =>
            picture with { Review = picture.Review with { Status = ReviewStatus.Approved } };

        public static bool HasStones(BasePicture picture)
        {
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int cell in row)
                {
                    if (cell == BasePicture.Stone)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MatchesThemes(BasePicture picture, GenerationProfile profile)
        {
            if (profile.PictureThemes.Count == 0)
            {
                return true;
            }

            foreach (string theme in picture.Tags.Themes)
            {
                foreach (string wanted in profile.PictureThemes)
                {
                    if (string.Equals(theme, wanted, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
