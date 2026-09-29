namespace Bloomlings.Core.Variants
{
    /// <summary>
    /// Color group of a variant and of a picture role. A role may only map to a variant of the same group (FR-006).
    /// </summary>
    public enum ColorGroup
    {
        Green,
        PinkPurple,
        BlueCyan,
        BrownOrange,
        Lime,
        Red,
        Indigo,
        Gold,
    }

    /// <summary>Whether a variant belongs to the 8-variant launch pool or to a later expansion.</summary>
    public enum VariantStatus
    {
        Launch,
        Expansion,
    }
}
