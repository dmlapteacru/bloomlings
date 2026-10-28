using System;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// One attempt at a round level (spec 006 FR-060), pinned to the published content version it started with, as the
    /// legacy <see cref="LevelAttempt"/> is (R6). The Gameplay scene plays it on the round rules (<c>RoundTimeline</c>,
    /// T058).
    /// </summary>
    public sealed record RoundAttempt(int LevelNumber, RoundLevel Level, RoundPicture Picture, int ContentVersion);

    /// <summary>
    /// The game's levels when the build carries published round content (spec 006 FR-060, T060): Level N is round level N
    /// of <see cref="Source"/>, and a level past the last one has no level yet, so the game shows "new levels soon"
    /// (<see cref="IsPastLastLevel"/>) and no old level follows. Engine-free: the Unity client's <c>GameFlow</c> and
    /// Home decide with it, and the DotnetCheck tests play its attempts.
    /// </summary>
    public sealed class RoundCatalogService
    {
        public RoundCatalogService(IRoundLevelSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>The active published round content.</summary>
        public IRoundLevelSource Source { get; private set; }

        public int ContentVersion => Source.ContentVersion;

        /// <summary>How many round levels the game has: 1..<see cref="LevelCount"/>.</summary>
        public int LevelCount => Source.LevelCount;

        /// <summary>Whether Level N is a round level of the active content.</summary>
        public bool HasLevel(int levelNumber) => Source.Has(levelNumber);

        /// <summary>
        /// Whether Level N is past the last round level: the game shows "new levels soon" (<c>round.soon.*</c>) instead of a
        /// level, until a later content version adds levels.
        /// </summary>
        public bool IsPastLastLevel(int levelNumber) => levelNumber > LevelCount;

        /// <summary>Activates newer round content for future attempts; an attempt keeps the content it started with.</summary>
        public void Activate(IRoundLevelSource source) => Source = source ?? throw new ArgumentNullException(nameof(source));

        /// <summary>An attempt at round level N with its picture, or null when Level N is not a round level.</summary>
        public RoundAttempt? BeginAttempt(int levelNumber)
        {
            IRoundLevelSource source = Source;
            if (!source.Has(levelNumber))
            {
                return null;
            }

            RoundLevel level = source.GetLevel(levelNumber);
            return new RoundAttempt(levelNumber, level, source.GetPicture(level), source.ContentVersion);
        }
    }
}
