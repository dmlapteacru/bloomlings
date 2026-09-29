using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Definitions
{
    /// <summary>
    /// A color role of a base picture, such as petal, leaf or background. "At least 2 roles; each role has exactly one
    /// color group". A level maps each role to one variant of the same color group.
    /// </summary>
    public sealed record PictureRole(string RoleId, string Name, ColorGroup ColorGroup, bool IsBackground);
}
