namespace Bloomlings.Playtest.Design
{
    /// <summary>Design-render capability only. Runtime painters keep the existing purchase size until a variant is chosen.</summary>
    public interface IPurchaseSizePreview
    {
        float PurchaseSize { get; }
    }
}
