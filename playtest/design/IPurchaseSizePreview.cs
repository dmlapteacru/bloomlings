namespace Bloomlings.Playtest.Design
{
    /// <summary>Design-render override for comparing purchase faces. Runtime uses the approved scale.</summary>
    public interface IPurchaseSizePreview
    {
        float PurchaseSize { get; }
    }
}
