using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>One attempt at a level, pinned to the content version it started with (R6).</summary>
    public sealed record LevelAttempt(int LevelNumber, LevelDefinition Definition, BasePicture Picture, SessionOptions Options);

    /// <summary>
    /// Maps a level number to its definition and picture (T061). Level N is the same for every player because it comes
    /// from the same versioned definition (SC-011). An attempt keeps the content it started with even if a newer
    /// catalog is activated meanwhile; the new content applies from the next attempt (R6).
    /// </summary>
    public sealed class CatalogService
    {
        public CatalogService(ContentSet content)
        {
            Content = content;
        }

        public ContentSet Content { get; private set; }

        public int ContentVersion => Content.ContentVersion;

        /// <summary>Activates a newer catalog for future attempts (content updates, T092).</summary>
        public void Activate(ContentSet content) => Content = content;

        public LevelAttempt BeginAttempt(int levelNumber)
        {
            ContentSet content = Content;
            LevelDefinition definition = content.GetLevel(Resolve(content, levelNumber));
            return new LevelAttempt(
                levelNumber,
                definition,
                content.GetPicture(definition.Picture),
                new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget));
        }

        /// <summary>
        /// The definition used for a level number. Past the end of the catalog (only possible with development
        /// content) the levels repeat from the start, deterministically, until a content update extends the catalog.
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
