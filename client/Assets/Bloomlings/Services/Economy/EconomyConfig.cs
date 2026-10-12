using System;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Services.Economy
{
    /// <summary>
    /// The economy values of one session (T118): the <c>economy.*</c> Remote Config keys with their bundled defaults and
    /// ranges (contracts/backend-services.md). Every value is clamped into its range. The first three prices buy three
    /// charges, while Bloom Burst's price buys one, so their pack prices are not ordered by strength.
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
            return new EconomyConfig(
                Get(RemoteConfigKeys.PetalsBase),
                Get(RemoteConfigKeys.PetalsCleanBonus),
                Get(RemoteConfigKeys.PetalsHardBonus),
                Get(RemoteConfigKeys.PetalsSuperHardBonus),
                Get(RemoteConfigKeys.PriceExtraSlot),
                Get(RemoteConfigKeys.PriceShuffle),
                Get(RemoteConfigKeys.PriceReturn),
                Get(RemoteConfigKeys.PriceBloomBurst),
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
