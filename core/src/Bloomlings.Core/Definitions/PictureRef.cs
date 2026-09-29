namespace Bloomlings.Core.Definitions
{
    public enum Mirror
    {
        None,
        Horizontal,
    }

    /// <summary>Reference to the base picture a level is built from (FR-006).</summary>
    public sealed record PictureRef(string Id, int Version, Mirror Mirror, string BackgroundTreatment);
}
