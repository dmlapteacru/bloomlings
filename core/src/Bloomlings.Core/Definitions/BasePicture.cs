using System.Collections.Generic;

namespace Bloomlings.Core.Definitions
{
    public enum FinishedLookMode
    {
        Auto,
        Illustration,
    }

    /// <summary>How the finished (restored) picture is rendered (FR-007).</summary>
    public sealed record FinishedLook(FinishedLookMode Mode, string? IllustrationKey);

    public sealed record PictureTags(IReadOnlyList<string> Themes, IReadOnlyList<string> Seasons, IReadOnlyList<string> Bands);

    /// <summary>
    /// Structure metrics computed on import (research R7). The background share is stored in per mille so that the
    /// core never needs floating point.
    /// </summary>
    public sealed record PictureStructure(int RegionCount, int NestingDepth, int BackgroundSharePermille);

    public enum ReviewStatus
    {
        Draft,
        Approved,
        Rejected,
    }

    /// <summary>"Only `approved` pictures can be used" (FR-084).</summary>
    public sealed record PictureReview(ReviewStatus Status, string? Reviewer, string? Date, string? Notes);

    public enum PictureSourceKind
    {
        Hand,
        Generated,
        GeneratedEdited,
    }

    /// <summary>Owned or licensed sources only (FR-091).</summary>
    public sealed record PictureSource(PictureSourceKind Kind, string? Origin, string Licence);

    /// <summary>
    /// A base picture of the picture library (FR-006; contracts/base-picture.schema.json; data-model §1.2).
    /// Width and height: "7 ≤ width ≤ 14, 8 ≤ height ≤ 16". Roles: "At least 2 roles; each role has exactly one
    /// color group". <see cref="Grid"/> rows run bottom (index 0) to top; each value is a role index,
    /// <see cref="Empty"/> or <see cref="Stone"/>.
    /// </summary>
    public sealed record BasePicture(
        string Id,
        int Version,
        string Subject,
        int Width,
        int Height,
        IReadOnlyList<PictureRole> Roles,
        IReadOnlyList<IReadOnlyList<int>> Grid,
        FinishedLook FinishedLook,
        PictureTags Tags,
        PictureStructure? Structure,
        PictureReview Review,
        PictureSource Source)
    {
        public const int Empty = -1;
        public const int Stone = -2;

        public const int MinWidth = 7;
        public const int MaxWidth = 14;
        public const int MinHeight = 8;
        public const int MaxHeight = 16;

        /// <summary>Role index, <see cref="Empty"/> or <see cref="Stone"/> at column <paramref name="x"/>, row <paramref name="y"/>.</summary>
        public int CellAt(int x, int y) => Grid[y][x];
    }
}
