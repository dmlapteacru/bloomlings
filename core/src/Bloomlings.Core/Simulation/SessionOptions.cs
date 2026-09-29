using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Per-session settings (contracts/simulation-api.md). <see cref="ContentVersion"/> salts the Shuffle PRNG and
    /// <see cref="ShuffleNodeBudget"/> comes from the content manifest, fixed per content version. Neither is ever read
    /// from Remote Config, app settings or device capabilities (FR-024).
    /// </summary>
    public sealed record SessionOptions(int ContentVersion, int ShuffleNodeBudget)
    {
        /// <summary>The variant catalog used to validate mappings and layers; the launch catalog by default.</summary>
        public VariantCatalog Catalog { get; init; } = VariantCatalog.Default;
    }
}
