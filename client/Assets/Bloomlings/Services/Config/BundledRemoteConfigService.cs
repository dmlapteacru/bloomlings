using System.Collections.Generic;
using System.Globalization;

namespace Bloomlings.Client.Services.Config
{
    /// <summary>
    /// Offline Remote Config: the bundled defaults, optionally overlaid with raw string values (for tests, or as the
    /// fallback layer of the UGS implementation in T126). Never touches the network.
    /// </summary>
    public sealed class BundledRemoteConfigService : IRemoteConfigService
    {
        private readonly IReadOnlyDictionary<string, string> _overrides;

        public BundledRemoteConfigService()
            : this(new Dictionary<string, string>())
        {
        }

        /// <param name="overrides">Raw values by key name, as a remote provider would deliver them.</param>
        public BundledRemoteConfigService(IReadOnlyDictionary<string, string> overrides)
        {
            _overrides = overrides;
        }

        public int Get(IntKey key)
        {
            if (_overrides.TryGetValue(key.Name, out string raw)
                && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return key.Clamp(value);
            }

            return key.Default;
        }

        public bool Get(BoolKey key)
        {
            if (_overrides.TryGetValue(key.Name, out string raw))
            {
                if (raw == "true")
                {
                    return true;
                }

                if (raw == "false")
                {
                    return false;
                }
            }

            return key.Default;
        }

        public string Get(StringKey key)
        {
            if (_overrides.TryGetValue(key.Name, out string raw) && key.Validate(raw))
            {
                return raw;
            }

            return key.Default;
        }
    }
}
