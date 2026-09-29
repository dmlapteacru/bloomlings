using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// One attempt at a level, pinned to the content version it started with (R6). A Daily Challenge attempt carries
    /// its UTC date (<c>yyyy-MM-dd</c>); it never changes Level N (FR-064).
    /// </summary>
    public sealed record LevelAttempt(int LevelNumber, LevelDefinition Definition, BasePicture Picture, SessionOptions Options, string? DailyUtcDate = null)
    {
        public bool IsDaily => DailyUtcDate != null;
    }

    /// <summary>
    /// Maps a level number to its definition and picture (T061). Level N is the same for every player because it comes
    /// from the same versioned definition (SC-011). An attempt keeps the content it started with even if a newer
    /// catalog is activated meanwhile; the new content applies from the next attempt (R6).
    /// Past the end of the catalog there is no level: Home says more levels are coming, until a content update extends
    /// the catalog. Only development builds may repeat the catalog from the start (<paramref name="repeatPastEnd"/>),
    /// so a short test catalog can be played on.
    /// </summary>
    public sealed class CatalogService
    {
        private readonly bool _repeatPastEnd;

        public CatalogService(ContentSet content, bool repeatPastEnd = false)
        {
            Content = content;
            _repeatPastEnd = repeatPastEnd;
        }

        public ContentSet Content { get; private set; }

        /// <summary>Whether Level N can be played with the active catalog.</summary>
        public bool HasLevel(int levelNumber) =>
            Content.TryGetLevel(levelNumber, out _) || (_repeatPastEnd && Content.LevelCount > 0 && levelNumber >= 1);

        public int ContentVersion => Content.ContentVersion;

        /// <summary>Activates a newer catalog for future attempts (content updates, T092).</summary>
        public void Activate(ContentSet content) => Content = content;

        /// <summary>An attempt at Level N, or null past the end of the catalog (see <see cref="HasLevel"/>).</summary>
        public LevelAttempt? BeginAttempt(int levelNumber)
        {
            if (!HasLevel(levelNumber))
            {
                return null;
            }

            ContentSet content = Content;
            LevelDefinition definition = content.GetLevel(Resolve(content, levelNumber));
            return new LevelAttempt(
                levelNumber,
                definition,
                content.GetPicture(definition.Picture),
                new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget));
        }

        /// <summary>
        /// The definition used for a level number. Past the end of the catalog (development builds only, see
        /// <see cref="HasLevel"/>) the levels repeat from the start, deterministically.
        /// </summary>
        public static int Resolve(ContentSet content, int levelNumber)
        {
            if (content.TryGetLevel(levelNumber, out _))
            {
                return levelNumber;
            }

            int count = content.LevelCount;
            return content.LevelNumbers[(levelNumber - 1) % count];
        }
    }
}
