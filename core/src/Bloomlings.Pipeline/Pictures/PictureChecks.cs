using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;

namespace Bloomlings.Pipeline.Pictures
{
    /// <summary>
    /// The automated picture checks that approve a base picture (FR-084 as amended on 2026-10-06: the owner reviewed the
    /// procedural style as a whole, so a picture no longer waits for a person). A picture passes when every role is used
    /// and keeps at least <see cref="BandGuidelines.MinPodSize"/> cells (so each variant a mapping gives it can fill a
    /// pod), every role's color group has a launch variant, the picture's occupancy (its non-empty cells) is
    /// <see cref="MinOccupancyPermille"/>–<see cref="MaxOccupancyPermille"/> per mille (FR-008's 75–95%) and its
    /// structure metrics are computed. <c>pictures import</c> approves a draft that passes; <c>pictures validate</c>
    /// lists what a picture misses.
    /// </summary>
    public static class PictureChecks
    {
        public const int MinOccupancyPermille = 750;

        public const int MaxOccupancyPermille = 950;

        /// <summary>Who approved a picture that passed the checks.</summary>
        public const string Reviewer = "automated picture checks";

        public const string Notes = "Approved by the automated picture checks (FR-084 as amended on 2026-10-06).";

        /// <summary>What keeps <paramref name="picture"/> from passing; empty when it passes.</summary>
        public static IReadOnlyList<string> Problems(BasePicture picture)
        {
            var problems = new List<string>();
            var counts = new int[picture.Roles.Count];
            int cells = 0;
            int empty = 0;
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int cell in row)
                {
                    cells++;
                    if (cell >= 0 && cell < counts.Length)
                    {
                        counts[cell]++;
                    }
                    else if (cell == BasePicture.Empty)
                    {
                        empty++;
                    }
                }
            }

            for (int i = 0; i < picture.Roles.Count; i++)
            {
                PictureRole role = picture.Roles[i];
                if (counts[i] == 0)
                {
                    problems.Add($"role '{role.RoleId}' is not used by the grid");
                }
                else if (counts[i] < BandGuidelines.MinPodSize)
                {
                    problems.Add($"role '{role.RoleId}' has {counts[i]} cells, fewer than the smallest pod ({BandGuidelines.MinPodSize})");
                }

                if (!HasPoolVariant(role.ColorGroup))
                {
                    problems.Add($"role '{role.RoleId}' ({role.ColorGroup}) has no launch variant and no pool expansion");
                }
            }

            int occupancy = cells == 0 ? 0 : (cells - empty) * 1000 / cells;
            if (occupancy < MinOccupancyPermille || occupancy > MaxOccupancyPermille)
            {
                problems.Add(string.Format(CultureInfo.InvariantCulture, "occupancy {0}‰ is outside {1}–{2}‰ (FR-008)", occupancy, MinOccupancyPermille, MaxOccupancyPermille));
            }

            if (picture.Structure == null)
            {
                problems.Add("structure metrics are missing (run pictures import)");
            }

            return problems;
        }

        /// <summary>
        /// A draft that passes the checks, approved by them; any other picture as it is (a person's approval or rejection
        /// stays, and a draft that fails stays a draft).
        /// </summary>
        public static BasePicture ApproveIfPassing(BasePicture picture) =>
            picture.Review.Status == ReviewStatus.Draft && Problems(picture).Count == 0
                ? picture with { Review = new PictureReview(ReviewStatus.Approved, Reviewer, null, Notes) }
                : picture;

        /// <summary>
        /// Whether a role of this color group can ever show a variant: a launch variant, or an expansion variant the pool
        /// adds at a roadmap row (Vine's lime from L45, Berry's red from L200; the owner, 2026-10-07). The generator keeps
        /// such a picture out of the levels before its variant joins (<c>PicturePicker.Candidates</c>).
        /// </summary>
        private static bool HasPoolVariant(ColorGroup group)
        {
            IReadOnlyList<VariantId> expansions = VariantPool.Default.Expansions;
            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                if (info.ColorGroup == group && (info.Status == VariantStatus.Launch || expansions.Contains(info.Id)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
