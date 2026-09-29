using System;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Services.Economy
{
    /// <summary>
    /// The economy values of one session (T118): the <c>economy.*</c> Remote Config keys with their bundled defaults and
    /// ranges (contracts/backend-services.md). Every value is clamped into its range, and Bloom Burst always stays the
    /// most expensive booster (FR-048). Prices never depend on the level number (FR-041).
    /// </summary>
    public sealed record EconomyConfig(
        int PetalsBase,
        int CleanBonus,
        int HardBonus,
        int SuperHardBonus,
        int PriceExtraSlot,
        int PriceShuffle,
        int PriceReturn,
        int PriceBloomBurst,
        int UnlockGrant,
        int DropEveryLevels)
    {
        /// <summary>The bundled defaults, used offline and before Remote Config answers.</summary>
        public static EconomyConfig Bundled { get; } = Read(key => key.Default);

        public static EconomyConfig From(IRemoteConfigService config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return Read(config.Get);
        }

        /// <summary>Reads every key through <paramref name="value"/> and clamps it (a remote value can be anything).</summary>
        public static EconomyConfig Read(Func<IntKey, int> value)
        {
            int Get(IntKey key) => key.Clamp(value(key));
            int extra = Get(RemoteConfigKeys.PriceExtraSlot);
            int shuffle = Get(RemoteConfigKeys.PriceShuffle);
            int back = Get(RemoteConfigKeys.PriceReturn);
            int othersMax = Math.Max(extra, Math.Max(shuffle, back));
            int burst = Math.Max(Get(RemoteConfigKeys.PriceBloomBurst), othersMax + 1);
            return new EconomyConfig(
                Get(RemoteConfigKeys.PetalsBase),
                Get(RemoteConfigKeys.PetalsCleanBonus),
                Get(RemoteConfigKeys.PetalsHardBonus),
                Get(RemoteConfigKeys.PetalsSuperHardBonus),
                extra,
                shuffle,
                back,
                RemoteConfigKeys.PriceBloomBurst.Clamp(burst),
                Get(RemoteConfigKeys.UnlockGrant),
                Get(RemoteConfigKeys.DropEveryLevels));
        }

        public int Price(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => PriceExtraSlot,
            BoosterKind.Shuffle => PriceShuffle,
            BoosterKind.Return => PriceReturn,
            _ => PriceBloomBurst,
        };
    }
}
